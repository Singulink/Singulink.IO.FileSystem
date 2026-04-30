<div class="article">

# Problems with System.IO

### Overview

`System.IO` is the standard .NET API for file system access, and it's a minefield. Decades of accumulated quirks, platform inconsistencies and silent path mutations make it painfully hard to write code that works reliably across Windows and Unix, across user-supplied input, and across the long tail of real-world file system states.

This article catalogues some of the most common pitfalls. It is not exhaustive; these are simply the issues that come up most often in practice. Each section pairs a concrete problem with a pointer to how this library addresses it. For the full picture, browse the [Guides](../guides/toc.yml).

## Weakly-Typed Path Strings

In `System.IO`, every path is a <xref:System.String>. The compiler can't tell you whether the path is absolute or relative, whether it points to a file or a directory, or even whether it's well-formed. The result: bugs that compile cleanly and only surface at runtime, often in obscure conditions.

```csharp
void OpenLog(string path) { File.Open(path, FileMode.Open); }

OpenLog(@"C:\Logs");           // path points to a directory: runtime IOException
OpenLog("relative\\file.log"); // resolved against whatever the CWD happens to be: surprise!
```

This library encodes those facts in the type system: paths are <xref:Singulink.IO.IAbsoluteFilePath>, <xref:Singulink.IO.IAbsoluteDirectoryPath>, <xref:Singulink.IO.IRelativeFilePath> or <xref:Singulink.IO.IRelativeDirectoryPath>. The bugs above are compile errors. See [Path Types](../guides/path-types.md).

## Silent Path Modification

`System.IO` silently rewrites path strings during file system operations. Most notoriously, it trims trailing spaces and dots from path segments. The same string can refer to two different entries depending on which API touched it last:

```csharp
var path = @"C:\directory \file.txt";   // user accidentally typed a trailing space
var fi = new FileInfo(path);
fi.Directory.Create();

using (fi.Create()) { /* write contents */ }

File.Exists(path);   // false: trimmed path doesn't match what was created
File.Open(path);     // FileNotFoundException
```

This library never alters named segments. By default it rejects such inputs at parse time with a clear message; with <xref:Singulink.IO.PathOptions.None> it preserves them exactly. See [Parsing Paths](../guides/parsing-paths.md) and [PathOptions](../guides/path-options.md).

## Unable to Open Existing Files

A path the user just selected from an OS file picker can be impossible to open through `System.IO` if the underlying name is "unfriendly" (trailing space, trailing dot, reserved device name, etc.):

```csharp
string fromPicker = openFileDialog.FileName;
File.Open(fromPicker);   // possible FileNotFoundException: even though the file is right there
```

The library's <xref:Singulink.IO.IAbsolutePath.PathExport> is a specially formatted string that bypasses these issues:

```csharp
var file = FilePath.ParseAbsolute(fromPicker, PathOptions.None);
using FileStream stream = file.OpenStream();
// or, when handing to a non-library API:
File.Open(file.PathExport);
```

See [Path Formats](../guides/path-formats.md).

## File / Directory Confusion

<xref:System.IO.FileSystemInfo.Attributes> on a <xref:System.IO.FileInfo> will happily return data for a path that points to a directory, and on a <xref:System.IO.DirectoryInfo> for a path that points to a file. <xref:System.IO.FileSystemInfo.Exists> will be `false` in both cases, but only after a separate access. Code that reads attributes "just to check" frequently gets the wrong answer.

<xref:System.IO.File.Delete*> on a directory throws <xref:System.UnauthorizedAccessException> on Windows. <xref:System.IO.Directory.Delete*> on a file throws <xref:System.IO.IOException> with the message "directory name is invalid". Neither matches what you'd expect, and the behavior differs across platforms.

The library's <xref:Singulink.IO.EntryState> explicitly distinguishes "doesn't exist" from "wrong type", and the type system prevents the cross-typed <xref:Singulink.IO.IAbsolutePath.Attributes> mistake entirely. See [Cached Entry Info](../guides/cached-entry-info.md) and [Working with Files](../guides/file-operations.md).

## Directory.GetParent Quirks

```csharp
Directory.GetParent(@"C:\temp\");   // returns "C:\temp" (the directory itself!), not "C:\"
Directory.GetParent(@"C:\temp");    // returns "C:\"
```

<xref:System.IO.Directory.GetParent*> makes a naive decision based on whether the trailing slash is present, so the same logical directory has two different parents depending on how its string happened to be written. The library's <xref:Singulink.IO.IPath.ParentDirectory> is a deterministic, type-safe walk up the path. See [Combining and Navigating Paths](../guides/combining-and-navigating.md).

## Navigating Past the Root

`System.IO` accepts paths that walk past the root and silently produces something that looks like a valid path:

```csharp
Path.GetFullPath(@"C:\foo\..\..\bar");   // "C:\bar": bug if "foo" was load-bearing
```

This pattern is a frequent source of silent bugs after files are moved or pasted. The library treats it as a parse-time error so the problem surfaces immediately. See [Parsing Paths](../guides/parsing-paths.md).

## FileInfo / DirectoryInfo Pitfalls

<xref:System.IO.FileInfo> and <xref:System.IO.DirectoryInfo> are designed in a peculiar way:

- They can be **constructed for paths that don't exist**. Property access lazily queries the file system on first read and may surprise-throw.
- Properties are **mutable**. Setting a property invalidates the cached state, causing the next access to re-query.
- Reading <xref:System.IO.FileSystemInfo.Attributes> from a <xref:System.IO.FileInfo> whose path is actually a directory **succeeds**, returning the directory's attributes: even though <xref:System.IO.FileInfo.Exists> is `false`.

