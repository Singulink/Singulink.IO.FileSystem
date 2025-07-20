namespace Singulink.IO.FileSystem.Tests;

[PrefixTestClass]
public class AbsoluteDirectoryParseTests
{
    [DataTestMethod]
    [DataRow("c:/test")]
    [DataRow("c:")]
    [DataRow(@"c:\test")]
    [DataRow(@"c:\")]
    [DataRow(@"\\server\test")]
    [DataRow(@"\\server\test\")]
    [DataRow("//server/test")]
    [DataRow("//server/test/test")]
    public void ParseToCorrectWindowsType(string path)
    {
        var dir = DirectoryPath.Parse(path, PathFormat.Windows);
        (dir is IAbsoluteDirectoryPath).ShouldBeTrue();
    }

    [DataTestMethod]
    [DataRow("/")]
    [DataRow("/test")]
    public void ParseToCorrectUnixType(string path)
    {
        var dir = DirectoryPath.Parse(path, PathFormat.Unix);
        (dir is IAbsoluteDirectoryPath).ShouldBeTrue();
    }

    [TestMethod]
    public void RootUnix()
    {
        var dir = DirectoryPath.ParseAbsolute("/", PathFormat.Unix, PathOptions.None);
        dir.Name.ShouldBe("/");
        dir.PathDisplay.ShouldBe("/");
        dir.PathExport.ShouldBe("/");
        dir.IsRooted.ShouldBeTrue();
        dir.IsRoot.ShouldBeTrue();
    }

    [TestMethod]
    public void RootWindowsDrive()
    {
        var dir = DirectoryPath.ParseAbsolute("c:", PathFormat.Windows, PathOptions.None);
        dir.Name.ShouldBe("c:");
        dir.PathDisplay.ShouldBe(@"c:\");
        dir.PathExport.ShouldBe(@"\\?\c:\");
        dir.IsRooted.ShouldBeTrue();
        dir.IsRoot.ShouldBeTrue();

        dir = DirectoryPath.ParseAbsolute("x:/", PathFormat.Windows, PathOptions.None);
        dir.Name.ShouldBe("x:");
        dir.PathDisplay.ShouldBe(@"x:\");
        dir.PathExport.ShouldBe(@"\\?\x:\");
        dir.IsRooted.ShouldBeTrue();
        dir.IsRoot.ShouldBeTrue();
    }

    [TestMethod]
    public void RootWindowsUnc()
    {
        var dir = DirectoryPath.ParseAbsolute(@"\\Server\Share", PathFormat.Windows, PathOptions.None);
        dir.Name.ShouldBe(@"\\Server\Share");
        dir.PathDisplay.ShouldBe(@"\\Server\Share\");
        dir.PathExport.ShouldBe(@"\\?\UNC\Server\Share\");
        dir.IsRooted.ShouldBeTrue();
        dir.IsRoot.ShouldBeTrue();

        Should.Throw<ArgumentException>(() => DirectoryPath.ParseAbsolute("\\Server", PathFormat.Windows, PathOptions.None));
    }

    [DataTestMethod]
    [DataRow("test")]
    [DataRow("")]
    [DataRow("xy:/ ")]
    [DataRow("1:/")]
    [DataRow(" :/")]
    public void BadWindowsPaths(string path)
    {
        Should.Throw<ArgumentException>(() => DirectoryPath.ParseAbsolute(path, PathFormat.Windows, PathOptions.None));
    }

    [DataTestMethod]
    [DataRow("test")]
    [DataRow("")]
    [DataRow(" /")]
    public void BadUnixPaths(string path)
    {
        Should.Throw<ArgumentException>(() => DirectoryPath.ParseAbsolute(path, PathFormat.Unix, PathOptions.None));
    }

    [TestMethod]
    public void Navigation()
    {
        var dir = DirectoryPath.ParseAbsolute(@"\\Server\Share\test1\test2\..\..", PathFormat.Windows, PathOptions.None);
        dir.PathDisplay.ShouldBe(@"\\Server\Share\");

        var ex = Should.Throw<ArgumentException>(() => DirectoryPath.ParseAbsolute(@"\\Server\Share\test1\test2\..\..\..", PathFormat.Windows, PathOptions.None));
        ex.Message.ShouldBe("Attempt to navigate past root directory. (Parameter 'path')");

        dir = DirectoryPath.ParseAbsolute("c:/./test/.././", PathFormat.Windows, PathOptions.None);
        dir.PathDisplay.ShouldBe(@"c:\");
        dir.IsRoot.ShouldBeTrue();

        dir = DirectoryPath.ParseAbsolute("/./test/.././", PathFormat.Unix, PathOptions.None);
        dir.PathDisplay.ShouldBe("/");
        dir.IsRoot.ShouldBeTrue();
    }

    [TestMethod]
    public void PathFormatDependent()
    {
        var dir = DirectoryPath.ParseAbsolute("/ test.", PathFormat.Unix, PathOptions.PathFormatDependent);
        dir.PathDisplay.ShouldBe("/ test.");

        dir = DirectoryPath.ParseAbsolute("c:/ test.", PathFormat.Windows, PathOptions.None);
        dir.PathDisplay.ShouldBe(@"c:\ test.");
        Should.Throw<ArgumentException>(() => DirectoryPath.ParseRelative("/ test.", PathFormat.Windows, PathOptions.PathFormatDependent));
    }

    [DataTestMethod]
    [DataRow("/test")]
    [DataRow("/")]
    public void NoUniversal(string path)
    {
        Should.Throw<ArgumentException>(() => DirectoryPath.Parse(path, PathFormat.Universal));
        Should.Throw<ArgumentException>(() => DirectoryPath.ParseAbsolute(path, PathFormat.Universal));
    }
}
