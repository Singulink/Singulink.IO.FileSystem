using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.Win32.SafeHandles;
using Singulink.IO.Json;

namespace Singulink.IO;

/// <content>
/// Contains the implementation of IAbsoluteFilePath.
/// </content>
public partial interface IAbsoluteFilePath
{
    [JsonConverter(typeof(AbsoluteFilePathJsonConverter))]
    internal new sealed class Impl(string path, int rootLength, PathFormat pathFormat, int validity)
        : IAbsolutePath.Impl(path, rootLength, pathFormat, validity), IAbsoluteFilePath
    {
        public override bool HasParentDirectory => true;

        public override IAbsoluteDirectoryPath ParentDirectory
        {
            get {
                var parentPath = PathFormat.GetParentDirectoryPath(PathDisplay, RootLength);
                return new IAbsoluteDirectoryPath.Impl(parentPath.ToString(), RootLength, PathFormat, InheritedValidity);
            }
        }

        public bool IsReadOnly
        {
            get => Attributes.HasAllFlags(FileAttributes.ReadOnly);
            set {
                var attributes = Attributes;
                bool isReadOnly = attributes.HasAllFlags(FileAttributes.ReadOnly);

                if (value == isReadOnly)
                    return;

                if (value)
                    attributes = attributes.SetFlags(FileAttributes.ReadOnly);
                else
                    attributes = attributes.ClearFlags(FileAttributes.ReadOnly);

                try
                {
                    File.SetAttributes(PathExport, attributes);
                }
                catch (UnauthorizedAccessException ex)
                {
                    throw Ex.Convert(ex);
                }
            }
        }

        public long Length
        {
            get {
                PathFormat.EnsureCurrent();
                return new FileInfo(PathExport).Length;
            }
        }

        public override bool Exists
        {
            get {
                PathFormat.EnsureCurrent();
                return File.Exists(PathExport); // Only returns true for actual files, not dirs
            }
        }

        public override EntryState State
        {
            get {
                PathFormat.EnsureCurrent();

                try
                {
                    return File.GetAttributes(PathExport).HasAllFlags(FileAttributes.Directory) ? EntryState.WrongType : EntryState.Exists;
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

        public override FileAttributes Attributes
        {
            get {
                PathFormat.EnsureCurrent();
                FileAttributes attributes;

                try
                {
                    attributes = File.GetAttributes(PathExport); // Works for both files and dirs
                }
                catch (UnauthorizedAccessException ex)
                {
                    throw Ex.Convert(ex);
                }

                if (attributes.HasAllFlags(FileAttributes.Directory))
                    throw Ex.NotFound(this);

                return attributes;
            }
            set {
                var current = Attributes; // Ensures that this is a file

                if (current != value)
                {
                    try
                    {
                        File.SetAttributes(PathExport, value); // Works for both files and dirs
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        throw Ex.Convert(ex);
                    }
                }
            }
        }

        #region Path Manipulation

        public IAbsoluteFilePath WithExtension(string? extension, PathOptions options)
        {
            string newPath = PathFormat.ChangeFileNameExtension(PathDisplay, extension, RootLength, options);

            if (newPath is null)
                return this;

            return new Impl(newPath, RootLength, PathFormat, InheritedValidity & PathValidity.FromNewFileName(PathFormat, options));
        }

        public IAbsoluteFilePath AddExtension(string? extension, PathOptions options) => AddExtensionCore(extension, options, allowMulti: false);

        public IAbsoluteFilePath AddMultiExtension(string? extension, PathOptions options) => AddExtensionCore(extension, options, allowMulti: true);

        public IAbsoluteFilePath RemoveExtension(string extension, PathOptions options) => RemoveExtensionCore(extension, options, allowMulti: false);

        public IAbsoluteFilePath RemoveMultiExtension(string extension, PathOptions options) => RemoveExtensionCore(extension, options, allowMulti: true);

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

        private protected override bool TryParseAgain(PathOptions options, [NotNullWhen(false)] out string? error)
        {
            return FilePath.TryParseAbsolute(PathDisplay, PathFormat, options, out _, out error);
        }

        #endregion

        #region File System Operations

        public override CachedFileInfo GetInfo()
        {
            PathFormat.EnsureCurrent();
            return new CachedFileInfo(new FileInfo(PathExport), this);
        }

        public FileStream OpenStream(FileMode mode = FileMode.Open, FileAccess access = FileAccess.ReadWrite, FileShare share = FileShare.None, int bufferSize = 4096, FileOptions options = FileOptions.None)
        {
            PathFormat.EnsureCurrent();

            try
            {
                // Throws UnauthorizedAccessException with a nonsense message if file is a dir, convert to IOEx.
                return new FileStream(PathExport, mode, access, share, bufferSize, options);
            }
            catch (UnauthorizedAccessException ex)
            {
                ThrowIfFileIsDir(this);
                throw Ex.Convert(ex);
            }
        }

        public SafeFileHandle OpenHandle(FileMode mode = FileMode.Open, FileAccess access = FileAccess.ReadWrite, FileShare share = FileShare.None, FileOptions options = FileOptions.None, long preallocationSize = 0)
        {
            PathFormat.EnsureCurrent();

            try
            {
                // Throws UnauthorizedAccessException with a nonsense message if file is a dir, convert to IOEx.
                return File.OpenHandle(PathExport, mode, access, share, options, preallocationSize);
            }
            catch (UnauthorizedAccessException ex)
            {
                ThrowIfFileIsDir(this);
                throw Ex.Convert(ex);
            }
        }

        public void CopyTo(IAbsoluteFilePath destinationFile, bool overwrite = false)
        {
            PathFormat.EnsureCurrent();
            destinationFile.PathFormat.EnsureCurrent(nameof(destinationFile));

            try
            {
                // If this path is dir then UnauthorizedAccessEx is thrown with nonsense message. Convert to IOEx.
                // If destination is dir then IOEx is thrown with message "The target file is a directory, not a file".
                File.Copy(PathExport, destinationFile.PathExport, overwrite);
            }
            catch (UnauthorizedAccessException ex)
            {
                ThrowIfFileIsDir(this);
                throw Ex.Convert(ex);
            }
        }

        public void MoveTo(IAbsoluteFilePath destinationFile, bool overwrite = false)
        {
            PathFormat.EnsureCurrent();
            destinationFile.PathFormat.EnsureCurrent(nameof(destinationFile));

            try
            {
                // If this path is dir FileNotFoundEx is thrown. Convert to IOEx.
                // If destination is dir then IOEx is thrown with "file already exists" message, except on Windows when overwrite is true, in which case
                // UnauthorizedAccessEx is thrown. Change that to IOEx for consistency.

                File.Move(PathExport, destinationFile.PathExport, overwrite);
            }
            catch (FileNotFoundException)
            {
                ThrowIfFileIsDir(this);
                throw;
            }
            catch (UnauthorizedAccessException ex)
            {
                if (PathFormat == PathFormat.Windows && overwrite)
                    ThrowIfFileIsDir(destinationFile);

                throw Ex.Convert(ex);
            }
        }

        public void MoveToExclusive(IAbsoluteFilePath destinationFile, bool overwrite = false)
        {
            PathFormat.EnsureCurrent();
            destinationFile.PathFormat.EnsureCurrent(nameof(destinationFile));

            try
            {
                if (PathFormat == PathFormat.Windows)
                {
                    Interop.Windows.MoveFileExclusive(this, destinationFile, overwrite);
                    return;
                }

                // A rename moves a dir as readily as a file on Unix, so check that this is a file first. Getting the state allocates there, so that is only
                // done to find out what is wrong, which is then reported the way Delete() does.
                if (!File.Exists(PathExport))
                {
                    var state = State;

                    if (state is EntryState.WrongType)
                        throw Ex.FileIsDir(this);
                    else if (state is EntryState.ParentExists)
                        throw Ex.NotFound(this);
                    else if (state is EntryState.ParentDoesNotExist)
                        throw Ex.NotFound(ParentDirectory);
                }

                Interop.Unix.MoveFileExclusive(this, destinationFile, overwrite);
            }
            catch (UnauthorizedIOAccessException) when (PathFormat == PathFormat.Windows)
            {
                // Windows denies access if this path is a dir, and if the destination is one and may be overwritten. Always throw IOEx for a dir.
                ThrowIfFileIsDir(this);

                if (overwrite)
                    ThrowIfFileIsDir(destinationFile);

                throw;
            }
        }

        public void Replace(IAbsoluteFilePath destinationFile, IAbsoluteFilePath? backupFile, bool ignoreMetadataErrors = false)
        {
            PathFormat.EnsureCurrent();
            destinationFile.PathFormat.EnsureCurrent(nameof(destinationFile));
            backupFile?.PathFormat.EnsureCurrent(nameof(backupFile));

            try
            {
                // If this path is a dir then UnauthorizedAccessEx is thrown. Convert to IOEx.
                // If destinationFile path is a dir UnauthorizedAccessEx is thrown. Convert to IOEx.
                // If backupFile is a dir Unix throws IOEx, Windows throws UnauthorizedAccessEx. Convert windows to IOEx for consistency.
                File.Replace(PathExport, destinationFile.PathExport, backupFile?.PathExport, ignoreMetadataErrors);
            }
            catch (UnauthorizedAccessException ex)
            {
                ThrowIfFileIsDir(this);
                ThrowIfFileIsDir(destinationFile);

                if (PathFormat == PathFormat.Windows && backupFile is not null)
                    ThrowIfFileIsDir(backupFile);

                throw Ex.Convert(ex);
            }
        }

        public void Delete(bool ignoreNotFound = true)
        {
            PathFormat.EnsureCurrent();

            // File.Delete() does not throw if the file does not exist, which is why the extra IO call below is needed.
            // TODO: avoid the extra call if/when this available: https://github.com/dotnet/runtime/issues/117853

            if (!ignoreNotFound)
            {
                var state = State;

                if (state is EntryState.WrongType)
                    throw Ex.FileIsDir(this);
                else if (state is EntryState.ParentExists)
                    throw Ex.NotFound(this);
                else if (state is EntryState.ParentDoesNotExist)
                    throw Ex.NotFound(ParentDirectory);
            }

            try
            {
                // If file is a dir then UnauthorizedAccessEx is thrown with a nonsense message. Convert to IOEx.
                File.Delete(PathExport);
            }
            catch (UnauthorizedAccessException ex)
            {
                ThrowIfFileIsDir(this);
                throw Ex.Convert(ex);
            }
        }

        public void FlushToDisk(bool ignoreNotFound = false)
        {
            PathFormat.EnsureCurrent();

            try
            {
                // Write access is required because FlushFileBuffers needs it on Windows. Read access is not requested, so handles that do not share reading
                // do not block the flush, and everything is shared so that the flush gets in the way of other handles as little as possible.
                // If file is a dir then UnauthorizedAccessEx is thrown with a nonsense message. Convert to IOEx.
                using var handle = File.OpenHandle(PathExport, FileMode.Open, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
                RandomAccess.FlushToDisk(handle);
            }
            catch (FileNotFoundException) when (ignoreNotFound)
            {
                // Thrown only if the parent dir exists and the file does not, which is what Delete() ignores as well. A missing parent dir throws
                // DirectoryNotFoundEx, from both.
            }
            catch (UnauthorizedAccessException ex)
            {
                ThrowIfFileIsDir(this);
                throw Ex.Convert(ex);
            }
        }

        public override IAbsoluteDirectoryPath GetLastExistingDirectory() => ParentDirectory.GetLastExistingDirectory();

        internal override void EnsureExists()
        {
            if (!File.Exists(PathExport))
                throw Ex.NotFound(this);
        }

        private static void ThrowIfFileIsDir(IAbsoluteFilePath file)
        {
            if (IsKnownToBeDir(file))
                throw Ex.FileIsDir(file);
        }

        private static bool IsKnownToBeDir(IAbsoluteFilePath file)
        {
            try
            {
                return file.State == EntryState.WrongType;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}
