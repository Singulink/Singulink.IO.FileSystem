using Singulink.IO.Converters;

namespace Singulink.IO.FileSystem.Tests;

/// <summary>
/// Tests reading and writing path strings with <see cref="PathFormat.Windows"/> and <see cref="PathFormat.Unix"/> as the current platform's format, on any OS.
/// </summary>
[PrefixTestClass]
public class PathStringTests
{
    private const string WindowsAbsoluteError = "Path is a Windows absolute path. Absolute paths are read in the format of the platform the app runs on (Unix).";
    private const string UnixAbsoluteError = "Path is a Unix absolute path, which is a rooted relative path on Windows. " +
        "Absolute paths are read in the format of the platform the app runs on (Windows).";

    private static readonly PathFormat Windows = PathFormat.Windows;
    private static readonly PathFormat Unix = PathFormat.Unix;

    private static readonly Dictionary<Type, ConverterInfo> Converters = new() {
        [typeof(IAbsoluteDirectoryPath)] = ConverterInfo.Create(new AbsoluteDirectoryPathConverter()),
        [typeof(IAbsoluteFilePath)] = ConverterInfo.Create(new AbsoluteFilePathConverter()),
        [typeof(IRelativeDirectoryPath)] = ConverterInfo.Create(new RelativeDirectoryPathConverter()),
        [typeof(IRelativeFilePath)] = ConverterInfo.Create(new RelativeFilePathConverter()),
        [typeof(IDirectoryPath)] = ConverterInfo.Create(new DirectoryPathConverter()),
        [typeof(IFilePath)] = ConverterInfo.Create(new FilePathConverter()),
        [typeof(IPath)] = ConverterInfo.Create(new PathConverter()),
        [typeof(IAbsolutePath)] = ConverterInfo.Create(new AbsolutePathConverter()),
        [typeof(IRelativePath)] = ConverterInfo.Create(new RelativePathConverter()),
    };

