using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Singulink.IO.FileSystem.Tests
{
    [TestClass]
    public class AbsoluteFileParseTests
    {
        [TestMethod]
        public void ParseToCorrectType()
        {
            var files = new[] {
                FilePath.Parse("/test.sdf", PathFormat.Unix),
                FilePath.Parse("c:/test.rga", PathFormat.Windows),
                FilePath.Parse(@"c:\test.agae", PathFormat.Windows),
                FilePath.Parse(@"\\server\test\test.sef", PathFormat.Windows),
                FilePath.Parse("//server/test/test.rae", PathFormat.Windows),
            };

            foreach (var file in files) {
                Assert.IsTrue(file.IsAbsolute);
                Assert.IsTrue(file is IAbsoluteFilePath);
            }
        }

        [TestMethod]
        public void NoUniversal()
        {
            Assert.ThrowsException<ArgumentException>(() => FilePath.Parse("/test.asdf", PathFormat.Universal));
            Assert.ThrowsException<ArgumentException>(() => FilePath.ParseAbsolute("/test.sdf", PathFormat.Universal));
        }

        [TestMethod]
        public void NoMissingFilePaths()
        {
            Assert.ThrowsException<ArgumentException>(() => FilePath.ParseAbsolute("C:", PathFormat.Windows));
            Assert.ThrowsException<ArgumentException>(() => FilePath.ParseAbsolute(@"C:\", PathFormat.Windows));
            Assert.ThrowsException<ArgumentException>(() => FilePath.ParseAbsolute(@"C:\test\", PathFormat.Windows));
            Assert.ThrowsException<ArgumentException>(() => FilePath.ParseAbsolute(@"C:\test\..", PathFormat.Windows));
            Assert.ThrowsException<ArgumentException>(() => FilePath.ParseAbsolute(@"C:\test.txt\.", PathFormat.Windows));
            Assert.ThrowsException<ArgumentException>(() => FilePath.ParseAbsolute(@"C:\test\test.txt\..", PathFormat.Windows));

            Assert.ThrowsException<ArgumentException>(() => FilePath.ParseAbsolute("/", PathFormat.Unix));
            Assert.ThrowsException<ArgumentException>(() => FilePath.ParseAbsolute("/test/", PathFormat.Unix));
            Assert.ThrowsException<ArgumentException>(() => FilePath.ParseAbsolute("/test/..", PathFormat.Unix));
            Assert.ThrowsException<ArgumentException>(() => FilePath.ParseAbsolute("/test.txt/.", PathFormat.Unix));
            Assert.ThrowsException<ArgumentException>(() => FilePath.ParseAbsolute("/test/test.txt/..", PathFormat.Unix));
        }
    }
}