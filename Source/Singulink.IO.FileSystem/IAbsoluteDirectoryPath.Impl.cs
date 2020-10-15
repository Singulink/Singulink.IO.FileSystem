using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using Singulink.IO.Utilities;

namespace Singulink.IO
{
    /// <content>
    /// Contains an implementation of IAbsoluteDirectoryPath.
    /// </content>
    public partial interface IAbsoluteDirectoryPath
    {
        internal new sealed class Impl : IAbsolutePath.Impl, IAbsoluteDirectoryPath
        {
            internal Impl(string path, int rootLength, PathFormat pathFormat) : base(path, rootLength, pathFormat)
            {
            }

            public override bool Exists {
                get {
                    PathFormat.EnsureCurrent();
                    return Directory.Exists(PathExport);
                }
            }

            public bool IsEmpty {
                get {
                    PathFormat.EnsureCurrent();
                    return !Directory.EnumerateFileSystemEntries(PathExport).Any();
                }
            }

            public bool IsRoot => PathDisplay.Length == RootLength;

            public IAbsoluteDirectoryPath? ParentDirectory {
                get {
                    if (!HasParentDirectory)
                        return null;

                    var parentPath = PathFormat.GetPreviousDirectory(PathDisplay, RootLength);
                    return new Impl(parentPath.ToString(), RootLength, PathFormat);
                }
            }

            IAbsoluteDirectoryPath? IAbsolutePath.ParentDirectory => ParentDirectory;

            public bool HasParentDirectory => !IsRoot;

            bool IPath.HasParentDirectory => HasParentDirectory;

            public override FileAttributes Attributes {
                get {
                    PathFormat.EnsureCurrent();

                    FileAttributes attributes;

                    try {
                        attributes = File.GetAttributes(PathExport);
                    }
                    catch (FileNotFoundException) {
                        attributes = 0;
                    }

                    if (!attributes.HasFlag(FileAttributes.Directory))
                        throw new DirectoryNotFoundException();

                    return attributes;
                }
                set {
                    PathFormat.EnsureCurrent();

                    // Ensure this is a directory
                    if (!Exists)
                        throw new DirectoryNotFoundException();

                    File.SetAttributes(PathExport, value);
                }
            }

            public bool IsReady => IsUnc || new DriveInfo(PathDisplay).IsReady;

            public DriveType DriveType => IsUnc ? DriveType.Network : new DriveInfo(PathDisplay).DriveType;

            public string FileSystem => IsUnc ? "Unknown" : new DriveInfo(PathDisplay).DriveFormat;

            public long AvailableFreeSpace => DiskSpace.GetAvailableFreeSpace(PathExport, PathFormat);

            public long TotalFreeSpace => DiskSpace.GetTotalFreeSpace(PathExport, PathFormat);

            public long TotalSize => DiskSpace.GetTotalSize(PathExport, PathFormat);

            #region File System Operations

            public void Create()
            {
                PathFormat.EnsureCurrent();
                Directory.CreateDirectory(PathExport);
            }

            public void Delete(bool recursive = false)
            {
                PathFormat.EnsureCurrent();
                EnsureExists();
                Directory.Delete(PathExport, recursive);
            }

            #endregion

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
                    throw new ArgumentException($"The provided path's format is not universal and does not match the {PathFormat} base path format.", formatParamName);

                var appendPath = path.PathFormat.SplitRelativeNavigation(path.PathDisplay, out int parentDirs);
                appendPath = PathFormat.ConvertRelativePathToMutualFormat(appendPath, path.PathFormat, PathFormat);

                string basePath = GetBasePathForAppending(parentDirs);

                if (basePath == null)
                    throw new ArgumentException("Invalid path combination: Attempt to navigate past root directory.", nameof(path));

                string newPath;

                if (appendPath.Length == 0)
                    newPath = basePath;
                else if (basePath.Length == RootLength)
                    newPath = StringHelper.Concat(basePath, appendPath);
                else
                    newPath = StringHelper.Concat(basePath, PathFormat.SeparatorString, appendPath);

                if (path.IsDirectory)
                    return new Impl(newPath, RootLength, PathFormat);
                else
                    return new IAbsoluteFilePath.Impl(newPath, RootLength, PathFormat);
            }

