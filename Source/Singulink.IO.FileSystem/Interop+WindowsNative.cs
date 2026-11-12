using System.Runtime.InteropServices;

#pragma warning disable SA1310 // Field names should not contain underscore

namespace Singulink.IO;

internal static partial class Interop
{
    private static class WindowsNative
    {
        public const uint SEM_FAILCRITICALERRORS = 0x0001;
        public const int INVALID_FILE_ATTRIBUTES = -1;
        public const nint INVALID_HANDLE_VALUE = -1;

        public const uint DELETE = 0x00010000;
        public const uint OPEN_EXISTING = 3;
        public const uint FILE_FLAG_OPEN_REPARSE_POINT = 0x00200000;

        /// <summary>
        /// The <c>FILE_INFO_BY_HANDLE_CLASS</c> value that <see cref="FILE_RENAME_INFO"/> is passed with.
        /// </summary>
        public const int FileRenameInfo = 3;

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetThreadErrorMode(uint dwNewMode, out uint lpOldMode);

        /// <summary>
        /// Returns <see cref="INVALID_HANDLE_VALUE"/> if it fails.
        /// </summary>
        [DllImport("kernel32.dll", EntryPoint = "CreateFileW", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern nint CreateFile(string lpFileName, uint dwDesiredAccess, FileShare dwShareMode, nint lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, nint hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern unsafe bool SetFileInformationByHandle(nint hFile, int fileInformationClass, void* lpFileInformation, uint dwBufferSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(nint hObject);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetDiskFreeSpaceEx(string lpDirectoryName, out long lpFreeBytesAvailable, out long lpTotalNumberOfBytes, out long lpTotalNumberOfFreeBytes);

        /// <summary>
        /// Returns <see cref="INVALID_FILE_ATTRIBUTES"/> if it fails.
        /// </summary>
        [DllImport("kernel32.dll", EntryPoint = "GetFileAttributesW", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int GetFileAttributes(string lpFileName);

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

        /// <summary>
        /// The header of the information that a file is renamed with, which the new path follows from <see cref="FileName"/> on.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct FILE_RENAME_INFO
        {
            /// <summary>
            /// A <c>BOOLEAN</c> in a union with the flags of <c>FileRenameInfoEx</c>, so it takes four bytes.
            /// </summary>
            public uint ReplaceIfExists;

            public nint RootDirectory;

            /// <summary>
            /// The length of the new path in bytes, without its terminating null character.
            /// </summary>
            public uint FileNameLength;

            public char FileName;
        }

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
