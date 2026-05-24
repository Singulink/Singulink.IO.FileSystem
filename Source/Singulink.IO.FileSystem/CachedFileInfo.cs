namespace Singulink.IO;

/// <summary>
/// Represents cached information about a file.
/// </summary>
public class CachedFileInfo : CachedEntryInfo
{
    internal CachedFileInfo(FileInfo info, IAbsoluteFilePath path) : base(ValidateInfo(info, path))
    {
        Path = path;
        Length = info.Length;
    }

    /// <summary>
    /// Creates a cached info snapshot for an existing file at the specified absolute path.
    /// </summary>
    /// <param name="path">An absolute path to an existing file, using the current platform's format.</param>
    /// <param name="options">Specifies the path parsing options for <paramref name="path"/>.</param>
    /// <exception cref="FileNotFoundException">No file exists at the specified path.</exception>
    /// <exception cref="DirectoryNotFoundException">A parent directory in the specified path does not exist.</exception>
    /// <exception cref="IOException">The path resolves to a directory instead of a file.</exception>
    public static new CachedFileInfo Create(ReadOnlySpan<char> path, PathOptions options = PathOptions.NoUnfriendlyNames)
    {
        var info = CachedEntryInfo.Create(path, options);

        if (info is not CachedFileInfo file)
            throw Ex.FileIsDir(info.Path);

        return file;
    }

    /// <summary>
    /// Gets the path to the file.
    /// </summary>
    public override IAbsoluteFilePath Path { get; }

    /// <summary>
    /// Gets a value indicating whether the file is read-only.
    /// </summary>
    public bool IsReadOnly => Attributes.HasAllFlags(FileAttributes.ReadOnly);

    /// <summary>
    /// Gets the size of the file in bytes.
    /// </summary>
    public long Length { get; private set; }

    /// <inheritdoc/>
    public override void Refresh()
    {
        var newInfo = ValidateInfo(new FileInfo(Path.PathExport), Path);
        long length = newInfo.Length;

        ApplyInfo(newInfo);
        Length = length;
    }

    private static FileInfo ValidateInfo(FileInfo info, IAbsoluteFilePath path)
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

        if (attributes.HasAllFlags(FileAttributes.Directory))
            throw Ex.FileIsDir(path);

        return info;
    }
}
