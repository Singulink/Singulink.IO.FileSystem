using System.Diagnostics.CodeAnalysis;

namespace Singulink.IO.FileSystem.Tests;

[PrefixTestClass]
public class IsValidForTests
{
    private static readonly PathFormat Windows = PathFormat.Windows;
    private static readonly PathFormat Unix = PathFormat.Unix;
    private static readonly PathFormat Universal = PathFormat.Universal;

    private static readonly PathOptions[] OptionFlags = [
        PathOptions.AllowEmptyDirectories,
        PathOptions.NoReservedDeviceNames,
        PathOptions.NoLeadingSpaces,
        PathOptions.NoTrailingSpaces,
        PathOptions.NoTrailingDots,
        PathOptions.NoNavigation,
        PathOptions.NoControlCharacters,
        PathOptions.PathFormatDependent,
    ];

    /// <summary>
    /// Every combination of the option flags.
    /// </summary>
    private static readonly PathOptions[] AllOptions = [.. Enumerable.Range(0, 1 << OptionFlags.Length).Select(GetOptions)];

    /// <summary>
    /// The options paths are parsed with, so that they start with different known validity.
    /// </summary>
    private static readonly PathOptions[] ParseOptions = [
        PathOptions.None,
        PathOptions.NoUnfriendlyNames,
        PathOptions.NoNavigation,
        PathOptions.PathFormatDependent,
        PathOptions.NoUnfriendlyNames | PathOptions.NoNavigation,
        PathOptions.NoTrailingSpaces | PathOptions.NoReservedDeviceNames,
    ];

