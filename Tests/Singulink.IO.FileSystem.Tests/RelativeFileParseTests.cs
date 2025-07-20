namespace Singulink.IO.FileSystem.Tests;

[PrefixTestClass]
public class RelativeFileParseTests
{
    [TestMethod]
    public void ParseToCorrectType()
    {
        var files = new[] {
            FilePath.Parse("test.sdf", PathFormat.Unix),
            FilePath.Parse("./test.sdf", PathFormat.Unix),
            FilePath.Parse("../test.sdf", PathFormat.Unix),

            FilePath.Parse("test.sdf", PathFormat.Universal),
            FilePath.Parse("./test.sdf", PathFormat.Universal),
            FilePath.Parse("../test.sdf", PathFormat.Universal),

            FilePath.Parse("test.rga", PathFormat.Windows),
            FilePath.Parse("/test.rga", PathFormat.Windows),
            FilePath.Parse("./test.sdf", PathFormat.Windows),
            FilePath.Parse("../test.sdf", PathFormat.Windows),
            FilePath.Parse(@"\test.agae", PathFormat.Windows),
            FilePath.Parse(@".\test.sdf", PathFormat.Windows),
            FilePath.Parse(@"..\test.sdf", PathFormat.Windows),
        };

        foreach (var file in files)
            (file is IRelativeFilePath).ShouldBeTrue();
    }

    [TestMethod]
    public void NoMissingFilePaths()
    {
        Should.Throw<ArgumentException>(() => FilePath.ParseRelative("", PathFormat.Windows));
        Should.Throw<ArgumentException>(() => FilePath.ParseRelative(@"\", PathFormat.Windows));
        Should.Throw<ArgumentException>(() => FilePath.ParseRelative(@"test\", PathFormat.Windows));
        Should.Throw<ArgumentException>(() => FilePath.ParseRelative(@"test\..", PathFormat.Windows));
        Should.Throw<ArgumentException>(() => FilePath.ParseRelative(@"test.txt\.", PathFormat.Windows));
        Should.Throw<ArgumentException>(() => FilePath.ParseRelative(@"test\test.txt\..", PathFormat.Windows));

        Should.Throw<ArgumentException>(() => FilePath.ParseRelative("", PathFormat.Unix));
        Should.Throw<ArgumentException>(() => FilePath.ParseRelative("test/", PathFormat.Unix));
        Should.Throw<ArgumentException>(() => FilePath.ParseRelative("test/..", PathFormat.Unix));
        Should.Throw<ArgumentException>(() => FilePath.ParseRelative("test.txt/.", PathFormat.Unix));
        Should.Throw<ArgumentException>(() => FilePath.ParseRelative("test/test.txt/..", PathFormat.Unix));

        Should.Throw<ArgumentException>(() => FilePath.ParseRelative("", PathFormat.Universal));
        Should.Throw<ArgumentException>(() => FilePath.ParseRelative("test/", PathFormat.Universal));
        Should.Throw<ArgumentException>(() => FilePath.ParseRelative("test/..", PathFormat.Universal));
        Should.Throw<ArgumentException>(() => FilePath.ParseRelative("test.txt/.", PathFormat.Universal));
        Should.Throw<ArgumentException>(() => FilePath.ParseRelative("test/test.txt/..", PathFormat.Universal));
    }
}
