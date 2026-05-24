using System.Diagnostics;

namespace Singulink.IO;

/// <content>
/// Contains the implementation of IAbsoluteDirectoryPath.
/// </content>
public partial interface IAbsoluteDirectoryPath
{
    internal new sealed class Impl(string path, int rootLength, PathFormat pathFormat) : IAbsolutePath.Impl(path, rootLength, pathFormat), IAbsoluteDirectoryPath
    {
        private static readonly EnumerationOptions CheckAccessEnumerationOptions = new() { IgnoreInaccessible = false };

        public override bool Exists
        {
            get {
                PathFormat.EnsureCurrent();
                return Directory.Exists(PathExport); // Only returns true for actual dirs, not files
            }
        }

        public override EntryState State
        {
            get {
                PathFormat.EnsureCurrent();

                try
                {
                    return File.GetAttributes(PathExport).HasAllFlags(FileAttributes.Directory) ? EntryState.Exists : EntryState.WrongType;
                }
                catch (FileNotFoundException)
                {
                    return EntryState.ParentExists;
                }
                catch (DirectoryNotFoundException)
                {
                    return EntryState.ParentDoesNotExist;
                }
                catch (UnauthorizedAccessException ex)
                {
                    throw Ex.Convert(ex);
                }
            }
        }

        public bool IsEmpty
        {
            get {
                PathFormat.EnsureCurrent();

                try
                {
                    return Directory.EnumerateFileSystemEntries(PathExport).Any();
                }
                catch (IOException ex) when (ex.GetType() == typeof(IOException))
                {
                    ThrowNotFoundIfDirIsFile(this);
                    throw;
                }
                catch (UnauthorizedAccessException ex)
                {
                    throw Ex.Convert(ex);
                }
            }
        }

        public bool IsRoot => PathDisplay.Length == RootLength;

        public override IAbsoluteDirectoryPath? ParentDirectory
        {
            get {
                if (!HasParentDirectory)
                    return null;

                var parentPath = PathFormat.GetParentDirectoryPath(PathDisplay, RootLength);
                return new Impl(parentPath.ToString(), RootLength, PathFormat);
            }
        }

        public override bool HasParentDirectory => !IsRoot;

        public override FileAttributes Attributes
        {
            get {
                PathFormat.EnsureCurrent();

                FileAttributes attributes;

                try
                {
                    attributes = File.GetAttributes(PathExport);
                }
                catch (FileNotFoundException)
                {
                    attributes = 0;
                }
                catch (UnauthorizedAccessException ex)
                {
                    throw Ex.Convert(ex);
                }

                if (!attributes.HasAllFlags(FileAttributes.Directory))
                    throw Ex.NotFound(this);

                return attributes;
            }
            set {
                var current = Attributes; // Ensures that this is a directory

                if (current != value)
                    File.SetAttributes(PathExport, value);
            }
        }

        public DriveType DriveType
        {
            get {
                PathFormat.EnsureCurrent();
                EnsureExists();

                DriveType type;

                if (PathFormat == PathFormat.Windows)
                {
                    type = IsUnc ? DriveType.Network : Interop.Windows.GetDriveType(this);
                }
                else
                {
                    try
                    {
                        type = new DriveInfo(PathDisplay).DriveType;
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        throw Ex.Convert(ex);
                    }
                }

                if (type is DriveType.NoRootDirectory)
                    throw Ex.NotFound(this);

                return type;
            }
        }

        public string FileSystem
        {
            get {
                PathFormat.EnsureCurrent();

                if (PathFormat == PathFormat.Windows)
                {
                    // Get last dir that is either the root or a reparse point

                    var dir = this;

                    while (!dir.IsRoot && (dir.Attributes & FileAttributes.ReparsePoint) == 0)
                        dir = (Impl)dir.ParentDirectory!;

                    return Interop.Windows.GetFileSystem(dir);
                }
                else
                {
                    EnsureExists();

                    try
                    {
                        return new DriveInfo(PathDisplay).DriveFormat;
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        throw Ex.Convert(ex);
                    }
                }
            }
        }

        public long AvailableFreeSpace
        {
            get {
                PathFormat.EnsureCurrent();

                if (PathFormat == PathFormat.Windows)
                {
                    Interop.Windows.GetSpace(this, out long available, out _, out _);
                    return available;
                }
                else
                {
                    EnsureExists();

                    try
                    {
                        return new DriveInfo(PathExport).AvailableFreeSpace;
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        throw Ex.Convert(ex);
                    }
                }
            }
        }

        public long TotalFreeSpace
        {
            get {
                PathFormat.EnsureCurrent();

                if (PathFormat == PathFormat.Windows)
                {
                    Interop.Windows.GetSpace(this, out _, out _, out long totalFree);
                    return totalFree;
                }

                EnsureExists();

                try
                {
                    return new DriveInfo(PathExport).TotalFreeSpace;
                }
                catch (UnauthorizedAccessException ex)
                {
                    throw Ex.Convert(ex);
                }
            }
        }

        public long TotalSize
        {
            get {
                PathFormat.EnsureCurrent();

                if (PathFormat == PathFormat.Windows)
                {
                    Interop.Windows.GetSpace(this, out _, out long totalSize, out _);
                    return totalSize;
                }

                EnsureExists();

                try
                {
                    return new DriveInfo(PathExport).TotalSize;
                }
                catch (UnauthorizedAccessException ex)
                {
                    throw Ex.Convert(ex);
                }
            }
        }

        #region Combine

        public IAbsoluteDirectoryPath Combine(IRelativeDirectoryPath path) => (IAbsoluteDirectoryPath)Combine(path, nameof(path));

        public IAbsoluteDirectoryPath CombineDirectory(ReadOnlySpan<char> path, RelativePathFormat format, PathOptions options)
        {
            var parseFormat = ResolveAppendFormat(format);
            return (IAbsoluteDirectoryPath)Combine(DirectoryPath.ParseRelative(path, parseFormat, options), nameof(path));
        }

        public IAbsoluteFilePath Combine(IRelativeFilePath path) => (IAbsoluteFilePath)Combine(path, nameof(path));

        public IAbsoluteFilePath CombineFile(ReadOnlySpan<char> path, RelativePathFormat format, PathOptions options)
        {
            var parseFormat = ResolveAppendFormat(format);
            return (IAbsoluteFilePath)Combine(FilePath.ParseRelative(path, parseFormat, options), nameof(path));
        }

        public IAbsolutePath Combine(IRelativePath path) => Combine(path, nameof(path));

        private PathFormat ResolveAppendFormat(RelativePathFormat format) => format switch {
            RelativePathFormat.MatchBase => PathFormat,
            RelativePathFormat.Universal => PathFormat.Universal,
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown relative path format."),
        };

        private IAbsolutePath Combine(IRelativePath path, string? formatSourceParamName)
        {
            if (PathFormat.GetMutualFormat(PathFormat, path.PathFormat) != PathFormat)
                throw new ArgumentException($"The provided path's format is not universal and does not match the '{PathFormat}' base dir format.", formatSourceParamName);

            var appendPath = path.PathFormat.SplitRelativeNavigation(path.PathDisplay, out int parentDirNavCount);
            appendPath = PathFormat.ConvertRelativePathToMutualFormat(appendPath, path.PathFormat, PathFormat);

            string basePath = GetBasePathForAppending(parentDirNavCount) ??
                throw new ArgumentException("Invalid dir combination: Attempt to navigate past root directory.", nameof(path));

            // basePath always ends with a path separator under the directory-path invariant, so we can append directly.
            string newPath = basePath + appendPath;

            if (path is IDirectoryPath)
                return new Impl(newPath, RootLength, PathFormat);
            else
                return new IAbsoluteFilePath.Impl(newPath, RootLength, PathFormat);
        }

        private string? GetBasePathForAppending(int parentDirNavCount)
        {
            if (parentDirNavCount is -1)
                return PathDisplay[..RootLength];

            if (parentDirNavCount is 0)
                return PathDisplay;

            // Directory paths always end with a separator (directory-path invariant), so PathDisplay.Length - 1 points at the trailing separator.
            // Walk backward, finding the previous separator at each parent navigation step.
            char separator = PathFormat.Separator;
            int index = PathDisplay.Length - 1;

            for (int i = 0; i < parentDirNavCount; i++)
            {
                index = PathDisplay.LastIndexOf(separator, index - 1);

                if (index < RootLength - 1)
                    return null;
            }

            return PathDisplay[..(index + 1)];
        }

        #endregion

        #region File System Operations

        public override CachedDirectoryInfo GetInfo()
        {
            PathFormat.EnsureCurrent();
            return new CachedDirectoryInfo(new DirectoryInfo(PathExport), this);
        }

        public CachedEntryInfo GetInfo(ReadOnlySpan<char> path, RelativePathFormat format, PathOptions options)
        {
            PathFormat.EnsureCurrent();

            var parseFormat = ResolveAppendFormat(format);
            var normalized = parseFormat.NormalizeSeparators(path);

            if (parseFormat.IsDirectoryShaped(normalized))
            {
                var relativeDir = DirectoryPath.ParseRelative(path, parseFormat, options);
                return CachedEntryInfo.CreateFromKnownDirectory(Combine(relativeDir));
            }

            var relativeFile = FilePath.ParseRelative(path, parseFormat, options);
            return CachedEntryInfo.CreateFromAmbiguousFile(Combine(relativeFile));
        }

        public void Create()
        {
            PathFormat.EnsureCurrent();

            try
            {
                Directory.CreateDirectory(PathExport);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw Ex.Convert(ex);
            }
        }

        public void Delete(bool recursive = false, bool ignoreNotFound = true)
        {
            PathFormat.EnsureCurrent();

            try
            {
                // If path points to file then IOEx is thrown with nonsense message on Windows and DirNotFoundEx on Unix. Always throw IOEx.
                Directory.Delete(PathExport, recursive);
            }
            catch (IOException ex) when (PathFormat == PathFormat.Windows && ex.GetType() == typeof(IOException))
            {
                ThrowIfDirIsFile(this); // Get a better error message on windows if path points to a file
                throw;
            }
            catch (DirectoryNotFoundException)
            {
                // On Unix, this means either the directory does not exist or the path points to a file, so we check if it is a file and throw IOex if we
                // should.

                if (PathFormat == PathFormat.Unix)
                    ThrowIfDirIsFile(this);

                // Dir wasn't a file, so it means the directory does not exist.
                // If ignoreNotFound is false or the parent directory does not exist, rethrow the exception.
                // TODO: avoid the extra call if/when this available: https://github.com/dotnet/runtime/issues/117853

                if (ignoreNotFound && State == EntryState.ParentExists)
                    return;

                throw;
            }
            catch (UnauthorizedAccessException ex)
            {
                throw Ex.Convert(ex);
            }
        }

        public void MoveTo(IAbsoluteDirectoryPath destinationDir)
        {
            PathFormat.EnsureCurrent();
            destinationDir.PathFormat.EnsureCurrent(nameof(destinationDir));

            try
            {
                // Directory.Move works for both files and dirs so check type first.

                ThrowIfDirIsFile(this);
                Directory.Move(PathExport, destinationDir.PathExport);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw Ex.Convert(ex);
            }
        }

        public override IAbsoluteDirectoryPath GetLastExistingDirectory()
        {
            // PathFormat.EnsureCurrent() is called by Exists so no need to do it again here.

            IAbsoluteDirectoryPath lastExistingDir = null;

            // Start at the root to prevent very slow repeat faulted network accesses starting from the last dir.

            foreach (var dir in GetPathsFromDirToRoot(this).Reverse())
            {
                if (!dir.Exists)
                    break;

                lastExistingDir = dir;
            }

            if (lastExistingDir is null)
                throw Ex.NotFound(RootDirectory);

            return lastExistingDir;

            static IEnumerable<IAbsoluteDirectoryPath> GetPathsFromDirToRoot(IAbsoluteDirectoryPath path)
            {
                while (true)
                {
                    yield return path;

                    if (path.IsRoot)
                        yield break;

                    path = path.ParentDirectory!;
                }
            }
        }

        internal override void EnsureExists()
        {
            if (!Directory.Exists(PathExport))
                throw Ex.NotFound(this);
        }

        private static void ThrowIfDirIsFile(IAbsoluteDirectoryPath dir)
        {
            if (IsKnownToBeFile(dir))
                throw Ex.DirIsFile(dir);
        }

        private static void ThrowNotFoundIfDirIsFile(IAbsoluteDirectoryPath dir)
        {
            if (IsKnownToBeFile(dir))
                throw Ex.NotFound(dir);
        }

        private static bool IsKnownToBeFile(IAbsoluteDirectoryPath dir)
        {
            try
            {
                return dir.State is EntryState.WrongType;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Enumeration

        public IEnumerable<CachedDirectoryInfo> GetChildDirectoriesInfo(string searchPattern, SearchOptions? options = null) =>
            GetChildEntriesInfo<CachedDirectoryInfo>(searchPattern, options);

        public IEnumerable<CachedFileInfo> GetChildFilesInfo(string searchPattern, SearchOptions? options = null) =>
            GetChildEntriesInfo<CachedFileInfo>(searchPattern, options);

        public IEnumerable<CachedEntryInfo> GetChildEntriesInfo(string searchPattern, SearchOptions? options = null) =>
            GetChildEntriesInfo<CachedEntryInfo>(searchPattern, options);

        public IEnumerable<IAbsoluteDirectoryPath> GetChildDirectories(string searchPattern, SearchOptions? options) =>
            GetChildEntries<IAbsoluteDirectoryPath>(searchPattern, options);

        public IEnumerable<IAbsoluteFilePath> GetChildFiles(string searchPattern, SearchOptions? options) =>
            GetChildEntries<IAbsoluteFilePath>(searchPattern, options);

        public IEnumerable<IAbsolutePath> GetChildEntries(string searchPattern, SearchOptions? options) =>
            GetChildEntries<IAbsolutePath>(searchPattern, options);

        public IEnumerable<IRelativeDirectoryPath> GetRelativeChildDirectories(string searchPattern, SearchOptions? options) =>
            GetRelativeChildEntries<IRelativeDirectoryPath>(searchPattern, options);

        public IEnumerable<IRelativeFilePath> GetRelativeChildFiles(string searchPattern, SearchOptions? options) =>
            GetRelativeChildEntries<IRelativeFilePath>(searchPattern, options);

        public IEnumerable<IRelativePath> GetRelativeChildEntries(string searchPattern, SearchOptions? options) =>
            GetRelativeChildEntries<IRelativePath>(searchPattern, options);

        public IEnumerable<IRelativeDirectoryPath> GetRelativeDirectories(IRelativeDirectoryPath searchLocation, string searchPattern, SearchOptions? options) =>
            GetRelativeEntries<IRelativeDirectoryPath>(searchLocation, searchPattern, options);

        public IEnumerable<IRelativeFilePath> GetRelativeFiles(IRelativeDirectoryPath searchLocation, string searchPattern, SearchOptions? options) =>
            GetRelativeEntries<IRelativeFilePath>(searchLocation, searchPattern, options);

        public IEnumerable<IRelativePath> GetRelativeEntries(IRelativeDirectoryPath searchLocation, string searchPattern, SearchOptions? options) =>
            GetRelativeEntries<IRelativePath>(searchLocation, searchPattern, options);

        private IEnumerable<TEntryInfo> GetChildEntriesInfo<TEntryInfo>(string searchPattern, SearchOptions? options)
            where TEntryInfo : CachedEntryInfo
        {
            foreach (var entryInfo in EnumerateEntries(searchPattern, options, GetFilterForInfoType<TEntryInfo>()).AsEnumerable())
            {
                // PathExport already ends with a path separator under the directory-path invariant, so the remainder of FullName
                // is the child entry's relative path without a leading separator.
                var relativePath = entryInfo.FullName.AsSpan()[PathExport.Length..];

                if (entryInfo is DirectoryInfo dirInfo)
                {
                    string resultPath = $"{PathDisplay}{relativePath}{PathFormat.SeparatorAsString}";
                    var dir = new IAbsoluteDirectoryPath.Impl(resultPath, RootLength, PathFormat);
                    yield return (TEntryInfo)(object)new CachedDirectoryInfo(dirInfo, dir);
                }
                else if (entryInfo is FileInfo fileInfo)
                {
                    string resultPath = $"{PathDisplay}{relativePath}";
                    var file = new IAbsoluteFilePath.Impl(resultPath, RootLength, PathFormat);
                    yield return (TEntryInfo)(object)new CachedFileInfo(fileInfo, file);
                }
            }
        }

        private IEnumerable<TEntry> GetChildEntries<TEntry>(string searchPattern, SearchOptions? options)
            where TEntry : IAbsolutePath
        {
            foreach (var entryInfo in EnumerateEntries(searchPattern, options, GetFilterForPathType<TEntry>()).AsEnumerable())
            {
                var relativePath = entryInfo.FullName.AsSpan()[PathExport.Length..];

                if (entryInfo is DirectoryInfo)
                {
                    string resultPath = $"{PathDisplay}{relativePath}{PathFormat.SeparatorAsString}";
                    yield return (TEntry)(object)new IAbsoluteDirectoryPath.Impl(resultPath, RootLength, PathFormat);
                }
                else if (entryInfo is FileInfo)
                {
                    string resultPath = $"{PathDisplay}{relativePath}";
                    yield return (TEntry)(object)new IAbsoluteFilePath.Impl(resultPath, RootLength, PathFormat);
                }
            }
        }

        private IEnumerable<TEntry> GetRelativeChildEntries<TEntry>(string searchPattern, SearchOptions? options)
            where TEntry : IRelativePath
        {
            foreach (var entryInfo in EnumerateEntries(searchPattern, options, GetFilterForPathType<TEntry>()).AsEnumerable())
            {
                // PathExport ends with a separator, so the remainder of FullName is the child's name without a leading separator.
                string relativePath = entryInfo.FullName[PathExport.Length..];

                if (entryInfo is DirectoryInfo)
                    yield return (TEntry)(object)new IRelativeDirectoryPath.Impl(relativePath + PathFormat.SeparatorAsString, 0, PathFormat);
                else if (entryInfo is FileInfo)
                    yield return (TEntry)(object)new IRelativeFilePath.Impl(relativePath, 0, PathFormat);
            }
        }

        private IEnumerable<TEntry> GetRelativeEntries<TEntry>(IRelativeDirectoryPath searchLocation, string searchPattern, SearchOptions? options)
            where TEntry : IRelativePath
        {
            var searchDir = (Impl)Combine(searchLocation);
            string[] matchDirs;
            string prefix;

            if (searchLocation.IsRooted)
            {
                // Rooted-relative searchLocation: express results as relative-nav from `this` (consistent with how "/" alone is handled).
                // If the caller wants results expressed from the root they can use `dir.RootDirectory.GetRelativeChildEntries(...)` instead.
                (matchDirs, prefix) = ComputeRootedSearchLocationPrefix(searchLocation);
            }
            else if (searchLocation.PathDisplay.Length > 0 && searchLocation.Name.Length is 0)
            {
                // Series of "../" navigating to a parent directory.
                searchLocation.PathFormat.SplitRelativeNavigation(searchLocation.PathDisplay, out int parentDirNavCount);

                matchDirs = [.. GetDirNamesFromLeafToRoot(this).Take(parentDirNavCount).Reverse()];
                prefix = searchLocation.PathDisplay;
            }
            else
            {
                matchDirs = [];
                prefix = searchLocation.PathDisplay;
            }

            foreach (var entry in searchDir.GetRelativeChildEntries<TEntry>(searchPattern, options))
            {
                StringOrSpan currentPrefix = prefix;
                StringOrSpan entryPath = entry.PathDisplay;

                foreach (string matchDir in matchDirs)
                {
                    var firstEntryName = PathFormat.GetFirstEntry(entryPath);

                    // Only strip when the matched entry is followed by a separator (i.e. has additional content after it). Otherwise the entry IS the
                    // matched name (a file that coincidentally shares a parent dir name) and must remain prefixed.
                    if (firstEntryName.Length < entryPath.Length && firstEntryName.Span.SequenceEqual(matchDir))
                    {
                        currentPrefix = currentPrefix.Span[3..];
                        entryPath = entryPath.Span[(firstEntryName.Length + 1)..];
                    }
                    else
                    {
                        break;
                    }
                }

                string finalPath = currentPrefix + entryPath;

                if (entry is IFilePath)
                    yield return (TEntry)(object)new IRelativeFilePath.Impl(finalPath, 0, PathFormat);
                else
                    yield return (TEntry)(object)new IRelativeDirectoryPath.Impl(finalPath, 0, PathFormat);
            }
        }

        /// <summary>
        /// Computes the matchDirs/prefix pair for a rooted-relative searchLocation by finding the LCA between its named segments and this
        /// directory's ancestor chain. The prefix expresses the path from this directory to the search location's directory as "../"-style
        /// navigation; matchDirs are populated only when the search directory is an ancestor of this directory (so the consumer's matchDirs
        /// loop can collapse the "../" prefix against entries that traverse back into this directory's ancestry).
        /// </summary>
        private (string[] MatchDirs, string Prefix) ComputeRootedSearchLocationPrefix(IRelativeDirectoryPath searchLocation)
        {
            Debug.Assert(searchLocation.IsRooted, "Expected rooted search location.");
            string[] thisChain = [.. GetDirNamesFromLeafToRoot(this).Reverse()];

            StringOrSpan rem = searchLocation.PathDisplay.AsSpan(1); // skip the leading rooted separator
            int k = 0;
            int remainingSegsStart = 1;

            while (rem.Length > 0 && k < thisChain.Length)
            {
                var first = searchLocation.PathFormat.GetFirstEntry(rem);

                if (!first.Span.SequenceEqual(thisChain[k]))
                    break;

                k++;
                int consumed = first.Length + 1; // segment + its trailing separator (guaranteed by dir-path invariant)
                remainingSegsStart += consumed;
                rem = rem.Span[consumed..];
            }

            string nav = PathFormat.GetRelativeParentNavPath(thisChain.Length - k);

            if (rem.Length is 0)
            {
                // All searchLocation segments matched: searchDir is this dir itself or one of its ancestors. The matchDirs loop will
                // collapse the "../" prefix against the remaining ancestor chain.
                return ([.. thisChain.AsSpan(k)], nav);
            }

            // searchLocation descends into a branch that diverges from this dir; no shared ancestry to collapse.
            return ([], nav + searchLocation.PathDisplay[remainingSegsStart..]);
        }

        private static IEnumerable<string> GetDirNamesFromLeafToRoot(Impl path)
        {
            while (!path.IsRoot)
            {
                yield return path.Name;
                path = (Impl)path.ParentDirectory!;
            }
        }

        private IEnumerator<FileSystemInfo> EnumerateEntries(string searchPattern, SearchOptions? options, EntryTypeFilter entryType)
        {
            PathFormat.EnsureCurrent();
            PathFormat.ValidateSearchPattern(searchPattern, nameof(searchPattern));

            using var enumerator = InnerEnumerateEntries(out bool ensureSearchDirAccess);
            bool yielded = false;

            while (true)
            {
                FileSystemInfo entryInfo = null;

                try
                {
                    if (enumerator.MoveNext())
                        entryInfo = enumerator.Current;
                }
                catch (UnauthorizedAccessException ex)
                {
                    throw Ex.Convert(ex);
                }

                if (entryInfo is null)
                    break;

                yield return entryInfo;
                yielded = true;
            }

            if (!yielded && ensureSearchDirAccess)
            {
                try
                {
                    // Create dummy enumerator to check access to the directory.
                    using var e = Directory.EnumerateFileSystemEntries(PathExport, "*", CheckAccessEnumerationOptions).GetEnumerator();
                }
                catch (UnauthorizedAccessException ex)
                {
                    throw Ex.Convert(ex);
                }
            }

            IEnumerator<FileSystemInfo> InnerEnumerateEntries(out bool requiresExtraAccessCheck)
            {
                var dirInfo = new DirectoryInfo(PathExport);
                var enumerationOptions = SearchOptions.ToEnumerationOptions(options, out requiresExtraAccessCheck);

                try
                {
                    return entryType switch {
                        EntryTypeFilter.Files => dirInfo.EnumerateFiles(searchPattern, enumerationOptions).GetEnumerator(),
                        EntryTypeFilter.Directories => dirInfo.EnumerateDirectories(searchPattern, enumerationOptions).GetEnumerator(),
                        _ => dirInfo.EnumerateFileSystemInfos(searchPattern, enumerationOptions).GetEnumerator(),
                    };
                }
                catch (DirectoryNotFoundException) when (PathFormat == PathFormat.Unix)
                {
                    // GetEnumerator() throws IOEx if dir is a file on windows, DirectoryNotFoundEx on Unix. Convert to IOEx.

                    ThrowIfDirIsFile(this);
                    throw;
                }
                catch (UnauthorizedAccessException ex)
                {
                    throw Ex.Convert(ex);
                }
            }
        }

        private static EntryTypeFilter GetFilterForPathType<TEntry>() where TEntry : IPath
        {
            if (typeof(TEntry).IsAssignableTo(typeof(IFilePath)))
                return EntryTypeFilter.Files;
            else if (typeof(TEntry).IsAssignableTo(typeof(IDirectoryPath)))
                return EntryTypeFilter.Directories;

            return EntryTypeFilter.All;
        }

        private static EntryTypeFilter GetFilterForInfoType<TEntryInfo>() where TEntryInfo : CachedEntryInfo
        {
            if (typeof(TEntryInfo) == typeof(CachedFileInfo))
                return EntryTypeFilter.Files;
            else if (typeof(TEntryInfo) == typeof(CachedDirectoryInfo))
                return EntryTypeFilter.Directories;

            return EntryTypeFilter.All;
        }

        private enum EntryTypeFilter
        {
            All,
            Files,
            Directories,
        }

        #endregion
    }
}
