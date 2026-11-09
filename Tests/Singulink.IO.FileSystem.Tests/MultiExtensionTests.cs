namespace Singulink.IO.FileSystem.Tests;

[PrefixTestClass]
public class MultiExtensionTests
{
    private const string ShapeError =
        "it must be a lone dot '.' or start with a dot and have a character other than a dot after every dot (e.g. \".gz\" or \".tar.gz\").";

    #region HasMultiExtension

    [TestMethod]
    [DataRow("Windows", @"C:\dir\backup.tar.gz", ".tar.gz", true)]
    [DataRow("Windows", @"C:\dir\backup.tar.gz", ".gz", true)]
    [DataRow("Windows", @"C:\dir\backup.tar.gz", ".TAR.GZ", true)]
    [DataRow("Windows", @"C:\dir\backup.tar.gz", ".tar", false)]
    [DataRow("Windows", @"C:\dir\backup.tar.gz", ".backup.tar.gz", false)]
    [DataRow("Windows", @"\\server\share\Makefile", ".e", false)]
    [DataRow("Unix", "/dir/photo.JPG", ".jpg", true)]
    [DataRow("Unix", "/dir/photo.jpg", ".JPG", true)]
    [DataRow("Unix", "/dir/x.tar.gz", ".ar.gz", false)]
    [DataRow("Unix", "/dir/a?b.x?y", ".x?y", true)]
    [DataRow("Unix", "/dir/data.", ".", true)]
    [DataRow("Unix", "/dir/data.txt", ".", false)]
    [DataRow("Unix", "/.tar.gz", ".tar.gz", true)]
    public void HasMultiExtension_Absolute(string formatName, string path, string extension, bool expected)
    {
        var file = FilePath.ParseAbsolute(path, GetFormat(formatName), PathOptions.None);
        file.HasMultiExtension(extension).ShouldBe(expected);
    }

    [TestMethod]
    [DataRow("Windows", @"dir\backup.Tar.Gz", ".tar.gz", true)]
    [DataRow("Windows", "backup.tar.gz", ".tar.bz2", false)]
    [DataRow("Windows", @"\a\.gitignore", ".gitignore", true)]
    [DataRow("Unix", "../backup.tar.gz", ".tar.gz", true)]
    [DataRow("Unix", "photo.jpeg", ".jpg", false)]
    [DataRow("Unix", "Makefile", ".Makefile", false)]
    [DataRow("Universal", "a/.gitignore", ".gitignore", true)]
    [DataRow("Universal", "a/b.TAR.gz", ".tar.GZ", true)]
    [DataRow("Universal", "a/b.gz", ".tar.gz", false)]
    public void HasMultiExtension_Relative(string formatName, string path, string extension, bool expected)
    {
        var file = FilePath.ParseRelative(path, GetFormat(formatName), PathOptions.None);
        file.HasMultiExtension(extension).ShouldBe(expected);
    }

    [TestMethod]
    [DataRow("Windows", "gz")]
    [DataRow("Windows", "tar.gz")]
    [DataRow("Windows", "..gz")]
    [DataRow("Windows", ".tar..gz")]
    [DataRow("Windows", ".tar.")]
    [DataRow("Unix", "..")]
    [DataRow("Unix", "...")]
    [DataRow("Universal", ".gz.")]
    public void HasMultiExtension_BadShape_Throws(string formatName, string extension)
    {
        var file = FilePath.ParseRelative("backup.tar.gz", GetFormat(formatName), PathOptions.None);

        var ex = Should.Throw<ArgumentException>(() => file.HasMultiExtension(extension));
        ex.Message.ShouldBe($"Invalid extension '{extension}': {ShapeError} (Parameter 'extension')");
        ex.ParamName.ShouldBe("extension");
    }

