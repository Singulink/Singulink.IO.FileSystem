namespace Singulink.IO;

/// <content>
/// Contains the implementation of IRelativeFilePath.
/// </content>
public partial interface IRelativeFilePath
{
    internal new sealed class Impl(string path, int rootLength, PathFormat pathFormat) : IRelativePath.Impl(path, rootLength, pathFormat), IRelativeFilePath
    {
        public override bool HasParentDirectory => true;

        public override IRelativeDirectoryPath ParentDirectory
        {
            get {
                var parentPath = PathFormat.GetParentDirectoryPath(PathDisplay, RootLength);
                return new IRelativeDirectoryPath.Impl(parentPath.ToString(), RootLength, PathFormat);
            }
        }

        public IRelativeFilePath WithExtension(string? newExtension, PathOptions options)
        {
            string newPath = PathFormat.ChangeFileNameExtension(PathDisplay, newExtension, options);
            return new Impl(newPath, RootLength, PathFormat);
        }

        public override IRelativeFilePath ToPathFormat(PathFormat format, PathOptions options)
        {
            var path = PathFormat.ConvertRelativePathFormat(PathDisplay, PathFormat, format);
            return FilePath.ParseRelative(path.Span, format, options);
        }
    }
}
