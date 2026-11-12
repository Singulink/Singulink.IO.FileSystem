namespace Singulink.IO;

internal static partial class Interop
{
    internal static partial class Windows
    {
        /// <summary>
        /// Gets a value indicating whether the path points to an existing directory, without allocating: <see cref="File.GetAttributes(string)"/> copies a
        /// directory's path to trim its trailing separator. A <see langword="false"/> result does not say what is wrong.
        /// </summary>
        public static bool IsExistingDirectory(IAbsoluteDirectoryPath.Impl dir)
        {
            using (MediaInsertionPromptGuard.Enter())
            {
                int attributes = WindowsNative.GetFileAttributes(dir.PathExport);
                return attributes is not WindowsNative.INVALID_FILE_ATTRIBUTES && ((FileAttributes)attributes).HasAllFlags(FileAttributes.Directory);
            }
        }
    }
}
