namespace Singulink.IO.FileSystem.Tests;

[PrefixTestClass]
public class RelativeDirectoryParseTests
{
    [TestMethod]
    public void ParseToCorrectType()
    {
        var dirs = new[] {
            DirectoryPath.Parse("test", PathFormat.Unix),
            DirectoryPath.Parse("./test", PathFormat.Unix),
            DirectoryPath.Parse("../test", PathFormat.Unix),

            DirectoryPath.Parse("test", PathFormat.Universal),
            DirectoryPath.Parse("./test", PathFormat.Universal),
            DirectoryPath.Parse("../test", PathFormat.Universal),

            DirectoryPath.Parse("test", PathFormat.Windows),
            DirectoryPath.Parse("/test", PathFormat.Windows),
            DirectoryPath.Parse("./test", PathFormat.Windows),
            DirectoryPath.Parse("../test", PathFormat.Windows),
            DirectoryPath.Parse(@"\test", PathFormat.Windows),
            DirectoryPath.Parse(@".\test", PathFormat.Windows),
            DirectoryPath.Parse(@"..\test", PathFormat.Windows),
        };

        foreach (var dir in dirs)
            (dir is IRelativeDirectoryPath).ShouldBeTrue();
    }

    [TestMethod]
    public void SpecialCurrent()
    {
        var dir = DirectoryPath.ParseRelative("", PathOptions.None);
        dir.Name.ShouldBe("");
        dir.PathDisplay.ShouldBe("");
        dir.IsRooted.ShouldBeFalse();

        dir = DirectoryPath.ParseRelative(".", PathOptions.None);
        dir.Name.ShouldBe("");
        dir.PathDisplay.ShouldBe("");
        dir.IsRooted.ShouldBeFalse();
    }

    [TestMethod]
    public void SpecialParent()
    {
        var dir = DirectoryPath.ParseRelative("..", PathOptions.None);
        dir.Name.ShouldBe("");
        dir.PathDisplay.ShouldBe("..");
        dir.IsRooted.ShouldBeFalse();

        dir = DirectoryPath.ParseRelative("../..", PathFormat.Unix, PathOptions.None);
        dir.Name.ShouldBe("");
        dir.PathDisplay.ShouldBe("../..");
        dir.IsRooted.ShouldBeFalse();
    }