    [TestMethod]
    [DataRow("Windows", ".a?b", "Invalid character '?' in entry name '.a?b'. Invalid characters include: < > : \" | ? * / \\")]
    [DataRow("Windows", ".tar.g:z", "Invalid character ':' in entry name '.g:z'. Invalid characters include: < > : \" | ? * / \\")]
    [DataRow("Windows", ".a\\b", "Invalid character '\\' in entry name '.a\\b'.")]
    [DataRow("Windows", ".a\u0001", "Invalid control character '\u0001' in entry name '.a\u0001'.")]
    [DataRow("Unix", ".tar/.gz", "Invalid character '/' in entry name '.tar/'.")]
    [DataRow("Universal", ".a*b", "Invalid character '*' in entry name '.a*b'. Invalid characters include: < > : \" | ? * / \\")]
    public void HasMultiExtension_InvalidCharacter_Throws(string formatName, string extension, string error)
    {
        var file = FilePath.ParseRelative("backup.tar.gz", GetFormat(formatName), PathOptions.None);

        var ex = Should.Throw<ArgumentException>(() => file.HasMultiExtension(extension));
        ex.Message.ShouldBe($"Invalid extension '{extension}': {error} (Parameter 'extension')");
        ex.ParamName.ShouldBe("extension");
    }

    [TestMethod]
    public void HasMultiExtension_Null_Throws()
    {
        var file = FilePath.ParseRelative("backup.tar.gz", PathFormat.Unix);
        Should.Throw<ArgumentNullException>(() => file.HasMultiExtension(null!));
    }

    [TestMethod]
    public void HasMultiExtension_Empty_Throws()
    {
        var file = FilePath.ParseRelative("Makefile", PathFormat.Unix);

        var ex = Should.Throw<ArgumentException>(() => file.HasMultiExtension(""));
        ex.Message.ShouldBe("Invalid extension '': it can't be empty. (Parameter 'extension')");
    }

    #endregion

    #region GetMultiExtension

    [TestMethod]
    [DataRow("Windows", @"C:\out\backup.tar.gz", ".tar.gz")]
    [DataRow("Windows", @"C:\out\BACKUP.TAR.GZ", ".tar.gz")]
    [DataRow("Windows", @"C:\out\backup.tar.bz2", ".tar.bz2")]
    [DataRow("Windows", @"C:\out\photo.jpg", ".jpg")]
    [DataRow("Unix", "/out/photo.JPG", ".JPG")]
    [DataRow("Unix", "/out/backup.gz", ".gz")]
    [DataRow("Unix", "/out/Makefile", "")]
    [DataRow("Unix", "/out/.tar.gz", ".tar.gz")]
    [DataRow("Unix", "/out/.gitignore", ".gitignore")]
    [DataRow("Unix", "/out/data.", ".")]
    public void GetMultiExtension_Absolute(string formatName, string path, string expected)
    {
        var file = FilePath.ParseAbsolute(path, GetFormat(formatName), PathOptions.None);
        file.GetMultiExtension(".tar.gz", ".tar.bz2").ShouldBe(expected);
    }

    [TestMethod]
    [DataRow("Windows", @"..\backup.tar.gz", ".tar.gz")]
    [DataRow("Unix", "backup.Tar.Bz2", ".tar.bz2")]
    [DataRow("Universal", "a/photo.jpg", ".jpg")]
    [DataRow("Universal", "a/x.tar.gz.bak", ".bak")]
    [DataRow("Universal", "Makefile", "")]
    public void GetMultiExtension_Relative(string formatName, string path, string expected)
    {
        var file = FilePath.ParseRelative(path, GetFormat(formatName), PathOptions.None);
        file.GetMultiExtension(".tar.gz", ".tar.bz2").ShouldBe(expected);
    }

    [TestMethod]
    public void GetMultiExtension_OverlappingKnownExtensions_GivesLongest()
    {
        var archive = FilePath.ParseRelative("backup.tar.gz", PathFormat.Universal);
        var gz = FilePath.ParseRelative("log.gz", PathFormat.Universal);

        archive.GetMultiExtension(".gz", ".tar.gz").ShouldBe(".tar.gz");
        archive.GetMultiExtension(".tar.gz", ".gz").ShouldBe(".tar.gz");
        archive.GetMultiExtension(".gz", ".tar.gz", ".backup.tar.gz").ShouldBe(".tar.gz");
        gz.GetMultiExtension(".gz", ".tar.gz").ShouldBe(".gz");
        gz.GetMultiExtension(".tar.gz").ShouldBe(".gz");
    }

    [TestMethod]
    public void GetMultiExtension_SameLengthMatches_GivesFirst()
    {
        var file = FilePath.ParseAbsolute("/backup.tar.gz", PathFormat.Unix);

        file.GetMultiExtension(".TAR.GZ", ".tar.gz").ShouldBe(".TAR.GZ");
        file.GetMultiExtension(".tar.gz", ".TAR.GZ").ShouldBe(".tar.gz");
    }

