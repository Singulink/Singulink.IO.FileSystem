namespace Singulink.IO.FileSystem.Tests;

[PrefixTestClass]
public class EnumeratingDirectoryTests
{
    private const int DirCount = 5;
    private const int SubDirCount = 6;
    private const int FileCount = 7;

    private const int TotalDirCount = DirCount + (DirCount * SubDirCount);
    private const int TotalFileCount = DirCount * SubDirCount * FileCount;

    private static readonly char Sep = Path.DirectorySeparatorChar;

    private static IAbsoluteDirectoryPath SetupTestDirectory()
    {
        var testDir = DirectoryPath.GetCurrent() + DirectoryPath.ParseRelative("_test");

        if (testDir.Exists)
            testDir.Delete(true);

        testDir.Create();

        for (int i = 0; i < DirCount; i++)
        {
            var dirLevel1 = testDir.CombineDirectory($"{i}_dir");

            for (int j = 0; j < SubDirCount; j++)
            {
                var dirLevel2 = dirLevel1.CombineDirectory($"{i}_{j}_subdir");
                dirLevel2.Create();

                for (int k = 0; k < FileCount; k++)
                    dirLevel2.CombineFile($"{i}_{j}_{k}_file.txt").OpenStream(FileMode.CreateNew).Dispose();
            }
        }

        return testDir;
    }

    /// <summary>
    /// Builds the expected set of "*_1_file.txt" relative paths under the test tree, joined with the given prefix and the platform separator.
    /// </summary>
    private static HashSet<string> BuildExpectedFile1Paths(string prefix)
    {
        var expected = new HashSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < DirCount; i++)
        {
            for (int j = 0; j < SubDirCount; j++)
                expected.Add($"{prefix}{i}_dir{Sep}{i}_{j}_subdir{Sep}{i}_{j}_1_file.txt");
        }

