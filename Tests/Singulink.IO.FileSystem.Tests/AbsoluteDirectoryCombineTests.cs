namespace Singulink.IO.FileSystem.Tests;

[PrefixTestClass]
public class AbsoluteDirectoryCombineTests
{
    [TestMethod]
    public void NavigateWindows()
    {
        var dir = DirectoryPath.ParseAbsolute(@"C:\dir1\dir2", PathFormat.Windows, PathOptions.None);
        var combined = dir.CombineDirectory("../", PathOptions.None);
        combined.PathDisplay.ShouldBe(@"C:\dir1\");
        combined.IsRoot.ShouldBeFalse();

        combined = dir.CombineDirectory("../../", PathOptions.None);
        combined.PathDisplay.ShouldBe(@"C:\");
        combined.IsRoot.ShouldBeTrue();

        combined = dir.CombineDirectory(".", PathOptions.None);
        combined.PathDisplay.ShouldBe(@"C:\dir1\dir2\");
    }

    [TestMethod]
    public void NavigateRootedWindows()
    {
        var dir = DirectoryPath.ParseAbsolute(@"C:\dir1\dir2", PathFormat.Windows, PathOptions.None);

        var combined = dir.CombineDirectory("/", PathOptions.None);
        combined.PathDisplay.ShouldBe(@"C:\");

        combined = dir.CombineDirectory("/test", PathOptions.None);
        combined.PathDisplay.ShouldBe(@"C:\test\");
    }

    [TestMethod]
    public void NavigateUnix()
    {
        var dir = DirectoryPath.ParseAbsolute("/dir1/dir2", PathFormat.Unix, PathOptions.None);
        var combined = dir.CombineDirectory("../", PathOptions.None);
        combined.PathDisplay.ShouldBe("/dir1/");
        combined.IsRoot.ShouldBeFalse();

        combined = dir.CombineDirectory("../../", PathOptions.None);
        combined.PathDisplay.ShouldBe("/");
        combined.IsRoot.ShouldBeTrue();

        combined = dir.CombineDirectory(".", PathOptions.None);
        combined.PathDisplay.ShouldBe("/dir1/dir2/");
    }

    [TestMethod]
    public void NavigatePastRootWindows()
    {
        var dir = DirectoryPath.ParseAbsolute(@"C:\dir1\dir2", PathFormat.Windows, PathOptions.None);
        Should.Throw<ArgumentException>(() => dir.CombineDirectory("../../..", PathOptions.None));
    }

    [TestMethod]
    public void NavigatePastRootUnix()
    {
        var dir = DirectoryPath.ParseAbsolute("/dir1/dir2", PathFormat.Unix, PathOptions.None);
        Should.Throw<ArgumentException>(() => dir.CombineDirectory("../../..", PathOptions.None));
    }

    [TestMethod]
    public void CombineUniversalFile()
    {
        var dir = DirectoryPath.ParseAbsolute(@"C:\dir1\dir2", PathFormat.Windows, PathOptions.None);
        var file = dir.CombineFile("../file.txt", RelativePathFormat.Universal, PathOptions.None);
        file.PathFormat.ShouldBe(PathFormat.Windows);
        file.PathDisplay.ShouldBe(@"C:\dir1\file.txt");
    }

    [TestMethod]
    public void CombineDirectory()
    {
        var dir = DirectoryPath.ParseAbsolute(@"C:\dir1\dir2", PathFormat.Windows, PathOptions.None);
        var combined = dir.CombineDirectory("..", RelativePathFormat.Universal, PathOptions.None);
        combined.PathFormat.ShouldBe(PathFormat.Windows);
        combined.PathDisplay.ShouldBe(@"C:\dir1\");

        dir = DirectoryPath.ParseAbsolute("/dir1/dir2", PathFormat.Unix, PathOptions.None);
        combined = dir.CombineDirectory(".", RelativePathFormat.Universal, PathOptions.None);
        combined.PathFormat.ShouldBe(PathFormat.Unix);
        combined.PathDisplay.ShouldBe("/dir1/dir2/");

        combined = dir.CombineDirectory("newdir/newdir2", RelativePathFormat.MatchBase, PathOptions.None);
        combined.PathDisplay.ShouldBe("/dir1/dir2/newdir/newdir2/");
    }

    [TestMethod]
    public void DeepParentNavigationWindows()
    {
        var dir = DirectoryPath.ParseAbsolute(@"C:\a\b\c\d\e", PathFormat.Windows, PathOptions.None);

        dir.CombineDirectory("../../../", PathOptions.None).PathDisplay.ShouldBe(@"C:\a\b\");
        dir.CombineDirectory("../../../../", PathOptions.None).PathDisplay.ShouldBe(@"C:\a\");
        dir.CombineDirectory("../../../../../", PathOptions.None).PathDisplay.ShouldBe(@"C:\");

        Should.Throw<ArgumentException>(() => dir.CombineDirectory("../../../../../../", PathOptions.None));
    }

    [TestMethod]
    public void DeepParentNavigationUnix()
    {
        var dir = DirectoryPath.ParseAbsolute("/a/b/c/d/e", PathFormat.Unix, PathOptions.None);

        dir.CombineDirectory("../../../", PathOptions.None).PathDisplay.ShouldBe("/a/b/");
        dir.CombineDirectory("../../../../", PathOptions.None).PathDisplay.ShouldBe("/a/");
        dir.CombineDirectory("../../../../../", PathOptions.None).PathDisplay.ShouldBe("/");

        Should.Throw<ArgumentException>(() => dir.CombineDirectory("../../../../../../", PathOptions.None));
    }

    [TestMethod]
    public void DeepParentNavigationUnc()
    {
        var dir = DirectoryPath.ParseAbsolute(@"\\server\share\a\b\c", PathFormat.Windows, PathOptions.None);

        dir.CombineDirectory("../", PathOptions.None).PathDisplay.ShouldBe(@"\\server\share\a\b\");
        dir.CombineDirectory("../../", PathOptions.None).PathDisplay.ShouldBe(@"\\server\share\a\");
        dir.CombineDirectory("../../../", PathOptions.None).PathDisplay.ShouldBe(@"\\server\share\");

        Should.Throw<ArgumentException>(() => dir.CombineDirectory("../../../../", PathOptions.None));
    }
}
