namespace Singulink.IO.FileSystem.Tests;

[PrefixTestClass]
public class RelativeFileExtensionTests
{
    [TestMethod]
    public void WithExtension_RootFile()
    {
        var filePath = FilePath.ParseRelative("file.txt", PathFormat.Unix);
        var newFilePath = filePath.WithExtension(".md", PathOptions.None);

        newFilePath.PathDisplay.ShouldBe("file.md");

        filePath = FilePath.ParseRelative("file.txt", PathFormat.Windows);
        newFilePath = filePath.WithExtension(".md", PathOptions.None);

        newFilePath.PathDisplay.ShouldBe("file.md");
    }

    [TestMethod]
    public void WithExtension_ChildDirFile()
    {
        var filePath = FilePath.ParseRelative("dir/file.txt", PathFormat.Unix);
        var newFilePath = filePath.WithExtension(".md", PathOptions.None);

        newFilePath.PathDisplay.ShouldBe("dir/file.md");

        filePath = FilePath.ParseRelative(@"dir\file.txt", PathFormat.Windows);
        newFilePath = filePath.WithExtension(".md", PathOptions.None);

        newFilePath.PathDisplay.ShouldBe(@"dir\file.md");
    }

    [TestMethod]
    public void WithExtension_ToNoExtension()
    {
        var filePath = FilePath.ParseRelative("file.txt", PathFormat.Unix);
        var newFilePath = filePath.WithExtension(null, PathOptions.None);

        newFilePath.PathDisplay.ShouldBe("file");

        filePath = FilePath.ParseRelative("file.txt", PathFormat.Windows);
        newFilePath = filePath.WithExtension("", PathOptions.None);

        newFilePath.PathDisplay.ShouldBe("file");
    }

    [TestMethod]
    [DataRow("Unix", ".txt", ".", ".")]
    [DataRow("Unix", "dir/..txt", "", ".")]
    [DataRow("Unix", "dir/..txt", ".", "..")]
    [DataRow("Windows", @"..\.txt", ".", ".")]
    [DataRow("Windows", @"dir\..txt", ".", "..")]
    [DataRow("Universal", "..txt", ".", "..")]
    public void WithExtension_NavigationalName_Throws(string formatName, string path, string extension, string newName)
    {
        var filePath = FilePath.ParseRelative(path, GetFormat(formatName), PathOptions.None);

        var ex = Should.Throw<ArgumentException>(() => filePath.WithExtension(extension, PathOptions.None));
        ex.Message.ShouldBe($"Invalid new file name: '{newName}' is a navigational path segment. (Parameter 'extension')");
        ex.ParamName.ShouldBe("extension");
    }

    [TestMethod]
    public void WithExtension_FromNoExtension()
    {
        var filePath = FilePath.ParseRelative("file", PathFormat.Unix);
        var newFilePath = filePath.WithExtension(".txt", PathOptions.None);

        newFilePath.PathDisplay.ShouldBe("file.txt");

        filePath = FilePath.ParseRelative("file", PathFormat.Windows);
        newFilePath = filePath.WithExtension(".txt", PathOptions.None);

        newFilePath.PathDisplay.ShouldBe("file.txt");
    }
}
