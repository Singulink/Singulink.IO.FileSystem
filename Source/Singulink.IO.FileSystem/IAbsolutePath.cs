using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Singulink.IO
{
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
        /// <para>This is the value that should always be used when a path string is needed for passing into file system calls (i.e. opening file streams).</para>
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
        bool Exists { get; }

        /// <summary>
        /// Gets or sets the file/directory attributes.
        /// </summary>
        FileAttributes Attributes { get; set; }

        /// <summary>
        /// Gets or sets the file/directory's creation time as a local time.
        /// </summary>
        DateTime CreationTime { get; set; }

        /// <summary>
        /// Gets or sets the file/directory's last access time as a local time.
        /// </summary>
        DateTime LastAccessTime { get; set; }

        /// <summary>
        /// Gets or sets the file/directory's last write time as a local time.
        /// </summary>
        DateTime LastWriteTime { get; set; }

        /// <summary>
        /// Gets the root directory of this file/directory.
        /// </summary>
        IAbsoluteDirectoryPath RootDirectory { get; }

        /// <inheritdoc cref="IPath.ParentDirectory"/>
        new IAbsoluteDirectoryPath? ParentDirectory => throw new NotImplementedException();

        /// <inheritdoc/>
        IDirectoryPath? IPath.ParentDirectory => ParentDirectory;

        /// <summary>
        /// Gets the last directory in the path that exists.
        /// </summary>
        IAbsoluteDirectoryPath GetLastExistingDirectory() => throw new NotImplementedException();
    }
}
