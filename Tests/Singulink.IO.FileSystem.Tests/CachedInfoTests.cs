namespace Singulink.IO.FileSystem.Tests;

[PrefixTestClass]
public class CachedInfoTests
{
    private const string FileName = "file.txt";
    private const string SubDirName = "sub";

    private static IAbsoluteDirectoryPath SetupTestDirectory()
    {
        var testDir = DirectoryPath.GetCurrent() + DirectoryPath.ParseRelative("_test_cachedinfo");

        if (testDir.Exists)
            testDir.Delete(true);

        testDir.Create();

        using (var s = testDir.CombineFile(FileName).OpenStream(FileMode.CreateNew))
            s.WriteByte(0x41);

        testDir.CombineDirectory(SubDirName).Create();

        return testDir;
    }

    // ---- CachedEntryInfo.Create ----

    [TestMethod]
    public void Create_ExistingFile_ReturnsCachedFileInfo()
    {
        var testDir = SetupTestDirectory();
        var info = CachedEntryInfo.Create(testDir.CombineFile(FileName).PathDisplay);

        info.ShouldBeOfType<CachedFileInfo>();
        info.Path.ShouldBeAssignableTo<IAbsoluteFilePath>();
        info.Path.PathDisplay.ShouldBe(testDir.CombineFile(FileName).PathDisplay);
        ((CachedFileInfo)info).Length.ShouldBe(1);
    }

    [TestMethod]
    public void Create_ExistingDirectory_ReturnsCachedDirectoryInfo()
    {
        var testDir = SetupTestDirectory();
        var dirPath = testDir.CombineDirectory(SubDirName);
        var info = CachedEntryInfo.Create(dirPath.PathDisplay);

        info.ShouldBeOfType<CachedDirectoryInfo>();
        info.Attributes.HasFlag(FileAttributes.Directory).ShouldBeTrue();
    }

    [TestMethod]
    public void Create_RootDirectory_ReturnsCachedDirectoryInfo()
    {
        // The root directory's PathDisplay has no trailing-path-name to slice off; ensure CreateCore handles it correctly.
        var root = DirectoryPath.GetCurrent().RootDirectory;
        var info = CachedEntryInfo.Create(root.PathDisplay);

        info.ShouldBeOfType<CachedDirectoryInfo>();
        info.Path.PathDisplay.ShouldBe(root.PathDisplay);
    }

    [TestMethod]
    public void Create_MissingEntry_ThrowsFileNotFound()
    {
        var testDir = SetupTestDirectory();
        var missing = testDir.CombineFile("missing.txt");

        Should.Throw<FileNotFoundException>(() => CachedEntryInfo.Create(missing.PathDisplay));
    }

    [TestMethod]
    public void Create_MissingParent_ThrowsDirectoryNotFound()
    {
        var testDir = SetupTestDirectory();
        var missing = testDir.CombineDirectory("missing").CombineFile("x.txt");

        Should.Throw<DirectoryNotFoundException>(() => CachedEntryInfo.Create(missing.PathDisplay));
    }

    // ---- IAbsoluteDirectoryPath.GetInfo (relative span) ----

    [TestMethod]
    public void GetInfo_RelativeFile_ReturnsCachedFileInfo()
    {
        var testDir = SetupTestDirectory();
        var info = testDir.GetInfo(FileName);

        info.ShouldBeOfType<CachedFileInfo>();
        info.Path.PathDisplay.ShouldBe(testDir.CombineFile(FileName).PathDisplay);
    }

    [TestMethod]
    public void GetInfo_RelativeDirectory_ReturnsCachedDirectoryInfo()
    {
        var testDir = SetupTestDirectory();
        var info = testDir.GetInfo(SubDirName);

        info.ShouldBeOfType<CachedDirectoryInfo>();
    }

