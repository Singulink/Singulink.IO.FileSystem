namespace Singulink.IO.FileSystem.Tests;

[PrefixTestClass]
public class SystemExtensionsTests
{
    private static IAbsoluteDirectoryPath SetupTestDirectory()
    {
        var testDir = DirectoryPath.GetCurrent() + DirectoryPath.ParseRelative("_test_sysext");

        if (testDir.Exists)
            testDir.Delete(true);

        testDir.Create();
        testDir.CombineFile("file.txt").OpenStream(FileMode.CreateNew).Dispose();

        return testDir;
    }

    [TestMethod]
    public void DirectoryInfoToPath_RoundtripsViaPathExport()
    {
        var testDir = SetupTestDirectory();
        var dirInfo = new DirectoryInfo(testDir.PathExport);

        var roundtripped = dirInfo.ToPath();

        roundtripped.ShouldBe(testDir);
        roundtripped.PathDisplay.ShouldBe(testDir.PathDisplay);
    }

    [TestMethod]
    public void FileInfoToPath_RoundtripsViaPathExport()
    {
        var testDir = SetupTestDirectory();
        var expected = testDir.CombineFile("file.txt");
        var fileInfo = new FileInfo(expected.PathExport);

        var roundtripped = fileInfo.ToPath();

        roundtripped.ShouldBe(expected);
        roundtripped.PathDisplay.ShouldBe(expected.PathDisplay);
    }

    [TestMethod]
    public void DirectoryInfoToPath_NoUnfriendlyNamesByDefault_Throws()
    {
        // " " is an unfriendly name (whitespace-only). A DirectoryInfo can hold one, but ToPath should refuse it by default.
        var dirInfo = new DirectoryInfo(Path.Combine(DirectoryPath.GetCurrent().PathExport, " "));

        Should.Throw<ArgumentException>(() => dirInfo.ToPath());
    }

    [TestMethod]
    public void DirectoryInfoToPath_AcceptsUnfriendlyNamesWithPathOptionsNone()
    {
        var dirInfo = new DirectoryInfo(Path.Combine(DirectoryPath.GetCurrent().PathExport, " "));

        var path = dirInfo.ToPath(PathOptions.None);
        path.Name.ShouldBe(" ");
    }
}
