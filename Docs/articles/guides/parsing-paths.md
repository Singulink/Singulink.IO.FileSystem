<div class="article">

# Parsing Paths

### Overview

Every path in this library starts as a string that you parse into a strongly-typed object. Parsing happens through one of two static gateway classes, <xref:Singulink.IO.FilePath> and <xref:Singulink.IO.DirectoryPath>, and is the only place where path strings are validated. After parsing, no method ever silently rewrites or "fixes" a path.

## The Six Parse Methods

Each of <xref:Singulink.IO.FilePath> and <xref:Singulink.IO.DirectoryPath> exposes three parse methods. Pick the one that matches what you statically know about the input.

#### Parse: Auto Detect

Use <xref:Singulink.IO.FilePath.Parse*> or <xref:Singulink.IO.DirectoryPath.Parse*> when the input could be either absolute or relative.

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

Use <xref:Singulink.IO.FilePath.ParseAbsolute*> or <xref:Singulink.IO.DirectoryPath.ParseAbsolute*> when the input must be absolute. The return type is the specific absolute interface, so no cast is needed.

```csharp
IAbsoluteFilePath logFile = FilePath.ParseAbsolute(@"C:\Logs\app.log");
IAbsoluteDirectoryPath docs = DirectoryPath.ParseAbsolute("/var/data");
```

If the input is not actually absolute, an <xref:System.ArgumentException> is thrown.

#### ParseRelative

Use <xref:Singulink.IO.FilePath.ParseRelative*> or <xref:Singulink.IO.DirectoryPath.ParseRelative*> when the input must be relative.

```csharp
IRelativeFilePath relFile = FilePath.ParseRelative("config/app.json");
IRelativeDirectoryPath relDir = DirectoryPath.ParseRelative("../shared");
```

If the input is absolute, an <xref:System.ArgumentException> is thrown.

> [!TIP]
> Prefer <xref:Singulink.IO.FilePath.ParseAbsolute*> / <xref:Singulink.IO.FilePath.ParseRelative*> over <xref:Singulink.IO.FilePath.Parse*> whenever you know the expected kind. The static return type makes the rest of your code simpler and catches mismatches at parse time.

## Optional Parameters

Every parse method takes the same two optional parameters:

```csharp
FilePath.ParseAbsolute(path, format: PathFormat.Windows, options: PathOptions.NoUnfriendlyNames);
```

#### format: Which Format to Parse As

The default is <xref:Singulink.IO.PathFormat.Current>: Windows on Windows, Unix on Unix. Pass an explicit <xref:Singulink.IO.PathFormat> to parse paths from another platform or to use the cross-platform <xref:Singulink.IO.PathFormat.Universal>:

```csharp
// Parse a Unix path on Windows for manipulation/storage purposes:
var unixPath = FilePath.ParseRelative("home/user/notes.txt", PathFormat.Unix);

// Parse a path that must be portable across all platforms:
var portable = FilePath.ParseRelative("data/users.json", PathFormat.Universal);
```

See [Path Formats](path-formats.md) for the full story.

#### options: How Strict to Be

The default is <xref:Singulink.IO.PathOptions.NoUnfriendlyNames>, which rejects paths likely to cause trouble (trailing dots, leading/trailing spaces, reserved device names, control characters). Use <xref:Singulink.IO.PathOptions.None> to accept any technically valid path, for example when re-opening a path the user just selected in an OS file dialog.

See [PathOptions](path-options.md) for every flag and when each one is appropriate.

## What Parsing Validates

Parsing performs **all** of the following before returning a path:

- Separator normalization (e.g. forward slashes are converted to backslashes for the Windows format).
- Resolution of `.` and `..` segments where possible.
- Rejection of any path that navigates past the root (e.g. `C:\foo\..\..\bar` is an error).
- Rejection of empty segments (e.g. `a//b`) unless <xref:Singulink.IO.PathOptions.AllowEmptyDirectories> is set.
- Rejection of invalid characters for the format.
- Rejection of "unfriendly" patterns when <xref:Singulink.IO.PathOptions.NoUnfriendlyNames> is in effect.

> [!IMPORTANT]
> Parsing **never** alters named segments. If a path entry contains a trailing space, leading space or trailing dot, it is either rejected (per <xref:Singulink.IO.PathOptions>) or preserved exactly. This eliminates an entire class of bugs where the same path string appears to point at different entries depending on which API touched it last.

## File Path Specifics

<xref:Singulink.IO.FilePath> parsing additionally rejects:

- Empty path strings.
- Paths that end with a separator (e.g. `C:\Logs\` is a directory path, not a file).
- Paths that resolve to just a root with no file name.

```csharp
FilePath.ParseAbsolute(@"C:\Logs\");        // ArgumentException: no file name
FilePath.ParseRelative("");                 // ArgumentException: no file name
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

Both <xref:Singulink.IO.IPath.PathDisplay> and <xref:Singulink.IO.IAbsolutePath.PathExport> (on absolute paths) round-trip cleanly through the matching parse method:

```csharp
var original = FilePath.ParseAbsolute(@"C:\Data\users.json");
var copy = FilePath.ParseAbsolute(original.PathDisplay);
copy.Equals(original);   // true
```

<xref:Singulink.IO.IAbsolutePath.PathExport> is the only string form safe to hand to non-library APIs (such as the <xref:System.IO.FileStream> constructor). See [Path Formats](path-formats.md).

## Converting From FileInfo / DirectoryInfo

Use the <xref:Singulink.IO.SystemExtensions> extension methods to bridge from existing `System.IO` code:

```csharp
DirectoryInfo di = new(@"C:\temp ");      // System.IO trims the trailing space silently!
IAbsoluteDirectoryPath path = di.ToPath(); // Re-parses the (already-trimmed) FullName
```

> [!NOTE]
> <xref:Singulink.IO.SystemExtensions.ToPath*> parses <xref:System.IO.FileSystemInfo.FullName>, which `System.IO` may have already mutated (trimming trailing spaces and dots). The library cannot recover what `System.IO` discarded; pass strings directly to a parse method whenever the original characters matter.

## Next Steps

- [PathOptions](path-options.md): fine-grained control over what counts as a valid path.
- [Path Formats](path-formats.md): Windows vs Unix vs Universal.
- [Path Types](path-types.md): what to do with the parsed result.

</div>
