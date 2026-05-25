namespace Singulink.IO;

/// <summary>
/// Represents cached information about a file or directory.
/// </summary>
/// <remarks>
/// The values exposed by this type are a snapshot of the file/directory's state at the time the instance was created or last refreshed. Call <see
/// cref="Refresh"/> to update the snapshot from the underlying file system.
/// </remarks>
public abstract class CachedEntryInfo
{
    private protected CachedEntryInfo(FileSystemInfo entryInfo)
    {
        ApplyInfo(entryInfo);
    }

    /// <summary>
    /// Creates a cached info snapshot for an existing file or directory at the specified absolute path. The returned instance is either a <see
    /// cref="CachedFileInfo"/> or <see cref="CachedDirectoryInfo"/> depending on the entry's type.
    /// </summary>
    /// <remarks>
    /// If <paramref name="path"/> is directory-shaped (i.e. it ends with a separator or a navigational segment) then it must point to a directory; an
    /// <see cref="IOException"/> is thrown if it instead resolves to a file. If <paramref name="path"/> is file-shaped then either a file or a directory at
    /// that path is accepted.
    /// </remarks>
    /// <param name="path">An absolute path to an existing file or directory, using the current platform's format.</param>
    /// <param name="options">Specifies the path parsing options for <paramref name="path"/>.</param>
    /// <exception cref="FileNotFoundException">No file or directory exists at the specified path.</exception>
    /// <exception cref="DirectoryNotFoundException">A parent directory in the specified path does not exist.</exception>
    /// <exception cref="IOException">The path is directory-shaped but resolves to a file.</exception>
    public static CachedEntryInfo Create(ReadOnlySpan<char> path, PathOptions options = PathOptions.NoUnfriendlyNames)
    {
        var format = PathFormat.Current;
        var normalized = format.NormalizeSeparators(path);

        if (format.IsDirectoryShaped(normalized))
            return CreateFromKnownDirectory(DirectoryPath.ParseAbsolute(path, format, options));

        return CreateFromAmbiguousFile(FilePath.ParseAbsolute(path, format, options));
    }

    internal static CachedEntryInfo CreateFromKnownDirectory(IAbsoluteDirectoryPath dirPath)
    {
        var fileInfo = new FileInfo(dirPath.PathExport);
        var attributes = GetAttributesOrThrowNotFound(fileInfo, dirPath);

        if (!attributes.HasAllFlags(FileAttributes.Directory))
            throw Ex.DirIsFile(dirPath);

        return new CachedDirectoryInfo(fileInfo, dirPath);
    }

    internal static CachedEntryInfo CreateFromAmbiguousFile(IAbsoluteFilePath filePath)
    {
        var fileInfo = new FileInfo(filePath.PathExport);
        var attributes = GetAttributesOrThrowNotFound(fileInfo, filePath);

        if (!attributes.HasAllFlags(FileAttributes.Directory))
            return new CachedFileInfo(fileInfo, filePath);

        // The path resolves to a directory on the file system: rebuild as an IAbsoluteDirectoryPath by appending the separator.
        var impl = (IPath.Impl)filePath;
        var dirPath = new IAbsoluteDirectoryPath.Impl(impl.PathDisplay + impl.PathFormat.SeparatorAsString, impl.RootLength, impl.PathFormat);
        return new CachedDirectoryInfo(fileInfo, dirPath);
    }

    private static FileAttributes GetAttributesOrThrowNotFound(FileInfo fileInfo, IAbsolutePath path)
    {
        FileAttributes attributes;

        try
        {
            attributes = fileInfo.Attributes;
        }
        catch (UnauthorizedAccessException ex)
        {
            throw Ex.Convert(ex);
        }

        if (attributes is not (FileAttributes)(-1))
            return attributes;

        if (path.State is EntryState.ParentDoesNotExist)
            throw new DirectoryNotFoundException($"A parent directory in the path '{path.PathDisplay}' does not exist.");

        throw new FileNotFoundException($"No file or directory was found at path '{path.PathDisplay}'.", path.PathExport);
    }

    /// <summary>
    /// Gets the path to the file/directory.
    /// </summary>
    public abstract IAbsolutePath Path { get; }

    /// <summary>
    /// Gets the file/directory attributes captured in the snapshot.
    /// </summary>
    public FileAttributes Attributes { get; private set; }

    /// <summary>
    /// Gets the file/directory's creation time as a local time.
    /// </summary>
    public DateTime CreationTime => CreationTimeUtc.ToLocalTime();

    /// <summary>
    /// Gets the file/directory's creation time in Coordinated Universal Time (UTC).
    /// </summary>
    public DateTime CreationTimeUtc { get; private set; }

    /// <summary>
    /// Gets the file/directory's last access time as a local time.
    /// </summary>
    public DateTime LastAccessTime => LastAccessTimeUtc.ToLocalTime();

    /// <summary>
    /// Gets the file/directory's last access time in Coordinated Universal Time (UTC).
    /// </summary>
    public DateTime LastAccessTimeUtc { get; private set; }

    /// <summary>
    /// Gets the file/directory's last write time as a local time.
    /// </summary>
    public DateTime LastWriteTime => LastWriteTimeUtc.ToLocalTime();

    /// <summary>
    /// Gets the file/directory's last write time in Coordinated Universal Time (UTC).
    /// </summary>
    public DateTime LastWriteTimeUtc { get; private set; }

    /// <summary>
    /// Refreshes the cached information about the file/directory. If the refresh fails for any reason, the existing snapshot is left unchanged.
    /// </summary>
    public abstract void Refresh();

    /// <summary>
    /// Copies the base-class snapshot values from the supplied <see cref="FileSystemInfo"/>. Derived types must validate attributes prior to calling this
    /// from their <see cref="Refresh"/> implementations so that a failure leaves the existing snapshot unchanged.
    /// </summary>
    private protected void ApplyInfo(FileSystemInfo entryInfo)
    {
        try
        {
            (Attributes, CreationTimeUtc, LastAccessTimeUtc, LastWriteTimeUtc) =
                (entryInfo.Attributes, entryInfo.CreationTimeUtc, entryInfo.LastAccessTimeUtc, entryInfo.LastWriteTimeUtc);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw Ex.Convert(ex);
        }
    }

    /// <summary>
    /// Returns a string containing the path format, entry type and the path. Not usable for file system operations.
    /// </summary>
    /// <remarks>
    /// This method returns the same value as <see cref="IPath.ToString"/> method on the <see cref="Path"/> property. See that method's documentation for more
    /// details on obtaining paths suitable for display or file system operations.
    /// </remarks>
    public override string ToString() => Path.ToString();
}
