using System.Runtime.InteropServices;

namespace Singulink.IO;

internal static partial class Interop
{
    internal static partial class Unix
    {
        /// <summary>
        /// Flushes the entries of a directory to disk. .NET has no API for this: it refuses to open a handle to a directory.
        /// </summary>
        /// <returns><see langword="false"/> if the directory was not found: it or a parent directory does not exist, or the path or a parent directory in it
        /// points to a file. The caller tells those apart. Otherwise <see langword="true"/>.</returns>
        public static bool FlushDirectory(IAbsoluteDirectoryPath.Impl dir)
        {
            int fd;

            // PathExport ends with a separator, so the open fails with ENOTDIR if the path points to a file.
            while ((fd = UnixNative.Open(dir.PathExport, UnixNative.OpenDirectoryFlags)) < 0)
            {
                int errno = Marshal.GetLastPInvokeError();

                if (errno is UnixNative.Errors.ENOENT or UnixNative.Errors.ENOTDIR)
                    return false;

                if (errno is not UnixNative.Errors.EINTR)
                    throw GetException(dir, "open", errno);
            }

            try
            {
                // On Apple platforms fsync leaves the data in the drive's write cache, where a power loss can still take it. F_FULLFSYNC flushes that cache
                // too, and is what .NET flushes files with there. It is not supported by every file system (network mounts in particular), so any failure
                // falls back to fsync, which then reports the error if there really is one.
                if (UnixNative.HasFullFSync && FullFSync(fd))
                    return true;

                while (UnixNative.FSync(fd) is not 0)
                {
                    int errno = Marshal.GetLastPInvokeError();

                    if (errno is UnixNative.Errors.EINTR)
                        continue;

                    // Some file systems (network mounts in particular) refuse fsync on a directory with EINVAL, EBADF or ENOTSUP. Throwing would fail every
                    // directory flush on such a mount, so, as PostgreSQL does for directory fsyncs, those are treated as "not supported here" and ignored:
                    // the directory entries then rely on the file system's own guarantees. Any other error is a real failure and is thrown. That includes
                    // EROFS, which ext4 fails fsync with after it has aborted its journal and remounted itself read-only.
                    if (errno is UnixNative.Errors.EINVAL or UnixNative.Errors.EBADF || errno == UnixNative.Errors.ENOTSUP)
                        break;

                    throw GetException(dir, "fsync", errno);
                }

                return true;
            }
            finally
            {
                // Not retried on EINTR: the descriptor is released regardless on Linux, and retrying could close one reused by another thread.
                _ = UnixNative.Close(fd);
            }
        }

        private static bool FullFSync(int fd)
        {
            while (UnixNative.FCntl(fd, UnixNative.F_FULLFSYNC) < 0)
            {
                if (Marshal.GetLastPInvokeError() is not UnixNative.Errors.EINTR)
                    return false;
            }

            return true;
        }

        private static IOException GetException(IAbsoluteDirectoryPath.Impl dir, string function, int errno)
        {
            string error = Marshal.GetPInvokeErrorMessage(errno);
            string message = $"Failed to flush directory '{dir.PathDisplay}' to disk: {function} failed with error {errno} ({error}).";

            if (errno is UnixNative.Errors.EACCES or UnixNative.Errors.EPERM)
                return new UnauthorizedIOAccessException(message, errno);

            if (errno == UnixNative.Errors.ENAMETOOLONG)
                return new PathTooLongException(message);

            return new IOException(message, errno);
        }
    }
}
