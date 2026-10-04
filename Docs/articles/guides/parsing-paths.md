<div class="article">

# Parsing Paths

Every path in this library starts as a string that you parse into a strongly-typed object. Parsing happens through one of two static gateway classes, <xref:Singulink.IO.FilePath> and <xref:Singulink.IO.DirectoryPath>, and is the only place where path strings are validated. After parsing, no method ever silently rewrites or "fixes" a path.

## The Parse Methods

Each of <xref:Singulink.IO.FilePath> and <xref:Singulink.IO.DirectoryPath> exposes three parse methods. Pick the one that matches what you statically know about the input. All of them throw <xref:System.ArgumentException> when the input is invalid, and each has a non-throwing counterpart covered in [Parsing Without Exceptions](#parsing-without-exceptions) below.

#### Parse: Auto Detect

Use <xref:Singulink.IO.FilePath.Parse*?displayProperty=nameWithType> or <xref:Singulink.IO.DirectoryPath.Parse*?displayProperty=nameWithType> when the input could be either absolute or relative.

```csharp
IFilePath file = FilePath.Parse(userInput);
IDirectoryPath dir = DirectoryPath.Parse(userInput);
```

The return type is the broader interface (<xref:Singulink.IO.IFilePath> or <xref:Singulink.IO.IDirectoryPath>). Pattern match on the result to recover the specific type:

```csharp
if (file is IAbsoluteFilePath absolute) { /* ... */ }
else { /* it's IRelativeFilePath */ }
```

#### ParseAbsolute

Use <xref:Singulink.IO.FilePath.ParseAbsolute*?displayProperty=nameWithType> or <xref:Singulink.IO.DirectoryPath.ParseAbsolute*?displayProperty=nameWithType> when the input must be absolute. The return type is the specific absolute interface, so no cast is needed.

```csharp
IAbsoluteFilePath logFile = FilePath.ParseAbsolute(@"C:\Logs\app.log");
IAbsoluteDirectoryPath docs = DirectoryPath.ParseAbsolute("/var/data");
```

If the input is not actually absolute, an <xref:System.ArgumentException> is thrown.

#### ParseRelative

Use <xref:Singulink.IO.FilePath.ParseRelative*?displayProperty=nameWithType> or <xref:Singulink.IO.DirectoryPath.ParseRelative*?displayProperty=nameWithType> when the input must be relative.

```csharp
IRelativeFilePath relFile = FilePath.ParseRelative("config/app.json");
IRelativeDirectoryPath relDir = DirectoryPath.ParseRelative("../shared");
```

If the input is absolute, an <xref:System.ArgumentException> is thrown.

> [!TIP]
> Prefer <xref:Singulink.IO.FilePath.ParseAbsolute*?displayProperty=nameWithType> / <xref:Singulink.IO.FilePath.ParseRelative*?displayProperty=nameWithType> over <xref:Singulink.IO.FilePath.Parse*?displayProperty=nameWithType> whenever you know the expected kind. The static return type makes the rest of your code simpler and catches mismatches at parse time.

## Parsing Without Exceptions

Every parse method has a `Try` counterpart that returns `false` instead of throwing when the input is invalid:

| Throwing | Non-throwing |
|----------|--------------|
| <xref:Singulink.IO.FilePath.Parse*?displayProperty=nameWithType> | <xref:Singulink.IO.FilePath.TryParse*?displayProperty=nameWithType> |
| <xref:Singulink.IO.FilePath.ParseAbsolute*?displayProperty=nameWithType> | <xref:Singulink.IO.FilePath.TryParseAbsolute*?displayProperty=nameWithType> |
| <xref:Singulink.IO.FilePath.ParseRelative*?displayProperty=nameWithType> | <xref:Singulink.IO.FilePath.TryParseRelative*?displayProperty=nameWithType> |
| <xref:Singulink.IO.DirectoryPath.Parse*?displayProperty=nameWithType> | <xref:Singulink.IO.DirectoryPath.TryParse*?displayProperty=nameWithType> |
| <xref:Singulink.IO.DirectoryPath.ParseAbsolute*?displayProperty=nameWithType> | <xref:Singulink.IO.DirectoryPath.TryParseAbsolute*?displayProperty=nameWithType> |
| <xref:Singulink.IO.DirectoryPath.ParseRelative*?displayProperty=nameWithType> | <xref:Singulink.IO.DirectoryPath.TryParseRelative*?displayProperty=nameWithType> |

Use them when a failed parse is an ordinary outcome, such as text typed by a user or values read from a file that may be malformed:

```csharp
if (FilePath.TryParseAbsolute(userInput, out IAbsoluteFilePath? file))
{
    // file is a fully validated path here
}
else
{
    // userInput is not a valid absolute file path
}
```

The parsed path is returned through the `out` parameter, which is `null` whenever the method returns `false`. The parameter is annotated so that the compiler treats it as non-null after a successful call, so no null-forgiving operator is needed inside the `if` block.

The return types mirror the throwing methods, so the auto-detecting variants still pair naturally with pattern matching:

```csharp
if (DirectoryPath.TryParse(userInput, out IDirectoryPath? dir) && dir is IAbsoluteDirectoryPath absoluteDir)
    absoluteDir.Create();
```

Both families run the same parsing code. A string is accepted by a `Try` method exactly when the matching throwing method accepts it, and the two produce equal paths.

> [!NOTE]
> The `Try` methods report only success or failure. When you need to tell a user *why* a path was rejected, call the throwing method and show the <xref:System.Exception.Message?displayProperty=nameWithType> of the <xref:System.ArgumentException>, which names the exact rule that failed. See [Exception Handling](exception-handling.md#parse-time-errors).

## Format and Options

Every parse method accepts a path format and a set of path options. The throwing methods take them as optional parameters:

```csharp
FilePath.ParseAbsolute(path, format: PathFormat.Windows, options: PathOptions.NoUnfriendlyNames);
```

The `Try` methods take the same two values through overloads, placed between the input string and the `out` parameter:

```csharp
FilePath.TryParseAbsolute(path, out var file);
FilePath.TryParseAbsolute(path, PathFormat.Windows, out file);
FilePath.TryParseAbsolute(path, PathOptions.None, out file);
FilePath.TryParseAbsolute(path, PathFormat.Windows, PathOptions.None, out file);
```

The defaults are the same for both families.

#### Format: Which Format to Parse As

The default is <xref:Singulink.IO.PathFormat.Current?displayProperty=nameWithType>: Windows on Windows, Unix on Unix. Pass an explicit <xref:Singulink.IO.PathFormat> to parse paths from another platform or to use the cross-platform <xref:Singulink.IO.PathFormat.Universal?displayProperty=nameWithType>:

```csharp
// Parse a Unix path on Windows for manipulation/storage purposes:
var unixPath = FilePath.ParseRelative("home/user/notes.txt", PathFormat.Unix);

// Parse a path that must be portable across all platforms:
var portable = FilePath.ParseRelative("data/users.json", PathFormat.Universal);
```

See [Path Formats](path-formats.md) for the full story.

#### Options: How Strict to Be

The default is <xref:Singulink.IO.PathOptions.NoUnfriendlyNames?displayProperty=nameWithType>, which rejects paths likely to cause trouble (trailing dots, leading/trailing spaces, reserved device names, control characters). Use <xref:Singulink.IO.PathOptions.None?displayProperty=nameWithType> to accept any technically valid path, for example when re-opening a path the user just selected in an OS file dialog.

See [PathOptions](path-options.md) for every flag and when each one is appropriate.

## What Parsing Validates

Parsing performs **all** of the following before returning a path:

- Separator normalization (e.g. forward slashes are converted to backslashes for the Windows format).
- Resolution of `.` and `..` segments where possible.
- Rejection of any path that navigates past the root (e.g. `C:\foo\..\..\bar` is an error).
- Rejection of empty segments (e.g. `a//b`) unless <xref:Singulink.IO.PathOptions.AllowEmptyDirectories?displayProperty=nameWithType> is set.
- Rejection of invalid characters for the format.
- Rejection of "unfriendly" patterns when <xref:Singulink.IO.PathOptions.NoUnfriendlyNames?displayProperty=nameWithType> is in effect.

> [!IMPORTANT]
> Parsing **never** alters named segments. If a path entry contains a trailing space, leading space or trailing dot, it is either rejected (per <xref:Singulink.IO.PathOptions>) or preserved exactly. This eliminates an entire class of bugs where the same path string appears to point at different entries depending on which API touched it last.

## File and Directory Specifics

<xref:Singulink.IO.FilePath> parsing additionally rejects any string that cannot name a file:

- Empty path strings.
- Paths that end with a separator (e.g. `C:\Logs\` is a directory path, not a file).
- Paths that end with a `.` or `..` segment, which can only refer to a directory.
- Paths that resolve to just a root with no file name.

```csharp
FilePath.ParseAbsolute(@"C:\Logs\");        // ArgumentException: no file name
FilePath.ParseRelative("");                 // ArgumentException: no file name
FilePath.ParseRelative("logs/..");          // ArgumentException: no file name
```

<xref:Singulink.IO.DirectoryPath> parsing accepts a directory written with or without a trailing separator. Both forms produce the same path, and its <xref:Singulink.IO.IPath.PathDisplay?displayProperty=nameWithType> always ends with the separator:

```csharp
var a = DirectoryPath.ParseAbsolute(@"C:\Logs");
var b = DirectoryPath.ParseAbsolute(@"C:\Logs\");

a.Equals(b);     // true
a.PathDisplay;   // "C:\Logs\"
```

## Past-Root Navigation

Walking past the root is always an error, regardless of how the navigation segments were arranged:

```csharp
DirectoryPath.ParseAbsolute(@"C:\foo\..\..\bar");   // ArgumentException
DirectoryPath.ParseRelative("../../..", PathFormat.Universal);  // OK: relative paths can keep ascending
```

> [!CAUTION]
> Past-root navigation is a frequent source of silent bugs in `System.IO`. The library treats it as a parse-time error so the problem surfaces immediately rather than producing a path that quietly resolves to the wrong location.

## Round-Trip Parsing

Both <xref:Singulink.IO.IPath.PathDisplay?displayProperty=nameWithType> and <xref:Singulink.IO.IAbsolutePath.PathExport?displayProperty=nameWithType> (on absolute paths) round-trip cleanly through the matching parse method:

```csharp
var original = FilePath.ParseAbsolute(@"C:\Data\users.json");
var copy = FilePath.ParseAbsolute(original.PathDisplay);
copy.Equals(original);   // true
```

<xref:Singulink.IO.IAbsolutePath.PathExport> is the only string form safe to hand to non-library APIs (such as the <xref:System.IO.FileStream> constructor). See [Path Formats](path-formats.md#three-string-forms).

## Converting From FileInfo / DirectoryInfo

Use the <xref:Singulink.IO.SystemExtensions> extension methods to bridge from existing `System.IO` code:

```csharp
DirectoryInfo di = new(@"C:\temp ");      // System.IO trims the trailing space silently!
IAbsoluteDirectoryPath path = di.ToPath(); // Re-parses the (already-trimmed) FullName
```

> [!NOTE]
> <xref:Singulink.IO.SystemExtensions.ToPath*?displayProperty=nameWithType> parses <xref:System.IO.FileSystemInfo.FullName?displayProperty=nameWithType>, which `System.IO` may have already mutated (trimming trailing spaces and dots). The library cannot recover what `System.IO` discarded; pass strings directly to a parse method whenever the original characters matter.

## Next Steps

These guides cover the topics that parsing touches on:

- [PathOptions](path-options.md): fine-grained control over what counts as a valid path.
- [Path Formats](path-formats.md): Windows vs Unix vs Universal.
- [Path Types](path-types.md): what to do with the parsed result.
- [Exception Handling](exception-handling.md): handling parse failures alongside I/O failures.

</div>
