namespace Singulink.IO.FileSystem.Tests;

[PrefixTestClass]
public class AbsoluteDirectoryParentTests
{
    [TestMethod]
    public void Windows()
    {
        var dir = DirectoryPath.ParseAbsolute(@"C:\test\test2\test3", PathFormat.Windows);
        dir.HasParentDirectory.ShouldBeTrue();
        dir.PathDisplay.ShouldBe(@"C:\test\test2\test3");

        dir = dir.ParentDirectory!;
        dir.HasParentDirectory.ShouldBeTrue();
        dir.PathDisplay.ShouldBe(@"C:\test\test2");

        dir = dir.ParentDirectory!;
        dir.HasParentDirectory.ShouldBeTrue();
        dir.PathDisplay.ShouldBe(@"C:\test");

        dir = dir.ParentDirectory!;
        dir.HasParentDirectory.ShouldBeFalse();
        dir.PathDisplay.ShouldBe(@"C:\");
        dir.ParentDirectory.ShouldBeNull();
    }

    [TestMethod]
    public void Unix()
    {
        var dir = DirectoryPath.ParseAbsolute("/test/test2/test3", PathFormat.Unix);
        dir.HasParentDirectory.ShouldBeTrue();
        dir.PathDisplay.ShouldBe("/test/test2/test3");

        dir = dir.ParentDirectory!;
        dir.HasParentDirectory.ShouldBeTrue();
        dir.PathDisplay.ShouldBe("/test/test2");

        dir = dir.ParentDirectory!;
        dir.HasParentDirectory.ShouldBeTrue();
        dir.PathDisplay.ShouldBe("/test");

        dir = dir.ParentDirectory!;
        dir.HasParentDirectory.ShouldBeFalse();
        dir.PathDisplay.ShouldBe("/");
        dir.ParentDirectory.ShouldBeNull();
    }
}
