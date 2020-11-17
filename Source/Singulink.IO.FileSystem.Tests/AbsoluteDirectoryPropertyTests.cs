using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Singulink.IO.FileSystem.Tests
{
    [TestClass]
    public class AbsoluteDirectoryPropertyTests
    {
        public static readonly DateTime EarliestDateTime = new DateTime(2010, 1, 1);

        [TestMethod]
        public void Space()
        {
            var dir = DirectoryPath.GetMountingPoints().First(d => d.DriveType == DriveType.Fixed);

            Assert.IsTrue(dir.Exists);
            Assert.IsTrue(dir.TotalSize > 0);
            Assert.IsTrue(dir.TotalFreeSpace > 0);
            Assert.IsTrue(dir.AvailableFreeSpace > 0);
            Assert.IsFalse(string.IsNullOrWhiteSpace(dir.FileSystem));
            Assert.IsTrue(dir.CreationTime > EarliestDateTime);
            Assert.IsTrue(dir.LastAccessTime > EarliestDateTime);
            Assert.IsTrue(dir.LastWriteTime > EarliestDateTime);
        }
    }
}
