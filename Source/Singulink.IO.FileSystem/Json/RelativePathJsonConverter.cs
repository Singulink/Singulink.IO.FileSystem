using System.Text.Json;
using System.Text.Json.Serialization;
using Singulink.IO.Converters;

namespace Singulink.IO.Json;

/// <summary>
/// Converts <see cref="IRelativePath"/> paths to and from JSON strings, both as values and as dictionary keys. Paths are written in the <see
/// cref="PathFormat.Universal"/> format (with <c>/</c> separators) so that they also read on the other platform when their names are valid there, and as
/// their <see cref="IPath.PathDisplay"/> string when they can't be converted to it. Strings are read in the current platform's path format, as the
/// <c>Parse</c> methods on <see cref="FilePath"/> and <see cref="DirectoryPath"/> read them on the platform the app runs on with the <see
/// cref="PathOptions.None"/> option, so a path in that format reads back as the same path. Strings that are empty, end with a separator or end with a
/// <c>.</c> or <c>..</c> segment are read as directory paths and all other strings as file paths.
/// </summary>
public sealed class RelativePathJsonConverter : JsonConverter<IRelativePath>
{
    private static readonly RelativePathConverter TypeConverter = new();

    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert) => PathJsonConversion.CanConvert<IRelativePath>(typeToConvert);

    /// <inheritdoc/>
    /// <exception cref="JsonException">The JSON value is not a string or it is not a valid path.</exception>
    public override IRelativePath Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return PathJsonConversion.Read(ref reader, TypeConverter);
    }

    /// <inheritdoc/>
    /// <exception cref="JsonException">The property name is not a valid path.</exception>
    public override IRelativePath ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return PathJsonConversion.ReadAsPropertyName(ref reader, TypeConverter);
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, IRelativePath value, JsonSerializerOptions options)
    {
        PathJsonConversion.Write(writer, value);
    }

    /// <inheritdoc/>
    public override void WriteAsPropertyName(Utf8JsonWriter writer, IRelativePath value, JsonSerializerOptions options)
    {
        PathJsonConversion.WriteAsPropertyName(writer, value);
    }
}