    [TestMethod]
    public void GetMultiExtension_NoKnownExtensions_GivesExtension()
    {
        FilePath.ParseRelative("backup.tar.gz", PathFormat.Windows).GetMultiExtension().ShouldBe(".gz");
        FilePath.ParseRelative("Makefile", PathFormat.Windows).GetMultiExtension([]).ShouldBe("");
    }

    [TestMethod]
    [DataRow("gz")]
    [DataRow(".tar..gz")]
    [DataRow(".gz.")]
    public void GetMultiExtension_BadShape_Throws(string bad)
    {
        var file = FilePath.ParseRelative("backup.tar.gz", PathFormat.Unix);

        // Every known extension is checked, including ones after a match.
        var ex = Should.Throw<ArgumentException>(() => file.GetMultiExtension(".tar.gz", bad));
        ex.Message.ShouldBe($"Invalid extension '{bad}': {ShapeError} (Parameter 'knownExtensions')");
        ex.ParamName.ShouldBe("knownExtensions");
    }

    [TestMethod]
    public void GetMultiExtension_InvalidCharacter_Throws()
    {
        var file = FilePath.ParseRelative("backup.tar.gz", PathFormat.Windows);

        var ex = Should.Throw<ArgumentException>(() => file.GetMultiExtension(".a|b"));
        ex.Message.ShouldBe(
            "Invalid extension '.a|b': Invalid character '|' in entry name '.a|b'. Invalid characters include: < > : \" | ? * / \\ " +
            "(Parameter 'knownExtensions')");

        // Valid in the Unix format.
        FilePath.ParseRelative("backup.a|b", PathFormat.Unix).GetMultiExtension(".a|b").ShouldBe(".a|b");
    }

    [TestMethod]
    public void GetMultiExtension_NullKnownExtension_Throws()
    {
        var file = FilePath.ParseRelative("backup.tar.gz", PathFormat.Unix);

        var ex = Should.Throw<ArgumentException>(() => file.GetMultiExtension(".gz", null!));
        ex.Message.ShouldBe("Known extensions can't contain null. (Parameter 'knownExtensions')");
        ex.ParamName.ShouldBe("knownExtensions");
    }

    [TestMethod]
    public void GetMultiExtension_EmptyKnownExtension_Throws()
    {
        var file = FilePath.ParseRelative("Makefile", PathFormat.Windows);

        var ex = Should.Throw<ArgumentException>(() => file.GetMultiExtension(".gz", ""));
        ex.Message.ShouldBe("Invalid extension '': it can't be empty. (Parameter 'knownExtensions')");
    }

    #endregion

    #region RemoveMultiExtension

    [TestMethod]
    [DataRow("Windows", @"C:\out\backup.tar.gz", ".tar.gz", @"C:\out\backup")]
    [DataRow("Windows", @"C:\backup.tar.gz", ".gz", @"C:\backup.tar")]
    [DataRow("Windows", @"\\server\share\BACKUP.TAR.GZ", ".tar.gz", @"\\server\share\BACKUP")]
    [DataRow("Windows", @"C:\photo.jpg", ".JPG", @"C:\photo")]
    [DataRow("Unix", "/out/photo.JPG", ".jpg", "/out/photo")]
    [DataRow("Unix", "/out/my.report.v2.pdf", ".v2.pdf", "/out/my.report")]
    [DataRow("Unix", "/a/.config.json", ".json", "/a/.config")]
    public void RemoveMultiExtension_Absolute(string formatName, string path, string extension, string expected)
    {
        IAbsoluteFilePath file = FilePath.ParseAbsolute(path, GetFormat(formatName));
        IAbsoluteFilePath result = file.RemoveMultiExtension(extension);

        result.PathDisplay.ShouldBe(expected);
        result.ShouldBe(FilePath.ParseAbsolute(expected, GetFormat(formatName)));
    }