            private string? GetBasePathForAppending(int parentDirs)
            {
                // TODO: this can be optimized so parent directory instances aren't created.

                if (parentDirs == -1)
                    return RootDirectory.PathDisplay;

                IAbsoluteDirectoryPath currentDir = this;

                for (int i = 0; i < parentDirs; i++) {
                    currentDir = currentDir.ParentDirectory;

                    if (currentDir == null)
                        return null;
                }

                return currentDir.PathDisplay;
            }

            #endregion

            #region Enumeration

            private delegate IEnumerable<FileSystemInfo> Enumerator(DirectoryInfo info, string searchPattern, EnumerationOptions options);

            private static readonly Enumerator DirectoryEnumerator = (info, searchPattern, options) => info.EnumerateDirectories(searchPattern, options);

            private static readonly Enumerator FileEnumerator = (info, searchPattern, options) => info.EnumerateFiles(searchPattern, options);

            private static readonly Enumerator EntryEnumerator = (info, searchPattern, options) => info.EnumerateFileSystemInfos(searchPattern, options);

            public IEnumerable<IAbsoluteDirectoryPath> GetChildDirectories(string searchPattern, SearchOptions options)
            {
                return GetChildEntries<IAbsoluteDirectoryPath>(searchPattern, options, DirectoryEnumerator);
            }

            public IEnumerable<IAbsoluteFilePath> GetChildFiles(string searchPattern, SearchOptions options)
            {
                return GetChildEntries<IAbsoluteFilePath>(searchPattern, options, FileEnumerator);
            }

            public IEnumerable<IAbsolutePath> GetChildEntries(string searchPattern, SearchOptions options)
            {
                return GetChildEntries<IAbsolutePath>(searchPattern, options, EntryEnumerator);
            }

            public IEnumerable<IRelativeDirectoryPath> GetRelativeChildDirectories(string searchPattern, SearchOptions options)
            {
                return GetRelativeChildEntries<IRelativeDirectoryPath>(searchPattern, options, DirectoryEnumerator);
            }

            public IEnumerable<IRelativeFilePath> GetRelativeChildFiles(string searchPattern, SearchOptions options)
            {
                return GetRelativeChildEntries<IRelativeFilePath>(searchPattern, options, FileEnumerator);
            }

            public IEnumerable<IRelativePath> GetRelativeChildEntries(string searchPattern, SearchOptions options)
            {
                return GetRelativeChildEntries<IRelativePath>(searchPattern, options, EntryEnumerator);
            }

            public IEnumerable<IRelativeFilePath> GetRelativeFiles(IRelativeDirectoryPath searchLocation, string searchPattern, SearchOptions options)
            {
                return GetRelativeEntries<IRelativeFilePath>(searchLocation, searchPattern, options, EntryEnumerator);
            }

            public IEnumerable<IRelativeDirectoryPath> GetRelativeDirectories(IRelativeDirectoryPath searchLocation, string searchPattern, SearchOptions options)
            {
                return GetRelativeEntries<IRelativeDirectoryPath>(searchLocation, searchPattern, options, EntryEnumerator);
            }

            public IEnumerable<IRelativePath> GetRelativeEntries(IRelativeDirectoryPath searchLocation, string searchPattern, SearchOptions options)
            {
                return GetRelativeEntries<IRelativePath>(searchLocation, searchPattern, options, EntryEnumerator);
            }

