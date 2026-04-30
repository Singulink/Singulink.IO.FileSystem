<div class="article">

# Searching and Enumeration

### Overview

Every absolute directory exposes a complete set of enumeration methods (twelve in total) that fall into a clean grid:

| | Files | Directories | Entries (either) |
|--|---|---|---|
| Cached info | <xref:Singulink.IO.IAbsoluteDirectoryPath.GetChildFilesInfo*> | <xref:Singulink.IO.IAbsoluteDirectoryPath.GetChildDirectoriesInfo*> | <xref:Singulink.IO.IAbsoluteDirectoryPath.GetChildEntriesInfo*> |
| Absolute paths | <xref:Singulink.IO.IAbsoluteDirectoryPath.GetChildFiles*> | <xref:Singulink.IO.IAbsoluteDirectoryPath.GetChildDirectories*> | <xref:Singulink.IO.IAbsoluteDirectoryPath.GetChildEntries*> |
| Relative to this directory | <xref:Singulink.IO.IAbsoluteDirectoryPath.GetRelativeChildFiles*> | <xref:Singulink.IO.IAbsoluteDirectoryPath.GetRelativeChildDirectories*> | <xref:Singulink.IO.IAbsoluteDirectoryPath.GetRelativeChildEntries*> |
| Relative to a sub-location | <xref:Singulink.IO.IAbsoluteDirectoryPath.GetRelativeFiles*> | <xref:Singulink.IO.IAbsoluteDirectoryPath.GetRelativeDirectories*> | <xref:Singulink.IO.IAbsoluteDirectoryPath.GetRelativeEntries*> |

All return <xref:System.Collections.Generic.IEnumerable`1> and stream lazily; iteration touches the file system as you go.

## Picking the Right Method

Decide along two axes:

1. **What do you want back?**
   - Just paths: `Get*` returns <xref:Singulink.IO.IAbsolutePath> or <xref:Singulink.IO.IRelativePath> derivatives.
   - Paths plus metadata in a single call: `Get*Info` returns <xref:Singulink.IO.CachedFileInfo> or <xref:Singulink.IO.CachedDirectoryInfo>.
2. **What kind of entries?**
   - Files only / directories only / either: pick the matching variant.

Use the `Info` variants when you'll read attributes, sizes or timestamps from each result; they save a stat per entry compared to calling <xref:Singulink.IO.IAbsolutePath.GetInfo*> afterwards.

```csharp
// Just paths:
foreach (IAbsoluteFilePath f in dir.GetChildFiles("*.cs"))
    Console.WriteLine(f.PathDisplay);

// Paths + metadata in one call:
foreach (CachedFileInfo info in dir.GetChildFilesInfo("*.cs"))
    Console.WriteLine($"{info.Path.Name}  {info.Length}  {info.LastWriteTimeUtc:O}");
```

## Search Patterns

The optional `searchPattern` argument supports two wildcards:

- `*`: matches any sequence of characters (including empty).
- `?`: matches any single character.

The default pattern (when omitted) is `"*"`, which matches everything.

```csharp
dir.GetChildFiles();              // all files
dir.GetChildFiles("*.json");      // matches "config.json", "users.json"
dir.GetChildFiles("data?.bin");   // matches "data1.bin" but not "data10.bin"
```

> [!NOTE]
> Search patterns are matched against the entry **name**, not the full path. Recursive searches still apply the pattern to each entry's name as they descend.

## SearchOptions

Tune behavior with <xref:Singulink.IO.SearchOptions>. All properties are optional and have sensible defaults:

| Property | Default | Purpose |
|----------|---------|---------|
| <xref:Singulink.IO.SearchOptions.Recursive> | `false` | Descend into subdirectories. |
| <xref:Singulink.IO.SearchOptions.MaxRecursionDepth> | <xref:System.Int32.MaxValue> | Cap recursion depth (only meaningful when <xref:Singulink.IO.SearchOptions.Recursive> is `true`). |
| <xref:Singulink.IO.SearchOptions.MatchCasing> | <xref:System.IO.MatchCasing.CaseInsensitive> | Filename matching mode. |
| <xref:Singulink.IO.SearchOptions.AttributesToSkip> | <xref:System.IO.FileAttributes.None> | Skip entries with any of the specified <xref:System.IO.FileAttributes>. |
| <xref:Singulink.IO.SearchOptions.BufferSize> | `0` (default) | Suggested OS buffer size. |
| <xref:Singulink.IO.SearchOptions.InaccessibleSearchBehavior> | <xref:Singulink.IO.InaccessibleSearchBehavior.ThrowForSearchDir> | How to handle inaccessible directories; see below. |

> [!TIP]
> The <xref:Singulink.IO.SearchOptions.MatchCasing> default is **case-insensitive**: the same on Windows and Unix. `System.IO`'s default differs by platform, which leads to platform-specific bugs that this library deliberately avoids.

```csharp
var opts = new SearchOptions
{
    Recursive = true,
    MaxRecursionDepth = 3,
    AttributesToSkip = FileAttributes.Hidden | FileAttributes.System,
};

foreach (IAbsoluteFilePath cs in repoRoot.GetChildFiles("*.cs", opts))
    Process(cs);