    [TestMethod]
    [DataRow("Windows", @"..\out\backup.tar.gz", ".tar.gz", @"..\out\backup")]
    [DataRow("Windows", @"\backup.Tar.Gz", ".tar.gz", @"\backup")]
    [DataRow("Unix", "backup.tar.gz", ".TAR.gz", "backup")]
    [DataRow("Unix", "a/b.c.d", ".c.d", "a/b")]
    [DataRow("Universal", "a/backup.tar.gz", ".gz", "a/backup.tar")]
    [DataRow("Universal", "x.tar.gz.bak", ".gz.bak", "x.tar")]
    public void RemoveMultiExtension_Relative(string formatName, string path, string extension, string expected)
    {
        IRelativeFilePath file = FilePath.ParseRelative(path, GetFormat(formatName));
        IRelativeFilePath result = file.RemoveMultiExtension(extension);

        result.PathDisplay.ShouldBe(expected);
        result.ShouldBe(FilePath.ParseRelative(expected, GetFormat(formatName)));
    }

    [TestMethod]
    public void RemoveMultiExtension_ThroughFilePathInterface()
    {
        IFilePath absolute = FilePath.ParseAbsolute(@"C:\backup.tar.gz", PathFormat.Windows);
        IFilePath relative = FilePath.ParseRelative("backup.tar.gz", PathFormat.Unix);

        absolute.RemoveMultiExtension(".tar.gz").ShouldBeAssignableTo<IAbsoluteFilePath>().PathDisplay.ShouldBe(@"C:\backup");
        relative.RemoveMultiExtension(".tar.gz").ShouldBeAssignableTo<IRelativeFilePath>().PathDisplay.ShouldBe("backup");
    }

    [TestMethod]
    public void RemoveMultiExtension_Empty_ReturnsSamePath()
    {
        var absolute = FilePath.ParseAbsolute("/out/Makefile", PathFormat.Unix);
        var relative = FilePath.ParseRelative("backup.tar.gz", PathFormat.Windows);

        absolute.RemoveMultiExtension("").ShouldBeSameAs(absolute);
        relative.RemoveMultiExtension("").ShouldBeSameAs(relative);
    }

    [TestMethod]
    [DataRow("Unix", "data.", "data")]
    [DataRow("Windows", @"dir\data.", @"dir\data")]
    public void RemoveMultiExtension_LoneDot(string formatName, string path, string expected)
    {
        var file = FilePath.ParseRelative(path, GetFormat(formatName), PathOptions.None);
        file.RemoveMultiExtension(".", PathOptions.None).PathDisplay.ShouldBe(expected);
    }

    [TestMethod]
    [DataRow("Windows", @"C:\out\backup.tar.gz", ".tar.bz2", "backup.tar.gz")]
    [DataRow("Windows", @"C:\out\backup.tar.gz", ".tar", "backup.tar.gz")]
    [DataRow("Unix", "/out/photo.jpeg", ".jpg", "photo.jpeg")]
    [DataRow("Unix", "/out/Makefile", ".Makefile", "Makefile")]
    public void RemoveMultiExtension_NotAtEnd_Throws(string formatName, string path, string extension, string name)
    {
        var file = FilePath.ParseAbsolute(path, GetFormat(formatName));

        var ex = Should.Throw<ArgumentException>(() => file.RemoveMultiExtension(extension));
        ex.Message.ShouldBe($"File name '{name}' does not end with the extension '{extension}'. (Parameter 'extension')");
        ex.ParamName.ShouldBe("extension");
    }

    [TestMethod]
    [DataRow("Windows", @"out\.tar.gz", ".tar.gz")]
    [DataRow("Unix", ".tar.gz", ".TAR.GZ")]
    [DataRow("Universal", "a/.gitignore", ".gitignore")]
    public void RemoveMultiExtension_WholeName_Throws(string formatName, string path, string extension)
    {
        var file = FilePath.ParseRelative(path, GetFormat(formatName));

        var ex = Should.Throw<ArgumentException>(() => file.RemoveMultiExtension(extension));
        ex.Message.ShouldBe($"Extension '{extension}' is the whole file name, so removing it leaves no name. (Parameter 'extension')");
        ex.ParamName.ShouldBe("extension");
    }

