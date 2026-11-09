namespace Singulink.IO.Converters;

/// <summary>
/// Converts <see cref="IAbsoluteDirectoryPath"/> paths to and from strings.
/// </summary>
internal sealed class AbsoluteDirectoryPathConverter()
    : PathTypeConverter<IAbsoluteDirectoryPath>("absolute directory path", isAbsolute: true, isDirectory: true);
