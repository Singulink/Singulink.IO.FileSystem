using System.Buffers;
using System.Runtime.InteropServices;

namespace Singulink.IO;

internal static partial class Interop
{
    internal static partial class Windows
    {
        /// <summary>
        /// Renames a file through a handle that keeps every other rename and deletion of it out for as long as it is open.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <c>MoveFileEx</c>, which <see cref="File.Move(string, string, bool)"/> calls, opens the source sharing deletion, so two of them moving the same
        /// file both open it and both rename it, and both report success. Here the source is opened with delete access (which a rename needs) but without
        /// sharing deletion: a second open that asks for delete access fails with a sharing violation while the first handle is open, and with file not
        /// found once the file is renamed. Handles that only read or write and share deletion conflict with neither.</para>
        /// <para>
        /// A reparse point is opened as itself, so a link is renamed and not its target. A directory cannot be opened this way (that needs
        /// <c>FILE_FLAG_BACKUP_SEMANTICS</c>), so it fails with access denied.</para>
        /// </remarks>
        public static unsafe void MoveFileExclusive(IAbsoluteFilePath.Impl source, IAbsoluteFilePath destination, bool overwrite)
        {
            const int StackBufferSize = 1024;

            const FileShare Share = FileShare.Read | FileShare.Write;
            const uint Flags = WindowsNative.FILE_FLAG_OPEN_REPARSE_POINT;

            nint handle;
            int openError = 0;

            using (MediaInsertionPromptGuard.Enter())
            {
                handle = WindowsNative.CreateFile(source.PathExport, WindowsNative.DELETE, Share, 0, WindowsNative.OPEN_EXISTING, Flags, 0);

                // Read before the guard is disposed, which sets the last error again.
                if (handle is WindowsNative.INVALID_HANDLE_VALUE)
                    openError = Marshal.GetLastWin32Error();
            }

            if (handle is WindowsNative.INVALID_HANDLE_VALUE)
            {
                // A file that is not found here is the source file, and a path that is not found is its parent directory.
                throw openError switch {
                    WindowsNative.Errors.FILE_NOT_FOUND => Ex.NotFound(source),
                    WindowsNative.Errors.PATH_NOT_FOUND => Ex.NotFound(source.ParentDirectory),
                    _ => GetException(openError, source),
                };
            }

            // The new path follows the header, with a terminating null character. The export path is in the form that the rename accepts for any length.
            string newPath = destination.PathExport;
            int size = sizeof(WindowsNative.FILE_RENAME_INFO) + (newPath.Length * sizeof(char));
            byte[]? rented = null;
            int renameError = 0;

            try
            {
                Span<byte> buffer = size <= StackBufferSize ? stackalloc byte[StackBufferSize] : (rented = ArrayPool<byte>.Shared.Rent(size));

                fixed (byte* bufferPtr = buffer)
                {
                    var info = (WindowsNative.FILE_RENAME_INFO*)bufferPtr;
                    info->ReplaceIfExists = overwrite ? 1u : 0u;
                    info->RootDirectory = 0;
                    info->FileNameLength = (uint)(newPath.Length * sizeof(char));

                    var fileName = new Span<char>(&info->FileName, newPath.Length + 1);
                    newPath.CopyTo(fileName);
                    fileName[newPath.Length] = '\0';

                    if (!WindowsNative.SetFileInformationByHandle(handle, WindowsNative.FileRenameInfo, info, (uint)size))
                        renameError = Marshal.GetLastWin32Error();
                }
            }
            finally
            {
                WindowsNative.CloseHandle(handle);

                if (rented is not null)
                    ArrayPool<byte>.Shared.Return(rented);
            }

            if (renameError is not 0)
            {
                // The source is open, so anything that is not found here is the directory that the file was to be moved to.
                throw renameError switch {
                    WindowsNative.Errors.FILE_NOT_FOUND or WindowsNative.Errors.PATH_NOT_FOUND => Ex.NotFound(destination.ParentDirectory),
                    _ => GetException(renameError, destination),
                };
            }
        }

        private static IOException GetException(int error, IAbsoluteFilePath path)
        {
            string message = $"{Marshal.GetPInvokeErrorMessage(error)} Path: '{path.PathDisplay}'.";
            int hresult = unchecked((int)0x80070000) | error;

            return error switch {
                WindowsNative.Errors.ACCESS_DENIED => new UnauthorizedIOAccessException(message, hresult),
                WindowsNative.Errors.FILENAME_EXCED_RANGE => new PathTooLongException(message),
                _ => new IOException(message, hresult),
            };
        }
    }
}