    [TestMethod]
    [DataRow("gz")]
    [DataRow("..gz")]
    [DataRow(".tar..gz")]
    [DataRow(".gz.")]
    [DataRow("..")]
    public void RemoveMultiExtension_BadShape_Throws(string extension)
    {
        var file = FilePath.ParseAbsolute(@"C:\backup.tar.gz", PathFormat.Windows);

        var ex = Should.Throw<ArgumentException>(() => file.RemoveMultiExtension(extension));
        ex.Message.ShouldBe($"Invalid extension '{extension}': {ShapeError} (Parameter 'extension')");
        ex.ParamName.ShouldBe("extension");
    }

    [TestMethod]
    public void RemoveMultiExtension_InvalidCharacter_Throws()
    {
        var file = FilePath.ParseRelative("backup.tar.gz", PathFormat.Universal);

        var ex = Should.Throw<ArgumentException>(() => file.RemoveMultiExtension(".g<z"));
        ex.Message.ShouldBe(
            "Invalid extension '.g<z': Invalid character '<' in entry name '.g<z'. Invalid characters include: < > : \" | ? * / \\ " +
            "(Parameter 'extension')");
    }

    [TestMethod]
    [DataRow("Windows", @"C:\out\con.tar.gz", ".tar.gz", "Invalid reserved device name in entry name 'con'.")]
    [DataRow("Windows", @"C:\out\x .tar.gz", ".tar.gz", "Entry name ends with a space.")]
    [DataRow("Unix", "/out/x..gz", ".gz", "Entry name ends with a dot.")]
    [DataRow("Unix", "/out/x .gz", ".GZ", "Entry name ends with a space.")]
    public void RemoveMultiExtension_InvalidRemainingName_Throws(string formatName, string path, string extension, string error)
    {
        var format = GetFormat(formatName);
        var file = FilePath.ParseAbsolute(path, format, PathOptions.None);

        var ex = Should.Throw<ArgumentException>(() => file.RemoveMultiExtension(extension));
        ex.Message.ShouldBe($"Invalid new file name: {error} (Parameter 'extension')");
        ex.ParamName.ShouldBe("extension");

        // Allowed with options that don't apply the check.
        file.RemoveMultiExtension(extension, PathOptions.None).Name.ShouldBe(file.Name[..^extension.Length]);
    }

    [TestMethod]
    [DataRow("Unix", "..txt", ".txt", ".")]
    [DataRow("Unix", "dir/...tar.gz", ".tar.gz", "..")]
    [DataRow("Windows", @"..\..TXT", ".txt", ".")]
    [DataRow("Universal", "a/...gz", ".gz", "..")]
    public void RemoveMultiExtension_NavigationalName_Throws(string formatName, string path, string extension, string newName)
    {
        var file = FilePath.ParseRelative(path, GetFormat(formatName), PathOptions.None);

        var ex = Should.Throw<ArgumentException>(() => file.RemoveMultiExtension(extension, PathOptions.None));
        ex.Message.ShouldBe($"Invalid new file name: '{newName}' is a navigational path segment. (Parameter 'extension')");
        ex.ParamName.ShouldBe("extension");
    }

    [TestMethod]
    [DataRow("Unix", "/a/..txt", ".txt", ".")]
    [DataRow("Windows", @"\\server\share\...gz", ".gz", "..")]
    public void RemoveMultiExtension_NavigationalName_Absolute_Throws(string formatName, string path, string extension, string newName)
    {
        var file = FilePath.ParseAbsolute(path, GetFormat(formatName), PathOptions.None);

        var ex = Should.Throw<ArgumentException>(() => file.RemoveMultiExtension(extension, PathOptions.None));
        ex.Message.ShouldBe($"Invalid new file name: '{newName}' is a navigational path segment. (Parameter 'extension')");
    }

    [TestMethod]
    public void RemoveMultiExtension_Null_Throws()
    {
        var file = FilePath.ParseRelative("backup.tar.gz", PathFormat.Unix);
        Should.Throw<ArgumentNullException>(() => file.RemoveMultiExtension(null!));
    }

    [TestMethod]
    public void RemoveMultiExtension_OfGetMultiExtension()
    {
        string[] known = [".tar.gz", ".tar.bz2"];

        IRelativeFilePath Strip(string path)
        {
            var file = FilePath.ParseRelative(path, PathFormat.Universal);
            return file.RemoveMultiExtension(file.GetMultiExtension(known));
        }

        Strip("a/backup.tar.gz").PathDisplay.ShouldBe("a/backup");
        Strip("a/backup.TAR.BZ2").PathDisplay.ShouldBe("a/backup");
        Strip("a/photo.jpg").PathDisplay.ShouldBe("a/photo");
        Strip("a/Makefile").PathDisplay.ShouldBe("a/Makefile");

        // A name that is all extension can't lose it.
        Should.Throw<ArgumentException>(() => Strip("a/.gitignore"));
        Should.Throw<ArgumentException>(() => Strip("a/.tar.gz"));
    }

