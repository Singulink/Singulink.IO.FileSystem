using Singulink.IO.Utilities;

namespace Singulink.IO;

/// <content>
/// Contains the implementation of IRelativeDirectoryPath.
/// </content>
public partial interface IRelativeDirectoryPath
{
    internal new sealed class Impl(string path, int rootLength, PathFormat pathFormat) : IRelativePath.Impl(path, rootLength, pathFormat), IRelativeDirectoryPath
    {
        public override bool HasParentDirectory => !IsRooted || PathDisplay.Length > RootLength;

        public override IRelativeDirectoryPath? ParentDirectory
        {
            get {
                if (!HasParentDirectory)
                    return null;

                if (PathDisplay.Length is 0)
                    return PathFormat.RelativeParentDirectory;

                string parentPath;

                if (!IsRooted && PathFormat.GetEntryName(PathDisplay, 0).Length is 0)
                    parentPath = $"{PathDisplay}{PathFormat.Separator}..";
                else
                    parentPath = PathFormat.GetParentDirectoryPath(PathDisplay, RootLength).ToString();

                return new Impl(parentPath, RootLength, PathFormat);
            }
        }

        public IRelativeDirectoryPath Combine(IRelativeDirectoryPath dir) => (IRelativeDirectoryPath)Combine(dir, nameof(dir), nameof(dir));

        public IRelativeDirectoryPath CombineDirectory(ReadOnlySpan<char> path, PathFormat format, PathOptions options)
        {
            return (IRelativeDirectoryPath)Combine(DirectoryPath.ParseRelative(path, format, options), nameof(path), nameof(format));
        }

        public IRelativeFilePath Combine(IRelativeFilePath file) => (IRelativeFilePath)Combine((IRelativePath)file);

        public IRelativeFilePath CombineFile(ReadOnlySpan<char> path, PathFormat format, PathOptions options)
        {
            return (IRelativeFilePath)Combine(FilePath.ParseRelative(path, format, options), nameof(path), nameof(format));
        }

        public IRelativePath Combine(IRelativePath entry) => Combine(entry, nameof(entry), nameof(entry));

        private IRelativePath Combine(IRelativePath entry, string? pathParamName, string? formatParamName)
        {
            var mutualFormat = PathFormat.GetMutualFormat(PathFormat, entry.PathFormat);

            if (mutualFormat == null)
                throw new ArgumentException("Cannot combine path formats that are not universal or do not match.", formatParamName);

            if (PathDisplay.Length == 0)
                return entry;

            if (entry.PathDisplay.Length == 0)
                return this;

            var appendPath = entry.PathFormat.SplitRelativeNavigation(entry.PathDisplay, out int parentDirs);
            appendPath = PathFormat.ConvertRelativePathToMutualFormat(appendPath, entry.PathFormat, mutualFormat);

            StringOrSpan basePath = GetBasePathForAppending(parentDirs) ??
                throw new ArgumentException("Invalid path combination: Attempt to navigate past root directory.", pathParamName);

            basePath = PathFormat.ConvertRelativePathToMutualFormat(basePath, PathFormat, mutualFormat);

            string newPath = appendPath.Length > 0 || parentDirs == -1 ?
                $"{basePath.Span}{PathFormat.Separator}{appendPath.Span}" : (string)basePath;

            if (entry is IDirectoryPath)
                return new Impl(newPath, RootLength, PathFormat);
            else
                return new IRelativeFilePath.Impl(newPath, RootLength, PathFormat);
        }

        private string? GetBasePathForAppending(int parentDirs)
        {
            // TODO: Can be optimized so that parent directory instances are not created.

            if (parentDirs == -1)
                return string.Empty;

            IRelativeDirectoryPath currentDir = this;

            for (int i = 0; i < parentDirs; i++)
            {
                currentDir = currentDir.ParentDirectory;

                if (currentDir == null)
                    return null;
            }

            return currentDir.PathDisplay;
        }

        public override IRelativeDirectoryPath ToPathFormat(PathFormat format, PathOptions options)
        {
            var path = PathFormat.ConvertRelativePathFormat(PathDisplay, PathFormat, format);
            return DirectoryPath.ParseRelative(path.Span, format, options);
        }
    }
}
