using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Singulink.IO.FileSystem.Tests
{
    [TestClass]
    public class EqualsTests
    {
        [TestMethod]
        public void EqualsMatchingFile()
        {
            var x = FilePath.Parse(@"c:\somepath", PathFormat.Windows);
            var y = FilePath.Parse(@"C:\somepath", PathFormat.Windows);

            Assert.IsTrue(x.Equals(y));
            Assert.AreEqual(x, y);
        }

        [TestMethod]
        public void NotEqualsOtherFile()
        {
            var x = FilePath.Parse(@"c:\somepath", PathFormat.Windows);
            var y = FilePath.Parse(@"C:\someotherpath", PathFormat.Windows);

            Assert.IsFalse(x.Equals(y));
            Assert.AreNotEqual(x, y);
        }

        [TestMethod]
        public void NotEqualsMatchingDir()
        {
            var x = FilePath.Parse(@"c:\somepath", PathFormat.Windows);
            var y = DirectoryPath.Parse(@"c:\somepath", PathFormat.Windows);

            Assert.IsFalse(x.Equals(y));
            Assert.AreNotEqual(x, y);
        }
    }
}