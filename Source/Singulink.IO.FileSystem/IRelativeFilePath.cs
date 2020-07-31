using System;

namespace Singulink.IO
{
    /// <summary>
    /// Represents a relative path to a file.
    /// </summary>
    public partial interface IRelativeFilePath : IRelativePath, IFilePath
    {
        /// <summary>
        /// Gets the file's parent directory.
        /// </summary>
        new IRelativeDirectoryPath ParentDirectory { get; }

        /// <inheritdoc/>
        IRelativeDirectoryPath? IRelativePath.ParentDirectory => ParentDirectory;

        #region Path Manipulation

        /// <inheritdoc cref="IFilePath.WithExtension(string?, PathOptions)"/>
        new IRelativeFilePath WithExtension(string? newExtension, PathOptions options = PathOptions.NoUnfriendlyNames);

        /// <inheritdoc/>
        IFilePath IFilePath.WithExtension(string? newExtension, PathOptions options) => WithExtension(newExtension, options);

        #endregion

        #region Path Format Conversion

        /// <inheritdoc cref="IRelativePath.ToPathFormat(PathFormat, PathOptions)"/>
        new IRelativeFilePath ToPathFormat(PathFormat format, PathOptions options = PathOptions.NoUnfriendlyNames);

        /// <inheritdoc/>
        IRelativePath IRelativePath.ToPathFormat(PathFormat format, PathOptions options) => ToPathFormat(format, options);

        #endregion
    }
}