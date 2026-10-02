using System.Diagnostics.CodeAnalysis;

namespace Singulink.IO;

/// <content>
/// Contains formatting for the Unix path format.
/// </content>
public abstract partial class PathFormat
{
    private sealed class UnixPathFormat : PathFormat
    {
        internal UnixPathFormat() : base('/') { }

        public override bool SupportsRelativeRootedPaths => false;

        internal override PathKind GetPathKind(ReadOnlySpan<char> path)
        {
            return path.Length > 0 && path[0] == Separator ? PathKind.Absolute : PathKind.Relative;
        }

        internal override bool IsUncPath(string path) => false;

        private protected override bool TrySplitAbsoluteRoot(ReadOnlySpan<char> path, out ReadOnlySpan<char> root, out ReadOnlySpan<char> rest, [NotNullWhen(false)] out string? error)
        {
            root = path[0..1];
            rest = path[1..];
            error = null;
            return true;
        }

        internal override string GetAbsolutePathExportString(string pathDisplay) => pathDisplay;

        public override string ToString() => "Unix";
    }
}
