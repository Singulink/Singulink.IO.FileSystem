namespace Singulink.IO;

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

    /// <summary>
    /// Gets the absolute path represented by the <see cref="FileSystemInfo"/> using the specified options. The returned instance is either an <see
    /// cref="IAbsoluteFilePath"/> or <see cref="IAbsoluteDirectoryPath"/> depending on the runtime type of <paramref name="info"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method disallows unfriendly names by default, but any silent path modifications performed by <see cref="FileSystemInfo"/> (i.e. trimming of
    /// trailing spaces and dots) will remain intact.</para>
    /// <para>
    /// The <see cref="FileSystemInfo.FullName"/> property is used to get the absolute path that is parsed.</para>
    /// </remarks>
    /// <exception cref="ArgumentException"><paramref name="info"/> is not a <see cref="FileInfo"/> or <see cref="DirectoryInfo"/>.</exception>
    public static IAbsolutePath ToPath(this FileSystemInfo info, PathOptions options = PathOptions.NoUnfriendlyNames)
    {
        return info switch {
            FileInfo fi => fi.ToPath(options),
            DirectoryInfo di => di.ToPath(options),
            _ => throw new ArgumentException($"Unsupported {nameof(FileSystemInfo)} type '{info.GetType()}'.", nameof(info)),
        };
    }

    /// <summary>
    /// Creates a cached info snapshot for the file represented by the <see cref="FileInfo"/> using the specified options.
    /// </summary>
    /// <remarks>The <see cref="FileSystemInfo.FullName"/> property is used to get the absolute path that is parsed.</remarks>
    /// <exception cref="FileNotFoundException">No file exists at the specified path.</exception>
    /// <exception cref="IOException">The path resolves to a directory instead of a file.</exception>
    public static CachedFileInfo ToCachedInfo(this FileInfo fileInfo, PathOptions options = PathOptions.NoUnfriendlyNames)
    {
        return new CachedFileInfo(fileInfo, fileInfo.ToPath(options));
    }

    /// <summary>
    /// Creates a cached info snapshot for the directory represented by the <see cref="DirectoryInfo"/> using the specified options.
    /// </summary>
    /// <remarks>The <see cref="FileSystemInfo.FullName"/> property is used to get the absolute path that is parsed.</remarks>
    /// <exception cref="FileNotFoundException">No directory exists at the specified path.</exception>
    /// <exception cref="IOException">The path resolves to a file instead of a directory.</exception>
    public static CachedDirectoryInfo ToCachedInfo(this DirectoryInfo dirInfo, PathOptions options = PathOptions.NoUnfriendlyNames)
    {
        return new CachedDirectoryInfo(dirInfo, dirInfo.ToPath(options));
    }

    /// <summary>
    /// Creates a cached info snapshot for the file or directory represented by the <see cref="FileSystemInfo"/> using the specified options. The returned
    /// instance is either a <see cref="CachedFileInfo"/> or <see cref="CachedDirectoryInfo"/> depending on the runtime type of <paramref name="info"/>.
    /// </summary>
    /// <remarks>The <see cref="FileSystemInfo.FullName"/> property is used to get the absolute path that is parsed.</remarks>
    /// <exception cref="ArgumentException"><paramref name="info"/> is not a <see cref="FileInfo"/> or <see cref="DirectoryInfo"/>.</exception>
    /// <exception cref="FileNotFoundException">No file or directory exists at the specified path.</exception>
    /// <exception cref="IOException">The entry's type does not match the runtime type of <paramref name="info"/>.</exception>
    public static CachedEntryInfo ToCachedInfo(this FileSystemInfo info, PathOptions options = PathOptions.NoUnfriendlyNames)
    {
        return info switch {
            FileInfo fi => fi.ToCachedInfo(options),
            DirectoryInfo di => di.ToCachedInfo(options),
            _ => throw new ArgumentException($"Unsupported {nameof(FileSystemInfo)} type '{info.GetType()}'.", nameof(info)),
        };
    }
}
