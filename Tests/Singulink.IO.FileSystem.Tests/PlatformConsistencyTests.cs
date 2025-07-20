namespace Singulink.IO.FileSystem.Tests;

[PrefixTestClass]
public class PlatformConsistencyTests
{
    private const string FileName = "test.file";

    private static IAbsoluteDirectoryPath SetupTestDirectory()
    {
        var testDir = DirectoryPath.GetCurrent() + DirectoryPath.ParseRelative("_test");

        if (testDir.Exists)
            testDir.Delete(true);

        testDir.Create();
        testDir.CombineFile(FileName).OpenStream(FileMode.CreateNew).Dispose();

        return testDir;
    }

    [TestMethod]
    public void FileIsDirectory()
    {
        var file = FilePath.ParseAbsolute(SetupTestDirectory().PathExport);

        file.Exists.ShouldBeFalse();

        Should.Throw<FileNotFoundException>(() => _ = file.Attributes);
        Should.Throw<FileNotFoundException>(() => _ = file.CreationTime);
        Should.Throw<FileNotFoundException>(() => file.IsReadOnly = true);
        Should.Throw<FileNotFoundException>(() => file.Attributes |= FileAttributes.Hidden);
        Should.Throw<FileNotFoundException>(() => file.Length);

        Should.Throw<IOException>(() => file.Delete())
            .ShouldBeOfType<IOException>();
    }

    [TestMethod]
    public void DirectoryIsFile()
    {
        var dir = SetupTestDirectory().CombineDirectory(FileName);

        dir.Exists.ShouldBeFalse();

        Should.Throw<DirectoryNotFoundException>(() => _ = dir.IsEmpty);
        Should.Throw<DirectoryNotFoundException>(() => _ = dir.Attributes);
        Should.Throw<DirectoryNotFoundException>(() => _ = dir.CreationTime);
        Should.Throw<DirectoryNotFoundException>(() => _ = dir.AvailableFreeSpace);
        Should.Throw<DirectoryNotFoundException>(() => _ = dir.TotalFreeSpace);
        Should.Throw<DirectoryNotFoundException>(() => _ = dir.TotalSize);
        Should.Throw<DirectoryNotFoundException>(() => dir.Attributes |= FileAttributes.Hidden);
        Should.Throw<DirectoryNotFoundException>(() => _ = dir.DriveType);
        Should.Throw<DirectoryNotFoundException>(() => _ = dir.FileSystem);

        Should.Throw<IOException>(() => dir.GetChildEntries().FirstOrDefault())
            .ShouldBeOfType<IOException>();

        Should.Throw<IOException>(() => dir.Delete(true))
            .ShouldBeOfType<IOException>();
    }
}
