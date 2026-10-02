namespace Singulink.IO.FileSystem.Tests;

[PrefixTestClass]
public class AbsoluteFileParseTests
{
    [TestMethod]
    public void ParseToCorrectType()
    {
        var files = new[] {
            FilePath.Parse("/test.sdf", PathFormat.Unix),
            FilePath.Parse("c:/test.rga", PathFormat.Windows),
            FilePath.Parse(@"c:\test.agae", PathFormat.Windows),
            FilePath.Parse(@"\\server\test\test.sef", PathFormat.Windows),
            FilePath.Parse("//server/test/test.rae", PathFormat.Windows),
        };

        foreach (var file in files)
        {
            (file is IAbsoluteFilePath).ShouldBeTrue();
        }
    }

    [TestMethod]
    public void NoUniversal()
    {
        Should.Throw<ArgumentException>(() => FilePath.Parse("/test.asdf", PathFormat.Universal));
        Should.Throw<ArgumentException>(() => FilePath.ParseAbsolute("/test.sdf", PathFormat.Universal));
    }

    [TestMethod]
    public void NoMissingFilePaths()
    {
        Should.Throw<ArgumentException>(() => FilePath.ParseAbsolute("C:", PathFormat.Windows));
        Should.Throw<ArgumentException>(() => FilePath.ParseAbsolute(@"C:\", PathFormat.Windows));
        Should.Throw<ArgumentException>(() => FilePath.ParseAbsolute(@"C:\test\", PathFormat.Windows));
        Should.Throw<ArgumentException>(() => FilePath.ParseAbsolute(@"C:\test\..", PathFormat.Windows));
        Should.Throw<ArgumentException>(() => FilePath.ParseAbsolute(@"C:\test.txt\.", PathFormat.Windows));
        Should.Throw<ArgumentException>(() => FilePath.ParseAbsolute(@"C:\test\test.txt\..", PathFormat.Windows));

        Should.Throw<ArgumentException>(() => FilePath.ParseAbsolute(@"\\server\share", PathFormat.Windows));
        Should.Throw<ArgumentException>(() => FilePath.ParseAbsolute(@"\\server\share\", PathFormat.Windows));

        Should.Throw<ArgumentException>(() => FilePath.ParseAbsolute("/", PathFormat.Unix));
        Should.Throw<ArgumentException>(() => FilePath.ParseAbsolute("/test/", PathFormat.Unix));
        Should.Throw<ArgumentException>(() => FilePath.ParseAbsolute("/test/..", PathFormat.Unix));
        Should.Throw<ArgumentException>(() => FilePath.ParseAbsolute("/test.txt/.", PathFormat.Unix));
        Should.Throw<ArgumentException>(() => FilePath.ParseAbsolute("/test/test.txt/..", PathFormat.Unix));
    }

    [TestMethod]
    public void TryParse()
    {
        FilePath.TryParseAbsolute(@"\\server\test\test.sef", PathFormat.Windows, PathOptions.NoUnfriendlyNames, out var file).ShouldBeTrue();
        file.ShouldBe(FilePath.ParseAbsolute(@"\\server\test\test.sef", PathFormat.Windows));

        FilePath.TryParse("/test.sdf", PathFormat.Unix, PathOptions.NoUnfriendlyNames, out var anyFile).ShouldBeTrue();
        (anyFile is IAbsoluteFilePath).ShouldBeTrue();

        FilePath.TryParseAbsolute(@"C:\test\", PathFormat.Windows, PathOptions.NoUnfriendlyNames, out file).ShouldBeFalse();
        file.ShouldBeNull();

        FilePath.TryParseAbsolute(@"\\server\share", PathFormat.Windows, PathOptions.NoUnfriendlyNames, out file).ShouldBeFalse();
        file.ShouldBeNull();

        FilePath.TryParseAbsolute("/test.sdf", PathFormat.Universal, PathOptions.NoUnfriendlyNames, out file).ShouldBeFalse();
        file.ShouldBeNull();
    }
}
