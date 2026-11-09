namespace Singulink.IO.FileSystem.Tests;

[PrefixTestClass]
public class SingleExtensionTests
{
    private const string ShapeError = "it must start with a dot '.' and have no other dots.";

    #region HasExtension

    [TestMethod]
    [DataRow("Windows", @"C:\dir\backup.tar.gz", ".gz", true)]
    [DataRow("Windows", @"C:\dir\backup.tar.gz", ".GZ", true)]
    [DataRow("Windows", @"C:\dir\backup.tar.gz", ".tar", false)]
    [DataRow("Windows", @"\\server\share\Makefile", ".e", false)]
    [DataRow("Unix", "/dir/photo.JPG", ".jpg", true)]
    [DataRow("Unix", "/dir/photo.jpg", ".JPG", true)]
    [DataRow("Unix", "/dir/a?b.x?y", ".x?y", true)]
    [DataRow("Unix", "/dir/data.", ".", true)]
    [DataRow("Unix", "/dir/data.txt", ".", false)]
    [DataRow("Unix", "/.gitignore", ".gitignore", true)]
    public void HasExtension_Absolute(string formatName, string path, string extension, bool expected)
    {
        var file = FilePath.ParseAbsolute(path, GetFormat(formatName), PathOptions.None);
        file.HasExtension(extension).ShouldBe(expected);
    }

    [TestMethod]
    [DataRow("Windows", @"dir\backup.Tar.Gz", ".gz", true)]
    [DataRow("Windows", @"\a\.gitignore", ".GITIGNORE", true)]
    [DataRow("Unix", "../backup.tar.gz", ".tar", false)]
    [DataRow("Unix", "Makefile", ".Makefile", false)]
    [DataRow("Universal", "a/b.TAR.gz", ".GZ", true)]
    [DataRow("Universal", "a/photo.jpeg", ".jpg", false)]
    public void HasExtension_Relative(string formatName, string path, string extension, bool expected)
    {
        var file = FilePath.ParseRelative(path, GetFormat(formatName), PathOptions.None);
        file.HasExtension(extension).ShouldBe(expected);
    }

    [TestMethod]
    [DataRow("Windows", ".a?b", "Invalid character '?' in entry name '.a?b'. Invalid characters include: < > : \" | ? * / \\")]
    [DataRow("Windows", ".a\u0001", "Invalid control character '\u0001' in entry name '.a\u0001'.")]
    [DataRow("Unix", ".a/b", "Invalid character '/' in entry name '.a/b'.")]
    [DataRow("Universal", ".a*b", "Invalid character '*' in entry name '.a*b'. Invalid characters include: < > : \" | ? * / \\")]
    public void HasExtension_InvalidCharacter_Throws(string formatName, string extension, string error)
    {
        var file = FilePath.ParseRelative("backup.tar.gz", GetFormat(formatName), PathOptions.None);

        var ex = Should.Throw<ArgumentException>(() => file.HasExtension(extension));
        ex.Message.ShouldBe($"Invalid extension '{extension}': {error} (Parameter 'extension')");
        ex.ParamName.ShouldBe("extension");
    }

    [TestMethod]
    public void HasExtension_Null_Throws()
    {
        var file = FilePath.ParseRelative("backup.tar.gz", PathFormat.Unix);
        Should.Throw<ArgumentNullException>(() => file.HasExtension(null!));
    }

    [TestMethod]
    public void HasExtension_Empty_Throws()
    {
        var file = FilePath.ParseRelative("Makefile", PathFormat.Unix);

        var ex = Should.Throw<ArgumentException>(() => file.HasExtension(""));
        ex.Message.ShouldBe("Invalid extension '': it can't be empty. (Parameter 'extension')");
    }

    #endregion

    #region WithExtension

    [TestMethod]
    public void WithExtension_LastExtensionOnly()
    {
        var file = FilePath.ParseRelative("backup.tar.gz", PathFormat.Universal);

        foreach (string extension in new[] { ".bz2", ".", ".GZ" })
            file.WithExtension(extension, PathOptions.None).Extension.ShouldBe(extension);

        file.WithExtension(null).PathDisplay.ShouldBe("backup.tar");
        file.WithExtension("").PathDisplay.ShouldBe("backup.tar");
    }

    #endregion

