using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Singulink.IO.Converters;

/// <summary>
/// Base class for type converters that convert path types to and from strings. Strings are read and written as described in <see cref="PathStrings"/>.
/// </summary>
/// <param name="pathTypeDescription">The description of the path type used in error messages, e.g. "absolute directory path".</param>
/// <param name="isAbsolute"><see langword="true"/> for absolute paths, <see langword="false"/> for relative paths, or <see langword="null"/> to decide by the
/// string.</param>
/// <param name="isDirectory"><see langword="true"/> for directory paths, <see langword="false"/> for file paths, or <see langword="null"/> to decide by the
/// shape of the string.</param>
internal abstract class PathTypeConverter<TPath>(string pathTypeDescription, bool? isAbsolute, bool? isDirectory) : TypeConverter where TPath : class, IPath
{
    /// <summary>
    /// Gets the description of the path type used in error messages, e.g. "absolute directory path".
    /// </summary>
    internal string PathTypeDescription { get; } = pathTypeDescription;

    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    public override bool CanConvertTo(ITypeDescriptorContext? context, [NotNullWhen(true)] Type? destinationType)
    {
        return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is not string s)
            return base.ConvertFrom(context, culture, value);

        if (!TryParse(s, out var path, out string? error))
            throw new FormatException(GetErrorMessage(s, error));

        return path;
    }

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if (destinationType == typeof(string) && value is TPath path)
            return PathStrings.GetString(path);

        return base.ConvertTo(context, culture, value, destinationType);
    }

    internal string GetErrorMessage(string s, string error) => $"Invalid {PathTypeDescription} '{s}': {error}";

    internal bool TryParse(string s, [NotNullWhen(true)] out TPath? path, [NotNullWhen(false)] out string? error)
    {
        return TryParse(s, PathFormat.Current, out path, out error);
    }

    /// <summary>
    /// Parses a string, using the specified format as the current platform's format.
    /// </summary>
    internal bool TryParse(string s, PathFormat currentFormat, [NotNullWhen(true)] out TPath? path, [NotNullWhen(false)] out string? error)
    {
        bool success = PathStrings.TryParse(s, currentFormat, isAbsolute, isDirectory, out var parsedPath, out error);
        path = (TPath?)parsedPath;

        return success;
    }
}
