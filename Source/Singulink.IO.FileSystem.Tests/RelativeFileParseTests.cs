using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
    }
}
