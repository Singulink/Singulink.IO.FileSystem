using Singulink.Enums;
using Singulink.IO.Utilities;

namespace Singulink.IO;

/// <summary>
/// Represents cached information about a directory.
/// </summary>
public class CachedDirectoryInfo : CachedEntryInfo
{
    private DirectoryInfo _info;

    internal CachedDirectoryInfo(DirectoryInfo info, IAbsoluteDirectoryPath path)
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
        Path = path;
    }

    /// <summary>
    /// Gets the path to the directory.
    /// </summary>
    public override IAbsoluteDirectoryPath Path { get; }

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
