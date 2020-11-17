using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#pragma warning disable SA1122 // Use string.Empty for empty strings

namespace Singulink.IO.FileSystem.Tests
{
    [TestClass]
    public class RelativeDirectoryParseTests
    {
        [TestMethod]
        public void ParseToCorrectType()
        {
            var dirs = new[] {
                DirectoryPath.Parse("test", PathFormat.Unix),
                DirectoryPath.Parse("./test", PathFormat.Unix),
                DirectoryPath.Parse("../test", PathFormat.Unix),

                DirectoryPath.Parse("test", PathFormat.Universal),
                DirectoryPath.Parse("./test", PathFormat.Universal),
                DirectoryPath.Parse("../test", PathFormat.Universal),

                DirectoryPath.Parse("test", PathFormat.Windows),
                DirectoryPath.Parse("/test", PathFormat.Windows),
                DirectoryPath.Parse("./test", PathFormat.Windows),
                DirectoryPath.Parse("../test", PathFormat.Windows),
                DirectoryPath.Parse(@"\test", PathFormat.Windows),
                DirectoryPath.Parse(@".\test", PathFormat.Windows),
                DirectoryPath.Parse(@"..\test", PathFormat.Windows),
            };

            foreach (var dir in dirs) {
                Assert.IsFalse(dir.IsAbsolute);
                Assert.IsTrue(dir is IRelativeDirectoryPath);
            }
        }

        [TestMethod]
        public void SpecialCurrent()
        {
            var dir = DirectoryPath.ParseRelative("", PathOptions.None);
            Assert.AreEqual("", dir.Name);
            Assert.AreEqual("", dir.PathDisplay);
            Assert.IsFalse(dir.IsRooted);

            dir = DirectoryPath.ParseRelative(".", PathOptions.None);
            Assert.AreEqual("", dir.Name);
            Assert.AreEqual("", dir.PathDisplay);
            Assert.IsFalse(dir.IsRooted);
        }

        [TestMethod]
        public void SpecialParent()
        {
            var dir = DirectoryPath.ParseRelative("..", PathOptions.None);
            Assert.AreEqual("", dir.Name);
            Assert.AreEqual("..", dir.PathDisplay);
            Assert.IsFalse(dir.IsRooted);

            dir = DirectoryPath.ParseRelative("../..", PathFormat.Unix, PathOptions.None);
            Assert.AreEqual("", dir.Name);
            Assert.AreEqual("../..", dir.PathDisplay);
            Assert.IsFalse(dir.IsRooted);
        }

