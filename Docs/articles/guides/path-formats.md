<div class="article">

# Path Formats

### Overview

Every path carries a <xref:Singulink.IO.PathFormat> describing the rules it follows: separator character, what counts as a valid entry name, whether absolute or rooted-relative paths are allowed, and so on. The library exposes three format singletons plus <xref:Singulink.IO.PathFormat.Current>.

Choosing the right format up front lets you parse paths from any platform, store paths in a portable way, and convert between formats explicitly.

## The Three Formats

#### PathFormat.Windows

<xref:Singulink.IO.PathFormat.Windows> provides native Windows path handling. Backslash separator (forward slashes are normalized to backslashes during parsing). Supports drive-letter roots (`C:\...`), UNC roots (`\\server\share\...`) and rooted-relative paths (`\Some\Path`).

#### PathFormat.Unix

<xref:Singulink.IO.PathFormat.Unix> provides native Unix path handling. Forward-slash separator. Supports absolute paths (`/var/data`) and non-rooted relative paths (`some/path`). Rooted-relative paths are not supported; anything starting with `/` is absolute.

#### PathFormat.Universal

<xref:Singulink.IO.PathFormat.Universal> is a cross-platform format that only allows constructs valid on **every** platform:

- Forward-slash separator only (backslashes are rejected, since they're valid filename characters on Unix).
- **Only relative, non-rooted paths.** Absolute paths and rooted-relative paths are platform concepts and don't belong in the universal format.
- Strict entry name validation that rejects anything Windows would object to.

> [!TIP]
> Use <xref:Singulink.IO.PathFormat.Universal> when storing relative paths in databases, configuration files or any data that may be read from another platform. The format guarantees the stored string will parse and behave identically everywhere.

#### PathFormat.Current

<xref:Singulink.IO.PathFormat.Current> returns <xref:Singulink.IO.PathFormat.Windows> on Windows and <xref:Singulink.IO.PathFormat.Unix> on Unix-based platforms (Linux, macOS, etc.). This is the default for every parse method when no format is specified.

## Format Restrictions on I/O

File system operations (such as <xref:Singulink.IO.IAbsoluteFilePath.OpenStream*>, <xref:Singulink.IO.IAbsoluteDirectoryPath.Create*>, <xref:Singulink.IO.IAbsoluteDirectoryPath.Delete*>, enumeration, etc.) only work on absolute paths whose <xref:Singulink.IO.IPath.PathFormat> matches <xref:Singulink.IO.PathFormat.Current>. Trying to perform I/O on a non-current path throws an <xref:System.InvalidOperationException> (or <xref:System.ArgumentException> when the path is a method argument).

```csharp
var winPath = FilePath.ParseAbsolute(@"C:\data\file.txt", PathFormat.Windows);
winPath.OpenStream();   // works on Windows; throws on Unix
```

> [!IMPORTANT]
> Parsing a path with a non-current format is purely for manipulation, conversion or storage. To actually access the file system, the path must be in the current platform's format.

## Format Conversion

Relative paths can be converted between formats with <xref:Singulink.IO.IRelativePath.ToPathFormat*>:

```csharp
IRelativeFilePath universal = FilePath.ParseRelative("data/users.json", PathFormat.Universal);
IRelativeFilePath windows = universal.ToPathFormat(PathFormat.Windows);    // "data\users.json"
IRelativeFilePath unix    = universal.ToPathFormat(PathFormat.Unix);       // "data/users.json"
```

Conversion may throw <xref:System.ArgumentException> if the path can't be represented in the target format. For example, a Unix path containing characters that are valid as filename characters on Unix but reserved on Windows:

```csharp
var unixPath = FilePath.ParseRelative("some/file?.txt", PathFormat.Unix, PathOptions.None);
winPath.ToPathFormat(PathFormat.Windows);   // throws: '?' is not valid in Windows
```

> [!NOTE]
> Absolute paths are platform-specific by definition and cannot be format-converted. To "move" an absolute path to another platform's format, take its relative remainder (relative to a known root) and convert that.

## Combining Across Formats

When combining a directory with a relative path, the formats are reconciled as follows:

| Directory format  | Relative format   | Result format     |
|-------------------|-------------------|-------------------|
| Windows           | Windows           | Windows           |
| Unix              | Unix              | Unix              |
| Universal         | Universal         | Universal         |
| Windows           | Universal         | Windows           |
| Unix              | Universal         | Unix              |
| Windows           | Unix              | **error**         |
| Unix              | Windows           | **error**         |

In short: matching formats win, <xref:Singulink.IO.PathFormat.Universal> yields to whichever specific format is on the other side, and mixing two specific formats is an error. This means a <xref:Singulink.IO.PathFormat.Universal> relative path is freely combinable with any platform-specific directory.

```csharp
IRelativeFilePath cfg = FilePath.ParseRelative("config/app.json", PathFormat.Universal);
IAbsoluteDirectoryPath baseDir = DirectoryPath.GetAppBase();   // current format
IAbsoluteFilePath fullPath = baseDir + cfg;                    // current format
```

## Three String Forms

Every path has three string representations. Use the right one for the job.

#### PathDisplay

<xref:Singulink.IO.IPath.PathDisplay> is friendly and human-readable. Suitable for:

- Display in UI, logs and error messages.
- Storage and serialization that you'll re-parse with this library.

<xref:Singulink.IO.IPath.PathDisplay> round-trips cleanly through the matching parse method.

```csharp
file.PathDisplay;   // "C:\Apps\MyApp\config.json"
dir.PathDisplay;    // "C:\Apps\MyApp\"   (note the trailing separator)
```

> [!NOTE]
> Non-empty directory paths always end with the format's separator in both <xref:Singulink.IO.IPath.PathDisplay> and <xref:Singulink.IO.IAbsolutePath.PathExport>; file paths never do. Empty relative directory paths (`PathDisplay == ""`) are the one exception: there is no segment to suffix. This invariant serves two purposes. First, it makes a path's textual form unambiguously declare whether it points to a file or a directory, so directory and file strings remain distinguishable when they cross out of the type system (logs, config, databases, etc.). Second, it makes raw string concatenation safe: an absolute directory's string can be concatenated with any number of relative directory strings and an optional trailing file name string to produce a valid path, with no need to insert or de-duplicate separators between segments.

#### PathExport (absolute paths only)

<xref:Singulink.IO.IAbsolutePath.PathExport> is specially formatted for handing to non-library APIs. On Windows, this typically prefixes with the `\\?\` extended-path syntax so the file system never silently mutates the path (no whitespace trimming, no reserved-name remapping).

Use <xref:Singulink.IO.IAbsolutePath.PathExport> whenever you need a string for `System.IO`, native interop, or any third-party API that takes a path string:

```csharp
using var stream = new FileStream(file.PathExport, FileMode.Open);
```

> [!IMPORTANT]
> Use <xref:Singulink.IO.IAbsolutePath.PathExport>, never <xref:Singulink.IO.IPath.PathDisplay>, when calling APIs outside this library. <xref:Singulink.IO.IPath.PathDisplay> looks normal but can be silently rewritten by lower-level path handling. <xref:Singulink.IO.IAbsolutePath.PathExport> cannot.

#### ToString()

<xref:Singulink.IO.IPath.ToString*> returns a deliberately unusable diagnostic string of the form `[Format] "<pathDisplay>"`. Useful in debug output, exceptions and logs, but **never pass it to anything that expects a path**.

```csharp
file.ToString();   // [Windows] "C:\Apps\MyApp\config.json"
```

## Format-Aware Members

A few <xref:Singulink.IO.PathFormat> members are useful from application code:

- <xref:Singulink.IO.PathFormat.Separator>: the format's path separator character.
- <xref:Singulink.IO.PathFormat.SupportsRelativeRootedPaths>: `true` for Windows only.
- <xref:Singulink.IO.PathFormat.RelativeCurrentDirectory>, <xref:Singulink.IO.PathFormat.RelativeParentDirectory>: pre-built `.` and `..` paths in the format.
- <xref:Singulink.IO.PathFormat.IsValidExtension*>: validate a file extension against the format's rules.

```csharp
PathFormat.Windows.Separator;                        // '\\'
PathFormat.Universal.IsValidExtension(".tar.gz");    // false: multiple dots
PathFormat.Universal.IsValidExtension(".gz");        // true
```

## Next Steps

- [PathOptions](path-options.md): interacts with <xref:Singulink.IO.PathFormat> (e.g. <xref:Singulink.IO.PathOptions.PathFormatDependent>).
- [Combining and Navigating Paths](combining-and-navigating.md): cross-format combine rules in practice.
- [Interop and Migration](interop-and-migration.md): when to use <xref:Singulink.IO.IAbsolutePath.PathExport>.

</div>
