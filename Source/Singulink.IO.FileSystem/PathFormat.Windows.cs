using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Singulink.Enums;

namespace Singulink.IO;

/// <content>
/// Contains formatting for the Windows path format.
/// </content>
public abstract partial class PathFormat
{
    private sealed class WindowsPathFormat : PathFormat
    {
        private static readonly FrozenSet<char> InvalidNameChars = GetInvalidNameChars(true);
        private static readonly FrozenSet<char> InvalidNameCharsWithoutWildcards = GetInvalidNameChars(false);

        internal WindowsPathFormat() : base('\\') { }

        public override bool SupportsRelativeRootedPaths => true;

        internal override ReadOnlySpan<char> NormalizeSeparators(ReadOnlySpan<char> path)
        {
            const char AltPathSeparatorChar = '/';

            int altSeparatorIndex = path.IndexOf(AltPathSeparatorChar);

            if (altSeparatorIndex < 0)
                return path;

            char[] normalizedPath = path.ToArray();

            for (int i = altSeparatorIndex; i < normalizedPath.Length; i++)
            {
                if (normalizedPath[i] == AltPathSeparatorChar)
                    normalizedPath[i] = Separator;
            }

            return normalizedPath;
        }

        internal override PathKind GetPathKind(ReadOnlySpan<char> path)
        {
            if (path.Length >= 2)
            {
                if (path[1] is ':' || path.StartsWith(@"\\") || path.StartsWith("//"))
                    return PathKind.Absolute;
            }

            return path.Length > 0 && path[0] == Separator ? PathKind.RelativeRooted : PathKind.Relative;
        }

        internal override bool ValidateEntryName(ReadOnlySpan<char> name, PathOptions options, bool allowWildcards, [NotNullWhen(false)] out string? error)
        {
            options = options.SetFlags(PathOptions.NoControlCharacters);

            if (options.HasAllFlags(PathOptions.PathFormatDependent))
                options = options.SetFlags(PathOptions.NoUnfriendlyNames);

            if (!base.ValidateEntryName(name, options, allowWildcards, out error))
                return false;

            if (allowWildcards)
            {
                foreach (char c in name)
                {
                    if (InvalidNameCharsWithoutWildcards.Contains(c))
                    {
                        error = $"Invalid character '{c}' in entry name '{name.ToString()}'. Invalid characters include: < > : \" | / \\";
                        return false;
                    }
                }
            }
            else
            {
                foreach (char c in name)
                {
                    if (InvalidNameChars.Contains(c))
                    {
                        error = $"Invalid character '{c}' in entry name '{name.ToString()}'. Invalid characters include: < > : \" | ? * / \\";
                        return false;
                    }
                }
            }

            if (options.HasAllFlags(PathOptions.NoReservedDeviceNames))
            {
                const StringComparison comp = StringComparison.OrdinalIgnoreCase;

                // File name without extension also cannot match a reserved device name.

                if (name.IndexOf('.') is int nameLength && nameLength >= 0)
                    name = name[..nameLength];

                // Reserved device names:
                // CON, PRN, AUX, NUL, COM1 to COM9, LPT1 to LPT9

                if ((name.Length == 3 && (name.Equals("CON", comp) || name.Equals("PRN", comp) || name.Equals("AUX", comp) || name.Equals("NUL", comp))) ||
                    (name.Length == 4 && char.IsDigit(name[3]) && (name.StartsWith("COM", comp) || name.StartsWith("LPT", comp))))
                {
                    error = $"Invalid reserved device name in entry name '{name.ToString()}'.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        internal override bool IsUncPath(string absoluteDisplayPath) => absoluteDisplayPath[1] != ':';

        private protected override ReadOnlySpan<char> SplitAbsoluteRoot(ReadOnlySpan<char> path, out ReadOnlySpan<char> rest)
        {
            if (path.StartsWith(@"\\?\") || path.StartsWith(@"\\.\"))
            {
                path = path[4..];

                if (path.StartsWith(@"UNC\"))
                    path = $@"\\{path[4..]}";
            }

            ReadOnlySpan<char> root;
            int firstIndex = path.IndexOf(Separator);

            if (firstIndex == 0)
            {
                if (path.Length < 5 || path[1] != Separator)
                    ThrowInvalidPathRoot();

                int serverLength = path[2..].IndexOf(Separator);
                int shareStart = 3 + serverLength;

                if (serverLength <= 0 || path.Length <= shareStart)
                    ThrowInvalidPathRoot();

                var server = path.Slice(2, serverLength);

                if (!IsValidServerName(server))
                    throw new ArgumentException("Invalid UNC server name.", nameof(path));

                int shareLength = path[shareStart..].IndexOf(Separator);

                ReadOnlySpan<char> share;

                if (shareLength <= 0)
                {
                    root = $"{path}{Separator}";
                    rest = default;
                    share = path[shareStart..];
                }
                else
                {
                    root = path[..(shareStart + shareLength + 1)];
                    rest = path[root.Length..];
                    share = path.Slice(shareStart, shareLength);
                }

                // Share names can contain trailing dots but no leading or trailing spaces. Reserved device names do not apply to the share name.

                if (!ValidateEntryName(share, PathOptions.NoLeadingSpaces | PathOptions.NoTrailingSpaces, false, out string error))
                    throw new ArgumentException($"Invalid UNC share name: {error}");
            }
            else
            {
                if (path.Length < 2 || (path.Length >= 3 && firstIndex is not 2) || (char.ToUpperInvariant(path[0]) is < 'A' or > 'Z') || path[1] is not ':')
                    ThrowInvalidPathRoot();

                if (path.Length is 2)
                {
                    root = $"{path}{Separator}";
                    rest = default;
                }
                else
                {
                    root = path[..3];
                    rest = path[3..];
                }
            }

            return root;

            static bool IsValidServerName(ReadOnlySpan<char> server)
            {
                // Server name can be any valid hostname

                if (server[0] == '.' || server[^1] == '.' || server.IndexOf("..", StringComparison.Ordinal) >= 0)
                    return false;

                foreach (char c in server)
                {
                    if (!char.IsLetter(c) && !char.IsDigit(c) && c != '.' && c != '-')
                        return false;
                }

                return true;
            }

            static void ThrowInvalidPathRoot() => throw new ArgumentException("Invalid absolute path root.", nameof(path));
        }

        internal override string GetAbsolutePathExportString(string pathDisplay) =>
            pathDisplay[1] == ':' ? @"\\?\" + pathDisplay : $@"\\?\UNC\{pathDisplay.AsSpan()[2..]}";

        private static FrozenSet<char> GetInvalidNameChars(bool includeWildcardChars)
        {
            var invalidChars = new List<char>() { '<', '>', ':', '"', '|', '/', '\\' };

            if (includeWildcardChars)
            {
                invalidChars.Add('?');
                invalidChars.Add('*');
            }

            return invalidChars.ToFrozenSet();
        }

        public override string ToString() => "Windows";
    }
}
