using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Singulink.IO.FileSystem.Tests;

/// <summary>
/// Helpers shared by the test classes, imported into every test file with <c>global using static</c>.
/// </summary>
internal static class TestHelpers
{
    /// <summary>
    /// Gets the Windows, Unix and universal path formats.
    /// </summary>
    public static IReadOnlyList<PathFormat> Formats { get; } = [PathFormat.Windows, PathFormat.Unix, PathFormat.Universal];

    /// <summary>
    /// Gets the path format with the given name: <c>"Windows"</c>, <c>"Unix"</c> or <c>"Universal"</c>. Any other name throws, so a misspelled data row
    /// fails instead of running in another format.
    /// </summary>
    public static PathFormat GetFormat(string formatName) => formatName switch {
        "Windows" => PathFormat.Windows,
        "Unix" => PathFormat.Unix,
        "Universal" => PathFormat.Universal,
        _ => throw new ArgumentException($"Unknown format '{formatName}'.", nameof(formatName)),
    };

    /// <summary>
    /// Creates an empty directory with the given name in the current directory, deleting it and its contents first if it exists.
    /// </summary>
    public static IAbsoluteDirectoryPath CreateEmptyTestDirectory(string name)
    {
        var testDir = DirectoryPath.GetCurrent() + DirectoryPath.ParseRelative(name);

        if (testDir.Exists)
            testDir.Delete(true);

        testDir.Create();

        return testDir;
    }

    /// <summary>
    /// Runs the function and gives its result, or returns <see langword="false"/> if it throws <see cref="ArgumentException"/>.
    /// </summary>
    public static bool TryGet<T>(Func<T> func, [MaybeNullWhen(false)] out T result)
    {
        try
        {
            result = func();
            return true;
        }
        catch (ArgumentException)
        {
            result = default;
            return false;
        }
    }

    /// <summary>
    /// Runs the action, or returns <see langword="false"/> and gives the exception if it throws <see cref="ArgumentException"/>.
    /// </summary>
    public static bool TryRun(Action action, out ArgumentException? exception)
    {
        try
        {
            action();
            exception = null;
            return true;
        }
        catch (ArgumentException ex)
        {
            exception = ex;
            return false;
        }
    }

    /// <summary>
    /// Gets the string as a JSON string literal. Not named <c>Json</c>, which would resolve to the <c>Singulink.IO.Json</c> namespace in the tests.
    /// </summary>
    public static string JsonString(string value) => JsonSerializer.Serialize(value);
}
