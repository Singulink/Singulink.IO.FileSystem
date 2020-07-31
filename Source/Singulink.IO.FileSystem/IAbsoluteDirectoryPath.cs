﻿using System;
using System.Collections.Generic;
using System.IO;

namespace Singulink.IO
{
    /// <summary>
    /// Represents an absolute path to a directory.
    /// </summary>
    public partial interface IAbsoluteDirectoryPath : IAbsolutePath, IDirectoryPath
    {
        /// <summary>
        /// Combines an absolute directory with a relative directory.
        /// </summary>
        public static IAbsoluteDirectoryPath operator +(IAbsoluteDirectoryPath x, IRelativeDirectoryPath y) => x.Combine(y);

        /// <summary>
        /// Combines an absolute directory with a relative file.
        /// </summary>
        public static IAbsoluteFilePath operator +(IAbsoluteDirectoryPath x, IRelativeFilePath y) => x.Combine(y);

        /// <summary>
        /// Combines an absolute directory with a relative entry.
        /// </summary>
        public static IAbsolutePath operator +(IAbsoluteDirectoryPath x, IRelativePath y) => x.Combine(y);

        /// <summary>
        /// Gets a value indicating whether this is a root directory.
        /// </summary>
        bool IsRoot { get; }

        /// <summary>
        /// Gets a value indicating whether this directory is empty.
        /// </summary>
        bool IsEmpty { get; }

        /// <summary>
        /// Gets a value indicating whether the directory is ready for access (i.e. disk is mounted or DVD is inserted into the drive). Always returns
        /// <c>true</c> for UNC paths.
        /// </summary>
        bool IsReady { get; }

        /// <summary>
        /// Gets the drive type that the directory points to, i.e. CD-ROM, removable, network or fixed.
        /// </summary>
        DriveType DriveType { get; }

        /// <summary>
        /// Gets the name of the file system, such as NTFS or FAT32. Always returns "Unknown" for UNC paths.
        /// </summary>
        string FileSystem { get; }

        /// <summary>
        /// Gets the available free space in the directory, in bytes, taking user quotas into account.
        /// </summary>
        long AvailableFreeSpace { get; }

        /// <summary>
        /// Gets the total free space in the directory, in bytes, not taking any user quotas into account.
        /// </summary>
        long TotalFreeSpace { get; }

        /// <summary>
        /// Gets the total size of storage space in the directory, in bytes.
        /// </summary>
        long TotalSize { get; }

        #region File System Operations

        /// <summary>
        /// Creates all directories and subdirectories in the specified path unless they already exist.
        /// </summary>
        void Create();

        /// <summary>
        /// Deletes the specified directory and, if indicated, any subdirectories and files in the directory.
        /// </summary>
        /// <param name="recursive">True to remove directories, subdirectories and files in path; otherwise, false.</param>
        void Delete(bool recursive = false);

        #endregion

        #region Combining

        // Directory

        /// <inheritdoc cref="IDirectoryPath.Combine(IRelativeDirectoryPath)"/>
        new IAbsoluteDirectoryPath Combine(IRelativeDirectoryPath path);

        /// <inheritdoc cref="IDirectoryPath.CombineDirectory(ReadOnlySpan{char}, PathOptions)"/>
        sealed new IAbsoluteDirectoryPath CombineDirectory(ReadOnlySpan<char> path, PathOptions options = PathOptions.NoUnfriendlyNames)
        {
            return CombineDirectory(path, PathFormat, options);
        }

        /// <inheritdoc cref="IDirectoryPath.CombineDirectory(ReadOnlySpan{char}, PathFormat, PathOptions)"/>
        new IAbsoluteDirectoryPath CombineDirectory(ReadOnlySpan<char> path, PathFormat format, PathOptions options = PathOptions.NoUnfriendlyNames);

        // File

        /// <inheritdoc cref="IDirectoryPath.Combine(IRelativeFilePath)"/>
        new IAbsoluteFilePath Combine(IRelativeFilePath path);