    #region AddExtension

    [TestMethod]
    [DataRow("Windows", @"C:\out\backup.tar", ".gz", @"C:\out\backup.tar.gz")]
    [DataRow("Windows", @"\\server\share\report", ".TXT", @"\\server\share\report.TXT")]
    [DataRow("Unix", "/out/backup.tar.gz", ".gz", "/out/backup.tar.gz.gz")]
    [DataRow("Unix", "/out/.config", ".json", "/out/.config.json")]
    public void AddExtension_Absolute(string formatName, string path, string extension, string expected)
    {
        IAbsoluteFilePath file = FilePath.ParseAbsolute(path, GetFormat(formatName));
        file.AddExtension(extension).PathDisplay.ShouldBe(expected);
    }

    [TestMethod]
    [DataRow("Windows", @"..\backup", ".zip", @"..\backup.zip")]
    [DataRow("Windows", @"\backup.tar", ".gz", @"\backup.tar.gz")]
    [DataRow("Unix", "a/backup", ".tar", "a/backup.tar")]
    [DataRow("Universal", "a/b.c", ".d", "a/b.c.d")]
    public void AddExtension_Relative(string formatName, string path, string extension, string expected)
    {
        IRelativeFilePath file = FilePath.ParseRelative(path, GetFormat(formatName));
        file.AddExtension(extension).PathDisplay.ShouldBe(expected);
    }

    [TestMethod]
    public void AddExtension_EmptyAndNewNameErrors()
    {
        var file = FilePath.ParseRelative("data", PathFormat.Windows);

        file.AddExtension(null).ShouldBeSameAs(file);
        file.AddExtension("").ShouldBeSameAs(file);
        file.AddExtension(".", PathOptions.None).PathDisplay.ShouldBe("data.");
        file.AddExtension(". ", PathOptions.None).PathDisplay.ShouldBe("data. ");

        var ex = Should.Throw<ArgumentException>(() => file.AddExtension("."));
        ex.Message.ShouldBe("Invalid new file name: Entry name ends with a dot. (Parameter 'extension')");

        ex = Should.Throw<ArgumentException>(() => file.AddExtension(".g?z"));
        ex.Message.ShouldBe(
            "Invalid new file name: Invalid character '?' in entry name 'data.g?z'. Invalid characters include: < > : \" | ? * / \\ (Parameter 'extension')");
    }

    #endregion

    #region RemoveExtension

    [TestMethod]
    [DataRow("Windows", @"C:\out\backup.tar.gz", ".GZ", @"C:\out\backup.tar")]
    [DataRow("Windows", @"\\server\share\BACKUP.TXT", ".txt", @"\\server\share\BACKUP")]
    [DataRow("Unix", "/out/photo.JPG", ".jpg", "/out/photo")]
    [DataRow("Unix", "/a/.config.json", ".json", "/a/.config")]
    public void RemoveExtension_Absolute(string formatName, string path, string extension, string expected)
    {
        IAbsoluteFilePath file = FilePath.ParseAbsolute(path, GetFormat(formatName));
        IAbsoluteFilePath result = file.RemoveExtension(extension);

        result.PathDisplay.ShouldBe(expected);
        result.ShouldBe(FilePath.ParseAbsolute(expected, GetFormat(formatName)));
    }

    [TestMethod]
    [DataRow("Windows", @"\backup.Tar.Gz", ".gz", @"\backup.Tar")]
    [DataRow("Unix", "../a/b.c.d", ".D", "../a/b.c")]
    [DataRow("Universal", "x.tar.gz.bak", ".bak", "x.tar.gz")]
    public void RemoveExtension_Relative(string formatName, string path, string extension, string expected)
    {
        IRelativeFilePath file = FilePath.ParseRelative(path, GetFormat(formatName));
        IRelativeFilePath result = file.RemoveExtension(extension);

        result.PathDisplay.ShouldBe(expected);
        result.ShouldBe(FilePath.ParseRelative(expected, GetFormat(formatName)));
    }

