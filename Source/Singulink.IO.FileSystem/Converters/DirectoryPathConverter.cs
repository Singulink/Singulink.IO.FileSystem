namespace Singulink.IO.Converters;

/// <summary>
/// Converts <see cref="IDirectoryPath"/> paths to and from strings. Absolute strings are parsed as absolute directory paths and all other strings as
/// relative directory paths.
/// </summary>
internal sealed class DirectoryPathConverter()
    : PathTypeConverter<IDirectoryPath>("directory path", isAbsolute: null, isDirectory: true);
