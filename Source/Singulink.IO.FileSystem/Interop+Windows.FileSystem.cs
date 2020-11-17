using System;

namespace Singulink.IO
{
    internal static partial class Interop
    {
        internal static partial class Windows
        {
            public static unsafe string GetFileSystem(IAbsoluteDirectoryPath.Impl rootDir)
            {
                // rootDir must be a symlink or root drive

                const int MAX_LENGTH = 261; // MAX_PATH + 1

                char* fileSystemName = stackalloc char[MAX_LENGTH];

                using (MediaInsertionPromptGuard.Enter()) {
                    if (!WindowsNative.GetVolumeInformation(rootDir.PathExportWithTrailingSeparator, null, 0, null, null, out int fileSystemFlags, fileSystemName, MAX_LENGTH)) {
                        throw GetLastWin32ErrorException(rootDir);
                    }
                }

                return new string(fileSystemName);
            }
        }
    }
}
