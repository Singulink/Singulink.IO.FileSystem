using System.Runtime.InteropServices;

namespace Singulink.IO;

internal static partial class Interop
{
    internal static partial class Unix
    {
        // Set once renameat2 turns out to be missing from the C library, so that it is not looked up again.
        private static volatile bool _renameAt2Missing;

        /// <summary>
        /// Renames a file with a single system call, so that of several renames of the same file only one can succeed, and without ever copying it.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <see cref="File.Move(string, string, bool)"/> gives neither guarantee: it copies the file and then deletes the source if the destination is on
        /// another file system, and without overwriting it can also end up linking the file under its new name and unlinking the source. A second mover that
        /// does the same at the same time finds nothing wrong (a source that is already gone is not an error for the delete), so both report success.</para>
        /// <para>
        /// The caller checks that the source is not a directory, which <c>rename</c> moves as readily as a file.</para>
        /// </remarks>
        public static void MoveFileExclusive(IAbsoluteFilePath.Impl source, IAbsoluteFilePath destination, bool overwrite)
        {
            string sourcePath = source.PathExport;
            string destinationPath = destination.PathExport;
            int errno;

            if (overwrite)
            {
                errno = Rename(sourcePath, destinationPath);
            }
            else if (!TryRenameNoReplace(sourcePath, destinationPath, out errno))
            {
                // The platform or the file system cannot refuse to replace the destination as part of the rename, so check first, as .NET does. A
                // destination that is created between the check and the rename is replaced. That only one rename of the source can succeed still holds.
                errno = Path.Exists(destinationPath) ? UnixNative.Errors.EEXIST : Rename(sourcePath, destinationPath);
            }

            if (errno is 0)
                return;

            if (errno is UnixNative.Errors.ENOENT)
            {
                // The source was found a moment ago, so either its new directory does not exist or the file has just been moved or deleted.
                var destinationDir = destination.ParentDirectory;

                if (destinationDir.Exists)
                    throw Ex.NotFound(source);

                throw Ex.NotFound(destinationDir);
            }

            if (errno is UnixNative.Errors.EISDIR)
                throw Ex.FileIsDir(destination);

            string error = Marshal.GetPInvokeErrorMessage(errno);
            string message = $"Failed to move file '{source.PathDisplay}' to '{destination.PathDisplay}': rename failed with error {errno} ({error}).";

            if (errno is UnixNative.Errors.EACCES or UnixNative.Errors.EPERM)
                throw new UnauthorizedIOAccessException(message, errno);

            if (errno == UnixNative.Errors.ENAMETOOLONG)
                throw new PathTooLongException(message);

            throw new IOException(message, errno);
        }

        /// <summary>
        /// Renames a file, replacing the destination if it exists, and returns zero or the error number.
        /// </summary>
        private static int Rename(string sourcePath, string destinationPath)
        {
            while (UnixNative.Rename(sourcePath, destinationPath) is not 0)
            {
                int errno = Marshal.GetLastPInvokeError();

                if (errno is not UnixNative.Errors.EINTR)
                    return errno;
            }

            return 0;
        }

        /// <summary>
        /// Renames a file with <c>renameat2</c> and <c>RENAME_NOREPLACE</c>, which fails with <c>EEXIST</c> if the destination exists, as one operation.
        /// </summary>
        /// <returns><see langword="false"/> if that is not supported here and nothing was done: the platform is not Linux, the C library or the kernel
        /// does not have the call, or the file system does not support the flag (NFS, for example). Otherwise <see langword="true"/>, with zero or the
        /// error number in <paramref name="errno"/>.</returns>
        private static bool TryRenameNoReplace(string sourcePath, string destinationPath, out int errno)
        {
            errno = 0;

            if (!UnixNative.IsLinux || _renameAt2Missing)
                return false;

            try
            {
                while (UnixNative.RenameAt2(UnixNative.AT_FDCWD, sourcePath, UnixNative.AT_FDCWD, destinationPath, UnixNative.RENAME_NOREPLACE) is not 0)
                {
                    errno = Marshal.GetLastPInvokeError();

                    if (errno is UnixNative.Errors.EINTR)
                        continue;

                    // A file system that does not support the flag fails with EINVAL, and a kernel without the call with ENOSYS.
                    return errno is not UnixNative.Errors.EINVAL && errno != UnixNative.Errors.ENOSYS && errno != UnixNative.Errors.ENOTSUP;
                }

                errno = 0;
                return true;
            }
            catch (EntryPointNotFoundException)
            {
                _renameAt2Missing = true;
                return false;
            }
        }
    }
}
