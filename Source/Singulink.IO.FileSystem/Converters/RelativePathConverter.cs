namespace Singulink.IO.Converters;

/// <summary>
/// Converts <see cref="IRelativePath"/> paths to and from strings. Directory-shaped strings are parsed as relative directory
/// paths and all other strings as relative file paths.
/// </summary>
internal sealed class RelativePathConverter()
    : PathTypeConverter<IRelativePath>("relative path", isAbsolute: false, isDirectory: null);
