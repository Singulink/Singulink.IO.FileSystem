using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Singulink.IO.Json;

namespace Singulink.IO;

/// <content>
/// Contains the implementation of IRelativeFilePath.
/// </content>
public partial interface IRelativeFilePath
{
    [JsonConverter(typeof(RelativeFilePathJsonConverter))]
    internal new sealed class Impl(string path, int rootLength, PathFormat pathFormat, int validity)
        : IRelativePath.Impl(path, rootLength, pathFormat, validity), IRelativeFilePath
    {
        public override bool HasParentDirectory => true;

        public override IRelativeDirectoryPath ParentDirectory
        {
            get {
                var parentPath = PathFormat.GetParentDirectoryPath(PathDisplay, RootLength);
                return new IRelativeDirectoryPath.Impl(parentPath.ToString(), RootLength, PathFormat, InheritedValidity);
            }
        }

        public IRelativeFilePath WithExtension(string? extension, PathOptions options)
        {
            string newPath = PathFormat.ChangeFileNameExtension(PathDisplay, extension, RootLength, options);

            if (newPath is null)
                return this;

            return new Impl(newPath, RootLength, PathFormat, InheritedValidity & PathValidity.FromNewFileName(PathFormat, options));
        }

        public IRelativeFilePath AddExtension(string? extension, PathOptions options) => AddExtensionCore(extension, options, allowMulti: false);

        public IRelativeFilePath AddMultiExtension(string? extension, PathOptions options) => AddExtensionCore(extension, options, allowMulti: true);

        public IRelativeFilePath RemoveExtension(string extension, PathOptions options) => RemoveExtensionCore(extension, options, allowMulti: false);

        public IRelativeFilePath RemoveMultiExtension(string extension, PathOptions options) => RemoveExtensionCore(extension, options, allowMulti: true);

        private Impl AddExtensionCore(string? extension, PathOptions options, bool allowMulti)
        {
            string newPath = PathFormat.AddFileNameExtension(PathDisplay, extension, RootLength, options, allowMulti);

            if (newPath is null)
                return this;

            return new Impl(newPath, RootLength, PathFormat, InheritedValidity & PathValidity.FromNewFileName(PathFormat, options));
        }

        private Impl RemoveExtensionCore(string extension, PathOptions options, bool allowMulti)
        {
            string newPath = PathFormat.RemoveFileNameExtension(PathDisplay, extension, RootLength, options, allowMulti);

            if (newPath is null)
                return this;

            return new Impl(newPath, RootLength, PathFormat, InheritedValidity & PathValidity.FromNewFileName(PathFormat, options));
        }

        public override IRelativeFilePath ToPathFormat(PathFormat format, PathOptions options)
        {
            if (!TryToPathFormat(format, options, out IRelativeFilePath? path, out string? error))
                throw new ArgumentException(error);

            return path;
        }

        internal bool TryToPathFormat(PathFormat format, PathOptions options, [NotNullWhen(true)] out IRelativeFilePath? path, [NotNullWhen(false)] out string? error)
        {
            if (!PathFormat.TryConvertRelativePathFormat(PathDisplay, PathFormat, format, out var convertedPath, out error))
            {
                path = null;
                return false;
            }

            return FilePath.TryParseRelative(convertedPath.Span, format, options, out path, out error);
        }

        internal override bool TryToPathFormat(PathFormat format, PathOptions options, [NotNullWhen(true)] out IRelativePath? path, [NotNullWhen(false)] out string? error)
        {
            bool success = TryToPathFormat(format, options, out IRelativeFilePath? filePath, out error);
            path = filePath;

            return success;
        }

        private protected override bool TryParseAgain(PathOptions options, [NotNullWhen(false)] out string? error)
        {
            return FilePath.TryParseRelative(PathDisplay, PathFormat, options, out _, out error);
        }
    }
}
