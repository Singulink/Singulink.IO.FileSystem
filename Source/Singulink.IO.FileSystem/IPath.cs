using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Singulink.IO;

/// <summary>
/// Represents an absolute or relative path to a file or directory.
/// </summary>
[SuppressMessage("Design", "CA1065:Do not raise exceptions in unexpected locations", Justification = "Properties need to be overridden by implementing types")]
public partial interface IPath : IEquatable<IPath?>
{
#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable CA1707 // Identifiers should not contain underscores

    /// <summary>
    /// Determines whether two paths are equal.
    /// </summary>
    /// <remarks>
    /// <para>The paths being compared must be the same type, have matching path formats and the same character casing (aside from the drive letter or UNC name,
    /// if applicable) in order to be considered equal.</para>
    /// </remarks>
    [SpecialName]
    public static bool op_Equality(IPath? left, IPath? right) => Equals(left, right);

    /// <summary>
    /// Determines whether two paths are not equal.
    /// </summary>
    /// <remarks>
    /// <para>The paths being compared must be the same type, have matching path formats and the same character casing (aside from the drive letter or UNC name,
    /// if applicable) in order to be considered equal.</para>
    /// </remarks>
    [SpecialName]
    public static bool op_Inequality(IPath? left, IPath? right) => !Equals(left, right);

#pragma warning restore SA1300
#pragma warning restore IDE1006
#pragma warning restore CA1707

    /// <summary>
    /// Gets the name of the file or directory that this path refers to.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets a path string suitable for user friendly display or serialization. Do not use this value to access the file system.
    /// </summary>
    /// <remarks>
    /// <para>The value returned by this property is ideal for display to the user. Parsing this value with the appropriate parse method that matches the
    /// actual type of this path will recreate an identical path object. If you need a string path parameter in order to perform IO operations (e.g.
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
    /// Gets a value indicating whether this path has a parent directory.
    /// </summary>
    bool HasParentDirectory { get; }

    /// <summary>
    /// Gets the parent directory of this file/directory.
    /// </summary>
    IDirectoryPath? ParentDirectory { get; }

    /// <summary>
    /// Gets a value indicating whether this path is rooted. Relative paths can be rooted and absolute paths are always rooted.
    /// </summary>
    /// <remarks>
    /// <para>On Windows, a rooted relative path starts with the path separator (e.g. <c>"\Some\Path"</c>). Rooted relative paths are not supported on
    /// Unix (paths that start with the path separator are absolute paths).</para>
    /// </remarks>
    bool IsRooted { get; }

    /// <summary>
    /// Determines whether this file/directory is equal to another file/directory.
    /// </summary>
    /// <remarks>
    /// <para>The paths being compared must be the same type, have matching path formats and the same character casing (aside from the drive letter or UNC name,
    /// if applicable) in order to be considered equal.</para>
    /// </remarks>
    new bool Equals(IPath? other);

    /// <summary>
    /// Returns a string containing the path format, entry type and the path. Not usable for file system operations.
    /// </summary>
    string ToString();
}