    [TestMethod]
    public void GetInfo_UniversalFormat_ParsesAppendedPath()
    {
        var testDir = SetupTestDirectory();

        // Always-forward-slash universal path is accepted regardless of platform.
        var info = testDir.GetInfo("sub", PathFormat.Universal);

        info.ShouldBeOfType<CachedDirectoryInfo>();
    }

    [TestMethod]
    public void GetInfo_Missing_ThrowsFileNotFound()
    {
        var testDir = SetupTestDirectory();

        Should.Throw<FileNotFoundException>(() => testDir.GetInfo("missing.txt"));
    }

    // ---- Validation ----

    [TestMethod]
    public void CachedFileInfo_Refresh_AfterEntryBecomesDirectory_LeavesSnapshotIntact()
    {
        var testDir = SetupTestDirectory();
        var filePath = testDir.CombineFile("swap.txt");

        using (var s = filePath.OpenStream(FileMode.CreateNew))
            s.WriteByte(0xFF);

        var info = (CachedFileInfo)CachedEntryInfo.Create(filePath.PathDisplay);
        long originalLength = info.Length;
        var originalAttrs = info.Attributes;

        // Replace the file with a directory at the same path.
        filePath.Delete();
        testDir.CombineDirectory("swap.txt").Create();

        Should.Throw<IOException>(() => info.Refresh());

        info.Length.ShouldBe(originalLength);
        info.Attributes.ShouldBe(originalAttrs);
    }

    [TestMethod]
    public void CachedDirectoryInfo_Refresh_AfterEntryBecomesFile_LeavesSnapshotIntact()
    {
        var testDir = SetupTestDirectory();
        var dirPath = testDir.CombineDirectory("swap");
        dirPath.Create();

        var info = (CachedDirectoryInfo)CachedEntryInfo.Create(dirPath.PathDisplay);
        var originalAttrs = info.Attributes;

        dirPath.Delete(false);

        using (var s = testDir.CombineFile("swap").OpenStream(FileMode.CreateNew))
            s.WriteByte(0x00);

        Should.Throw<IOException>(() => info.Refresh());

        info.Attributes.ShouldBe(originalAttrs);
    }

    // ---- Snapshot semantics ----

    [TestMethod]
    public void CachedFileInfo_Length_StaysStaleUntilRefresh()
    {
        var testDir = SetupTestDirectory();
        var filePath = testDir.CombineFile("growing.bin");

        using (var s = filePath.OpenStream(FileMode.CreateNew))
            s.WriteByte(0x01);

        var info = (CachedFileInfo)CachedEntryInfo.Create(filePath.PathDisplay);
        info.Length.ShouldBe(1);

        using (var s = filePath.OpenStream(FileMode.Append, FileAccess.Write))
        {
            s.WriteByte(0x02);
            s.WriteByte(0x03);
        }

        info.Length.ShouldBe(1, "snapshot should be stale before Refresh");

        info.Refresh();
        info.Length.ShouldBe(3);
    }

    // ---- ToString ----

    [TestMethod]
    public void ToString_ReturnsPathToString()
    {
        var testDir = SetupTestDirectory();
        var info = CachedEntryInfo.Create(testDir.CombineFile(FileName).PathDisplay);

        info.ToString().ShouldBe(info.Path.ToString());
    }

    // ---- GetChildEntriesInfo concrete types ----

    [TestMethod]
    public void GetChildEntriesInfo_ReturnsCorrectConcreteTypes()
    {
        var testDir = SetupTestDirectory();

        var entries = testDir.GetChildEntriesInfo().ToList();
        entries.OfType<CachedFileInfo>().Select(f => f.Path.Name).ShouldContain(FileName);
        entries.OfType<CachedDirectoryInfo>().Select(d => d.Path.Name).ShouldContain(SubDirName);
    }

    // ---- Path-shape behavior ----

