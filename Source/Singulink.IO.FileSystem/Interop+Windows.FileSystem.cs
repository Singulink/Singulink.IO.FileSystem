using System;

namespace Singulink.IO
{
    internal static partial class Interop
    {
        internal static partial class Windows
        {
            public static unsafe string GetFileSystem(IAbsoluteDirectoryPath path)
            {
                const int MAX_LENGTH = 261; // MAX_PATH + 1

                char* fileSystemName = stackalloc char[MAX_LENGTH];

                using (MediaInsertionPromptGuard.Enter()) {
                    if (!WindowsNative.GetVolumeInformation(path.PathExportWithTrailingSeparator, null, 0, null, null, out int fileSystemFlags, fileSystemName, MAX_LENGTH)) {
                        throw GetLastWin32ErrorDirException(path);
                    }
                }

                return new string(fileSystemName);
            }
        }
    }
}
