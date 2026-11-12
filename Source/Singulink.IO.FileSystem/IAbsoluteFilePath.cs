using System.ComponentModel;
using System.Text.Json.Serialization;
using Microsoft.Win32.SafeHandles;
using Singulink.IO.Converters;
using Singulink.IO.Json;

namespace Singulink.IO;

/// <summary>
/// Represents an absolute path to a file.
/// </summary>
[TypeConverter(typeof(AbsoluteFilePathConverter))]
[JsonConverter(typeof(AbsoluteFilePathJsonConverter))]
public partial interface IAbsoluteFilePath : IAbsolutePath, IFilePath
{
    /// <summary>
    /// Gets the file's parent directory.
    /// </summary>
    new IAbsoluteDirectoryPath ParentDirectory { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the file is read-only.
    /// </summary>
    bool IsReadOnly { get; set; }

    /// <summary>
    /// Gets the size of the file in bytes.
    /// </summary>
    long Length { get; }

    /// <summary>
    /// Gets information about this file.
    /// </summary>
    new CachedFileInfo GetInfo();

    /// <inheritdoc/>
    CachedEntryInfo IAbsolutePath.GetInfo() => GetInfo();

    /// <summary>
    /// Opens a file stream to a new or existing file.
    /// </summary>
    /// <param name="mode">A <see cref="FileMode"/> constant specifying the mode (for example, Open or Append) in which to open the file.</param>
    /// <param name="access">A <see cref="FileAccess"/> constant specifying whether to open the file with <c>Read</c>, <c>Write</c>, or <c>ReadWrite</c>
    /// file access.</param>
    /// <param name="share">A <see cref="FileShare"/> constant specifying the type of access other <c>FileStream</c> objects have to this file.</param>
    /// <param name="bufferSize">A positive value indicating the buffer size.</param>
    /// <param name="options">Additional file options.</param>
    /// <returns>A new <see cref="FileStream"/> to the opened file.</returns>
    FileStream OpenStream(FileMode mode = FileMode.Open, FileAccess access = FileAccess.ReadWrite, FileShare share = FileShare.None, int bufferSize = 4096, FileOptions options = FileOptions.None);

    /// <summary>
    /// Opens an asynchronous file stream to a new or existing file (the <see cref="FileOptions.Asynchronous"/> option is always appended).
    /// </summary>
    /// <param name="mode">A <see cref="FileMode"/> constant specifying the mode (for example, Open or Append) in which to open the file.</param>
    /// <param name="access">A <see cref="FileAccess"/> constant specifying whether to open the file with <c>Read</c>, <c>Write</c>, or <c>ReadWrite</c>
    /// file access.</param>
    /// <param name="share">A <see cref="FileShare"/> constant specifying the type of access other <c>FileStream</c> objects have to this file.</param>
    /// <param name="bufferSize">A positive value indicating the buffer size.</param>
    /// <param name="options">Additional file options.</param>
    /// <returns>A new <see cref="FileStream"/> to the opened file.</returns>
    /// <remarks>
    /// <para>Note that the underlying operating system might not support asynchronous I/O, so the handle might be opened synchronously depending on the
    /// platform.</para>
    /// </remarks>
    sealed FileStream OpenAsyncStream(FileMode mode = FileMode.Open, FileAccess access = FileAccess.ReadWrite, FileShare share = FileShare.None, int bufferSize = 4096, FileOptions options = FileOptions.None)
    {
        return OpenStream(mode, access, share, bufferSize, options | FileOptions.Asynchronous);
    }

    /// <summary>
    /// Opens a handle to a new or existing file, for use with the methods of <see cref="RandomAccess"/>.
    /// </summary>
    /// <param name="mode">A <see cref="FileMode"/> constant specifying the mode (for example, Open or Append) in which to open the file.</param>
    /// <param name="access">A <see cref="FileAccess"/> constant specifying whether to open the file with <c>Read</c>, <c>Write</c>, or <c>ReadWrite</c>
    /// file access.</param>
    /// <param name="share">A <see cref="FileShare"/> constant specifying the type of access other handles have to this file.</param>
    /// <param name="options">Additional file options.</param>
    /// <param name="preallocationSize">The initial allocation size in bytes for the file. Must be at least zero.</param>
    /// <returns>A new <see cref="SafeFileHandle"/> to the opened file.</returns>
    /// <remarks>
    /// <para>
    /// A handle has no position and no buffer: each read and write through <see cref="RandomAccess"/> names its own offset in the file and goes straight
    /// to the operating system. That makes it lighter than the <see cref="FileStream"/> that <see cref="OpenStream"/> returns where the file is read or
    /// written in a few large pieces, and heavier where it is read or written in many small ones.</para>
    /// <para>
    /// <paramref name="preallocationSize"/> is a hint that only has an effect when a file is created or overwritten with write access. The file's length
    /// stays zero until it is written to.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="preallocationSize"/> is negative, or <paramref name="mode"/>, <paramref
    /// name="access"/>, <paramref name="share"/> or <paramref name="options"/> is not a valid value.</exception>
    /// <exception cref="ArgumentException">The combination of <paramref name="mode"/>, <paramref name="access"/> and <paramref
    /// name="preallocationSize"/> is not valid, such as a mode that creates a file together with read-only access.</exception>
    SafeFileHandle OpenHandle(FileMode mode = FileMode.Open, FileAccess access = FileAccess.ReadWrite, FileShare share = FileShare.None, FileOptions options = FileOptions.None, long preallocationSize = 0);

    /// <summary>
    /// Opens an asynchronous handle to a new or existing file, for use with the methods of <see cref="RandomAccess"/> (the <see
    /// cref="FileOptions.Asynchronous"/> option is always appended).
    /// </summary>
    /// <param name="mode">A <see cref="FileMode"/> constant specifying the mode (for example, Open or Append) in which to open the file.</param>
    /// <param name="access">A <see cref="FileAccess"/> constant specifying whether to open the file with <c>Read</c>, <c>Write</c>, or <c>ReadWrite</c>
    /// file access.</param>
    /// <param name="share">A <see cref="FileShare"/> constant specifying the type of access other handles have to this file.</param>
    /// <param name="options">Additional file options.</param>
    /// <param name="preallocationSize">The initial allocation size in bytes for the file. Must be at least zero.</param>
    /// <returns>A new <see cref="SafeFileHandle"/> to the opened file.</returns>
    /// <remarks>
    /// <para>Note that the underlying operating system might not support asynchronous I/O, so the handle might be opened synchronously depending on the
    /// platform.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="preallocationSize"/> is negative, or <paramref name="mode"/>, <paramref
    /// name="access"/>, <paramref name="share"/> or <paramref name="options"/> is not a valid value.</exception>
    /// <exception cref="ArgumentException">The combination of <paramref name="mode"/>, <paramref name="access"/> and <paramref
    /// name="preallocationSize"/> is not valid, such as a mode that creates a file together with read-only access.</exception>
    sealed SafeFileHandle OpenAsyncHandle(FileMode mode = FileMode.Open, FileAccess access = FileAccess.ReadWrite, FileShare share = FileShare.None, FileOptions options = FileOptions.None, long preallocationSize = 0)
    {
        return OpenHandle(mode, access, share, options | FileOptions.Asynchronous, preallocationSize);
    }

    /// <summary>
    /// Copies the file to a new file, optionally allowing the overwriting of an existing file.
    /// </summary>
    /// <param name="destinationFile">The new file to copy to.</param>
    /// <param name="overwrite"><see langword="true"/> to allow an existing file to be overwritten; otherwise, <see langword="false"/>.</param>
    void CopyTo(IAbsoluteFilePath destinationFile, bool overwrite = false);

    /// <summary>
    /// Moves the file to a new location, optionally allowing the overwriting of an existing file.
    /// </summary>
    /// <param name="destinationFile">The new location for the file.</param>
    /// <param name="overwrite"><see langword="true"/> to allow an existing file to be overwritten; otherwise, <see langword="false"/>.</param>
    void MoveTo(IAbsoluteFilePath destinationFile, bool overwrite = false);

    /// <summary>
    /// Moves the file to a new location on the same volume in such a way that, of several attempts to move the same file at the same time, at most one
    /// succeeds.
    /// </summary>
    /// <param name="destinationFile">The new location for the file, which must be on the same volume as the file.</param>
    /// <param name="overwrite"><see langword="true"/> to allow an existing file to be overwritten; otherwise, <see langword="false"/>.</param>
    /// <remarks>
    /// <para>
    /// <see cref="MoveTo"/> does not decide between parties that move the same file to different names at the same time: on Windows each of them reports
    /// success and the file ends up under the name that was applied last. With this method one party's move succeeds and every other party's move throws,
    /// which makes a move a way to claim a file or to settle which of two changes to it happens. The guarantee is between callers of this method.</para>
    /// <para>
    /// A party that loses gets a <see cref="FileNotFoundException"/> once the file has been moved. On Windows a party whose attempt overlaps the winner's
    /// gets an <see cref="IOException"/> for a sharing violation instead. Any other handle that is open on the file without sharing deletion causes the
    /// same sharing violation there, so that exception alone does not say that the file was moved: a caller that tries again has lost when the attempt
    /// throws <see cref="FileNotFoundException"/>.</para>
    /// <para>
    /// The file is never copied, so unlike <see cref="MoveTo"/> this method throws an <see cref="IOException"/> if <paramref name="destinationFile"/> is
    /// on another volume. A symbolic link is moved itself, not its target.</para>
    /// <para>
    /// On Windows the file is opened with delete access and without sharing deletion, and is renamed through that handle. Handles that are open on the
    /// file and share deletion (<see cref="FileShare.Delete"/>), such as those of readers and of a writer appending to it, do not block the move and stay
    /// valid.</para>
    /// <para>
    /// On Unix the file is renamed with a single system call, and open handles never block the move. If <paramref name="overwrite"/> is <see
    /// langword="false"/>, an existing destination is refused as part of the rename on Linux (<c>renameat2</c> with <c>RENAME_NOREPLACE</c>). On other
    /// Unix platforms, and on file systems that do not support that (NFS, for example), the destination is checked before the rename instead, so a file
    /// that is created there between the two is replaced.</para>
    /// </remarks>
    /// <exception cref="FileNotFoundException">The file does not exist, which includes that another party has moved it.</exception>
    /// <exception cref="DirectoryNotFoundException">The file's parent directory or the parent directory of <paramref name="destinationFile"/> does not
    /// exist.</exception>
    /// <exception cref="UnauthorizedIOAccessException">Access to the file or to <paramref name="destinationFile"/> is denied.</exception>
    /// <exception cref="IOException">The file is being moved by another party or is open without sharing deletion (Windows), <paramref
    /// name="destinationFile"/> exists and <paramref name="overwrite"/> is <see langword="false"/>, <paramref name="destinationFile"/> is on another
    /// volume, or either path points to a directory.</exception>
    void MoveToExclusive(IAbsoluteFilePath destinationFile, bool overwrite = false);

    /// <summary>
    /// Replaces the contents of a file with the current file, deleting the original file and optionally creating a backup of the replaced file.
    /// </summary>
    /// <param name="destinationFile">The file to replace.</param>
    /// <param name="backupFile">The location to backup the file described by the <paramref name="destinationFile"/> parameter.</param>
    /// <param name="ignoreMetadataErrors"><see langword="true"/> to ignore merge errors (such as attributes and ACLs) from the replaced file to the replacement file;
    /// otherwise <see langword="false"/>.</param>
    void Replace(IAbsoluteFilePath destinationFile, IAbsoluteFilePath? backupFile, bool ignoreMetadataErrors = false);

    /// <summary>
    /// Deletes the file.
    /// </summary>
    /// <param name="ignoreNotFound"><see langword="true"/> to ignore file not found errors; otherwise, <see langword="false"/> to throw <see
    /// cref="FileNotFoundException"/> if the file was not found.</param>
    void Delete(bool ignoreNotFound = true);

    /// <summary>
    /// Flushes the file's data and metadata to disk, so that they survive a power loss or an operating system crash.
    /// </summary>
    /// <param name="ignoreNotFound"><see langword="true"/> to ignore file not found errors; otherwise, <see langword="false"/> to throw <see
    /// cref="FileNotFoundException"/> if the file was not found.</param>
    /// <remarks>
    /// <para>
    /// The file is opened for writing while it is flushed, because Windows only flushes a file through a handle with write access, so a file
    /// that cannot be opened for writing (such as a read-only file) cannot be flushed. Other handles to the file may be open as long as they share writing:
    /// the flush itself shares reading, writing and deletion. On Unix, where sharing is advisory, only a handle that shares nothing blocks the flush.</para>
    /// <para>
    /// Only data that the operating system has already been given is flushed. Data that is still in the buffer of an open <see cref="FileStream"/> is not,
    /// so flush or dispose the stream first, or call <see cref="FileStream.Flush(bool)"/> on it with <see langword="true"/> instead, which writes its buffer
    /// and then flushes the file to disk.</para>
    /// <para>
    /// The name of a file is an entry of its directory and is not part of the file. To make a file that was created, renamed or moved durable under its
    /// name, also call <see cref="IAbsoluteDirectoryPath.FlushToDisk(bool)"/> on its <see cref="ParentDirectory"/>.</para>
    /// <para>
    /// Unlike <see cref="Delete(bool)"/>, a file that is not found is not ignored by default: nothing was made durable, which is usually a mistake. If
    /// <paramref name="ignoreNotFound"/> is <see langword="true"/> then what <see cref="Delete(bool)"/> ignores is ignored: a file that does not exist in
    /// a parent directory that does. A parent directory that does not exist and a path that points to a directory still throw.</para>
    /// </remarks>
    /// <exception cref="FileNotFoundException">The file does not exist and <paramref name="ignoreNotFound"/> is <see langword="false"/>.</exception>
    /// <exception cref="DirectoryNotFoundException">The file's parent directory does not exist.</exception>
    /// <exception cref="UnauthorizedIOAccessException">The file cannot be opened for writing, for example because it is read-only.</exception>
    /// <exception cref="IOException">The path points to a directory, another handle to the file does not share writing, or the flush failed.</exception>
    void FlushToDisk(bool ignoreNotFound = false);

    /// <inheritdoc cref="IFilePath.WithExtension(string?, PathOptions)"/>
    new IAbsoluteFilePath WithExtension(string? extension, PathOptions options = PathOptions.NoUnfriendlyNames);

    /// <inheritdoc/>
    IFilePath IFilePath.WithExtension(string? extension, PathOptions options) => WithExtension(extension, options);

    /// <inheritdoc cref="IFilePath.AddExtension(string?, PathOptions)"/>
    new IAbsoluteFilePath AddExtension(string? extension, PathOptions options = PathOptions.NoUnfriendlyNames);

    /// <inheritdoc/>
    IFilePath IFilePath.AddExtension(string? extension, PathOptions options) => AddExtension(extension, options);

    /// <inheritdoc cref="IFilePath.AddMultiExtension(string?, PathOptions)"/>
    new IAbsoluteFilePath AddMultiExtension(string? extension, PathOptions options = PathOptions.NoUnfriendlyNames);

    /// <inheritdoc/>
    IFilePath IFilePath.AddMultiExtension(string? extension, PathOptions options) => AddMultiExtension(extension, options);

    /// <inheritdoc cref="IFilePath.RemoveExtension(string, PathOptions)"/>
    new IAbsoluteFilePath RemoveExtension(string extension, PathOptions options = PathOptions.NoUnfriendlyNames);

    /// <inheritdoc/>
    IFilePath IFilePath.RemoveExtension(string extension, PathOptions options) => RemoveExtension(extension, options);

    /// <inheritdoc cref="IFilePath.RemoveMultiExtension(string, PathOptions)"/>
    new IAbsoluteFilePath RemoveMultiExtension(string extension, PathOptions options = PathOptions.NoUnfriendlyNames);

    /// <inheritdoc/>
    IFilePath IFilePath.RemoveMultiExtension(string extension, PathOptions options) => RemoveMultiExtension(extension, options);
}
