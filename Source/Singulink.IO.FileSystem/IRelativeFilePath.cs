namespace Singulink.IO;

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
    new IRelativeFilePath WithExtension(string? extension, PathOptions options = PathOptions.NoUnfriendlyNames);

    /// <inheritdoc/>
    IFilePath IFilePath.WithExtension(string? extension, PathOptions options) => WithExtension(extension, options);

    /// <inheritdoc cref="IFilePath.AddExtension(string?, PathOptions)"/>
    new IRelativeFilePath AddExtension(string? extension, PathOptions options = PathOptions.NoUnfriendlyNames);

    /// <inheritdoc/>
    IFilePath IFilePath.AddExtension(string? extension, PathOptions options) => AddExtension(extension, options);

    #endregion

    #region Path Format Conversion

    /// <inheritdoc cref="IRelativePath.ToPathFormat(PathFormat, PathOptions)"/>
    new IRelativeFilePath ToPathFormat(PathFormat format, PathOptions options = PathOptions.NoUnfriendlyNames);

    /// <inheritdoc/>
    IRelativePath IRelativePath.ToPathFormat(PathFormat format, PathOptions options) => ToPathFormat(format, options);

    #endregion
}
