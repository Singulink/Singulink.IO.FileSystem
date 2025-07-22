using Singulink.Enums;
using Singulink.IO.Utilities;

namespace Singulink.IO;

/// <summary>
/// Represents cached information about a file.
/// </summary>
public class CachedFileInfo : CachedEntryInfo
{
    private FileInfo _info;

    internal CachedFileInfo(FileInfo info, IAbsoluteFilePath path)
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
        Path = path;
    }

    /// <summary>
    /// Gets the path to the file.
    /// </summary>
    public override IAbsoluteFilePath Path { get; }

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