        return expected;
    }

    [TestMethod]
    public void GetTotalChildEntries()
    {
        var dir = SetupTestDirectory();
        var recursive = new SearchOptions { Recursive = true };

        int entryCount = dir.GetChildEntries(recursive).Count();
        entryCount.ShouldBe(TotalDirCount + TotalFileCount);

        int fileCount = dir.GetChildFiles(recursive).Count();
        fileCount.ShouldBe(TotalFileCount);

        int dirCount = dir.GetChildDirectories(recursive).Count();
        dirCount.ShouldBe(TotalDirCount);
    }

    [TestMethod]
    public void GetFilteredChildEntries()
    {
        var dir = SetupTestDirectory();
        var recursive = new SearchOptions { Recursive = true };
        var nonRecursive = new SearchOptions();

        var dirs = dir.GetChildDirectories("?_?_subdir", recursive).ToList();
        dirs.Count().ShouldBe(DirCount * SubDirCount);
        dirs[0].RootDirectory.ShouldBe(dir.RootDirectory); // Ensure RootLength is set correctly

        int fileCount = dir
            .GetChildDirectories("1_dir", nonRecursive).Single()
            .GetChildDirectories("1_1_subdir", nonRecursive).Single()
            .GetChildFiles("*file.txt", nonRecursive).Count();

        fileCount.ShouldBe(FileCount);
    }

    [TestMethod]
    public void GetRelativeChildEntries()
    {
        var dir = SetupTestDirectory();
        var recursive = new SearchOptions { Recursive = true };
        var nonRecursive = new SearchOptions();

        var file = dir
            .GetChildDirectories("1_dir", nonRecursive).Single()
            .GetChildDirectories("1_1_subdir", nonRecursive).Single()
            .GetRelativeChildFiles("*_1_file.txt", nonRecursive).Single();

        file.PathDisplay.ShouldBe("1_1_1_file.txt");

        var files = dir.GetRelativeChildFiles("*_1_file.txt", recursive).ToArray();
        var actualFilePaths = files.Select(f => f.PathDisplay).ToHashSet(StringComparer.Ordinal);
        actualFilePaths.ShouldBe(BuildExpectedFile1Paths(prefix: string.Empty), ignoreOrder: true);

        foreach (var f in files)
        {
            f.ParentDirectory!.ParentDirectory!.ParentDirectory!.PathDisplay.ShouldBe("");
            f.IsRooted.ShouldBeFalse();
        }

        // Relative child directories must end with the trailing separator (directory-path invariant).
        var subdirs = dir.GetRelativeChildDirectories("?_?_subdir", recursive).ToArray();
        subdirs.Length.ShouldBe(DirCount * SubDirCount);

        foreach (var d in subdirs)
        {
            d.PathDisplay.ShouldEndWith(Sep.ToString());
            d.IsRooted.ShouldBeFalse();
        }

        var expectedSubdirs = new HashSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < DirCount; i++)
        {
            for (int j = 0; j < SubDirCount; j++)
                expectedSubdirs.Add($"{i}_dir{Sep}{i}_{j}_subdir{Sep}");
        }

        subdirs.Select(d => d.PathDisplay).ToHashSet(StringComparer.Ordinal).ShouldBe(expectedSubdirs, ignoreOrder: true);
    }

    [TestMethod]
    public void GetRelativeEntriesFromParentSearchLocation()
    {
        var dir = SetupTestDirectory();
        var recursive = new SearchOptions { Recursive = true };

        var parentDir = DirectoryPath.ParseRelative("..");

        var files = dir.GetRelativeEntries(parentDir, "*_1_file.txt", recursive).ToArray();
        files.Length.ShouldBe(DirCount * SubDirCount);

        // The matchDir loop should collapse the ".." navigation against the "_test" entry in each child path,
        // producing the same relative paths as if we had searched the test directory itself.
        // Previously a slicing off-by-one bug here silently chopped the first character of each child name,
        // which still produced parseable paths and passed the count assertion below.
        var actualPaths = files.Select(f => f.PathDisplay).ToHashSet(StringComparer.Ordinal);
        actualPaths.ShouldBe(BuildExpectedFile1Paths(prefix: string.Empty), ignoreOrder: true);

        foreach (var f in files)
        {
            f.ParentDirectory!.ParentDirectory!.ParentDirectory!.PathDisplay.ShouldBe("");
            f.IsRooted.ShouldBeFalse();
        }
    }

    [TestMethod]
    public void GetRelativeEntriesFromSubdirectorySearchLocation()
    {
        var dir = SetupTestDirectory();
        var recursive = new SearchOptions { Recursive = true };

        var dirLevel1 = DirectoryPath.ParseRelative("1_dir");

        var files = dir.GetRelativeEntries(dirLevel1, "*_1_file.txt", recursive).ToArray();
        files.Length.ShouldBe(SubDirCount);

        // Each file path is rooted at the searchLocation "1_dir/" prefix.
        var expected = new HashSet<string>(StringComparer.Ordinal);

        for (int j = 0; j < SubDirCount; j++)
            expected.Add($"1_dir{Sep}1_{j}_subdir{Sep}1_{j}_1_file.txt");

        files.Select(f => f.PathDisplay).ToHashSet(StringComparer.Ordinal).ShouldBe(expected, ignoreOrder: true);

        foreach (var f in files)
        {
            f.ParentDirectory!.ParentDirectory!.ParentDirectory!.PathDisplay.ShouldBe("");
            f.IsRooted.ShouldBeFalse();
        }
    }

    [TestMethod]
    [DataRow(".")]
    [DataRow("../_test")]
    public void GetRelativeEntriesFromCurrent(string currentDirPath)
    {
        var dir = SetupTestDirectory();
        var recursive = new SearchOptions { Recursive = true };

        var currentDir = DirectoryPath.ParseRelative(currentDirPath);
        var files = dir.GetRelativeEntries(currentDir, "*_1_file.txt", recursive).ToArray();
        files.Length.ShouldBe(DirCount * SubDirCount);

        // Files should be prefixed with the searchLocation's PathDisplay (which always ends with the separator under the directory-path invariant).
        var actualPaths = files.Select(f => f.PathDisplay).ToHashSet(StringComparer.Ordinal);
        actualPaths.ShouldBe(BuildExpectedFile1Paths(prefix: currentDir.PathDisplay), ignoreOrder: true);

        foreach (var f in files)
        {
            f.ParentDirectory!.ParentDirectory!.ParentDirectory!.PathDisplay.ShouldBe(currentDir.PathDisplay);
            f.IsRooted.ShouldBeFalse();
        }
    }

    /// <summary>
    /// Verifies that a rooted-relative searchLocation that resolves to one of `this`'s ancestors produces results expressed
    /// as relative-nav from `this` (just like the "/" case), rather than leaking the rooted form. The matchDirs loop should
    /// also collapse the "../" prefix against the matching ancestor chain.
    /// </summary>
    [TestMethod]
    public void GetRelativeEntriesFromRootedSearchLocation_AncestorOfThis()
    {
        var testDir = SetupTestDirectory();
        var dir = testDir.CombineDirectory("0_dir").CombineDirectory("0_0_subdir"); // `this` is 2 levels below testDir
        var recursive = new SearchOptions { Recursive = true };

        // Construct a rooted-relative path that resolves to testDir/0_dir/ (one of `this`'s ancestors).
        string rootedPath = Sep + testDir.PathDisplay[testDir.RootDirectory.PathDisplay.Length..] + "0_dir" + Sep;
        var searchLocation = DirectoryPath.ParseRelative(rootedPath);
        searchLocation.IsRooted.ShouldBeTrue();

        var files = dir.GetRelativeEntries(searchLocation, "0_0_*_file.txt", recursive).ToArray();
        files.Length.ShouldBe(FileCount);

        // Results should be relative-nav from `this`. Since searchLocation is `this`'s grandparent's child (= `this`'s parent),
        // and the files live under `this` itself, the LCA collapse should yield just the file name.
        var expected = new HashSet<string>(StringComparer.Ordinal);
        for (int k = 0; k < FileCount; k++)
            expected.Add($"0_0_{k}_file.txt");

        files.Select(f => f.PathDisplay).ToHashSet(StringComparer.Ordinal).ShouldBe(expected, ignoreOrder: true);

        foreach (var f in files)
            f.IsRooted.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that a rooted-relative searchLocation that resolves to a sibling branch of `this` produces a relative path
    /// that navigates up to the LCA and back down, rather than leaking the rooted form.
    /// </summary>
    [TestMethod]
    public void GetRelativeEntriesFromRootedSearchLocation_SiblingBranch()
    {
        var testDir = SetupTestDirectory();
        var dir = testDir.CombineDirectory("0_dir").CombineDirectory("0_0_subdir"); // `this` lives under 0_dir
        var recursive = new SearchOptions { Recursive = true };

        // Rooted-relative path resolving to testDir/1_dir/ (sibling branch of 0_dir).
        string rootedPath = Sep + testDir.PathDisplay[testDir.RootDirectory.PathDisplay.Length..] + "1_dir" + Sep;
        var searchLocation = DirectoryPath.ParseRelative(rootedPath);

        var files = dir.GetRelativeEntries(searchLocation, "*_1_file.txt", recursive).ToArray();
        files.Length.ShouldBe(SubDirCount);

        // Expected: navigate up two levels (out of 0_0_subdir, out of 0_dir) to the LCA (testDir), then descend into 1_dir.
        var expected = new HashSet<string>(StringComparer.Ordinal);
        for (int j = 0; j < SubDirCount; j++)
            expected.Add($"..{Sep}..{Sep}1_dir{Sep}1_{j}_subdir{Sep}1_{j}_1_file.txt");

        files.Select(f => f.PathDisplay).ToHashSet(StringComparer.Ordinal).ShouldBe(expected, ignoreOrder: true);

        foreach (var f in files)
            f.IsRooted.ShouldBeFalse();
    }

    [TestMethod]
    public void GetChildEntriesInfo_ReturnsDirectories()
    {
        var dir = SetupTestDirectory();

        var children = dir.GetChildEntriesInfo().ToArray();

        children.Length.ShouldBe(DirCount);
    }

    /// <summary>
    /// Verifies that absolute directory paths returned from enumeration satisfy the directory-path invariant (trailing separator)
    /// and that absolute file paths do not. Also verifies that PathDisplay roundtrips through ParseAbsolute.
    /// </summary>
    [TestMethod]
    public void GetChildEntries_AbsolutePathsHaveCorrectTrailingSeparators()
    {
        var dir = SetupTestDirectory();
        var recursive = new SearchOptions { Recursive = true };

        var subdirs = dir.GetChildDirectories("?_?_subdir", recursive).ToArray();
        subdirs.Length.ShouldBe(DirCount * SubDirCount);

        foreach (var d in subdirs)
        {
            d.PathDisplay.ShouldEndWith(Sep.ToString());
            d.PathExport.ShouldEndWith(Sep.ToString());

            // Roundtrip through the parser to ensure the path is canonically formatted.
            var reparsed = DirectoryPath.ParseAbsolute(d.PathDisplay);
            reparsed.ShouldBe(d);
        }

        var files = dir.GetChildFiles("*_1_file.txt", recursive).ToArray();
        files.Length.ShouldBe(DirCount * SubDirCount);

        foreach (var f in files)
        {
            f.PathDisplay.ShouldNotEndWith(Sep.ToString());
            f.PathExport.ShouldNotEndWith(Sep.ToString());

            var reparsed = FilePath.ParseAbsolute(f.PathDisplay);
            reparsed.ShouldBe(f);
        }
    }

    /// <summary>
    /// Regression test for an off-by-one bug in GetRelativeChildEntries that chopped the first character of every entry name
    /// when enumerating a directory whose PathExport ended with a separator. The malformed paths were still well-formed enough
    /// to pass count and parent-chain assertions, so this test asserts the exact entry names returned.
    /// </summary>
    [TestMethod]
    public void GetRelativeChildEntries_DoesNotChopFirstCharacterOfEntryName()
    {
        var dir = SetupTestDirectory();
        var nonRecursive = new SearchOptions();

        var topLevelDirs = dir.GetRelativeChildDirectories("*_dir", nonRecursive).ToArray();
        topLevelDirs.Length.ShouldBe(DirCount);

        var expected = Enumerable.Range(0, DirCount).Select(i => $"{i}_dir{Sep}").ToHashSet(StringComparer.Ordinal);
        topLevelDirs.Select(d => d.PathDisplay).ToHashSet(StringComparer.Ordinal).ShouldBe(expected, ignoreOrder: true);

        foreach (var d in topLevelDirs)
            d.Name.ShouldEndWith("_dir"); // ensures the leading digit was not lost
    }

    /// <summary>
    /// Verifies that relative paths returned from GetRelativeChildEntries roundtrip through the parser, which would fail if
    /// the enumeration produced malformed paths (e.g. missing the leading character or missing the trailing directory separator).
    /// </summary>
    [TestMethod]
    public void GetRelativeChildEntries_PathsRoundtripThroughParser()
    {
        var dir = SetupTestDirectory();
        var recursive = new SearchOptions { Recursive = true };

        foreach (var f in dir.GetRelativeChildFiles("*_1_file.txt", recursive))
        {
            var reparsed = FilePath.ParseRelative(f.PathDisplay);
            reparsed.ShouldBe(f);
        }

        foreach (var d in dir.GetRelativeChildDirectories("?_?_subdir", recursive))
        {
            var reparsed = DirectoryPath.ParseRelative(d.PathDisplay);
            reparsed.ShouldBe(d);
        }
    }
}
