namespace Singulink.IO.Converters;

/// <summary>
/// Converts <see cref="IRelativeDirectoryPath"/> paths to and from strings.
/// </summary>
internal sealed class RelativeDirectoryPathConverter()
    : PathTypeConverter<IRelativeDirectoryPath>("relative directory path", isAbsolute: false, isDirectory: true);
