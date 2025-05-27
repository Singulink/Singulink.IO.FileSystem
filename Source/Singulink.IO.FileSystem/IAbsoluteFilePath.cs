using System;
using System.IO;
using System.Threading.Tasks;

namespace Singulink.IO;

/// <summary>
/// Represents an absolute path to a file.
/// </summary>
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

    #region Path Manipulation

    /// <inheritdoc cref="IFilePath.WithExtension(string?, PathOptions)"/>
    new IAbsoluteFilePath WithExtension(string? newExtension, PathOptions options = PathOptions.NoUnfriendlyNames);

    /// <inheritdoc/>
    IFilePath IFilePath.WithExtension(string? newExtension, PathOptions options) => WithExtension(newExtension, options);

    #endregion

    #region File System Operations

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
    /// Opens an asynchronous file stream to a new or existing file (the <see cref="FileOptions.Asynchronous"/> option is always appended).
    /// </summary>
    /// <param name="mode">A <see cref="FileMode"/> constant specifying the mode (for example, Open or Append) in which to open the file.</param>
    /// <param name="access">A <see cref="FileAccess"/> constant specifying whether to open the file with <c>Read</c>, <c>Write</c>, or <c>ReadWrite</c>
    /// file access.</param>
    /// <param name="share">A <see cref="FileShare"/> constant specifying the type of access other <c>FileStream</c> objects have to this file.</param>
    /// <param name="bufferSize">A positive value indicating the buffer size.</param>
    /// <param name="options">Additional file options.</param>
    /// <returns>A task with a new <see cref="FileStream"/> to the opened file.</returns>
    /// <remarks>
    /// <para>Note that the underlying operating system might not support asynchronous I/O, so the handle might be opened synchronously depending on the
    /// platform.</para>
    /// </remarks>
    sealed Task<FileStream> OpenStreamAsync(FileMode mode = FileMode.Open, FileAccess access = FileAccess.ReadWrite, FileShare share = FileShare.None, int bufferSize = 4096, FileOptions options = FileOptions.None)
    {
        return Task.Run(() => OpenAsyncStream(mode, access, share, bufferSize, options));
    }

    /// <summary>
    /// Copies the file to a new file, optionally allowing the overwriting of an existing file.
    /// </summary>
    /// <param name="destinationFile">The new file to copy to.</param>
    /// <param name="overwrite">True to allow an existing file to be overwritten, otherwise false.</param>
    void CopyTo(IAbsoluteFilePath destinationFile, bool overwrite = false);

    /// <summary>
    /// Moves the file to a new location, optionally allowing the overwriting of an existing file. Overwriting is only supported on .NET Core 3+ only,
    /// other runtimes (i.e. Mono/Xamarin) will throw <see cref="NotSupportedException"/>).
    /// </summary>
    /// <param name="destinationFile">The new location for the file.</param>
    /// <param name="overwrite">True to allow an existing file to be overwritten, otherwise false.</param>
    void MoveTo(IAbsoluteFilePath destinationFile, bool overwrite = false);

    /// <summary>
    /// Replaces the contents of a file with the current file, deleting the original file and creating a backup of the replaced file.
    /// </summary>
    /// <param name="destinationFile">The file to replace.</param>
    /// <param name="backupFile">The location to backup the file described by the <paramref name="destinationFile"/> parameter.</param>
    /// <param name="ignoreMetadataErrors">True to ignore merge errors (such as attributes and ACLs) from the replaced file to the replacement file;
    /// otherwise false.</param>
    void Replace(IAbsoluteFilePath destinationFile, IAbsoluteFilePath backupFile, bool ignoreMetadataErrors = false);

    /// <summary>
    /// Deletes the file.
    /// </summary>
    void Delete();

    #endregion
}