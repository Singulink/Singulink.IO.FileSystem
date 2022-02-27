using System;

namespace Singulink.IO
{
    internal static partial class Interop
    {
        internal static partial class Windows
        {
            public static void GetSpace(IAbsoluteDirectoryPath.Impl path, out long availableBytes, out long totalBytes, out long freeBytes)
            {
                using (MediaInsertionPromptGuard.Enter()) {
                    if (!WindowsNative.GetDiskFreeSpaceEx(path.PathExport, out availableBytes, out totalBytes, out freeBytes))
                        throw GetLastWin32ErrorException(path);
                }
            }
        }
    }
}