    [TestMethod]
    public void RemoveExtension_ThroughFilePathInterface()
    {
        IFilePath absolute = FilePath.ParseAbsolute(@"C:\backup.tar.gz", PathFormat.Windows);
        IFilePath relative = FilePath.ParseRelative("backup.tar.gz", PathFormat.Unix);

        absolute.RemoveExtension(".gz").ShouldBeAssignableTo<IAbsoluteFilePath>().PathDisplay.ShouldBe(@"C:\backup.tar");
        relative.RemoveExtension(".gz").ShouldBeAssignableTo<IRelativeFilePath>().PathDisplay.ShouldBe("backup.tar");
    }

    [TestMethod]
    public void RemoveExtension_Empty_ReturnsSamePath()
    {
        var absolute = FilePath.ParseAbsolute("/out/Makefile", PathFormat.Unix);
        var relative = FilePath.ParseRelative("backup.tar.gz", PathFormat.Windows);

        absolute.RemoveExtension("").ShouldBeSameAs(absolute);
        relative.RemoveExtension("").ShouldBeSameAs(relative);
    }

    [TestMethod]
    [DataRow("Unix", "data.", "data")]
    [DataRow("Windows", @"dir\data.", @"dir\data")]
    public void RemoveExtension_LoneDot(string formatName, string path, string expected)
    {
        var file = FilePath.ParseRelative(path, GetFormat(formatName), PathOptions.None);
        file.RemoveExtension(".", PathOptions.None).PathDisplay.ShouldBe(expected);
    }

    [TestMethod]
    [DataRow("Windows", @"C:\out\backup.tar.gz", ".tar", "backup.tar.gz")]
    [DataRow("Unix", "/out/photo.jpeg", ".jpg", "photo.jpeg")]
    [DataRow("Unix", "/out/Makefile", ".Makefile", "Makefile")]
    public void RemoveExtension_NotLastExtension_Throws(string formatName, string path, string extension, string name)
    {
        var file = FilePath.ParseAbsolute(path, GetFormat(formatName));

        var ex = Should.Throw<ArgumentException>(() => file.RemoveExtension(extension));
        ex.Message.ShouldBe($"File name '{name}' does not end with the extension '{extension}'. (Parameter 'extension')");
        ex.ParamName.ShouldBe("extension");
    }

    [TestMethod]
    [DataRow("Windows", @"out\.gitignore", ".GITIGNORE")]
    [DataRow("Universal", "a/.gitignore", ".gitignore")]
    public void RemoveExtension_WholeName_Throws(string formatName, string path, string extension)
    {
        var file = FilePath.ParseRelative(path, GetFormat(formatName));

        var ex = Should.Throw<ArgumentException>(() => file.RemoveExtension(extension));
        ex.Message.ShouldBe($"Extension '{extension}' is the whole file name, so removing it leaves no name. (Parameter 'extension')");
        ex.ParamName.ShouldBe("extension");
    }

    [TestMethod]
    public void RemoveExtension_InvalidCharacter_Throws()
    {
        var file = FilePath.ParseRelative("backup.tar.gz", PathFormat.Universal);

        var ex = Should.Throw<ArgumentException>(() => file.RemoveExtension(".g<z"));
        ex.Message.ShouldBe(
            "Invalid extension '.g<z': Invalid character '<' in entry name '.g<z'. Invalid characters include: < > : \" | ? * / \\ (Parameter 'extension')");
    }

    [TestMethod]
    [DataRow("Windows", @"C:\out\con.txt", ".txt", "Invalid reserved device name in entry name 'con'.")]
    [DataRow("Windows", @"C:\out\x .txt", ".TXT", "Entry name ends with a space.")]
    [DataRow("Unix", "/out/x..gz", ".gz", "Entry name ends with a dot.")]
    public void RemoveExtension_InvalidRemainingName_Throws(string formatName, string path, string extension, string error)
    {
        var file = FilePath.ParseAbsolute(path, GetFormat(formatName), PathOptions.None);

        var ex = Should.Throw<ArgumentException>(() => file.RemoveExtension(extension));
        ex.Message.ShouldBe($"Invalid new file name: {error} (Parameter 'extension')");
        ex.ParamName.ShouldBe("extension");

        // Allowed with options that don't apply the check.
        file.RemoveExtension(extension, PathOptions.None).Name.ShouldBe(file.Name[..^extension.Length]);
    }

