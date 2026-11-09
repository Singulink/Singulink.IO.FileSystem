using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Singulink.IO.FileSystem.Tests;

[PrefixTestClass]
public partial class ValidPathAttributeTests
{
    private const string UnfriendlyError = "The Data directory field is not a valid path. Entry name ends with a space.";
    private const string NavigationError = "The File field is not a valid path. Invalid navigational path segment.";
    private const string NotPathError = "The Other field is not a valid path. Value of type String is not a path.";
    private const string CustomError = "Custom must be friendly (Entry name ends with a space.)";
    private const string ResourceError = "Path Resource is bad: Entry name ends with a dot.";

    [TestMethod]
    public void Validator_ValidAndNull_Succeeds()
    {
        var settings = new Settings {
            DataDir = DirectoryPath.ParseAbsolute(@"C:\data\", PathFormat.Windows),
            File = FilePath.ParseRelative("data/file.txt", PathFormat.Unix, PathOptions.None),
        };

        var results = new List<ValidationResult>();
        Validator.TryValidateObject(settings, new ValidationContext(settings), results, validateAllProperties: true).ShouldBeTrue();
        results.ShouldBeEmpty();
    }

    [TestMethod]
    public void Validator_Invalid_ReportsEveryMember()
    {
        var settings = GetInvalidSettings();

        var results = new List<ValidationResult>();
        Validator.TryValidateObject(settings, new ValidationContext(settings), results, validateAllProperties: true).ShouldBeFalse();

        results.Select(r => (r.ErrorMessage, r.MemberNames.Single())).ShouldBe([
            (UnfriendlyError, nameof(Settings.DataDir)),
            (NavigationError, nameof(Settings.File)),
            (NotPathError, nameof(Settings.Other)),
            (CustomError, nameof(Settings.Custom)),
            (ResourceError, nameof(Settings.Resource)),
        ], ignoreOrder: true);
    }

    [TestMethod]
    public void IsValid_Value()
    {
        var attribute = new ValidPathAttribute(PathOptions.NoUnfriendlyNames);

        attribute.IsValid(null).ShouldBeTrue();
        attribute.IsValid(DirectoryPath.ParseAbsolute(@"C:\data\", PathFormat.Windows)).ShouldBeTrue();
        attribute.IsValid(DirectoryPath.ParseAbsolute(@"C:\data \", PathFormat.Windows, PathOptions.None)).ShouldBeFalse();
        attribute.IsValid(@"C:\data\").ShouldBeFalse();
        attribute.IsValid(42).ShouldBeFalse();
    }

    [TestMethod]
    public void GetValidationResult_WithoutMemberName_HasNoMemberNames()
    {
        var attribute = new ValidPathAttribute(PathOptions.NoUnfriendlyNames);
        var path = FilePath.ParseRelative("name.", PathFormat.Unix, PathOptions.None);

        var result = attribute.GetValidationResult(path, new ValidationContext(path) { DisplayName = "Name" });

        result.ShouldNotBeNull();
        result.ErrorMessage.ShouldBe("The Name field is not a valid path. Entry name ends with a dot.");
        result.MemberNames.ShouldBeEmpty();
    }

    [TestMethod]
    public void GetValidationResult_NotPath_NamesValueType()
    {
        var attribute = new ValidPathAttribute(PathOptions.NoUnfriendlyNames);
        var context = new ValidationContext(new object()) { DisplayName = "Count", MemberName = "Count" };

        var result = attribute.GetValidationResult(42, context);

        result.ShouldNotBeNull();
        result.ErrorMessage.ShouldBe("The Count field is not a valid path. Value of type Int32 is not a path.");
        result.MemberNames.ShouldBe(["Count"]);
    }

    [TestMethod]
    public void FormatErrorMessage_HasNoReason()
    {
        new ValidPathAttribute(PathOptions.NoUnfriendlyNames).FormatErrorMessage("Name").ShouldBe("The Name field is not a valid path.");
        new ValidPathAttribute(PathOptions.None) { ErrorMessage = "{0}: {1}" }.FormatErrorMessage("Name").ShouldBe("Name:");
    }

    [TestMethod]
    public void Options_ValidateDataAnnotations()
    {
        var services = new ServiceCollection();
        services.AddOptions<Settings>().Configure(s => Copy(GetInvalidSettings(), s)).ValidateDataAnnotations();

        using var provider = services.BuildServiceProvider();
        var ex = Should.Throw<OptionsValidationException>(() => provider.GetRequiredService<IOptions<Settings>>().Value);

        ex.Failures.Count().ShouldBe(5);
        ex.Failures.ShouldContain(f => f.Contains(UnfriendlyError, StringComparison.Ordinal));
        ex.Failures.ShouldContain(f => f.Contains(NavigationError, StringComparison.Ordinal));
        ex.Failures.ShouldContain(f => f.Contains(NotPathError, StringComparison.Ordinal));
        ex.Failures.ShouldContain(f => f.Contains(CustomError, StringComparison.Ordinal));
        ex.Failures.ShouldContain(f => f.Contains(ResourceError, StringComparison.Ordinal));
    }

    [TestMethod]
    public void Options_ValidateDataAnnotations_Valid()
    {
        var services = new ServiceCollection();
        services.AddOptions<Settings>().Configure(s => s.DataDir = DirectoryPath.ParseAbsolute(@"C:\data\", PathFormat.Windows)).ValidateDataAnnotations();

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IOptions<Settings>>().Value.DataDir.ShouldNotBeNull();
    }

    [TestMethod]
    public void Options_SourceGeneratedValidator()
    {
        var validator = new SettingsValidator();

        // The generated validator chooses the display names and how failures are worded, so only the member names and our reasons are checked.
        var result = validator.Validate(null, GetInvalidSettings());
        result.Failed.ShouldBeTrue();
        result.Failures.Count().ShouldBe(5);

        (string Member, string Reason)[] expected = [
            (nameof(Settings.DataDir), "is not a valid path. Entry name ends with a space."),
            (nameof(Settings.File), "is not a valid path. Invalid navigational path segment."),
            (nameof(Settings.Other), "is not a valid path. Value of type String is not a path."),
            (nameof(Settings.Custom), "must be friendly (Entry name ends with a space.)"),
            (nameof(Settings.Resource), "is bad: Entry name ends with a dot."),
        ];

        foreach (var (member, reason) in expected)
        {
            result.Failures.ShouldContain(
                f => f.Contains(member, StringComparison.Ordinal) && f.Contains(reason, StringComparison.Ordinal), $"{member}: {reason}");
        }

        validator.Validate(null, new Settings()).Succeeded.ShouldBeTrue();
    }

    private static Settings GetInvalidSettings() => new() {
        DataDir = DirectoryPath.ParseAbsolute(@"C:\data \", PathFormat.Windows, PathOptions.None),
        File = FilePath.ParseRelative(@"..\file.txt", PathFormat.Windows),
        Other = @"C:\data\",
        Custom = DirectoryPath.ParseAbsolute("/data /", PathFormat.Unix, PathOptions.None),
        Resource = FilePath.ParseRelative("name.", PathFormat.Unix, PathOptions.None),
    };

    private static void Copy(Settings source, Settings destination)
    {
        destination.DataDir = source.DataDir;
        destination.File = source.File;
        destination.Other = source.Other;
        destination.Custom = source.Custom;
        destination.Resource = source.Resource;
    }

    public sealed class Settings
    {
        [Display(Name = "Data directory")]
        [ValidPath(PathOptions.NoUnfriendlyNames)]
        public IAbsoluteDirectoryPath? DataDir { get; set; }

        [ValidPath(PathOptions.NoNavigation)]
        public IRelativeFilePath? File { get; set; }

        [ValidPath(PathOptions.NoUnfriendlyNames)]
        public object? Other { get; set; }

        [ValidPath(PathOptions.NoUnfriendlyNames, ErrorMessage = "{0} must be friendly ({1})")]
        public IPath? Custom { get; set; }

        [ValidPath(PathOptions.NoTrailingDots, ErrorMessageResourceType = typeof(Messages), ErrorMessageResourceName = nameof(Messages.BadPath))]
        public IPath? Resource { get; set; }
    }

    public static class Messages
    {
        // ErrorMessageResourceType reads the message from a public static string property.
        public static string BadPath => "Path {0} is bad: {1}";
    }

    [OptionsValidator]
    public sealed partial class SettingsValidator : IValidateOptions<Settings>;
}
