<div class="article">

# Combining and Navigating Paths

### Overview

Paths are immutable. Every "modification" returns a new path object. The library offers three ways to build new paths from existing ones:

- The `+` operator for concise combine expressions.
- <xref:Singulink.IO.IDirectoryPath.Combine*>, <xref:Singulink.IO.IDirectoryPath.CombineDirectory*> and <xref:Singulink.IO.IDirectoryPath.CombineFile*> methods with explicit overloads for strings or pre-parsed paths.
- Navigation members (<xref:Singulink.IO.IPath.ParentDirectory>, <xref:Singulink.IO.IAbsolutePath.RootDirectory>) for walking up.

All combinations preserve type strength: combining an absolute directory with a relative file produces an absolute file, and so on.

## The + Operator

<xref:Singulink.IO.IDirectoryPath> defines `+` overloads for every relative path type:

```csharp
IAbsoluteDirectoryPath baseDir = DirectoryPath.GetAppBase();
IRelativeDirectoryPath subDir = DirectoryPath.ParseRelative("logs");
IRelativeFilePath logFile = FilePath.ParseRelative("today.log");

IAbsoluteDirectoryPath logsDir = baseDir + subDir;       // IAbsoluteDirectoryPath
IAbsoluteFilePath fullLogPath  = logsDir + logFile;      // IAbsoluteFilePath
IAbsoluteFilePath inOneStep    = baseDir + (subDir + logFile);
```

Return types are inferred from the operands. Absolute + relative produces absolute; relative + relative produces relative.

## Combine With a Pre-Parsed Path

When you already have a relative path object, use <xref:Singulink.IO.IDirectoryPath.Combine*>:

```csharp
IAbsoluteFilePath result = baseDir.Combine(logFile);
```

This is identical to the `+` operator, just spelled out.

## Combine With a String

When the relative segment is a string (e.g. read from configuration), use <xref:Singulink.IO.IDirectoryPath.CombineDirectory*> or <xref:Singulink.IO.IDirectoryPath.CombineFile*>. These overloads parse the string in one step:

```csharp
IAbsoluteFilePath cfgFile = baseDir.CombineFile("config/app.json");
IAbsoluteDirectoryPath dataDir = baseDir.CombineDirectory("data");
```

By default, the string is parsed using the parent's <xref:Singulink.IO.IPath.PathFormat> and <xref:Singulink.IO.PathOptions.NoUnfriendlyNames>. Override either:

```csharp
IAbsoluteFilePath portable = baseDir.CombineFile("data/users.json", PathFormat.Universal);
IAbsoluteDirectoryPath relaxed = baseDir.CombineDirectory(rawDir, PathOptions.None);
```

> [!TIP]
> If you'll combine the same relative path more than once, parse it into an <xref:Singulink.IO.IRelativePath> once and reuse it. Combining a parsed relative path is cheaper and skips re-validation.

## Generic Combine

For code that works with arbitrary relative paths, <xref:Singulink.IO.IDirectoryPath.Combine*> accepting an <xref:Singulink.IO.IRelativePath> returns the unifying base type (<xref:Singulink.IO.IPath>, <xref:Singulink.IO.IAbsolutePath> or <xref:Singulink.IO.IRelativePath> depending on the receiver). Pattern match on the result if you need the specific type:

```csharp
IAbsolutePath result = baseDir.Combine(someRelative);

if (result is IAbsoluteFilePath file) { /* ... */ }
```

## Walking Upward: ParentDirectory

Every path has a <xref:Singulink.IO.IPath.ParentDirectory>. For files it's always the containing directory; for directories it's `null` when the path is a root or otherwise has no parent (check <xref:Singulink.IO.IPath.HasParentDirectory> first).

```csharp
IAbsoluteFilePath cfg = FilePath.ParseAbsolute(@"C:\Apps\MyApp\config\app.json");
IAbsoluteDirectoryPath cfgDir = cfg.ParentDirectory;       // C:\Apps\MyApp\config
IAbsoluteDirectoryPath appDir = cfgDir.ParentDirectory!;   // C:\Apps\MyApp
```

A common pattern: ensure a file's directory exists before writing to it:

```csharp
file.ParentDirectory.Create();   // creates the directory tree if missing
using var stream = file.OpenStream(FileMode.Create);
```

## Walking to the Root

Use <xref:Singulink.IO.IAbsolutePath.RootDirectory> (on absolute paths) to jump straight to the root, or loop with <xref:Singulink.IO.IPath.HasParentDirectory>:

```csharp
IAbsoluteDirectoryPath root = cfg.RootDirectory;     // C:\

IDirectoryPath current = cfg.ParentDirectory;
while (current.HasParentDirectory)
    current = current.ParentDirectory!;
```

## GetLastExistingDirectory

When working with a path that may not exist yet, <xref:Singulink.IO.IAbsolutePath.GetLastExistingDirectory*> walks up the path until it finds a directory that does:

```csharp
var maybeMissing = FilePath.ParseAbsolute(@"C:\Apps\NewApp\data\users.json");
IAbsoluteDirectoryPath existing = maybeMissing.GetLastExistingDirectory();
long free = existing.AvailableFreeSpace;   // useful for pre-flight checks
```

See [Drive and Disk Information](drive-and-disk-info.md) for more on disk-space queries.

## Navigation in Relative Paths

Relative paths can encode upward navigation with `..`. The library resolves these as far as possible during parsing:

```csharp
DirectoryPath.ParseRelative("a/b/../c");      // "a/c"
DirectoryPath.ParseRelative("../../shared");  // "../../shared" (kept: can't resolve further)
```

Navigating past the root of an absolute path is always an error (see [Parsing Paths](parsing-paths.md)).

> [!NOTE]
> Every path is normalized as part of parsing. After parsing, the <xref:Singulink.IO.IPath.PathDisplay> you see is what the library will use everywhere; there is no separate "canonical form".

## Cross-Format Combines

Combining works seamlessly when one side uses <xref:Singulink.IO.PathFormat.Universal>:

```csharp
IRelativeFilePath portableCfg = FilePath.ParseRelative("config/app.json", PathFormat.Universal);
IAbsoluteDirectoryPath baseDir = DirectoryPath.GetAppBase();   // current format
IAbsoluteFilePath cfg = baseDir + portableCfg;                 // current format
```

Combining two specific formats that don't match (e.g. Windows + Unix) is an error. See the table in [Path Formats](path-formats.md).

## Next Steps

- [File Names and Extensions](file-names-and-extensions.md): manipulate the trailing segment of a path.
- [Working with Directories](directory-operations.md): once you've combined a path, do something with it.

</div>
