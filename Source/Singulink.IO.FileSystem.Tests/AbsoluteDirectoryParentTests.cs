using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Singulink.IO.FileSystem.Tests
{
    [TestClass]
    public class AbsoluteDirectoryParentTests
    {
        [TestMethod]
        public void Windows()
        {
            var dir = DirectoryPath.ParseAbsolute(@"C:\test\test2\test3", PathFormat.Windows);
            Assert.IsTrue(dir.HasParentDirectory);
            Assert.AreEqual(@"C:\test\test2\test3", dir.PathDisplay);

            dir = dir.ParentDirectory!;
            Assert.IsTrue(dir.HasParentDirectory);
            Assert.AreEqual(@"C:\test\test2", dir.PathDisplay);

            dir = dir.ParentDirectory!;
            Assert.IsTrue(dir.HasParentDirectory);
            Assert.AreEqual(@"C:\test", dir.PathDisplay);

            dir = dir.ParentDirectory!;
            Assert.IsFalse(dir.HasParentDirectory);
            Assert.AreEqual(@"C:\", dir.PathDisplay);
            Assert.IsNull(dir.ParentDirectory);
        }

        [TestMethod]
        public void Unix()
        {
            var dir = DirectoryPath.ParseAbsolute("/test/test2/test3", PathFormat.Unix);
            Assert.IsTrue(dir.HasParentDirectory);
            Assert.AreEqual("/test/test2/test3", dir.PathDisplay);

            dir = dir.ParentDirectory!;
            Assert.IsTrue(dir.HasParentDirectory);
            Assert.AreEqual("/test/test2", dir.PathDisplay);

            dir = dir.ParentDirectory!;
            Assert.IsTrue(dir.HasParentDirectory);
            Assert.AreEqual("/test", dir.PathDisplay);

            dir = dir.ParentDirectory!;
            Assert.IsFalse(dir.HasParentDirectory);
            Assert.AreEqual("/", dir.PathDisplay);
            Assert.IsNull(dir.ParentDirectory);
        }
    }
}
