using System;
using System.Diagnostics.CodeAnalysis;

namespace Singulink.IO
{
    /// <content>
    /// Contains formatting for the unix path format.
    /// </content>
    public abstract partial class PathFormat
    {
        private sealed class UnixPathFormat : PathFormat
        {
            internal UnixPathFormat() : base('/') { }

            public override bool SupportsRelativeRootedPaths => false;

            internal override PathKind GetPathKind(ReadOnlySpan<char> path)
            {
                return path.Length > 0 && path[0] == SeparatorChar ? PathKind.Absolute : PathKind.Relative;
            }

            internal override bool ValidateEntryName(ReadOnlySpan<char> name, PathOptions options, bool allowWildcards, [NotNullWhen(false)] out string? error)
            {
                if (!base.ValidateEntryName(name, options, allowWildcards, out error))
                    return false;

                if (name.IndexOfAny('/', (char)0) is int i && i >= 0) {
                    error = $"Invalid character in path segment '{name.ToString()}' ({name[i]}).";
                    return false;
                }

                error = null;
                return true;
            }

            internal override ReadOnlySpan<char> SplitAbsoluteRoot(ReadOnlySpan<char> path, out ReadOnlySpan<char> rest)
            {
                var root = path[0..1];
                rest = path[1..];
                return root;
            }

            internal override string GetAbsolutePathExportString(string pathDisplay) => pathDisplay;

            public override string ToString() => "Unix";

            #region Not Supported

            internal override bool IsUncPath(string path) => throw new NotSupportedException();

            #endregion
        }
    }
}