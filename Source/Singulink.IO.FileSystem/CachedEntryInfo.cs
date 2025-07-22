using Singulink.Enums;
using Singulink.IO.Utilities;

namespace Singulink.IO;

/// <summary>
/// Represents cached information about a file system entry.
/// </summary>
public abstract class CachedEntryInfo
{
    /// <summary>
    /// Gets the path to the file system entry.
    /// </summary>
    public abstract IAbsolutePath Path { get; }

    /// <summary>
    /// Gets the file/directory attributes.
    /// </summary>
    public FileAttributes Attributes => EntryInfo.Attributes;

    /// <summary>
    /// Gets the file/directory's creation time as a local time.
    /// </summary>
    public DateTime CreationTime => EntryInfo.CreationTime;

    /// <summary>
    /// Gets the file/directory's last access time as a local time.
    /// </summary>
    public DateTime LastAccessTime => EntryInfo.LastAccessTime;

    /// <summary>
    /// Gets the file/directory's last write time as a local time.
    /// </summary>
    public DateTime LastWriteTime => EntryInfo.LastWriteTime;

    internal abstract FileSystemInfo EntryInfo { get; }

    /// <summary>
    /// Refreshes the cached information about the file system entry.
    /// </summary>
    public abstract void Refresh();
}

/// <summary>
/// Represents cached information about a file.
/// </summary>
public class CachedFileInfo : CachedEntryInfo
{
    private FileInfo _info;
    private IAbsoluteFilePath? _path;

    internal CachedFileInfo(FileInfo info, IAbsoluteFilePath? path)
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

        if (attributes.HasAllFlags(FileAttributes.Directory))
            throw new IOException("Path points to a directory, not a file.");

        _info = info;
        _path = path;
    }

    /// <inheritdoc/>
    public override IAbsoluteFilePath Path => _path ??= FilePath.ParseAbsolute(EntryInfo.FullName, PathOptions.None);

    /// <summary>
    /// Gets a value indicating whether the file is read-only.
    /// </summary>
    public bool IsReadOnly => _info.IsReadOnly;

    /// <summary>
    /// Gets the size of the file in bytes.
    /// </summary>
    public long Length => _info.Length;

    internal override FileInfo EntryInfo => _info;

    /// <inheritdoc/>
    public override void Refresh()
    {
        FileInfo newInfo;

        try
        {
            newInfo = new FileInfo(_info.FullName);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw Ex.Convert(ex);
        }

        if (newInfo.Attributes.HasAllFlags(FileAttributes.Directory))
            throw new IOException("Path points to a directory, not a file.");

        _info = newInfo;
    }
}

/// <summary>
/// Represents cached information about a directory.
/// </summary>
public class CachedDirectoryInfo : CachedEntryInfo
{
    private DirectoryInfo _info;
    private IAbsoluteDirectoryPath? _path;

    internal CachedDirectoryInfo(DirectoryInfo info, IAbsoluteDirectoryPath? path)
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

        if (!attributes.HasAllFlags(FileAttributes.Directory))
            throw new IOException("Path points to a file, not a directory.");

        _info = info;
        _path = path;
    }

    /// <inheritdoc/>
    public override IAbsoluteDirectoryPath Path => _path ??= DirectoryPath.ParseAbsolute(EntryInfo.FullName, PathOptions.None);

    internal override DirectoryInfo EntryInfo => _info;

    /// <inheritdoc/>
    public override void Refresh()
    {
        DirectoryInfo newInfo;

        try
        {
            newInfo = new DirectoryInfo(_info.FullName);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw Ex.Convert(ex);
        }

        if (!newInfo.Attributes.HasAllFlags(FileAttributes.Directory))
            throw new IOException("Path points to a file, not a directory.");

        _info = newInfo;
    }
}
