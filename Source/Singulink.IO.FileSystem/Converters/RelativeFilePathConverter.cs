namespace Singulink.IO.Converters;

/// <summary>
/// Converts <see cref="IRelativeFilePath"/> paths to and from strings.
/// </summary>
internal sealed class RelativeFilePathConverter()
    : PathTypeConverter<IRelativeFilePath>("relative file path", isAbsolute: false, isDirectory: false);
