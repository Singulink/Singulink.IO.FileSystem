using System;
using System.Diagnostics.CodeAnalysis;

namespace Singulink.IO
{
    /// <summary>
    /// Represents an absolute or relative path to a file or directory.
    /// </summary>
    [SuppressMessage("Design", "CA1065:Do not raise exceptions in unexpected locations", Justification = "Properties need to be overriden by implementing types")]
    public partial interface IPath : IEquatable<IPath?>
    {
        /// <summary>
        /// Gets the name of the file or directory that this path refers to.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets a path string suitable for user friendly display or serialization. Do not use this value to access the file system.
        /// </summary>
        /// <remarks>
        /// <para>The value returned by this property is ideal for display to the user. Parsing this value with the appropriate parse method that matches the
        /// actual type of this path will recreate an identical path object. If you need a string path parameter in order to perform IO operations (i.e.
        /// opening a file stream) you should obtain an absolute path and use the <see cref="IAbsolutePath.PathExport"/> property value instead as it is
        /// specifically formatted to ensure the path is correctly parsed by the underlying file system.</para>
        /// </remarks>
        string PathDisplay { get; }

        /// <summary>
        /// Gets the length of the path that comprises the root.
        /// </summary>
        internal int RootLength { get; }

        /// <summary>
        /// Gets the format of this path.
        /// </summary>
        PathFormat PathFormat { get; }

        /// <summary>
        /// Gets the parent directory of this file/directory.
        /// </summary>
        IDirectoryPath? ParentDirectory => throw new NotImplementedException();

        /// <summary>
        /// Gets a value indicating whether this path has a parent directory.
        /// </summary>
        bool HasParentDirectory => throw new NotImplementedException();

        /// <summary>
        /// Gets a value indicating whether this path is rooted. Relative paths can be rooted and absolute paths are always rooted.
        /// </summary>
        /// <remarks>
        /// <para>A rooted relative path starts with the path separator.</para>
        /// </remarks>
        bool IsRooted { get; }

        /// <summary>
        /// Gets a value indicating whether this is an absolute path.
        /// </summary>
        sealed bool IsAbsolute => this is IAbsolutePath;

        /// <summary>
        /// Gets a value indicating whether this is a relative path.
        /// </summary>
        sealed bool IsRelative => this is IRelativePath;

        /// <summary>
        /// Gets a value indicating whether this is a directory path.
        /// </summary>
        sealed bool IsDirectory => this is IDirectoryPath;

        /// <summary>
        /// Gets a value indicating whether this is a file path.
        /// </summary>
        sealed bool IsFile => this is IFilePath;

        /// <summary>
        /// Determines whether this file/directory is equal to another file/directory.
        /// </summary>
        /// <remarks>
        /// <para>The items being compared must be the same type and have matching path formats and character casing (aside from the drive letter or UNC name,
        /// if applicable) in order to be considered equal.</para>
        /// </remarks>
        new bool Equals(IPath? other);
    }
}