            private IEnumerable<TEntry> GetChildEntries<TEntry>(string searchPattern, SearchOptions options, Enumerator enumerator)
                where TEntry : IAbsolutePath
            {
                foreach (var entryInfo in GetFileSystemInfos(searchPattern, options, enumerator)) {
                    string relativePath = entryInfo.FullName[PathExport.Length..];

                    if (entryInfo is DirectoryInfo dirInfo)
                        yield return (TEntry)(object)new Impl(StringHelper.Concat(PathDisplay, relativePath), 0, PathFormat);
                    else if (entryInfo is FileInfo fileInfo)
                        yield return (TEntry)(object)new IAbsoluteFilePath.Impl(StringHelper.Concat(PathDisplay, relativePath), 0, PathFormat);
                }
            }

            private IEnumerable<TEntry> GetRelativeChildEntries<TEntry>(string searchPattern, SearchOptions options, Enumerator enumerator)
                where TEntry : IRelativePath
            {
                foreach (var entryInfo in GetFileSystemInfos(searchPattern, options, enumerator)) {
                    string relativePath = entryInfo.FullName[(PathExport.Length + 1)..];

                    if (entryInfo is DirectoryInfo)
                        yield return (TEntry)(object)new IRelativeDirectoryPath.Impl(relativePath, 0, PathFormat);
                    else if (entryInfo is FileInfo)
                        yield return (TEntry)(object)new IRelativeFilePath.Impl(relativePath, 0, PathFormat);
                }
            }

            private IEnumerable<TEntry> GetRelativeEntries<TEntry>(IRelativeDirectoryPath searchLocation, string searchPattern, SearchOptions options, Enumerator enumerator)
                where TEntry : IRelativePath
            {
                var searchDir = (Impl)Combine(searchLocation);
                string[] matchDirs = Array.Empty<string>();
                string prefix = searchLocation.PathDisplay;

                if (prefix.Length > 0 && searchLocation.Name.Length == 0) {
                    searchLocation.PathFormat.SplitRelativeNavigation(searchLocation.PathDisplay, out int parentDirs);

                    if (parentDirs == -1) {
                        matchDirs = GetAllDirNames(this).Reverse().ToArray();
                        prefix = string.Join(PathFormat.SeparatorChar, Enumerable.Repeat("..", matchDirs.Length));
                    }
                    else {
                        matchDirs = GetAllDirNames(this).Take(parentDirs).Reverse().ToArray();
                    }
                }

                foreach (var entry in searchDir.GetRelativeChildEntries<TEntry>(searchPattern, options, enumerator)) {
                    StringOrSpan currentPrefix = prefix;
                    StringOrSpan entryPath = entry.PathDisplay;

                    foreach (string matchDir in matchDirs) {
                        var firstEntryName = PathFormat.GetFirstEntry(entryPath);

                        if (firstEntryName.Span.SequenceEqual(matchDir)) {
                            currentPrefix = currentPrefix.Span[Math.Min(3, currentPrefix.Length)..];
                            entryPath = entryPath.Span[(firstEntryName.Length + 1)..];
                        }
                        else {
                            break;
                        }
                    }

                    string finalPath = currentPrefix.Length == 0 ? (string)entryPath : StringHelper.Concat(currentPrefix, PathFormat.SeparatorString, entryPath);

                    if (entry.IsFile) {
                        yield return (TEntry)(object)new IRelativeFilePath.Impl(finalPath, 0, PathFormat);
                    }
                    else {
                        yield return (TEntry)(object)new IRelativeDirectoryPath.Impl(finalPath, 0, PathFormat);
                    }
                }

                static IEnumerable<string> GetAllDirNames(IAbsoluteDirectoryPath path)
                {
                    while (!path.IsRoot) {
                        yield return path.Name;
                        path = path.ParentDirectory!;
                    }
                }
            }

            private IEnumerable<FileSystemInfo> GetFileSystemInfos(string searchPattern, SearchOptions options, Enumerator enumerator)
            {
                PathFormat.EnsureCurrent();
                PathFormat.ValidateSearchPattern(searchPattern, nameof(searchPattern));

                var info = new DirectoryInfo(PathExport);
                return enumerator.Invoke(info, searchPattern, options.ToEnumerationOptions());
            }

            #endregion
        }
    }
}
