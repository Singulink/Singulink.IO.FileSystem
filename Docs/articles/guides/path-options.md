<div class="article">

# PathOptions

### Overview

<xref:Singulink.IO.PathOptions> is a `[Flags]` enum that controls how strictly path strings are validated during parsing. Every parse method, every `Combine*` overload that takes a string, and every extension-related method accepts a <xref:Singulink.IO.PathOptions> parameter that defaults to <xref:Singulink.IO.PathOptions.NoUnfriendlyNames>.

The default exists to protect you from path strings that are technically valid in some file systems but reliably cause trouble in real-world code. Loosen it only when you have a clear reason.

## Default: NoUnfriendlyNames

<xref:Singulink.IO.PathOptions.NoUnfriendlyNames> is the recommended default for almost all application code. It is a combination of:

- <xref:Singulink.IO.PathOptions.NoReservedDeviceNames>
- <xref:Singulink.IO.PathOptions.NoLeadingSpaces>
- <xref:Singulink.IO.PathOptions.NoTrailingSpaces>
- <xref:Singulink.IO.PathOptions.NoTrailingDots>
- <xref:Singulink.IO.PathOptions.NoControlCharacters>

Any of those patterns will cause parsing to throw <xref:System.ArgumentException> with a precise message about why the path was rejected.

```csharp
FilePath.ParseAbsolute(@"C:\data\report .pdf");   // throws: trailing space in "report "
FilePath.ParseAbsolute(@"C:\nul");                // throws: reserved device name
FilePath.ParseAbsolute(@"C:\data\file.");         // throws: trailing dot
```

> [!TIP]
> If your code is rejecting valid-looking paths and you don't know why, the exception message names the exact rule that fired. Use that to decide whether to fix the input or relax the option.

## Individual Flags

#### None

<xref:Singulink.IO.PathOptions.None> allows every path that is technically valid for the given format. Use this when you must work with whatever the file system contains, for example, paths returned by an OS file picker or paths read directly from an existing file system.

```csharp
string filePathString = openFileDialog.FileName;
var file = FilePath.ParseAbsolute(filePathString, PathOptions.None);
file.OpenStream();
```

> [!CAUTION]
> Don't store user-supplied "unfriendly" paths and re-display them later without care. Leading and trailing spaces are invisible in most UI; round-tripping a path string through a control that trims whitespace will produce a different path that points at a different file.

#### AllowEmptyDirectories

By default, `a//b` (two consecutive separators producing an empty segment) is rejected because it is almost always a bug, for example a missing variable expansion. Set <xref:Singulink.IO.PathOptions.AllowEmptyDirectories> to silently collapse the empty segment instead:

```csharp
DirectoryPath.ParseRelative("path/to//some/dir");                              // throws
DirectoryPath.ParseRelative("path/to//some/dir", PathOptions.AllowEmptyDirectories); // "path/to/some/dir"
```

#### NoReservedDeviceNames

<xref:Singulink.IO.PathOptions.NoReservedDeviceNames> rejects entry names that match Windows reserved device names: `CON`, `PRN`, `AUX`, `NUL`, `COM1`-`COM9`, `LPT1`-`LPT9`. Has no effect when parsing <xref:Singulink.IO.PathFormat.Unix>.

#### NoLeadingSpaces / NoTrailingSpaces

<xref:Singulink.IO.PathOptions.NoLeadingSpaces> and <xref:Singulink.IO.PathOptions.NoTrailingSpaces> reject entry names with a leading or trailing space. These names break Windows File Explorer, are difficult to handle in UI/serialization, and are a frequent source of silent failures in `System.IO`.

#### NoTrailingDots

<xref:Singulink.IO.PathOptions.NoTrailingDots> rejects entry names ending in `.`. Same rationale as above: Windows applications typically can't handle these reliably.

#### NoNavigation

<xref:Singulink.IO.PathOptions.NoNavigation> rejects any path that contains `.` or `..` segments and rejects rooted-relative paths (e.g. `\Some\Path` on Windows). Use this when you parse paths from untrusted input that must remain inside a known directory:

```csharp
// Reject "../../etc/passwd" style inputs up front:
var safe = FilePath.ParseRelative(userInput, PathOptions.NoUnfriendlyNames | PathOptions.NoNavigation);
```

> [!IMPORTANT]
> <xref:Singulink.IO.PathOptions.NoNavigation> is a parse-time check. It does **not** by itself sandbox a path. Combine the parsed relative path with a known-safe absolute base directory before doing any I/O.

#### NoControlCharacters

<xref:Singulink.IO.PathOptions.NoControlCharacters> rejects characters with ASCII codes 1-31 in entry names. Has no effect on <xref:Singulink.IO.PathFormat.Windows> (where these are always disallowed).

#### PathFormatDependent

<xref:Singulink.IO.PathOptions.PathFormatDependent> is a modifier flag. When set, <xref:Singulink.IO.PathOptions.NoUnfriendlyNames> rules get appended when the path's format is <xref:Singulink.IO.PathFormat.Windows> or <xref:Singulink.IO.PathFormat.Universal>. Use this when you want your code to be friendly to Unix's more permissive file system without losing protection on Windows or in stored cross-platform data.

```csharp
// Strict on Windows / Universal, lenient on Unix:
var opts = PathOptions.NoUnfriendlyNames | PathOptions.PathFormatDependent;
```

## Choosing the Right Options

| Scenario | Recommended options |
|----------|---------------------|
| App-defined path or user-typed input you'll store | <xref:Singulink.IO.PathOptions.NoUnfriendlyNames> (default) |
| Path returned from an OS file picker that you'll open and discard | <xref:Singulink.IO.PathOptions.None> |
| Path read from a file system enumeration | <xref:Singulink.IO.PathOptions.None> |
| Untrusted user input that must stay inside a known directory | <xref:Singulink.IO.PathOptions.NoUnfriendlyNames> + <xref:Singulink.IO.PathOptions.NoNavigation> |
| Cross-platform data that should be strict on Windows but accept everything Unix accepts | <xref:Singulink.IO.PathOptions.PathFormatDependent> |

## Storage and Round-Tripping

When you store paths, the options used to parse them later must accept everything they accepted originally; otherwise round-trips will fail:

```csharp
// At save time:
var path = FilePath.ParseAbsolute(input, PathOptions.None);
File.WriteAllText("path.txt", path.PathDisplay);

// At load time (must use the same options):
var path = FilePath.ParseAbsolute(File.ReadAllText("path.txt"), PathOptions.None);
```

> [!WARNING]
> If your storage layer trims whitespace (a database column with `TRIM`, an INI parser, etc.), do not use <xref:Singulink.IO.PathOptions.None>; the round-tripped string won't match the original path on the file system. Either constrain inputs to <xref:Singulink.IO.PathOptions.NoUnfriendlyNames> or guarantee verbatim storage.

## Next Steps

- [Parsing Paths](parsing-paths.md): where <xref:Singulink.IO.PathOptions> is applied.
- [Path Formats](path-formats.md): formats interact with options (e.g. <xref:Singulink.IO.PathOptions.PathFormatDependent>).

</div>
