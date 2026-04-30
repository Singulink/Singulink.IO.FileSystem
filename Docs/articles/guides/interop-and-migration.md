<div class="article">

# Interop and Migration

### Overview

This article is a practical guide for code that already uses `System.IO`, either because you're migrating an existing application or because you depend on a third-party library that takes <xref:System.String>, <xref:System.IO.FileInfo> or <xref:System.IO.DirectoryInfo> parameters. The library is designed to coexist with `System.IO` so you can adopt it incrementally.

## Bridging at the Boundaries

The two key bridges are:

- **<xref:Singulink.IO.IAbsolutePath.PathExport>**: convert a path to a string the underlying file system will reliably accept.
- **<xref:Singulink.IO.SystemExtensions.ToPath*>**: convert <xref:System.IO.FileInfo> / <xref:System.IO.DirectoryInfo> to a path object.

Use them at the boundaries of your code where it interacts with non-library APIs. Inside your own code, work with path objects.

## Strings ? Paths

To turn a <xref:System.String> you got from somewhere else into a path, parse it with the parser that matches what you know about the input:

```csharp
IAbsoluteFilePath path = FilePath.ParseAbsolute(stringFromExternalApi, PathOptions.None);
```

Use <xref:Singulink.IO.PathOptions.None> when the string came from the file system itself (e.g. an OS file picker). The OS may surface "unfriendly" paths that exist and need to be opened. Use <xref:Singulink.IO.PathOptions.NoUnfriendlyNames> (the default) for application-defined or user-typed input.

See [Parsing Paths](parsing-paths.md) and [PathOptions](path-options.md).

## Paths ? Strings

When an external API takes a <xref:System.String> path, hand it <xref:Singulink.IO.IAbsolutePath.PathExport> (only available on absolute paths):

```csharp
ThirdPartyApi.OpenFile(file.PathExport);
```

