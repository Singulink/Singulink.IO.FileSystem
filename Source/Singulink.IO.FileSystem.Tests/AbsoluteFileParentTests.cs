using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet.Frameworks;

namespace Singulink.IO.FileSystem.Tests
{
    [TestClass]
    public class AbsoluteFileParentTests
    {
        [TestMethod]
        public void IsImplemented()
        {
            var file = FilePath.ParseAbsolute(@"C:\test.asdf", PathFormat.Windows);
            Assert.IsTrue(file.HasParentDirectory);
            Assert.IsNotNull(file.ParentDirectory);
        }
    }
}
