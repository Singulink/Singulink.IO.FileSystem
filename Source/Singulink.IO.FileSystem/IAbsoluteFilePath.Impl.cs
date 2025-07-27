using Singulink.Enums;
using Singulink.IO.Utilities;

namespace Singulink.IO;

/// <content>
/// Contains the implementation of IAbsoluteFilePath.
/// </content>
public partial interface IAbsoluteFilePath
{
    internal new sealed class Impl(string path, int rootLength, PathFormat pathFormat) : IAbsolutePath.Impl(path, rootLength, pathFormat), IAbsoluteFilePath
    {
        public override bool HasParentDirectory => true;

        public override IAbsoluteDirectoryPath ParentDirectory
        {
            get {
                var parentPath = PathFormat.GetParentDirectoryPath(PathDisplay, RootLength);
                return new IAbsoluteDirectoryPath.Impl(parentPath.ToString(), RootLength, PathFormat);
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

        public IAbsoluteFilePath WithExtension(string? newExtension, PathOptions options)
        {
            string newPath = PathFormat.ChangeFileNameExtension(PathDisplay, newExtension, options);
            return new Impl(newPath, RootLength, PathFormat);
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
