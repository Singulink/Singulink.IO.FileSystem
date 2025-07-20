namespace Singulink.IO.FileSystem.Tests;

[PrefixTestClass]
public class AbsoluteFileParentTests
{
    [TestMethod]
    public void IsImplemented()
    {
        var file = FilePath.ParseAbsolute(@"C:\test.asdf", PathFormat.Windows);
        file.HasParentDirectory.ShouldBeTrue();
        file.ParentDirectory.ShouldNotBeNull();
    }
}
