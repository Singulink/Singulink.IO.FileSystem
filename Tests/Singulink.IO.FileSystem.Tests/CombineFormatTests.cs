namespace Singulink.IO.FileSystem.Tests;

[PrefixTestClass]
public class CombineFormatTests
{
    [TestMethod]
    public void MatchBase_OnWindowsBase_ParsesAsWindows()
    {
        var dir = DirectoryPath.ParseAbsolute(@"C:\foo\", PathFormat.Windows);

        // Backslash separator only parses cleanly under Windows format.
        var combined = dir.CombineDirectory(@"a\b", PathFormat.Windows, PathOptions.None);

        combined.PathDisplay.ShouldBe(@"C:\foo\a\b\");
    }

    [TestMethod]
    public void MatchBase_OnUnixBase_ParsesAsUnix()
    {
        var dir = DirectoryPath.ParseAbsolute("/foo/", PathFormat.Unix);
        var combined = dir.CombineDirectory("a/b", PathFormat.Unix, PathOptions.None);

        combined.PathDisplay.ShouldBe("/foo/a/b/");
    }

    [TestMethod]
    public void Universal_OnWindowsBase_ParsesAsUniversalAndResultIsWindows()
    {
        var dir = DirectoryPath.ParseAbsolute(@"C:\foo\", PathFormat.Windows);
        var combined = dir.CombineDirectory("a/b", PathFormat.Universal, PathOptions.None);

        combined.PathFormat.ShouldBe(PathFormat.Windows);
        combined.PathDisplay.ShouldBe(@"C:\foo\a\b\");
    }

    [TestMethod]
    public void Universal_OnUnixBase_ParsesAsUniversalAndResultIsUnix()
    {
        var dir = DirectoryPath.ParseAbsolute("/foo/", PathFormat.Unix);
        var combined = dir.CombineDirectory("a/b", PathFormat.Universal, PathOptions.None);

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

    [TestMethod]
    public void UniversalRelativeBase_AppendWindowsSpecificPath_ResultIsWindows()
    {
        // Verifies that a universal relative base can have a platform-specific relative path appended.
        // This is the scenario that motivated reverting RelativePathFormat back to PathFormat in v3.0.1
        // (under the prior RelativePathFormat API, a backslash-separated string could not be parsed
        // against a Universal base since the only options were MatchBase and Universal).
        var dir = DirectoryPath.ParseRelative("base/dir", PathFormat.Universal);
        var combined = dir.CombineDirectory(@"a\b", PathFormat.Windows, PathOptions.None);

        combined.PathFormat.ShouldBe(PathFormat.Windows);
        combined.PathDisplay.ShouldBe(@"base\dir\a\b\");
    }

    [TestMethod]
    public void UniversalRelativeBase_AppendUnixSpecificPath_ResultIsUnix()
    {
        var dir = DirectoryPath.ParseRelative("base/dir", PathFormat.Universal);
        var combined = dir.CombineDirectory("a/b", PathFormat.Unix, PathOptions.None);

        combined.PathFormat.ShouldBe(PathFormat.Unix);
        combined.PathDisplay.ShouldBe("base/dir/a/b/");
    }
}
