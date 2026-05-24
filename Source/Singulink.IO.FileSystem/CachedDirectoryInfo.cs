namespace Singulink.IO;

/// <summary>
/// Represents cached information about a directory.
/// </summary>
public class CachedDirectoryInfo : CachedEntryInfo
{
    internal CachedDirectoryInfo(FileSystemInfo info, IAbsoluteDirectoryPath path) : base(ValidateInfo(info, path))
    {
        // NOTE: Unlike CachedFileInfo (which only accepts a FileInfo), we accept any FileSystemInfo here since they all expose the properties we need for
        // directories (and work fine on directories), and we want to be able to create CachedDirectoryInfo instances from both DirectoryInfo and FileInfo
        // so that we don't need to re-query the file system if we want to get a CachedEntryInfo instance for a path that is an unknown file system entry
        // type (we can just get a FileInfo for it and then create either a CachedFileInfo or CachedDirectoryInfo based on the attributes).

        Path = path;
    }

    /// <summary>
    /// Creates a cached info snapshot for an existing directory at the specified absolute path.
    /// </summary>
    /// <param name="path">An absolute path to an existing directory, using the current platform's format.</param>
    /// <param name="options">Specifies the path parsing options for <paramref name="path"/>.</param>
    /// <exception cref="FileNotFoundException">No directory exists at the specified path.</exception>
    /// <exception cref="DirectoryNotFoundException">A parent directory in the specified path does not exist.</exception>
    /// <exception cref="IOException">The path resolves to a file instead of a directory.</exception>
    public static new CachedDirectoryInfo Create(ReadOnlySpan<char> path, PathOptions options = PathOptions.NoUnfriendlyNames)
    {
        var info = CachedEntryInfo.Create(path, options);

        if (info is not CachedDirectoryInfo dir)
            throw Ex.DirIsFile(info.Path);

        return dir;
    }

    /// <summary>
    /// Gets the path to the directory.
    /// </summary>
    public override IAbsoluteDirectoryPath Path { get; }

    /// <inheritdoc/>
    public override void Refresh()
    {
        var newInfo = new DirectoryInfo(Path.PathExport);
        ValidateInfo(newInfo, Path);
        ApplyInfo(newInfo);
    }

    private static FileSystemInfo ValidateInfo(FileSystemInfo info, IAbsoluteDirectoryPath path)
    {
        FileAttributes attributes;

        try
        {
            attributes = info.Attributes;
        }
        catch (UnauthorizedAccessException ex)
        {
            throw Ex.Convert(ex);
        }

        if (attributes is (FileAttributes)(-1))
            throw Ex.NotFound(path);

        if (!attributes.HasAllFlags(FileAttributes.Directory))
            throw Ex.DirIsFile(path);

        return info;
    }
}
