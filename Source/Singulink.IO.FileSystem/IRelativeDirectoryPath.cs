using System;

namespace Singulink.IO;

/// <summary>
/// Represents a relative path to a directory.
/// </summary>
public partial interface IRelativeDirectoryPath : IRelativePath, IDirectoryPath
{
    /// <summary>
    /// Combines a relative directory with another relative directory.
    /// </summary>
    public static IRelativeDirectoryPath operator +(IRelativeDirectoryPath x, IRelativeDirectoryPath y) => x.Combine(y);

    /// <summary>
    /// Combines a relative directory with another relative file.
    /// </summary>
    public static IRelativeFilePath operator +(IRelativeDirectoryPath x, IRelativeFilePath y) => x.Combine(y);

    /// <summary>
    /// Combines a relative directory with another relative entry.
    /// </summary>
    public static IRelativePath operator +(IRelativeDirectoryPath x, IRelativePath y) => x.Combine(y);

    #region Combining

    // Directory

    /// <inheritdoc cref="IDirectoryPath.Combine(IRelativeDirectoryPath)"/>
    new IRelativeDirectoryPath Combine(IRelativeDirectoryPath path);

    /// <inheritdoc cref="IDirectoryPath.CombineDirectory(ReadOnlySpan{char}, PathOptions)"/>
    sealed new IRelativeDirectoryPath CombineDirectory(ReadOnlySpan<char> path, PathOptions options = PathOptions.NoUnfriendlyNames)
    {
        return CombineDirectory(path, PathFormat, options);
    }

    /// <inheritdoc cref="IDirectoryPath.CombineDirectory(ReadOnlySpan{char}, PathFormat, PathOptions)"/>
    new IRelativeDirectoryPath CombineDirectory(ReadOnlySpan<char> path, PathFormat format, PathOptions options = PathOptions.NoUnfriendlyNames);

    // File

    /// <inheritdoc cref="IDirectoryPath.Combine(IRelativeFilePath)"/>
    new IRelativeFilePath Combine(IRelativeFilePath path);

    /// <inheritdoc cref="IDirectoryPath.CombineFile(ReadOnlySpan{char}, PathOptions)"/>
    sealed new IRelativeFilePath CombineFile(ReadOnlySpan<char> path, PathOptions options = PathOptions.NoUnfriendlyNames)
    {
        return CombineFile(path, PathFormat, options);
    }

    /// <inheritdoc cref="IDirectoryPath.CombineFile(ReadOnlySpan{char}, PathFormat, PathOptions)"/>
    new IRelativeFilePath CombineFile(ReadOnlySpan<char> path, PathFormat format, PathOptions options = PathOptions.NoUnfriendlyNames);

    // Entry

    /// <inheritdoc cref="IDirectoryPath.Combine(IRelativePath)"/>
    new IRelativePath Combine(IRelativePath path);

    // Explicit base implementations

    /// <inheritdoc/>
    IDirectoryPath IDirectoryPath.Combine(IRelativeDirectoryPath path) => Combine(path);

    /// <inheritdoc/>
    IDirectoryPath IDirectoryPath.CombineDirectory(ReadOnlySpan<char> path, PathFormat format, PathOptions options) => CombineDirectory(path, format, options);

    /// <inheritdoc/>
    IFilePath IDirectoryPath.Combine(IRelativeFilePath path) => Combine(path);

    /// <inheritdoc/>
    IFilePath IDirectoryPath.CombineFile(ReadOnlySpan<char> path, PathFormat format, PathOptions options) => CombineFile(path, format, options);

    /// <inheritdoc/>
    IPath IDirectoryPath.Combine(IRelativePath path) => Combine(path);

    #endregion

    #region Path Format Conversion

    /// <inheritdoc cref="IRelativePath.ToPathFormat(PathFormat, PathOptions)"/>
    new IRelativeDirectoryPath ToPathFormat(PathFormat format, PathOptions options = PathOptions.NoUnfriendlyNames);

    /// <inheritdoc/>
    IRelativePath IRelativePath.ToPathFormat(PathFormat format, PathOptions options) => ToPathFormat(format, options);

    #endregion
}