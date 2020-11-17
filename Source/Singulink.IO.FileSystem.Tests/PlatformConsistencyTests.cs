using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Singulink.IO.FileSystem.Tests
{
    [TestClass]
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

            Assert.IsFalse(file.Exists);
            Assert.ThrowsException<FileNotFoundException>(() => _ = file.Attributes);
            Assert.ThrowsException<FileNotFoundException>(() => file.IsReadOnly = true);
            Assert.ThrowsException<FileNotFoundException>(() => file.Attributes |= FileAttributes.Hidden);
            Assert.ThrowsException<FileNotFoundException>(() => file.Length);

            Assert.ThrowsException<FileNotFoundException>(() => file.Delete());
        }

        [TestMethod]
        public void DirectoryIsFile()
        {
            var dir = SetupTestDirectory().CombineDirectory(FileName);

            Assert.IsFalse(dir.Exists);
            Assert.ThrowsException<DirectoryNotFoundException>(() => _ = dir.IsEmpty);
            Assert.ThrowsException<DirectoryNotFoundException>(() => _ = dir.Attributes);
            Assert.ThrowsException<DirectoryNotFoundException>(() => _ = dir.TotalFreeSpace);
            Assert.ThrowsException<DirectoryNotFoundException>(() => dir.Attributes |= FileAttributes.Hidden);
            Assert.ThrowsException<DirectoryNotFoundException>(() => _ = dir.DriveType);
            Assert.ThrowsException<DirectoryNotFoundException>(() => _ = dir.FileSystem);

            Assert.ThrowsException<DirectoryNotFoundException>(() => dir.GetChildEntries().FirstOrDefault());

            Assert.ThrowsException<DirectoryNotFoundException>(() => dir.Delete(true));
        }
    }
}