    [TestMethod]
    public void Create_DirectoryShapedPath_PointingAtFile_ThrowsIOException()
    {
        var testDir = SetupTestDirectory();
        string filePathWithSep = testDir.CombineFile(FileName).PathDisplay + System.IO.Path.DirectorySeparatorChar;

        Should.Throw<IOException>(() => CachedEntryInfo.Create(filePathWithSep));
    }

    [TestMethod]
    public void Create_FileShapedPath_PointingAtDirectory_ReturnsCachedDirectoryInfo()
    {
        var testDir = SetupTestDirectory();
        var dirPath = testDir.CombineDirectory(SubDirName);

        // Strip trailing separator to make the input file-shaped.
        string fileShaped = dirPath.PathDisplay.TrimEnd(System.IO.Path.DirectorySeparatorChar);

        var info = CachedEntryInfo.Create(fileShaped);

        info.ShouldBeOfType<CachedDirectoryInfo>();
        info.Path.PathDisplay.ShouldBe(dirPath.PathDisplay);
    }

    [TestMethod]
    public void Create_TrailingNavSegment_RequiresDirectory()
    {
        var testDir = SetupTestDirectory();

        // Trailing '.' makes the path directory-shaped; pointing at the test dir itself should succeed.
        string dotPath = testDir.PathDisplay + ".";
        var info = CachedEntryInfo.Create(dotPath);
        info.ShouldBeOfType<CachedDirectoryInfo>();

        // Pointing at a file via a '..' navigational tail is also directory-shaped and must error if it resolves to a file.
        // file.txt/.. resolves to testDir which is a directory, so it should succeed.
        string navToDir = testDir.CombineFile(FileName).PathDisplay + System.IO.Path.DirectorySeparatorChar + "..";
        var navInfo = CachedEntryInfo.Create(navToDir);
        navInfo.ShouldBeOfType<CachedDirectoryInfo>();
    }

    // ---- CachedFileInfo.Create / CachedDirectoryInfo.Create shadows ----

    [TestMethod]
    public void CachedFileInfo_Create_ExistingFile_ReturnsCachedFileInfo()
    {
        var testDir = SetupTestDirectory();
        var file = CachedFileInfo.Create(testDir.CombineFile(FileName).PathDisplay);

        file.ShouldBeOfType<CachedFileInfo>();
        file.Length.ShouldBe(1);
    }

    [TestMethod]
    public void CachedFileInfo_Create_PathPointsAtDirectory_ThrowsIOException()
    {
        var testDir = SetupTestDirectory();
        var dirPath = testDir.CombineDirectory(SubDirName);

        // File-shaped (no trailing separator) but resolves to a directory.
        string fileShaped = dirPath.PathDisplay.TrimEnd(System.IO.Path.DirectorySeparatorChar);

        Should.Throw<IOException>(() => CachedFileInfo.Create(fileShaped));
    }

    [TestMethod]
    public void CachedDirectoryInfo_Create_ExistingDirectory_BothShapes_ReturnsCachedDirectoryInfo()
    {
        var testDir = SetupTestDirectory();
        var dirPath = testDir.CombineDirectory(SubDirName);

        var dirShaped = CachedDirectoryInfo.Create(dirPath.PathDisplay);
        dirShaped.ShouldBeOfType<CachedDirectoryInfo>();

        string fileShaped = dirPath.PathDisplay.TrimEnd(System.IO.Path.DirectorySeparatorChar);
        var byFileShape = CachedDirectoryInfo.Create(fileShaped);
        byFileShape.ShouldBeOfType<CachedDirectoryInfo>();
        byFileShape.Path.PathDisplay.ShouldBe(dirPath.PathDisplay);
    }

    [TestMethod]
    public void CachedDirectoryInfo_Create_PathPointsAtFile_ThrowsIOException()
    {
        var testDir = SetupTestDirectory();
        var filePath = testDir.CombineFile(FileName);

        // File-shaped, resolves to a file => IOException (expected a directory).
        Should.Throw<IOException>(() => CachedDirectoryInfo.Create(filePath.PathDisplay));
    }
}
