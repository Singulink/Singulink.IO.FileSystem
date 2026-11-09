namespace Singulink.IO.Converters;

/// <summary>
/// Converts <see cref="IPath"/> paths to and from strings. Directory-shaped strings are parsed as directory paths and all
/// other strings as file paths; absolute strings are parsed as absolute paths and all other strings as relative paths.
/// </summary>
internal sealed class PathConverter()
    : PathTypeConverter<IPath>("path", isAbsolute: null, isDirectory: null);
