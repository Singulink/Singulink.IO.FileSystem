using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Singulink.IO;

/// <summary>
/// Contains methods for parsing file paths and working with special files.
/// </summary>
public static class FilePath
{
    #region File Parsing

    /// <summary>
    /// Parses an absolute or relative file path using the specified options and the current platform's format.
    /// </summary>
    /// <param name="path">A file path.</param>
    /// <param name="options">Specifies the path parsing options.</param>
    public static IFilePath Parse(ReadOnlySpan<char> path, PathOptions options = PathOptions.NoUnfriendlyNames)
    {
        return Parse(path, PathFormat.Current, options);
    }

    /// <summary>
    /// Parses an absolute or relative file path using the specified format and options.
    /// </summary>
    /// <param name="path">A file path.</param>
    /// <param name="format">The path's format.</param>
    /// <param name="options">Specifies the path parsing options.</param>
    public static IFilePath Parse(ReadOnlySpan<char> path, PathFormat format, PathOptions options = PathOptions.NoUnfriendlyNames)
    {
        if (format.GetPathKind(path) == PathKind.Absolute)
            return ParseAbsolute(path, format, options);

        return ParseRelative(path, format, options);
    }

    /// <summary>
    /// Attempts to parse an absolute or relative file path using the <see cref="PathOptions.NoUnfriendlyNames"/> option and the current platform's format.
    /// </summary>
    /// <param name="path">A file path.</param>
    /// <param name="result">When this method returns, contains the parsed path if parsing succeeded, otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the path was parsed successfully, otherwise <see langword="false"/>.</returns>
    public static bool TryParse(ReadOnlySpan<char> path, [NotNullWhen(true)] out IFilePath? result)
    {
        return TryParse(path, PathFormat.Current, PathOptions.NoUnfriendlyNames, out result);
    }

    /// <summary>
    /// Attempts to parse an absolute or relative file path using the specified format and the <see cref="PathOptions.NoUnfriendlyNames"/> option.
    /// </summary>
    /// <param name="path">A file path.</param>
    /// <param name="format">The path's format.</param>
    /// <param name="result">When this method returns, contains the parsed path if parsing succeeded, otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the path was parsed successfully, otherwise <see langword="false"/>.</returns>
    public static bool TryParse(ReadOnlySpan<char> path, PathFormat format, [NotNullWhen(true)] out IFilePath? result)
    {
        return TryParse(path, format, PathOptions.NoUnfriendlyNames, out result);
    }

    /// <summary>
    /// Attempts to parse an absolute or relative file path using the specified options and the current platform's format.
    /// </summary>
    /// <param name="path">A file path.</param>
    /// <param name="options">Specifies the path parsing options.</param>
    /// <param name="result">When this method returns, contains the parsed path if parsing succeeded, otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the path was parsed successfully, otherwise <see langword="false"/>.</returns>
    public static bool TryParse(ReadOnlySpan<char> path, PathOptions options, [NotNullWhen(true)] out IFilePath? result)
    {
        return TryParse(path, PathFormat.Current, options, out result);
    }

    /// <summary>
    /// Attempts to parse an absolute or relative file path using the specified format and options.
    /// </summary>
    /// <param name="path">A file path.</param>
    /// <param name="format">The path's format.</param>
    /// <param name="options">Specifies the path parsing options.</param>
    /// <param name="result">When this method returns, contains the parsed path if parsing succeeded, otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the path was parsed successfully, otherwise <see langword="false"/>.</returns>
    public static bool TryParse(ReadOnlySpan<char> path, PathFormat format, PathOptions options, [NotNullWhen(true)] out IFilePath? result)
    {
        if (format.GetPathKind(path) == PathKind.Absolute)
        {
            result = TryParseAbsolute(path, format, options, out var absolutePath) ? absolutePath : null;
            return result is not null;
        }

        result = TryParseRelative(path, format, options, out var relativePath) ? relativePath : null;
        return result is not null;
    }

    #endregion

    #region Absolute File Parsing

    /// <summary>
    /// Parses an absolute file path using the specified options and the current platform's format.
    /// </summary>
    /// <param name="path">An absolute file path.</param>
    /// <param name="options">Specifies the path parsing options.</param>
    public static IAbsoluteFilePath ParseAbsolute(ReadOnlySpan<char> path, PathOptions options = PathOptions.NoUnfriendlyNames)
    {
        return ParseAbsolute(path, PathFormat.Current, options);
    }

