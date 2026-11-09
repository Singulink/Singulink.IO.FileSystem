using System.Diagnostics.CodeAnalysis;

namespace Singulink.IO.Converters;

/// <summary>
/// Reads and writes the path strings of the path type converters and JSON converters.
/// </summary>
/// <remarks>
/// <para>Strings are read in the current platform's format with <see cref="PathOptions.None"/>, exactly as the <c>Parse</c> methods read them there with
/// that option, so that every valid path reads. When an absolute path is expected, a string in the other platform's absolute form (a Windows absolute path
/// on Unix, or a Unix absolute path on Windows) fails with a message that says so.</para>
/// <para>Absolute paths are written as their <see cref="IPath.PathDisplay"/>. Relative paths are written in the <see cref="PathFormat.Universal"/> format
/// (with <c>/</c> separators) and as their <see cref="IPath.PathDisplay"/> when they can't be converted to it. A path in the current platform's format reads
/// back as the same path, and a relative path also reads on the other platform as the same names when its names are valid there.</para>
/// </remarks>
internal static class PathStrings
{
    private const PathOptions Options = PathOptions.None;

    /// <summary>
    /// Parses a path string, using the specified format as the current platform's format.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="currentFormat">The current platform's format (<see cref="PathFormat.Windows"/> or <see cref="PathFormat.Unix"/>).</param>
    /// <param name="isAbsolute"><see langword="true"/> to parse an absolute path, <see langword="false"/> to parse a relative path, or <see langword="null"/>
    /// to decide by the string, as the <c>Parse</c> methods do.</param>
    /// <param name="isDirectory"><see langword="true"/> to parse a directory path, <see langword="false"/> to parse a file path, or <see langword="null"/> to
    /// parse directory-shaped strings (empty, ending with a separator or ending with a <c>.</c> or <c>..</c> segment) as directory paths and all other strings
    /// as file paths.</param>
    /// <param name="path">The parsed path.</param>
    /// <param name="error">The reason the string could not be parsed.</param>
    public static bool TryParse(string s, PathFormat currentFormat, bool? isAbsolute, bool? isDirectory, [NotNullWhen(true)] out IPath? path, [NotNullWhen(false)] out string? error)
    {
        if (isAbsolute ?? (currentFormat.GetPathKind(s) is PathKind.Absolute))
        {
            // These strings are not absolute paths on the current platform, so they fail either way; only the message is clearer.

            if (currentFormat == PathFormat.Unix && IsWindowsAbsolutePath(s))
            {
                path = null;
                error = $"Path is a Windows absolute path. Absolute paths are read in the format of the platform the app runs on ({currentFormat}).";
                return false;
            }

            if (currentFormat == PathFormat.Windows && IsUnixAbsolutePath(s))
            {
                path = null;
                error = "Path is a Unix absolute path, which is a rooted relative path on Windows. " +
                    $"Absolute paths are read in the format of the platform the app runs on ({currentFormat}).";

                return false;
            }

            return TryParseAbsolute(s, currentFormat, isDirectory ?? IsDirectoryShaped(s, currentFormat), out path, out error);
        }

        return TryParseRelative(s, currentFormat, isDirectory ?? IsDirectoryShaped(s, currentFormat), out path, out error);
    }

    /// <summary>
    /// Gets the string a path is written as: absolute paths as their <see cref="IPath.PathDisplay"/>, relative paths in the <see cref="PathFormat.Universal"/>
    /// format when they can be converted to it and as their <see cref="IPath.PathDisplay"/> otherwise.
    /// </summary>
    public static string GetString(IPath path)
    {
        if (path is IRelativePath.Impl { IsRooted: false } relativePath &&
            relativePath.TryToPathFormat(PathFormat.Universal, PathOptions.None, out IRelativePath? universalPath, out _))
        {
            return universalPath.PathDisplay;
        }

        return path.PathDisplay;
    }

    private static bool TryParseAbsolute(string s, PathFormat format, bool isDirectory, [NotNullWhen(true)] out IPath? path, [NotNullWhen(false)] out string? error)
    {
        bool success;

        if (isDirectory)
        {
            success = DirectoryPath.TryParseAbsolute(s, format, Options, out var directoryPath, out error);
            path = directoryPath;
        }
        else
        {
            success = FilePath.TryParseAbsolute(s, format, Options, out var filePath, out error);
            path = filePath;
        }

        return success;
    }

    private static bool TryParseRelative(string s, PathFormat format, bool isDirectory, [NotNullWhen(true)] out IPath? path, [NotNullWhen(false)] out string? error)
    {
        bool success;

        if (isDirectory)
        {
            success = DirectoryPath.TryParseRelative(s, format, Options, out var directoryPath, out error);
            path = directoryPath;
        }
        else
        {
            success = FilePath.TryParseRelative(s, format, Options, out var filePath, out error);
            path = filePath;
        }

        return success;
    }

    private static bool IsDirectoryShaped(string s, PathFormat format) => format.IsDirectoryShaped(format.NormalizeSeparators(s));

    /// <summary>
    /// Determines whether a string is in a Windows absolute form: a drive letter followed by <c>:</c> and nothing else or a separator (e.g. <c>C:</c>,
    /// <c>C:\</c> or <c>C:/</c>), or a <c>\\</c> prefix (e.g. <c>\\server\share\</c>, <c>\\?\C:\</c> or <c>\\.\C:\</c>).
    /// </summary>
    private static bool IsWindowsAbsolutePath(string s)
    {
        if (s.StartsWith(@"\\", StringComparison.Ordinal))
            return true;

        return s is [char drive, ':', ..] && char.IsAsciiLetter(drive) && s is [_, _] or [_, _, '\\' or '/', ..];
    }

    /// <summary>
    /// Determines whether a string starts with a single <c>/</c>, which is an absolute path on Unix and a rooted relative path on Windows.
    /// </summary>
    private static bool IsUnixAbsolutePath(string s) => s is ['/'] or ['/', not '/', ..];
}
