using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Singulink.IO.FileSystem.Tests;

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