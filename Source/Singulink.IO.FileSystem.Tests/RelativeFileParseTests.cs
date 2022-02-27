using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#pragma warning disable SA1122 // Use string.Empty for empty strings

namespace Singulink.IO.FileSystem.Tests
{
    [TestClass]
    public class RelativeFileParseTests
    {
        [TestMethod]
        public void ParseToCorrectType()
        {
            var files = new[] {
                FilePath.Parse("test.sdf", PathFormat.Unix),
                FilePath.Parse("./test.sdf", PathFormat.Unix),
                FilePath.Parse("../test.sdf", PathFormat.Unix),

                FilePath.Parse("test.sdf", PathFormat.Universal),
                FilePath.Parse("./test.sdf", PathFormat.Universal),
                FilePath.Parse("../test.sdf", PathFormat.Universal),

                FilePath.Parse("test.rga", PathFormat.Windows),
                FilePath.Parse("/test.rga", PathFormat.Windows),
                FilePath.Parse("./test.sdf", PathFormat.Windows),
                FilePath.Parse("../test.sdf", PathFormat.Windows),
                FilePath.Parse(@"\test.agae", PathFormat.Windows),
                FilePath.Parse(@".\test.sdf", PathFormat.Windows),
                FilePath.Parse(@"..\test.sdf", PathFormat.Windows),
            };

            foreach (var file in files) {
                Assert.IsFalse(file.IsAbsolute);
                Assert.IsTrue(file is IRelativeFilePath);
            }
        }

        [TestMethod]
        public void NoMissingFilePaths()
        {
            Assert.ThrowsException<ArgumentException>(() => FilePath.ParseRelative("", PathFormat.Windows));
            Assert.ThrowsException<ArgumentException>(() => FilePath.ParseRelative(@"\", PathFormat.Windows));
            Assert.ThrowsException<ArgumentException>(() => FilePath.ParseRelative(@"test\", PathFormat.Windows));
            Assert.ThrowsException<ArgumentException>(() => FilePath.ParseRelative(@"test\..", PathFormat.Windows));
            Assert.ThrowsException<ArgumentException>(() => FilePath.ParseRelative(@"test.txt\.", PathFormat.Windows));
            Assert.ThrowsException<ArgumentException>(() => FilePath.ParseRelative(@"test\test.txt\..", PathFormat.Windows));

            Assert.ThrowsException<ArgumentException>(() => FilePath.ParseRelative("", PathFormat.Unix));
            Assert.ThrowsException<ArgumentException>(() => FilePath.ParseRelative("test/", PathFormat.Unix));
            Assert.ThrowsException<ArgumentException>(() => FilePath.ParseRelative("test/..", PathFormat.Unix));
            Assert.ThrowsException<ArgumentException>(() => FilePath.ParseRelative("test.txt/.", PathFormat.Unix));
            Assert.ThrowsException<ArgumentException>(() => FilePath.ParseRelative("test/test.txt/..", PathFormat.Unix));

            Assert.ThrowsException<ArgumentException>(() => FilePath.ParseRelative("", PathFormat.Universal));
            Assert.ThrowsException<ArgumentException>(() => FilePath.ParseRelative("test/", PathFormat.Universal));
            Assert.ThrowsException<ArgumentException>(() => FilePath.ParseRelative("test/..", PathFormat.Universal));
            Assert.ThrowsException<ArgumentException>(() => FilePath.ParseRelative("test.txt/.", PathFormat.Universal));
            Assert.ThrowsException<ArgumentException>(() => FilePath.ParseRelative("test/test.txt/..", PathFormat.Universal));
        }
    }
}