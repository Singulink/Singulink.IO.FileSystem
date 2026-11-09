namespace Singulink.IO.Converters;

/// <summary>
/// Converts <see cref="IAbsoluteFilePath"/> paths to and from strings.
/// </summary>
internal sealed class AbsoluteFilePathConverter()
    : PathTypeConverter<IAbsoluteFilePath>("absolute file path", isAbsolute: true, isDirectory: false);
