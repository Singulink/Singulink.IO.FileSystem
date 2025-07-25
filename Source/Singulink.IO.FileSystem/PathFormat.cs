using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Singulink.Enums;
using Singulink.IO.Utilities;

namespace Singulink.IO;

/// <summary>
/// Provides formatting for OS specific and universal path formats.
/// </summary>
public abstract partial class PathFormat
{
    #region Singleton Instances

    /// <summary>
    /// Gets a path format compatible with the Windows platform.
    /// </summary>
    public static PathFormat Windows { get; } = new WindowsPathFormat();

    /// <summary>
    /// Gets a path format compatible with Unix-based platforms like macOS, iOS, Android and Linux.
    /// </summary>
    public static PathFormat Unix { get; } = new UnixPathFormat();

    /// <summary>
    /// Gets a universal cross-platform path format that plays nice across all operating systems. Only non-rooted relative paths are supported.
    /// </summary>
    /// <remarks>
    /// <para>The universal path format is useful for storing relative paths in a cross-platform compatible fashion and only allows paths that are valid on
    /// all platforms. It only supports the forward slash (<c>/</c>) character as a path separator, which works on both Windows and Unix-based systems
    /// alike. Attempting to parse paths containing backslashes (<c>\</c>) will throw an exception, but you can convert a <see cref="Windows"/> format path
    /// with backslashes to a <see cref="Universal"/> format path with methods like `<see cref="IRelativePath.ToPathFormat(PathFormat, PathOptions)"/>.
    /// </para>
    /// </remarks>
    public static PathFormat Universal { get; } = new UniversalPathFormat();

    /// <summary>
    /// Gets the path format for the current platform.
    /// </summary>
    ///
    public static PathFormat Current { get; } = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? Windows : Unix;

    #endregion

    /// <summary>
    /// Gets the standard path separator character. Alternate path characters are normalized to use the standard path separator.
    /// </summary>
    public char Separator { get; }

    internal string SeparatorString { get; }

    /// <summary>
    /// Gets a value indicating whether the path format supports relative rooted paths, i.e. relative paths that start with a path separator.
    /// </summary>
    public abstract bool SupportsRelativeRootedPaths { get; }

    /// <summary>
    /// Gets the relative directory that represents the current directory (i.e. <c>"."</c>).
    /// </summary>
    public IRelativeDirectoryPath RelativeCurrentDirectory { get; }

    /// <summary>
    /// Gets the relative directory that represents the parent directory (i.e. <c>".."</c>).
    /// </summary>
    public IRelativeDirectoryPath RelativeParentDirectory { get; }

    /// <summary>
    /// Gets the name of the path format.
    /// </summary>
    public abstract override string ToString();

    internal string ParentDirectoryWithSeparator { get; }

    internal PathFormat(char separator)
    {
        Separator = separator;
        SeparatorString = separator.ToString();
        ParentDirectoryWithSeparator = $"..{separator}";

        RelativeCurrentDirectory = DirectoryPath.ParseRelative(".", this, PathOptions.None);
        RelativeParentDirectory = DirectoryPath.ParseRelative("..", this, PathOptions.None);
    }

    /// <summary>
    /// Gets the kind of path that the given path will be interpreted as.
    /// </summary>
    /// <remarks>
    /// <para>This method does the minimum work necessary to categorize a given path into one of the three kinds of paths (<see cref="PathKind.Absolute"/>,
    /// <see cref="PathKind.Relative"/> or <see cref="PathKind.RelativeRooted"/>). It never throws an exception and does not validate that the path is
    /// actually in a correct format.
    /// </para>
    /// <para>The <see cref="Universal"/> path format always returns <see cref="PathKind.Relative"/> since that is the only path format it supports. The
    /// <see cref="Unix"/> path format never returns <see cref="PathKind.RelativeRooted"/> since those are always interpreted as absolute paths.</para>
    /// </remarks>
    internal abstract PathKind GetPathKind(ReadOnlySpan<char> path);

    internal abstract bool IsUncPath(string path);

    internal virtual ReadOnlySpan<char> NormalizeSeparators(ReadOnlySpan<char> path) => path;

    internal virtual bool ValidateEntryName(ReadOnlySpan<char> name, PathOptions options, bool allowWildcards, [NotNullWhen(false)] out string? error)
    {
        if (name.IsEmpty) {
            error = "Empty entry name.";
            return false;
        }

        if (name.IndexOfAny(Separator, (char)0) is int i and >= 0) {
            error = $"Invalid character '{name[i]}' in entry name '{name.ToString()}'.";
            return false;
        }

        if (options.HasAllFlags(PathOptions.NoControlCharacters)) {
            foreach (char c in name) {
                if (c < 32) {
                    error = $"Invalid control character '{c}' in entry name '{name.ToString()}'.";
                    return false;
                }
            }
        }

        if (options.HasAllFlags(PathOptions.NoLeadingSpaces) && name[0] is ' ') {
            error = "Entry name starts with a space.";
            return false;
        }

        if (options.HasAllFlags(PathOptions.NoTrailingDots) && name[^1] is '.') {
            error = "Entry name ends with a dot.";
            return false;
        }

        if (options.HasAllFlags(PathOptions.NoTrailingSpaces) && name[^1] is ' ') {
            error = "Entry name ends with a space.";
            return false;
        }

        error = null;
        return true;
    }

