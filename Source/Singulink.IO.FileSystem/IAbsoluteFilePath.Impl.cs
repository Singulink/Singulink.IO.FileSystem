using System;
using System.IO;
using System.Reflection;
using Singulink.IO.Utilities;

namespace Singulink.IO
{
    /// <content>
    /// Contains an implementation of IAbsoluteFilePath.
    /// </content>
    public partial interface IAbsoluteFilePath
    {
        internal new sealed class Impl : IAbsolutePath.Impl, IAbsoluteFilePath
        {
            private static MethodInfo? _moveWithOverwriteMethod;

            internal Impl(string path, int rootLength, PathFormat pathFormat) : base(path, rootLength, pathFormat)
            {
            }

            public string NameWithoutExtension => PathFormat.GetFileNameWithoutExtension(PathDisplay);

            public string Extension => PathFormat.GetFileNameExtension(PathDisplay);

            public bool IsReadOnly {
                get => (Attributes & FileAttributes.ReadOnly) != 0;
                set {
                    var attributes = Attributes;
                    bool isReadOnly = (attributes & FileAttributes.ReadOnly) != 0;

                    if (value == isReadOnly)
                        return;

                    if (value)
                        attributes |= FileAttributes.ReadOnly;
                    else
                        attributes &= ~FileAttributes.ReadOnly;

                    try {
                        File.SetAttributes(PathExport, attributes);
                    }
                    catch (UnauthorizedAccessException ex) {
                        throw Ex.Convert(ex);
                    }
                }
            }

            public long Length {
                get {
                    PathFormat.EnsureCurrent();
                    return new FileInfo(PathExport).Length;
                }
            }

            public override bool Exists {
                get {
                    PathFormat.EnsureCurrent();
                    return File.Exists(PathExport); // Only returns true for actual files, not dirs
                }
            }

            public override FileAttributes Attributes {
                get {
                    PathFormat.EnsureCurrent();
                    FileAttributes attributes;

                    try {
                        attributes = File.GetAttributes(PathExport); // Works for both files and dirs
                    }
                    catch (UnauthorizedAccessException ex) {
                        throw Ex.Convert(ex);
                    }

                    if (attributes.HasFlag(FileAttributes.Directory))
                        throw Ex.NotFound(this);

                    return attributes;
                }
                set {
                    var current = Attributes; // Ensures that this is a file

                    if (current != value) {
                        try {
                            File.SetAttributes(PathExport, value); // Works for both files and dirs
                        }
                        catch (UnauthorizedAccessException ex) {
                            throw Ex.Convert(ex);
                        }
                    }
                }
            }

            public override IAbsoluteDirectoryPath ParentDirectory {
                get {
                    var parentPath = PathFormat.GetPreviousDirectory(PathDisplay, RootLength);
                    return new IAbsoluteDirectoryPath.Impl(parentPath.ToString(), RootLength, PathFormat);
                }
            }

            #region Path Manipulation

            /// <summary>
            /// Gets a new file with the extension changed to the new extension.
            /// </summary>
            /// <param name="newExtension">The new extension that the file name should be changed to.</param>
            /// <param name="options">The path options to apply to the new file name.</param>
            /// <remarks>
            /// <para>The path options are only applied to the new file name and not the rest of the path.</para>
            /// </remarks>
            public IAbsoluteFilePath WithExtension(string? newExtension, PathOptions options)
            {
                string newPath = PathFormat.ChangeFileNameExtension(PathDisplay, newExtension, options);
                return new Impl(newPath, RootLength, PathFormat);
            }

            #endregion

            #region File System Operations

            public FileStream OpenStream(FileMode mode = FileMode.Open, FileAccess access = FileAccess.ReadWrite, FileShare share = FileShare.None, int bufferSize = 4096, FileOptions options = FileOptions.None)
            {
                PathFormat.EnsureCurrent();

                try {
                    // Throws UnauthorizedAccessException with a nonsense message if file is a dir, convert to FileNotFoundEx.
                    return new FileStream(PathExport, mode, access, share, bufferSize, options);
                }
                catch (UnauthorizedAccessException ex) {
                    ThrowNotFoundIfFileIsDir(this);
                    throw Ex.Convert(ex);
                }
            }

            public void CopyTo(IAbsoluteFilePath destinationFile, bool overwrite = false)
            {
                PathFormat.EnsureCurrent();
                destinationFile.PathFormat.EnsureCurrent(nameof(destinationFile));

                try {
                    // If this path is dir UnauthorizedAccessEx is thrown with nonsense message. Convert to FileNotFoundEx.
                    // If destination is dir IOEx is thrown with "The target file is a directory, not a file." message, which is fine.
                    File.Copy(PathExport, destinationFile.PathExport, overwrite);
                }
                catch (UnauthorizedAccessException ex) {
                    ThrowNotFoundIfFileIsDir(this);
                    throw Ex.Convert(ex);
                }
            }

            public void MoveTo(IAbsoluteFilePath destinationFile, bool overwrite = false)
            {
                PathFormat.EnsureCurrent();
                destinationFile.PathFormat.EnsureCurrent(nameof(destinationFile));

                try {
                    // If this path is dir FileNotFoundEx is thrown.
                    // If destination is dir then IOEx is thrown with "file already exists" message, except on Windows when overwrite is true, in which case
                    // UnauthorizedAccessEx is thrown. Change that to IOEx for consistency.

                    if (overwrite)
                        GetOverwriteMethod().Invoke(null, new object[] { PathExport, destinationFile.PathExport, true });
                    else
                        File.Move(PathExport, destinationFile.PathExport);
                }
                catch (UnauthorizedAccessException ex) {
                    if (PathFormat == PathFormat.Windows && overwrite)
                        ThrowIfFileIsDir(destinationFile);

                    throw Ex.Convert(ex);
                }

                static MethodInfo GetOverwriteMethod()
                {
                    // Netstandard does not have a File.Move method with an overwrite flag. Use this temp hack until we move the lib off of netstandard.

                    if (_moveWithOverwriteMethod == null) {
                        _moveWithOverwriteMethod = typeof(File).GetMethod("Move", new[] { typeof(string), typeof(string), typeof(bool) });

                        if (_moveWithOverwriteMethod == null)
                            throw new NotSupportedException("Moving a file while overwriting the existing file is not supported on this .NET runtime.");
                    }

                    return _moveWithOverwriteMethod;
                }
            }

            public void Replace(IAbsoluteFilePath destinationFile, IAbsoluteFilePath? backupFile, bool ignoreMetadataErrors = false)
            {
                PathFormat.EnsureCurrent();
                destinationFile.PathFormat.EnsureCurrent(nameof(destinationFile));
                backupFile?.PathFormat.EnsureCurrent(nameof(backupFile));

                // Windows throws IOEx if files are the same, Unix does not. Check if they are the same file paths for better consistency here.

                if (PathDisplay == destinationFile.PathDisplay)
                    throw new IOException("Source and destination file paths are the same.");

                // Unix File.Replace works on directories so we need to guard against this.

                if (PathFormat == PathFormat.Unix)
                    EnsureExists();

                try {
                    // If this path is a dir Windows throws UnauthorizedAccessEx. Change to FileNotFoundEx for consistency. We guard against this on Unix
                    // above with EnsureExists() which throws FileNotFoundEx as well.
                    // If destinationFile path is a dir Unix throws IOEx, Windows throws UnauthorizedAccessEx. Change both to FileNotFoundEx.
                    // If backupFile is a dir Unix throws IOEx, Windows throws UnauthorizedAccessEx. Change windows to IOEx for consistency.
                    File.Replace(PathExport, destinationFile.PathExport, backupFile?.PathExport, ignoreMetadataErrors);
                }
                catch (UnauthorizedAccessException ex) {
                    if (PathFormat == PathFormat.Windows) {
                        ThrowNotFoundIfFileIsDir(this);
                        ThrowNotFoundIfFileIsDir(destinationFile);

                        if (backupFile != null)
                            ThrowIfFileIsDir(backupFile);
                    }

                    throw Ex.Convert(ex);
                }
                catch (IOException ex) when (PathFormat == PathFormat.Unix && ex.GetType() == typeof(IOException)) {
                    ThrowNotFoundIfFileIsDir(destinationFile);
                    throw;
                }
            }

            public void Delete()
            {
                PathFormat.EnsureCurrent();

                try {
                    // Throws UnauthorizedAccessException with a nonsense message if file is a dir.
                    File.Delete(PathExport);
                }
                catch (UnauthorizedAccessException ex) {
                    // File.Delete does not throw if the file is not found so makes more sense not to throw if the path is a dir.
                    if (IsKnownToBeDir(this))
                        return;

                    throw Ex.Convert(ex);
                }
            }

            public override IAbsoluteDirectoryPath GetLastExistingDirectory() => ParentDirectory.GetLastExistingDirectory();

            private static bool IsKnownToBeDir(IAbsoluteFilePath path)
            {
                try {
                    if ((File.GetAttributes(path.PathExport) & FileAttributes.Directory) != 0)
                        return true;
                }
                catch { }

                return false;
            }

            private static void ThrowNotFoundIfFileIsDir(IAbsoluteFilePath path)
            {
                if (IsKnownToBeDir(path))
                    throw Ex.NotFound(path);
            }

            private static void ThrowIfFileIsDir(IAbsoluteFilePath path)
            {
                if (IsKnownToBeDir(path))
                    throw Ex.FileIsDir(path);
            }

            #endregion
        }
    }
}
