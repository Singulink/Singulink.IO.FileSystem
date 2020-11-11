using System;
using System.IO;

namespace Singulink.IO
{
    internal static partial class Interop
    {
        internal static partial class Windows
        {
            public static DriveType GetDriveType(IAbsoluteDirectoryPath path) => WindowsNative.GetDriveType(path.PathExportWithTrailingSeparator);
        }
    }
}
