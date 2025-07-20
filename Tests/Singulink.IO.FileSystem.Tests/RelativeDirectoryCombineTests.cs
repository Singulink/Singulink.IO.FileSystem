namespace Singulink.IO.FileSystem.Tests;

[PrefixTestClass]
public class RelativeDirectoryCombineTests
{
    [TestMethod]
    public void NavigateRooted()
    {
        var dir = DirectoryPath.ParseRelative("test", PathFormat.Windows, PathOptions.None);

        var combined = dir.CombineDirectory("/rooted", PathOptions.None);
        combined.PathDisplay.ShouldBe(@"\rooted");

        combined = dir.CombineDirectory("/", PathOptions.None);
        combined.PathDisplay.ShouldBe(@"\");

        dir = DirectoryPath.ParseRelative("/dir1/dir2", PathFormat.Windows, PathOptions.None);

        combined = dir.CombineDirectory("/rooted", PathOptions.None);
        combined.PathDisplay.ShouldBe(@"\rooted");

        combined = dir.CombineDirectory("/", PathOptions.None);
        combined.PathDisplay.ShouldBe(@"\");

        dir = DirectoryPath.ParseRelative("", PathFormat.Windows, PathOptions.None);

        combined = dir.CombineDirectory("/rooted", PathOptions.None);
        combined.PathDisplay.ShouldBe(@"\rooted");

        combined = dir.CombineDirectory("/", PathOptions.None);
        combined.PathDisplay.ShouldBe(@"\");

        var combinedFile = dir.CombineFile("/dir/file.txt", PathFormat.Windows, PathOptions.None);
        combinedFile.PathDisplay.ShouldBe(@"\dir\file.txt");
    }

    [TestMethod]
    public void CombineUniversalFile()
    {
        var dir = DirectoryPath.ParseRelative(@"dir1\dir2", PathFormat.Windows, PathOptions.None);
        var file = dir.CombineFile("../file.txt", PathFormat.Universal, PathOptions.None);
        file.PathFormat.ShouldBe(PathFormat.Windows);
        file.PathDisplay.ShouldBe(@"dir1\file.txt");
    }

    [TestMethod]
    public void CombineDirectory()
    {
        var dir = DirectoryPath.ParseRelative("dir1/dir2", PathFormat.Unix, PathOptions.None);
        var combined = dir.CombineDirectory("..", PathFormat.Universal, PathOptions.None);
        combined.PathFormat.ShouldBe(PathFormat.Unix);
        combined.PathDisplay.ShouldBe("dir1");

        dir = DirectoryPath.ParseRelative("dir1/dir2", PathFormat.Unix, PathOptions.None);
        combined = dir.CombineDirectory(".", PathFormat.Universal, PathOptions.None);
        combined.PathFormat.ShouldBe(PathFormat.Unix);
        combined.PathDisplay.ShouldBe("dir1/dir2");

        combined = dir.CombineDirectory("newdir/newdir2", PathFormat.Unix, PathOptions.None);
        combined.PathDisplay.ShouldBe("dir1/dir2/newdir/newdir2");
    }
}
