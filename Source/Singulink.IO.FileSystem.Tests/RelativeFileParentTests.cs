using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#pragma warning disable SA1122 // Use string.Empty for empty strings

namespace Singulink.IO.FileSystem.Tests
{
    [TestClass]
    public class RelativeFileParentTests
    {
        [TestMethod]
        public void IsImplemented()
        {
            var file = FilePath.ParseRelative("test.asdf", PathFormat.Windows);
            Assert.IsTrue(file.HasParentDirectory);

            var dir = file.ParentDirectory;
            Assert.AreEqual(PathFormat.Windows.RelativeCurrentDirectory, dir);
        }
    }
}
