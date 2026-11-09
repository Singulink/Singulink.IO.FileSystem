namespace Singulink.IO.FileSystem.Tests;

[PrefixTestClass]
public class AbsoluteFileExtensionTests
{
    [TestMethod]
    public void WithExtension_RootFile()
    {
        var filePath = FilePath.ParseAbsolute("/file.txt", PathFormat.Unix);
        var newFilePath = filePath.WithExtension(".md", PathOptions.None);

        newFilePath.PathDisplay.ShouldBe("/file.md");

        filePath = FilePath.ParseAbsolute(@"C:\file.txt", PathFormat.Windows);
        newFilePath = filePath.WithExtension(".md", PathOptions.None);

        newFilePath.PathDisplay.ShouldBe(@"C:\file.md");
    }

    [TestMethod]
    [DataRow(false, "/.txt", ".", ".")]
    [DataRow(false, "/dir/..txt", ".", "..")]
    [DataRow(true, @"C:\..txt", "", ".")]
    [DataRow(true, @"\\server\share\..txt", ".", "..")]
    public void WithExtension_NavigationalName_Throws(bool windows, string path, string extension, string newName)
    {
        var filePath = FilePath.ParseAbsolute(path, windows ? PathFormat.Windows : PathFormat.Unix, PathOptions.None);

        var ex = Should.Throw<ArgumentException>(() => filePath.WithExtension(extension, PathOptions.None));
        ex.Message.ShouldBe($"Invalid new file name: '{newName}' is a navigational path segment. (Parameter 'extension')");
        ex.ParamName.ShouldBe("extension");
    }

    [TestMethod]
    public void WithExtension_ChildDirFile()
    {
        var filePath = FilePath.ParseAbsolute("/dir/file.txt", PathFormat.Unix);
        var newFilePath = filePath.WithExtension(".md", PathOptions.None);

        newFilePath.PathDisplay.ShouldBe("/dir/file.md");

        filePath = FilePath.ParseAbsolute(@"C:\dir\file.txt", PathFormat.Windows);
        newFilePath = filePath.WithExtension(".md", PathOptions.None);

        newFilePath.PathDisplay.ShouldBe(@"C:\dir\file.md");
    }

    [TestMethod]
    public void WithExtension_ToNoExtension()
    {
        var filePath = FilePath.ParseAbsolute("/file.txt", PathFormat.Unix);
        var newFilePath = filePath.WithExtension(null, PathOptions.None);

        newFilePath.PathDisplay.ShouldBe("/file");

        filePath = FilePath.ParseAbsolute(@"C:\file.txt", PathFormat.Windows);
        newFilePath = filePath.WithExtension("", PathOptions.None);

        newFilePath.PathDisplay.ShouldBe(@"C:\file");
    }

    [TestMethod]
    public void WithExtension_FromNoExtension()
    {
        var filePath = FilePath.ParseAbsolute("/file", PathFormat.Unix);
        var newFilePath = filePath.WithExtension(".txt", PathOptions.None);

        newFilePath.PathDisplay.ShouldBe("/file.txt");

        filePath = FilePath.ParseAbsolute(@"C:\file", PathFormat.Windows);
        newFilePath = filePath.WithExtension(".txt", PathOptions.None);

        newFilePath.PathDisplay.ShouldBe(@"C:\file.txt");
    }
}
