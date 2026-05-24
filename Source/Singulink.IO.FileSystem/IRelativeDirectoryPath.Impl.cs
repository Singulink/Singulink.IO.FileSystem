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
                {
                    // PathDisplay is a series of "../" navigations; append another parent nav segment.
                    parentPath = PathDisplay + PathFormat.RelativeParentDirectory.PathDisplay;
                }
                else
                {
                    parentPath = PathFormat.GetParentDirectoryPath(PathDisplay, RootLength).ToString();
                }

                return new Impl(parentPath, RootLength, PathFormat);
            }
        }

        public IRelativeDirectoryPath Combine(IRelativeDirectoryPath path) => (IRelativeDirectoryPath)Combine(path, nameof(path));

        public IRelativeDirectoryPath CombineDirectory(ReadOnlySpan<char> path, RelativePathFormat format, PathOptions options)
        {
            var parseFormat = ResolveAppendFormat(format);
            return (IRelativeDirectoryPath)Combine(DirectoryPath.ParseRelative(path, parseFormat, options), nameof(path));
        }

        public IRelativeFilePath Combine(IRelativeFilePath file) => (IRelativeFilePath)Combine((IRelativePath)file);

        public IRelativeFilePath CombineFile(ReadOnlySpan<char> path, RelativePathFormat format, PathOptions options)
        {
            var parseFormat = ResolveAppendFormat(format);
            return (IRelativeFilePath)Combine(FilePath.ParseRelative(path, parseFormat, options), nameof(path));
        }

        public IRelativePath Combine(IRelativePath path) => Combine(path, nameof(path));

        private PathFormat ResolveAppendFormat(RelativePathFormat format) => format switch {
            RelativePathFormat.MatchBase => PathFormat,
            RelativePathFormat.Universal => PathFormat.Universal,
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown relative path format."),
        };

        private IRelativePath Combine(IRelativePath path, string? formatSourceParamName)
        {
            var mutualFormat = PathFormat.GetMutualFormat(PathFormat, path.PathFormat)
                ?? throw new ArgumentException("Cannot combine path formats that are not universal or do not match.", formatSourceParamName);

            if (PathDisplay.Length is 0)
                return path;

            if (path.PathDisplay.Length is 0)
                return this;

            var appendPath = path.PathFormat.SplitRelativeNavigation(path.PathDisplay, out int parentDirNavCount);
            appendPath = PathFormat.ConvertRelativePathToMutualFormat(appendPath, path.PathFormat, mutualFormat);

            StringOrSpan basePath = GetBasePathForAppending(parentDirNavCount) ??
                throw new ArgumentException("Invalid path combination: Attempt to navigate past root directory.", nameof(path));

            basePath = PathFormat.ConvertRelativePathToMutualFormat(basePath, PathFormat, mutualFormat);

            // basePath always ends with a separator (or is empty for the current-dir / rooted-relative cases),
            // so we can append directly to it.
            string newPath;

            if (parentDirNavCount == -1)
                newPath = $"{PathFormat.SeparatorAsString}{appendPath.Span}";
            else if (appendPath.Length is 0)
                newPath = basePath.ToString();
            else
                newPath = $"{basePath.Span}{appendPath.Span}";

            if (path is IDirectoryPath)
                return new Impl(newPath, RootLength, PathFormat);
            else
                return new IRelativeFilePath.Impl(newPath, RootLength, PathFormat);
        }

        private string? GetBasePathForAppending(int parentDirNavCount)
        {
            if (parentDirNavCount is -1)
                return string.Empty;

            if (parentDirNavCount is 0)
                return PathDisplay;

            // Empty current-directory: each parent navigation becomes a leading "../" segment.
            if (PathDisplay.Length is 0)
                return PathFormat.GetRelativeParentNavPath(parentDirNavCount);

            // Walk backward stripping named segments, one per parent navigation, until we hit a ".." segment or the root boundary.
            // Any remaining navigations (after no more named segments are available) are prepended as "../" prefix.
            char separator = PathFormat.Separator;
            int endIndex = PathDisplay.Length; // exclusive end of the kept prefix; PathDisplay[endIndex - 1] is the trailing separator
            int remaining = parentDirNavCount;

            while (remaining > 0 && endIndex > RootLength)
            {
                int prevSep = PathDisplay.AsSpan(0, endIndex - 1).LastIndexOf(separator);
                int segStart = prevSep + 1;
                int segLen = (endIndex - 1) - segStart;

                // ".." segment marks the start of the leading navigation prefix; we cannot strip further, only add more navigation.
                if (segLen is 2 && PathDisplay[segStart] is '.' && PathDisplay[segStart + 1] is '.')
                    break;

                endIndex = segStart;
                remaining--;
            }

            if (remaining > 0 && IsRooted)
                return null;

            string basePath = endIndex == PathDisplay.Length ? PathDisplay : PathDisplay[..endIndex];

            if (remaining > 0)
                return PathFormat.GetRelativeParentNavPath(remaining) + basePath;

            return basePath;
        }

        public override IRelativeDirectoryPath ToPathFormat(PathFormat format, PathOptions options)
        {
            var path = PathFormat.ConvertRelativePathFormat(PathDisplay, PathFormat, format);
            return DirectoryPath.ParseRelative(path.Span, format, options);
        }
    }
}