```

## Inaccessible Directories

When recursive searches encounter a directory the process can't read, you have three behaviors via <xref:Singulink.IO.InaccessibleSearchBehavior>:

| Value | Behavior |
|-------|----------|
| <xref:Singulink.IO.InaccessibleSearchBehavior.ThrowForSearchDir> (default) | Throw if the directory you started searching is inaccessible; silently skip inaccessible nested directories. |
| <xref:Singulink.IO.InaccessibleSearchBehavior.ThrowForAll> | Throw the moment any inaccessible directory is encountered. |
| <xref:Singulink.IO.InaccessibleSearchBehavior.IgnoreAll> | Skip everything inaccessible without complaint. |

```csharp
var resilient = new SearchOptions
{
    Recursive = true,
    InaccessibleSearchBehavior = InaccessibleSearchBehavior.IgnoreAll,
};

long total = profile.GetChildFiles("*", resilient).Sum(f => f.Length);
```

> [!NOTE]
> <xref:Singulink.IO.InaccessibleSearchBehavior.ThrowForSearchDir> is the friendliest default: you reliably get notified if the path you handed in is unreadable, but missing permissions deep in the tree don't blow up an otherwise-valid search. While this is the least surprising behavior, it does incur an extra file system call for searches that yield no matches. This should not be an issue for the vast majority of applications, but is worth noting.

When a search throws because of inaccessibility, the exception is <xref:Singulink.IO.UnauthorizedIOAccessException>. See [Exception Handling](exception-handling.md).

## Relative Enumeration

The `GetRelative*` methods return relative paths instead of absolute ones, useful when you'll move/copy/store the results relative to the search root:

```csharp
IAbsoluteDirectoryPath src = appBase.CombineDirectory("source");
IAbsoluteDirectoryPath dst = appBase.CombineDirectory("dist");

var opts = new SearchOptions { Recursive = true };
foreach (IRelativeFilePath rel in src.GetRelativeChildFiles("*", opts))
{
    IAbsoluteFilePath target = dst + rel;
    target.ParentDirectory.Create();
    (src + rel).CopyTo(target, overwrite: true);
}
```

#### GetRelative* with a Search Location

<xref:Singulink.IO.IAbsoluteDirectoryPath.GetRelativeFiles*> (and the <xref:Singulink.IO.IAbsoluteDirectoryPath.GetRelativeDirectories*> / <xref:Singulink.IO.IAbsoluteDirectoryPath.GetRelativeEntries*> variants) start the search inside a `searchLocation` that is relative to the outer directory, while keeping every returned path relative to that **outer** directory.

When `searchLocation` points to a sub-directory of the outer directory, the behavior is straightforward:

```csharp
// outerDir = /app
IRelativeDirectoryPath sub = DirectoryPath.ParseRelative("config/users");

foreach (IRelativeFilePath f in outerDir.GetRelativeFiles(sub, "*.json"))
{
    // Search runs inside /app/config/users.
    // f is relative to /app, e.g. "config/users/admin.json".
}
```

##### Searching in a Parent Directory

`searchLocation` may also point upward via `..` segments. The search then runs *outside* of the outer directory, but results are still expressed relative to it. Because some matches may live inside the same subtree the outer directory is in, the library applies the following rule when constructing each returned path:

- For a match whose absolute path is **inside the outer directory**, the upward navigation cancels out and the result is a clean forward-only relative path.
- For a match whose absolute path is **not inside the outer directory**, the result is a relative path that begins with as many `..` segments as needed to walk back out to the search location.

Example: searching the parent directory of `/repo/src` with `searchLocation = ".."`:

```csharp
// outerDir = /repo/src
IRelativeDirectoryPath up = DirectoryPath.ParseRelative("..");

foreach (IRelativeFilePath f in outerDir.GetRelativeFiles(up, "*.md", new SearchOptions { Recursive = true }))
{
    // Search runs inside /repo (the parent of /repo/src).
    //
    // /repo/README.md        -> "../README.md"        (sibling of outerDir)
    // /repo/docs/intro.md    -> "../docs/intro.md"    (sibling subtree)
    // /repo/src/notes.md     -> "notes.md"            (inside outerDir)
    // /repo/src/api/x.md     -> "api/x.md"            (inside outerDir)
}
```

The same rule scales to deeper navigation (`../..`, `../../..`) and to a rooted search location: `searchLocation` rooted to the file system root means the search runs from the root, and each result is given as many leading `..` segments as required to reach the outer directory.

> [!TIP]
> Use this overload when you have a stable "anchor" directory (your outer directory) but the actual search location is computed at runtime. Every result you store, log or compare is anchored against the same reference point regardless of where the search ran.

## Lazy Iteration and Exceptions

Enumeration is lazy. The file system call that produces the next batch of entries can throw at any iteration step, typically <xref:System.IO.IOException>, <xref:System.IO.DirectoryNotFoundException> or <xref:Singulink.IO.UnauthorizedIOAccessException>. Wrap the `foreach` (or materialization with <xref:System.Linq.Enumerable.ToList*>) in a `try`/`catch` if you need to handle errors:

```csharp
try
{
    foreach (var f in dir.GetChildFiles("*", new SearchOptions { Recursive = true }))
        Process(f);
}
catch (UnauthorizedIOAccessException ex)
{
    log.Warn($"Search interrupted: {ex.Message}");
}
```

## Next Steps

- [Cached Entry Info](cached-entry-info.md): make the most of the `*Info` variants.
- [Exception Handling](exception-handling.md): handle search-time errors cleanly.

</div>