    internal string NormalizeRelativePath(ReadOnlySpan<char> path, PathOptions options, bool isFile, out int rootLength)
    {
        var pathKind = GetPathKind(path);

        if (pathKind is PathKind.Absolute)
            throw new ArgumentException("Path is not a relative path.", nameof(path));

        if (pathKind is PathKind.RelativeRooted) {
            if (options.HasAllFlags(PathOptions.NoNavigation))
                throw new ArgumentException("Rooted relative paths are not allowed for this path.");

            if (path.Length == 1) {
                rootLength = 1;
                return SeparatorString;
            }

            path = path[1..];
        }
        else if (path.Length is 0 || path.SequenceEqual(".")) {
            if (path.Length is 1 && options.HasAllFlags(PathOptions.NoNavigation))
                throw new ArgumentException("Invalid navigational path segment.", nameof(path));

            rootLength = 0;
            return string.Empty;
        }

        var segments = SplitNonRootedRelativePath(path, isFile, options);

        if (pathKind is PathKind.RelativeRooted) {
            if (segments.Count > 0 && segments[0] is "..")
                throw new ArgumentException("Attempt to navigate past root directory.", nameof(path));

            rootLength = 1;

            if (segments.Count is 0)
                return SeparatorString;

            segments.Insert(0, string.Empty);
        }
        else {
            rootLength = 0;
        }

        return string.Join(SeparatorString, segments);
    }

    internal string NormalizeAbsolutePath(ReadOnlySpan<char> path, PathOptions options, bool isFile, out int rootLength)
    {
        var pathKind = GetPathKind(path);

        if (pathKind is not PathKind.Absolute)
            throw new ArgumentException("Path is not an absolute path.", nameof(path));

        var root = SplitAbsoluteRoot(path, out var rest);
        var segments = SplitNonRootedRelativePath(rest, isFile, options);

        if (segments.Count > 0 && segments[0] is "..")
            throw new ArgumentException("Attempt to navigate past root directory.", nameof(path));

        rootLength = root.Length;
        return $"{root}{string.Join(Separator, segments)}";
    }

    internal abstract string GetAbsolutePathExportString(string pathDisplay);

    /// <summary>
    /// Splits out any relative parent navigation from the rest of the path and outputs the number of parent directories to drill down into.
    /// </summary>
    internal StringOrSpan SplitRelativeNavigation(StringOrSpan path, out int parentDirs)
    {
        if (GetPathKind(path) is PathKind.RelativeRooted) {
            parentDirs = -1;
            return path.Span[1..];
        }

        parentDirs = 0;

        while (path.Span is ".." || path.Span.StartsWith(ParentDirectoryWithSeparator)) {
            parentDirs++;
            int nextStartIndex = path.Length == 2 ? 2 : 3;
            path = path.Span[nextStartIndex..];
        }

        return path;
    }

    internal void ValidateSearchPattern(ReadOnlySpan<char> searchPattern, string paramName)
    {
        if (searchPattern is "*")
            return;

        if (!ValidateEntryName(searchPattern, PathOptions.None, allowWildcards: true, out string error))
            throw new ArgumentException($"Invalid search pattern: {error}", paramName);
    }

    /// <summary>
    /// Gets a mutually acceptable path format for combining two paths.
    /// </summary>
    /// <returns>
    /// If both formats match then the matching format, if one is universal then the other format, otherwise null to indicate that the
    /// paths are incompatible for combining.
    /// </returns>
    internal static PathFormat? GetMutualFormat(PathFormat first, PathFormat second)
    {
        if (first == second || second == Universal)
            return first;

        if (first == Universal)
            return second;

        return null;
    }

    internal static StringOrSpan ConvertRelativePathToMutualFormat(StringOrSpan path, PathFormat sourceFormat, PathFormat destinationFormat)
    {
        if (sourceFormat.Separator != destinationFormat.Separator)
            path = path.Replace(sourceFormat.Separator, destinationFormat.Separator);

        return path;
    }

    internal static StringOrSpan ConvertRelativePathFormat(StringOrSpan path, PathFormat sourceFormat, PathFormat destinationFormat)
    {
        if (sourceFormat.Separator != destinationFormat.Separator) {
            if (path.Span.Contains(destinationFormat.SeparatorString, StringComparison.Ordinal))
                throw new ArgumentException($"Invalid separator character '{destinationFormat.Separator}' in path entry.");
            else if (sourceFormat.GetPathKind(path) == PathKind.RelativeRooted && !destinationFormat.SupportsRelativeRootedPaths)
                throw new ArgumentException("Destination path format does not support relative rooted paths.");
            else
                path = path.Replace(sourceFormat.Separator, destinationFormat.Separator);
        }

        return path;
    }