    #endregion

    #region AddMultiExtension

    [TestMethod]
    [DataRow("Windows", @"C:\out\backup", ".tar.gz", @"C:\out\backup.tar.gz")]
    [DataRow("Windows", @"\\server\share\backup.tar", ".gz", @"\\server\share\backup.tar.gz")]
    [DataRow("Unix", "/out/backup.tar.gz", ".tar.gz", "/out/backup.tar.gz.tar.gz")]
    [DataRow("Unix", "/out/report", ".v2.PDF", "/out/report.v2.PDF")]
    public void AddMultiExtension_Absolute(string formatName, string path, string extension, string expected)
    {
        IAbsoluteFilePath file = FilePath.ParseAbsolute(path, GetFormat(formatName));
        file.AddMultiExtension(extension).PathDisplay.ShouldBe(expected);
    }

    [TestMethod]
    [DataRow("Windows", @"..\backup", ".tar.gz", @"..\backup.tar.gz")]
    [DataRow("Unix", "a/backup", ".tar.gz", "a/backup.tar.gz")]
    [DataRow("Universal", "backup", ".a.b.c", "backup.a.b.c")]
    public void AddMultiExtension_Relative(string formatName, string path, string extension, string expected)
    {
        IRelativeFilePath file = FilePath.ParseRelative(path, GetFormat(formatName));
        file.AddMultiExtension(extension).PathDisplay.ShouldBe(expected);
    }

    [TestMethod]
    [DataRow("gz")]
    [DataRow("..gz")]
    [DataRow(".tar..gz")]
    [DataRow(".tar.")]
    [DataRow("..")]
    public void AddMultiExtension_BadShape_Throws(string extension)
    {
        var file = FilePath.ParseRelative("backup", PathFormat.Unix);

        var ex = Should.Throw<ArgumentException>(() => file.AddMultiExtension(extension, PathOptions.None));
        ex.Message.ShouldBe($"Invalid extension '{extension}': {ShapeError} (Parameter 'extension')");
        ex.ParamName.ShouldBe("extension");
    }

    [TestMethod]
    public void AddMultiExtension_EmptyOnePartAndNewNameErrors()
    {
        var file = FilePath.ParseRelative("data", PathFormat.Windows);

        file.AddMultiExtension(null).ShouldBeSameAs(file);
        file.AddMultiExtension("").ShouldBeSameAs(file);
        file.AddMultiExtension(".txt").PathDisplay.ShouldBe("data.txt");
        file.AddMultiExtension(".", PathOptions.None).PathDisplay.ShouldBe("data.");
        file.AddMultiExtension(". ", PathOptions.None).PathDisplay.ShouldBe("data. ");

        var ex = Should.Throw<ArgumentException>(() => file.AddMultiExtension("."));
        ex.Message.ShouldBe("Invalid new file name: Entry name ends with a dot. (Parameter 'extension')");

        ex = Should.Throw<ArgumentException>(() => file.AddMultiExtension(".tar.g?z"));
        ex.Message.ShouldBe(
            "Invalid new file name: Invalid character '?' in entry name 'data.tar.g?z'. Invalid characters include: < > : \" | ? * / \\ " +
            "(Parameter 'extension')");
    }

    #endregion

    #region Guarantees

