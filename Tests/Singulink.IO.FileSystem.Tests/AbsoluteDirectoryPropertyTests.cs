using System.Reflection;

namespace Singulink.IO.FileSystem.Tests;

[PrefixTestClass]
public class AbsoluteDirectoryPropertyTests
{
    public static readonly DateTime EarliestDateTime = new DateTime(2010, 1, 1);

    [TestMethod]
    public void Properties()
    {
        var dir = FilePath.ParseAbsolute(Assembly.GetExecutingAssembly().Location).ParentDirectory;

        dir.Exists.ShouldBeTrue();
        dir.TotalSize.ShouldBeGreaterThan(0);
        dir.TotalFreeSpace.ShouldBeGreaterThan(0);
        dir.AvailableFreeSpace.ShouldBeGreaterThan(0);
        dir.FileSystem.ShouldNotBeNullOrWhiteSpace();
        dir.DriveType.ShouldNotBe(DriveType.NoRootDirectory);
        dir.CreationTime.ShouldBeGreaterThan(EarliestDateTime);
        dir.LastAccessTime.ShouldBeGreaterThan(EarliestDateTime);
        dir.LastWriteTime.ShouldBeGreaterThan(EarliestDateTime);
    }
}
