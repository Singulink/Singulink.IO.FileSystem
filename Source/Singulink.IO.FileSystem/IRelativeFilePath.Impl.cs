using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Singulink.IO.Utilities;

namespace Singulink.IO
{
    /// <content>
    /// Contains an implementation of IRelativeFilePath.
    /// </content>
    public partial interface IRelativeFilePath
    {
        internal new sealed class Impl : IRelativePath.Impl, IRelativeFilePath
        {
            internal Impl(string path, int rootLength, PathFormat pathFormat) : base(path, rootLength, pathFormat)
            {
            }

            public string NameWithoutExtension => PathFormat.GetFileNameWithoutExtension(PathDisplay);

            public string Extension => PathFormat.GetFileNameExtension(PathDisplay);

            public IRelativeDirectoryPath ParentDirectory {
                get {
                    var parentPath = PathFormat.GetPreviousDirectory(PathDisplay, RootLength);
                    return new IRelativeDirectoryPath.Impl(parentPath.ToString(), RootLength, PathFormat);
                }
            }

            public IRelativeFilePath WithExtension(string? newExtension, PathOptions options)
            {
                string newPath = PathFormat.ChangeFileNameExtension(PathDisplay, newExtension, options);
                return new Impl(newPath, RootLength, PathFormat);
            }

            #region Path Format Conversion

            public IRelativeFilePath ToPathFormat(PathFormat format, PathOptions options)
            {
                var path = PathFormat.ConvertRelativePathFormat(PathDisplay, PathFormat, format);
                return FilePath.ParseRelative(path.Span, format, options);
            }

            #endregion
        }
    }
}
