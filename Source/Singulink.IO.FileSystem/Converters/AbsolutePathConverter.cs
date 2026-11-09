namespace Singulink.IO.Converters;

/// <summary>
/// Converts <see cref="IAbsolutePath"/> paths to and from strings. Directory-shaped strings are parsed as absolute directory
/// paths and all other strings as absolute file paths.
/// </summary>
internal sealed class AbsolutePathConverter()
    : PathTypeConverter<IAbsolutePath>("absolute path", isAbsolute: true, isDirectory: null);
