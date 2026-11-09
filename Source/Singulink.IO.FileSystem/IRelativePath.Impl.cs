using System.Diagnostics.CodeAnalysis;

namespace Singulink.IO;

/// <content>
/// Contains the implementation of IRelativePath.
/// </content>
public partial interface IRelativePath
{
    internal new abstract class Impl(string path, int rootLength, PathFormat pathFormat, int validity)
        : IPath.Impl(path, rootLength, pathFormat, validity), IRelativePath
    {
        public override abstract IRelativeDirectoryPath? ParentDirectory { get; }

        public abstract IRelativePath ToPathFormat(PathFormat format, PathOptions options = PathOptions.NoUnfriendlyNames);

        /// <summary>
        /// Converts the path to use a different format, like <see cref="ToPathFormat(PathFormat, PathOptions)"/>, but returns <see langword="false"/> with
        /// the reason instead of throwing when the path can't be converted.
        /// </summary>
        internal abstract bool TryToPathFormat(PathFormat format, PathOptions options, [NotNullWhen(true)] out IRelativePath? path, [NotNullWhen(false)] out string? error);
    }
}
