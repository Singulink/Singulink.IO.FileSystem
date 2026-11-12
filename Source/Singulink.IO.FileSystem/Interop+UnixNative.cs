using System.Runtime.InteropServices;

#pragma warning disable SA1310 // Field names should not contain underscore

namespace Singulink.IO;

internal static partial class Interop
{
    private static partial class UnixNative
    {
        /// <summary>
        /// The <c>fcntl</c> command on Apple platforms that flushes a file and then asks the drive to flush its write cache to the media.
        /// </summary>
        public const int F_FULLFSYNC = 51;

        /// <summary>
        /// The value that stands for the current directory where <c>renameat2</c> takes a directory descriptor. Linux only.
        /// </summary>
        public const int AT_FDCWD = -100;

        /// <summary>
        /// The <c>renameat2</c> flag that fails the rename with <c>EEXIST</c> instead of replacing the destination. Linux only.
        /// </summary>
        public const uint RENAME_NOREPLACE = 1;

        /// <summary>
        /// Gets a value indicating whether <see cref="F_FULLFSYNC"/> is available, which is the case on Apple platforms only.
        /// </summary>
        public static readonly bool HasFullFSync =
            OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst() || OperatingSystem.IsIOS() || OperatingSystem.IsTvOS() || OperatingSystem.IsWatchOS();

        /// <summary>
        /// The flags to open a directory with for reading: <c>O_RDONLY</c> (0 everywhere) and <c>O_CLOEXEC</c>.
        /// </summary>
        /// <remarks>
        /// Close-on-exec is requested atomically through the flags so that a process started concurrently on another thread never inherits the descriptor.
        /// Its value differs per platform, but setting it afterwards through <c>fcntl</c> is not an option: the flag would be the variadic third argument,
        /// and Apple's arm64 ABI passes variadic arguments on the stack, where a fixed-signature P/Invoke never puts them. On platforms that are not listed
        /// the descriptor is briefly inheritable.
        /// </remarks>
        public static readonly int OpenDirectoryFlags =
            IsLinux ? 0x80000 :
            HasFullFSync ? 0x1000000 :
            OperatingSystem.IsFreeBSD() ? 0x100000 :
            0;

        /// <summary>
        /// Gets a value indicating whether the platform is Linux, which Android is too although <see cref="OperatingSystem.IsLinux"/> is false there.
        /// </summary>
        public static bool IsLinux => OperatingSystem.IsLinux() || OperatingSystem.IsAndroid();

        // The mode argument that open takes with O_CREAT is left out: it is variadic, and is not read without that flag.
        [LibraryImport("libc", EntryPoint = "open", SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
        public static partial int Open(string path, int flags);

        [LibraryImport("libc", EntryPoint = "fsync", SetLastError = true)]
        public static partial int FSync(int fd);

        // Only for commands that take no third argument, such as F_FULLFSYNC (see OpenDirectoryFlags for why).
        [LibraryImport("libc", EntryPoint = "fcntl", SetLastError = true)]
        public static partial int FCntl(int fd, int command);

        [LibraryImport("libc", EntryPoint = "close", SetLastError = true)]
        public static partial int Close(int fd);

        [LibraryImport("libc", EntryPoint = "rename", SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
        public static partial int Rename(string oldPath, string newPath);

        // Linux only, and not in every C library there: glibc has it from 2.28, and calling it throws EntryPointNotFoundException where it is missing.
        [LibraryImport("libc", EntryPoint = "renameat2", SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
        public static partial int RenameAt2(int oldDirFd, string oldPath, int newDirFd, string newPath, uint flags);

        internal static class Errors
        {
            public const int EPERM = 1;
            public const int ENOENT = 2;
            public const int EINTR = 4;
            public const int EBADF = 9;
            public const int EACCES = 13;
            public const int EEXIST = 17;
            public const int EXDEV = 18;
            public const int ENOTDIR = 20;
            public const int EISDIR = 21;
            public const int EINVAL = 22;

            // The values below are those of Linux, and otherwise those of the BSDs (including Apple platforms).

            public static readonly int ENAMETOOLONG = IsLinux ? 36 : 63;
            public static readonly int ENOSYS = IsLinux ? 38 : 78;
            public static readonly int ENOTSUP = IsLinux ? 95 : 45;
        }
    }
}
