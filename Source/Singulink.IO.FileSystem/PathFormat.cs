using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
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
    public char SeparatorChar { get; }

    /// <summary>
    /// Gets a value indicating whether the path format supports relative rooted paths, i.e. relative paths that start with a path separator.
    /// </summary>
    public abstract bool SupportsRelativeRootedPaths { get; }

    /// <summary>
    /// Gets the relative directory that represents the current directory.
    /// </summary>
    public IRelativeDirectoryPath RelativeCurrentDirectory { get; }

    /// <summary>
    /// Gets the relative directory that represents the parent directory.
    /// </summary>
    public IRelativeDirectoryPath RelativeParentDirectory { get; }

    internal string SeparatorString { get; }

    internal string ParentDirectoryWithSeparator { get; }

    internal PathFormat(char separator)
    {
        SeparatorChar = separator;
        SeparatorString = SeparatorChar.ToString();
        ParentDirectoryWithSeparator = ".." + SeparatorString;

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
        SetPathFormatDependentOptions(ref options);

        if (name.IsEmpty) {
            error = "Empty entry name.";
            return false;
        }

        if (options.HasFlag(PathOptions.NoLeadingSpaces) && name[0] == ' ') {
            error = "Entry name starts with a space.";
            return false;
        }

        if (options.HasFlag(PathOptions.NoTrailingDots) && name[^1] == '.') {
            error = "Entry name ends with a dot.";
            return false;
        }

        if (options.HasFlag(PathOptions.NoTrailingSpaces) && name[^1] == ' ') {
            error = "Entry name ends with a space.";
            return false;
        }

        error = null;
        return true;
    }

    internal string NormalizeRelativePath(ReadOnlySpan<char> path, PathOptions options, bool isFile, out int rootLength)
    {
        SetPathFormatDependentOptions(ref options);

        var pathKind = GetPathKind(path);

        if (pathKind == PathKind.Absolute)
            throw new ArgumentException("Path is not a relative path.", nameof(path));

        if (pathKind == PathKind.RelativeRooted) {
            if (options.HasFlag(PathOptions.NoNavigation))
                throw new ArgumentException("Rooted relative paths are not allowed for this path.");

            if (path.Length == 1) {
                rootLength = 1;
                return SeparatorString;
            }

            path = path[1..];
        }
        else if (path.Length == 0 || path.SequenceEqual(".")) {
            if (path.Length == 1 && options.HasFlag(PathOptions.NoNavigation))
                throw new ArgumentException("Invalid navigational path segment.", nameof(path));

            rootLength = 0;
            return string.Empty;
        }

        var segments = SplitNonRootedRelativePath(path, isFile, options);

        if (pathKind == PathKind.RelativeRooted) {
            if (segments.Count > 0 && segments[0] == "..")
                throw new ArgumentException("Attempt to navigate past root directory.", nameof(path));

            rootLength = 1;

            if (segments.Count == 0)
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
        SetPathFormatDependentOptions(ref options);

        var pathKind = GetPathKind(path);

        if (pathKind != PathKind.Absolute)
            throw new ArgumentException("Path is not an absolute path.", nameof(path));

        var root = SplitAbsoluteRoot(path, out var rest);
        var segments = SplitNonRootedRelativePath(rest, isFile, options);

        if (segments.Count > 0 && segments[0] == "..")
            throw new ArgumentException("Attempt to navigate past root directory.", nameof(path));

        rootLength = root.Length;
        return StringHelper.Concat(root, string.Join(SeparatorString, segments));
    }

    internal abstract string GetAbsolutePathExportString(string pathDisplay);

    /// <summary>
    /// Splits out any relative parent navigation from the rest of the path and outputs the number of parent directories to drill down into.
    /// </summary>
    internal StringOrSpan SplitRelativeNavigation(StringOrSpan path, out int parentDirs)
    {
        if (GetPathKind(path) == PathKind.RelativeRooted) {
            parentDirs = -1;
            return path.Span[1..];
        }

        parentDirs = 0;

        while (path.Span.SequenceEqual("..") || path.Span.StartsWith(ParentDirectoryWithSeparator)) {
            parentDirs++;
            int nextStartIndex = path.Length == 2 ? 2 : 3;
            path = path.Span[nextStartIndex..];
        }

        return path;
    }

    internal void ValidateSearchPattern(ReadOnlySpan<char> searchPattern, string paramName)
    {
        if (searchPattern.Length == 1 && searchPattern[0] == '*')
            return;

        if (!ValidateEntryName(searchPattern, PathOptions.None, true, out string error))
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
        if (sourceFormat.SeparatorChar != destinationFormat.SeparatorChar)
            path = path.Replace(sourceFormat.SeparatorChar, destinationFormat.SeparatorChar);

        return path;
    }

    internal static StringOrSpan ConvertRelativePathFormat(StringOrSpan path, PathFormat sourceFormat, PathFormat destinationFormat)
    {
        if (sourceFormat.SeparatorChar != destinationFormat.SeparatorChar) {
            if (path.Span.Contains(destinationFormat.SeparatorString, StringComparison.Ordinal))
                throw new ArgumentException($"Invalid separator character '{destinationFormat.SeparatorChar}' in path entry.");
            else if (sourceFormat.GetPathKind(path) == PathKind.RelativeRooted && !destinationFormat.SupportsRelativeRootedPaths)
                throw new ArgumentException("Destination path format does not support relative rooted paths.");
            else
                path = path.Replace(sourceFormat.SeparatorChar, destinationFormat.SeparatorChar);
        }

        return path;
    }

    internal string ChangeFileNameExtension(string path, string? newExtension, PathOptions options)
    {
        if (newExtension == null)
            newExtension = string.Empty;
        else if (newExtension.Length > 0 && newExtension[0] != '.')
            throw new ArgumentException("New extension must either be empty or start with a dot '.' character.", nameof(newExtension));

        var parentDir = GetPreviousDirectory(path, 0);
        var fileNameWithoutExtension = GetFileNameWithoutExtension(path);

        string newFileName = StringHelper.Concat(fileNameWithoutExtension, newExtension);

        if (!ValidateEntryName(newFileName, options, false, out string error))
            throw new ArgumentException($"Invalid new file name: {error}", nameof(newExtension));

        return StringHelper.Concat(parentDir, newFileName);
    }

    internal ReadOnlySpan<char> GetPreviousDirectory(ReadOnlySpan<char> path, int rootLength)
    {
        int lastSeparatorIndex = path.LastIndexOf(SeparatorChar);
        return lastSeparatorIndex >= 0 ? path[..Math.Max(lastSeparatorIndex, rootLength)] : string.Empty;
    }

    internal StringOrSpan GetEntryName(StringOrSpan path, int rootLength)
    {
        if (path.Length == 0)
            return path;

        if (path.Length == 1)
            return GetPathKind(path) == PathKind.RelativeRooted ? StringOrSpan.Empty : path;

        if (path.Span[^1] == SeparatorChar)
            path = path.Span[..^1];

        if (path.Length <= rootLength)
            return path;

        int lastSeparatorIndex = path.Span.LastIndexOf(SeparatorChar);

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
        int separatorIndex = path.Span.IndexOf(SeparatorChar);
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
    protected abstract ReadOnlySpan<char> SplitAbsoluteRoot(ReadOnlySpan<char> path, out ReadOnlySpan<char> rest);

    /// <summary>
    /// Appends NoUnfriendlyNames if the PathFormatDependent flag is set for the Universal and Windows path formats. Must be called at the start of all
    /// non-private entry points into PathFormat methods that accept a PathOptions parameter.
    /// </summary>
    private void SetPathFormatDependentOptions(ref PathOptions options)
    {
        if (options.HasFlag(PathOptions.PathFormatDependent) && this != Unix)
            options |= PathOptions.NoUnfriendlyNames;
    }

    /// <summary>
    /// Splits a non-rooted relative path into a list of parts.
    /// </summary>
    private List<string> SplitNonRootedRelativePath(ReadOnlySpan<char> path, bool isFile, PathOptions options)
    {
        int maxSegmentCount = 1;

        foreach (char c in path) {
            if (c == SeparatorChar)
                maxSegmentCount++;
        }

        var segments = new List<string>(maxSegmentCount);

        while (path.Length > 0) {
            int separatorIndex = path.IndexOf(SeparatorChar);

            ReadOnlySpan<char> segment;

            if (separatorIndex < 0) {
                segment = path;
                path = default;
            }
            else {
                segment = path[..separatorIndex];
                path = path[(separatorIndex + 1)..];
            }

            if (segment.Length == 0) {
                if (!options.HasFlag(PathOptions.AllowEmptyDirectories))
                    throw new ArgumentException("Invalid empty directory in path.", nameof(path));
            }
            else if (segment.SequenceEqual(".") || segment.SequenceEqual("..")) {
                if (isFile && path.Length == 0)
                    throw new ArgumentException("File paths cannot end in a navigational path segment.", nameof(path));

                if (options.HasFlag(PathOptions.NoNavigation))
                    throw new ArgumentException("Invalid navigational path segment.", nameof(path));

                if (segment.Length == 2) {
                    if (segments.Count == 0 || segments[^1].Equals("..", StringComparison.Ordinal))
                        segments.Add("..");
                    else
                        segments.RemoveAt(segments.Count - 1);
                }
            }
            else if (!ValidateEntryName(segment, options, false, out string error)) {
                throw new ArgumentException(error, nameof(path));
            }
            else {
                segments.Add(segment.ToString());
            }
        }

        return segments;
    }
}