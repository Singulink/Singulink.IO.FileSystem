using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#pragma warning disable SA1122 // Use string.Empty for empty strings

namespace Singulink.IO.FileSystem.Tests
{
    [TestClass]
    public class RelativeDirectoryParentTests
    {
        [TestMethod]
        public void SpecialCurrent()
        {
            var dir = DirectoryPath.ParseRelative("", PathOptions.None);
            Assert.IsTrue(dir.HasParentDirectory);
            Assert.AreEqual("..", dir.ParentDirectory!.PathDisplay);

            dir = DirectoryPath.ParseRelative(".", PathOptions.None);
            Assert.IsTrue(dir.HasParentDirectory);
            Assert.AreEqual("..", dir.ParentDirectory!.PathDisplay);
        }

        [TestMethod]
        public void SpecialParent()
        {
            var dir = DirectoryPath.ParseRelative("..", PathFormat.Windows, PathOptions.None);
            Assert.IsTrue(dir.HasParentDirectory);
            Assert.AreEqual(@"..\..", dir.ParentDirectory!.PathDisplay);

            dir = DirectoryPath.ParseRelative("../..", PathFormat.Unix, PathOptions.None);
            Assert.IsTrue(dir.HasParentDirectory);
            Assert.AreEqual("../../..", dir.ParentDirectory!.PathDisplay);
        }

        [TestMethod]
        public void Rooted()
        {
            var dir = DirectoryPath.ParseRelative("/", PathFormat.Windows, PathOptions.None);
            Assert.IsFalse(dir.HasParentDirectory);
            Assert.IsNull(dir.ParentDirectory);

            dir = DirectoryPath.ParseRelative("/test", PathFormat.Windows, PathOptions.None);
            Assert.IsTrue(dir.HasParentDirectory);

            dir = dir.ParentDirectory!;
            Assert.AreEqual(@"\", dir.PathDisplay);
            Assert.IsFalse(dir.HasParentDirectory);
        }

        [TestMethod]
        public void NavigatingPastEmpty()
        {
            var dir = DirectoryPath.ParseRelative("dir1/dir2", PathFormat.Universal, PathOptions.None);
            Assert.AreEqual("dir1/dir2", dir.PathDisplay);

            var parent = dir.ParentDirectory!;
            Assert.AreEqual("dir1", parent.PathDisplay);

            parent = parent.ParentDirectory!;
            Assert.AreEqual("", parent.PathDisplay);

            parent = parent.ParentDirectory!;
            Assert.AreEqual("..", parent.PathDisplay);

            parent = parent.ParentDirectory!;
            Assert.AreEqual("../..", parent.PathDisplay);

            parent = parent.ParentDirectory!;
            Assert.AreEqual("../../..", parent.PathDisplay);
        }
    }
}