    /// <summary>
    /// Parses an absolute file path using the specified format and options.
    /// </summary>
    /// <param name="path">An absolute file path.</param>
    /// <param name="format">The path's format.</param>
    /// <param name="options">Specifies the path parsing options.</param>
    public static IAbsoluteFilePath ParseAbsolute(ReadOnlySpan<char> path, PathFormat format, PathOptions options = PathOptions.NoUnfriendlyNames)
    {
        if (!TryParseAbsolute(path, format, options, out var result, out string? error))
            throw new ArgumentException(error, nameof(path));

        return result;
    }

    /// <summary>
    /// Attempts to parse an absolute file path using the <see cref="PathOptions.NoUnfriendlyNames"/> option and the current platform's format.
    /// </summary>
    /// <param name="path">An absolute file path.</param>
    /// <param name="result">When this method returns, contains the parsed path if parsing succeeded, otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the path was parsed successfully, otherwise <see langword="false"/>.</returns>
    public static bool TryParseAbsolute(ReadOnlySpan<char> path, [NotNullWhen(true)] out IAbsoluteFilePath? result)
    {
        return TryParseAbsolute(path, PathFormat.Current, PathOptions.NoUnfriendlyNames, out result);
    }

    /// <summary>
    /// Attempts to parse an absolute file path using the specified format and the <see cref="PathOptions.NoUnfriendlyNames"/> option.
    /// </summary>
    /// <param name="path">An absolute file path.</param>
    /// <param name="format">The path's format.</param>
    /// <param name="result">When this method returns, contains the parsed path if parsing succeeded, otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the path was parsed successfully, otherwise <see langword="false"/>.</returns>
    public static bool TryParseAbsolute(ReadOnlySpan<char> path, PathFormat format, [NotNullWhen(true)] out IAbsoluteFilePath? result)
    {
        return TryParseAbsolute(path, format, PathOptions.NoUnfriendlyNames, out result);
    }

    /// <summary>
    /// Attempts to parse an absolute file path using the specified options and the current platform's format.
    /// </summary>
    /// <param name="path">An absolute file path.</param>
    /// <param name="options">Specifies the path parsing options.</param>
    /// <param name="result">When this method returns, contains the parsed path if parsing succeeded, otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the path was parsed successfully, otherwise <see langword="false"/>.</returns>
    public static bool TryParseAbsolute(ReadOnlySpan<char> path, PathOptions options, [NotNullWhen(true)] out IAbsoluteFilePath? result)
    {
        return TryParseAbsolute(path, PathFormat.Current, options, out result);
    }

    /// <summary>
    /// Attempts to parse an absolute file path using the specified format and options.
    /// </summary>
    /// <param name="path">An absolute file path.</param>
    /// <param name="format">The path's format.</param>
    /// <param name="options">Specifies the path parsing options.</param>
    /// <param name="result">When this method returns, contains the parsed path if parsing succeeded, otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the path was parsed successfully, otherwise <see langword="false"/>.</returns>
    public static bool TryParseAbsolute(ReadOnlySpan<char> path, PathFormat format, PathOptions options, [NotNullWhen(true)] out IAbsoluteFilePath? result)
    {
        return TryParseAbsolute(path, format, options, out result, out _);
    }

    private static bool TryParseAbsolute(ReadOnlySpan<char> path, PathFormat format, PathOptions options, [NotNullWhen(true)] out IAbsoluteFilePath? result, [NotNullWhen(false)] out string? error)
    {
        result = null;
        path = format.NormalizeSeparators(path);

        if (format.IsDirectoryShaped(path))
        {
            error = "No file name in path.";
            return false;
        }

        if (!format.TryNormalizeAbsolutePath(path, options, asDirectory: false, out string? finalPath, out int rootLength, out error))
            return false;

        if (rootLength == finalPath.Length)
        {
            error = "No file name in path.";
            return false;
        }

        result = new IAbsoluteFilePath.Impl(finalPath, rootLength, format);
        return true;
    }

    #endregion

    #region Relative Parsing

    /// <summary>
    /// Parses a relative file path using the specified options and the current platform's format.
    /// </summary>
    /// <param name="path">A relative file path.</param>
    /// <param name="options">Specifies the path parsing options.</param>
    public static IRelativeFilePath ParseRelative(ReadOnlySpan<char> path, PathOptions options = PathOptions.NoUnfriendlyNames)
    {
        return ParseRelative(path, PathFormat.Current, options);
    }

