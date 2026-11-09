namespace Singulink.IO.FileSystem.Tests;

[PrefixTestClass]
public class RelativeDirectoryCombineTests
{
    [TestMethod]
    public void NavigateRooted()
    {
        var dir = DirectoryPath.ParseRelative("test", PathFormat.Windows, PathOptions.None);

        var combined = dir.CombineDirectory("/rooted", PathOptions.None);
        combined.PathDisplay.ShouldBe(@"\rooted\");

        combined = dir.CombineDirectory("/", PathOptions.None);
        combined.PathDisplay.ShouldBe(@"\");

        dir = DirectoryPath.ParseRelative("/dir1/dir2", PathFormat.Windows, PathOptions.None);

        combined = dir.CombineDirectory("/rooted", PathOptions.None);
        combined.PathDisplay.ShouldBe(@"\rooted\");

        combined = dir.CombineDirectory("/", PathOptions.None);
        combined.PathDisplay.ShouldBe(@"\");

        dir = DirectoryPath.ParseRelative("", PathFormat.Windows, PathOptions.None);

        combined = dir.CombineDirectory("/rooted", PathOptions.None);
        combined.PathDisplay.ShouldBe(@"\rooted\");

        combined = dir.CombineDirectory("/", PathOptions.None);
        combined.PathDisplay.ShouldBe(@"\");

        var combinedFile = dir.CombineFile("/dir/file.txt", PathFormat.Windows, PathOptions.None);
        combinedFile.PathDisplay.ShouldBe(@"\dir\file.txt");
    }

    [TestMethod]
    [DataRow(@"test\", false, @"\rooted\")]
    [DataRow(@"test\", false, @"\")]
    [DataRow(@"test\", false, @"\dir\file.txt")]
    [DataRow(@"..\test\", false, @"\dir\sub\")]
    [DataRow(@"\dir1\dir2\", false, @"\rooted\")]
    [DataRow(@"\dir1\dir2\", false, @"\dir\file.txt")]
    [DataRow("", false, @"\dir\file.txt")]
    [DataRow("a/b/", true, @"\rooted\")]
    [DataRow("a/b/", true, @"\dir\file.txt")]
    public void RootedAppended_EqualsParsedRootedPath(string baseDir, bool universalBase, string rooted)
    {
        var dir = DirectoryPath.ParseRelative(baseDir, universalBase ? PathFormat.Universal : PathFormat.Windows, PathOptions.None);
        bool isFile = rooted[^1] is not '\\';

        IRelativePath expected = isFile ?
            FilePath.ParseRelative(rooted, PathFormat.Windows, PathOptions.None) :
            DirectoryPath.ParseRelative(rooted, PathFormat.Windows, PathOptions.None);

        var combined = dir.Combine(expected);

        combined.ShouldBe(expected);
        combined.GetHashCode().ShouldBe(expected.GetHashCode());
        ((IPath.Impl)combined).RootLength.ShouldBe(1);

        // The parent chains match up to the root, which has no parent.
        IRelativeDirectoryPath? combinedParent = combined.ParentDirectory;
        IRelativeDirectoryPath? expectedParent = expected.ParentDirectory;

        while (expectedParent is not null)
        {
            combinedParent.ShouldNotBeNull().ShouldBe(expectedParent);
            combinedParent.GetHashCode().ShouldBe(expectedParent.GetHashCode());

            combinedParent = combinedParent.ParentDirectory;
            expectedParent = expectedParent.ParentDirectory;
        }

        combinedParent.ShouldBeNull();
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
        combined.PathDisplay.ShouldBe("dir1/");

        dir = DirectoryPath.ParseRelative("dir1/dir2", PathFormat.Unix, PathOptions.None);
        combined = dir.CombineDirectory(".", PathFormat.Universal, PathOptions.None);
        combined.PathFormat.ShouldBe(PathFormat.Unix);
        combined.PathDisplay.ShouldBe("dir1/dir2/");

        combined = dir.CombineDirectory("newdir/newdir2", PathFormat.Unix, PathOptions.None);
        combined.PathDisplay.ShouldBe("dir1/dir2/newdir/newdir2/");
    }

    [TestMethod]
    public void DeepParentNavigationStripsNamedSegments()
    {
        var dir = DirectoryPath.ParseRelative("a/b/c/d", PathFormat.Unix, PathOptions.None);

        dir.CombineDirectory("../../../", PathOptions.None).PathDisplay.ShouldBe("a/");
        dir.CombineDirectory("../../../../", PathOptions.None).PathDisplay.ShouldBe("");
        dir.CombineDirectory("../../../../../", PathOptions.None).PathDisplay.ShouldBe("../");
        dir.CombineDirectory("../../../../../../", PathOptions.None).PathDisplay.ShouldBe("../../");
    }

    [TestMethod]
    public void DeepParentNavigationExtendsExistingNavPrefix()
    {
        var dir = DirectoryPath.ParseRelative("../../foo/bar", PathFormat.Unix, PathOptions.None);

        dir.CombineDirectory("../", PathOptions.None).PathDisplay.ShouldBe("../../foo/");
        dir.CombineDirectory("../../", PathOptions.None).PathDisplay.ShouldBe("../../");
        dir.CombineDirectory("../../../", PathOptions.None).PathDisplay.ShouldBe("../../../");
        dir.CombineDirectory("../../../../", PathOptions.None).PathDisplay.ShouldBe("../../../../");
    }

    [TestMethod]
    public void DeepParentNavigationFromEmpty()
    {
        var dir = DirectoryPath.ParseRelative("", PathFormat.Unix, PathOptions.None);

        dir.CombineDirectory("../", PathOptions.None).PathDisplay.ShouldBe("../");
        dir.CombineDirectory("../../", PathOptions.None).PathDisplay.ShouldBe("../../");
        dir.CombineDirectory("../../../", PathOptions.None).PathDisplay.ShouldBe("../../../");
    }

    [TestMethod]
    public void RootedRelativeDeepParentNavigation()
    {
        var dir = DirectoryPath.ParseRelative("/a/b/c", PathFormat.Windows, PathOptions.None);

        dir.CombineDirectory("../", PathOptions.None).PathDisplay.ShouldBe(@"\a\b\");
        dir.CombineDirectory("../../", PathOptions.None).PathDisplay.ShouldBe(@"\a\");
        dir.CombineDirectory("../../../", PathOptions.None).PathDisplay.ShouldBe(@"\");

        Should.Throw<ArgumentException>(() => dir.CombineDirectory("../../../../", PathOptions.None));
    }
}
