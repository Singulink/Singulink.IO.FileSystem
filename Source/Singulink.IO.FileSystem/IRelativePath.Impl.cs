namespace Singulink.IO;

/// <content>
/// Contains the implementation of IRelativePath.
/// </content>
public partial interface IRelativePath
{
    internal new abstract class Impl(string path, int rootLength, PathFormat pathFormat) : IPath.Impl(path, rootLength, pathFormat), IRelativePath
    {
        public override abstract IRelativeDirectoryPath? ParentDirectory { get; }

        public abstract IRelativePath ToPathFormat(PathFormat format, PathOptions options = PathOptions.NoUnfriendlyNames);
    }
}