    /// <summary>
    /// Parses a relative file path using the specified format and options.
    /// </summary>
    /// <param name="path">A relative file path.</param>
    /// <param name="format">The path's format.</param>
    /// <param name="options">Specifies the path parsing options.</param>
    public static IRelativeFilePath ParseRelative(ReadOnlySpan<char> path, PathFormat format, PathOptions options = PathOptions.NoUnfriendlyNames)
    {
        if (!TryParseRelative(path, format, options, out var result, out string? error))
            throw new ArgumentException(error, nameof(path));

        return result;
    }

    /// <summary>
    /// Attempts to parse a relative file path using the <see cref="PathOptions.NoUnfriendlyNames"/> option and the current platform's format.
    /// </summary>
    /// <param name="path">A relative file path.</param>
    /// <param name="result">When this method returns, contains the parsed path if parsing succeeded, otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the path was parsed successfully, otherwise <see langword="false"/>.</returns>
    public static bool TryParseRelative(ReadOnlySpan<char> path, [NotNullWhen(true)] out IRelativeFilePath? result)
    {
        return TryParseRelative(path, PathFormat.Current, PathOptions.NoUnfriendlyNames, out result);
    }

    /// <summary>
    /// Attempts to parse a relative file path using the specified format and the <see cref="PathOptions.NoUnfriendlyNames"/> option.
    /// </summary>
    /// <param name="path">A relative file path.</param>
    /// <param name="format">The path's format.</param>
    /// <param name="result">When this method returns, contains the parsed path if parsing succeeded, otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the path was parsed successfully, otherwise <see langword="false"/>.</returns>
    public static bool TryParseRelative(ReadOnlySpan<char> path, PathFormat format, [NotNullWhen(true)] out IRelativeFilePath? result)
    {
        return TryParseRelative(path, format, PathOptions.NoUnfriendlyNames, out result);
    }

    /// <summary>
    /// Attempts to parse a relative file path using the specified options and the current platform's format.
    /// </summary>
    /// <param name="path">A relative file path.</param>
    /// <param name="options">Specifies the path parsing options.</param>
    /// <param name="result">When this method returns, contains the parsed path if parsing succeeded, otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the path was parsed successfully, otherwise <see langword="false"/>.</returns>
    public static bool TryParseRelative(ReadOnlySpan<char> path, PathOptions options, [NotNullWhen(true)] out IRelativeFilePath? result)
    {
        return TryParseRelative(path, PathFormat.Current, options, out result);
    }

    /// <summary>
    /// Attempts to parse a relative file path using the specified format and options.
    /// </summary>
    /// <param name="path">A relative file path.</param>
    /// <param name="format">The path's format.</param>
    /// <param name="options">Specifies the path parsing options.</param>
    /// <param name="result">When this method returns, contains the parsed path if parsing succeeded, otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the path was parsed successfully, otherwise <see langword="false"/>.</returns>
    public static bool TryParseRelative(ReadOnlySpan<char> path, PathFormat format, PathOptions options, [NotNullWhen(true)] out IRelativeFilePath? result)
    {
        return TryParseRelative(path, format, options, out result, out _);
    }

    private static bool TryParseRelative(ReadOnlySpan<char> path, PathFormat format, PathOptions options, [NotNullWhen(true)] out IRelativeFilePath? result, [NotNullWhen(false)] out string? error)
    {
        result = null;
        path = format.NormalizeSeparators(path);

        if (format.IsDirectoryShaped(path))
        {
            error = "No file name in path.";
            return false;
        }

        if (!format.TryNormalizeRelativePath(path, options, appendSeparator: false, out string? finalPath, out int rootLength, out error))
            return false;

        result = new IRelativeFilePath.Impl(finalPath, rootLength, format);
        return true;
    }

    #endregion

    #region Special Files

    /// <summary>
    /// Gets the file path to the specified assembly.
    /// </summary>
    public static IAbsoluteFilePath GetAssemblyLocation(Assembly assembly)
    {
#pragma warning disable IL3000 // Avoid accessing Assembly file path when publishing as a single file
        string location = assembly.Location;
#pragma warning restore IL3000

        if (string.IsNullOrEmpty(location))
            throw new InvalidOperationException("Assembly does not have a location.");

        return ParseAbsolute(location, PathOptions.None);
    }

    /// <summary>
    /// Creates a new uniquely named zero-byte temporary file.
    /// </summary>
    /// <returns>The path to the newly created file.</returns>
    public static IAbsoluteFilePath CreateTempFile() => ParseAbsolute(Path.GetTempFileName(), PathOptions.None);

    #endregion
}
