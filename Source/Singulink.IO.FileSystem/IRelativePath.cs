using System;

namespace Singulink.IO
{
    /// <summary>
    /// Represents a relative path to a file or directory.
    /// </summary>
    public partial interface IRelativePath : IPath
    {
        /// <inheritdoc cref="IPath.ParentDirectory"/>
        new IRelativeDirectoryPath? ParentDirectory => null; // Override higher up.

        /// <inheritdoc/>
        IDirectoryPath? IPath.ParentDirectory => ParentDirectory;

        #region Combining

        #endregion

        #region Path Format Conversion

        /// <summary>
        /// Converts the path to use a different format.
        /// </summary>
        /// <param name="format">The format that the path should be converted to.</param>
        /// <param name="options">The options to use when parsing the new path.</param>
        IRelativePath ToPathFormat(PathFormat format, PathOptions options = PathOptions.NoUnfriendlyNames) => throw new NotImplementedException();

        #endregion
    }
}
