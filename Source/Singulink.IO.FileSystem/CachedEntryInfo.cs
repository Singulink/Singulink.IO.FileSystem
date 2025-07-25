namespace Singulink.IO;

/// <summary>
/// Represents cached information about a file or directory.
/// </summary>
public abstract class CachedEntryInfo
{
    /// <summary>
    /// Gets the path to the file/directory.
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
    /// Gets the file/directory's creation time in Coordinated Universal Time (UTC).
    /// </summary>
    public DateTime CreationTimeUtc => EntryInfo.CreationTimeUtc;

    /// <summary>
    /// Gets the file/directory's last access time as a local time.
    /// </summary>
    public DateTime LastAccessTime => EntryInfo.LastAccessTime;

    /// <summary>
    /// Gets the file/directory's last access time in Coordinated Universal Time (UTC).
    /// </summary>
    public DateTime LastAccessTimeUtc => EntryInfo.LastAccessTimeUtc;

    /// <summary>
    /// Gets the file/directory's last write time as a local time.
    /// </summary>
    public DateTime LastWriteTime => EntryInfo.LastWriteTime;

    /// <summary>
    /// Gets the file/directory's last write time in Coordinated Universal Time (UTC).
    /// </summary>
    public DateTime LastWriteTimeUtc => EntryInfo.LastWriteTimeUtc;

    internal abstract FileSystemInfo EntryInfo { get; }

    /// <summary>
    /// Refreshes the cached information about the file/directory.
    /// </summary>
    public abstract void Refresh();
}
