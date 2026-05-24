namespace Singulink.IO.FileSystem.Tests;

[PrefixTestClass]
public class PathInvariantTests
{
    // ---- Trailing separator invariants ----

    [TestMethod]
    [DataRow(@"C:\foo\bar")]
    [DataRow(@"C:\")]
    [DataRow(@"\\server\share")]
    [DataRow(@"\\server\share\sub")]
    public void AbsoluteWindowsDirectory_PathDisplayAndExport_EndWithSeparator(string input)
    {
        var dir = DirectoryPath.ParseAbsolute(input, PathFormat.Windows);

        dir.PathDisplay.ShouldEndWith(@"\");
        dir.PathExport.ShouldEndWith(@"\");
    }

    [TestMethod]
    [DataRow("/foo/bar")]
    [DataRow("/")]
    public void AbsoluteUnixDirectory_PathDisplayAndExport_EndWithSeparator(string input)
    {
        var dir = DirectoryPath.ParseAbsolute(input, PathFormat.Unix);

        dir.PathDisplay.ShouldEndWith("/");
        dir.PathExport.ShouldEndWith("/");
    }

    [TestMethod]
    [DataRow(@"C:\foo\bar.txt")]
    [DataRow(@"\\server\share\file.txt")]
    public void AbsoluteWindowsFile_PathDisplay_DoesNotEndWithSeparator(string input)
    {
        var file = FilePath.ParseAbsolute(input, PathFormat.Windows);

        file.PathDisplay.ShouldNotEndWith(@"\");
        file.PathExport.ShouldNotEndWith(@"\");
    }

    [TestMethod]
    public void AbsoluteUnixFile_PathDisplay_DoesNotEndWithSeparator()
    {
        var file = FilePath.ParseAbsolute("/foo/bar.txt", PathFormat.Unix);

        file.PathDisplay.ShouldNotEndWith("/");
        file.PathExport.ShouldNotEndWith("/");
    }

    // ---- Parse roundtrip ----

    [TestMethod]
    [DataRow(@"C:\foo\bar\")]
    [DataRow(@"C:\")]
    [DataRow(@"\\server\share\")]
    [DataRow(@"\\server\share\sub\")]
    public void DirectoryParse_RoundtripsViaPathDisplay_Windows(string input)
    {
        var dir = DirectoryPath.ParseAbsolute(input, PathFormat.Windows);
        var roundtrip = DirectoryPath.ParseAbsolute(dir.PathDisplay, PathFormat.Windows);

        roundtrip.ShouldBe(dir);
    }

    [TestMethod]
    [DataRow("/foo/bar/")]
    [DataRow("/")]
    public void DirectoryParse_RoundtripsViaPathDisplay_Unix(string input)
    {
        var dir = DirectoryPath.ParseAbsolute(input, PathFormat.Unix);
        var roundtrip = DirectoryPath.ParseAbsolute(dir.PathDisplay, PathFormat.Unix);

        roundtrip.ShouldBe(dir);
    }

    [TestMethod]
    public void FileParse_RoundtripsViaPathDisplay()
    {
        var file = FilePath.ParseAbsolute(@"C:\foo\bar.txt", PathFormat.Windows);
        var roundtrip = FilePath.ParseAbsolute(file.PathDisplay, PathFormat.Windows);

        roundtrip.ShouldBe(file);
    }

    // ---- Combine/Parent identities ----

    [TestMethod]
    public void CombineFile_RecoversFromParentAndName_Windows()
    {
        var file = FilePath.ParseAbsolute(@"C:\foo\bar.txt", PathFormat.Windows);

        file.ParentDirectory.CombineFile(file.Name).ShouldBe(file);
    }

    [TestMethod]
    public void CombineFile_RecoversFromParentAndName_Unix()
    {
        var file = FilePath.ParseAbsolute("/foo/bar.txt", PathFormat.Unix);

        file.ParentDirectory.CombineFile(file.Name).ShouldBe(file);
    }

    [TestMethod]
    public void CombineDirectory_RecoversFromParentAndName_Windows()
    {
        var child = DirectoryPath.ParseAbsolute(@"C:\foo\bar\", PathFormat.Windows);

        child.ParentDirectory!.CombineDirectory(child.Name).ShouldBe(child);
    }

    [TestMethod]
    public void CombineDirectory_RecoversFromParentAndName_Unix()
    {
        var child = DirectoryPath.ParseAbsolute("/foo/bar/", PathFormat.Unix);

        child.ParentDirectory!.CombineDirectory(child.Name).ShouldBe(child);
    }
}
