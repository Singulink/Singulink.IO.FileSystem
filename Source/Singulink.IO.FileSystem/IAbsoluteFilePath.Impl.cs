using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

                    File.SetAttributes(PathExport, attributes);
                }
            }

            public long Length => new FileInfo(PathExport).Length;

            public override bool Exists => File.Exists(PathExport);

            public override FileAttributes Attributes {
                get {
                    var attributes = File.GetAttributes(PathExport);

                    if (attributes.HasFlag(FileAttributes.Directory))
                        throw new FileNotFoundException();

                    return attributes;
                }
                set {
                    // Ensure this is a file
                    if (!Exists)
                        throw new FileNotFoundException();

                    File.SetAttributes(PathExport, value);
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

            public FileStream OpenStream(
                FileMode mode = FileMode.Open,
                FileAccess access = FileAccess.ReadWrite,
                FileShare share = FileShare.None,
                int bufferSize = 4096,
                FileOptions options = FileOptions.None)
            {
                PathFormat.EnsureCurrent();
                return new FileStream(PathExport, mode, access, share, bufferSize, options);
            }

            public void CopyTo(IAbsoluteFilePath destinationFile, bool overwrite = false)
            {
                PathFormat.EnsureCurrent();
                destinationFile.PathFormat.EnsureCurrent(nameof(destinationFile));

                File.Copy(PathExport, destinationFile.PathExport, overwrite);
            }

            public void MoveTo(IAbsoluteFilePath destinationFile)
            {
                PathFormat.EnsureCurrent();
                destinationFile.PathFormat.EnsureCurrent(nameof(destinationFile));

                File.Move(PathExport, destinationFile.PathExport);
            }

            public void Replace(IAbsoluteFilePath destinationFile, IAbsoluteFilePath backupFile, bool ignoreMetadataErrors = false)
            {
                PathFormat.EnsureCurrent();
                destinationFile.PathFormat.EnsureCurrent(nameof(destinationFile));
                backupFile.PathFormat.EnsureCurrent(nameof(backupFile));

                File.Replace(PathExport, destinationFile.PathExport, backupFile.PathExport, ignoreMetadataErrors);
            }

            public void Delete()
            {
                PathFormat.EnsureCurrent();
                EnsureExists();
                File.Delete(PathExport);
            }

            #endregion
        }
    }
}
