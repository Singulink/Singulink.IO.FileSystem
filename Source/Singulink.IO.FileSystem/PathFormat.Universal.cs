using System;
using System.Diagnostics.CodeAnalysis;

namespace Singulink.IO;

/// <content>
/// Contains formatting for the universal path format.
/// </content>
public abstract partial class PathFormat
{
    private sealed class UniversalPathFormat : PathFormat
    {
        internal UniversalPathFormat() : base('/') { }

        public override bool SupportsRelativeRootedPaths => false;

        internal override PathKind GetPathKind(ReadOnlySpan<char> path) => PathKind.Relative;

        internal override bool ValidateEntryName(ReadOnlySpan<char> name, PathOptions options, bool allowWildcards, [NotNullWhen(false)] out string? error)
        {
            if (!Unix.ValidateEntryName(name, options, allowWildcards, out error))
                return false;

            if (!Windows.ValidateEntryName(name, options, allowWildcards, out error))
                return false;

            error = null;
            return true;
        }

        #region Not Supported

        internal override bool IsUncPath(string path) => throw new NotSupportedException();

        protected override ReadOnlySpan<char> SplitAbsoluteRoot(ReadOnlySpan<char> path, out ReadOnlySpan<char> rest) => throw new NotSupportedException();

        internal override string GetAbsolutePathExportString(string pathDisplay) => throw new NotSupportedException();

        #endregion

        public override string ToString() => "Universal";
    }
}