    [TestMethod]
    public void Rooted()
    {
        var dir = DirectoryPath.ParseRelative("/", PathFormat.Windows, PathOptions.None);
        dir.Name.ShouldBe("");
        dir.PathDisplay.ShouldBe(@"\");
        dir.IsRooted.ShouldBeTrue();

        dir = DirectoryPath.ParseRelative("/test", PathFormat.Windows, PathOptions.None);
        dir.Name.ShouldBe("test");
        dir.PathDisplay.ShouldBe(@"\test");
        dir.IsRooted.ShouldBeTrue();
    }

    [TestMethod]
    public void NoUnixRooted()
    {
        Should.Throw<ArgumentException>(() => DirectoryPath.ParseRelative("/", PathFormat.Unix, PathOptions.None));
        Should.Throw<ArgumentException>(() => DirectoryPath.ParseRelative("/test", PathFormat.Unix, PathOptions.None));
    }

    [TestMethod]
    public void EmptyDirectories()
    {
        var dir = DirectoryPath.ParseRelative("test////", PathFormat.Universal, PathOptions.AllowEmptyDirectories);
        dir.PathDisplay.ShouldBe("test");

        dir = DirectoryPath.ParseRelative(".///test////", PathFormat.Universal, PathOptions.AllowEmptyDirectories);
        dir.PathDisplay.ShouldBe("test");
    }

    [TestMethod]
    public void Navigation()
    {
        var dir = DirectoryPath.ParseRelative("..////../test//././", PathFormat.Universal, PathOptions.AllowEmptyDirectories);
        dir.PathDisplay.ShouldBe("../../test");
        dir.IsRooted.ShouldBeFalse();

        dir = DirectoryPath.ParseRelative("..////../test//.././", PathFormat.Universal, PathOptions.AllowEmptyDirectories);
        dir.PathDisplay.ShouldBe("../..");
        dir.IsRooted.ShouldBeFalse();
    }

    [TestMethod]
    public void RootedNavigation()
    {
        var dir = DirectoryPath.ParseRelative("/test/../test2/../test3", PathFormat.Windows, PathOptions.None);
        dir.PathDisplay.ShouldBe(@"\test3");
        dir.IsRooted.ShouldBeTrue();

        dir = DirectoryPath.ParseRelative("/test/../test2/../test3/./..", PathFormat.Windows, PathOptions.None);
        dir.PathDisplay.ShouldBe(@"\");
        dir.IsRooted.ShouldBeTrue();
    }

    [TestMethod]
    public void NavigatePastRoot()
    {
        Should.Throw<ArgumentException>(() => DirectoryPath.ParseRelative("/test/../..", PathFormat.Windows, PathOptions.None));
        Should.Throw<ArgumentException>(() => DirectoryPath.ParseRelative("/..", PathFormat.Windows, PathOptions.None));
    }

    [TestMethod]
    public void NoNavigation()
    {
        Should.Throw<ArgumentException>(() => DirectoryPath.ParseRelative(".", PathOptions.NoNavigation));
        Should.Throw<ArgumentException>(() => DirectoryPath.ParseRelative("..", PathOptions.NoNavigation));
        Should.Throw<ArgumentException>(() => DirectoryPath.ParseRelative("test/value/.", PathOptions.NoNavigation));
        Should.Throw<ArgumentException>(() => DirectoryPath.ParseRelative("test/value/..", PathOptions.NoNavigation));
        Should.Throw<ArgumentException>(() => DirectoryPath.ParseRelative("/test", PathFormat.Windows, PathOptions.NoNavigation));
        Should.Throw<ArgumentException>(() => DirectoryPath.ParseRelative("/", PathFormat.Windows, PathOptions.NoNavigation));
        Should.Throw<ArgumentException>(() => DirectoryPath.ParseRelative("/test", PathFormat.Windows, PathOptions.NoNavigation));
    }

    [TestMethod]
    public void PathFormatDependent()
    {
        var dir = DirectoryPath.ParseRelative("./ test.", PathFormat.Universal, PathOptions.None);
        dir.PathDisplay.ShouldBe(" test.");
        Should.Throw<ArgumentException>(() => DirectoryPath.ParseRelative("/ test.", PathFormat.Universal, PathOptions.PathFormatDependent));

        dir = DirectoryPath.ParseRelative("./ test.", PathFormat.Unix, PathOptions.PathFormatDependent);
        dir.PathDisplay.ShouldBe(" test.");

        dir = DirectoryPath.ParseRelative("/ test.", PathFormat.Windows, PathOptions.None);
        dir.PathDisplay.ShouldBe(@"\ test.");
        Should.Throw<ArgumentException>(() => DirectoryPath.ParseRelative("/ test.", PathFormat.Windows, PathOptions.PathFormatDependent));
    }

    [TestMethod]
    public void PathOptionExceptions()
    {
        Should.Throw<ArgumentException>(() => DirectoryPath.ParseRelative("/ test", PathFormat.Windows, PathOptions.NoLeadingSpaces));
        Should.Throw<ArgumentException>(() => DirectoryPath.ParseRelative("/test ", PathFormat.Windows, PathOptions.NoTrailingSpaces));
        Should.Throw<ArgumentException>(() => DirectoryPath.ParseRelative("/test.", PathFormat.Windows, PathOptions.NoTrailingDots));
        Should.Throw<ArgumentException>(() => DirectoryPath.ParseRelative("/test/..", PathFormat.Windows, PathOptions.NoNavigation));
        Should.Throw<ArgumentException>(() => DirectoryPath.ParseRelative("/./", PathFormat.Windows, PathOptions.NoNavigation));

        Should.Throw<ArgumentException>(() => DirectoryPath.ParseRelative("/COM1/", PathFormat.Windows, PathOptions.NoReservedDeviceNames));
        Should.Throw<ArgumentException>(() => DirectoryPath.ParseRelative("AUX", PathFormat.Windows, PathOptions.NoReservedDeviceNames));
        Should.Throw<ArgumentException>(() => DirectoryPath.ParseRelative("CON", PathFormat.Windows, PathOptions.NoReservedDeviceNames));
        Should.Throw<ArgumentException>(() => DirectoryPath.ParseRelative("/test/NUL", PathFormat.Windows, PathOptions.NoReservedDeviceNames));
        Should.Throw<ArgumentException>(() => DirectoryPath.ParseRelative("PRN/test", PathFormat.Windows, PathOptions.NoReservedDeviceNames));
        Should.Throw<ArgumentException>(() => DirectoryPath.ParseRelative("/test/LPT5/test2", PathFormat.Windows, PathOptions.NoReservedDeviceNames));
    }
}
