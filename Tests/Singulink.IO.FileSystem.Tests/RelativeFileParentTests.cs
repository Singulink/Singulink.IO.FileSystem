namespace Singulink.IO.FileSystem.Tests;

[PrefixTestClass]
public class RelativeFileParentTests
{
    [TestMethod]
    public void IsImplemented()
    {
        var file = FilePath.ParseRelative("test.asdf", PathFormat.Windows);
        file.HasParentDirectory.ShouldBeTrue();

        var dir = file.ParentDirectory;
        dir.ShouldBe(PathFormat.Windows.RelativeCurrentDirectory);
    }
}
