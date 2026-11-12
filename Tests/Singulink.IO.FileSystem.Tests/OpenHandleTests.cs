using Microsoft.Win32.SafeHandles;

namespace Singulink.IO.FileSystem.Tests;

[PrefixTestClass]
public class OpenHandleTests
{
    private const string TestDirName = "_test_openhandle";

    private static readonly byte[] Content = [1, 2, 3, 4, 5];

    private static IAbsoluteFilePath CreateFile(IAbsoluteDirectoryPath dir, string name)
    {
        var file = dir.CombineFile(name);

        using (var stream = file.OpenStream(FileMode.CreateNew, FileAccess.Write))
            stream.Write(Content);

        return file;
    }

    private static byte[] ReadAll(SafeFileHandle handle)
    {
        byte[] content = new byte[RandomAccess.GetLength(handle)];
        int read = 0;

        while (read < content.Length)
        {
            int count = RandomAccess.Read(handle, content.AsSpan(read), read);
            count.ShouldBeGreaterThan(0);
            read += count;
        }

        return content;
    }

    [TestMethod]
    public void CreateNew_WritesAndReads()
    {
        var file = CreateEmptyTestDirectory(TestDirName).CombineFile("test.file");

        using (var handle = file.OpenHandle(FileMode.CreateNew))
        {
            handle.IsInvalid.ShouldBeFalse();

            RandomAccess.Write(handle, Content, 0);
            RandomAccess.Write(handle, Content, Content.Length);

            ReadAll(handle).ShouldBe([.. Content, .. Content]);
        }

        file.Length.ShouldBe(Content.Length * 2);
    }

    [TestMethod]
    public void Defaults_OpenExistingForReadingAndWritingWithoutSharing()
    {
        var file = CreateFile(CreateEmptyTestDirectory(TestDirName), "test.file");

        using (var handle = file.OpenHandle())
        {
            ReadAll(handle).ShouldBe(Content);
            RandomAccess.Write(handle, [9], 0);

            Should.Throw<IOException>(() => file.OpenHandle(FileMode.Open, FileAccess.Read, FileShare.ReadWrite)).ShouldBeOfType<IOException>();
            Should.Throw<IOException>(() => file.OpenStream(FileMode.Open, FileAccess.Read, FileShare.ReadWrite)).ShouldBeOfType<IOException>();
        }

        using (var handle = file.OpenHandle())
            ReadAll(handle).ShouldBe([9, 2, 3, 4, 5]);
    }

    [TestMethod]
    public void Defaults_MissingFile_ThrowsFileNotFound()
    {
        var file = CreateEmptyTestDirectory(TestDirName).CombineFile("missing.file");

        Should.Throw<FileNotFoundException>(() => file.OpenHandle());
        file.Exists.ShouldBeFalse();
    }

    [TestMethod]
    public void MissingParent_ThrowsDirectoryNotFound()
    {
        var file = CreateEmptyTestDirectory(TestDirName).CombineDirectory("missing_dir").CombineFile("missing.file");

        Should.Throw<DirectoryNotFoundException>(() => file.OpenHandle());
        Should.Throw<DirectoryNotFoundException>(() => file.OpenHandle(FileMode.Create));
    }

