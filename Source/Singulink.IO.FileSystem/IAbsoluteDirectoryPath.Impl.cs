using Singulink.Enums;
using Singulink.IO.Utilities;

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

        /// <summary>
        /// Gets the export path with a trailing separator, which is required for some Win32 functions.
        /// </summary>
        internal string PathExportWithTrailingSeparator
        {
            get {
                string path = PathExport;
                string separator = PathFormat.SeparatorString; // Avoid extra string alloc when appending char

                if (path[^1] != separator[0])
                    path += separator;

                return path;
            }
        }

        #region Combine

        public IAbsoluteDirectoryPath Combine(IRelativeDirectoryPath path) => (IAbsoluteDirectoryPath)Combine(path, nameof(path));

        public IAbsoluteDirectoryPath CombineDirectory(ReadOnlySpan<char> path, PathFormat format, PathOptions options)
        {
            return (IAbsoluteDirectoryPath)Combine(DirectoryPath.ParseRelative(path, format, options), nameof(format));
        }

        public IAbsoluteFilePath Combine(IRelativeFilePath path) => (IAbsoluteFilePath)Combine(path, nameof(path));

        public IAbsoluteFilePath CombineFile(ReadOnlySpan<char> path, PathFormat format, PathOptions options)
        {
            return Combine(FilePath.ParseRelative(path, format, options));
        }

        public IAbsolutePath Combine(IRelativePath path) => Combine(path, nameof(path));

        private IAbsolutePath Combine(IRelativePath path, string? formatParamName)
        {
            if (PathFormat.GetMutualFormat(PathFormat, path.PathFormat) != PathFormat)
                throw new ArgumentException($"The provided dir's format is not universal and does not match the {PathFormat} base dir format.", formatParamName);

            var appendPath = path.PathFormat.SplitRelativeNavigation(path.PathDisplay, out int parentDirs);
            appendPath = PathFormat.ConvertRelativePathToMutualFormat(appendPath, path.PathFormat, PathFormat);

            string basePath = GetBasePathForAppending(parentDirs) ?? throw new ArgumentException("Invalid dir combination: Attempt to navigate past root directory.", nameof(path));
            string newPath;

            if (appendPath.Length is 0)
                newPath = basePath;
            else if (basePath.Length == RootLength)
                newPath = $"{basePath}{appendPath.Span}";
            else
                newPath = $"{basePath}{PathFormat.Separator}{appendPath.Span}";

            if (path is IDirectoryPath)
                return new Impl(newPath, RootLength, PathFormat);
            else
                return new IAbsoluteFilePath.Impl(newPath, RootLength, PathFormat);
        }

        private string? GetBasePathForAppending(int parentDirs)
        {
            // TODO: this can be optimized so parent directory instances aren't created.

            if (parentDirs is -1)
                return RootDirectory.PathDisplay;

            IAbsoluteDirectoryPath currentDir = this;

            for (int i = 0; i < parentDirs; i++)
            {
                currentDir = currentDir.ParentDirectory;

                if (currentDir == null)
                    return null;
            }

            return currentDir.PathDisplay;
        }

        #endregion

        #region File System Operations

        public override CachedDirectoryInfo GetInfo()
        {
            PathFormat.EnsureCurrent();
            return new CachedDirectoryInfo(new DirectoryInfo(PathExport), this);
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

        // NOTE: Enumeration methods will never throw UnauthorizedAccessException

        private delegate IEnumerable<FileSystemInfo> GetSystemInfosFunc(DirectoryInfo info, string searchPattern, EnumerationOptions options);

        private static readonly GetSystemInfosFunc GetSystemDirectoryInfos = (info, searchPattern, options) => info.EnumerateDirectories(searchPattern, options);

        private static readonly GetSystemInfosFunc GetSystemFileInfos = (info, searchPattern, options) => info.EnumerateFiles(searchPattern, options);

        private static readonly GetSystemInfosFunc GetSystemEntryInfos = (info, searchPattern, options) => info.EnumerateFileSystemInfos(searchPattern, options);

        public IEnumerable<CachedDirectoryInfo> GetChildDirectoriesInfo(string searchPattern, SearchOptions? options = null) =>
            GetChildEntriesInfo<CachedDirectoryInfo>(searchPattern, options, GetSystemDirectoryInfos);

        public IEnumerable<CachedFileInfo> GetChildFilesInfo(string searchPattern, SearchOptions? options = null) =>
            GetChildEntriesInfo<CachedFileInfo>(searchPattern, options, GetSystemFileInfos);

        public IEnumerable<CachedEntryInfo> GetChildEntriesInfo(string searchPattern, SearchOptions? options = null) =>
            GetChildEntriesInfo<CachedEntryInfo>(searchPattern, options, GetSystemEntryInfos);

        public IEnumerable<IAbsoluteDirectoryPath> GetChildDirectories(string searchPattern, SearchOptions? options) =>
            GetChildEntries<IAbsoluteDirectoryPath>(searchPattern, options, GetSystemDirectoryInfos);

        public IEnumerable<IAbsoluteFilePath> GetChildFiles(string searchPattern, SearchOptions? options) =>
            GetChildEntries<IAbsoluteFilePath>(searchPattern, options, GetSystemFileInfos);

        public IEnumerable<IAbsolutePath> GetChildEntries(string searchPattern, SearchOptions? options) =>
            GetChildEntries<IAbsolutePath>(searchPattern, options, GetSystemEntryInfos);

        public IEnumerable<IRelativeDirectoryPath> GetRelativeChildDirectories(string searchPattern, SearchOptions? options) =>
            GetRelativeChildEntries<IRelativeDirectoryPath>(searchPattern, options, GetSystemDirectoryInfos);

        public IEnumerable<IRelativeFilePath> GetRelativeChildFiles(string searchPattern, SearchOptions? options) =>
            GetRelativeChildEntries<IRelativeFilePath>(searchPattern, options, GetSystemFileInfos);

        public IEnumerable<IRelativePath> GetRelativeChildEntries(string searchPattern, SearchOptions? options) =>
            GetRelativeChildEntries<IRelativePath>(searchPattern, options, GetSystemEntryInfos);

        public IEnumerable<IRelativeDirectoryPath> GetRelativeDirectories(IRelativeDirectoryPath searchLocation, string searchPattern, SearchOptions? options) =>
            GetRelativeEntries<IRelativeDirectoryPath>(searchLocation, searchPattern, options, GetSystemDirectoryInfos);

        public IEnumerable<IRelativeFilePath> GetRelativeFiles(IRelativeDirectoryPath searchLocation, string searchPattern, SearchOptions? options) =>
            GetRelativeEntries<IRelativeFilePath>(searchLocation, searchPattern, options, GetSystemFileInfos);

        public IEnumerable<IRelativePath> GetRelativeEntries(IRelativeDirectoryPath searchLocation, string searchPattern, SearchOptions? options) =>
            GetRelativeEntries<IRelativePath>(searchLocation, searchPattern, options, GetSystemEntryInfos);

        private IEnumerable<TEntryInfo> GetChildEntriesInfo<TEntryInfo>(string searchPattern, SearchOptions? options, GetSystemInfosFunc getInfos)
            where TEntryInfo : CachedEntryInfo
        {
            foreach (var entryInfo in GetEntryInfos(searchPattern, options, getInfos))
            {
                var relativePath = entryInfo.FullName.AsSpan()[PathExport.Length..];
                string resultPath = $"{PathDisplay}{relativePath}";

                if (entryInfo is DirectoryInfo dirInfo)
                {
                    var dir = new Impl(resultPath, RootLength, PathFormat);
                    yield return (TEntryInfo)(object)new CachedDirectoryInfo(dirInfo, dir);
                }
                else if (entryInfo is FileInfo fileInfo)
                {
                    var file = new IAbsoluteFilePath.Impl(resultPath, RootLength, PathFormat);
                    yield return (TEntryInfo)(object)new CachedFileInfo(fileInfo, file);
                }
            }
        }

        private IEnumerable<TEntry> GetChildEntries<TEntry>(string searchPattern, SearchOptions? options, GetSystemInfosFunc getInfos)
            where TEntry : IAbsolutePath
        {
            foreach (var entryInfo in GetEntryInfos(searchPattern, options, getInfos))
            {
                var relativePath = entryInfo.FullName.AsSpan()[PathExport.Length..];
                string resultPath = $"{PathDisplay}{relativePath}";

                if (entryInfo is DirectoryInfo)
                    yield return (TEntry)(object)new Impl(resultPath, RootLength, PathFormat);
                else if (entryInfo is FileInfo)
                    yield return (TEntry)(object)new IAbsoluteFilePath.Impl(resultPath, RootLength, PathFormat);
            }
        }

        private IEnumerable<TEntry> GetRelativeChildEntries<TEntry>(string searchPattern, SearchOptions? options, GetSystemInfosFunc getInfos)
            where TEntry : IRelativePath
        {
            foreach (var entryInfo in GetEntryInfos(searchPattern, options, getInfos))
            {
                string relativePath = entryInfo.FullName[(PathExport.Length + 1)..];

                if (entryInfo is DirectoryInfo)
                    yield return (TEntry)(object)new IRelativeDirectoryPath.Impl(relativePath, 0, PathFormat);
                else if (entryInfo is FileInfo)
                    yield return (TEntry)(object)new IRelativeFilePath.Impl(relativePath, 0, PathFormat);
            }
        }

        private IEnumerable<TEntry> GetRelativeEntries<TEntry>(IRelativeDirectoryPath searchLocation, string searchPattern, SearchOptions? options, GetSystemInfosFunc getInfos)
            where TEntry : IRelativePath
        {
            var searchDir = (Impl)Combine(searchLocation);
            string[] matchDirs;
            string prefix;

            if (searchLocation.PathDisplay.Length > 0 && searchLocation.Name.Length is 0)
            {
                searchLocation.PathFormat.SplitRelativeNavigation(searchLocation.PathDisplay, out int parentDirs);

                if (parentDirs is -1)
                {
                    matchDirs = GetAllDirNames(this).Reverse().ToArray();
                    prefix = string.Join(PathFormat.Separator, Enumerable.Repeat("..", matchDirs.Length));
                }
                else
                {
                    matchDirs = GetAllDirNames(this).Take(parentDirs).Reverse().ToArray();
                    prefix = searchLocation.PathDisplay;
                }
            }
            else
            {
                matchDirs = [];
                prefix = searchLocation.PathDisplay;
            }

            foreach (var entry in searchDir.GetRelativeChildEntries<TEntry>(searchPattern, options, getInfos))
            {
                StringOrSpan currentPrefix = prefix;
                StringOrSpan entryPath = entry.PathDisplay;

                foreach (string matchDir in matchDirs)
                {
                    var firstEntryName = PathFormat.GetFirstEntry(entryPath);

                    if (firstEntryName.Span.SequenceEqual(matchDir))
                    {
                        currentPrefix = currentPrefix.Span[Math.Min(3, currentPrefix.Length)..];
                        entryPath = entryPath.Span[(firstEntryName.Length + 1)..];
                    }
                    else
                    {
                        break;
                    }
                }

                string finalPath = currentPrefix.Length is 0 ? entryPath : $"{currentPrefix}{PathFormat.Separator}{entryPath}";

                if (entry is IFilePath)
                {
                    yield return (TEntry)(object)new IRelativeFilePath.Impl(finalPath, 0, PathFormat);
                }
                else
                {
                    yield return (TEntry)(object)new IRelativeDirectoryPath.Impl(finalPath, 0, PathFormat);
                }
            }

            static IEnumerable<string> GetAllDirNames(IAbsoluteDirectoryPath path)
            {
                while (!path.IsRoot)
                {
                    yield return path.Name;
                    path = path.ParentDirectory!;
                }
            }
        }

        private IEnumerable<FileSystemInfo> GetEntryInfos(string searchPattern, SearchOptions? options, GetSystemInfosFunc getInfos)
        {
            PathFormat.EnsureCurrent();
            PathFormat.ValidateSearchPattern(searchPattern, nameof(searchPattern));

            IEnumerator<FileSystemInfo> enumerator = GetEnumerator(out bool requiresExtraAccessCheck);
            bool yielded = false;

            try
            {
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
            }
            finally
            {
                enumerator.Dispose();
            }

            if (!yielded && requiresExtraAccessCheck)
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

            IEnumerator<FileSystemInfo> GetEnumerator(out bool requiresExtraAccessCheck)
            {
                var dirInfo = new DirectoryInfo(PathExport);
                var enumerationOptions = SearchOptions.ToEnumerationOptions(options, out requiresExtraAccessCheck);

                try
                {
                    // Throws IOEx if dir is a file on windows, DirectoryNotFoundEx on Unix. Convert to IOEx.
                    return getInfos(dirInfo, searchPattern, enumerationOptions).GetEnumerator();
                }
                catch (DirectoryNotFoundException) when (PathFormat == PathFormat.Unix)
                {
                    ThrowIfDirIsFile(this);
                    throw;
                }
                catch (UnauthorizedAccessException ex)
                {
                    throw Ex.Convert(ex);
                }
            }
        }

        #endregion
    }
}
