using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Singulink.IO.Converters;
using Singulink.IO.Json;

namespace Singulink.IO;

/// <summary>
/// Represents an absolute or relative path to a file or directory.
/// </summary>
[TypeConverter(typeof(PathConverter))]
[JsonConverter(typeof(PathJsonConverter))]
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

#pragma warning restore CA1707
#pragma warning restore SA1300
#pragma warning restore IDE1006

    /// <summary>
    /// Gets the name of the file or directory that this path refers to.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets a path string suitable for user-friendly display or serialization. Do not use this value to access the file system.
    /// </summary>
    /// <remarks>
    /// <para>The value returned by this property is ideal for display to the user. Parsing this value with the appropriate parse method that matches the
    /// actual type of this path will recreate an identical path object. If you need a string path parameter in order to perform IO operations (e.g.
    /// opening a file stream) you should obtain an absolute path and use the <see cref="IAbsolutePath.PathExport"/> property value instead as it is
    /// specifically formatted to ensure the path is correctly parsed by the underlying file system.</para>
    /// <para>Non-empty directory paths always end with the path separator (e.g. <c>"C:\Foo\"</c>, <c>"/foo/"</c>, <c>"../"</c>) while file paths
    /// never do. The only directory path whose display value does not end with a separator is the empty relative path (<c>""</c>) which represents the
    /// current directory.</para>
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
    /// Determines whether this path is valid for the specified options.
    /// </summary>
    /// <param name="options">The path options to check the path against.</param>
    /// <returns><see langword="true"/> if parsing this path's <see cref="PathDisplay"/> in its <see cref="PathFormat"/>, as the same kind of path (absolute or
    /// relative, file or directory), with <paramref name="options"/> would succeed; otherwise <see langword="false"/>.</returns>
    /// <remarks>
    /// <para>The options apply as they do when parsing: <see cref="PathOptions.NoReservedDeviceNames"/> has no effect on <see cref="PathFormat.Unix"/>
    /// paths, control characters are always invalid in <see cref="PathFormat.Windows"/> paths, <see cref="PathOptions.PathFormatDependent"/> adds the <see
    /// cref="PathOptions.NoUnfriendlyNames"/> flags for <see cref="PathFormat.Windows"/> and <see cref="PathFormat.Universal"/> paths, and <see
    /// cref="PathOptions.AllowEmptyDirectories"/> makes no difference because paths never contain empty directories.</para>
    /// <para>Paths remember what they are known to be valid for. A path that was parsed is known to be valid for the options it was parsed with, and paths
    /// derived from it (e.g. its parent directory or a combined path) keep what is known about their names. Anything else is worked out the first time it is
    /// needed, in one pass over the names, and remembered for every later check.</para>
    /// </remarks>
    bool IsValidFor(PathOptions options);

    /// <summary>
    /// Determines whether this path is valid for the specified options and outputs the reason when it isn't.
    /// </summary>
    /// <param name="options">The path options to check the path against.</param>
    /// <param name="errorMessage">When this method returns <see langword="false"/>, contains the error message that parsing this path with <paramref
    /// name="options"/> gives; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if parsing this path's <see cref="PathDisplay"/> in its <see cref="PathFormat"/>, as the same kind of path (absolute or
    /// relative, file or directory), with <paramref name="options"/> would succeed; otherwise <see langword="false"/>.</returns>
    /// <remarks>
    /// <para>See <see cref="IsValidFor(PathOptions)"/> for how the options apply. The error message is only worked out when the path isn't valid.</para>
    /// </remarks>
    bool IsValidFor(PathOptions options, [NotNullWhen(false)] out string? errorMessage);

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
    /// <remarks>
    /// To obtain a path suitable for user-friendly display or serialization, use the <see cref="PathDisplay"/> property. To obtain a path suitable for
    /// file system operations, use the <see cref="IAbsolutePath.PathExport"/> property on an absolute path.
    /// </remarks>
    string ToString();
}
