namespace Singulink.IO.FileSystem.Tests;

[PrefixTestClass]
public class AbsoluteFileParentTests
{
    [TestMethod]
    public void ParentDirectory_RootFile_ReturnsRoot()
    {
        var file = FilePath.ParseAbsolute(@"C:\test.asdf", PathFormat.Windows);
        file.HasParentDirectory.ShouldBeTrue();
        file.ParentDirectory.ShouldNotBeNull();
        file.ParentDirectory.PathDisplay.ShouldBe(@"C:\");
        file.ParentDirectory.IsRoot.ShouldBeTrue();
    }

    [TestMethod]
    public void ParentDirectory_NestedWindowsFile_WalksUpToRoot()
    {
        var file = FilePath.ParseAbsolute(@"C:\a\b\c\file.txt", PathFormat.Windows);

        var dir = file.ParentDirectory!;
        dir.PathDisplay.ShouldBe(@"C:\a\b\c\");

        dir = dir.ParentDirectory!;
        dir.PathDisplay.ShouldBe(@"C:\a\b\");

        dir = dir.ParentDirectory!;
        dir.PathDisplay.ShouldBe(@"C:\a\");

        dir = dir.ParentDirectory!;
        dir.PathDisplay.ShouldBe(@"C:\");
        dir.IsRoot.ShouldBeTrue();
        dir.ParentDirectory.ShouldBeNull();
    }

    [TestMethod]
    public void ParentDirectory_NestedUnixFile_WalksUpToRoot()
    {
        var file = FilePath.ParseAbsolute("/a/b/c/file.txt", PathFormat.Unix);

        var dir = file.ParentDirectory!;
        dir.PathDisplay.ShouldBe("/a/b/c/");

        dir = dir.ParentDirectory!;
        dir.PathDisplay.ShouldBe("/a/b/");

        dir = dir.ParentDirectory!;
        dir.PathDisplay.ShouldBe("/a/");

        dir = dir.ParentDirectory!;
        dir.PathDisplay.ShouldBe("/");
        dir.IsRoot.ShouldBeTrue();
        dir.ParentDirectory.ShouldBeNull();
    }

    [TestMethod]
    public void ParentDirectory_UncFile_ParentChainStopsAtShareRoot()
    {
        var file = FilePath.ParseAbsolute(@"\\server\share\dir\file.txt", PathFormat.Windows);

        var dir = file.ParentDirectory!;
        dir.PathDisplay.ShouldBe(@"\\server\share\dir\");

        dir = dir.ParentDirectory!;
        dir.PathDisplay.ShouldBe(@"\\server\share\");
        dir.IsRoot.ShouldBeTrue();
        dir.ParentDirectory.ShouldBeNull();
    }
}