    [TestMethod]
    public void AddMultiExtension_ThenHasGetAndRemove()
    {
        string[] extensions = [".gz", ".tar.gz", ".TAR.GZ", ".a.b.c", ". .x", "."];

        foreach (var format in Formats)
        {
            foreach (string name in new[] { "backup", "backup.tar", ".config", "my.report.v2" })
            {
                string absolute = format == PathFormat.Windows ? $@"C:\dir\{name}" : $"/dir/{name}";

                IFilePath[] files = format == PathFormat.Universal ?
                    [FilePath.ParseRelative($"dir/{name}", format)] :
                    [FilePath.ParseRelative(name, format), FilePath.ParseAbsolute(absolute, format)];

                foreach (var file in files)
                {
                    foreach (string extension in extensions)
                    {
                        string context = $"{file} + '{extension}'";
                        var added = file.AddMultiExtension(extension, PathOptions.None);

                        added.HasMultiExtension(extension).ShouldBeTrue(context);
                        added.HasMultiExtension(extension.ToUpperInvariant()).ShouldBeTrue(context);
                        added.GetMultiExtension(extension).ShouldBe(extension, context);
                        added.RemoveMultiExtension(extension, PathOptions.None).ShouldBe(file, context);
                        added.RemoveMultiExtension(extension.ToLowerInvariant(), PathOptions.None).ShouldBe(file, context);
                    }
                }
            }
        }
    }

    #endregion

    #region IsValidMultiExtension

    [TestMethod]
    [DataRow(".gz", true, true)]
    [DataRow(".tar.gz", false, true)]
    [DataRow(".a.b.c", false, true)]
    [DataRow(".", true, true)]
    [DataRow(". ", true, true)]
    [DataRow(". .gz", false, true)]
    [DataRow("..", false, false)]
    [DataRow("..gz", false, false)]
    [DataRow(".tar.", false, false)]
    [DataRow(".tar..gz", false, false)]
    [DataRow("gz", false, false)]
    [DataRow("a.gz", false, false)]
    public void IsValidExtension_SingleVsMulti(string extension, bool single, bool multi)
    {
        foreach (var format in Formats)
        {
            format.IsValidExtension(extension, PathOptions.None).ShouldBe(single, format.ToString());
            format.IsValidMultiExtension(extension, PathOptions.None).ShouldBe(multi, format.ToString());
        }
    }

    [TestMethod]
    public void IsValidMultiExtension_Empty_ValidEitherWay()
    {
        foreach (var format in Formats)
        {
            format.IsValidExtension("").ShouldBeTrue();
            format.IsValidMultiExtension("").ShouldBeTrue();

            foreach (var options in new[] { PathOptions.None, PathOptions.NoUnfriendlyNames, PathOptions.PathFormatDependent })
            {
                format.IsValidExtension("", options).ShouldBeTrue();
                format.IsValidMultiExtension("", options).ShouldBeTrue();
            }
        }
    }

    [TestMethod]
    public void IsValidMultiExtension_DefaultOptions()
    {
        var format = PathFormat.Windows;

        // Without options, PathOptions.NoUnfriendlyNames applies, as in IsValidExtension.
        format.IsValidMultiExtension(".tar .gz").ShouldBeFalse();
        format.IsValidMultiExtension(".tar .gz", PathOptions.NoUnfriendlyNames).ShouldBeFalse();
        format.IsValidMultiExtension(".tar .gz", PathOptions.None).ShouldBeTrue();
    }

    [TestMethod]
    [DataRow("Unix", ".tar .gz", PathOptions.None, true)]
    [DataRow("Unix", ".tar .gz", PathOptions.NoTrailingSpaces, false)]
    [DataRow("Windows", ".tar .gz", PathOptions.NoUnfriendlyNames, false)]
    [DataRow("Universal", ".tar.gz ", PathOptions.NoTrailingSpaces, false)]
    [DataRow("Windows", ". tar. gz", PathOptions.NoUnfriendlyNames, true)]
    [DataRow("Windows", ".con.aux.lpt1", PathOptions.NoUnfriendlyNames, true)]
    [DataRow("Unix", ".a\u0001.gz", PathOptions.None, true)]
    [DataRow("Unix", ".a\u0001.gz", PathOptions.NoControlCharacters, false)]
    [DataRow("Windows", ".a\u0001.gz", PathOptions.None, false)]
    [DataRow("Unix", ".gz.a?b", PathOptions.NoUnfriendlyNames, true)]
    [DataRow("Windows", ".gz.a?b", PathOptions.None, false)]
    [DataRow("Universal", ".gz.a?b", PathOptions.None, false)]
    [DataRow("Unix", ".tar/.gz", PathOptions.None, false)]
    [DataRow("Windows", ".tar\\.gz", PathOptions.None, false)]
    [DataRow("Unix", ".tar\0.gz", PathOptions.None, false)]
    [DataRow("Windows", ".", PathOptions.NoUnfriendlyNames, true)]
    [DataRow("Unix", ".", PathOptions.PathFormatDependent, true)]
    [DataRow("Windows", ".", PathOptions.PathFormatDependent, true)]
    [DataRow("Universal", ".tar. gz", PathOptions.PathFormatDependent, true)]
    public void IsValidMultiExtension_PartRules(string formatName, string extension, PathOptions options, bool expected)
    {
        GetFormat(formatName).IsValidMultiExtension(extension, options).ShouldBe(expected);
    }

