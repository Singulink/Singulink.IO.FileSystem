namespace Singulink.IO.FileSystem.Tests;

[PrefixTestClass]
public class EqualsTests
{
    [TestMethod]
    public void EqualityOperatorsMatchingFile()
    {
        var x = FilePath.Parse(@"c:\somepath", PathFormat.Windows);
        var y = FilePath.Parse(@"C:\somepath", PathFormat.Windows);

        (x == y).ShouldBeTrue();
        (x != y).ShouldBeFalse();
    }

    [TestMethod]
    public void EqualityOperatorsOtherFile()
    {
        var x = FilePath.Parse(@"c:\somepath", PathFormat.Windows);
        var y = FilePath.Parse(@"C:\someotherpath", PathFormat.Windows);

        (x == y).ShouldBeFalse();
        (x != y).ShouldBeTrue();
    }

    [TestMethod]
    public void EqualsMatchingFile()
    {
        var x = FilePath.Parse(@"c:\somepath", PathFormat.Windows);
        var y = FilePath.Parse(@"C:\somepath", PathFormat.Windows);

        x.Equals(y).ShouldBeTrue();
        x.ShouldBe(y);
    }

    [TestMethod]
    public void NotEqualsOtherFile()
    {
        var x = FilePath.Parse(@"c:\somepath", PathFormat.Windows);
        var y = FilePath.Parse(@"C:\someotherpath", PathFormat.Windows);

        x.Equals(y).ShouldBeFalse();
        x.ShouldNotBe(y);
    }

    [TestMethod]
    public void NotEqualsMatchingDir()
    {
        var x = FilePath.Parse(@"c:\somepath", PathFormat.Windows);
        var y = DirectoryPath.Parse(@"c:\somepath", PathFormat.Windows);

        x.Equals(y).ShouldBeFalse();
        ((IPath)x).ShouldNotBe(y);
    }
}
