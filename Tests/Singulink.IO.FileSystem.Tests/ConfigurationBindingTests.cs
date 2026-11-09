using System.ComponentModel;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Singulink.IO.FileSystem.Tests;

[PrefixTestClass]
public class ConfigurationBindingTests
{
    private static readonly string AbsoluteDir = OperatingSystem.IsWindows() ? @"C:\data\uploads" : "/data/uploads";
    private static readonly string AbsoluteFile = OperatingSystem.IsWindows() ? @"C:\data\ranges.bin" : "/data/ranges.bin";
    private static readonly string RelativeDir = OperatingSystem.IsWindows() ? @"data\uploads" : "data/uploads";
    private static readonly string RelativeFile = OperatingSystem.IsWindows() ? @"data\ranges.bin" : "data/ranges.bin";

    [TestMethod]
    public void Get_BindsAllPathTypes()
    {
        var config = BuildConfig($$"""
            {
              "Storage": {
                "Directory": {{JsonString(AbsoluteDir)}},
                "SavedRangesPath": {{JsonString(AbsoluteFile)}},
                "RelativeDirectory": {{JsonString(RelativeDir)}},
                "RelativeFile": {{JsonString(RelativeFile)}},
                "AnyDirectory": {{JsonString(AbsoluteDir)}},
                "AnyFile": {{JsonString(RelativeFile)}}
              }
            }
            """);

        var options = config.GetSection("Storage").Get<StorageOptions>().ShouldNotBeNull();

        options.Directory.ShouldBe(DirectoryPath.ParseAbsolute(AbsoluteDir));
        options.SavedRangesPath.ShouldBe(FilePath.ParseAbsolute(AbsoluteFile));
        options.RelativeDirectory.ShouldBe(DirectoryPath.ParseRelative(RelativeDir));
        options.RelativeFile.ShouldBe(FilePath.ParseRelative(RelativeFile));
        options.AnyDirectory.ShouldBeAssignableTo<IAbsoluteDirectoryPath>().ShouldBe(DirectoryPath.ParseAbsolute(AbsoluteDir));
        options.AnyFile.ShouldBeAssignableTo<IRelativeFilePath>().ShouldBe(FilePath.ParseRelative(RelativeFile));
    }

    [TestMethod]
    public void Bind_ReplacesExistingValues()
    {
        var config = BuildConfig($$"""{ "Storage": { "Directory": {{JsonString(AbsoluteDir)}} } }""");
        var options = new StorageOptions { Directory = DirectoryPath.GetTemp(), SavedRangesPath = FilePath.ParseAbsolute(AbsoluteFile) };

        config.GetSection("Storage").Bind(options);

        options.Directory.ShouldBe(DirectoryPath.ParseAbsolute(AbsoluteDir));
        options.SavedRangesPath.ShouldBe(FilePath.ParseAbsolute(AbsoluteFile));
    }

    [TestMethod]
    public void Configure_BindsOptionsFromSection()
    {
        var config = BuildConfig($$"""{ "Storage": { "Directory": {{JsonString(AbsoluteDir)}}, "SavedRangesPath": {{JsonString(AbsoluteFile)}} } }""");

        using var services = new ServiceCollection().Configure<StorageOptions>(config.GetSection("Storage")).BuildServiceProvider();
        var options = services.GetRequiredService<IOptions<StorageOptions>>().Value;

        options.Directory.ShouldBe(DirectoryPath.ParseAbsolute(AbsoluteDir));
        options.SavedRangesPath.ShouldBe(FilePath.ParseAbsolute(AbsoluteFile));
    }

