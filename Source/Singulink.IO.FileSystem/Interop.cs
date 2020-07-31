using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

#pragma warning disable SA1310 // Field names should not contain underscore

namespace Singulink.IO
{
    internal static class Interop
    {
        internal const uint SEM_FAILCRITICALERRORS = 0x0001;

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool SetThreadErrorMode(uint dwNewMode, out uint lpOldMode);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetDiskFreeSpaceEx(
            string lpDirectoryName,
            out long lpFreeBytesAvailable,
            out long lpTotalNumberOfBytes,
            out long lpTotalNumberOfFreeBytes);

        internal static class Errors
        {
            internal const int ERROR_PATH_NOT_FOUND = 0x3;
            internal const int ERROR_INVALID_DRIVE = 0xF;
        }
    }
}
