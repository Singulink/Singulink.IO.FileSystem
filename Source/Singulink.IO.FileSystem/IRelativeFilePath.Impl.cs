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

        public IRelativeFilePath WithExtension(string? extension, PathOptions options)
        {
            string newPath = PathFormat.ChangeFileNameExtension(PathDisplay, extension, RootLength, options);

            if (newPath is null)
                return this;

            return new Impl(newPath, RootLength, PathFormat);
        }

        public IRelativeFilePath AddExtension(string? extension, PathOptions options)
        {
            string newPath = PathFormat.AddFileNameExtension(PathDisplay, extension, RootLength, options);

            if (newPath is null)
                return this;

            return new Impl(newPath, RootLength, PathFormat);
        }

        public override IRelativeFilePath ToPathFormat(PathFormat format, PathOptions options)
        {
            var path = PathFormat.ConvertRelativePathFormat(PathDisplay, PathFormat, format);
            return FilePath.ParseRelative(path.Span, format, options);
        }
    }
}