    [TestMethod]
    public void IsValidMultiExtension_PathFormatDependent_MatchesFormatOptions()
    {
        string[] extensions = [
            "", ".", ". ", ".gz", ".gz ", ". gz", ".con", ".a?b", ".a\u0001", ".tar.gz", ".tar .gz", ". tar. gz", ".con.aux", ".x.a\u0001", "..", ".tar.",
        ];

        (PathFormat Format, PathOptions Same)[] cases = [
            (PathFormat.Windows, PathOptions.NoUnfriendlyNames),
            (PathFormat.Universal, PathOptions.NoUnfriendlyNames),
            (PathFormat.Unix, PathOptions.None),
        ];

        foreach (var (format, same) in cases)
        {
            foreach (string extension in extensions)
            {
                string context = $"[{format}] '{extension}'";

                format.IsValidExtension(extension, PathOptions.PathFormatDependent).ShouldBe(format.IsValidExtension(extension, same), context);
                format.IsValidMultiExtension(extension, PathOptions.PathFormatDependent).ShouldBe(format.IsValidMultiExtension(extension, same), context);
            }
        }
    }

    [TestMethod]
    public void IsValidMultiExtension_EachPartAsSingle()
    {
        string[] extensions = [".tar.gz", ".tar .gz", ". tar. gz", ".con.aux", ".a\u0001.gz", ".gz.a?b", ".a.b|c", ".x. ", ".a.b.c"];
        PathOptions[] optionsList = [
            PathOptions.None,
            PathOptions.NoUnfriendlyNames,
            PathOptions.NoTrailingSpaces,
            PathOptions.NoControlCharacters,
            PathOptions.PathFormatDependent,
            PathOptions.NoReservedDeviceNames | PathOptions.NoLeadingSpaces,
        ];

        foreach (var format in Formats)
        {
            foreach (var options in optionsList)
            {
                foreach (string extension in extensions)
                {
                    bool partsValid = extension.Split('.')[1..].All(p => format.IsValidExtension($".{p}", options));
                    format.IsValidMultiExtension(extension, options).ShouldBe(partsValid, $"[{format}] '{extension}' with {options}");
                }
            }
        }
    }

    [TestMethod]
    public void IsValidMultiExtension_AgreesWithMembers()
    {
        string[] extensions = [
            ".gz", ".tar.gz", ".A.B.C", ".", ". ", ". .gz", "..", "..gz", ".tar.", ".tar..gz", "gz", "a.gz",
            ".a?b", ".tar.g:z", ".a/b", ".a\\b", ".a\u0001", ".a\0b", ".x|y", ".tar.gz ",
        ];

        foreach (var format in Formats)
        {
            var file = FilePath.ParseRelative("file.tar.gz", format);

            foreach (string extension in extensions)
            {
                string context = $"[{format}] '{extension}'";
                bool valid = format.IsValidMultiExtension(extension, PathOptions.None);

                CheckAgrees(() => file.HasMultiExtension(extension));
                CheckAgrees(() => file.GetMultiExtension(extension));
                CheckAgrees(() => file.RemoveMultiExtension(extension, PathOptions.None));

                // AddMultiExtension checks the characters through the new file name, so it throws exactly where the extension is invalid.
                bool added = TryRun(() => file.AddMultiExtension(extension, PathOptions.None), out _);
                added.ShouldBe(valid, context);

                void CheckAgrees(Action action)
                {
                    bool success = TryRun(action, out var ex);
                    bool invalidExtension = !success && ex!.Message.StartsWith($"Invalid extension '{extension}':", StringComparison.Ordinal);
                    invalidExtension.ShouldBe(!valid, context);
                }
            }
        }
    }

    #endregion
}