        [TestMethod]
        public void Rooted()
        {
            var dir = DirectoryPath.ParseRelative("/", PathFormat.Windows, PathOptions.None);
            Assert.AreEqual("", dir.Name);
            Assert.AreEqual(@"\", dir.PathDisplay);
            Assert.IsTrue(dir.IsRooted);

            dir = DirectoryPath.ParseRelative("/test", PathFormat.Windows, PathOptions.None);
            Assert.AreEqual("test", dir.Name);
            Assert.AreEqual(@"\test", dir.PathDisplay);
            Assert.IsTrue(dir.IsRooted);
        }

        [TestMethod]
        public void NoUnixRooted()
        {
            Assert.ThrowsException<ArgumentException>(() => DirectoryPath.ParseRelative("/", PathFormat.Unix, PathOptions.None));
            Assert.ThrowsException<ArgumentException>(() => DirectoryPath.ParseRelative("/test", PathFormat.Unix, PathOptions.None));
        }

        [TestMethod]
        public void EmptyDirectories()
        {
            var dir = DirectoryPath.ParseRelative("test////", PathFormat.Universal, PathOptions.AllowEmptyDirectories);
            Assert.AreEqual("test", dir.PathDisplay);

            dir = DirectoryPath.ParseRelative(".///test////", PathFormat.Universal, PathOptions.AllowEmptyDirectories);
            Assert.AreEqual("test", dir.PathDisplay);
        }

        [TestMethod]
        public void Navigation()
        {
            var dir = DirectoryPath.ParseRelative("..////../test//././", PathFormat.Universal, PathOptions.AllowEmptyDirectories);
            Assert.AreEqual("../../test", dir.PathDisplay);
            Assert.IsFalse(dir.IsRooted);

            dir = DirectoryPath.ParseRelative("..////../test//.././", PathFormat.Universal, PathOptions.AllowEmptyDirectories);
            Assert.AreEqual("../..", dir.PathDisplay);
            Assert.IsFalse(dir.IsRooted);
        }

        [TestMethod]
        public void RootedNavigation()
        {
            var dir = DirectoryPath.ParseRelative("/test/../test2/../test3", PathFormat.Windows, PathOptions.None);
            Assert.AreEqual(@"\test3", dir.PathDisplay);
            Assert.IsTrue(dir.IsRooted);

            dir = DirectoryPath.ParseRelative("/test/../test2/../test3/./..", PathFormat.Windows, PathOptions.None);
            Assert.AreEqual(@"\", dir.PathDisplay);
            Assert.IsTrue(dir.IsRooted);
        }

        [TestMethod]
        public void NavigatePastRoot()
        {
            Assert.ThrowsException<ArgumentException>(() => DirectoryPath.ParseRelative("/test/../..", PathFormat.Windows, PathOptions.None));
            Assert.ThrowsException<ArgumentException>(() => DirectoryPath.ParseRelative("/..", PathFormat.Windows, PathOptions.None));
        }

        [TestMethod]
        public void NoNavigation()
        {
            Assert.ThrowsException<ArgumentException>(() => DirectoryPath.ParseRelative(".", PathOptions.NoNavigation));
            Assert.ThrowsException<ArgumentException>(() => DirectoryPath.ParseRelative("..", PathOptions.NoNavigation));
            Assert.ThrowsException<ArgumentException>(() => DirectoryPath.ParseRelative("test/value/.", PathOptions.NoNavigation));
            Assert.ThrowsException<ArgumentException>(() => DirectoryPath.ParseRelative("test/value/..", PathOptions.NoNavigation));
            Assert.ThrowsException<ArgumentException>(() => DirectoryPath.ParseRelative("/test", PathFormat.Windows, PathOptions.NoNavigation));
            Assert.ThrowsException<ArgumentException>(() => DirectoryPath.ParseRelative("/", PathFormat.Windows, PathOptions.NoNavigation));
            Assert.ThrowsException<ArgumentException>(() => DirectoryPath.ParseRelative("/test", PathFormat.Windows, PathOptions.NoNavigation));
        }

        [TestMethod]
        public void PathFormatDependent()
        {
            var dir = DirectoryPath.ParseRelative("./ test.", PathFormat.Universal, PathOptions.None);
            Assert.AreEqual(" test.", dir.PathDisplay);
            Assert.ThrowsException<ArgumentException>(() => DirectoryPath.ParseRelative("/ test.", PathFormat.Universal, PathOptions.PathFormatDependent));

            dir = DirectoryPath.ParseRelative("./ test.", PathFormat.Unix, PathOptions.PathFormatDependent);
            Assert.AreEqual(" test.", dir.PathDisplay);

            dir = DirectoryPath.ParseRelative("/ test.", PathFormat.Windows, PathOptions.None);
            Assert.AreEqual(@"\ test.", dir.PathDisplay);
            Assert.ThrowsException<ArgumentException>(() => DirectoryPath.ParseRelative("/ test.", PathFormat.Windows, PathOptions.PathFormatDependent));
        }

        [TestMethod]
        public void PathOptionExceptions()
        {
            Assert.ThrowsException<ArgumentException>(() => DirectoryPath.ParseRelative("/ test", PathFormat.Windows, PathOptions.NoLeadingSpaces));
            Assert.ThrowsException<ArgumentException>(() => DirectoryPath.ParseRelative("/test ", PathFormat.Windows, PathOptions.NoTrailingSpaces));
            Assert.ThrowsException<ArgumentException>(() => DirectoryPath.ParseRelative("/test.", PathFormat.Windows, PathOptions.NoTrailingDots));
            Assert.ThrowsException<ArgumentException>(() => DirectoryPath.ParseRelative("/test/..", PathFormat.Windows, PathOptions.NoNavigation));
            Assert.ThrowsException<ArgumentException>(() => DirectoryPath.ParseRelative("/./", PathFormat.Windows, PathOptions.NoNavigation));

            Assert.ThrowsException<ArgumentException>(() => DirectoryPath.ParseRelative("/COM1/", PathFormat.Windows, PathOptions.NoReservedDeviceNames));
            Assert.ThrowsException<ArgumentException>(() => DirectoryPath.ParseRelative("AUX", PathFormat.Windows, PathOptions.NoReservedDeviceNames));
            Assert.ThrowsException<ArgumentException>(() => DirectoryPath.ParseRelative("CON", PathFormat.Windows, PathOptions.NoReservedDeviceNames));
            Assert.ThrowsException<ArgumentException>(() => DirectoryPath.ParseRelative("/test/NUL", PathFormat.Windows, PathOptions.NoReservedDeviceNames));
            Assert.ThrowsException<ArgumentException>(() => DirectoryPath.ParseRelative("PRN/test", PathFormat.Windows, PathOptions.NoReservedDeviceNames));
            Assert.ThrowsException<ArgumentException>(() => DirectoryPath.ParseRelative("/test/LPT5/test2", PathFormat.Windows, PathOptions.NoReservedDeviceNames));
        }
    }
}