    internal string ChangeFileNameExtension(string path, string? newExtension, PathOptions options)
    {
        if (newExtension is null)
            newExtension = string.Empty;
        else if (newExtension.Length > 0 && newExtension.LastIndexOf('.') is not 0)
            throw new ArgumentException("New file extension must either be empty or start with a dot '.' character and contain no additional dots.", nameof(newExtension));

        var parentDir = GetParentDirectoryPath(path, 0);
        var fileNameWithoutExtension = GetFileNameWithoutExtension(path);

        string newFileName = $"{fileNameWithoutExtension.Span}{newExtension}";

        if (!ValidateEntryName(newFileName, options, allowWildcards: false, out string error))
            throw new ArgumentException($"Invalid new file name: {error}", nameof(newExtension));

        return $"{parentDir}{newFileName}";
    }

    internal ReadOnlySpan<char> GetParentDirectoryPath(ReadOnlySpan<char> path, int rootLength)
    {
        int lastSeparatorIndex = path.LastIndexOf(Separator);
        return lastSeparatorIndex >= 0 ? path[..Math.Max(lastSeparatorIndex, rootLength)] : string.Empty;
    }

    internal StringOrSpan GetEntryName(StringOrSpan path, int rootLength)
    {
        if (path.Length is 0)
            return path;

        if (path.Length is 1)
            return GetPathKind(path) == PathKind.RelativeRooted ? StringOrSpan.Empty : path;

        if (path.Span[^1] == Separator)
            path = path.Span[..^1];

        if (path.Length <= rootLength)
            return path;

        int lastSeparatorIndex = path.Span.LastIndexOf(Separator);

        StringOrSpan name = lastSeparatorIndex < 0 ? path : path.Span[(lastSeparatorIndex + 1)..];

        if (name.Span.SequenceEqual(".."))
            return string.Empty;

        return name;
    }

    /// <summary>
    /// Gets the first entry of a non-rooted relative path.
    /// </summary>
    internal StringOrSpan GetFirstEntry(StringOrSpan path)
    {
        int separatorIndex = path.Span.IndexOf(Separator);
        return separatorIndex < 0 ? path : path.Span[..separatorIndex];
    }

    internal StringOrSpan GetFileNameWithoutExtension(string path)
    {
        StringOrSpan fileName = GetEntryName(path, 0);
        int extensionIndex = fileName.Span.LastIndexOf('.');

        if (extensionIndex < 0)
            return fileName;

        return fileName.Span[..extensionIndex];
    }

    internal StringOrSpan GetFileNameExtension(string path)
    {
        StringOrSpan fileName = GetEntryName(path, 0);
        int extensionIndex = fileName.Span.LastIndexOf('.');

        if (extensionIndex < 0)
            return string.Empty;

        return fileName.Span[extensionIndex..];
    }

    internal void EnsureCurrent()
    {
        if (this != Current)
            throw new InvalidOperationException("The path format is not the correct type for the current platform.");
    }

    internal void EnsureCurrent(string? paramName)
    {
        if (this != Current)
            throw new ArgumentException("The path format is not the correct type for the current platform.", paramName);
    }

    /// <summary>
    /// Returns the root of the absolute path and outputs the remaining non-rooted relative component of the path.
    /// </summary>
    private protected abstract ReadOnlySpan<char> SplitAbsoluteRoot(ReadOnlySpan<char> path, out ReadOnlySpan<char> rest);

    /// <summary>
    /// Splits a non-rooted relative path into a list of parts.
    /// </summary>
    private List<string> SplitNonRootedRelativePath(ReadOnlySpan<char> path, bool isFile, PathOptions options)
    {
        int maxSegmentCount = 1;

        foreach (char c in path) {
            if (c == Separator)
                maxSegmentCount++;
        }

        var segments = new List<string>(maxSegmentCount);

        while (path.Length > 0) {
            int separatorIndex = path.IndexOf(Separator);

            ReadOnlySpan<char> segment;

            if (separatorIndex < 0) {
                segment = path;
                path = default;
            }
            else {
                segment = path[..separatorIndex];
                path = path[(separatorIndex + 1)..];
            }

            if (segment.Length is 0) {
                if (!options.HasAllFlags(PathOptions.AllowEmptyDirectories))
                    throw new ArgumentException("Invalid empty directory in path.", nameof(path));
            }
            else if (segment is "." or "..") {
                if (isFile && path.Length is 0)
                    throw new ArgumentException("File paths cannot end in a navigational path segment.", nameof(path));

                if (options.HasAllFlags(PathOptions.NoNavigation))
                    throw new ArgumentException("Invalid navigational path segment.", nameof(path));

                if (segment.Length is 2) {
                    if (segments.Count is 0 || segments[^1] is "..")
                        segments.Add("..");
                    else
                        segments.RemoveAt(segments.Count - 1);
                }
            }
            else if (!ValidateEntryName(segment, options, allowWildcards: false, out string error)) {
                throw new ArgumentException(error, nameof(path));
            }
            else {
                segments.Add(segment.ToString());
            }
        }

        return segments;
    }
}
