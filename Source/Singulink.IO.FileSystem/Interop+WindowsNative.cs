using System.IO;
using System.Runtime.InteropServices;

#pragma warning disable SA1310 // Field names should not contain underscore

namespace Singulink.IO;

internal static partial class Interop
{
    private static class WindowsNative
    {
        public const uint SEM_FAILCRITICALERRORS = 0x0001;

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetThreadErrorMode(uint dwNewMode, out uint lpOldMode);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetDiskFreeSpaceEx(string lpDirectoryName, out long lpFreeBytesAvailable, out long lpTotalNumberOfBytes, out long lpTotalNumberOfFreeBytes);

        /// <summary>
        /// A trailing backslash is required.
        /// </summary>
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern DriveType GetDriveType(string lpRootPathName);

        /// <summary>
        /// A trailing backslash is required.
        /// </summary>
        [DllImport("kernel32.dll", EntryPoint = "GetVolumeInformation", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern unsafe bool GetVolumeInformation(string drive, char* volumeName, int volumeNameBufLen, int* volSerialNumber, int* maxFileNameLen, out int fileSystemFlags, char* fileSystemName, int fileSystemNameBufLen);

        internal static class Errors
        {
            public const int FILE_NOT_FOUND = 0x2;
            public const int PATH_NOT_FOUND = 0x3;
            public const int ACCESS_DENIED = 0x5;
            public const int INVALID_DRIVE = 0xF;
            public const int BAD_NETPATH = 0x35;
            public const int BAD_NET_NAME = 0x43;
            public const int DIR_NOT_ROOT = 0x90;
            public const int FILENAME_EXCED_RANGE = 0xCE;
        }
    }
}