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
