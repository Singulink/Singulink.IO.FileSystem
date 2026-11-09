namespace Singulink.IO.FileSystem.Tests;

[PrefixTestClass]
public class RelativePathToPathFormatTests
{
    private const string WindowsInvalidChars = "Invalid characters include: < > : \" | ? * / \\";

    [TestMethod]
    public void File_ConvertsBetweenFormats()
    {
        var windows = FilePath.ParseRelative(@"..\data\file.txt", PathFormat.Windows);

        var universal = windows.ToPathFormat(PathFormat.Universal);
        universal.ShouldBe(FilePath.ParseRelative("../data/file.txt", PathFormat.Universal));

        universal.ToPathFormat(PathFormat.Unix).ShouldBe(FilePath.ParseRelative("../data/file.txt", PathFormat.Unix));
        universal.ToPathFormat(PathFormat.Windows).ShouldBe(windows);
    }

    [TestMethod]
    public void Directory_ConvertsBetweenFormats()
    {
        var windows = DirectoryPath.ParseRelative(@"data\uploads\", PathFormat.Windows);

        windows.ToPathFormat(PathFormat.Unix).ShouldBe(DirectoryPath.ParseRelative("data/uploads/", PathFormat.Unix));
        windows.ToPathFormat(PathFormat.Universal).PathDisplay.ShouldBe("data/uploads/");
        DirectoryPath.ParseRelative("", PathFormat.Windows).ToPathFormat(PathFormat.Universal).PathDisplay.ShouldBe("");
    }

    [TestMethod]
    public void RootedPath_ToFormatWithoutRootedPaths_Throws()
    {
        var path = DirectoryPath.ParseRelative(@"\data\", PathFormat.Windows);

        var ex = Should.Throw<ArgumentException>(() => path.ToPathFormat(PathFormat.Unix));
        ex.Message.ShouldBe("Destination path format does not support relative rooted paths.");
        ex.ParamName.ShouldBeNull();

        Should.Throw<ArgumentException>(() => path.ToPathFormat(PathFormat.Universal))
            .Message.ShouldBe("Destination path format does not support relative rooted paths.");
    }

    [TestMethod]
    public void DestinationSeparatorInName_Throws()
    {
        var path = FilePath.ParseRelative(@"a\b.txt", PathFormat.Unix);

        var ex = Should.Throw<ArgumentException>(() => path.ToPathFormat(PathFormat.Windows));
        ex.Message.ShouldBe(@"Invalid separator character '\' in path entry.");
        ex.ParamName.ShouldBeNull();
    }

    [TestMethod]
    public void NameInvalidInDestinationFormat_Throws()
    {
        var path = FilePath.ParseRelative(@"a\b.txt", PathFormat.Unix);

        var ex = Should.Throw<ArgumentException>(() => path.ToPathFormat(PathFormat.Universal));
        ex.Message.ShouldBe(@"Invalid character '\' in entry name 'a\b.txt'.");
        ex.ParamName.ShouldBeNull();

        var directory = DirectoryPath.ParseRelative("data/a:b/", PathFormat.Unix);

        Should.Throw<ArgumentException>(() => directory.ToPathFormat(PathFormat.Windows))
            .Message.ShouldBe($"Invalid character ':' in entry name 'a:b'. {WindowsInvalidChars}");
    }

    [TestMethod]
    public void UnfriendlyName_ThrowsWithDefaultOptionsOnly()
    {
        var path = FilePath.ParseRelative(@"data \file.txt", PathFormat.Windows, PathOptions.None);

        Should.Throw<ArgumentException>(() => path.ToPathFormat(PathFormat.Universal))
            .Message.ShouldBe("Entry name ends with a space.");

        path.ToPathFormat(PathFormat.Universal, PathOptions.None).PathDisplay.ShouldBe("data /file.txt");
    }

    [TestMethod]
    [DataRow(@"\data\", "Windows", "Unix", "Destination path format does not support relative rooted paths.")]
    [DataRow(@"\", "Windows", "Universal", "Destination path format does not support relative rooted paths.")]
    [DataRow(@"a\b/", "Unix", "Windows", @"Invalid separator character '\' in path entry.")]
    [DataRow(@"a\b/", "Unix", "Universal", @"Invalid character '\' in entry name 'a\b'.")]
    [DataRow("data/a:b/", "Unix", "Windows", "Invalid character ':' in entry name 'a:b'. " + WindowsInvalidChars)]
    [DataRow(@"data \", "Windows", "Universal", "Entry name ends with a space.")]
    public void TryToPathFormat_Directory_Fails(string value, string from, string to, string error)
    {
        var path = (IRelativeDirectoryPath.Impl)DirectoryPath.ParseRelative(value, GetFormat(from), PathOptions.None);

        path.TryToPathFormat(GetFormat(to), PathOptions.NoUnfriendlyNames, out IRelativeDirectoryPath? result, out string? resultError).ShouldBeFalse();
        result.ShouldBeNull();
        resultError.ShouldBe(error);

        ((IRelativePath.Impl)path).TryToPathFormat(GetFormat(to), PathOptions.NoUnfriendlyNames, out IRelativePath? baseResult, out resultError)
            .ShouldBeFalse();
        baseResult.ShouldBeNull();
        resultError.ShouldBe(error);

        Should.Throw<ArgumentException>(() => path.ToPathFormat(GetFormat(to), PathOptions.NoUnfriendlyNames)).Message.ShouldBe(error);
    }

    [TestMethod]
    [DataRow(@"\data\file.txt", "Windows", "Unix", "Destination path format does not support relative rooted paths.")]
    [DataRow(@"a\b.txt", "Unix", "Windows", @"Invalid separator character '\' in path entry.")]
    [DataRow(@"a\b.txt", "Unix", "Universal", @"Invalid character '\' in entry name 'a\b.txt'.")]
    [DataRow("a*b.txt", "Unix", "Universal", "Invalid character '*' in entry name 'a*b.txt'. " + WindowsInvalidChars)]
    [DataRow("data/file.txt ", "Unix", "Windows", "Entry name ends with a space.")]
    public void TryToPathFormat_File_Fails(string value, string from, string to, string error)
    {
        var path = (IRelativeFilePath.Impl)FilePath.ParseRelative(value, GetFormat(from), PathOptions.None);

        path.TryToPathFormat(GetFormat(to), PathOptions.NoUnfriendlyNames, out IRelativeFilePath? result, out string? resultError).ShouldBeFalse();
        result.ShouldBeNull();
        resultError.ShouldBe(error);

        ((IRelativePath.Impl)path).TryToPathFormat(GetFormat(to), PathOptions.NoUnfriendlyNames, out IRelativePath? baseResult, out resultError)
            .ShouldBeFalse();
        baseResult.ShouldBeNull();
        resultError.ShouldBe(error);

        Should.Throw<ArgumentException>(() => path.ToPathFormat(GetFormat(to), PathOptions.NoUnfriendlyNames)).Message.ShouldBe(error);
    }

    [TestMethod]
    public void TryToPathFormat_Succeeds_LikeToPathFormat()
    {
        var file = (IRelativeFilePath.Impl)FilePath.ParseRelative(@"..\data\file.txt", PathFormat.Windows);
        var directory = (IRelativeDirectoryPath.Impl)DirectoryPath.ParseRelative(@"data \", PathFormat.Windows, PathOptions.None);

        file.TryToPathFormat(PathFormat.Unix, PathOptions.NoUnfriendlyNames, out IRelativeFilePath? fileResult, out string? error).ShouldBeTrue();
        error.ShouldBeNull();
        fileResult.ShouldBe(file.ToPathFormat(PathFormat.Unix, PathOptions.NoUnfriendlyNames));

        directory.TryToPathFormat(PathFormat.Universal, PathOptions.None, out IRelativeDirectoryPath? directoryResult, out error).ShouldBeTrue();
        error.ShouldBeNull();
        directoryResult.ShouldBe(directory.ToPathFormat(PathFormat.Universal, PathOptions.None));

        ((IRelativePath.Impl)file).TryToPathFormat(PathFormat.Universal, PathOptions.None, out IRelativePath? baseResult, out error).ShouldBeTrue();
        baseResult.ShouldBe(FilePath.ParseRelative("../data/file.txt", PathFormat.Universal));
    }
}