    [TestMethod]
    public void PathPointsAtDirectory_ThrowsIOException()
    {
        var dir = CreateEmptyTestDirectory(TestDirName).CombineDirectory("existing_dir");
        dir.Create();

        // Construct a file path that actually points at an existing directory by trimming the trailing separator.
        var file = FilePath.ParseAbsolute(dir.PathExport.AsSpan().TrimEnd(['\\', '/']));

        Should.Throw<IOException>(() => file.OpenHandle())
            .ShouldBeOfType<IOException>()
            .Message.ShouldBe($"The path '{file.PathDisplay}' points to a directory, not a file.");

        Should.Throw<IOException>(() => file.OpenHandle(FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            .ShouldBeOfType<IOException>()
            .Message.ShouldBe($"The path '{file.PathDisplay}' points to a directory, not a file.");
    }

    [TestMethod]
    public void ReadOnlyFile_WriteAccess_ThrowsUnauthorizedIOAccess()
    {
        // A privileged process on Unix can open any file for writing.
        if (!OperatingSystem.IsWindows() && Environment.IsPrivilegedProcess)
            return;

        var file = CreateFile(CreateEmptyTestDirectory(TestDirName), "readonly.file");
        file.IsReadOnly = true;

        try
        {
            Should.Throw<UnauthorizedIOAccessException>(() => file.OpenHandle());

            using var handle = file.OpenHandle(FileMode.Open, FileAccess.Read);
            ReadAll(handle).ShouldBe(Content);
        }
        finally
        {
            // A read-only file would keep the test directory from being deleted on Windows.
            file.IsReadOnly = false;
        }
    }

    [TestMethod]
    public void Sharing_AllowsOtherHandles()
    {
        var file = CreateFile(CreateEmptyTestDirectory(TestDirName), "test.file");

        using var reader = file.OpenHandle(FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var writer = file.OpenHandle(FileMode.Open, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);

        RandomAccess.Write(writer, [9, 9], Content.Length);

        ReadAll(reader).ShouldBe([.. Content, 9, 9]);
    }

    [TestMethod]
    public void Options_ArePassedOn()
    {
        var file = CreateEmptyTestDirectory(TestDirName).CombineFile("test.file");

        using (var handle = file.OpenHandle(FileMode.CreateNew, options: FileOptions.DeleteOnClose))
        {
            RandomAccess.Write(handle, Content, 0);
            file.Exists.ShouldBeTrue();
        }

        file.Exists.ShouldBeFalse();
    }

    [TestMethod]
    public void PreallocationSize_DoesNotChangeLength()
    {
        var file = CreateEmptyTestDirectory(TestDirName).CombineFile("test.file");

        using (var handle = file.OpenHandle(FileMode.CreateNew, FileAccess.Write, preallocationSize: 1024 * 1024))
        {
            RandomAccess.GetLength(handle).ShouldBe(0);
            RandomAccess.Write(handle, Content, 0);
        }

        file.Length.ShouldBe(Content.Length);

        Should.Throw<ArgumentOutOfRangeException>(() => file.OpenHandle(FileMode.Create, FileAccess.Write, preallocationSize: -1));
    }

    [TestMethod]
    public async Task OpenAsyncHandle_WritesAndReads()
    {
        var file = CreateEmptyTestDirectory(TestDirName).CombineFile("test.file");

        using (var handle = file.OpenAsyncHandle(FileMode.CreateNew, preallocationSize: 4096))
        {
            // Unix has no asynchronous handles, so the option makes no difference to the handle there.
            if (OperatingSystem.IsWindows())
                handle.IsAsync.ShouldBeTrue();

            await RandomAccess.WriteAsync(handle, Content, 0);

            byte[] read = new byte[Content.Length];
            (await RandomAccess.ReadAsync(handle, read, 0)).ShouldBe(Content.Length);
            read.ShouldBe(Content);
        }

        using (var handle = file.OpenHandle())
        {
            if (OperatingSystem.IsWindows())
                handle.IsAsync.ShouldBeFalse();

            ReadAll(handle).ShouldBe(Content);
        }

        Should.Throw<FileNotFoundException>(() => file.ParentDirectory.CombineFile("missing.file").OpenAsyncHandle());
    }

    [TestMethod]
    public void OtherPlatformFormat_ThrowsInvalidOperation()
    {
        var format = OperatingSystem.IsWindows() ? PathFormat.Unix : PathFormat.Windows;
        string dir = OperatingSystem.IsWindows() ? "/data/" : @"C:\data\";
        var file = FilePath.ParseAbsolute(dir + "test.file", format);

        Should.Throw<InvalidOperationException>(() => file.OpenHandle());
        Should.Throw<InvalidOperationException>(() => file.OpenAsyncHandle());
    }
}
