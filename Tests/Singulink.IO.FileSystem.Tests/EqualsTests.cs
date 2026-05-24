namespace Singulink.IO.FileSystem.Tests;

[PrefixTestClass]
public class EqualsTests
{
    [TestMethod]
    public void EqualityOperator_WindowsDriveLetterCaseInsensitive_ReturnsTrue()
    {
        // The drive letter is normalized to lowercase during parsing, but the rest of the path is compared case-sensitively.
        var x = FilePath.Parse(@"c:\somepath", PathFormat.Windows);
        var y = FilePath.Parse(@"C:\somepath", PathFormat.Windows);

        (x == y).ShouldBeTrue();
        (x != y).ShouldBeFalse();
    }

    [TestMethod]
    public void EqualityOperator_DifferentFiles_ReturnsFalse()
    {
        var x = FilePath.Parse(@"c:\somepath", PathFormat.Windows);
        var y = FilePath.Parse(@"C:\someotherpath", PathFormat.Windows);

        (x == y).ShouldBeFalse();
        (x != y).ShouldBeTrue();
    }

    [TestMethod]
    public void Equals_MatchingFilesWithDriveLetterCaseDifference_ReturnsTrue()
    {
        var x = FilePath.Parse(@"c:\somepath", PathFormat.Windows);
        var y = FilePath.Parse(@"C:\somepath", PathFormat.Windows);

        x.Equals(y).ShouldBeTrue();
        x.ShouldBe(y);
    }

    [TestMethod]
    public void Equals_DifferentFiles_ReturnsFalse()
    {
        var x = FilePath.Parse(@"c:\somepath", PathFormat.Windows);
        var y = FilePath.Parse(@"C:\someotherpath", PathFormat.Windows);

        x.Equals(y).ShouldBeFalse();
        x.ShouldNotBe(y);
    }

    [TestMethod]
    public void Equals_FileAndDirectoryWithSamePath_ReturnsFalse()
    {
        var x = FilePath.Parse(@"c:\somepath", PathFormat.Windows);
        var y = DirectoryPath.Parse(@"c:\somepath", PathFormat.Windows);

        x.Equals(y).ShouldBeFalse();
        ((IPath)x).ShouldNotBe(y);
    }

    [TestMethod]
    public void Equals_WindowsDirsDifferingNameCase_ReturnsFalse()
    {
        // Path equality is case-sensitive on entry names (only the drive letter is normalized to lowercase).
        // Two paths that refer to the same Windows file system entry but differ in case are NOT considered equal here.
        var x = DirectoryPath.ParseAbsolute(@"C:\Some\Path", PathFormat.Windows);
        var y = DirectoryPath.ParseAbsolute(@"c:\some\path", PathFormat.Windows);

        x.Equals(y).ShouldBeFalse();
        x.ShouldNotBe(y);
    }

    [TestMethod]
    public void Equals_UnixDirsMatchingCase_ReturnsTrue()
    {
        var x = DirectoryPath.ParseAbsolute("/some/path", PathFormat.Unix);
        var y = DirectoryPath.ParseAbsolute("/some/path", PathFormat.Unix);

        x.Equals(y).ShouldBeTrue();
        x.ShouldBe(y);
    }

    [TestMethod]
    public void Equals_UnixDirsDifferingCase_ReturnsFalse()
    {
        var x = DirectoryPath.ParseAbsolute("/some/path", PathFormat.Unix);
        var y = DirectoryPath.ParseAbsolute("/Some/Path", PathFormat.Unix);

        x.Equals(y).ShouldBeFalse();
        x.ShouldNotBe(y);
    }

    [TestMethod]
    public void Equals_DifferentPathFormats_ReturnsFalse()
    {
        // Equality is per-format; the same textual path under different formats is not equal because PathExport differs.
        var x = FilePath.Parse("/dir/file.txt", PathFormat.Unix);
        var y = FilePath.Parse("dir/file.txt", PathFormat.Universal);

        x.Equals(y).ShouldBeFalse();
    }

    [TestMethod]
    public void Equals_RelativeFilesMatching_ReturnsTrue()
    {
        var x = FilePath.ParseRelative("dir/file.txt", PathFormat.Unix);
        var y = FilePath.ParseRelative("dir/file.txt", PathFormat.Unix);

        x.Equals(y).ShouldBeTrue();
        (x == y).ShouldBeTrue();
        x.ShouldBe(y);
    }

    [TestMethod]
    public void Equals_RelativeDirsMatching_ReturnsTrue()
    {
        var x = DirectoryPath.ParseRelative("dir/sub", PathFormat.Unix);
        var y = DirectoryPath.ParseRelative("dir/sub", PathFormat.Unix);

        x.Equals(y).ShouldBeTrue();
        x.ShouldBe(y);
    }

    [TestMethod]
    public void Equals_EmptyRelativeDirs_ReturnsTrue()
    {
        var x = DirectoryPath.ParseRelative("", PathFormat.Universal);
        var y = DirectoryPath.ParseRelative(".", PathFormat.Universal);

        x.Equals(y).ShouldBeTrue();
        x.ShouldBe(y);
    }

    [TestMethod]
    public void GetHashCode_EqualPaths_ReturnsSameHash()
    {
        var x = FilePath.Parse(@"c:\some\path\file.txt", PathFormat.Windows);
        var y = FilePath.Parse(@"C:\some\path\file.txt", PathFormat.Windows);

        x.Equals(y).ShouldBeTrue();
        x.GetHashCode().ShouldBe(y.GetHashCode());
    }

    [TestMethod]
    public void GetHashCode_FileAndDirSamePath_ReturnsDifferentHash()
    {
        // Not strictly required by the contract, but a useful sanity check that the file-vs-dir distinction is incorporated.
        var x = FilePath.Parse(@"c:\somepath", PathFormat.Windows);
        var y = DirectoryPath.Parse(@"c:\somepath", PathFormat.Windows);

        x.GetHashCode().ShouldNotBe(y.GetHashCode());
    }

    [TestMethod]
    public void HashSet_TreatsEqualPathsAsSame()
    {
        var set = new HashSet<IAbsoluteFilePath>
        {
            FilePath.ParseAbsolute(@"C:\foo\bar.txt", PathFormat.Windows),
        };

        set.Contains(FilePath.ParseAbsolute(@"c:\foo\bar.txt", PathFormat.Windows)).ShouldBeTrue();
        set.Add(FilePath.ParseAbsolute(@"c:\foo\bar.txt", PathFormat.Windows)).ShouldBeFalse();
        set.Count.ShouldBe(1);
    }

    [TestMethod]
    public void Dictionary_LooksUpByEquivalentPath()
    {
        var dict = new Dictionary<IAbsoluteDirectoryPath, int>
        {
            [DirectoryPath.ParseAbsolute(@"C:\dir", PathFormat.Windows)] = 42,
        };

        dict[DirectoryPath.ParseAbsolute(@"c:\dir", PathFormat.Windows)].ShouldBe(42);
    }
}
