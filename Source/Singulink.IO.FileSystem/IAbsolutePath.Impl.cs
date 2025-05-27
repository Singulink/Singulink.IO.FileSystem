using System;
using System.IO;

namespace Singulink.IO;

/// <content>
/// Contains an implementation of IAbsoluteEntryPath.
/// </content>
public partial interface IAbsolutePath
{
    internal new abstract class Impl : IPath.Impl, IAbsolutePath
    {
        protected Impl(string path, int rootLength, PathFormat pathFormat) : base(path, rootLength, pathFormat)
        {
        }

        public string PathExport => PathFormat.GetAbsolutePathExportString(PathDisplay);

        public bool IsUnc => PathFormat.IsUncPath(PathDisplay);

        public abstract bool Exists { get; }

        public abstract FileAttributes Attributes { get; set; }

        public IAbsoluteDirectoryPath RootDirectory {
            get {
                if (PathDisplay.Length == RootLength && this is IAbsoluteDirectoryPath dir)
                    return dir;

                return new IAbsoluteDirectoryPath.Impl(PathDisplay[..RootLength], RootLength, PathFormat);
            }
        }

        public abstract IAbsoluteDirectoryPath? ParentDirectory { get; }

        public DateTime CreationTime {
            get {
                EnsureExists();
                return File.GetCreationTime(PathExport);
            }
            set {
                EnsureExists();
                File.SetCreationTime(PathExport, value);
            }
        }

        public DateTime LastAccessTime {
            get {
                EnsureExists();
                return File.GetLastAccessTime(PathExport);
            }
            set {
                EnsureExists();
                File.SetLastAccessTime(PathExport, value);
            }
        }

        public DateTime LastWriteTime {
            get {
                EnsureExists();
                return File.GetLastWriteTime(PathExport);
            }
            set {
                EnsureExists();
                File.SetLastWriteTime(PathExport, value);
            }
        }

        public abstract IAbsoluteDirectoryPath GetLastExistingDirectory();

        /// <summary>
        /// This method is necessary before calling some operations because they work on both files and directories.
        /// </summary>
        /// <remarks>
        /// <para>Examples of problematic methods: File.GetLastWriteTime / Directory.GetLastWriteTime (plus all other timestamp methods).</para>
        /// </remarks>
        internal void EnsureExists() => _ = Attributes;
    }
}