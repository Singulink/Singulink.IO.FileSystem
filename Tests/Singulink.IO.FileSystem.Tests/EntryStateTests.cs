namespace Singulink.IO.FileSystem.Tests;

[PrefixTestClass]
public class EntryStateTests
{
    private static IAbsoluteDirectoryPath SetupTestDirectory()
    {
        var testDir = DirectoryPath.GetCurrent() + DirectoryPath.ParseRelative("_test_entrystate");

        if (testDir.Exists)
            testDir.Delete(true);

        testDir.Create();
        testDir.CombineFile("existing.txt").OpenStream(FileMode.CreateNew).Dispose();
        testDir.CombineDirectory("existing_dir").Create();

        return testDir;
    }

    [TestMethod]
    public void FilePath_ExistingFile_ReturnsExists()
    {
        var testDir = SetupTestDirectory();
        var file = testDir.CombineFile("existing.txt");

        file.State.ShouldBe(EntryState.Exists);
        file.Exists.ShouldBeTrue();
    }

    [TestMethod]
    public void FilePath_PathPointsAtDirectory_ReturnsWrongType()
    {
        var testDir = SetupTestDirectory();

        // Construct a file path that actually points at an existing directory by trimming the trailing separator.
        string dirExport = testDir.CombineDirectory("existing_dir").PathExport;
        var file = FilePath.ParseAbsolute(dirExport.AsSpan().TrimEnd(['\\', '/']));

        file.State.ShouldBe(EntryState.WrongType);
        file.Exists.ShouldBeFalse();
    }

    [TestMethod]
    public void FilePath_ParentExists_ReturnsParentExists()
    {
        var testDir = SetupTestDirectory();
        var file = testDir.CombineFile("missing.txt");

        file.State.ShouldBe(EntryState.ParentExists);
        file.Exists.ShouldBeFalse();
    }

    [TestMethod]
    public void FilePath_ParentMissing_ReturnsParentDoesNotExist()
    {
        var testDir = SetupTestDirectory();
        var file = testDir.CombineDirectory("missing_dir").CombineFile("missing.txt");

        file.State.ShouldBe(EntryState.ParentDoesNotExist);
        file.Exists.ShouldBeFalse();
    }

    [TestMethod]
    public void DirectoryPath_ExistingDirectory_ReturnsExists()
    {
        var testDir = SetupTestDirectory();
        var dir = testDir.CombineDirectory("existing_dir");

        dir.State.ShouldBe(EntryState.Exists);
        dir.Exists.ShouldBeTrue();
    }

    [TestMethod]
    public void DirectoryPath_PathPointsAtFile_ReturnsWrongType()
    {
        var testDir = SetupTestDirectory();
        var dir = testDir.CombineDirectory("existing.txt");

        dir.State.ShouldBe(EntryState.WrongType);
        dir.Exists.ShouldBeFalse();
    }

    [TestMethod]
    public void DirectoryPath_ParentExists_ReturnsParentExists()
    {
        var testDir = SetupTestDirectory();
        var dir = testDir.CombineDirectory("missing_dir");

        dir.State.ShouldBe(EntryState.ParentExists);
        dir.Exists.ShouldBeFalse();
    }

    [TestMethod]
    public void DirectoryPath_ParentMissing_ReturnsParentDoesNotExist()
    {
        var testDir = SetupTestDirectory();
        var dir = testDir.CombineDirectory("missing_dir").CombineDirectory("nested");

        dir.State.ShouldBe(EntryState.ParentDoesNotExist);
        dir.Exists.ShouldBeFalse();
    }
}