    [TestMethod]
    [DataRow(typeof(IRelativeFilePath), @"data\file.txt", "data/file.txt")]
    [DataRow(typeof(IRelativeFilePath), @"..\..\data\file.txt", "../../data/file.txt")]
    [DataRow(typeof(IRelativeFilePath), "file.txt", "file.txt")]
    [DataRow(typeof(IRelativeDirectoryPath), @"data\uploads\", "data/uploads/")]
    [DataRow(typeof(IRelativeDirectoryPath), @"..\data\", "../data/")]
    [DataRow(typeof(IRelativeDirectoryPath), @"..\", "../")]
    [DataRow(typeof(IRelativeDirectoryPath), "", "")]
    [DataRow(typeof(IFilePath), @"data\file.txt", "data/file.txt")]
    [DataRow(typeof(IDirectoryPath), @"..\data\", "../data/")]
    [DataRow(typeof(IDirectoryPath), "", "")]
    [DataRow(typeof(IRelativePath), @"data\file.txt", "data/file.txt")]
    [DataRow(typeof(IRelativePath), @"data\uploads\", "data/uploads/")]
    [DataRow(typeof(IRelativePath), @"..\", "../")]
    [DataRow(typeof(IRelativePath), "", "")]
    [DataRow(typeof(IPath), @"..\data\file.txt", "../data/file.txt")]
    [DataRow(typeof(IPath), @"..\data\", "../data/")]
    [DataRow(typeof(IPath), "", "")]
    public void RelativePath_WrittenOnWindows_ReadOnUnixAndBack(Type type, string windowsDisplay, string universal)
    {
        bool isDirectory = IsDirectory(type, universal);
        var windowsPath = ParseRelative(windowsDisplay, Windows, isDirectory);

        Write(type, windowsPath).ShouldBe(universal);

        var unixPath = Read(type, universal, Unix);
        unixPath.ShouldBe(ParseRelative(universal, Unix, isDirectory));

        Write(type, unixPath).ShouldBe(universal);
        Read(type, universal, Windows).ShouldBe(windowsPath);
    }

    [TestMethod]
    [DataRow(typeof(IRelativeFilePath), "data/file.txt")]
    [DataRow(typeof(IRelativeFilePath), "../data/file.txt")]
    [DataRow(typeof(IRelativeDirectoryPath), "data/uploads/")]
    [DataRow(typeof(IRelativeDirectoryPath), "../../")]
    [DataRow(typeof(IRelativeDirectoryPath), "")]
    [DataRow(typeof(IFilePath), "data/file.txt")]
    [DataRow(typeof(IDirectoryPath), "data/uploads/")]
    [DataRow(typeof(IRelativePath), "data/file.txt")]
    [DataRow(typeof(IRelativePath), "../")]
    [DataRow(typeof(IPath), "data/uploads/")]
    [DataRow(typeof(IPath), "data/file.txt")]
    [DataRow(typeof(IPath), "")]
    public void RelativePath_WrittenOnUnix_ReadOnWindowsAndBack(Type type, string unixDisplay)
    {
        bool isDirectory = IsDirectory(type, unixDisplay);
        var unixPath = ParseRelative(unixDisplay, Unix, isDirectory);

        Write(type, unixPath).ShouldBe(unixDisplay);

        var windowsPath = Read(type, unixDisplay, Windows);
        windowsPath.ShouldBe(ParseRelative(unixDisplay, Windows, isDirectory));
        windowsPath.PathDisplay.ShouldBe(unixDisplay.Replace('/', '\\'));

        Write(type, windowsPath).ShouldBe(unixDisplay);
        Read(type, unixDisplay, Unix).ShouldBe(unixPath);
    }

    [TestMethod]
    [DataRow(typeof(IRelativeFilePath), "./data/../file.txt", "file.txt")]
    [DataRow(typeof(IRelativeDirectoryPath), ".", "")]
    [DataRow(typeof(IRelativeDirectoryPath), "data/..", "")]
    [DataRow(typeof(IRelativeDirectoryPath), "data", "data/")]
    [DataRow(typeof(IRelativePath), "data/..", "")]
    [DataRow(typeof(IRelativePath), "..", "../")]
    [DataRow(typeof(IPath), ".", "")]
    [DataRow(typeof(IPath), "a/b/.", "a/b/")]
    public void RelativePath_HandWritten_ReadsLikeParseOnBothPlatforms(Type type, string value, string written)
    {
        foreach (var current in new[] { Windows, Unix })
        {
            bool isDirectory = IsDirectory(type, value);
            var path = Read(type, value, current);

            path.ShouldBe(ParseRelative(value, current, isDirectory));
            Write(type, path).ShouldBe(written);
            Read(type, written, current).ShouldBe(path);
        }
    }

    [TestMethod]
    [DataRow(typeof(IRelativeFilePath), "a:b/file.txt")]
    [DataRow(typeof(IRelativeFilePath), "data/a*b.txt")]
    [DataRow(typeof(IRelativeDirectoryPath), "data/a?b/")]
    [DataRow(typeof(IRelativeDirectoryPath), @"data/a\b/")]
    [DataRow(typeof(IPath), "data/a|b/")]
    [DataRow(typeof(IRelativePath), @"a\b.txt")]
    public void UnixPath_NotInUniversalFormat_WrittenAsPathDisplay(Type type, string unixDisplay)
    {
        var unixPath = ParseRelative(unixDisplay, Unix, IsDirectory(type, unixDisplay));

        Write(type, unixPath).ShouldBe(unixDisplay);
        Read(type, unixDisplay, Unix).ShouldBe(unixPath);
    }

    [TestMethod]
    [DataRow(typeof(IRelativeFilePath), @"\data\file.txt")]
    [DataRow(typeof(IRelativeDirectoryPath), @"\data\")]
    [DataRow(typeof(IRelativeDirectoryPath), @"\")]
    [DataRow(typeof(IDirectoryPath), @"\data\")]
    [DataRow(typeof(IRelativePath), @"\data\file.txt")]
    [DataRow(typeof(IPath), @"\data\")]
    public void WindowsRootedRelativePath_WrittenAsPathDisplay(Type type, string windowsDisplay)
    {
        var windowsPath = ParseRelative(windowsDisplay, Windows, IsDirectory(type, windowsDisplay));

        Write(type, windowsPath).ShouldBe(windowsDisplay);
        Read(type, windowsDisplay, Windows).ShouldBe(windowsPath);
    }

    [TestMethod]
    [DataRow(typeof(IAbsoluteDirectoryPath), true, @"C:\data \")]
    [DataRow(typeof(IAbsoluteDirectoryPath), true, @"C:\data.\")]
    [DataRow(typeof(IAbsoluteDirectoryPath), true, @"C:\con\")]
    [DataRow(typeof(IAbsoluteDirectoryPath), true, @"\\server\share\ x\nul.txt\")]
    [DataRow(typeof(IAbsoluteFilePath), true, @"C:\data\file ")]
    [DataRow(typeof(IAbsoluteFilePath), true, @"C:\data\aux.txt")]
    [DataRow(typeof(IAbsoluteFilePath), true, @"C:\x.")]
    [DataRow(typeof(IRelativeDirectoryPath), true, @"data \")]
    [DataRow(typeof(IRelativeDirectoryPath), true, @"..\con.\")]
    [DataRow(typeof(IRelativeFilePath), true, @"data \file.")]
    [DataRow(typeof(IRelativeFilePath), true, " lpt1")]
    [DataRow(typeof(IDirectoryPath), true, @"C:\data \")]
    [DataRow(typeof(IDirectoryPath), true, @"data.\")]
    [DataRow(typeof(IFilePath), true, @"data\prn")]
    [DataRow(typeof(IPath), true, @"C:\data \file.")]
    [DataRow(typeof(IPath), true, @"data \")]
    [DataRow(typeof(IPath), true, "x.")]
    [DataRow(typeof(IAbsolutePath), true, @"C:\com1\")]
    [DataRow(typeof(IRelativePath), true, @"data \file")]
    [DataRow(typeof(IAbsoluteDirectoryPath), false, "/data\u0001/")]
    [DataRow(typeof(IAbsoluteDirectoryPath), false, "/data /")]
    [DataRow(typeof(IAbsoluteFilePath), false, "/data/\u001f.")]
    [DataRow(typeof(IAbsoluteFilePath), false, "/data/ x")]
    [DataRow(typeof(IRelativeDirectoryPath), false, "a\u0001/")]
    [DataRow(typeof(IRelativeDirectoryPath), false, "con /")]
    [DataRow(typeof(IRelativeFilePath), false, "data/\tfile")]
    [DataRow(typeof(IRelativeFilePath), false, "../x.")]
    [DataRow(typeof(IDirectoryPath), false, "a\u0001/")]
    [DataRow(typeof(IFilePath), false, "/a\u0001")]
    [DataRow(typeof(IPath), false, "/a\u0002/")]
    [DataRow(typeof(IPath), false, "a/\u0003")]
    [DataRow(typeof(IAbsolutePath), false, "/x ")]
    [DataRow(typeof(IRelativePath), false, "\u0001/")]
    public void PathWithUnfriendlyName_ReadsBackAsSamePath(Type type, bool isWindows, string display)
    {
        var current = isWindows ? Windows : Unix;
        var other = isWindows ? Unix : Windows;
        bool absolute = current.GetPathKind(display) is PathKind.Absolute;
        var path = Parse(display, current, absolute, IsDirectory(type, display), PathOptions.None);

        path.IsValidFor(PathOptions.NoUnfriendlyNames).ShouldBeFalse();

        string written = Write(type, path);
        Read(type, written, current).ShouldBe(path);

        // A relative path also reads on the other platform as the same names when its names are valid there.
        if (path is IRelativePath relativePath && ((IRelativePath.Impl)relativePath).TryToPathFormat(other, PathOptions.None, out var otherPath, out _))
            Read(type, written, other).ShouldBe(otherPath);
    }

    [TestMethod]
    public void UniversalFormatPath_WrittenAsIs()
    {
        var path = DirectoryPath.ParseRelative("../data/", PathFormat.Universal);

        Write(typeof(IRelativeDirectoryPath), path).ShouldBe("../data/");
        Write(typeof(IPath), path).ShouldBe("../data/");
    }

    [TestMethod]
    [DataRow(typeof(IAbsoluteDirectoryPath), @"C:\data\", WindowsAbsoluteError)]
    [DataRow(typeof(IAbsoluteDirectoryPath), "C:/data/", WindowsAbsoluteError)]
    [DataRow(typeof(IAbsoluteDirectoryPath), "c:", WindowsAbsoluteError)]
    [DataRow(typeof(IAbsoluteDirectoryPath), @"\\server\share\", WindowsAbsoluteError)]
    [DataRow(typeof(IAbsoluteDirectoryPath), @"\\?\C:\data\", WindowsAbsoluteError)]
    [DataRow(typeof(IAbsoluteDirectoryPath), @"\\.\C:\data\", WindowsAbsoluteError)]
    [DataRow(typeof(IAbsoluteFilePath), @"C:\data\file.txt", WindowsAbsoluteError)]
    [DataRow(typeof(IAbsolutePath), @"C:\data\", WindowsAbsoluteError)]
    [DataRow(typeof(IAbsolutePath), @"C:\data\file.txt", WindowsAbsoluteError)]
    public void WindowsAbsoluteString_ReadOnUnixAsAbsoluteType_Throws(Type type, string value, string error)
    {
        ReadError(type, value, Unix).ShouldBe($"Invalid {Description(type)} '{value}': {error}");
    }

    [TestMethod]
    [DataRow(typeof(IPath), @"C:\data\", false)]
    [DataRow(typeof(IPath), "C:/data/", true)]
    [DataRow(typeof(IPath), @"\\server\share\file.txt", false)]
    [DataRow(typeof(IDirectoryPath), @"C:\data\", true)]
    [DataRow(typeof(IFilePath), "C:/data/file.txt", false)]
    [DataRow(typeof(IRelativeDirectoryPath), @"\\server\share\", true)]
    [DataRow(typeof(IRelativeFilePath), "C:", false)]
    [DataRow(typeof(IRelativePath), @"\\?\C:\data\file.txt", false)]
    public void WindowsAbsoluteString_ReadOnUnixAsOtherType_IsUnixRelativePath(Type type, string value, bool isDirectory)
    {
        var path = Read(type, value, Unix);

        path.ShouldBe(ParseRelative(value, Unix, isDirectory));
        Write(type, path).ShouldBe(path.PathDisplay);
        Read(type, path.PathDisplay, Unix).ShouldBe(path);
    }

    [TestMethod]
    [DataRow(@"C:\data\")]
    [DataRow("C:/data/file.txt")]
    [DataRow("C:")]
    [DataRow(@"\\server\share\file.txt")]
    [DataRow(@"data\file.txt")]
    [DataRow(@"data\uploads\")]
    [DataRow("data/uploads/")]
    [DataRow("/data/")]
    [DataRow("/data/file.txt")]
    [DataRow(@"\data\")]
    [DataRow("a:b.txt")]
    [DataRow("")]
    [DataRow("..")]
    [DataRow("data/name ")]
    [DataRow(@"C:\con\nul.txt")]
    [DataRow("/con./ x")]
    [DataRow("a\u0001b")]
    [DataRow("data//name")]
    public void TypedConverters_ReadLikeParseWithNone(string value)
    {
        foreach (var current in new[] { Windows, Unix })
        {
            Check(typeof(IAbsoluteDirectoryPath), () => DirectoryPath.ParseAbsolute(value, current, PathOptions.None));
            Check(typeof(IAbsoluteFilePath), () => FilePath.ParseAbsolute(value, current, PathOptions.None));
            Check(typeof(IRelativeDirectoryPath), () => DirectoryPath.ParseRelative(value, current, PathOptions.None));
            Check(typeof(IRelativeFilePath), () => FilePath.ParseRelative(value, current, PathOptions.None));
            Check(typeof(IDirectoryPath), () => DirectoryPath.Parse(value, current, PathOptions.None));
            Check(typeof(IFilePath), () => FilePath.Parse(value, current, PathOptions.None));

            void Check(Type type, Func<IPath> parse)
            {
                IPath? expected;

                try
                {
                    expected = parse();
                }
                catch (ArgumentException)
                {
                    expected = null;
                }

                TryRead(type, value, current).Path.ShouldBe(expected);
            }
        }
    }

    [TestMethod]
    [DataRow(typeof(IAbsoluteDirectoryPath), "/data/")]
    [DataRow(typeof(IAbsoluteDirectoryPath), "/")]
    [DataRow(typeof(IAbsoluteFilePath), "/data/file.txt")]
    [DataRow(typeof(IAbsolutePath), "/data/")]
    [DataRow(typeof(IAbsolutePath), "/data/file.txt")]
    public void UnixAbsoluteString_ReadOnWindowsAsAbsoluteType_Throws(Type type, string value)
    {
        ReadError(type, value, Windows).ShouldBe($"Invalid {Description(type)} '{value}': {UnixAbsoluteError}");
    }

    [TestMethod]
    [DataRow(typeof(IRelativeDirectoryPath), "/data/")]
    [DataRow(typeof(IRelativeFilePath), "/data/file.txt")]
    [DataRow(typeof(IDirectoryPath), "/data/")]
    [DataRow(typeof(IFilePath), "/data/file.txt")]
    [DataRow(typeof(IRelativePath), "/data/")]
    [DataRow(typeof(IPath), "/data/file.txt")]
    public void UnixAbsoluteString_ReadOnWindowsAsOtherType_IsRootedRelativePath(Type type, string value)
    {
        var path = Read(type, value, Windows);

        path.ShouldBe(ParseRelative(value, Windows, IsDirectory(type, value)));
        path.IsRooted.ShouldBeTrue();
        Write(type, path).ShouldBe(value.Replace('/', '\\'));
    }

    [TestMethod]
    [DataRow(typeof(IAbsoluteDirectoryPath), @"C:\data\")]
    [DataRow(typeof(IAbsoluteDirectoryPath), "C:/data/")]
    [DataRow(typeof(IAbsoluteDirectoryPath), "//server/share/")]
    [DataRow(typeof(IAbsoluteFilePath), @"\\server\share\file.txt")]
    [DataRow(typeof(IPath), @"C:\data\file.txt")]
    [DataRow(typeof(IDirectoryPath), @"\\?\C:\data\")]
    public void AbsolutePath_OnWindows_ReadAndWrittenInWindowsFormat(Type type, string value)
    {
        var path = Read(type, value, Windows);
        var expected = Parse(value, Windows, absolute: true, IsDirectory(type, value));

        path.ShouldBe(expected);
        Write(type, path).ShouldBe(expected.PathDisplay);
    }

    [TestMethod]
    [DataRow(typeof(IAbsoluteDirectoryPath), "/data/")]
    [DataRow(typeof(IAbsoluteFilePath), @"/data/a\b.txt")]
    [DataRow(typeof(IAbsolutePath), "/data/a:b/")]
    [DataRow(typeof(IPath), "/data/file.txt")]
    [DataRow(typeof(IFilePath), "/data/file.txt")]
    public void AbsolutePath_OnUnix_ReadAndWrittenInUnixFormat(Type type, string value)
    {
        var path = Read(type, value, Unix);
        var expected = Parse(value, Unix, absolute: true, IsDirectory(type, value));

        path.ShouldBe(expected);
        Write(type, path).ShouldBe(value);
    }

    [TestMethod]
    [DataRow(typeof(IRelativeFilePath), "c:file.txt")]
    [DataRow(typeof(IRelativeFilePath), "data:file.txt")]
    [DataRow(typeof(IPath), "1:/file.txt")]
    [DataRow(typeof(IPath), @"\data\file.txt")]
    public void WindowsLikeRelativeString_ReadOnUnix_IsRelativePath(Type type, string value)
    {
        Read(type, value, Unix).ShouldBe(ParseRelative(value, Unix, IsDirectory(type, value)));
    }

    [TestMethod]
    [DataRow(typeof(IRelativeFilePath), @"data\file.txt")]
    [DataRow(typeof(IRelativeDirectoryPath), @"data\uploads\")]
    [DataRow(typeof(IPath), @"data\file.txt")]
    public void BackslashString_ReadOnUnix_IsUnixName(Type type, string value)
    {
        var path = Read(type, value, Unix);

        path.ShouldBe(ParseRelative(value, Unix, IsDirectory(type, value)));
        path.PathDisplay.ShouldStartWith(value);
    }

    private static bool IsDirectory(Type type, string value)
    {
        if (typeof(IDirectoryPath).IsAssignableFrom(type))
            return true;

        if (typeof(IFilePath).IsAssignableFrom(type))
            return false;

        return value.Length is 0 || value[^1] is '/' or '\\' || value is "." or ".." || value.EndsWith("/.", StringComparison.Ordinal) ||
            value.EndsWith("/..", StringComparison.Ordinal) || value.EndsWith(@"\..", StringComparison.Ordinal);
    }

    private static IPath ParseRelative(string value, PathFormat format, bool isDirectory) => Parse(value, format, absolute: false, isDirectory);

    private static IPath Parse(string value, PathFormat format, bool absolute, bool isDirectory, PathOptions options = PathOptions.NoUnfriendlyNames)
    {
        return (absolute, isDirectory) switch {
            (true, true) => DirectoryPath.ParseAbsolute(value, format, options),
            (true, false) => FilePath.ParseAbsolute(value, format, options),
            (false, true) => DirectoryPath.ParseRelative(value, format, options),
            (false, false) => FilePath.ParseRelative(value, format, options),
        };
    }

    private static IPath Read(Type type, string value, PathFormat current)
    {
        var (path, error) = TryRead(type, value, current);
        error.ShouldBeNull();

        return path.ShouldNotBeNull();
    }

    private static string ReadError(Type type, string value, PathFormat current)
    {
        var (path, error) = TryRead(type, value, current);
        path.ShouldBeNull();

        return error.ShouldNotBeNull();
    }

    private static (IPath? Path, string? Error) TryRead(Type type, string value, PathFormat current) => Converters[type].TryRead(value, current);

    private static string Write(Type type, IPath path)
    {
        type.IsInstanceOfType(path).ShouldBeTrue();
        return Converters[type].Write(path);
    }

    private static string Description(Type type) => Converters[type].Description;

    private sealed record ConverterInfo(string Description, Func<string, PathFormat, (IPath? Path, string? Error)> TryRead, Func<IPath, string> Write)
    {
        public static ConverterInfo Create<TPath>(PathTypeConverter<TPath> converter) where TPath : class, IPath
        {
            return new(converter.PathTypeDescription, (value, current) => TryParse(converter, value, current), path => PathStrings.GetString((TPath)path));
        }

        private static (IPath? Path, string? Error) TryParse<TPath>(PathTypeConverter<TPath> converter, string value, PathFormat current) where TPath : class, IPath
        {
            if (!converter.TryParse(value, current, out var path, out string? error))
                return (null, converter.GetErrorMessage(value, error));

            // The path read is always in the current platform's format.
            path.PathFormat.ShouldBe(current);

            return (path, null);
        }
    }
}
