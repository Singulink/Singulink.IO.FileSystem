using System.Diagnostics.CodeAnalysis;

namespace Singulink.IO;

/// <summary>
/// Represents an absolute path to a file or directory.
/// </summary>
[SuppressMessage("Design", "CA1065:Do not raise exceptions in unexpected locations", Justification = "Properties need to be overriden by implementing types")]
public partial interface IAbsolutePath : IPath
{
    /// <summary>
    /// Gets a path string that is specially formatted for reliably accessing this path through file system calls.
    /// </summary>
    /// <remarks>
    /// <para>This is the value that should always be used when a path string is needed for passing into file system calls (e.g. opening file streams).</para>
    /// </remarks>
    string PathExport { get; }

    /// <summary>
    /// Gets a value indicating whether this path is a UNC path. This can only ever return true for paths that use the <see cref="PathFormat.Windows"/>
    /// path format.
    /// </summary>
    bool IsUnc { get; }

    /// <summary>
    /// Gets a value indicating whether the file/directory exists.
    /// </summary>
    /// <remarks>
    /// Usage of the <see cref="State"/> property is recommended instead of this property in most cases, as it provides more detailed information about the
    /// entry state, allowing better handling of the various scenarios that can cause a file not to exist.
    /// </remarks>
    bool Exists { get; }

    /// <summary>
    /// Gets the state of the file/directory entry, which indicates if the entry or its parent directory exists.
    /// </summary>
    EntryState State { get; }

    /// <summary>
    /// Gets or sets the file/directory attributes.
    /// </summary>
    FileAttributes Attributes { get; set; }

    /// <summary>
    /// Gets or sets the file/directory's creation time as a local time.
    /// </summary>
    DateTime CreationTime { get; set; }

    /// <summary>
    /// Gets or sets the file/directory's creation time in Coordinated Universal Time (UTC).
    /// </summary>
    DateTime CreationTimeUtc { get; set; }

    /// <summary>
    /// Gets or sets the file/directory's last access time as a local time.
    /// </summary>
    DateTime LastAccessTime { get; set; }

    /// <summary>
    /// Gets or sets the file/directory's last access time in Coordinated Universal Time (UTC).
    /// </summary>
    DateTime LastAccessTimeUtc { get; set; }

    /// <summary>
    /// Gets or sets the file/directory's last write time as a local time.
    /// </summary>
    DateTime LastWriteTime { get; set; }

    /// <summary>
    /// Gets or sets the file/directory's last write time in Coordinated Universal Time (UTC).
    /// </summary>
    DateTime LastWriteTimeUtc { get; set; }

    /// <summary>
    /// Gets the root directory of this file/directory.
    /// </summary>
    IAbsoluteDirectoryPath RootDirectory { get; }

    /// <inheritdoc cref="IPath.ParentDirectory"/>
    new IAbsoluteDirectoryPath? ParentDirectory { get; }

    /// <inheritdoc/>
    IDirectoryPath? IPath.ParentDirectory => ParentDirectory;

    /// <summary>
    /// Gets information about this file/directory.
    /// </summary>
    CachedEntryInfo GetInfo();

    /// <summary>
    /// Gets the last directory in the path that exists.
    /// </summary>
    IAbsoluteDirectoryPath GetLastExistingDirectory();
}
