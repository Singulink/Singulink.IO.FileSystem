using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Singulink.IO.FileSystem.Tests
{
    [TestClass]
    public class AbsoluteDirectoryCombineTests
    {
        [TestMethod]
        public void NavigateWindows()
        {
            var dir = DirectoryPath.ParseAbsolute(@"C:\dir1\dir2", PathFormat.Windows, PathOptions.None);
            var combined = dir.CombineDirectory("../", PathOptions.None);
            Assert.AreEqual(@"C:\dir1", combined.PathDisplay);
            Assert.IsFalse(combined.IsRoot);

            combined = dir.CombineDirectory("../../", PathOptions.None);
            Assert.AreEqual(@"C:\", combined.PathDisplay);
            Assert.IsTrue(combined.IsRoot);

            combined = dir.CombineDirectory(".", PathOptions.None);
            Assert.AreEqual(@"C:\dir1\dir2", combined.PathDisplay);
        }

        [TestMethod]
        public void NavigateRootedWindows()
        {
            var dir = DirectoryPath.ParseAbsolute(@"C:\dir1\dir2", PathFormat.Windows, PathOptions.None);

            var combined = dir.CombineDirectory("/", PathOptions.None);
            Assert.AreEqual(@"C:\", combined.PathDisplay);

            combined = dir.CombineDirectory("/test", PathOptions.None);
            Assert.AreEqual(@"C:\test", combined.PathDisplay);
        }

        [TestMethod]
        public void NavigateUnix()
        {
            var dir = DirectoryPath.ParseAbsolute("/dir1/dir2", PathFormat.Unix, PathOptions.None);
            var combined = dir.CombineDirectory("../", PathOptions.None);
            Assert.AreEqual("/dir1", combined.PathDisplay);
            Assert.IsFalse(combined.IsRoot);

            combined = dir.CombineDirectory("../../", PathOptions.None);
            Assert.AreEqual("/", combined.PathDisplay);
            Assert.IsTrue(combined.IsRoot);

            combined = dir.CombineDirectory(".", PathOptions.None);
            Assert.AreEqual("/dir1/dir2", combined.PathDisplay);
        }

        [TestMethod]
        public void NavigatePastRootWindows()
        {
            var dir = DirectoryPath.ParseAbsolute(@"C:\dir1\dir2", PathFormat.Windows, PathOptions.None);
            Assert.ThrowsException<ArgumentException>(() => dir.CombineDirectory("../../..", PathOptions.None));
        }

        [TestMethod]
        public void NavigatePastRootUnix()
        {
            var dir = DirectoryPath.ParseAbsolute("/dir1/dir2", PathFormat.Unix, PathOptions.None);
            Assert.ThrowsException<ArgumentException>(() => dir.CombineDirectory("../../..", PathOptions.None));
        }

        [TestMethod]
        public void CombineUniversalFile()
        {
            var dir = DirectoryPath.ParseAbsolute(@"C:\dir1\dir2", PathFormat.Windows, PathOptions.None);
            var file = dir.CombineFile("../file.txt", PathFormat.Universal, PathOptions.None);
            Assert.AreEqual(PathFormat.Windows, file.PathFormat);
            Assert.AreEqual(@"C:\dir1\file.txt", file.PathDisplay);
        }

        [TestMethod]
        public void CombineDirectory()
        {
            var dir = DirectoryPath.ParseAbsolute(@"C:\dir1\dir2", PathFormat.Windows, PathOptions.None);
            var combined = dir.CombineDirectory("..", PathFormat.Universal, PathOptions.None);
            Assert.AreEqual(PathFormat.Windows, combined.PathFormat);
            Assert.AreEqual(@"C:\dir1", combined.PathDisplay);

            dir = DirectoryPath.ParseAbsolute("/dir1/dir2", PathFormat.Unix, PathOptions.None);
            combined = dir.CombineDirectory(".", PathFormat.Universal, PathOptions.None);
            Assert.AreEqual(PathFormat.Unix, combined.PathFormat);
            Assert.AreEqual("/dir1/dir2", combined.PathDisplay);

            combined = dir.CombineDirectory("newdir/newdir2", PathFormat.Unix, PathOptions.None);
            Assert.AreEqual("/dir1/dir2/newdir/newdir2", combined.PathDisplay);
        }
    }
}