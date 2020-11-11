using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#pragma warning disable SA1122 // Use string.Empty for empty strings

namespace Singulink.IO.FileSystem.Tests
{
    [TestClass]
    public class AbsoluteDirectoryParsing
    {
        [TestMethod]
        public void AbsoluteRootUnix()
        {
            var dir = DirectoryPath.ParseAbsolute("/", PathFormat.Unix, PathOptions.None);
            Assert.AreEqual("/", dir.Name);
            Assert.AreEqual("/", dir.PathDisplay);
            Assert.AreEqual("/", dir.PathExport);
            Assert.IsTrue(dir.IsRooted);
            Assert.IsTrue(dir.IsRoot);
        }

        [TestMethod]
        public void AbsoluteRootWindowsDrive()
        {
            var dir = DirectoryPath.ParseAbsolute("c:", PathFormat.Windows, PathOptions.None);
            Assert.AreEqual("c:", dir.Name);
            Assert.AreEqual(@"c:\", dir.PathDisplay);
            Assert.AreEqual(@"\\?\c:\", dir.PathExport);
            Assert.IsTrue(dir.IsRooted);
            Assert.IsTrue(dir.IsRoot);

            dir = DirectoryPath.ParseAbsolute("x:/", PathFormat.Windows, PathOptions.None);
            Assert.AreEqual("x:", dir.Name);
            Assert.AreEqual(@"x:\", dir.PathDisplay);
            Assert.AreEqual(@"\\?\x:\", dir.PathExport);
            Assert.IsTrue(dir.IsRooted);
            Assert.IsTrue(dir.IsRoot);
        }

        [TestMethod]
        public void AbsoluteRootWindowsUnc()
        {
            var dir = DirectoryPath.ParseAbsolute(@"\\Server\Share", PathFormat.Windows, PathOptions.None);
            Assert.AreEqual(@"\\Server\Share", dir.Name);
            Assert.AreEqual(@"\\Server\Share\", dir.PathDisplay);
            Assert.AreEqual(@"\\?\UNC\Server\Share\", dir.PathExport);
            Assert.IsTrue(dir.IsRooted);
            Assert.IsTrue(dir.IsRoot);

            Assert.ThrowsException<ArgumentException>(() => DirectoryPath.ParseAbsolute("\\Server", PathFormat.Windows, PathOptions.None));
        }

        [TestMethod]
        public void BadAbsolutePaths()
        {
            Assert.ThrowsException<ArgumentException>(() => DirectoryPath.ParseAbsolute("test", PathFormat.Windows, PathOptions.None));
            Assert.ThrowsException<ArgumentException>(() => DirectoryPath.ParseAbsolute("", PathFormat.Windows, PathOptions.None));
            Assert.ThrowsException<ArgumentException>(() => DirectoryPath.ParseAbsolute("xy:/", PathFormat.Windows, PathOptions.None));
            Assert.ThrowsException<ArgumentException>(() => DirectoryPath.ParseAbsolute("1:/", PathFormat.Windows, PathOptions.None));
            Assert.ThrowsException<ArgumentException>(() => DirectoryPath.ParseAbsolute(" :/", PathFormat.Windows, PathOptions.None));

            Assert.ThrowsException<ArgumentException>(() => DirectoryPath.ParseAbsolute("test", PathFormat.Unix, PathOptions.None));
            Assert.ThrowsException<ArgumentException>(() => DirectoryPath.ParseAbsolute("", PathFormat.Unix, PathOptions.None));
        }

        [TestMethod]
        public void Navigation()
        {
            var dir = DirectoryPath.ParseAbsolute(@"\\Server\Share\test1\test2\..\..", PathFormat.Windows, PathOptions.None);
            Assert.AreEqual(@"\\Server\Share\", dir.PathDisplay);

            var ex = Assert.ThrowsException<ArgumentException>(() => DirectoryPath.ParseAbsolute(@"\\Server\Share\test1\test2\..\..\..", PathFormat.Windows, PathOptions.None));
            Assert.AreEqual("Attempt to navigate past root directory. (Parameter 'path')", ex.Message);

            dir = DirectoryPath.ParseAbsolute("c:/./test/.././", PathFormat.Windows, PathOptions.None);
            Assert.AreEqual(@"c:\", dir.PathDisplay);
            Assert.IsTrue(dir.IsRoot);

            dir = DirectoryPath.ParseAbsolute("/./test/.././", PathFormat.Unix, PathOptions.None);
        }

        [TestMethod]
        public void PathFormatDependent()
        {
            var dir = DirectoryPath.ParseAbsolute("/ test.", PathFormat.Unix, PathOptions.PathFormatDependent);
            Assert.AreEqual("/ test.", dir.PathDisplay);

            dir = DirectoryPath.ParseAbsolute("c:/ test.", PathFormat.Windows, PathOptions.None);
            Assert.AreEqual(@"c:\ test.", dir.PathDisplay);
            Assert.ThrowsException<ArgumentException>(() => DirectoryPath.ParseRelative("/ test.", PathFormat.Windows, PathOptions.PathFormatDependent));
        }
    }
}
