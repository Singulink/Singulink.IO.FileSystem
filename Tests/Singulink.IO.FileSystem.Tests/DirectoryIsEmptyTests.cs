namespace Singulink.IO.FileSystem.Tests;

[PrefixTestClass]
public class DirectoryIsEmptyTests
{
    private static IAbsoluteDirectoryPath SetupTestDirectory()
    {
        var testDir = DirectoryPath.GetCurrent() + DirectoryPath.ParseRelative("_test");

        if (testDir.Exists)
            testDir.Delete(true);

        testDir.Create();

        return testDir;
    }

    [TestMethod]
    public void EmptyDirectory()
    {
        var dir = SetupTestDirectory();

        dir.IsEmpty.ShouldBeTrue();
    }

    [TestMethod]
    public void DirectoryWithFile()
    {
        var dir = SetupTestDirectory();
        dir.CombineFile("test.file").OpenStream(FileMode.CreateNew).Dispose();

        dir.IsEmpty.ShouldBeFalse();
    }

    [TestMethod]
    public void DirectoryWithSubdirectory()
    {
        var dir = SetupTestDirectory();
        dir.CombineDirectory("subdir").Create();

        // An empty subdirectory is still an entry of its parent, and is itself empty.
        dir.IsEmpty.ShouldBeFalse();
        dir.CombineDirectory("subdir").IsEmpty.ShouldBeTrue();
    }

    [TestMethod]
    public void DirectoryEmptiedAgain()
    {
        var dir = SetupTestDirectory();
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
        var dir = SetupTestDirectory().CombineDirectory("missing");

        dir.Exists.ShouldBeFalse();
        Should.Throw<DirectoryNotFoundException>(() => _ = dir.IsEmpty);
    }
}
