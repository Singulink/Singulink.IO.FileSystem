using System.ComponentModel;
using System.Text.Json.Serialization;
using Singulink.IO.Converters;
using Singulink.IO.Json;

namespace Singulink.IO;

/// <summary>
/// Represents an absolute path to a directory.
/// </summary>
[TypeConverter(typeof(AbsoluteDirectoryPathConverter))]
[JsonConverter(typeof(AbsoluteDirectoryPathJsonConverter))]
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
    /// Gets a value indicating whether this directory is empty, i.e. it contains no files and no subdirectories.
    /// </summary>
    bool IsEmpty { get; }

    /// <summary>
    /// Gets the drive type that the directory points to, e.g. CD-ROM, removable, network or fixed.
    /// </summary>
    DriveType DriveType { get; }

    /// <summary>
    /// Gets the name of the file system, such as NTFS or FAT32.
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
    /// Gets cached info for an existing file or directory located by combining this directory with the specified relative path parsed using the specified
    /// options and this directory's path format. The returned instance is either a <see cref="CachedFileInfo"/> or <see cref="CachedDirectoryInfo"/>
    /// depending on the entry's type.
    /// </summary>
    /// <param name="path">A relative path to an existing file or directory under (or navigated from) this directory.</param>
    /// <param name="options">The options to use for parsing the appended relative path.</param>
    /// <exception cref="FileNotFoundException">No file or directory exists at the combined path.</exception>
    /// <exception cref="DirectoryNotFoundException">A parent directory in the combined path does not exist.</exception>
    sealed CachedEntryInfo GetInfo(ReadOnlySpan<char> path, PathOptions options = PathOptions.NoUnfriendlyNames)
    {
        return GetInfo(path, PathFormat, options);
    }

    /// <summary>
    /// Gets cached info for an existing file or directory located by combining this directory with the specified relative path parsed using the specified
    /// format and options. The returned instance is either a <see cref="CachedFileInfo"/> or <see cref="CachedDirectoryInfo"/> depending on the entry's
    /// type.
    /// </summary>
    /// <param name="path">A relative path to an existing file or directory under (or navigated from) this directory.</param>
    /// <param name="format">The path format to use for parsing the appended relative path. Must be either this directory's path format or the universal format.</param>
    /// <param name="options">The options to use for parsing the appended relative path.</param>
    /// <exception cref="FileNotFoundException">No file or directory exists at the combined path.</exception>
    /// <exception cref="DirectoryNotFoundException">A parent directory in the combined path does not exist.</exception>
    CachedEntryInfo GetInfo(ReadOnlySpan<char> path, PathFormat format, PathOptions options = PathOptions.NoUnfriendlyNames);

    /// <summary>
    /// Gets information about this directory.
    /// </summary>
    new CachedDirectoryInfo GetInfo();

    /// <inheritdoc/>
    CachedEntryInfo IAbsolutePath.GetInfo() => GetInfo();

    /// <summary>
    /// Creates all directories and subdirectories in the specified path unless they already exist.
    /// </summary>
    void Create();

    /// <summary>
    /// Deletes the specified directory and, if indicated, any subdirectories and files in the directory.
    /// </summary>
    /// <param name="recursive"><see langword="true"/> to remove directories, subdirectories and files in path; otherwise, <see langword="false"/>.</param>
    /// <param name="ignoreNotFound"><see langword="true"/> to ignore directory not found errors; otherwise, <see langword="false"/> to throw <see
    /// cref="DirectoryNotFoundException"/> if the file was not found.</param>
    void Delete(bool recursive = false, bool ignoreNotFound = true);

    /// <summary>
    /// Moves the directory to a new location. Destination must be on the same volume and cannot be an existing directory.
    /// </summary>
    void MoveTo(IAbsoluteDirectoryPath destinationDir);

    /// <summary>
    /// Flushes the directory's entries to disk, so that files and directories created in, renamed into or removed from it survive a power loss or an
    /// operating system crash.
    /// </summary>
    /// <param name="ignoreNotFound"><see langword="true"/> to ignore directory not found errors; otherwise, <see langword="false"/> to throw <see
    /// cref="DirectoryNotFoundException"/> if the directory was not found.</param>
    /// <remarks>
    /// <para>
    /// Only the directory's own entries are flushed: not the contents of the files in it (see <see cref="IAbsoluteFilePath.FlushToDisk(bool)"/>), and not
    /// the entries of its subdirectories or of its parent directory. A directory's own name is an entry of its parent directory.</para>
    /// <para>
    /// On Unix the directory is opened and flushed with <c>fsync</c>. On Apple platforms <c>F_FULLFSYNC</c> is tried first, as it also flushes the drive's
    /// write cache, and <c>fsync</c> is used if it fails. A file system that refuses to flush a directory, by failing <c>fsync</c> with <c>EINVAL</c>,
    /// <c>EBADF</c> or <c>ENOTSUP</c> (as some network and virtual file systems do), is tolerated: the call returns normally without anything having been
    /// flushed, and the entries are then only as durable as that file system makes them by itself. Any other failure is thrown.</para>
    /// <para>
    /// On Windows nothing is flushed, and the call only checks that the directory exists. NTFS records changes to directories in its log, and flushing any
    /// file on the volume forces that log to disk. A change to a directory is therefore durable on Windows once a file on the same volume has been flushed
    /// after the change was made, such as by calling <see cref="IAbsoluteFilePath.FlushToDisk(bool)"/> on a file after creating it in or moving it into the
    /// directory. Windows file systems that keep no such log, such as FAT32 and exFAT, give no equivalent guarantee.</para>
    /// <para>
    /// Unlike <see cref="Delete(bool, bool)"/>, a directory that is not found is not ignored by default: nothing was made durable, which is usually a
    /// mistake. If <paramref name="ignoreNotFound"/> is <see langword="true"/> then on Unix what <see cref="Delete(bool, bool)"/> ignores is ignored: a
    /// directory that does not exist in a parent directory that does. A parent directory that does not exist and a path that points to a file still throw
    /// there. On Windows, where the existence of the directory is all there is to check, nothing is checked and no system call is made, so a parent
    /// directory that does not exist and a path that points to a file pass as well.</para>
    /// </remarks>
    /// <exception cref="DirectoryNotFoundException">The directory does not exist and <paramref name="ignoreNotFound"/> is <see langword="false"/>, or on Unix
    /// its parent directory does not exist.</exception>
    /// <exception cref="UnauthorizedIOAccessException">Access to the directory is denied.</exception>
    /// <exception cref="IOException">The path points to a file, or the directory could not be opened or flushed.</exception>
    void FlushToDisk(bool ignoreNotFound = false);

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

    #region Info Enumeration

    // Directories

    /// <summary>
    /// Gets info for the child directories that reside in this directory and match the specified search options.
    /// </summary>
    /// <remarks>
    /// An entry that is deleted during the enumeration may or may not be included.
    /// </remarks>
    sealed IEnumerable<CachedDirectoryInfo> GetChildDirectoriesInfo(SearchOptions? options = null) => GetChildDirectoriesInfo("*", options);

    /// <summary>
    /// Gets info for the child directories that reside in this directory and match the specified search pattern and search options.
    /// </summary>
    /// <remarks>
    /// An entry that is deleted during the enumeration may or may not be included.
    /// </remarks>
    IEnumerable<CachedDirectoryInfo> GetChildDirectoriesInfo(string searchPattern, SearchOptions? options = null);

    // Files

    /// <summary>
    /// Gets info for the child files that reside in this directory and match the specified search options.
    /// </summary>
    /// <remarks>
    /// An entry that is deleted during the enumeration may or may not be included.
    /// </remarks>
    sealed IEnumerable<CachedFileInfo> GetChildFilesInfo(SearchOptions? options = null) => GetChildFilesInfo("*", options);

    /// <summary>
    /// Gets info for the child files that reside in this directory and match the specified search pattern and search options.
    /// </summary>
    /// <remarks>
    /// An entry that is deleted during the enumeration may or may not be included.
    /// </remarks>
    IEnumerable<CachedFileInfo> GetChildFilesInfo(string searchPattern, SearchOptions? options = null);

    // Entries

    /// <summary>
    /// Gets info for the child files/directories that reside in this directory and match the specified search options.
    /// </summary>
    /// <remarks>
    /// An entry that is deleted during the enumeration may or may not be included.
    /// </remarks>
    sealed IEnumerable<CachedEntryInfo> GetChildEntriesInfo(SearchOptions? options = null) => GetChildEntriesInfo("*", options);

    /// <summary>
    /// Gets info for the child files/directories that reside in this directory and match the specified search pattern and search options.
    /// </summary>
    /// <remarks>
    /// An entry that is deleted during the enumeration may or may not be included.
    /// </remarks>
    IEnumerable<CachedEntryInfo> GetChildEntriesInfo(string searchPattern, SearchOptions? options = null);

    #endregion

    #region Absolute Enumeration

    // Directories

    /// <summary>
    /// Gets the child directories that reside in this directory and match the specified search options.
    /// </summary>
    sealed IEnumerable<IAbsoluteDirectoryPath> GetChildDirectories(SearchOptions? options = null) => GetChildDirectories("*", options);

    /// <summary>
    /// Gets the child directories that reside in this directory and match the specified search pattern and search options.
    /// </summary>
    IEnumerable<IAbsoluteDirectoryPath> GetChildDirectories(string searchPattern, SearchOptions? options = null);

    // Files

    /// <summary>
    /// Gets the child files that reside in this directory and match the specified search options.
    /// </summary>
    sealed IEnumerable<IAbsoluteFilePath> GetChildFiles(SearchOptions? options = null) => GetChildFiles("*", options);

    /// <summary>
    /// Gets the child files that reside in this directory and match the specified search pattern and search options.
    /// </summary>
    IEnumerable<IAbsoluteFilePath> GetChildFiles(string searchPattern, SearchOptions? options = null);

    // Entries

    /// <summary>
    /// Gets the child files/directories that reside in this directory and match the specified search options.
    /// </summary>
    sealed IEnumerable<IAbsolutePath> GetChildEntries(SearchOptions? options = null) => GetChildEntries("*", options);

    /// <summary>
    /// Gets the child files/directories that reside in this directory and match the specified search pattern and search options.
    /// </summary>
    IEnumerable<IAbsolutePath> GetChildEntries(string searchPattern, SearchOptions? options = null);

    #endregion

    #region Relative Child Enumeration

    // Directories

    /// <summary>
    /// Gets the child directories that reside in this directory, relative to this directory, which match the specified search options.
    /// </summary>
    /// <param name="options">The options to use when searching the directory.</param>
    sealed IEnumerable<IRelativeDirectoryPath> GetRelativeChildDirectories(SearchOptions? options = null) => GetRelativeChildDirectories("*", options);

    /// <summary>
    /// Gets the child directories that reside in this directory, relative to this directory, which match the specified search pattern and search options.
    /// </summary>
    /// <param name="searchPattern">The pattern that describes the names to search, which can contain wildcards <c>*</c> and <c>?</c>.</param>
    /// <param name="options">The options to use when searching the directory.</param>
    IEnumerable<IRelativeDirectoryPath> GetRelativeChildDirectories(string searchPattern, SearchOptions? options = null);

    // Files

    /// <summary>
    /// Gets the child files that reside in this directory, relative to this directory, which match the specified search options.
    /// </summary>
    /// <param name="options">The options to use when searching the directory.</param>
    sealed IEnumerable<IRelativeFilePath> GetRelativeChildFiles(SearchOptions? options = null) => GetRelativeChildFiles("*", options);

    /// <summary>
    /// Gets the child files that reside in this directory, relative to this directory, which match the specified search pattern and search options.
    /// </summary>
    /// <param name="searchPattern">The pattern that describes the names to search, which can contain wildcards <c>*</c> and <c>?</c>.</param>
    /// <param name="options">The options to use when searching the directory.</param>
    IEnumerable<IRelativeFilePath> GetRelativeChildFiles(string searchPattern, SearchOptions? options = null);

    // Entries

    /// <summary>
    /// Gets the child files/directories that reside in this directory, relative to this directory, which match the specified search options.
    /// </summary>
    /// <param name="options">The options to use when searching the directory.</param>
    sealed IEnumerable<IRelativePath> GetRelativeChildEntries(SearchOptions? options = null) => GetRelativeChildEntries("*", options);

    /// <summary>
    /// Gets the child files/directories that reside in this directory, relative to this directory, which match the specified search pattern and search
    /// options.
    /// </summary>
    /// <param name="searchPattern">The pattern that describes the names to search, which can contain wildcards <c>*</c> and <c>?</c>.</param>
    /// <param name="options">The options to use when searching the directory.</param>
    IEnumerable<IRelativePath> GetRelativeChildEntries(string searchPattern, SearchOptions? options = null);

    #endregion

    #region Relative Enumeration

    /// <summary>
    /// Gets the files that reside in the combination of this directory and the search location, relative to this directory, which match
    /// the specified search options.
    /// </summary>
    /// <param name="searchLocation">The relative location from this directory to search.</param>
    /// <param name="options">The options to use when searching the directory.</param>
    sealed IEnumerable<IRelativeFilePath> GetRelativeFiles(IRelativeDirectoryPath searchLocation, SearchOptions? options = null)
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
    IEnumerable<IRelativeFilePath> GetRelativeFiles(IRelativeDirectoryPath searchLocation, string searchPattern, SearchOptions? options = null);

    /// <summary>
    /// Gets the directories that reside in the combination of this directory and the search location, relative to this directory, which match
    /// the specified search options.
    /// </summary>
    /// <param name="searchLocation">The relative location from this directory to search.</param>
    /// <param name="options">The options to use when searching the directory.</param>
    sealed IEnumerable<IRelativeDirectoryPath> GetRelativeDirectories(IRelativeDirectoryPath searchLocation, SearchOptions? options = null)
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
    IEnumerable<IRelativeDirectoryPath> GetRelativeDirectories(IRelativeDirectoryPath searchLocation, string searchPattern, SearchOptions? options = null);

    /// <summary>
    /// Gets the files/directories that reside in the combination of this directory and the search location, relative to this directory, which match
    /// the specified search options.
    /// </summary>
    /// <param name="searchLocation">The relative location from this directory to search.</param>
    /// <param name="options">The options to use when searching the directory.</param>
    sealed IEnumerable<IRelativePath> GetRelativeEntries(IRelativeDirectoryPath searchLocation, SearchOptions? options = null)
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
    IEnumerable<IRelativePath> GetRelativeEntries(IRelativeDirectoryPath searchLocation, string searchPattern, SearchOptions? options = null);

    #endregion
}
