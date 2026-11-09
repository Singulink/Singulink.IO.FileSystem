using System.Text.Json;
using System.Text.Json.Serialization;
using Singulink.IO.Converters;

namespace Singulink.IO.Json;

/// <summary>
/// Converts <see cref="IAbsoluteFilePath"/> paths to and from JSON strings, both as values and as dictionary keys. Paths are written as their <see
/// cref="IPath.PathDisplay"/> string. Strings are read in the current platform's path format, as the <c>Parse</c> methods on <see cref="FilePath"/> and <see
/// cref="DirectoryPath"/> read them on the platform the app runs on with the <see cref="PathOptions.None"/> option, so a path in that format reads back as
/// the same path. A string in the other platform's absolute form, such as <c>C:\data\</c> on Unix or <c>/data/</c> on Windows, is not read as an absolute
/// path.
/// </summary>
public sealed class AbsoluteFilePathJsonConverter : JsonConverter<IAbsoluteFilePath>
{
    private static readonly AbsoluteFilePathConverter TypeConverter = new();

    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert) => PathJsonConversion.CanConvert<IAbsoluteFilePath>(typeToConvert);

    /// <inheritdoc/>
    /// <exception cref="JsonException">The JSON value is not a string or it is not a valid path.</exception>
    public override IAbsoluteFilePath Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return PathJsonConversion.Read(ref reader, TypeConverter);
    }

    /// <inheritdoc/>
    /// <exception cref="JsonException">The property name is not a valid path.</exception>
    public override IAbsoluteFilePath ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return PathJsonConversion.ReadAsPropertyName(ref reader, TypeConverter);
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, IAbsoluteFilePath value, JsonSerializerOptions options)
    {
        PathJsonConversion.Write(writer, value);
    }

    /// <inheritdoc/>
    public override void WriteAsPropertyName(Utf8JsonWriter writer, IAbsoluteFilePath value, JsonSerializerOptions options)
    {
        PathJsonConversion.WriteAsPropertyName(writer, value);
    }
}
