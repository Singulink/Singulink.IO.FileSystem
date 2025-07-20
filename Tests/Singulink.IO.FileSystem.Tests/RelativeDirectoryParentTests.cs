namespace Singulink.IO.FileSystem.Tests;

[PrefixTestClass]
public class RelativeDirectoryParentTests
{
    [TestMethod]
    public void SpecialCurrent()
    {
        var dir = DirectoryPath.ParseRelative("", PathOptions.None);
        dir.HasParentDirectory.ShouldBeTrue();
        dir.ParentDirectory!.PathDisplay.ShouldBe("..");

        dir = DirectoryPath.ParseRelative(".", PathOptions.None);
        dir.HasParentDirectory.ShouldBeTrue();
        dir.ParentDirectory!.PathDisplay.ShouldBe("..");
    }

    [TestMethod]
    public void SpecialParent()
    {
        var dir = DirectoryPath.ParseRelative("..", PathFormat.Windows, PathOptions.None);
        dir.HasParentDirectory.ShouldBeTrue();
        dir.ParentDirectory!.PathDisplay.ShouldBe(@"..\..");

        dir = DirectoryPath.ParseRelative("../..", PathFormat.Unix, PathOptions.None);
        dir.HasParentDirectory.ShouldBeTrue();
        dir.ParentDirectory!.PathDisplay.ShouldBe("../../..");
    }

    [TestMethod]
    public void Rooted()
    {
        var dir = DirectoryPath.ParseRelative("/", PathFormat.Windows, PathOptions.None);
        dir.HasParentDirectory.ShouldBeFalse();
        dir.ParentDirectory.ShouldBeNull();

        dir = DirectoryPath.ParseRelative("/test", PathFormat.Windows, PathOptions.None);
        dir.HasParentDirectory.ShouldBeTrue();

        dir = dir.ParentDirectory!;
        dir.PathDisplay.ShouldBe(@"\");
        dir.HasParentDirectory.ShouldBeFalse();
    }

    [TestMethod]
    public void NavigatingPastEmpty()
    {
        var dir = DirectoryPath.ParseRelative("dir1/dir2", PathFormat.Universal, PathOptions.None);
        dir.PathDisplay.ShouldBe("dir1/dir2");

        var parent = dir.ParentDirectory!;
        parent.PathDisplay.ShouldBe("dir1");

        parent = parent.ParentDirectory!;
        parent.PathDisplay.ShouldBe("");

        parent = parent.ParentDirectory!;
        parent.PathDisplay.ShouldBe("..");

        parent = parent.ParentDirectory!;
        parent.PathDisplay.ShouldBe("../..");

        parent = parent.ParentDirectory!;
        parent.PathDisplay.ShouldBe("../../..");
    }
}
