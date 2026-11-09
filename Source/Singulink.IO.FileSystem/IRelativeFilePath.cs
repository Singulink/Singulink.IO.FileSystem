using System.ComponentModel;
using System.Text.Json.Serialization;
using Singulink.IO.Converters;
using Singulink.IO.Json;

namespace Singulink.IO;

/// <summary>
/// Represents a relative path to a file.
/// </summary>
[TypeConverter(typeof(RelativeFilePathConverter))]
[JsonConverter(typeof(RelativeFilePathJsonConverter))]
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

    /// <inheritdoc cref="IFilePath.AddMultiExtension(string?, PathOptions)"/>
    new IRelativeFilePath AddMultiExtension(string? extension, PathOptions options = PathOptions.NoUnfriendlyNames);

    /// <inheritdoc/>
    IFilePath IFilePath.AddMultiExtension(string? extension, PathOptions options) => AddMultiExtension(extension, options);

    /// <inheritdoc cref="IFilePath.RemoveExtension(string, PathOptions)"/>
    new IRelativeFilePath RemoveExtension(string extension, PathOptions options = PathOptions.NoUnfriendlyNames);

    /// <inheritdoc/>
    IFilePath IFilePath.RemoveExtension(string extension, PathOptions options) => RemoveExtension(extension, options);

    /// <inheritdoc cref="IFilePath.RemoveMultiExtension(string, PathOptions)"/>
    new IRelativeFilePath RemoveMultiExtension(string extension, PathOptions options = PathOptions.NoUnfriendlyNames);

    /// <inheritdoc/>
    IFilePath IFilePath.RemoveMultiExtension(string extension, PathOptions options) => RemoveMultiExtension(extension, options);

    #endregion

    #region Path Format Conversion

    /// <inheritdoc cref="IRelativePath.ToPathFormat(PathFormat, PathOptions)"/>
    new IRelativeFilePath ToPathFormat(PathFormat format, PathOptions options = PathOptions.NoUnfriendlyNames);

    /// <inheritdoc/>
    IRelativePath IRelativePath.ToPathFormat(PathFormat format, PathOptions options) => ToPathFormat(format, options);

    #endregion
}
