using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Singulink.IO.FileSystem.Tests
{
    public class AbsoluteFileParsing
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
            Assert.ThrowsException<ArgumentException>(() => DirectoryPath.Parse("/test.asdf", PathFormat.Universal));
            Assert.ThrowsException<ArgumentException>(() => DirectoryPath.ParseAbsolute("/test.sdf", PathFormat.Universal));
        }
    }
}
