using System;
using System.IO;
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
                        throw Error.ConvertException(ex);
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
                        throw Error.ConvertException(ex);
                    }

                    if (attributes.HasFlag(FileAttributes.Directory))
                        throw Error.NotFoundException(this);

                    return attributes;
                }
                set {
                    var current = Attributes; // Ensures that this is a file

                    if (current != value) {
                        try {
                            File.SetAttributes(PathExport, value); // Works for both files and dirs
                        }
                        catch (UnauthorizedAccessException ex) {
                            throw Error.ConvertException(ex);
                        }
                    }
                }
            }

            public IAbsoluteDirectoryPath ParentDirectory {
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
                    return new FileStream(PathExport, mode, access, share, bufferSize, options);
                }
                catch (UnauthorizedAccessException ex) {
                    throw Error.ConvertException(ex);
                }
            }

            public void CopyTo(IAbsoluteFilePath destinationFile, bool overwrite = false)
            {
                PathFormat.EnsureCurrent();
                destinationFile.PathFormat.EnsureCurrent(nameof(destinationFile));

                try {
                    File.Copy(PathExport, destinationFile.PathExport, overwrite);
                }
                catch (UnauthorizedAccessException ex) {
                    throw Error.ConvertException(ex);
                }
            }

            public void MoveTo(IAbsoluteFilePath destinationFile)
            {
                PathFormat.EnsureCurrent();
                destinationFile.PathFormat.EnsureCurrent(nameof(destinationFile));

                try {
                    File.Move(PathExport, destinationFile.PathExport);
                }
                catch (UnauthorizedAccessException ex) {
                    throw Error.ConvertException(ex);
                }
            }

            public void Replace(IAbsoluteFilePath destinationFile, IAbsoluteFilePath backupFile, bool ignoreMetadataErrors = false)
            {
                PathFormat.EnsureCurrent();
                destinationFile.PathFormat.EnsureCurrent(nameof(destinationFile));
                backupFile.PathFormat.EnsureCurrent(nameof(backupFile));

                try {
                    File.Replace(PathExport, destinationFile.PathExport, backupFile.PathExport, ignoreMetadataErrors);
                }
                catch (UnauthorizedAccessException ex) {
                    throw Error.ConvertException(ex);
                }
            }

            public void Delete()
            {
                PathFormat.EnsureCurrent();

                try {
                    File.Delete(PathExport);
                }
                catch (UnauthorizedAccessException ex) {
                    ThrowExceptionIfFileIsDir(this);
                    throw Error.ConvertException(ex);
                }
            }

            public IAbsoluteDirectoryPath GetLastExistingDirectory() => ParentDirectory.GetLastExistingDirectory();

            IAbsoluteDirectoryPath IAbsolutePath.GetLastExistingDirectory() => GetLastExistingDirectory();

            private static void ThrowExceptionIfFileIsDir(IAbsoluteFilePath path)
            {
                try {
                    if ((File.GetAttributes(path.PathExport) & FileAttributes.Directory) == 0)
                        return;
                }
                catch {
                    return;
                }

                throw Error.FileIsDirException(path);
            }

            #endregion
        }
    }
}
