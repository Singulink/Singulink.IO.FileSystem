using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Singulink.IO
{
    internal static class DiskSpace
    {
        internal static long GetAvailableFreeSpace(string dirPath, PathFormat pathFormat)
        {
            pathFormat.EnsureCurrent();

            if (pathFormat == PathFormat.Unix)
                return new DriveInfo(dirPath).AvailableFreeSpace;

            GetSpaceWindows(dirPath, out long available, out _, out _);
            return available;
        }

        internal static long GetTotalFreeSpace(string dirPath, PathFormat pathFormat)
        {
            pathFormat.EnsureCurrent();

            if (pathFormat == PathFormat.Unix)
                return new DriveInfo(dirPath).TotalFreeSpace;

            GetSpaceWindows(dirPath, out _, out _, out long totalFree);
            return totalFree;
        }

        internal static long GetTotalSize(string dirPath, PathFormat pathFormat)
        {
            pathFormat.EnsureCurrent();

            if (pathFormat == PathFormat.Unix)
                return new DriveInfo(dirPath).TotalSize;

            GetSpaceWindows(dirPath, out _, out long totalSize, out _);
            return totalSize;
        }

        private static void GetSpaceWindows(string dirPath, out long availableBytes, out long totalBytes, out long freeBytes)
        {
            bool modeSet = Interop.SetThreadErrorMode(Interop.SEM_FAILCRITICALERRORS, out uint oldMode);

            try {
                bool result = Interop.GetDiskFreeSpaceEx(dirPath, out availableBytes, out totalBytes, out freeBytes);

                if (!result) {
                    int error = Marshal.GetLastWin32Error();

                    if (error == Interop.Errors.ERROR_INVALID_DRIVE)
                        throw new DriveNotFoundException("Invalid drive: " + dirPath);

                    if (error == Interop.Errors.ERROR_PATH_NOT_FOUND)
                        throw new DirectoryNotFoundException("Directory not found: " + dirPath);

                    throw new Win32Exception(error, dirPath);
                }
            }
            finally {
                if (modeSet)
                    Interop.SetThreadErrorMode(oldMode, out _);
            }
        }
    }
}
