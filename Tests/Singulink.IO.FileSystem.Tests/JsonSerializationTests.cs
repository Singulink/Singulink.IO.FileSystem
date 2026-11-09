using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Singulink.IO.Json;

namespace Singulink.IO.FileSystem.Tests;

[PrefixTestClass]
public partial class JsonSerializationTests
{
    // Relative paths are written in the universal format.
    private const string WrittenRelativeDir = "data/uploads/";
    private const string WrittenRelativeFile = "data/ranges.bin";

    private static readonly string AbsoluteDir = OperatingSystem.IsWindows() ? @"C:\data\uploads\" : "/data/uploads/";
    private static readonly string AbsoluteFile = OperatingSystem.IsWindows() ? @"C:\data\ranges.bin" : "/data/ranges.bin";
    private static readonly string RelativeDir = OperatingSystem.IsWindows() ? @"data\uploads\" : "data/uploads/";
    private static readonly string RelativeFile = OperatingSystem.IsWindows() ? @"data\ranges.bin" : "data/ranges.bin";

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Serialize_WritesPathStrings(bool useContext)
    {
        var model = new PathModel {
            Directory = DirectoryPath.ParseAbsolute(AbsoluteDir),
            File = FilePath.ParseAbsolute(AbsoluteFile),
            RelativeDirectory = DirectoryPath.ParseRelative(RelativeDir),
            RelativeFile = FilePath.ParseRelative(RelativeFile),
            AnyDirectory = DirectoryPath.ParseRelative(RelativeDir),
            AnyFile = FilePath.ParseAbsolute(AbsoluteFile),
            Directories = [DirectoryPath.ParseAbsolute(AbsoluteDir)],
        };

        string json = Serialize(model, useContext);

        json.ShouldBe(Obj(
            ("Directory", JsonString(AbsoluteDir)),
            ("File", JsonString(AbsoluteFile)),
            ("RelativeDirectory", JsonString(WrittenRelativeDir)),
            ("RelativeFile", JsonString(WrittenRelativeFile)),
            ("AnyDirectory", JsonString(WrittenRelativeDir)),
            ("AnyFile", JsonString(AbsoluteFile)),
            ("Directories", $"[{JsonString(AbsoluteDir)}]")));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Deserialize_ParsesAllPathTypes(bool useContext)
    {
        string json = Obj(
            ("Directory", JsonString(AbsoluteDir)),
            ("File", JsonString(AbsoluteFile)),
            ("RelativeDirectory", JsonString(RelativeDir)),
            ("RelativeFile", JsonString(RelativeFile)),
            ("AnyDirectory", JsonString(AbsoluteDir)),
            ("AnyFile", JsonString(RelativeFile)),
            ("Directories", $"[{JsonString(AbsoluteDir)},{JsonString(AbsoluteDir)}]"));

        var model = Deserialize(json, useContext);

        model.Directory.ShouldBe(DirectoryPath.ParseAbsolute(AbsoluteDir));
        model.File.ShouldBe(FilePath.ParseAbsolute(AbsoluteFile));
        model.RelativeDirectory.ShouldBe(DirectoryPath.ParseRelative(RelativeDir));
        model.RelativeFile.ShouldBe(FilePath.ParseRelative(RelativeFile));
        model.AnyDirectory.ShouldBeAssignableTo<IAbsoluteDirectoryPath>().ShouldBe(DirectoryPath.ParseAbsolute(AbsoluteDir));
        model.AnyFile.ShouldBeAssignableTo<IRelativeFilePath>().ShouldBe(FilePath.ParseRelative(RelativeFile));
        model.Directories.ShouldBe([DirectoryPath.ParseAbsolute(AbsoluteDir), DirectoryPath.ParseAbsolute(AbsoluteDir)]);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void RoundTrip_PreservesPaths(bool useContext)
    {
        var model = new PathModel {
            Directory = DirectoryPath.ParseAbsolute(AbsoluteDir),
            File = FilePath.ParseAbsolute(AbsoluteFile),
            RelativeDirectory = DirectoryPath.ParseRelative(""),
            RelativeFile = FilePath.ParseRelative(RelativeFile),
            AnyDirectory = DirectoryPath.ParseRelative(".."),
            AnyFile = FilePath.ParseRelative(RelativeFile),
        };

        var result = Deserialize(Serialize(model, useContext), useContext);

        result.Directory.ShouldBe(model.Directory);
        result.File.ShouldBe(model.File);
        result.RelativeDirectory.ShouldBe(model.RelativeDirectory);
        result.RelativeFile.ShouldBe(model.RelativeFile);
        result.AnyDirectory.ShouldBe(model.AnyDirectory);
        result.AnyFile.ShouldBe(model.AnyFile);
        result.Directories.ShouldBeNull();
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void EmptyRelativeDirectory_RoundTripsAsEmptyString(bool useContext)
    {
        var model = new PathModel { RelativeDirectory = DirectoryPath.ParseRelative(""), AnyDirectory = DirectoryPath.ParseRelative("") };

        string json = Serialize(model, useContext);
        json.ShouldBe("""{"Directory":null,"File":null,"RelativeDirectory":"","RelativeFile":null,"AnyDirectory":"","AnyFile":null,"Directories":null}""");

        var result = Deserialize(json, useContext);
        result.RelativeDirectory.ShouldBe(DirectoryPath.ParseRelative(""));
        result.AnyDirectory.ShouldBeAssignableTo<IRelativeDirectoryPath>().ShouldBe(DirectoryPath.ParseRelative(""));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Null_MapsToNull(bool useContext)
    {
        var model = Deserialize("""{"Directory":null,"File":null,"AnyDirectory":null,"Directories":[null]}""", useContext);

        model.Directory.ShouldBeNull();
        model.File.ShouldBeNull();
        model.AnyDirectory.ShouldBeNull();
        model.Directories.ShouldBe([null]);

        Serialize(new PathModel(), useContext).ShouldContain("\"Directory\":null");
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Deserialize_RelativeValueForAbsoluteType_Throws(bool useContext)
    {
        var ex = Should.Throw<JsonException>(() => Deserialize($$"""{"Directory":{{JsonString(RelativeDir)}}}""", useContext));

        ex.Message.ShouldBe($"Invalid absolute directory path '{RelativeDir}': Path is not an absolute path.");
        ex.Path.ShouldBe("$.Directory");
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Deserialize_DirectoryValueForFileType_Throws(bool useContext)
    {
        var ex = Should.Throw<JsonException>(() => Deserialize($$"""{"Directories":[],"File":{{JsonString(AbsoluteDir)}}}""", useContext));

        ex.Message.ShouldBe($"Invalid absolute file path '{AbsoluteDir}': No file name in path.");
        ex.Path.ShouldBe("$.File");
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Deserialize_InvalidValueInCollection_Throws(bool useContext)
    {
        var ex = Should.Throw<JsonException>(() => Deserialize($$"""{"Directories":[{{JsonString(AbsoluteDir)}},{{JsonString(RelativeDir)}}]}""", useContext));

        ex.Message.ShouldBe($"Invalid absolute directory path '{RelativeDir}': Path is not an absolute path.");
        ex.Path.ShouldBe("$.Directories[1]");
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Deserialize_NonStringValue_Throws(bool useContext)
    {
        var ex = Should.Throw<JsonException>(() => Deserialize("""{"AnyFile":42}""", useContext));

        ex.Message.ShouldBe("Invalid file path: expected a JSON string but found Number.");
        ex.Path.ShouldBe("$.AnyFile");
    }

    [TestMethod]
    public void SourceGeneratedContext_UsesAttachedConverters()
    {
        var options = PathJsonContext.Default.Options;

        options.GetTypeInfo(typeof(IAbsoluteDirectoryPath)).Converter.ShouldBeOfType<AbsoluteDirectoryPathJsonConverter>();
        options.GetTypeInfo(typeof(IAbsoluteFilePath)).Converter.ShouldBeOfType<AbsoluteFilePathJsonConverter>();
        options.GetTypeInfo(typeof(IRelativeDirectoryPath)).Converter.ShouldBeOfType<RelativeDirectoryPathJsonConverter>();
        options.GetTypeInfo(typeof(IRelativeFilePath)).Converter.ShouldBeOfType<RelativeFilePathJsonConverter>();
        options.GetTypeInfo(typeof(IDirectoryPath)).Converter.ShouldBeOfType<DirectoryPathJsonConverter>();
        options.GetTypeInfo(typeof(IFilePath)).Converter.ShouldBeOfType<FilePathJsonConverter>();
        options.GetTypeInfo(typeof(IPath)).Converter.ShouldBeOfType<PathJsonConverter>();
        options.GetTypeInfo(typeof(IAbsolutePath)).Converter.ShouldBeOfType<AbsolutePathJsonConverter>();
        options.GetTypeInfo(typeof(IRelativePath)).Converter.ShouldBeOfType<RelativePathJsonConverter>();
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void DictionaryKeys_RoundTrip(bool useContext)
    {
        var model = new DictionaryModel {
            Files = new() { [FilePath.ParseAbsolute(AbsoluteFile)] = 1 },
            Directories = new() { [DirectoryPath.ParseRelative(RelativeDir)] = "a", [DirectoryPath.ParseRelative("")] = "b" },
            AnyDirectories = new() { [DirectoryPath.ParseAbsolute(AbsoluteDir)] = 2, [DirectoryPath.ParseRelative(RelativeDir)] = 3 },
        };

        string json = Serialize(model, useContext);

        json.ShouldBe(Obj(
            ("Files", Obj((AbsoluteFile, "1"))),
            ("Directories", Obj((WrittenRelativeDir, "\"a\""), ("", "\"b\""))),
            ("AnyDirectories", Obj((AbsoluteDir, "2"), (WrittenRelativeDir, "3")))));

        var result = Deserialize<DictionaryModel>(json, useContext);

        result.Files.ShouldBe(model.Files);
        result.Directories.ShouldBe(model.Directories);
        result.AnyDirectories.ShouldBe(model.AnyDirectories);
        result.AnyDirectories!.Keys.First().ShouldBeAssignableTo<IAbsoluteDirectoryPath>();
        result.AnyDirectories.Keys.Last().ShouldBeAssignableTo<IRelativeDirectoryPath>();
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void DictionaryKeys_InvalidKey_Throws(bool useContext)
    {
        string json = Obj(("Files", Obj((AbsoluteFile, "1"), (RelativeFile, "2"))));

        var ex = Should.Throw<JsonException>(() => Deserialize<DictionaryModel>(json, useContext));

        ex.Message.ShouldBe($"Invalid absolute file path '{RelativeFile}': Path is not an absolute path.");
        ex.Path.ShouldBe($"$.Files['{RelativeFile}']");
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void GeneralTypes_SerializeAsPathStrings(bool useContext)
    {
        var model = new GeneralModel {
            Path = DirectoryPath.ParseAbsolute(AbsoluteDir),
            AbsolutePath = FilePath.ParseAbsolute(AbsoluteFile),
            RelativePath = DirectoryPath.ParseRelative(RelativeDir),
            Paths = new() { [FilePath.ParseRelative(RelativeFile)] = 1 },
        };

        Serialize(model, useContext).ShouldBe(Obj(
            ("Path", JsonString(AbsoluteDir)),
            ("AbsolutePath", JsonString(AbsoluteFile)),
            ("RelativePath", JsonString(WrittenRelativeDir)),
            ("Paths", Obj((WrittenRelativeFile, "1")))));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void GeneralTypes_RoundTrip(bool useContext)
    {
        var model = new GeneralModel {
            Path = FilePath.ParseRelative(RelativeFile),
            AbsolutePath = DirectoryPath.ParseAbsolute(AbsoluteDir),
            RelativePath = DirectoryPath.ParseRelative(""),
            Paths = new() {
                [DirectoryPath.ParseAbsolute(AbsoluteDir)] = 1,
                [FilePath.ParseAbsolute(AbsoluteFile)] = 2,
                [DirectoryPath.ParseRelative(RelativeDir)] = 3,
                [FilePath.ParseRelative(RelativeFile)] = 4,
                [DirectoryPath.ParseRelative("")] = 5,
                [DirectoryPath.ParseRelative("..")] = 6,
            },
        };

        var result = Deserialize<GeneralModel>(Serialize(model, useContext), useContext);

        result.Path.ShouldBeAssignableTo<IRelativeFilePath>().ShouldBe(model.Path);
        result.AbsolutePath.ShouldBeAssignableTo<IAbsoluteDirectoryPath>().ShouldBe(model.AbsolutePath);
        result.RelativePath.ShouldBeAssignableTo<IRelativeDirectoryPath>().ShouldBe(model.RelativePath);
        result.Paths.ShouldBe(model.Paths);
        result.Paths!.Keys.Select(k => k.GetType()).ShouldBe(model.Paths.Keys.Select(k => k.GetType()));
    }

    [TestMethod]
    [DataRow(false, "", true)]
    [DataRow(true, "", true)]
    [DataRow(false, ".", true)]
    [DataRow(true, ".", true)]
    [DataRow(false, "..", true)]
    [DataRow(true, "..", true)]
    [DataRow(false, "a/..", true)]
    [DataRow(true, "a/..", true)]
    [DataRow(false, "a/b/.", true)]
    [DataRow(true, "a/b/.", true)]
    [DataRow(false, "a/b/", true)]
    [DataRow(true, "a/b/", true)]
    [DataRow(false, "a/b", false)]
    [DataRow(true, "a/b", false)]
    [DataRow(false, "a/b.txt", false)]
    [DataRow(true, "a/b.txt", false)]
    [DataRow(false, "a/.b", false)]
    [DataRow(true, "a/.b", false)]
    public void GeneralTypes_ReadRelativeByShape(bool useContext, string value, bool isDirectory)
    {
        var model = Deserialize<GeneralModel>(Obj(("Path", JsonString(value)), ("RelativePath", JsonString(value))), useContext);
        IRelativePath expected = isDirectory ? DirectoryPath.ParseRelative(value) : FilePath.ParseRelative(value);

        model.Path.ShouldBe(expected);
        model.RelativePath.ShouldBe(expected);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void GeneralTypes_ReadAbsoluteByShape(bool useContext)
    {
        string dirWithoutSeparator = AbsoluteDir.TrimEnd(Path.DirectorySeparatorChar);
        string json = Obj(("Path", JsonString(AbsoluteDir)), ("AbsolutePath", JsonString(dirWithoutSeparator)));

        var model = Deserialize<GeneralModel>(json, useContext);

        model.Path.ShouldBeAssignableTo<IAbsoluteDirectoryPath>().ShouldBe(DirectoryPath.ParseAbsolute(AbsoluteDir));
        model.AbsolutePath.ShouldBeAssignableTo<IAbsoluteFilePath>().ShouldBe(FilePath.ParseAbsolute(dirWithoutSeparator));
    }

    [TestMethod]
    [DataRow(false, "AbsolutePath", "data/ranges.bin", "Invalid absolute path 'data/ranges.bin': Path is not an absolute path.")]
    [DataRow(true, "AbsolutePath", "data/ranges.bin", "Invalid absolute path 'data/ranges.bin': Path is not an absolute path.")]
    [DataRow(false, "AbsolutePath", "", "Invalid absolute path '': Path is not an absolute path.")]
    [DataRow(true, "AbsolutePath", "", "Invalid absolute path '': Path is not an absolute path.")]
    [DataRow(false, "Path", "data//name", "Invalid path 'data//name': Invalid empty directory in path.")]
    [DataRow(true, "Path", "data//name", "Invalid path 'data//name': Invalid empty directory in path.")]
    [DataRow(false, "RelativePath", "data//name/", "Invalid relative path 'data//name/': Invalid empty directory in path.")]
    [DataRow(true, "RelativePath", "data//name/", "Invalid relative path 'data//name/': Invalid empty directory in path.")]
    public void GeneralTypes_InvalidValue_Throws(bool useContext, string property, string value, string message)
    {
        var ex = Should.Throw<JsonException>(() => Deserialize<GeneralModel>(Obj((property, JsonString(value))), useContext));

        ex.Message.ShouldBe(message);
        ex.Path.ShouldBe("$." + property);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void GeneralTypes_AbsoluteValueForRelativeType_Throws(bool useContext)
    {
        var ex = Should.Throw<JsonException>(() => Deserialize<GeneralModel>(Obj(("RelativePath", JsonString(AbsoluteFile))), useContext));

        ex.Message.ShouldStartWith($"Invalid relative path '{AbsoluteFile}': ");
        ex.Path.ShouldBe("$.RelativePath");
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void GeneralTypes_NullReadsAsNull(bool useContext)
    {
        var model = Deserialize<GeneralModel>("""{"Path":null,"AbsolutePath":null,"RelativePath":null}""", useContext);

        model.Path.ShouldBeNull();
        model.AbsolutePath.ShouldBeNull();
        model.RelativePath.ShouldBeNull();
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void ObjectProperties_SerializeAsPathStrings(bool useContext)
    {
        var model = new ObjectModel {
            Value = DirectoryPath.ParseAbsolute(AbsoluteDir),
            Values = [FilePath.ParseAbsolute(AbsoluteFile), DirectoryPath.ParseRelative(RelativeDir), FilePath.ParseRelative(RelativeFile), "text"],
        };

        Serialize(model, useContext).ShouldBe(Obj(
            ("Value", JsonString(AbsoluteDir)),
            ("Values", $"[{JsonString(AbsoluteFile)},{JsonString(WrittenRelativeDir)},{JsonString(WrittenRelativeFile)},\"text\"]")));
    }

    [TestMethod]
    public void SourceGeneratedContext_ConvertersListedInOptions()
    {
        var model = new ListedConvertersModel { Directory = DirectoryPath.ParseAbsolute(AbsoluteDir) };

        string json = JsonSerializer.Serialize(model, ListedConvertersJsonContext.Default.ListedConvertersModel);
        json.ShouldBe($$"""{"Directory":{{JsonString(AbsoluteDir)}}}""");

        JsonSerializer.Deserialize(json, ListedConvertersJsonContext.Default.ListedConvertersModel).ShouldNotBeNull().Directory.ShouldBe(model.Directory);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Deserialize_OtherPlatformAbsolutePath_Throws(bool useContext)
    {
        string value = OperatingSystem.IsWindows() ? "/data/uploads/" : @"C:\data\uploads\";
        string error = OperatingSystem.IsWindows()
            ? "Path is a Unix absolute path, which is a rooted relative path on Windows. " +
                "Absolute paths are read in the format of the platform the app runs on (Windows)."
            : "Path is a Windows absolute path. Absolute paths are read in the format of the platform the app runs on (Unix).";

        var ex = Should.Throw<JsonException>(() => Deserialize($$"""{"Directory":{{JsonString(value)}}}""", useContext));

        ex.Message.ShouldBe($"Invalid absolute directory path '{value}': {error}");
        ex.Path.ShouldBe("$.Directory");
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void GeneralTypes_OtherPlatformAbsolutePath_ReadAsParseReadsIt(bool useContext)
    {
        if (OperatingSystem.IsWindows())
        {
            var model = Deserialize<GeneralModel>(Obj(("Path", JsonString("/data/uploads/"))), useContext);
            model.Path.ShouldBeAssignableTo<IRelativeDirectoryPath>().ShouldBe(DirectoryPath.ParseRelative(@"\data\uploads\"));
        }
        else
        {
            // On Unix, the string is a relative path with one name, as FilePath.Parse reads it.
            var model = Deserialize<GeneralModel>(Obj(("Path", JsonString(@"C:\data\uploads\"))), useContext);
            model.Path.ShouldBeAssignableTo<IRelativeFilePath>().ShouldBe(FilePath.Parse(@"C:\data\uploads\"));
        }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void RelativePathNotInUniversalFormat_WrittenAsPathDisplay(bool useContext)
    {
        // Windows: a rooted relative path. Unix: a name that isn't valid on Windows.
        string value = OperatingSystem.IsWindows() ? @"\data\ranges.bin" : "data/a:b.bin";
        var model = new PathModel { RelativeFile = FilePath.ParseRelative(value), AnyFile = FilePath.ParseRelative(value) };

        string json = Serialize(model, useContext);
        json.ShouldContain($"\"RelativeFile\":{JsonString(value)}");
        json.ShouldContain($"\"AnyFile\":{JsonString(value)}");

        var result = Deserialize(json, useContext);
        result.RelativeFile.ShouldBe(model.RelativeFile);
        result.AnyFile.ShouldBe(model.AnyFile);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void UnfriendlyNames_RoundTrip(bool useContext)
    {
        // Windows: trailing spaces and dots, leading spaces and reserved device names. Unix: control characters and trailing spaces.
        bool windows = OperatingSystem.IsWindows();
        var absoluteDir = DirectoryPath.ParseAbsolute(windows ? @"C:\data \" : "/data\u0001/", PathOptions.None);
        var absoluteFile = FilePath.ParseAbsolute(windows ? @"C:\con.\file." : "/data /file.", PathOptions.None);
        var relativeDir = DirectoryPath.ParseRelative(windows ? @"data.\" : "a\u0002/", PathOptions.None);
        var relativeFile = FilePath.ParseRelative(windows ? @" aux\x " : " x/\u0003", PathOptions.None);

        var model = new PathModel {
            Directory = absoluteDir,
            File = absoluteFile,
            RelativeDirectory = relativeDir,
            RelativeFile = relativeFile,
            AnyDirectory = relativeDir,
            AnyFile = absoluteFile,
            Directories = [absoluteDir],
        };

        var result = Deserialize(Serialize(model, useContext), useContext);

        result.Directory.ShouldBe(absoluteDir);
        result.File.ShouldBe(absoluteFile);
        result.RelativeDirectory.ShouldBe(relativeDir);
        result.RelativeFile.ShouldBe(relativeFile);
        result.AnyDirectory.ShouldBe(relativeDir);
        result.AnyFile.ShouldBe(absoluteFile);
        result.Directories.ShouldBe([absoluteDir]);

        var dictionaries = new DictionaryModel {
            Files = new() { [absoluteFile] = 1 },
            Directories = new() { [relativeDir] = "a" },
            AnyDirectories = new() { [absoluteDir] = 2, [relativeDir] = 3 },
        };

        var dictionariesResult = Deserialize<DictionaryModel>(Serialize(dictionaries, useContext), useContext);

        dictionariesResult.Files.ShouldBe(dictionaries.Files);
        dictionariesResult.Directories.ShouldBe(dictionaries.Directories);
        dictionariesResult.AnyDirectories.ShouldBe(dictionaries.AnyDirectories);

        var general = new GeneralModel {
            Path = relativeFile,
            AbsolutePath = absoluteDir,
            RelativePath = relativeDir,
            Paths = new() { [absoluteFile] = 1, [relativeFile] = 2, [relativeDir] = 3 },
        };

        var generalResult = Deserialize<GeneralModel>(Serialize(general, useContext), useContext);

        generalResult.Path.ShouldBe(relativeFile);
        generalResult.AbsolutePath.ShouldBe(absoluteDir);
        generalResult.RelativePath.ShouldBe(relativeDir);
        generalResult.Paths.ShouldBe(general.Paths);
    }

    private static string Obj(params (string Name, string Value)[] properties)
    {
        return "{" + string.Join(",", properties.Select(p => $"{JsonString(p.Name)}:{p.Value}")) + "}";
    }

    private static PathModel Deserialize(string json, bool useContext) => Deserialize<PathModel>(json, useContext);

    private static string Serialize<T>(T model, bool useContext)
    {
        return useContext ? JsonSerializer.Serialize(model, GetTypeInfo<T>()) : JsonSerializer.Serialize(model);
    }

    private static T Deserialize<T>(string json, bool useContext) where T : class
    {
        var model = useContext ? JsonSerializer.Deserialize(json, GetTypeInfo<T>()) : JsonSerializer.Deserialize<T>(json);
        return model.ShouldNotBeNull();
    }

    private static JsonTypeInfo<T> GetTypeInfo<T>() => (JsonTypeInfo<T>)PathJsonContext.Default.GetTypeInfo(typeof(T)).ShouldNotBeNull();

    public class PathModel
    {
        public IAbsoluteDirectoryPath? Directory { get; set; }

        public IAbsoluteFilePath? File { get; set; }

        public IRelativeDirectoryPath? RelativeDirectory { get; set; }

        public IRelativeFilePath? RelativeFile { get; set; }

        public IDirectoryPath? AnyDirectory { get; set; }

        public IFilePath? AnyFile { get; set; }

        public List<IAbsoluteDirectoryPath?>? Directories { get; set; }
    }

    public class DictionaryModel
    {
        public Dictionary<IAbsoluteFilePath, int>? Files { get; set; }

        public Dictionary<IRelativeDirectoryPath, string>? Directories { get; set; }

        public Dictionary<IDirectoryPath, int>? AnyDirectories { get; set; }
    }

    public class GeneralModel
    {
        public IPath? Path { get; set; }

        public IAbsolutePath? AbsolutePath { get; set; }

        public IRelativePath? RelativePath { get; set; }

        public Dictionary<IPath, int>? Paths { get; set; }
    }

    public class ObjectModel
    {
        public object? Value { get; set; }

        public List<object>? Values { get; set; }
    }

    public class ListedConvertersModel
    {
        public IAbsoluteDirectoryPath? Directory { get; set; }
    }

    [JsonSerializable(typeof(PathModel))]
    [JsonSerializable(typeof(IAbsoluteDirectoryPath))]
    [JsonSerializable(typeof(IAbsoluteFilePath))]
    [JsonSerializable(typeof(IRelativeDirectoryPath))]
    [JsonSerializable(typeof(IRelativeFilePath))]
    [JsonSerializable(typeof(IDirectoryPath))]
    [JsonSerializable(typeof(IFilePath))]
    [JsonSerializable(typeof(IPath))]
    [JsonSerializable(typeof(IAbsolutePath))]
    [JsonSerializable(typeof(IRelativePath))]
    [JsonSerializable(typeof(DictionaryModel))]
    [JsonSerializable(typeof(GeneralModel))]
    [JsonSerializable(typeof(ObjectModel))]
    [JsonSerializable(typeof(string))]
    internal partial class PathJsonContext : JsonSerializerContext;

    [JsonSourceGenerationOptions(Converters = [typeof(AbsoluteDirectoryPathJsonConverter)])]
    [JsonSerializable(typeof(ListedConvertersModel))]
    internal partial class ListedConvertersJsonContext : JsonSerializerContext;
}