    [TestMethod]
    [DataRow("Unix", "..txt", ".txt", ".")]
    [DataRow("Windows", @"..\..TXT", ".txt", ".")]
    [DataRow("Universal", "a/...gz", ".gz", "..")]
    public void RemoveExtension_NavigationalName_Throws(string formatName, string path, string extension, string newName)
    {
        var file = FilePath.ParseRelative(path, GetFormat(formatName), PathOptions.None);

        var ex = Should.Throw<ArgumentException>(() => file.RemoveExtension(extension, PathOptions.None));
        ex.Message.ShouldBe($"Invalid new file name: '{newName}' is a navigational path segment. (Parameter 'extension')");
        ex.ParamName.ShouldBe("extension");
    }

    [TestMethod]
    public void RemoveExtension_Null_Throws()
    {
        var file = FilePath.ParseRelative("backup.tar.gz", PathFormat.Unix);
        Should.Throw<ArgumentNullException>(() => file.RemoveExtension(null!));
    }

    #endregion

    #region Shape

    [TestMethod]
    [DataRow(".tar.gz")]
    [DataRow(".a.b.c")]
    [DataRow("..gz")]
    [DataRow(".tar.")]
    [DataRow("..")]
    [DataRow("...")]
    [DataRow("gz")]
    [DataRow("a.gz")]
    public void SingleMembers_BadShape_Throws(string extension)
    {
        string expected = $"Invalid extension '{extension}': {ShapeError} (Parameter 'extension')";

        foreach (var format in Formats)
        {
            // Relative and absolute paths (relative only in the universal format).
            foreach (var file in GetFiles(format, ["backup.tar.gz"]))
            {
                string context = $"[{format}] {file}";

                Check(() => file.HasExtension(extension));
                Check(() => file.WithExtension(extension));
                Check(() => file.WithExtension(extension, PathOptions.None));
                Check(() => file.AddExtension(extension));
                Check(() => file.AddExtension(extension, PathOptions.None));
                Check(() => file.RemoveExtension(extension));
                Check(() => file.RemoveExtension(extension, PathOptions.None));

                void Check(Action action)
                {
                    var ex = Should.Throw<ArgumentException>(action, context);
                    ex.Message.ShouldBe(expected, context);
                    ex.ParamName.ShouldBe("extension", context);
                }
            }
        }
    }

    #endregion

    #region Guarantees

    [TestMethod]
    public void SingleMembers_KeepExtensionAccurate()
    {
        string[] names = ["backup", "backup.tar", "backup.TAR.GZ", ".gitignore", "data.", "my.report.v2", "photo.JPG", "x . y", "con", "x..gz"];
        string[] extensions = [".gz", ".GZ", ".tar", ".jpg", ".", ". ", ".gitignore", ".txt", ".V2", ". y", " .y", ".con", ".a?b", ".a\u0001"];

        foreach (var format in Formats)
        {
            int has = 0, hasTrue = 0, with = 0, add = 0, remove = 0;

            foreach (var file in GetFiles(format, names))
            {
                foreach (string extension in extensions)
                {
                    string context = $"[{format}] {file} '{extension}'";
                    bool isExtension = string.Equals(file.Extension, extension, StringComparison.OrdinalIgnoreCase);

                    if (TryGet(() => file.HasExtension(extension), out bool hasExtension))
                    {
                        hasExtension.ShouldBe(isExtension, context);
                        has++;
                        hasTrue += hasExtension ? 1 : 0;
                    }
                    else
                    {
                        format.IsValidExtension(extension, PathOptions.None).ShouldBeFalse(context);
                    }

                    foreach (var options in new[] { PathOptions.None, PathOptions.NoUnfriendlyNames })
                    {
                        if (TryGet(() => file.WithExtension(extension, options), out var withExtension))
                        {
                            withExtension.Extension.ShouldBe(extension, context);
                            with++;
                        }

                        if (TryGet(() => file.AddExtension(extension, options), out var addedExtension))
                        {
                            addedExtension.Extension.ShouldBe(extension, context);
                            add++;
                        }

                        if (TryGet(() => file.RemoveExtension(extension, options), out var removedExtension))
                        {
                            isExtension.ShouldBeTrue(context);
                            file.HasExtension(extension).ShouldBeTrue(context);
                            removedExtension.Name.ShouldBe(file.Name[..^extension.Length], context);
                            remove++;
                        }
                    }
                }
            }

            // Each guarantee was checked on a fair number of paths.
            has.ShouldBeGreaterThan(100, format.ToString());
            hasTrue.ShouldBeGreaterThan(8, format.ToString());
            with.ShouldBeGreaterThan(100, format.ToString());
            add.ShouldBeGreaterThan(100, format.ToString());
            remove.ShouldBeGreaterThan(10, format.ToString());
        }
    }

