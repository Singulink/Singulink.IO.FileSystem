namespace Singulink.IO.FileSystem.Tests;

[PrefixTestClass]
public class RelativePathFormatTests
{
    [TestMethod]
    public void AbsoluteCombineDirectory_InvalidEnumValue_ThrowsArgumentOutOfRange()
    {
        var dir = DirectoryPath.ParseAbsolute(@"C:\foo\", PathFormat.Windows);

        var ex = Should.Throw<ArgumentOutOfRangeException>(
            () => dir.CombineDirectory("sub", (RelativePathFormat)999, PathOptions.None));

        ex.ParamName.ShouldBe("format");
    }

    [TestMethod]
    public void AbsoluteCombineFile_InvalidEnumValue_ThrowsArgumentOutOfRange()
    {
        var dir = DirectoryPath.ParseAbsolute(@"C:\foo\", PathFormat.Windows);

        var ex = Should.Throw<ArgumentOutOfRangeException>(
            () => dir.CombineFile("file.txt", (RelativePathFormat)999, PathOptions.None));

        ex.ParamName.ShouldBe("format");
    }

    [TestMethod]
    public void RelativeCombineDirectory_InvalidEnumValue_ThrowsArgumentOutOfRange()
    {
        var dir = DirectoryPath.ParseRelative("foo/", PathFormat.Universal);

        var ex = Should.Throw<ArgumentOutOfRangeException>(
            () => dir.CombineDirectory("sub", (RelativePathFormat)999, PathOptions.None));

        ex.ParamName.ShouldBe("format");
    }

    [TestMethod]
    public void RelativeCombineFile_InvalidEnumValue_ThrowsArgumentOutOfRange()
    {
        var dir = DirectoryPath.ParseRelative("foo/", PathFormat.Universal);

        var ex = Should.Throw<ArgumentOutOfRangeException>(
            () => dir.CombineFile("file.txt", (RelativePathFormat)999, PathOptions.None));

        ex.ParamName.ShouldBe("format");
    }

    [TestMethod]
    public void MatchBase_OnWindowsBase_ParsesAsWindows()
    {
        var dir = DirectoryPath.ParseAbsolute(@"C:\foo\", PathFormat.Windows);

        // Backslash separator only parses cleanly under Windows format.
        var combined = dir.CombineDirectory(@"a\b", RelativePathFormat.MatchBase, PathOptions.None);

        combined.PathDisplay.ShouldBe(@"C:\foo\a\b\");
    }

    [TestMethod]
    public void MatchBase_OnUnixBase_ParsesAsUnix()
    {
        var dir = DirectoryPath.ParseAbsolute("/foo/", PathFormat.Unix);
        var combined = dir.CombineDirectory("a/b", RelativePathFormat.MatchBase, PathOptions.None);

        combined.PathDisplay.ShouldBe("/foo/a/b/");
    }

    [TestMethod]
    public void Universal_OnWindowsBase_ParsesAsUniversalAndResultIsWindows()
    {
        var dir = DirectoryPath.ParseAbsolute(@"C:\foo\", PathFormat.Windows);
        var combined = dir.CombineDirectory("a/b", RelativePathFormat.Universal, PathOptions.None);

        combined.PathFormat.ShouldBe(PathFormat.Windows);
        combined.PathDisplay.ShouldBe(@"C:\foo\a\b\");
    }

    [TestMethod]
    public void Universal_OnUnixBase_ParsesAsUniversalAndResultIsUnix()
    {
        var dir = DirectoryPath.ParseAbsolute("/foo/", PathFormat.Unix);
        var combined = dir.CombineDirectory("a/b", RelativePathFormat.Universal, PathOptions.None);

        combined.PathFormat.ShouldBe(PathFormat.Unix);
        combined.PathDisplay.ShouldBe("/foo/a/b/");
    }

    [TestMethod]
    public void TypedCombine_UniversalRelativeIntoWindowsBase_ResultFormatIsWindows()
    {
        var dir = DirectoryPath.ParseAbsolute(@"C:\foo\", PathFormat.Windows);
        var rel = DirectoryPath.ParseRelative("a/b", PathFormat.Universal);

        var combined = dir.Combine(rel);

        combined.PathFormat.ShouldBe(PathFormat.Windows);
        combined.PathDisplay.ShouldBe(@"C:\foo\a\b\");
    }

    [TestMethod]
    public void TypedCombine_FormatMismatch_ThrowsWithPathParamName()
    {
        var dir = DirectoryPath.ParseAbsolute(@"C:\foo\", PathFormat.Windows);
        var rel = DirectoryPath.ParseRelative("a/b", PathFormat.Unix);

        var ex = Should.Throw<ArgumentException>(() => dir.Combine(rel));
        ex.ParamName.ShouldBe("path");
    }
}