        /// <inheritdoc cref="IDirectoryPath.CombineFile(ReadOnlySpan{char}, PathOptions)"/>
        sealed new IAbsoluteFilePath CombineFile(ReadOnlySpan<char> path, PathOptions options = PathOptions.NoUnfriendlyNames)
        {
            return CombineFile(path, PathFormat, options);
        }

        /// <inheritdoc cref="IDirectoryPath.CombineFile(ReadOnlySpan{char}, PathFormat, PathOptions)"/>
        new IAbsoluteFilePath CombineFile(ReadOnlySpan<char> path, PathFormat format, PathOptions options = PathOptions.NoUnfriendlyNames);

        // Entry

        /// <inheritdoc cref="IDirectoryPath.Combine(IRelativePath)"/>
        new IAbsolutePath Combine(IRelativePath path);

        // Explicit base implementations

        /// <inheritdoc/>
        IDirectoryPath IDirectoryPath.Combine(IRelativeDirectoryPath path) => Combine(path);

        /// <inheritdoc/>
        IDirectoryPath IDirectoryPath.CombineDirectory(ReadOnlySpan<char> path, PathFormat format, PathOptions options)
        {
            return CombineDirectory(path, format, options);
        }

        /// <inheritdoc/>
        IFilePath IDirectoryPath.Combine(IRelativeFilePath path) => Combine(path);

        /// <inheritdoc/>
        IFilePath IDirectoryPath.CombineFile(ReadOnlySpan<char> path, PathFormat format, PathOptions options)
        {
            return CombineFile(path, format, options);
        }

        /// <inheritdoc/>
        IPath IDirectoryPath.Combine(IRelativePath path) => Combine(path);

        #endregion

        #region Absolute Enumeration

        // Directories

        /// <summary>
        /// Gets the child directories that directly reside in this directory.
        /// </summary>
        sealed IEnumerable<IAbsoluteDirectoryPath> GetChildDirectories() => GetChildDirectories("*", SearchOptions.Default);

        /// <summary>
        /// Gets the child directories that reside in this directory and match the specified search options.
        /// </summary>
        sealed IEnumerable<IAbsoluteDirectoryPath> GetChildDirectories(SearchOptions options) => GetChildDirectories("*", options);

        /// <summary>
        /// Gets the child directories that reside in this directory and match the specified search pattern and search options.
        /// </summary>
        IEnumerable<IAbsoluteDirectoryPath> GetChildDirectories(string searchPattern, SearchOptions options);

        // Files

        /// <summary>
        /// Gets the child files that directly reside in this directory.
        /// </summary>
        sealed IEnumerable<IAbsoluteFilePath> GetChildFiles() => GetChildFiles("*", SearchOptions.Default);

        /// <summary>
        /// Gets the child files that reside in this directory and match the specified search options.
        /// </summary>
        sealed IEnumerable<IAbsoluteFilePath> GetChildFiles(SearchOptions options) => GetChildFiles("*", options);

        /// <summary>
        /// Gets the child files that reside in this directory and match the specified search pattern and search options.
        /// </summary>
        IEnumerable<IAbsoluteFilePath> GetChildFiles(string searchPattern, SearchOptions options);

        // Entries

        /// <summary>
        /// Gets the child files/directories that directly reside in this directory.
        /// </summary>
        sealed IEnumerable<IAbsolutePath> GetChildEntries() => GetChildEntries("*", SearchOptions.Default);

        /// <summary>
        /// Gets the child files/directories that reside in this directory and match the specified search options.
        /// </summary>
        sealed IEnumerable<IAbsolutePath> GetChildEntries(SearchOptions options) => GetChildEntries("*", options);

        /// <summary>
        /// Gets the child files/directories that reside in this directory and match the specified search pattern and search options.
        /// </summary>
        IEnumerable<IAbsolutePath> GetChildEntries(string searchPattern, SearchOptions options);

        #endregion