    [TestMethod]
    public void SingleAndMultiMembers_AgreeOnOnePart()
    {
        string[] names = ["backup", "backup.tar.gz", ".gitignore", "data.", "con.txt", "x .gz", "...gz"];

        // ".a\\b" is a valid one-part extension in the Unix format and has an invalid character in the others.
        string[] extensions = ["", ".gz", ".GZ", ".txt", ".", ". ", ".gitignore", ".tar", ".a?b", ".a\u0001", ".a/b", ".a\\b"];

        foreach (var format in Formats)
        {
            foreach (var file in GetFiles(format, names))
            {
                foreach (string extension in extensions)
                {
                    string context = $"[{format}] {file} '{extension}'";

                    Outcome(() => file.HasExtension(extension)).ShouldBe(Outcome(() => file.HasMultiExtension(extension)), context);

                    foreach (var options in new[] { PathOptions.None, PathOptions.NoUnfriendlyNames })
                    {
                        Outcome(() => file.AddExtension(extension, options)).ShouldBe(Outcome(() => file.AddMultiExtension(extension, options)), context);
                        Outcome(() => file.RemoveExtension(extension, options)).ShouldBe(Outcome(() => file.RemoveMultiExtension(extension, options)), context);
                    }

                    string Outcome(Func<object> action)
                    {
                        try
                        {
                            object result = action();
                            return ReferenceEquals(result, file) ? "same" : $"{result}";
                        }
                        catch (ArgumentException ex)
                        {
                            return $"{ex.GetType().Name}: {ex.Message}";
                        }
                    }
                }
            }
        }
    }

    #endregion

    #region IsValidExtension

    [TestMethod]
    public void IsValidExtension_DefaultOptions()
    {
        var format = PathFormat.Windows;

        // Without options, PathOptions.NoUnfriendlyNames applies.
        format.IsValidExtension(".gz ").ShouldBe(format.IsValidExtension(".gz ", PathOptions.NoUnfriendlyNames));
        format.IsValidExtension(".gz ", PathOptions.None).ShouldBeTrue();
    }

    [TestMethod]
    public void IsValidExtension_AgreesWithMembers()
    {
        string[] extensions = [
            ".gz", ".A", ".", ". ", ".tar.gz", "..", "..gz", ".gz.", "gz", "a.gz",
            ".a?b", ".g:z", ".a/b", ".a\\b", ".a\u0001", ".a\0b", ".x|y", ".gz ",
        ];

        foreach (var format in Formats)
        {
            var file = FilePath.ParseRelative("file.tar.gz", format);

            foreach (string extension in extensions)
            {
                string context = $"[{format}] '{extension}'";
                bool valid = format.IsValidExtension(extension, PathOptions.None);

                CheckAgrees(() => file.HasExtension(extension));
                CheckAgrees(() => file.RemoveExtension(extension, PathOptions.None));

                // AddExtension and WithExtension check the characters through the new file name, so with PathOptions.None they throw exactly where the
                // extension is invalid.
                TryRun(() => file.AddExtension(extension, PathOptions.None), out _).ShouldBe(valid, context);
                TryRun(() => file.WithExtension(extension, PathOptions.None), out _).ShouldBe(valid, context);

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

    /// <summary>
    /// Gets relative and absolute file paths with the given names, parsed with <see cref="PathOptions.None"/>. The universal format has relative paths only.
    /// </summary>
    private static IEnumerable<IFilePath> GetFiles(PathFormat format, string[] names)
    {
        foreach (string name in names)
        {
            if (format == PathFormat.Universal)
            {
                yield return FilePath.ParseRelative($"dir/{name}", format, PathOptions.None);
                continue;
            }

            yield return FilePath.ParseRelative(name, format, PathOptions.None);
            yield return FilePath.ParseAbsolute(format == PathFormat.Windows ? $@"C:\dir\{name}" : $"/dir/{name}", format, PathOptions.None);
        }
    }
}
