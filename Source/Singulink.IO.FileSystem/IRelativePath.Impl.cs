using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Singulink.IO.Utilities;

namespace Singulink.IO
{
    /// <content>
    /// Contains an implementation of IRelativeEntryPath.
    /// </content>
    public partial interface IRelativePath
    {
        internal new abstract class Impl : IPath.Impl, IRelativePath
        {
            protected Impl(string path, int rootLength, PathFormat pathFormat) : base(path, rootLength, pathFormat)
            {
            }
        }
    }
}