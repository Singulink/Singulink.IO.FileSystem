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

        Should.Throw<ArgumentException>(() => FilePath.ParseAbsolute("/", PathFormat.Unix));
        Should.Throw<ArgumentException>(() => FilePath.ParseAbsolute("/test/", PathFormat.Unix));
        Should.Throw<ArgumentException>(() => FilePath.ParseAbsolute("/test/..", PathFormat.Unix));
        Should.Throw<ArgumentException>(() => FilePath.ParseAbsolute("/test.txt/.", PathFormat.Unix));
        Should.Throw<ArgumentException>(() => FilePath.ParseAbsolute("/test/test.txt/..", PathFormat.Unix));
    }
}
