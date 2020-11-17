using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#pragma warning disable SA1122 // Use string.Empty for empty strings

namespace Singulink.IO.FileSystem.Tests
{
    [TestClass]
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

            for (int i = 0; i < DirCount; i++) {
                var dirLevel1 = testDir.CombineDirectory($"{i}_dir");

                for (int j = 0; j < SubDirCount; j++) {
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
            Assert.AreEqual(TotalDirCount + TotalFileCount, entryCount);

            int fileCount = dir.GetChildFiles(recursive).Count();
            Assert.AreEqual(TotalFileCount, fileCount);

            int dirCount = dir.GetChildDirectories(recursive).Count();
            Assert.AreEqual(TotalDirCount, dirCount);
        }

        [TestMethod]
        public void GetFilteredChildEntries()
        {
            var dir = SetupTestDirectory();
            var recursive = new SearchOptions { Recursive = true };
            var nonRecursive = new SearchOptions();

            int dirCount = dir.GetChildDirectories("?_?_subdir", recursive).Count();
            Assert.AreEqual(DirCount * SubDirCount, dirCount);

            int fileCount = dir
                .GetChildDirectories("1_dir", nonRecursive).Single()
                .GetChildDirectories("1_1_subdir", nonRecursive).Single()
                .GetChildFiles("*file.txt", nonRecursive).Count();

            Assert.AreEqual(FileCount, fileCount);
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

            Assert.AreEqual("1_1_1_file.txt", file.PathDisplay);

            var files = dir.GetRelativeChildFiles("*_1_file.txt", recursive).ToArray();
            Assert.AreEqual(DirCount * SubDirCount, files.Length);

            foreach (var f in files) {
                Assert.AreEqual("", f.ParentDirectory!.ParentDirectory!.ParentDirectory!.PathDisplay);
                Assert.AreEqual(false, f.IsRooted);
            }
        }

        [TestMethod]
        public void GetRelativeEntriesFromParentSearchLocation()
        {
            var dir = SetupTestDirectory();
            var recursive = new SearchOptions { Recursive = true };

            var parentDir = DirectoryPath.ParseRelative("..");

            var files = dir.GetRelativeEntries(parentDir, "*_1_file.txt", recursive).ToArray();
            Assert.AreEqual(DirCount * SubDirCount, files.Length);

            foreach (var f in files) {
                Assert.AreEqual("", f.ParentDirectory!.ParentDirectory!.ParentDirectory!.PathDisplay);
                Assert.AreEqual(false, f.IsRooted);
            }
        }

        [TestMethod]
        public void GetRelativeEntriesFromSubdirectorySearchLocation()
        {
            var dir = SetupTestDirectory();
            var recursive = new SearchOptions { Recursive = true };

            var dirLevel1 = DirectoryPath.ParseRelative("1_dir");

            var files = dir.GetRelativeEntries(dirLevel1, "*_1_file.txt", recursive).ToArray();
            Assert.AreEqual(SubDirCount, files.Length);

            foreach (var f in files) {
                Assert.AreEqual("", f.ParentDirectory!.ParentDirectory!.ParentDirectory!.PathDisplay);
                Assert.AreEqual(false, f.IsRooted);
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
            Assert.AreEqual(DirCount * SubDirCount, files.Length);

            foreach (var f in files) {
                Assert.AreEqual(currentDir.PathDisplay, f.ParentDirectory!.ParentDirectory!.ParentDirectory!.PathDisplay);
                Assert.AreEqual(false, f.IsRooted);
            }
        }
    }
}
