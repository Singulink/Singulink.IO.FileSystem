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

        // The Unix checks are a subset of the Windows checks with the same options, so the Windows options are the ones applied.
        internal override PathOptions GetEffectiveOptions(PathOptions options) => Windows.GetEffectiveOptions(options);

        internal override PathOptions CheckEntryName(ReadOnlySpan<char> name, PathOptions options, bool allowWildcards, bool collect, out string? error, bool wantsError)
        {
            var failed = Unix.CheckEntryName(name, options, allowWildcards, collect, out error, wantsError);

            if (failed is not 0 && !collect)
                return failed;

            return failed | Windows.CheckEntryName(name, options, allowWildcards, collect, out error, wantsError);
        }

        #region Not Supported

        internal override bool IsUncPath(string path) =>
            throw new NotSupportedException("Operation not supported in universal path format.");

        private protected override bool TrySplitAbsoluteRoot(ReadOnlySpan<char> path, out ReadOnlySpan<char> root, out ReadOnlySpan<char> rest, [NotNullWhen(false)] out string? error) =>
            throw new NotSupportedException("Operation not supported in universal path format.");

        internal override string GetAbsolutePathExportString(string pathDisplay) =>
            throw new NotSupportedException("Operation not supported in universal path format.");

        #endregion

        public override string ToString() => "Universal";
    }
}