    [TestMethod]
    public void AddOptionsBind_BindsNestedOptionsAndCollections()
    {
        var config = BuildConfig($$"""
            {
              "App": {
                "Storage": { "Directory": {{JsonString(AbsoluteDir)}} },
                "Directories": [ {{JsonString(AbsoluteDir)}}, {{JsonString(Path.GetTempPath())}} ],
                "NamedFiles": { "Ranges": {{JsonString(AbsoluteFile)}} }
              }
            }
            """);

        var services = new ServiceCollection();
        services.AddOptions<AppOptions>().Bind(config.GetSection("App"));
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AppOptions>>().Value;

        options.Storage.Directory.ShouldBe(DirectoryPath.ParseAbsolute(AbsoluteDir));
        options.Directories.ShouldBe([DirectoryPath.ParseAbsolute(AbsoluteDir), DirectoryPath.GetTemp()]);
        options.NamedFiles["Ranges"].ShouldBe(FilePath.ParseAbsolute(AbsoluteFile));
    }

    [TestMethod]
    public void Get_EnvironmentStyleKeys()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection([new("Storage:Directory", AbsoluteDir)]).Build();
        config.GetSection("Storage").Get<StorageOptions>().ShouldNotBeNull().Directory.ShouldBe(DirectoryPath.ParseAbsolute(AbsoluteDir));
    }

    [TestMethod]
    public void Get_RelativeValueForAbsoluteType_Throws()
    {
        var config = BuildConfig($$"""{ "Storage": { "Directory": {{JsonString(RelativeDir)}} } }""");

        var ex = Should.Throw<InvalidOperationException>(() => config.GetSection("Storage").Get<StorageOptions>());

        ex.Message.ShouldBe($"Failed to convert configuration value '{RelativeDir}' at 'Storage:Directory' to type 'Singulink.IO.IAbsoluteDirectoryPath'.");
        ex.InnerException.ShouldBeOfType<FormatException>().Message.ShouldBe($"Invalid absolute directory path '{RelativeDir}': Path is not an absolute path.");
    }

    [TestMethod]
    public void Get_AbsoluteValueForRelativeType_Throws()
    {
        var config = BuildConfig($$"""{ "Storage": { "RelativeFile": {{JsonString(AbsoluteFile)}} } }""");

        var ex = Should.Throw<InvalidOperationException>(() => config.GetSection("Storage").Get<StorageOptions>());

        ex.Message.ShouldBe($"Failed to convert configuration value '{AbsoluteFile}' at 'Storage:RelativeFile' to type 'Singulink.IO.IRelativeFilePath'.");
        ex.InnerException.ShouldBeOfType<FormatException>().Message.ShouldStartWith($"Invalid relative file path '{AbsoluteFile}': ");
    }

    [TestMethod]
    public void Get_OtherPlatformAbsoluteValue_Throws()
    {
        string value = OperatingSystem.IsWindows() ? "/data/uploads" : @"C:\data\uploads";
        string error = OperatingSystem.IsWindows()
            ? "Path is a Unix absolute path, which is a rooted relative path on Windows. " +
                "Absolute paths are read in the format of the platform the app runs on (Windows)."
            : "Path is a Windows absolute path. Absolute paths are read in the format of the platform the app runs on (Unix).";

        var config = BuildConfig($$"""{ "Storage": { "Directory": {{JsonString(value)}} } }""");

        var ex = Should.Throw<InvalidOperationException>(() => config.GetSection("Storage").Get<StorageOptions>());

        ex.InnerException.ShouldBeOfType<FormatException>().Message.ShouldBe($"Invalid absolute directory path '{value}': {error}");
    }

    [TestMethod]
    public void Get_RelativeValuesInEitherFormat_ReadInCurrentFormat()
    {
        var config = BuildConfig("""{ "Storage": { "RelativeDirectory": "data/uploads", "RelativeFile": "../data/ranges.bin" } }""");

        var options = config.GetSection("Storage").Get<StorageOptions>().ShouldNotBeNull();

        options.RelativeDirectory.ShouldBe(DirectoryPath.ParseRelative("data/uploads/", PathFormat.Universal).ToPathFormat(PathFormat.Current));
        options.RelativeFile.ShouldBe(FilePath.ParseRelative("../data/ranges.bin", PathFormat.Universal).ToPathFormat(PathFormat.Current));
    }

    [TestMethod]
    public void Get_DirectoryValueForFileType_Throws()
    {
        string dir = AbsoluteDir + Path.DirectorySeparatorChar;
        var config = BuildConfig($$"""{ "Storage": { "SavedRangesPath": {{JsonString(dir)}} } }""");

        var ex = Should.Throw<InvalidOperationException>(() => config.GetSection("Storage").Get<StorageOptions>());

        ex.InnerException.ShouldBeOfType<FormatException>().Message.ShouldBe($"Invalid absolute file path '{dir}': No file name in path.");
    }

    [TestMethod]
    public void Get_UnfriendlyName_Reads()
    {
        string dir = AbsoluteDir + "trailing ";
        var config = BuildConfig($$"""{ "Storage": { "Directory": {{JsonString(dir)}} } }""");

        var options = config.GetSection("Storage").Get<StorageOptions>().ShouldNotBeNull();

        var directory = options.Directory.ShouldNotBeNull();
        directory.ShouldBe(DirectoryPath.ParseAbsolute(dir, PathOptions.None));
        directory.IsValidFor(PathOptions.NoUnfriendlyNames, out string? error).ShouldBeFalse();
        error.ShouldBe("Entry name ends with a space.");
    }

    [TestMethod]
    public void Get_EmptyValueForAbsoluteType_Throws()
    {
        var config = BuildConfig("""{ "Storage": { "Directory": "" } }""");

        var ex = Should.Throw<InvalidOperationException>(() => config.GetSection("Storage").Get<StorageOptions>());

        ex.InnerException.ShouldBeOfType<FormatException>().Message.ShouldBe("Invalid absolute directory path '': Path is not an absolute path.");
    }

    [TestMethod]
    public void Bind_NullValue_SetsPropertyToNull()
    {
        var config = BuildConfig("""{ "Storage": { "Directory": null } }""");
        var options = new StorageOptions { Directory = DirectoryPath.ParseAbsolute(AbsoluteDir) };

        config.GetSection("Storage").Bind(options);

        options.Directory.ShouldBeNull();
    }

    [TestMethod]
    [DataRow(typeof(IAbsoluteDirectoryPath))]
    [DataRow(typeof(IAbsoluteFilePath))]
    [DataRow(typeof(IRelativeDirectoryPath))]
    [DataRow(typeof(IRelativeFilePath))]
    [DataRow(typeof(IDirectoryPath))]
    [DataRow(typeof(IFilePath))]
    public void TypeConverter_RoundTrips(Type type)
    {
        bool isDir = typeof(IDirectoryPath).IsAssignableFrom(type);
        bool isRelative = typeof(IRelativePath).IsAssignableFrom(type);
        string value = isRelative ? (isDir ? RelativeDir : RelativeFile) : (isDir ? AbsoluteDir : AbsoluteFile);

        var converter = TypeDescriptor.GetConverter(type);
        converter.CanConvertFrom(typeof(string)).ShouldBeTrue();
        converter.CanConvertTo(typeof(string)).ShouldBeTrue();

        var path = converter.ConvertFromInvariantString(value).ShouldBeAssignableTo<IPath>().ShouldNotBeNull();
        type.IsInstanceOfType(path).ShouldBeTrue();

        // Relative paths are written in the universal format.
        string expected = isRelative ? value.Replace('\\', '/') : value;
        expected = isDir ? expected + (isRelative ? '/' : Path.DirectorySeparatorChar) : expected;
        converter.ConvertToInvariantString(path).ShouldBe(expected);
        converter.ConvertFromInvariantString(expected).ShouldBe(path);
    }

    [TestMethod]
    [DataRow(typeof(IPath))]
    [DataRow(typeof(IAbsolutePath))]
    [DataRow(typeof(IRelativePath))]
    public void TypeConverter_GeneralTypes_ReadByShapeAndRoundTrip(Type type)
    {
        bool isRelative = type == typeof(IRelativePath);
        string dir = (isRelative ? RelativeDir : AbsoluteDir) + Path.DirectorySeparatorChar;
        string file = isRelative ? RelativeFile : AbsoluteFile;

        // Relative paths are written in the universal format.
        string writtenDir = isRelative ? dir.Replace('\\', '/') : dir;
        string writtenFile = isRelative ? file.Replace('\\', '/') : file;

        var converter = TypeDescriptor.GetConverter(type);
        converter.CanConvertFrom(typeof(string)).ShouldBeTrue();
        converter.CanConvertTo(typeof(string)).ShouldBeTrue();

        var dirPath = converter.ConvertFromInvariantString(dir).ShouldBeAssignableTo<IDirectoryPath>().ShouldNotBeNull();
        var filePath = converter.ConvertFromInvariantString(file).ShouldBeAssignableTo<IFilePath>().ShouldNotBeNull();
        type.IsInstanceOfType(dirPath).ShouldBeTrue();
        type.IsInstanceOfType(filePath).ShouldBeTrue();

        converter.ConvertToInvariantString(dirPath).ShouldBe(writtenDir);
        converter.ConvertToInvariantString(filePath).ShouldBe(writtenFile);
        converter.ConvertFromInvariantString(writtenDir).ShouldBe(dirPath);
        converter.ConvertFromInvariantString(writtenFile).ShouldBe(filePath);
    }

    [TestMethod]
    public void Get_BindsGeneralPathTypesByShape()
    {
        string dir = AbsoluteDir + Path.DirectorySeparatorChar;
        string relativeDir = RelativeDir + Path.DirectorySeparatorChar;

        var config = BuildConfig($$"""
            {
              "Storage": {
                "AnyPath": {{JsonString(dir)}},
                "AbsolutePath": {{JsonString(AbsoluteDir)}},
                "RelativePath": {{JsonString(relativeDir)}},
                "Paths": [ {{JsonString(RelativeFile)}}, {{JsonString(dir)}}, "" ]
              }
            }
            """);

        var options = config.GetSection("Storage").Get<StorageOptions>().ShouldNotBeNull();

        options.AnyPath.ShouldBeAssignableTo<IAbsoluteDirectoryPath>().ShouldBe(DirectoryPath.ParseAbsolute(dir));
        options.AbsolutePath.ShouldBeAssignableTo<IAbsoluteFilePath>().ShouldBe(FilePath.ParseAbsolute(AbsoluteDir));
        options.RelativePath.ShouldBeAssignableTo<IRelativeDirectoryPath>().ShouldBe(DirectoryPath.ParseRelative(relativeDir));
        options.Paths.ShouldBe([FilePath.ParseRelative(RelativeFile), DirectoryPath.ParseAbsolute(dir), DirectoryPath.ParseRelative("")]);
    }

    [TestMethod]
    public void Get_RelativeValueForGeneralAbsoluteType_Throws()
    {
        var config = BuildConfig($$"""{ "Storage": { "AbsolutePath": {{JsonString(RelativeFile)}} } }""");

        var ex = Should.Throw<InvalidOperationException>(() => config.GetSection("Storage").Get<StorageOptions>());

        ex.InnerException.ShouldBeOfType<FormatException>().Message.ShouldBe($"Invalid absolute path '{RelativeFile}': Path is not an absolute path.");
    }

    private static IConfigurationRoot BuildConfig(string json)
    {
        return new ConfigurationBuilder().AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(json))).Build();
    }

    public class StorageOptions
    {
        public IAbsoluteDirectoryPath? Directory { get; set; }

        public IAbsoluteFilePath? SavedRangesPath { get; set; }

        public IRelativeDirectoryPath? RelativeDirectory { get; set; }

        public IRelativeFilePath? RelativeFile { get; set; }

        public IDirectoryPath? AnyDirectory { get; set; }

        public IFilePath? AnyFile { get; set; }

        public IPath? AnyPath { get; set; }

        public IAbsolutePath? AbsolutePath { get; set; }

        public IRelativePath? RelativePath { get; set; }

        public List<IPath>? Paths { get; set; }
    }

    public class AppOptions
    {
        public StorageOptions Storage { get; set; } = new();

        public List<IAbsoluteDirectoryPath> Directories { get; set; } = [];

        public Dictionary<string, IAbsoluteFilePath> NamedFiles { get; set; } = [];
    }
}
