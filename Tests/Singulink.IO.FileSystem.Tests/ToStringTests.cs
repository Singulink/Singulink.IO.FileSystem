namespace Singulink.IO.FileSystem.Tests;

[PrefixTestClass]
public class ToStringTests
{
    [TestMethod]
    public void PathFormat_Windows_ToString()
    {
        PathFormat.Windows.ToString().ShouldBe("Windows");
    }

    [TestMethod]
    public void PathFormat_Unix_ToString()
    {
        PathFormat.Unix.ToString().ShouldBe("Unix");
    }

    [TestMethod]
    public void PathFormat_Universal_ToString()
    {
        PathFormat.Universal.ToString().ShouldBe("Universal");
    }

    [TestMethod]
    public void AbsoluteWindowsDirectory_ToString_HasFormatAndQuotedPath()
    {
        var dir = DirectoryPath.ParseAbsolute(@"C:\foo\bar\", PathFormat.Windows);
        dir.ToString().ShouldBe(@"[Windows] ""C:\foo\bar\""");
    }

    [TestMethod]
    public void AbsoluteUnixFile_ToString_HasFormatAndQuotedPath()
    {
        var file = FilePath.ParseAbsolute("/foo/bar.txt", PathFormat.Unix);
        file.ToString().ShouldBe(@"[Unix] ""/foo/bar.txt""");
    }

    [TestMethod]
    public void RelativeUniversalDirectory_ToString_HasFormatAndQuotedPath()
    {
        var dir = DirectoryPath.ParseRelative("foo/bar/", PathFormat.Universal);
        dir.ToString().ShouldBe(@"[Universal] ""foo/bar/""");
    }

    [TestMethod]
    public void RelativeWindowsFile_ToString_HasFormatAndQuotedPath()
    {
        var file = FilePath.ParseRelative(@"foo\bar.txt", PathFormat.Windows);
        file.ToString().ShouldBe(@"[Windows] ""foo\bar.txt""");
    }
}
