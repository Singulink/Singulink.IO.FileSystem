namespace Singulink.IO.FileSystem.Tests;

[PrefixTestClass]
public class RelativeFileParentTests
{
    [TestMethod]
    public void ParentDirectory_BareFile_ReturnsCurrentDirectory()
    {
        var file = FilePath.ParseRelative("test.asdf", PathFormat.Windows);
        file.HasParentDirectory.ShouldBeTrue();

        var dir = file.ParentDirectory!;
        dir.ShouldBe(PathFormat.Windows.RelativeCurrentDirectory);
        dir.PathDisplay.ShouldBe("");
    }

    [TestMethod]
    public void ParentDirectory_NestedRelativeFile_WalksUpToEmpty()
    {
        var file = FilePath.ParseRelative("a/b/c/file.txt", PathFormat.Universal);

        var dir = file.ParentDirectory!;
        dir.PathDisplay.ShouldBe("a/b/c/");

        dir = dir.ParentDirectory!;
        dir.PathDisplay.ShouldBe("a/b/");

        dir = dir.ParentDirectory!;
        dir.PathDisplay.ShouldBe("a/");

        dir = dir.ParentDirectory!;
        dir.PathDisplay.ShouldBe("");
        dir.IsRooted.ShouldBeFalse();
    }

    [TestMethod]
    public void ParentDirectory_RootedRelativeFile_WalksUpToRoot()
    {
        var file = FilePath.ParseRelative("/a/b/file.txt", PathFormat.Windows);

        var dir = file.ParentDirectory!;
        dir.PathDisplay.ShouldBe(@"\a\b\");
        dir.IsRooted.ShouldBeTrue();

        dir = dir.ParentDirectory!;
        dir.PathDisplay.ShouldBe(@"\a\");

        dir = dir.ParentDirectory!;
        dir.PathDisplay.ShouldBe(@"\");
        dir.IsRooted.ShouldBeTrue();
        dir.HasParentDirectory.ShouldBeFalse();
        dir.ParentDirectory.ShouldBeNull();
    }

    [TestMethod]
    public void ParentDirectory_ParentNavigationFile_NavigatesUpFurther()
    {
        var file = FilePath.ParseRelative("../file.txt", PathFormat.Universal);

        var dir = file.ParentDirectory!;
        dir.PathDisplay.ShouldBe("../");

        dir = dir.ParentDirectory!;
        dir.PathDisplay.ShouldBe("../../");
    }
}
