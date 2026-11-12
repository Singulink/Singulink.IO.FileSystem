namespace Singulink.IO.FileSystem.Tests;

/// <summary>
/// Tests what can be observed of a flush without cutting the power: that it succeeds and leaves the entries alone, what it shares the file with, and how it
/// fails. They run against the Windows implementation on Windows and the Unix one elsewhere.
/// </summary>
[PrefixTestClass]
public class FlushToDiskTests
{
    private const string TestDirName = "_test_flushtodisk";

    private static readonly byte[] Content = [1, 2, 3, 4, 5];

    private static IAbsoluteFilePath CreateFile(IAbsoluteDirectoryPath dir, string name)
    {
        var file = dir.CombineFile(name);

        using (var stream = file.OpenStream(FileMode.CreateNew, FileAccess.Write))
            stream.Write(Content);

        return file;
    }

    private static byte[] ReadFile(IAbsoluteFilePath file)
    {
        using var stream = file.OpenStream(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        byte[] content = new byte[stream.Length];
        stream.ReadExactly(content);

        return content;
    }

    // ---- Files ----

    [TestMethod]
    public void File_Existing_FlushesAndKeepsContent()
    {
        var file = CreateFile(CreateEmptyTestDirectory(TestDirName), "test.file");

        file.FlushToDisk();
        file.FlushToDisk();

        ReadFile(file).ShouldBe(Content);
    }

    [TestMethod]
    public void File_Empty_Flushes()
    {
        var file = CreateEmptyTestDirectory(TestDirName).CombineFile("empty.file");
        file.OpenStream(FileMode.CreateNew).Dispose();

        file.FlushToDisk();

        file.Length.ShouldBe(0);
    }

    [TestMethod]
    public void File_OpenWithWriteSharing_Flushes()
    {
        var file = CreateEmptyTestDirectory(TestDirName).CombineFile("test.file");

        using var stream = file.OpenStream(FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite);
        stream.Write(Content);
        stream.Flush();

        file.FlushToDisk();

        ReadFile(file).ShouldBe(Content);
    }

    [TestMethod]
    public void File_OpenWithoutReadSharing_Flushes()
    {
        var file = CreateEmptyTestDirectory(TestDirName).CombineFile("test.file");

        // The flush asks for write access only, so a handle that shares writing but not reading does not block it.
        using var stream = file.OpenStream(FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Write);
        stream.Write(Content);
        stream.Flush();

        file.FlushToDisk();
    }

    [TestMethod]
    public void File_OpenWithoutWriteSharing_Throws()
    {
        var file = CreateEmptyTestDirectory(TestDirName).CombineFile("test.file");

        using var stream = file.OpenStream(FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read);

        // Sharing is advisory on Unix, where .NET only keeps a handle that shares nothing from being opened again.
        if (OperatingSystem.IsWindows())
            Should.Throw<IOException>(() => file.FlushToDisk()).ShouldBeOfType<IOException>();
        else
            file.FlushToDisk();
    }

    [TestMethod]
    public void File_OpenWithoutSharing_Throws()
    {
        var file = CreateEmptyTestDirectory(TestDirName).CombineFile("test.file");

        using var stream = file.OpenStream(FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);

        Should.Throw<IOException>(() => file.FlushToDisk()).ShouldBeOfType<IOException>();
    }

    [TestMethod]
    public void File_Missing_ThrowsFileNotFound()
    {
        var file = CreateEmptyTestDirectory(TestDirName).CombineFile("missing.file");

        Should.Throw<FileNotFoundException>(() => file.FlushToDisk());
        file.Exists.ShouldBeFalse();
    }

    [TestMethod]
    public void File_MissingParent_ThrowsDirectoryNotFound()
    {
        var file = CreateEmptyTestDirectory(TestDirName).CombineDirectory("missing_dir").CombineFile("missing.file");

        Should.Throw<DirectoryNotFoundException>(() => file.FlushToDisk());
    }

    [TestMethod]
    public void File_PathPointsAtDirectory_ThrowsIOException()
    {
        var dir = CreateEmptyTestDirectory(TestDirName).CombineDirectory("existing_dir");
        dir.Create();

        // Construct a file path that actually points at an existing directory by trimming the trailing separator.
        var file = FilePath.ParseAbsolute(dir.PathExport.AsSpan().TrimEnd(['\\', '/']));

        Should.Throw<IOException>(() => file.FlushToDisk())
            .ShouldBeOfType<IOException>()
            .Message.ShouldBe($"The path '{file.PathDisplay}' points to a directory, not a file.");
    }

    [TestMethod]
    public void File_IgnoreNotFound_Existing_Flushes()
    {
        var file = CreateFile(CreateEmptyTestDirectory(TestDirName), "test.file");

        file.FlushToDisk(ignoreNotFound: true);

        ReadFile(file).ShouldBe(Content);
    }

    [TestMethod]
    public void File_IgnoreNotFound_Missing_ReturnsNormally()
    {
        var file = CreateEmptyTestDirectory(TestDirName).CombineFile("missing.file");

        file.FlushToDisk(ignoreNotFound: true);

        file.Exists.ShouldBeFalse();
    }

    [TestMethod]
    public void File_IgnoreNotFound_MissingParent_ThrowsDirectoryNotFound()
    {
        var file = CreateEmptyTestDirectory(TestDirName).CombineDirectory("missing_dir").CombineFile("missing.file");

        Should.Throw<DirectoryNotFoundException>(() => file.FlushToDisk(ignoreNotFound: true));
    }

    [TestMethod]
    public void File_IgnoreNotFound_PathPointsAtDirectory_ThrowsIOException()
    {
        var dir = CreateEmptyTestDirectory(TestDirName).CombineDirectory("existing_dir");
        dir.Create();

        var file = FilePath.ParseAbsolute(dir.PathExport.AsSpan().TrimEnd(['\\', '/']));

        Should.Throw<IOException>(() => file.FlushToDisk(ignoreNotFound: true))
            .ShouldBeOfType<IOException>()
            .Message.ShouldBe($"The path '{file.PathDisplay}' points to a directory, not a file.");
    }

    [TestMethod]
    public void File_IgnoreNotFound_IgnoresWhatDeleteIgnores()
    {
        var testDir = CreateEmptyTestDirectory(TestDirName);
        CreateFile(testDir, "existing.file");
        testDir.CombineDirectory("existing_dir").Create();

        IAbsoluteFilePath[] files = [
            testDir.CombineFile("missing.file"),
            testDir.CombineDirectory("missing_dir").CombineFile("missing.file"),
            FilePath.ParseAbsolute(testDir.CombineDirectory("existing_dir").PathExport.AsSpan().TrimEnd(['\\', '/'])),
            testDir.CombineDirectory("existing.file").CombineFile("nested.file"),
        ];

        foreach (var file in files)
        {
            var deleteException = GetException(() => file.Delete(ignoreNotFound: true));
            var flushException = GetException(() => file.FlushToDisk(ignoreNotFound: true));

            (flushException?.GetType()).ShouldBe(deleteException?.GetType(), file.PathDisplay);
        }
    }

    [TestMethod]
    public void File_ReadOnly_ThrowsUnauthorizedIOAccess()
    {
        // A privileged process on Unix can open any file for writing.
        if (!OperatingSystem.IsWindows() && Environment.IsPrivilegedProcess)
            return;

        var file = CreateFile(CreateEmptyTestDirectory(TestDirName), "readonly.file");
        file.IsReadOnly = true;

        try
        {
            Should.Throw<UnauthorizedIOAccessException>(() => file.FlushToDisk());
        }
        finally
        {
            // A read-only file would keep the test directory from being deleted on Windows.
            file.IsReadOnly = false;
        }
    }

    // ---- Directories ----

    [TestMethod]
    public void Directory_Empty_Flushes()
    {
        var dir = CreateEmptyTestDirectory(TestDirName);

        dir.FlushToDisk();
        dir.FlushToDisk();

        dir.IsEmpty.ShouldBeTrue();
    }

    [TestMethod]
    public void Directory_ChangedEntries_FlushesAndKeepsEntries()
    {
        var dir = CreateEmptyTestDirectory(TestDirName);
        var subDir = dir.CombineDirectory("subdir");

        // Created in.
        var file = CreateFile(dir, "created.file");
        file.FlushToDisk();
        dir.FlushToDisk();

        // Renamed into.
        subDir.Create();
        dir.FlushToDisk();

        var movedFile = subDir.CombineFile("moved.file");
        file.MoveTo(movedFile);
        subDir.FlushToDisk();
        dir.FlushToDisk();

        dir.GetChildEntries().ShouldBe([subDir]);
        subDir.GetChildEntries().ShouldBe([movedFile]);
        ReadFile(movedFile).ShouldBe(Content);

        // Removed from.
        movedFile.Delete();
        subDir.FlushToDisk();
        subDir.Delete();
        dir.FlushToDisk();

        dir.IsEmpty.ShouldBeTrue();
    }

    [TestMethod]
    public void Directory_WithOpenFile_Flushes()
    {
        var dir = CreateEmptyTestDirectory(TestDirName);

        using var stream = dir.CombineFile("test.file").OpenStream(FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);

        dir.FlushToDisk();
    }

    [TestMethod]
    public void Directory_NonAsciiName_Flushes()
    {
        var dir = CreateEmptyTestDirectory(TestDirName).CombineDirectory("donn\u00E9es \u65E5\u672C\u8A9E");
        dir.Create();

        var file = CreateFile(dir, "\u0444\u0430\u0439\u043B.file");

        file.FlushToDisk();
        dir.FlushToDisk();

        ReadFile(file).ShouldBe(Content);
    }

    [TestMethod]
    public void Directory_LongPath_Flushes()
    {
        var testDir = CreateEmptyTestDirectory(TestDirName);
        var dir = testDir;

        // Longer than the Windows MAX_PATH (260 characters) and than the buffer that the path is converted to UTF-8 in on Unix (256 bytes).
        for (int i = 0; i < 6; i++)
            dir = dir.CombineDirectory($"{i}_{new string('d', 50)}_\u00E9");

        try
        {
            dir.Create();
            var file = CreateFile(dir, "test.file");

            dir.PathDisplay.Length.ShouldBeGreaterThan(300);

            file.FlushToDisk();
            dir.FlushToDisk();

            ReadFile(file).ShouldBe(Content);
        }
        finally
        {
            // Don't leave paths behind that tools without long path support fail on.
            testDir.Delete(recursive: true);
        }
    }

    [TestMethod]
    public void Directory_FileSystemRefusesFlush_ReturnsNormally()
    {
        // procfs has no fsync for its directories, which Linux reports as EINVAL. Other platforms have no file system here that is known to refuse.
        if (!OperatingSystem.IsLinux())
            return;

        var dir = DirectoryPath.ParseAbsolute("/proc/");

        dir.FlushToDisk();
    }

    [TestMethod]
    public void Directory_Missing_ThrowsDirectoryNotFound()
    {
        var dir = CreateEmptyTestDirectory(TestDirName).CombineDirectory("missing_dir");

        Should.Throw<DirectoryNotFoundException>(() => dir.FlushToDisk())
            .Message.ShouldBe($"Could not find directory '{dir.PathDisplay}'.");

        dir.Exists.ShouldBeFalse();
    }

    [TestMethod]
    public void Directory_MissingParent_ThrowsDirectoryNotFound()
    {
        var dir = CreateEmptyTestDirectory(TestDirName).CombineDirectory("missing_dir").CombineDirectory("nested");

        Should.Throw<DirectoryNotFoundException>(() => dir.FlushToDisk())
            .Message.ShouldBe($"Could not find directory '{dir.PathDisplay}'.");
    }

    [TestMethod]
    public void Directory_PathPointsAtFile_ThrowsIOException()
    {
        var testDir = CreateEmptyTestDirectory(TestDirName);
        CreateFile(testDir, "existing.file");

        var dir = testDir.CombineDirectory("existing.file");

        Should.Throw<IOException>(() => dir.FlushToDisk())
            .ShouldBeOfType<IOException>()
            .Message.ShouldBe($"The path '{dir.PathDisplay}' points to a file, not a directory.");
    }

    [TestMethod]
    public void Directory_ParentPointsAtFile_ThrowsDirectoryNotFound()
    {
        var testDir = CreateEmptyTestDirectory(TestDirName);
        CreateFile(testDir, "existing.file");

        var dir = testDir.CombineDirectory("existing.file").CombineDirectory("nested");

        Should.Throw<DirectoryNotFoundException>(() => dir.FlushToDisk());
    }

    [TestMethod]
    public void Directory_IgnoreNotFound_Existing_Flushes()
    {
        var dir = CreateEmptyTestDirectory(TestDirName);
        var file = CreateFile(dir, "test.file");

        dir.FlushToDisk(ignoreNotFound: true);

        dir.GetChildEntries().ShouldBe([file]);
    }

    [TestMethod]
    public void Directory_IgnoreNotFound_Missing_ReturnsNormally()
    {
        var dir = CreateEmptyTestDirectory(TestDirName).CombineDirectory("missing_dir");

        dir.FlushToDisk(ignoreNotFound: true);

        dir.Exists.ShouldBeFalse();
    }

    [TestMethod]
    public void Directory_IgnoreNotFound_MissingParent_ThrowsDirectoryNotFoundOnUnix()
    {
        var dir = CreateEmptyTestDirectory(TestDirName).CombineDirectory("missing_dir").CombineDirectory("nested");

        // Nothing is checked on Windows.
        if (OperatingSystem.IsWindows())
            dir.FlushToDisk(ignoreNotFound: true);
        else
            Should.Throw<DirectoryNotFoundException>(() => dir.FlushToDisk(ignoreNotFound: true));
    }

    [TestMethod]
    public void Directory_IgnoreNotFound_PathPointsAtFile_ThrowsIOExceptionOnUnix()
    {
        var testDir = CreateEmptyTestDirectory(TestDirName);
        CreateFile(testDir, "existing.file");

        var dir = testDir.CombineDirectory("existing.file");

        // Nothing is checked on Windows.
        if (OperatingSystem.IsWindows())
        {
            dir.FlushToDisk(ignoreNotFound: true);
        }
        else
        {
            Should.Throw<IOException>(() => dir.FlushToDisk(ignoreNotFound: true))
                .ShouldBeOfType<IOException>()
                .Message.ShouldBe($"The path '{dir.PathDisplay}' points to a file, not a directory.");
        }
    }

    [TestMethod]
    public void Directory_IgnoreNotFound_IgnoresWhatDeleteIgnoresOnUnix()
    {
        var testDir = CreateEmptyTestDirectory(TestDirName);
        CreateFile(testDir, "existing.file");

        IAbsoluteDirectoryPath[] dirs = [
            testDir.CombineDirectory("missing_dir"),
            testDir.CombineDirectory("missing_dir").CombineDirectory("nested"),
            testDir.CombineDirectory("existing.file"),
            testDir.CombineDirectory("existing.file").CombineDirectory("nested"),
        ];

        foreach (var dir in dirs)
        {
            var deleteException = GetException(() => dir.Delete(ignoreNotFound: true));
            var flushException = GetException(() => dir.FlushToDisk(ignoreNotFound: true));

            // Nothing is checked on Windows, so nothing is thrown there.
            if (OperatingSystem.IsWindows())
                flushException.ShouldBeNull(dir.PathDisplay);
            else
                (flushException?.GetType()).ShouldBe(deleteException?.GetType(), dir.PathDisplay);
        }
    }

    // ---- Path format ----

    [TestMethod]
    public void OtherPlatformFormat_ThrowsInvalidOperation()
    {
        var format = OperatingSystem.IsWindows() ? PathFormat.Unix : PathFormat.Windows;
        string dir = OperatingSystem.IsWindows() ? "/data/" : @"C:\data\";

        Should.Throw<InvalidOperationException>(() => FilePath.ParseAbsolute(dir + "test.file", format).FlushToDisk());
        Should.Throw<InvalidOperationException>(() => FilePath.ParseAbsolute(dir + "test.file", format).FlushToDisk(ignoreNotFound: true));
        Should.Throw<InvalidOperationException>(() => DirectoryPath.ParseAbsolute(dir, format).FlushToDisk());
        Should.Throw<InvalidOperationException>(() => DirectoryPath.ParseAbsolute(dir, format).FlushToDisk(ignoreNotFound: true));
    }

    private static Exception? GetException(Action action)
    {
        try
        {
            action();
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
}
