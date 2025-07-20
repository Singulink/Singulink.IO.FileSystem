using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Singulink.IO;

internal static partial class Interop
{
    internal static partial class Windows
    {
        public static Exception GetLastWin32ErrorException(IAbsoluteDirectoryPath.Impl path)
        {
            int error = Marshal.GetLastWin32Error();

            Debug.Assert(error != 0, "no error");

            var win32Ex = new Win32Exception(error);
            string message = $"{win32Ex.Message} Path: '{path.PathDisplay}'.";

            switch (error)
            {
                case WindowsNative.Errors.FILE_NOT_FOUND:
                case WindowsNative.Errors.PATH_NOT_FOUND:
                case WindowsNative.Errors.INVALID_DRIVE:
                    return new DirectoryNotFoundException(message, win32Ex);
                case WindowsNative.Errors.ACCESS_DENIED:
                    return new UnauthorizedIOAccessException(message, win32Ex);
                case WindowsNative.Errors.FILENAME_EXCED_RANGE:
                    return new PathTooLongException(message, win32Ex);
                default:
                    path.EnsureExists(); // Throw DirectoryNotFound exception instead of IOException if path is a file.
                    return new IOException(message, win32Ex);
            }
        }
    }
}
