using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#pragma warning disable SA1122 // Use string.Empty for empty strings

namespace Singulink.IO.FileSystem.Tests
{
    [TestClass]
    public class RelativeDirectoryCombining
    {
        [TestMethod]
        public void NavigateRooted()
        {
            var dir = DirectoryPath.ParseRelative("test", PathFormat.Windows, PathOptions.None);

            var combined = dir.CombineDirectory("/rooted", PathOptions.None);
            Assert.AreEqual(@"\rooted", combined.PathDisplay);

            combined = dir.CombineDirectory("/", PathOptions.None);
            Assert.AreEqual(@"\", combined.PathDisplay);

            dir = DirectoryPath.ParseRelative("/dir1/dir2", PathFormat.Windows, PathOptions.None);

            combined = dir.CombineDirectory("/rooted", PathOptions.None);
            Assert.AreEqual(@"\rooted", combined.PathDisplay);

            combined = dir.CombineDirectory("/", PathOptions.None);
            Assert.AreEqual(@"\", combined.PathDisplay);

            dir = DirectoryPath.ParseRelative("", PathFormat.Windows, PathOptions.None);

            combined = dir.CombineDirectory("/rooted", PathOptions.None);
            Assert.AreEqual(@"\rooted", combined.PathDisplay);

            combined = dir.CombineDirectory("/", PathOptions.None);
            Assert.AreEqual(@"\", combined.PathDisplay);

            var combinedFile = dir.CombineFile("/dir/file.txt", PathFormat.Windows, PathOptions.None);
            Assert.AreEqual(@"\dir\file.txt", combinedFile.PathDisplay);
        }

        [TestMethod]
        public void CombineUniversalFile()
        {
            var dir = DirectoryPath.ParseRelative(@"dir1\dir2", PathFormat.Windows, PathOptions.None);
            var file = dir.CombineFile("../file.txt", PathFormat.Universal, PathOptions.None);
            Assert.AreEqual(PathFormat.Windows, file.PathFormat);
            Assert.AreEqual(@"dir1\file.txt", file.PathDisplay);
        }

        [TestMethod]
        public void CombineDirectory()
        {
            var dir = DirectoryPath.ParseRelative("dir1/dir2", PathFormat.Unix, PathOptions.None);
            var combined = dir.CombineDirectory("..", PathFormat.Universal, PathOptions.None);
            Assert.AreEqual(PathFormat.Unix, combined.PathFormat);
            Assert.AreEqual("dir1", combined.PathDisplay);

            dir = DirectoryPath.ParseRelative("dir1/dir2", PathFormat.Unix, PathOptions.None);
            combined = dir.CombineDirectory(".", PathFormat.Universal, PathOptions.None);
            Assert.AreEqual(PathFormat.Unix, combined.PathFormat);
            Assert.AreEqual("dir1/dir2", combined.PathDisplay);

            combined = dir.CombineDirectory("newdir/newdir2", PathFormat.Unix, PathOptions.None);
            Assert.AreEqual("dir1/dir2/newdir/newdir2", combined.PathDisplay);
        }
    }
}
