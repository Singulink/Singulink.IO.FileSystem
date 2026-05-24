using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

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

    internal string SeparatorAsString { get; }

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

    internal PathFormat(char separator)
    {
        Separator = separator;
        SeparatorAsString = separator.ToString();

        RelativeCurrentDirectory = DirectoryPath.ParseRelative(".", this, PathOptions.None);
        RelativeParentDirectory = DirectoryPath.ParseRelative("..", this, PathOptions.None);
    }

    /// <summary>
    /// Builds a relative path consisting of <paramref name="parentDirNavCount"/> consecutive parent-directory navigation segments
    /// (e.g. <c>"../../"</c>). Returns the cached <see cref="RelativeParentDirectory"/> path when <paramref name="parentDirNavCount"/>
    /// is 1 to avoid allocation.
    /// </summary>
    internal string GetRelativeParentNavPath(int parentDirNavCount)
    {
        string nav = RelativeParentDirectory.PathDisplay;

        if (parentDirNavCount is 1)
            return nav;

        if (parentDirNavCount <= 0)
            return string.Empty;

        return string.Create(nav.Length * parentDirNavCount, nav, static (span, source) => {
            var src = source.AsSpan();
            for (int i = 0; i < span.Length; i += src.Length)
                src.CopyTo(span[i..]);
        });
    }

    /// <summary>
    /// Determines whether the given file extension is valid for this path format.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The empty extension is valid for all path formats.</para>
    /// <para>
    /// If non-empty, the extension should begin with a <c>.</c> character, and not have any <c>.</c> characters after the first one.</para>
    /// </remarks>
    public virtual bool IsValidExtension(ReadOnlySpan<char> extension, PathOptions options = PathOptions.NoUnfriendlyNames)
    {
        if (extension.Length is 0)
            return true;

        if (extension is not ['.', .. var rest] || rest.Contains('.'))
            return false;

        options = options.ClearFlags(PathOptions.NoReservedDeviceNames | PathOptions.NoLeadingSpaces | PathOptions.NoTrailingDots);
        return ValidateEntryName(extension, options, allowWildcards: false, out _, wantsError: false);
    }

    /// <summary>
    /// Gets the name of the path format.
    /// </summary>
    public abstract override string ToString();

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

    /// <summary>
    /// Determines whether the shape of the supplied (separator-normalized) path requires it to refer to a directory. A path is directory-shaped when it ends
    /// with a separator or with a trailing navigational segment (<c>.</c> or <c>..</c>); otherwise it could refer to either a file or a directory.
    /// </summary>
    internal bool IsDirectoryShaped(ReadOnlySpan<char> separatorNormalizedPath)
    {
        // An empty path can only represent the current directory (files can never have an empty path).
        if (separatorNormalizedPath.Length is 0)
            return true;

        char last = separatorNormalizedPath[^1];

        if (last == Separator)
            return true;

        if (last is not '.')
            return false;

        // Trailing '.': directory-shaped only if it forms a "." or ".." segment (preceded by start of path or separator).
        if (separatorNormalizedPath.Length is 1)
            return true;

        char prev = separatorNormalizedPath[^2];

        if (prev == Separator)
            return true;

        if (prev is not '.')
            return false;

        // Trailing "..": standalone ".." segment only if it is the entire path or is preceded by a separator (e.g. "foo/..");
        // otherwise it is just the tail of a filename like "foo..".
        return separatorNormalizedPath.Length is 2 || separatorNormalizedPath[^3] == Separator;
    }

    // 'wantsError' indicates whether we should potentially allocate a useful error message or just return a possibly generic one or none.
    internal virtual bool ValidateEntryName(ReadOnlySpan<char> name, PathOptions options, bool allowWildcards, [NotNullWhen(false)] out string? error, bool wantsError = true)
    {
        if (name.IsEmpty) {
            error = "Empty entry name.";
            return false;
        }

        if (name.IndexOfAny(Separator, (char)0) is int i and >= 0) {
            error = wantsError ? $"Invalid character '{name[i]}' in entry name '{name}'." : "Invalid.";
            return false;
        }

        if (options.HasAllFlags(PathOptions.NoControlCharacters)) {
            int idx = name.IndexOfAnyInRange((char)1, (char)31);
            if (idx >= 0) {
                error = wantsError ? $"Invalid control character '{name[idx]}' in entry name '{name}'." : "Invalid.";
                return false;
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

    internal string NormalizeRelativePath(ReadOnlySpan<char> path, PathOptions options, bool appendSeparator, out int rootLength)
    {
        var pathKind = GetPathKind(path);

        if (pathKind is PathKind.Absolute)
            throw new ArgumentException("Path is not a relative path.", nameof(path));

        if (pathKind is PathKind.RelativeRooted) {
            if (options.HasAllFlags(PathOptions.NoNavigation))
                throw new ArgumentException("Rooted relative paths are not allowed for this path.");

            if (path.Length == 1) {
                rootLength = 1;
                return SeparatorAsString;
            }

            path = path[1..];
        }
        else if (path.Length is 0 || path.SequenceEqual(".")) {
            if (path.Length is 1 && options.HasAllFlags(PathOptions.NoNavigation))
                throw new ArgumentException("Invalid navigational path segment.", nameof(path));

            rootLength = 0;
            return string.Empty;
        }

        var segments = SplitNonRootedRelativePath(path, options);

        if (pathKind is PathKind.RelativeRooted) {
            if (segments.Count > 0 && segments[0] is "..")
                throw new ArgumentException("Attempt to navigate past root directory.", nameof(path));

            rootLength = 1;

            if (segments.Count is 0)
                return SeparatorAsString;

            segments.Insert(0, string.Empty);
        }
        else {
            rootLength = 0;
        }

        // Directory paths always end with a separator (except for the empty relative "current directory" path).
        if (appendSeparator && segments is not [.., ""])
            segments.Add(string.Empty);

        return string.Join(SeparatorAsString, segments);
    }

    internal string NormalizeAbsolutePath(ReadOnlySpan<char> path, PathOptions options, bool asDirectory, out int rootLength)
    {
        var pathKind = GetPathKind(path);

        if (pathKind is not PathKind.Absolute)
            throw new ArgumentException("Path is not an absolute path.", nameof(path));

        var root = SplitAbsoluteRoot(path, out var rest);
        var segments = SplitNonRootedRelativePath(rest, options);

        if (segments.Count > 0 && segments[0] is "..")
            throw new ArgumentException("Attempt to navigate past root directory.", nameof(path));

        rootLength = root.Length;

        if (segments.Count is 0)
            return root.ToString();

        if (asDirectory)
            segments.Add(string.Empty);

        string tail = string.Join(Separator, segments);
        return $"{root}{tail}";
    }

    internal abstract string GetAbsolutePathExportString(string pathDisplay);

    /// <summary>
    /// Splits out any relative parent navigation from the rest of the path and outputs the number of parent directories to drill down into.
    /// </summary>
    internal StringOrSpan SplitRelativeNavigation(StringOrSpan path, out int parentDirNavCount)
    {
        if (GetPathKind(path) is PathKind.RelativeRooted) {
            parentDirNavCount = -1;
            return path.Span[1..];
        }

        parentDirNavCount = 0;

        while (path.Span is ".." || path.Span.StartsWith(RelativeParentDirectory.PathDisplay)) {
            parentDirNavCount++;
            path = path.Span[Math.Min(path.Length, 3)..];
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
            if (path.Span.Contains(destinationFormat.SeparatorAsString, StringComparison.Ordinal))
                throw new ArgumentException($"Invalid separator character '{destinationFormat.Separator}' in path entry.");
            else if (sourceFormat.GetPathKind(path) == PathKind.RelativeRooted && !destinationFormat.SupportsRelativeRootedPaths)
                throw new ArgumentException("Destination path format does not support relative rooted paths.");
            else
                path = path.Replace(sourceFormat.Separator, destinationFormat.Separator);
        }

        return path;
    }

    internal string? ChangeFileNameExtension(string path, string? extension, int rootLength, PathOptions options)
    {
        if (extension?.Length > 0 && extension.LastIndexOf('.') is not 0)
            throw new ArgumentException("New file extension must either be empty or start with a dot '.' character and contain no additional dots.", nameof(extension));

        string newFileName = $"{GetFileNameWithoutExtension(path).Span}{extension}";

        if (!ValidateEntryName(newFileName, options, allowWildcards: false, out string error))
            throw new ArgumentException($"Invalid new file name: {error}", nameof(extension));

        if (GetEntryName(path, rootLength).Span.SequenceEqual(newFileName))
            return null;

        var parentDir = GetParentDirectoryPath(path, rootLength);

        // parentDir always ends with a separator under the directory-path invariant.
        return $"{parentDir}{newFileName}";
    }

    internal string? AddFileNameExtension(string path, string? extension, int rootLength, PathOptions options)
    {
        if (extension?.Length > 0 && extension.LastIndexOf('.') is not 0)
            throw new ArgumentException("New file extension must either be empty or start with a dot '.' character and contain no additional dots.", nameof(extension));

        StringOrSpan fileName = GetEntryName(path, rootLength);

        string newFileName = $"{fileName.Span}{extension}";

        if (!ValidateEntryName(newFileName, options, allowWildcards: false, out string error))
            throw new ArgumentException($"Invalid new file name: {error}", nameof(extension));

        if (fileName.Span.SequenceEqual(newFileName))
            return null;

        var parentDir = GetParentDirectoryPath(path, rootLength);

        // parentDir always ends with a separator under the directory-path invariant.
        return $"{parentDir}{newFileName}";
    }

    /// <summary>
    /// Gets the parent directory path of the given path. The returned path always ends with the path separator, satisfying the directory-path invariant.
    /// Returns an empty span if the path has no parent (i.e. it is a relative path with no separators).
    /// </summary>
    internal ReadOnlySpan<char> GetParentDirectoryPath(ReadOnlySpan<char> path, int rootLength)
    {
        // Strip a single trailing separator if present (and the path is not the root itself) so that LastIndexOf finds the
        // separator preceding the last entry rather than the trailing one.
        if (path.Length > rootLength && path[^1] == Separator)
            path = path[..^1];

        int lastSeparatorIndex = path.LastIndexOf(Separator);

        if (lastSeparatorIndex < 0)
            return ReadOnlySpan<char>.Empty;

        // Include the trailing separator in the parent directory path. For paths whose last separator is the root separator,
        // returning rootLength bytes (which already includes the separator) yields the root directory.
        return path[..Math.Max(lastSeparatorIndex + 1, rootLength)];
    }

    internal StringOrSpan GetEntryName(StringOrSpan path, int rootLength)
    {
        if (path.Length is 0)
            return path;

        if (path.Length is 1)
            return GetPathKind(path) is PathKind.RelativeRooted ? StringOrSpan.Empty : path;

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
    /// Splits a normalized non-rooted relative path into a list of parts.
    /// </summary>
    private List<string> SplitNonRootedRelativePath(ReadOnlySpan<char> path, PathOptions options)
    {
        // Add extra capacity for possible prepended+appended empty segments (to insert separators) in the methods that call this.
        int maxSegmentCount = 3;

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
