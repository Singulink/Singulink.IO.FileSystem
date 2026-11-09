using System.Text.Json;
using Singulink.IO.Converters;

namespace Singulink.IO.Json;

/// <summary>
/// Shared implementation of the path JSON converters. Paths are read from JSON strings with their type's <see cref="PathTypeConverter{TPath}"/> and written as
/// described in <see cref="PathStrings"/>, both as values and as dictionary keys.
/// </summary>
internal static class PathJsonConversion
{
    /// <summary>
    /// Determines whether a converter for <typeparamref name="TPath"/> converts the specified type: <typeparamref name="TPath"/> itself and the path classes
    /// that implement it, so paths are also converted when the serializer only knows their runtime type (e.g. in <see cref="object"/> properties).
    /// </summary>
    public static bool CanConvert<TPath>(Type typeToConvert) where TPath : class, IPath
    {
        return typeToConvert == typeof(TPath) || (!typeToConvert.IsInterface && typeof(TPath).IsAssignableFrom(typeToConvert));
    }

    public static TPath Read<TPath>(ref Utf8JsonReader reader, PathTypeConverter<TPath> converter) where TPath : class, IPath
    {
        if (reader.TokenType is not JsonTokenType.String)
            throw new JsonException($"Invalid {converter.PathTypeDescription}: expected a JSON string but found {reader.TokenType}.");

        return Parse(converter, reader.GetString()!);
    }

    public static TPath ReadAsPropertyName<TPath>(ref Utf8JsonReader reader, PathTypeConverter<TPath> converter) where TPath : class, IPath
    {
        return Parse(converter, reader.GetString()!);
    }

    public static void Write(Utf8JsonWriter writer, IPath value) => writer.WriteStringValue(PathStrings.GetString(value));

    public static void WriteAsPropertyName(Utf8JsonWriter writer, IPath value) => writer.WritePropertyName(PathStrings.GetString(value));

    private static TPath Parse<TPath>(PathTypeConverter<TPath> converter, string s) where TPath : class, IPath
    {
        if (!converter.TryParse(s, out var path, out string? error))
            throw new JsonException(converter.GetErrorMessage(s, error));

        return path;
    }
}