        #region Relative Child Enumeration

        // Directories

        /// <summary>
        /// Gets the child directories that directly reside in this directory, relative to this directory.
        /// </summary>
        sealed IEnumerable<IRelativeDirectoryPath> GetRelativeChildDirectories() => GetRelativeChildDirectories("*", SearchOptions.Default);

        /// <summary>
        /// Gets the child directories that reside in this directory, relative to this directory, which match the specified search options.
        /// </summary>
        /// <param name="options">The options to use when searching the directory.</param>
        sealed IEnumerable<IRelativeDirectoryPath> GetRelativeChildDirectories(SearchOptions options) => GetRelativeChildDirectories("*", options);

        /// <summary>
        /// Gets the child directories that reside in this directory, relative to this directory, which match the specified search pattern and search options.
        /// </summary>
        /// <param name="searchPattern">The pattern that describes the names to search, which can contain wildcards <c>*</c> and <c>?</c>.</param>
        /// <param name="options">The options to use when searching the directory.</param>
        IEnumerable<IRelativeDirectoryPath> GetRelativeChildDirectories(string searchPattern, SearchOptions options);

        // Files

        /// <summary>
        /// Gets the child files that directly reside in this directory, relative to this directory.
        /// </summary>
        sealed IEnumerable<IRelativeFilePath> GetRelativeChildFiles() => GetRelativeChildFiles("*", SearchOptions.Default);

        /// <summary>
        /// Gets the child files that reside in this directory, relative to this directory, which match the specified search options.
        /// </summary>
        /// <param name="options">The options to use when searching the directory.</param>
        sealed IEnumerable<IRelativeFilePath> GetRelativeChildFiles(SearchOptions options) => GetRelativeChildFiles("*", options);

        /// <summary>
        /// Gets the child files that reside in this directory, relative to this directory, which match the specified search pattern and search options.
        /// </summary>
        /// <param name="searchPattern">The pattern that describes the names to search, which can contain wildcards <c>*</c> and <c>?</c>.</param>
        /// <param name="options">The options to use when searching the directory.</param>
        IEnumerable<IRelativeFilePath> GetRelativeChildFiles(string searchPattern, SearchOptions options);

        // Entries

        /// <summary>
        /// Gets the child files/directories that directly reside in this directory, relative to this directory.
        /// </summary>
        sealed IEnumerable<IRelativePath> GetRelativeChildEntries() => GetRelativeChildEntries("*", SearchOptions.Default);

        /// <summary>
        /// Gets the child files/directories that reside in this directory, relative to this directory, which match the specified search options.
        /// </summary>
        /// <param name="options">The options to use when searching the directory.</param>
        sealed IEnumerable<IRelativePath> GetRelativeChildEntries(SearchOptions options) => GetRelativeChildEntries("*", options);

        /// <summary>
        /// Gets the child files/directories that reside in this directory, relative to this directory, which match the specified search pattern and search
        /// options.
        /// </summary>
        /// <param name="searchPattern">The pattern that describes the names to search, which can contain wildcards <c>*</c> and <c>?</c>.</param>
        /// <param name="options">The options to use when searching the directory.</param>
        IEnumerable<IRelativePath> GetRelativeChildEntries(string searchPattern, SearchOptions options);

        #endregion

        #region Relative Enumeration

        /// <summary>
        /// Gets the files that reside in the combination of this directory and the search location, relative to this directory.
        /// </summary>
        /// <param name="searchLocation">The relative location from this directory to search.</param>
        sealed IEnumerable<IRelativeFilePath> GetRelativeFiles(IRelativeDirectoryPath searchLocation)
        {
            return GetRelativeFiles(searchLocation, "*", SearchOptions.Default);
        }