    /// <summary>
    /// Paths that parse with <see cref="PathOptions.None"/>.
    /// </summary>
    private static readonly (Kind Kind, PathFormat Format, string Path)[] Paths = [
        (Kind.AbsoluteDirectory, Windows, @"C:\"),
        (Kind.AbsoluteDirectory, Windows, @"C:\data\"),
        (Kind.AbsoluteDirectory, Windows, @"C:\data \"),
        (Kind.AbsoluteDirectory, Windows, @"C:\ data\"),
        (Kind.AbsoluteDirectory, Windows, @"C:\data.\"),
        (Kind.AbsoluteDirectory, Windows, @"C:\con\"),
        (Kind.AbsoluteDirectory, Windows, @"C:\aux.txt\"),
        (Kind.AbsoluteDirectory, Windows, @"C:\com1\x\"),
        (Kind.AbsoluteDirectory, Windows, @"C:\a\..\b\"),
        (Kind.AbsoluteDirectory, Windows, @"\\server\share\"),
        (Kind.AbsoluteDirectory, Windows, @"\\server\share.\con\"),
        (Kind.AbsoluteDirectory, Windows, @"\\server\share\lpt9.log\"),
        (Kind.AbsoluteDirectory, Windows, @"\\?\C:\x \"),
        (Kind.AbsoluteFile, Windows, @"C:\file.txt"),
        (Kind.AbsoluteFile, Windows, @"C:\dir\file "),
        (Kind.AbsoluteFile, Windows, @"C:\dir\ file"),
        (Kind.AbsoluteFile, Windows, @"C:\dir\file."),
        (Kind.AbsoluteFile, Windows, @"C:\nul"),
        (Kind.AbsoluteFile, Windows, @"C:\COM9."),
        (Kind.AbsoluteFile, Windows, @"C:\dir.\con.txt"),
        (Kind.AbsoluteFile, Windows, @"C:\x\..\y"),
        (Kind.AbsoluteFile, Windows, @"\\server\share\a. \b"),
        (Kind.AbsoluteFile, Windows, @"C:\dir\con.tar.gz"),
        (Kind.AbsoluteFile, Windows, @"C:\dir\x .Tar.GZ"),
        (Kind.RelativeDirectory, Windows, ""),
        (Kind.RelativeDirectory, Windows, @"data\"),
        (Kind.RelativeDirectory, Windows, @"..\"),
        (Kind.RelativeDirectory, Windows, @"..\..\data \"),
        (Kind.RelativeDirectory, Windows, @"\"),
        (Kind.RelativeDirectory, Windows, @"\data.\"),
        (Kind.RelativeDirectory, Windows, "/x/"),
        (Kind.RelativeDirectory, Windows, @"con\"),
        (Kind.RelativeDirectory, Windows, @"a\..\b\"),
        (Kind.RelativeDirectory, Windows, @" x\"),
        (Kind.RelativeDirectory, Windows, @".\x\"),
        (Kind.RelativeFile, Windows, "file.txt"),
        (Kind.RelativeFile, Windows, @"..\file.txt"),
        (Kind.RelativeFile, Windows, @"\file.txt"),
        (Kind.RelativeFile, Windows, @"data \file."),
        (Kind.RelativeFile, Windows, "prn"),
        (Kind.RelativeFile, Windows, " lead"),
        (Kind.RelativeFile, Windows, "trail "),
        (Kind.RelativeFile, Windows, "a/b.txt"),
        (Kind.RelativeFile, Windows, @"a\..txt"),
        (Kind.RelativeFile, Windows, ".gitignore"),
        (Kind.AbsoluteDirectory, Unix, "/"),
        (Kind.AbsoluteDirectory, Unix, "/data/"),
        (Kind.AbsoluteDirectory, Unix, "/data /"),
        (Kind.AbsoluteDirectory, Unix, "/ data/"),
        (Kind.AbsoluteDirectory, Unix, "/data./"),
        (Kind.AbsoluteDirectory, Unix, "/con/"),
        (Kind.AbsoluteDirectory, Unix, "/a\u0001b/"),
        (Kind.AbsoluteDirectory, Unix, @"/C:\x/"),
        (Kind.AbsoluteDirectory, Unix, "/a/../b/"),
        (Kind.AbsoluteFile, Unix, "/file"),
        (Kind.AbsoluteFile, Unix, "/a/b "),
        (Kind.AbsoluteFile, Unix, "/a/\tb"),
        (Kind.AbsoluteFile, Unix, "/a/nul.txt"),
        (Kind.AbsoluteFile, Unix, "/a/x."),
        (Kind.AbsoluteFile, Unix, "/a/...txt"),
        (Kind.RelativeDirectory, Unix, ""),
        (Kind.RelativeDirectory, Unix, "a/"),
        (Kind.RelativeDirectory, Unix, "../"),
        (Kind.RelativeDirectory, Unix, "../x /"),
        (Kind.RelativeDirectory, Unix, "con/"),
        (Kind.RelativeDirectory, Unix, "a\u0002/"),
        (Kind.RelativeDirectory, Unix, "x/../y/"),
        (Kind.RelativeDirectory, Unix, "./"),
        (Kind.RelativeFile, Unix, "f"),
        (Kind.RelativeFile, Unix, "../f."),
        (Kind.RelativeFile, Unix, @"a\b.txt"),
        (Kind.RelativeFile, Unix, "\u0001"),
        (Kind.RelativeFile, Unix, " x"),
        (Kind.RelativeFile, Unix, @"C:\x"),
        (Kind.RelativeFile, Unix, "x..gz"),
        (Kind.RelativeDirectory, Universal, ""),
        (Kind.RelativeDirectory, Universal, "a/"),
        (Kind.RelativeDirectory, Universal, "../a/"),
        (Kind.RelativeDirectory, Universal, "x /"),
        (Kind.RelativeDirectory, Universal, "con/"),
        (Kind.RelativeDirectory, Universal, ".x./"),
        (Kind.RelativeFile, Universal, "f"),
        (Kind.RelativeFile, Universal, "../f."),
        (Kind.RelativeFile, Universal, " f"),
        (Kind.RelativeFile, Universal, "aux.c"),
        (Kind.RelativeFile, Universal, "../../lpt1"),
        (Kind.RelativeFile, Universal, "a/b .tar.gz"),
        (Kind.RelativeFile, Universal, ".tar.gz"),
    ];

    private enum Kind
    {
        AbsoluteFile,
        AbsoluteDirectory,
        RelativeFile,
        RelativeDirectory,
    }

    [TestMethod]
    [DataRow(@"C:\data \", PathOptions.None, null)]
    [DataRow(@"C:\data \", PathOptions.AllowEmptyDirectories, null)]
    [DataRow(@"C:\data \", PathOptions.NoUnfriendlyNames, "Entry name ends with a space.")]
    [DataRow(@"C:\data \", PathOptions.PathFormatDependent, "Entry name ends with a space.")]
    [DataRow(@"C:\con\", PathOptions.NoReservedDeviceNames, "Invalid reserved device name in entry name 'con'.")]
    [DataRow(@"C:\con\", PathOptions.NoNavigation, null)]
    public void WindowsAbsoluteDirectory(string path, PathOptions options, string? error)
    {
        var dir = DirectoryPath.ParseAbsolute(path, Windows, PathOptions.None);

        dir.IsValidFor(options).ShouldBe(error is null);
        dir.IsValidFor(options, out string? errorMessage).ShouldBe(error is null);
        errorMessage.ShouldBe(error);
    }

    [TestMethod]
    [DataRow("/data /", PathOptions.PathFormatDependent, null)]
    [DataRow("/data /", PathOptions.NoTrailingSpaces, "Entry name ends with a space.")]
    [DataRow("/con/", PathOptions.NoUnfriendlyNames, null)]
    [DataRow("/a\u0001b/", PathOptions.None, null)]
    [DataRow("/a\u0001b/", PathOptions.NoControlCharacters, "Invalid control character '\u0001' in entry name 'a\u0001b'.")]
    public void UnixAbsoluteDirectory(string path, PathOptions options, string? error)
    {
        var dir = DirectoryPath.ParseAbsolute(path, Unix, PathOptions.None);

        dir.IsValidFor(options).ShouldBe(error is null);
        dir.IsValidFor(options, out string? errorMessage).ShouldBe(error is null);
        errorMessage.ShouldBe(error);
    }

    [TestMethod]
    [DataRow(@"..\data\file.txt", PathOptions.NoUnfriendlyNames, null)]
    [DataRow(@"..\data\file.txt", PathOptions.NoNavigation, "Invalid navigational path segment.")]
    [DataRow(@"\data\file.txt", PathOptions.NoNavigation, "Rooted relative paths are not allowed for this path.")]
    [DataRow(@"data\file.txt", PathOptions.NoNavigation, null)]
    public void WindowsRelativeFile(string path, PathOptions options, string? error)
    {
        var file = FilePath.ParseRelative(path, Windows, PathOptions.None);

        file.IsValidFor(options).ShouldBe(error is null);
        file.IsValidFor(options, out string? errorMessage).ShouldBe(error is null);
        errorMessage.ShouldBe(error);
    }

    [TestMethod]
    public void PathOptions_LeaveValidityBitsFree()
    {
        int reserved = (int)PathValidity.Basic | PathValidity.Computed;

        foreach (var value in Enum.GetValues<PathOptions>())
            ((int)value & reserved).ShouldBe(0, value.ToString());
    }

    [TestMethod]
    public void EmptyRelativeDirectory_ValidForEveryOption()
    {
        foreach (var format in new[] { Windows, Unix, Universal })
        {
            var dir = DirectoryPath.ParseRelative(".", format, PathOptions.None);

            foreach (var options in AllOptions)
                dir.IsValidFor(options).ShouldBeTrue();
        }
    }

    [TestMethod]
    public void ParsedPaths_MatchParsingAgain()
    {
        foreach (var (kind, format, s) in Paths)
        {
            TryParse(kind, s, format, PathOptions.None, out _, out string? error).ShouldBeTrue($"[{format}] {kind} '{s}': {error}");

            foreach (var parseOptions in ParseOptions)
            {
                if (!TryParse(kind, s, format, parseOptions, out var parsed, out _))
                    continue;

                int known = Impl(parsed).KnownValidity;

                // A fresh copy for each check, so that each one starts from what the parse proved.
                foreach (var options in AllOptions)
                    CheckMatchesParsingAgain(Copy(parsed, known), options);
            }
        }
    }

    [TestMethod]
    public void UnknownAndComputedPaths_MatchParsingAgain()
    {
        foreach (var path in GetParsedPaths(PathOptions.None).Concat(GetPathsThatCantBeParsed()))
        {
            foreach (var options in AllOptions)
                CheckMatchesParsingAgain(Copy(path, PathValidity.Unknown), options);

            var computed = Copy(path, PathValidity.Unknown);
            Impl(computed).ComputeValidity();

            foreach (var options in AllOptions)
                CheckMatchesParsingAgain(computed, options);
        }
    }

    [TestMethod]
    public void PathsThatCantBeParsed_InvalidForEveryOption()
    {
        foreach (var path in GetPathsThatCantBeParsed())
        {
            foreach (var options in AllOptions)
                path.IsValidFor(options).ShouldBeFalse($"{path} with {options}");
        }
    }

    [TestMethod]
    public void Parsed_AnswersParseOptionsWithoutComputing()
    {
        var windowsFile = FilePath.ParseAbsolute(@"C:\data\file.txt", Windows, PathOptions.NoUnfriendlyNames);

        windowsFile.IsValidFor(PathOptions.NoUnfriendlyNames).ShouldBeTrue();
        windowsFile.IsValidFor(PathOptions.None).ShouldBeTrue();
        windowsFile.IsValidFor(PathOptions.NoLeadingSpaces | PathOptions.NoTrailingDots).ShouldBeTrue();
        windowsFile.IsValidFor(PathOptions.PathFormatDependent).ShouldBeTrue();
        windowsFile.IsValidFor(PathOptions.NoUnfriendlyNames | PathOptions.NoNavigation | PathOptions.AllowEmptyDirectories).ShouldBeTrue();
        IsComputed(windowsFile).ShouldBeFalse();

        var unixDir = DirectoryPath.ParseRelative("data/files/", Unix, PathOptions.NoUnfriendlyNames);

        unixDir.IsValidFor(PathOptions.NoUnfriendlyNames).ShouldBeTrue();
        unixDir.IsValidFor(PathOptions.PathFormatDependent).ShouldBeTrue();
        IsComputed(unixDir).ShouldBeFalse();

        // Not proven by the parse, so it is computed.
        unixDir.IsValidFor(PathOptions.NoNavigation).ShouldBeTrue();
        IsComputed(unixDir).ShouldBeTrue();
    }

    [TestMethod]
    public void UnixParsedWithNone_AnswersReservedDeviceNamesWithoutComputing()
    {
        var dir = DirectoryPath.ParseAbsolute("/data/", Unix, PathOptions.None);

        dir.IsValidFor(PathOptions.NoReservedDeviceNames).ShouldBeTrue();
        dir.IsValidFor(PathOptions.PathFormatDependent).ShouldBeTrue();
        IsComputed(dir).ShouldBeFalse();
    }

    [TestMethod]
    public void Computed_StoresEveryBit()
    {
        var file = FilePath.ParseRelative("data /file.txt", Unix, PathOptions.None);

        file.IsValidFor(PathOptions.NoTrailingSpaces).ShouldBeFalse();
        IsComputed(file).ShouldBeTrue();

        int validity = Impl(file).KnownValidity;
        validity.ShouldBe((int)((PathValidity.Restrictions | PathValidity.Basic) & ~PathOptions.NoTrailingSpaces) | PathValidity.Computed);

        foreach (var options in AllOptions)
            file.IsValidFor(options);

        Impl(file).KnownValidity.ShouldBe(validity);
    }

    [TestMethod]
    public void DerivedPaths_InheritWhatIsKnown()
    {
        const PathOptions Friendly = PathOptions.NoUnfriendlyNames;

        var dir = DirectoryPath.ParseAbsolute(@"C:\data\files\", Windows, Friendly);
        var file = dir.CombineFile("file.txt", Friendly);
        var relativeDir = DirectoryPath.ParseRelative("a/b/", Universal, Friendly | PathOptions.NoNavigation);

        IPath[] derived = [
            file,
            file.ParentDirectory,
            file.RootDirectory,
            file.WithExtension(".log"),
            file.AddExtension(".bak"),
            file.AddMultiExtension(".tar.gz"),
            file.RemoveExtension(".TXT"),
            file.RemoveMultiExtension(".TXT"), // Same path as the line above, through the multi-extension member.
            dir.ParentDirectory!,
            dir.Combine(relativeDir),
            relativeDir.ParentDirectory!,
            relativeDir.CombineFile("c.txt", Friendly | PathOptions.NoNavigation),
            relativeDir.ParentDirectory!.ParentDirectory!,
        ];

        foreach (var path in derived)
        {
            path.IsValidFor(Friendly).ShouldBeTrue(path.ToString());
            IsComputed(path).ShouldBeFalse(path.ToString());
        }

        var relativeFile = (IRelativeFilePath)derived[^2];
        relativeFile.IsValidFor(Friendly | PathOptions.NoNavigation).ShouldBeTrue();
        IsComputed(relativeFile).ShouldBeFalse();

        // Combined with a part parsed with PathOptions.None, the result doesn't know the friendly flags.
        var combined = dir.CombineFile("file.txt", PathOptions.None);
        combined.IsValidFor(Friendly).ShouldBeTrue();
        IsComputed(combined).ShouldBeTrue();

        var unfriendly = dir.CombineFile("file.txt ", PathOptions.None);
        unfriendly.IsValidFor(Friendly, out string? error).ShouldBeFalse();
        error.ShouldBe("Entry name ends with a space.");
    }

    [TestMethod]
    public void DerivedPaths_KnowOnlyWhatIsTrue()
    {
        int count = 0;

        PathOptions[] parseOptionsList = [
            PathOptions.None,
            PathOptions.NoUnfriendlyNames,
            PathOptions.NoUnfriendlyNames | PathOptions.NoNavigation,
            PathOptions.PathFormatDependent,
        ];

        foreach (var parseOptions in parseOptionsList)
        {
            foreach (var path in GetParsedPaths(parseOptions).SelectMany(GetDerivedPaths))
            {
                var impl = Impl(path);
                int known = impl.KnownValidity;
                int computed = path.PathFormat.GetValidity(path.PathDisplay, impl.RootLength, path is IAbsolutePath, path is IFilePath);

                (known & ~computed).ShouldBe(0, path.ToString());

                foreach (var options in AllOptions)
                    CheckMatchesParsingAgain(Copy(path, known), options);

                count++;
            }
        }

        count.ShouldBeGreaterThan(1000);
    }

    [TestMethod]
    public void FileSystemPaths_MatchParsingAgain()
    {
        // Parsed again with NoUnfriendlyNames so that the paths built on it are known to be valid for those options.
        var currentDir = DirectoryPath.ParseAbsolute(DirectoryPath.GetCurrent().PathDisplay, PathOptions.NoUnfriendlyNames);
        var testDir = currentDir + DirectoryPath.ParseRelative("_test_isvalidfor");

        if (testDir.Exists)
            testDir.Delete(true);

        testDir.Create();

        try
        {
            testDir.CombineDirectory("sub").Create();
            testDir.CombineFile("file.txt").OpenStream(FileMode.CreateNew).Dispose();

            // Read as a file path, then found to be a directory.
            var info = testDir.GetInfo("sub", PathOptions.NoUnfriendlyNames);
            info.Path.ShouldBeAssignableTo<IAbsoluteDirectoryPath>();
            info.Path.IsValidFor(PathOptions.NoUnfriendlyNames).ShouldBeTrue();
            IsComputed(info.Path).ShouldBeFalse();

            IPath[] paths = [
                info.Path,
                .. testDir.GetChildEntries(),
                .. testDir.GetChildEntriesInfo().Select(i => i.Path),
                .. testDir.GetRelativeChildEntries(),
                .. testDir.CombineDirectory("sub").GetRelativeEntries(DirectoryPath.ParseRelative("../"), "*"),
            ];

            paths.Length.ShouldBe(9);

            foreach (var path in paths)
            {
                int known = Impl(path).KnownValidity;

                foreach (var options in AllOptions)
                    CheckMatchesParsingAgain(Copy(path, known), options);
            }
        }
        finally
        {
            testDir.Delete(true);
        }
    }

    private static IEnumerable<IPath> GetParsedPaths(PathOptions options)
    {
        foreach (var (kind, format, s) in Paths)
        {
            if (TryParse(kind, s, format, options, out var path, out _))
                yield return path;
        }
    }

    /// <summary>
    /// Gets paths that parsing never produces: names with control characters in the Windows formats (as the file system could return), and file names
    /// <c>.</c> and <c>..</c>.
    /// </summary>
    private static IEnumerable<IPath> GetPathsThatCantBeParsed()
    {
        yield return new IRelativeFilePath.Impl("a\u0001b", 0, Windows, PathValidity.Unknown);
        yield return new IAbsoluteDirectoryPath.Impl("C:\\a\u001f\\", 3, Windows, PathValidity.Unknown);
        yield return new IRelativeDirectoryPath.Impl("x/\u0002/", 0, Universal, PathValidity.Unknown);
        yield return new IRelativeFilePath.Impl(".", 0, Unix, PathValidity.Unknown);
        yield return new IAbsoluteFilePath.Impl(@"C:\dir\..", 3, Windows, PathValidity.Unknown);
    }

    private static IEnumerable<IPath> GetDerivedPaths(IPath path)
    {
        var parent = path.ParentDirectory;

        for (int i = 0; i < 3 && parent is not null; i++)
        {
            yield return parent;
            parent = parent.ParentDirectory;
        }

        if (path is IAbsolutePath absolutePath)
            yield return absolutePath.RootDirectory;

        if (path is IFilePath file)
        {
            foreach (string extension in new[] { ".txt", ".", "", ". ", ".tar.gz", ".gz", ".TXT", ". .gz" })
            {
                foreach (var options in new[] { PathOptions.None, PathOptions.NoUnfriendlyNames })
                {
                    if (TryGet(() => file.WithExtension(extension, options), out var withExtension))
                        yield return withExtension;

                    if (TryGet(() => file.AddExtension(extension, options), out var addedExtension))
                        yield return addedExtension;

                    if (TryGet(() => file.AddMultiExtension(extension, options), out var addedMultiExtension))
                        yield return addedMultiExtension;

                    if (TryGet(() => file.RemoveExtension(extension, options), out var removedExtension))
                        yield return removedExtension;

                    if (TryGet(() => file.RemoveMultiExtension(extension, options), out var removedMultiExtension))
                        yield return removedMultiExtension;
                }
            }
        }

        if (path is IDirectoryPath dir)
        {
            foreach (var part in GetParsedPaths(PathOptions.None).Concat(GetParsedPaths(PathOptions.NoUnfriendlyNames)).OfType<IRelativePath>())
            {
                if (TryGet(() => dir.Combine(part), out var combined))
                    yield return combined;
            }

            foreach (var options in new[] { PathOptions.None, PathOptions.NoUnfriendlyNames, PathOptions.NoNavigation })
            {
                foreach (string s in new[] { "x", "x ", "con", "../y.", " z" })
                {
                    if (TryGet(() => dir.CombineFile(s, options), out var combinedFile))
                        yield return combinedFile;

                    if (TryGet(() => dir.CombineDirectory(s, options), out var combinedDir))
                        yield return combinedDir;
                }
            }
        }
    }

    private static void CheckMatchesParsingAgain(IPath path, PathOptions options)
    {
        bool expected = TryParse(GetKind(path), path.PathDisplay, path.PathFormat, options, out _, out string? expectedError);
        string context = $"{path} with {options}";

        int known = Impl(path).KnownValidity;
        path.IsValidFor(options).ShouldBe(expected, context);

        Copy(path, known).IsValidFor(options, out string? error).ShouldBe(expected, context);
        error.ShouldBe(expectedError, context);
    }

    private static bool TryParse(Kind kind, string s, PathFormat format, PathOptions options, [NotNullWhen(true)] out IPath? path, out string? error)
    {
        bool success;

        switch (kind)
        {
            case Kind.AbsoluteFile:
                success = FilePath.TryParseAbsolute(s, format, options, out var absoluteFile, out error);
                path = absoluteFile;
                break;
            case Kind.AbsoluteDirectory:
                success = DirectoryPath.TryParseAbsolute(s, format, options, out var absoluteDirectory, out error);
                path = absoluteDirectory;
                break;
            case Kind.RelativeFile:
                success = FilePath.TryParseRelative(s, format, options, out var relativeFile, out error);
                path = relativeFile;
                break;
            default:
                success = DirectoryPath.TryParseRelative(s, format, options, out var relativeDirectory, out error);
                path = relativeDirectory;
                break;
        }

        return success;
    }

    private static Kind GetKind(IPath path) => path switch {
        IAbsoluteFilePath => Kind.AbsoluteFile,
        IAbsoluteDirectoryPath => Kind.AbsoluteDirectory,
        IRelativeFilePath => Kind.RelativeFile,
        _ => Kind.RelativeDirectory,
    };

    private static IPath Copy(IPath path, int validity)
    {
        int rootLength = Impl(path).RootLength;

        return path switch {
            IAbsoluteFilePath => new IAbsoluteFilePath.Impl(path.PathDisplay, rootLength, path.PathFormat, validity),
            IAbsoluteDirectoryPath => new IAbsoluteDirectoryPath.Impl(path.PathDisplay, rootLength, path.PathFormat, validity),
            IRelativeFilePath => new IRelativeFilePath.Impl(path.PathDisplay, rootLength, path.PathFormat, validity),
            _ => new IRelativeDirectoryPath.Impl(path.PathDisplay, rootLength, path.PathFormat, validity),
        };
    }

    private static IPath.Impl Impl(IPath path) => (IPath.Impl)path;

    private static bool IsComputed(IPath path) => (Impl(path).KnownValidity & PathValidity.Computed) is not 0;

    private static PathOptions GetOptions(int combination)
    {
        var options = PathOptions.None;

        for (int i = 0; i < OptionFlags.Length; i++)
        {
            if ((combination & (1 << i)) is not 0)
                options |= OptionFlags[i];
        }

        return options;
    }
}
