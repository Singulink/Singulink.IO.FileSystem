namespace Singulink.IO.FileSystem.Tests;

[PrefixTestClass]
public class EnumeratingDirectoryTests
{
    private const int DirCount = 5;
    private const int SubDirCount = 6;
    private const int FileCount = 7;

    private const int TotalDirCount = DirCount + (DirCount * SubDirCount);
    private const int TotalFileCount = DirCount * SubDirCount * FileCount;

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
        files.Length.ShouldBe(DirCount * SubDirCount);

        foreach (var f in files)
        {
            f.ParentDirectory!.ParentDirectory!.ParentDirectory!.PathDisplay.ShouldBe("");
            f.IsRooted.ShouldBeFalse();
        }
    }

    [TestMethod]
    public void GetRelativeEntriesFromParentSearchLocation()
    {
        var dir = SetupTestDirectory();
        var recursive = new SearchOptions { Recursive = true };

        var parentDir = DirectoryPath.ParseRelative("..");

        var files = dir.GetRelativeEntries(parentDir, "*_1_file.txt", recursive).ToArray();
        files.Length.ShouldBe(DirCount * SubDirCount);

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

        foreach (var f in files)
        {
            f.ParentDirectory!.ParentDirectory!.ParentDirectory!.PathDisplay.ShouldBe("");
            f.IsRooted.ShouldBeFalse();
        }
    }

    [DataTestMethod]
    [DataRow(".")]
    [DataRow("../_test")]
    public void GetRelativeEntriesFromCurrent(string currentDirPath)
    {
        var dir = SetupTestDirectory();
        var recursive = new SearchOptions { Recursive = true };

        var currentDir = DirectoryPath.ParseRelative(currentDirPath);
        var files = dir.GetRelativeEntries(currentDir, "*_1_file.txt", recursive).ToArray();
        files.Length.ShouldBe(DirCount * SubDirCount);

        foreach (var f in files)
        {
            f.ParentDirectory!.ParentDirectory!.ParentDirectory!.PathDisplay.ShouldBe(currentDir.PathDisplay);
            f.IsRooted.ShouldBeFalse();
        }
    }
}
