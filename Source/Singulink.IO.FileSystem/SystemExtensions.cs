using System;
using System.IO;

namespace Singulink.IO
{
    /// <summary>
    /// Provides extension methods that convert System.IO types to Singulink.IO.FileSystem types.
    /// </summary>
    public static class SystemExtensions
    {
        /// <summary>
        /// Gets the absolute directory path represented by the <see cref="DirectoryInfo"/> using the specified options.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method disallows unfriendly names by default, but any silent path modifications performed by <see cref="DirectoryInfo"/> (i.e. trimming of
        /// trailing spaces and dots) will remain intact.</para>
        /// <para>
        /// The <see cref="FileSystemInfo.FullName"/> property is used to get the absolute path that is parsed.</para>
        /// </remarks>
        public static IAbsoluteDirectoryPath ToPath(this DirectoryInfo dirInfo, PathOptions options = PathOptions.NoUnfriendlyNames)
        {
            return DirectoryPath.ParseAbsolute(dirInfo.FullName, options);
        }

        /// <summary>
        /// Gets the absolute file path represented by the <see cref="FileInfo"/> using the specified options.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method disallows unfriendly names by default, but any silent path modifications performed by <see cref="FileInfo"/> (i.e. trimming of trailing
        /// spaces and dots) will remain intact.</para>
        /// <para>
        /// The <see cref="FileSystemInfo.FullName"/> property is used to get the absolute path that is parsed.</para>
        /// </remarks>
        public static IAbsoluteFilePath ToPath(this FileInfo fileInfo, PathOptions options = PathOptions.NoUnfriendlyNames)
        {
            return FilePath.ParseAbsolute(fileInfo.FullName, options);
        }
    }
}