> [!IMPORTANT]
> Use <xref:Singulink.IO.IAbsolutePath.PathExport>, not <xref:Singulink.IO.IPath.PathDisplay> or <xref:System.Object.ToString*>, when calling APIs outside this library. <xref:Singulink.IO.IAbsolutePath.PathExport> is specially formatted (e.g. with `\\?\` on Windows) so the OS won't silently rewrite it.

## FileInfo / DirectoryInfo ? Paths

Use the <xref:Singulink.IO.SystemExtensions> extension methods:

```csharp
using Singulink.IO;

DirectoryInfo di = new(@"C:\some\path");
FileInfo fi = new(@"C:\some\file.txt");

IAbsoluteDirectoryPath dirPath = di.ToPath();
IAbsoluteFilePath filePath = fi.ToPath();
```

Both extensions parse <xref:System.IO.FileSystemInfo.FullName> with <xref:Singulink.IO.PathOptions.NoUnfriendlyNames> by default; pass <xref:Singulink.IO.PathOptions.None> to accept any path:

```csharp
IAbsoluteFilePath filePath = fi.ToPath(PathOptions.None);
```

> [!CAUTION]
> <xref:System.IO.FileSystemInfo.FullName> may have already been rewritten by `System.IO` (trimming trailing spaces and dots) before <xref:Singulink.IO.SystemExtensions.ToPath*> sees it. If preserving the exact original characters matters, parse the original string directly with <xref:Singulink.IO.FilePath.ParseAbsolute*>.

## Paths ? FileInfo / DirectoryInfo

When an external API takes a <xref:System.IO.FileInfo> or <xref:System.IO.DirectoryInfo>, construct one from <xref:Singulink.IO.IAbsolutePath.PathExport>:

```csharp
FileInfo fi = new(absoluteFilePath.PathExport);
DirectoryInfo di = new(absoluteDirectoryPath.PathExport);

ThirdPartyApi.Process(fi);
```

## System.IO ? Library Mapping

A reference of common `System.IO` operations and their library equivalents:

#### Path Construction

| `System.IO` | Library |
|-------------|---------|
| <xref:System.IO.Path.Combine*> | <xref:Singulink.IO.IAbsoluteDirectoryPath.CombineFile*> / <xref:Singulink.IO.IAbsoluteDirectoryPath.CombineDirectory*> / `dir + relative` |
| <xref:System.IO.Path.GetDirectoryName*> | `path.ParentDirectory.PathDisplay` |
| <xref:System.IO.Path.GetFileName*> | <xref:Singulink.IO.IPath.Name> |
| <xref:System.IO.Path.GetFileNameWithoutExtension*> | <xref:Singulink.IO.IFilePath.NameWithoutExtension> |
| <xref:System.IO.Path.GetExtension*> | <xref:Singulink.IO.IFilePath.Extension> |
| <xref:System.IO.Path.ChangeExtension*> | <xref:Singulink.IO.IFilePath.WithExtension*> |
| <xref:System.IO.Path.GetFullPath*> | <xref:Singulink.IO.FilePath.ParseAbsolute*> (or <xref:Singulink.IO.FilePath.Parse*> if it could be relative) |
| <xref:System.IO.Path.GetTempPath*> | <xref:Singulink.IO.DirectoryPath.GetTemp*> |
| <xref:System.IO.Path.GetTempFileName*> | <xref:Singulink.IO.FilePath.CreateTempFile*> |

#### Files

| `System.IO` | Library |
|-------------|---------|
| <xref:System.IO.File.Exists*> | <xref:Singulink.IO.IAbsolutePath.Exists> |
| <xref:System.IO.File.Open*> | <xref:Singulink.IO.IAbsoluteFilePath.OpenStream*> |
| <xref:System.IO.File.Copy*> | <xref:Singulink.IO.IAbsoluteFilePath.CopyTo*> |
| <xref:System.IO.File.Move*> | <xref:Singulink.IO.IAbsoluteFilePath.MoveTo*> |
| <xref:System.IO.File.Replace*> | <xref:Singulink.IO.IAbsoluteFilePath.Replace*> |
| <xref:System.IO.File.Delete*> | <xref:Singulink.IO.IAbsoluteFilePath.Delete*> |
| <xref:System.IO.File.GetAttributes*> | <xref:Singulink.IO.IAbsolutePath.Attributes> |
| <xref:System.IO.File.GetLastWriteTimeUtc*> | <xref:Singulink.IO.IAbsolutePath.LastWriteTimeUtc> |
| `new FileInfo(p)` (for metadata) | <xref:Singulink.IO.IAbsoluteFilePath.GetInfo*> (returns <xref:Singulink.IO.CachedFileInfo>) |

#### Directories

| `System.IO` | Library |
|-------------|---------|
| <xref:System.IO.Directory.Exists*> | <xref:Singulink.IO.IAbsolutePath.Exists> |
| <xref:System.IO.Directory.CreateDirectory*> | <xref:Singulink.IO.IAbsoluteDirectoryPath.Create*> |
| <xref:System.IO.Directory.Delete*> | <xref:Singulink.IO.IAbsoluteDirectoryPath.Delete*> |
| <xref:System.IO.Directory.GetCurrentDirectory*> | <xref:Singulink.IO.DirectoryPath.GetCurrent*> |
| <xref:System.IO.Directory.SetCurrentDirectory*> | <xref:Singulink.IO.DirectoryPath.SetCurrent*> |
| <xref:System.IO.Directory.GetParent*> | <xref:Singulink.IO.IPath.ParentDirectory> |
| <xref:System.IO.Directory.GetFiles*> | <xref:Singulink.IO.IAbsoluteDirectoryPath.GetChildFiles*> |
| <xref:System.IO.Directory.GetDirectories*> | <xref:Singulink.IO.IAbsoluteDirectoryPath.GetChildDirectories*> |
| <xref:System.IO.Directory.EnumerateFileSystemEntries*> | <xref:Singulink.IO.IAbsoluteDirectoryPath.GetChildEntries*> |

#### Drives

| `System.IO` | Library |
|-------------|---------|
| <xref:System.IO.DriveInfo.GetDrives*> | <xref:Singulink.IO.DirectoryPath.GetMountingPoints*> |
| <xref:System.IO.DriveInfo.AvailableFreeSpace> | <xref:Singulink.IO.IAbsoluteDirectoryPath.AvailableFreeSpace> (on **any** absolute directory) |
| <xref:System.IO.DriveInfo.TotalFreeSpace> | <xref:Singulink.IO.IAbsoluteDirectoryPath.TotalFreeSpace> |
| <xref:System.IO.DriveInfo.TotalSize> | <xref:Singulink.IO.IAbsoluteDirectoryPath.TotalSize> |
| <xref:System.IO.DriveInfo.DriveType> | <xref:Singulink.IO.IAbsoluteDirectoryPath.DriveType> |
| <xref:System.IO.DriveInfo.DriveFormat> | <xref:Singulink.IO.IAbsoluteDirectoryPath.FileSystem> |

See [Drive and Disk Information](drive-and-disk-info.md).

## Migration Strategy

A pragmatic order of operations:

1. **Adopt at the edges first.** New code parses inputs into path objects; old code keeps working with strings until you touch it.
2. **Use <xref:Singulink.IO.SystemExtensions.ToPath*> to bridge existing <xref:System.IO.FileInfo> / <xref:System.IO.DirectoryInfo> chains** without rewriting the calling code.
3. **Replace `try`/`catch` ladders with the parse-vs-IO split.** Anything inside a parse step catches <xref:System.ArgumentException>; anything inside an I/O step catches <xref:System.IO.IOException>. See [Exception Handling](exception-handling.md).
4. **Convert <xref:System.IO.Directory.GetFiles*> / <xref:System.IO.Directory.EnumerateFiles*> calls** to `GetChild*` enumeration with <xref:Singulink.IO.SearchOptions>. Pay attention to <xref:Singulink.IO.SearchOptions.MatchCasing>: the library's case-insensitive default is consistent across platforms, which is a behavior change from `System.IO` on Unix.
5. **Drop <xref:System.IO.DriveInfo> workarounds.** UNC paths, mounted subdirs and per-user quotas Just Work via the disk-space members on absolute directories.

## When You Still Need Strings

You'll still encounter APIs that demand a <xref:System.String>. Continue using <xref:Singulink.IO.IAbsolutePath.PathExport>:

```csharp
public Task UploadAsync(IAbsoluteFilePath file)
{
    return _httpClient.PostAsync(_url, new StreamContent(File.OpenRead(file.PathExport)));
}
```

Don't use <xref:Singulink.IO.IPath.PathDisplay> for I/O even if it looks the same as <xref:Singulink.IO.IAbsolutePath.PathExport> for typical paths; the difference is invisible until something subtle goes wrong.

## Next Steps

- [Path Formats](path-formats.md): the difference between <xref:Singulink.IO.IPath.PathDisplay>, <xref:Singulink.IO.IAbsolutePath.PathExport> and <xref:System.Object.ToString*>.
- [Exception Handling](exception-handling.md): replace `System.IO` catch ladders with the cleaner two-phase model.

</div>
