using System;
using System.IO;
using System.Linq;

namespace Singulink.IO
{
    /// <summary>
    /// Represents an absolute or relative path to a directory.
    /// </summary>
    public interface IDirectoryPath : IPath
    {
        /// <summary>
        /// Combines a directory with a relative directory.
        /// </summary>
        public static IDirectoryPath operator +(IDirectoryPath x, IRelativeDirectoryPath y) => x.Combine(y);

        /// <summary>
        /// Combines a directory with a relative file.
        /// </summary>
        public static IFilePath operator +(IDirectoryPath x, IRelativeFilePath y) => x.Combine(y);

        /// <summary>
        /// Combines a directory with a relative entry.
        /// </summary>
        public static IPath operator +(IDirectoryPath x, IRelativePath y) => x.Combine(y);

        #region Combining

        // Directory

        /// <summary>
        /// Combines this directory with a relative directory.
        /// </summary>
        /// <param name="path">The relative directory to apprend to this directory.</param>
        IDirectoryPath Combine(IRelativeDirectoryPath path);

        /// <summary>
        /// Combines this directory with a relative directory path parsed using the specified options and this directory's path format.
        /// </summary>
        /// <param name="path">The relative directory path to append to this directory.</param>
        /// <param name="options">The options to use for parsing the appended relative directory path.</param>
        sealed IDirectoryPath CombineDirectory(ReadOnlySpan<char> path, PathOptions options = PathOptions.NoUnfriendlyNames)
        {
            return CombineDirectory(path, PathFormat, options);
        }

        /// <summary>
        /// Combines this directory with a relative directory path parsed using the specified format and options.
        /// </summary>
        /// <param name="path">The relative directory path to append to this directory.</param>
        /// <param name="format">The appended relative directory path's format.</param>
        /// <param name="options">The options to use for parsing the appended relative directory path.</param>
        IDirectoryPath CombineDirectory(ReadOnlySpan<char> path, PathFormat format, PathOptions options = PathOptions.NoUnfriendlyNames);

        // File

        /// <summary>
        /// Combines this directory with a relative file.
        /// </summary>
        /// <param name="path">The relative file to apprend to this directory.</param>
        IFilePath Combine(IRelativeFilePath path);

        /// <summary>
        /// Combines this directory with a relative file path parsed using the specified options and this directory's path format.
        /// </summary>
        /// <param name="path">The relative file path to append to this directory.</param>
        /// <param name="options">The options to use for parsing the appended relative file path.</param>
        sealed IFilePath CombineFile(ReadOnlySpan<char> path, PathOptions options = PathOptions.NoUnfriendlyNames) => CombineFile(path, PathFormat, options);

        /// <summary>
        /// Combines this directory with a relative file path parsed using the specified format and options.
        /// </summary>
        /// <param name="path">The relative file path to append to this directory.</param>
        /// <param name="format">The appended relative file path's format.</param>
        /// <param name="options">The options to use for parsing the appended relative file path.</param>
        IFilePath CombineFile(ReadOnlySpan<char> path, PathFormat format, PathOptions options = PathOptions.NoUnfriendlyNames);

        // Entry

        /// <summary>
        /// Combines this directory with a relative entry.
        /// </summary>
        /// <param name="path">The relative entry to apprend to this directory.</param>
        IPath Combine(IRelativePath path);

        #endregion
    }
}