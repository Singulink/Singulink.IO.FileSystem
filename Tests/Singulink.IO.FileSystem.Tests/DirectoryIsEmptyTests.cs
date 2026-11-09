namespace Singulink.IO.FileSystem.Tests;

[PrefixTestClass]
public class DirectoryIsEmptyTests
{
    private const string TestDirName = "_test_isempty";

    [TestMethod]
    public void EmptyDirectory()
    {
        var dir = CreateEmptyTestDirectory(TestDirName);

        dir.IsEmpty.ShouldBeTrue();
    }

    [TestMethod]
    public void DirectoryWithFile()
    {
        var dir = CreateEmptyTestDirectory(TestDirName);
        dir.CombineFile("test.file").OpenStream(FileMode.CreateNew).Dispose();

        dir.IsEmpty.ShouldBeFalse();
    }

    [TestMethod]
    public void DirectoryWithSubdirectory()
    {
        var dir = CreateEmptyTestDirectory(TestDirName);
        dir.CombineDirectory("subdir").Create();

        // An empty subdirectory is still an entry of its parent, and is itself empty.
        dir.IsEmpty.ShouldBeFalse();
        dir.CombineDirectory("subdir").IsEmpty.ShouldBeTrue();
    }

    [TestMethod]
    public void DirectoryEmptiedAgain()
    {
        var dir = CreateEmptyTestDirectory(TestDirName);
        var file = dir.CombineFile("test.file");
        var subDir = dir.CombineDirectory("subdir");

        file.OpenStream(FileMode.CreateNew).Dispose();
        subDir.Create();
        dir.IsEmpty.ShouldBeFalse();

        file.Delete();
        dir.IsEmpty.ShouldBeFalse();

        subDir.Delete();
        dir.IsEmpty.ShouldBeTrue();
    }

    [TestMethod]
    public void MissingDirectory()
    {
        var dir = CreateEmptyTestDirectory(TestDirName).CombineDirectory("missing");

        dir.Exists.ShouldBeFalse();
        Should.Throw<DirectoryNotFoundException>(() => _ = dir.IsEmpty);
    }
}
