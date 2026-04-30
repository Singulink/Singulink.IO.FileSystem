<div class="article">

# Getting Started

### Overview

**Singulink.IO.FileSystem** provides strongly-typed file and directory paths together with reliable, cross-platform file system access for .NET. Compared to `System.IO`, it eliminates entire categories of bugs (silent path modification, file/directory confusion, inconsistent exceptions, cross-platform mismatches) by encoding intent in the type system and making parsing explicit and separate from I/O.

This article walks you through installation, the core concepts and a small end-to-end example. From here you can branch off into the rest of the guides for in-depth coverage of any topic.

### Installation

Install the package from NuGet:

```
dotnet add package Singulink.IO.FileSystem
```

**Supported runtimes**: .NET 8.0+

All public types live under the <xref:Singulink.IO> namespace.

## Core Concepts

The library is built around four ideas:

1. **Paths are strongly typed.** A path is never just a <xref:System.String>. It is one of four concrete interfaces: <xref:Singulink.IO.IAbsoluteFilePath>, <xref:Singulink.IO.IAbsoluteDirectoryPath>, <xref:Singulink.IO.IRelativeFilePath> or <xref:Singulink.IO.IRelativeDirectoryPath>.
2. **Parsing is separate from I/O.** Path strings are validated and converted into path objects up-front. Once you have a path object, the I/O API surface is small and predictable.
3. **File system operations require absolute paths.** Relative paths can be combined, navigated and converted but cannot directly access the file system. This forces you to make explicit what a relative path is relative to.
4. **No silent path modification.** The library never trims, drops or "cleans up" the named segments of a path. Trailing dots, leading spaces and reserved device names are either rejected (default) or preserved exactly (when explicitly opted in).

#### The Path Hierarchy

Every concrete path is one of these four leaf interfaces:

| Path kind | Implements |
|-----------|------------|
| <xref:Singulink.IO.IAbsoluteFilePath> | <xref:Singulink.IO.IAbsolutePath> + <xref:Singulink.IO.IFilePath> |
| <xref:Singulink.IO.IAbsoluteDirectoryPath> | <xref:Singulink.IO.IAbsolutePath> + <xref:Singulink.IO.IDirectoryPath> |
| <xref:Singulink.IO.IRelativeFilePath> | <xref:Singulink.IO.IRelativePath> + <xref:Singulink.IO.IFilePath> |
| <xref:Singulink.IO.IRelativeDirectoryPath> | <xref:Singulink.IO.IRelativePath> + <xref:Singulink.IO.IDirectoryPath> |

<xref:Singulink.IO.IAbsolutePath> and <xref:Singulink.IO.IRelativePath> capture the absolute/relative axis; <xref:Singulink.IO.IFilePath> and <xref:Singulink.IO.IDirectoryPath> capture the file/directory axis. All of them extend <xref:Singulink.IO.IPath>. Each concrete path inherits members from both axes, and most of your code will work directly with one of the four leaf interfaces. See [Path Types](path-types.md) for the full member breakdown.

## A 30-Second Tour

#### Parse a Path

```csharp
using Singulink.IO;

IAbsoluteFilePath configFile = FilePath.ParseAbsolute(@"C:\Apps\MyApp\config.json");
IRelativeFilePath relativeLog = FilePath.ParseRelative("logs/today.log");
```

#### Combine Paths

```csharp
IAbsoluteDirectoryPath appBase = DirectoryPath.GetAppBase();
IAbsoluteFilePath logFile = appBase + relativeLog;          // operator +
IAbsoluteFilePath dataFile = appBase.CombineFile("data/users.json");
```

#### Do I/O

```csharp
logFile.ParentDirectory.Create();                            // ensure parent exists

using (FileStream stream = logFile.OpenStream(FileMode.Append, FileAccess.Write))
{
    // write to the file
}

if (dataFile.Exists)
    Console.WriteLine($"Size: {dataFile.Length} bytes");
```

#### Enumerate

```csharp
foreach (IAbsoluteFilePath csFile in appBase.GetChildFiles("*.cs", new SearchOptions { Recursive = true }))
    Console.WriteLine(csFile.PathDisplay);
```

## End-to-End Example

The following example loads a config file relative to the application base, processes it, and writes results to a temp file:

```csharp
using Singulink.IO;

// Resolve "config/app.json" relative to the application's base directory.
IAbsoluteDirectoryPath appBase = DirectoryPath.GetAppBase();
IAbsoluteFilePath configFile = appBase.CombineFile("config/app.json");

if (configFile.State is not EntryState.Exists)
    throw new FileNotFoundException($"Missing config file: {configFile.PathDisplay}");

// Read the config.
string json;
using (StreamReader reader = new(configFile.OpenStream(FileMode.Open, FileAccess.Read, FileShare.Read)))
    json = reader.ReadToEnd();

// Write a result to a uniquely named temp file.
IAbsoluteFilePath resultFile = FilePath.CreateTempFile();

using (StreamWriter writer = new(resultFile.OpenStream(FileMode.Truncate, FileAccess.Write)))
    writer.Write(Process(json));

Console.WriteLine($"Wrote results to: {resultFile.PathDisplay}");
```

> [!TIP]
> Use <xref:Singulink.IO.IPath.PathDisplay> for messages, logs and serialization. Use <xref:Singulink.IO.IAbsolutePath.PathExport> only when you need a string for an external API. Never use <xref:System.Object.ToString*> for I/O; its output is intentionally not a usable path. See [Path Formats](path-formats.md) for the full story.

## Next Steps

Each subsequent guide takes one slice of the library and covers it in depth:

- [Path Types](path-types.md): the seven path interfaces and what each one offers.
- [Parsing Paths](parsing-paths.md): how to turn strings into paths reliably.
- [PathOptions](path-options.md): controlling what counts as a valid path.
- [Path Formats](path-formats.md): Windows, Unix and the cross-platform Universal format.
- [Combining and Navigating Paths](combining-and-navigating.md), [File Names and Extensions](file-names-and-extensions.md), [Special Locations](special-locations.md).
- [Working with Files](file-operations.md), [Working with Directories](directory-operations.md), [Searching and Enumeration](searching-and-enumeration.md).
- [Drive and Disk Information](drive-and-disk-info.md), [Cached Entry Info](cached-entry-info.md), [Exception Handling](exception-handling.md), [Interop and Migration](interop-and-migration.md).

If you're coming from `System.IO` and want a quick sense of what's broken there and why, see [Problems with System.IO](../system.io/problems-with-system-io.md).

</div>