The result is an info object whose behavior depends on hidden invalidation state and whose properties may or may not reflect a consistent snapshot of the file system.

The library's <xref:Singulink.IO.CachedEntryInfo> (with <xref:Singulink.IO.CachedFileInfo> and <xref:Singulink.IO.CachedDirectoryInfo>) replaces these:

- Construction validates existence and type up front: the object is never in an invalid state.
- Properties are read-only and represent a consistent snapshot.
- Mutations go through the path object; <xref:Singulink.IO.CachedEntryInfo.Refresh*> is an explicit re-query.

See [Cached Entry Info](../guides/cached-entry-info.md).

## Mixed and Inconsistent Exception Types

I/O operations in `System.IO` can throw <xref:System.ArgumentException>, <xref:System.IO.IOException> (and subtypes), <xref:System.UnauthorizedAccessException>, and <xref:System.NotSupportedException>. To make matters worse, <xref:System.UnauthorizedAccessException> does **not** derive from <xref:System.IO.IOException>. To handle "any I/O failure", you have to catch two unrelated base types or fall back to `catch (Exception)`.

On top of that, the same operation can throw different exception types on Windows and Unix.

This library normalizes both:

- Parse-time errors are always <xref:System.ArgumentException> (and subtypes).
- I/O-time errors are always <xref:System.IO.IOException> (and subtypes), including <xref:Singulink.IO.UnauthorizedIOAccessException> for permission failures.

A single `catch (IOException)` covers every I/O failure, and the exception type for a given failure is the same on every platform. See [Exception Handling](../guides/exception-handling.md).

## Cross-Platform Inconsistencies

<xref:System.IO.Directory.GetFiles*> defaults to **case-sensitive matching on Unix and case-insensitive matching on Windows**. Code that runs cleanly on Windows finds nothing on Linux, or worse, finds the wrong subset.

There's no way to validate that a path is portable across platforms, no way to manipulate Unix paths from Windows, and no shared format for storing paths in cross-platform data.

The library:

- Defaults search <xref:Singulink.IO.SearchOptions.MatchCasing> to case-insensitive on every platform: consistent behavior by default.
- Provides explicit <xref:Singulink.IO.PathFormat.Windows>, <xref:Singulink.IO.PathFormat.Unix> and a <xref:Singulink.IO.PathFormat.Universal> format that is portable across platforms.
- Validates entries against the strictest rules when using <xref:Singulink.IO.PathFormat.Universal>, so anything that parses is guaranteed to work everywhere.

See [Path Formats](../guides/path-formats.md) and [Searching and Enumeration](../guides/searching-and-enumeration.md).

## DriveInfo Limitations

Available/used space in `System.IO` only comes from <xref:System.IO.DriveInfo>, which has three problems:

1. **Drives are a Windows concept.** Unix mount points don't fit.
2. **UNC paths are not supported.** `new DriveInfo(@"\\server\share")` doesn't work.
3. **It ignores per-user quotas and mounted subdirectories.** The "free space" reported for the volume isn't necessarily the free space available at the path you care about.

This library moves disk-space data onto every <xref:Singulink.IO.IAbsoluteDirectoryPath>, where it can answer the question for the actual location:

```csharp
target.GetLastExistingDirectory().AvailableFreeSpace;   // works for UNC, mounts and quotas
DirectoryPath.GetMountingPoints();                      // cross-platform DriveInfo.GetDrives()
```

See [Drive and Disk Information](../guides/drive-and-disk-info.md).

## UNC Path Handling

UNC paths trip up many `System.IO` methods. There is no global guarantee that an arbitrary `System.IO` API works correctly with `\\server\share\...`.

This library treats UNC as a first-class citizen. <xref:Singulink.IO.IAbsolutePath.IsUnc> reports it, every operation supports it, disk-space queries work as expected. See [Drive and Disk Information](../guides/drive-and-disk-info.md).

## And More

The list above is a sampling, not a survey. There are countless smaller pitfalls in `System.IO` (stream lifetime around <xref:System.IO.FileInfo.Open*>, inconsistent behavior of <xref:System.IO.Path.GetFullPath*> for relative paths with no current directory, <xref:System.IO.Path.Combine*> discarding earlier segments when a later one is rooted, <xref:System.IO.EnumerationOptions.MatchCasing> defaults), and more discovered the longer you use it. Getting all of this right consistently is hard, especially in cross-platform code, and you can spend a lot of time and energy chasing edge cases that the library design simply doesn't have.

If you want a quick walk-through of how the library wants to be used instead, start at [Getting Started](../guides/getting-started.md).

## Further Reading

The full set of guides covers each topic above in depth:

- [Getting Started](../guides/getting-started.md): installation and a 30-second tour.
- [Path Types](../guides/path-types.md), [Parsing Paths](../guides/parsing-paths.md), [PathOptions](../guides/path-options.md), [Path Formats](../guides/path-formats.md).
- [Combining and Navigating Paths](../guides/combining-and-navigating.md), [File Names and Extensions](../guides/file-names-and-extensions.md), [Special Locations](../guides/special-locations.md).
- [Working with Files](../guides/file-operations.md), [Working with Directories](../guides/directory-operations.md), [Searching and Enumeration](../guides/searching-and-enumeration.md).
- [Drive and Disk Information](../guides/drive-and-disk-info.md), [Cached Entry Info](../guides/cached-entry-info.md), [Exception Handling](../guides/exception-handling.md), [Interop and Migration](../guides/interop-and-migration.md).

</div>