        /// <summary>
        /// Gets the files that reside in the combination of this directory and the search location, relative to this directory, which match
        /// the specified search options.
        /// </summary>
        /// <param name="searchLocation">The relative location from this directory to search.</param>
        /// <param name="options">The options to use when searching the directory.</param>
        sealed IEnumerable<IRelativeFilePath> GetRelativeFiles(IRelativeDirectoryPath searchLocation, SearchOptions options)
        {
            return GetRelativeFiles(searchLocation, "*", options);
        }

        /// <summary>
        /// Gets the child files that reside in the combination of this directory and the search location, relative to this directory, which match
        /// the specified search pattern and search options.
        /// </summary>
        /// <param name="searchLocation">The relative location from this directory to search.</param>
        /// <param name="searchPattern">The pattern that describes the names to search, which can contain wildcards <c>*</c> and <c>?</c>.</param>
        /// <param name="options">The options to use when searching the directory.</param>
        IEnumerable<IRelativeFilePath> GetRelativeFiles(IRelativeDirectoryPath searchLocation, string searchPattern, SearchOptions options);

        /// <summary>
        /// Gets the directories that reside in the combination of this directory and the search location, relative to this directory.
        /// </summary>
        /// <param name="searchLocation">The relative location from this directory to search.</param>
        sealed IEnumerable<IRelativeDirectoryPath> GetRelativeDirectories(IRelativeDirectoryPath searchLocation)
        {
            return GetRelativeDirectories(searchLocation, "*", SearchOptions.Default);
        }

        /// <summary>
        /// Gets the directories that reside in the combination of this directory and the search location, relative to this directory, which match
        /// the specified search options.
        /// </summary>
        /// <param name="searchLocation">The relative location from this directory to search.</param>
        /// <param name="options">The options to use when searching the directory.</param>
        sealed IEnumerable<IRelativeDirectoryPath> GetRelativeDirectories(IRelativeDirectoryPath searchLocation, SearchOptions options)
        {
            return GetRelativeDirectories(searchLocation, "*", options);
        }

        /// <summary>
        /// Gets the child directories that reside in the combination of this directory and the search location, relative to this directory, which match
        /// the specified search pattern and search options.
        /// </summary>
        /// <param name="searchLocation">The relative location from this directory to search.</param>
        /// <param name="searchPattern">The pattern that describes the names to search, which can contain wildcards <c>*</c> and <c>?</c>.</param>
        /// <param name="options">The options to use when searching the directory.</param>
        IEnumerable<IRelativeDirectoryPath> GetRelativeDirectories(IRelativeDirectoryPath searchLocation, string searchPattern, SearchOptions options);

        /// <summary>
        /// Gets the files/directories that reside in the combination of this directory and the search location, relative to this directory.
        /// </summary>
        /// <param name="searchLocation">The relative location from this directory to search.</param>
        sealed IEnumerable<IRelativePath> GetRelativeEntries(IRelativeDirectoryPath searchLocation)
        {
            return GetRelativeEntries(searchLocation, "*", SearchOptions.Default);
        }

        /// <summary>
        /// Gets the files/directories that reside in the combination of this directory and the search location, relative to this directory, which match
        /// the specified search options.
        /// </summary>
        /// <param name="searchLocation">The relative location from this directory to search.</param>
        /// <param name="options">The options to use when searching the directory.</param>
        sealed IEnumerable<IRelativePath> GetRelativeEntries(IRelativeDirectoryPath searchLocation, SearchOptions options)
        {
            return GetRelativeEntries(searchLocation, "*", options);
        }

        /// <summary>
        /// Gets the files/directories that reside in the combination of this directory and the search location, relative to this directory, which match
        /// the specified search pattern and search options.
        /// </summary>
        /// <param name="searchLocation">The relative location from this directory to search.</param>
        /// <param name="searchPattern">The pattern that describes the names to search, which can contain wildcards <c>*</c> and <c>?</c>.</param>
        /// <param name="options">The options to use when searching the directory.</param>
        IEnumerable<IRelativePath> GetRelativeEntries(IRelativeDirectoryPath searchLocation, string searchPattern, SearchOptions options);

        #endregion
    }
}