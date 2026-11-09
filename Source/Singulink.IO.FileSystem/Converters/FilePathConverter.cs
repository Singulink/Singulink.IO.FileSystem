namespace Singulink.IO.Converters;

/// <summary>
/// Converts <see cref="IFilePath"/> paths to and from strings. Absolute strings are parsed as absolute file paths and all other strings as
/// relative file paths.
/// </summary>
internal sealed class FilePathConverter()
    : PathTypeConverter<IFilePath>("file path", isAbsolute: null, isDirectory: false);
