<div class="article">

# Interop and Migration

This article is a practical guide for code that already uses `System.IO`, either because you're migrating an existing application or because you depend on a third-party library that takes <xref:System.String>, <xref:System.IO.FileInfo> or <xref:System.IO.DirectoryInfo> parameters. The library is designed to coexist with `System.IO` so you can adopt it incrementally.

## Bridging at the Boundaries

The two key bridges are:

- **<xref:Singulink.IO.IAbsolutePath.PathExport?displayProperty=nameWithType>**: convert a path to a string the underlying file system will reliably accept.
- **<xref:Singulink.IO.SystemExtensions.ToPath*?displayProperty=nameWithType>**: convert <xref:System.IO.FileInfo> / <xref:System.IO.DirectoryInfo> to a path object.

Use them at the boundaries of your code where it interacts with non-library APIs. Inside your own code, work with path objects.

## Strings → Paths

To turn a <xref:System.String> you got from somewhere else into a path, parse it with the parser that matches what you know about the input:

```csharp
IAbsoluteFilePath path = FilePath.ParseAbsolute(stringFromExternalApi, PathOptions.None);
```

Use <xref:Singulink.IO.PathOptions.None?displayProperty=nameWithType> when the string came from the file system itself (e.g. an OS file picker). The OS may surface "unfriendly" paths that exist and need to be opened. Use <xref:Singulink.IO.PathOptions.NoUnfriendlyNames?displayProperty=nameWithType> (the default) for application-defined or user-typed input.

If the string may not be a valid path at all, the non-throwing <xref:Singulink.IO.FilePath.TryParseAbsolute*?displayProperty=nameWithType> (or one of the other `Try` parse methods) reports failure through its return value instead of an exception.

See [Parsing Paths](parsing-paths.md) and [PathOptions](path-options.md).

## Paths → Strings

When an external API takes a <xref:System.String> path, hand it <xref:Singulink.IO.IAbsolutePath.PathExport?displayProperty=nameWithType> (only available on absolute paths):

```csharp
ThirdPartyApi.OpenFile(file.PathExport);
```

> [!IMPORTANT]
> Use <xref:Singulink.IO.IAbsolutePath.PathExport?displayProperty=nameWithType>, not <xref:Singulink.IO.IPath.PathDisplay?displayProperty=nameWithType> or <xref:Singulink.IO.IPath.ToString*?displayProperty=nameWithType>, when calling APIs outside this library. <xref:Singulink.IO.IAbsolutePath.PathExport> is specially formatted (e.g. with `\\?\` on Windows) so the OS won't silently rewrite it.

## FileInfo / DirectoryInfo → Paths

Use the <xref:Singulink.IO.SystemExtensions> extension methods:

```csharp
using Singulink.IO;

DirectoryInfo di = new(@"C:\some\path");
FileInfo fi = new(@"C:\some\file.txt");

IAbsoluteDirectoryPath dirPath = di.ToPath();
IAbsoluteFilePath filePath = fi.ToPath();
```

Both extensions parse <xref:System.IO.FileSystemInfo.FullName?displayProperty=nameWithType> with <xref:Singulink.IO.PathOptions.NoUnfriendlyNames?displayProperty=nameWithType> by default; pass <xref:Singulink.IO.PathOptions.None?displayProperty=nameWithType> to accept any path:

```csharp
IAbsoluteFilePath filePath = fi.ToPath(PathOptions.None);
```

When you only have a <xref:System.IO.FileSystemInfo> reference (e.g. from a file-picker that surfaces either kind), call <xref:Singulink.IO.SystemExtensions.ToPath*?displayProperty=nameWithType> on the base type. The returned <xref:Singulink.IO.IAbsolutePath> has the runtime type that matches the underlying info object:

```csharp
FileSystemInfo fsi = picker.SelectedItem;
IAbsolutePath path = fsi.ToPath();

switch (path)
{
    case IAbsoluteFilePath file: /* ... */ break;
    case IAbsoluteDirectoryPath dir: /* ... */ break;
}
```

If you also need cached metadata for the entry, use <xref:Singulink.IO.SystemExtensions.ToCachedInfo*?displayProperty=nameWithType> instead of <xref:Singulink.IO.SystemExtensions.ToPath*?displayProperty=nameWithType>. It returns a <xref:Singulink.IO.CachedFileInfo> / <xref:Singulink.IO.CachedDirectoryInfo> for a <xref:System.IO.FileInfo> / <xref:System.IO.DirectoryInfo>, or the matching concrete type when called on a <xref:System.IO.FileSystemInfo>:

```csharp
CachedFileInfo cached = fi.ToCachedInfo();
CachedEntryInfo info = fsi.ToCachedInfo();
```

> [!CAUTION]
> <xref:System.IO.FileSystemInfo.FullName?displayProperty=nameWithType> may have already been rewritten by `System.IO` (trimming trailing spaces and dots) before <xref:Singulink.IO.SystemExtensions.ToPath*?displayProperty=nameWithType> sees it. If preserving the exact original characters matters, parse the original string directly with <xref:Singulink.IO.FilePath.ParseAbsolute*?displayProperty=nameWithType>.

## Paths → FileInfo / DirectoryInfo

When an external API takes a <xref:System.IO.FileInfo> or <xref:System.IO.DirectoryInfo>, construct one from <xref:Singulink.IO.IAbsolutePath.PathExport?displayProperty=nameWithType>:

```csharp
FileInfo fi = new(absoluteFilePath.PathExport);
DirectoryInfo di = new(absoluteDirectoryPath.PathExport);

ThirdPartyApi.Process(fi);
```

## System.IO → Library Mapping

A reference of common `System.IO` operations and their library equivalents:

#### Path Construction

| `System.IO` | Library |
|-------------|---------|
| <xref:System.IO.Path.Combine*?displayProperty=nameWithType> | <xref:Singulink.IO.IDirectoryPath.CombineFile*?displayProperty=nameWithType> / <xref:Singulink.IO.IDirectoryPath.CombineDirectory*?displayProperty=nameWithType> / the `+` operator |
| <xref:System.IO.Path.GetDirectoryName*?displayProperty=nameWithType> | <xref:Singulink.IO.IPath.ParentDirectory?displayProperty=nameWithType> |
| <xref:System.IO.Path.GetFileName*?displayProperty=nameWithType> | <xref:Singulink.IO.IPath.Name?displayProperty=nameWithType> |
| <xref:System.IO.Path.GetFileNameWithoutExtension*?displayProperty=nameWithType> | <xref:Singulink.IO.IFilePath.NameWithoutExtension?displayProperty=nameWithType> |
| <xref:System.IO.Path.GetExtension*?displayProperty=nameWithType> | <xref:Singulink.IO.IFilePath.Extension?displayProperty=nameWithType> |
| <xref:System.IO.Path.ChangeExtension*?displayProperty=nameWithType> | <xref:Singulink.IO.IFilePath.WithExtension*?displayProperty=nameWithType> |
| <xref:System.IO.Path.GetFullPath*?displayProperty=nameWithType> | <xref:Singulink.IO.FilePath.ParseAbsolute*?displayProperty=nameWithType> (or <xref:Singulink.IO.FilePath.Parse*?displayProperty=nameWithType> if it could be relative) |
| <xref:System.IO.Path.GetTempPath*?displayProperty=nameWithType> | <xref:Singulink.IO.DirectoryPath.GetTemp*?displayProperty=nameWithType> |
| <xref:System.IO.Path.GetTempFileName*?displayProperty=nameWithType> | <xref:Singulink.IO.FilePath.CreateTempFile*?displayProperty=nameWithType> |

#### Files

| `System.IO` | Library |
|-------------|---------|
| <xref:System.IO.File.Exists*?displayProperty=nameWithType> | <xref:Singulink.IO.IAbsolutePath.Exists?displayProperty=nameWithType> |
| <xref:System.IO.File.Open*?displayProperty=nameWithType> | <xref:Singulink.IO.IAbsoluteFilePath.OpenStream*?displayProperty=nameWithType> |
| <xref:System.IO.File.Copy*?displayProperty=nameWithType> | <xref:Singulink.IO.IAbsoluteFilePath.CopyTo*?displayProperty=nameWithType> |
| <xref:System.IO.File.Move*?displayProperty=nameWithType> | <xref:Singulink.IO.IAbsoluteFilePath.MoveTo*?displayProperty=nameWithType> |
| <xref:System.IO.File.Replace*?displayProperty=nameWithType> | <xref:Singulink.IO.IAbsoluteFilePath.Replace*?displayProperty=nameWithType> |
| <xref:System.IO.File.Delete*?displayProperty=nameWithType> | <xref:Singulink.IO.IAbsoluteFilePath.Delete*?displayProperty=nameWithType> |
| <xref:System.IO.File.GetAttributes*?displayProperty=nameWithType> | <xref:Singulink.IO.IAbsolutePath.Attributes?displayProperty=nameWithType> |
| <xref:System.IO.File.GetLastWriteTimeUtc*?displayProperty=nameWithType> | <xref:Singulink.IO.IAbsolutePath.LastWriteTimeUtc?displayProperty=nameWithType> |
| A new <xref:System.IO.FileInfo> (for metadata) | <xref:Singulink.IO.IAbsoluteFilePath.GetInfo*?displayProperty=nameWithType> (returns <xref:Singulink.IO.CachedFileInfo>) |

#### Directories

| `System.IO` | Library |
|-------------|---------|
| <xref:System.IO.Directory.Exists*?displayProperty=nameWithType> | <xref:Singulink.IO.IAbsolutePath.Exists?displayProperty=nameWithType> |
| <xref:System.IO.Directory.CreateDirectory*?displayProperty=nameWithType> | <xref:Singulink.IO.IAbsoluteDirectoryPath.Create*?displayProperty=nameWithType> |
| <xref:System.IO.Directory.Delete*?displayProperty=nameWithType> | <xref:Singulink.IO.IAbsoluteDirectoryPath.Delete*?displayProperty=nameWithType> |
| <xref:System.IO.Directory.Move*?displayProperty=nameWithType> | <xref:Singulink.IO.IAbsoluteDirectoryPath.MoveTo*?displayProperty=nameWithType> |
| <xref:System.IO.Directory.GetCurrentDirectory*?displayProperty=nameWithType> | <xref:Singulink.IO.DirectoryPath.GetCurrent*?displayProperty=nameWithType> |
| <xref:System.IO.Directory.SetCurrentDirectory*?displayProperty=nameWithType> | <xref:Singulink.IO.DirectoryPath.SetCurrent*?displayProperty=nameWithType> |
| <xref:System.IO.Directory.GetParent*?displayProperty=nameWithType> | <xref:Singulink.IO.IPath.ParentDirectory?displayProperty=nameWithType> |
| <xref:System.IO.Directory.GetFiles*?displayProperty=nameWithType> | <xref:Singulink.IO.IAbsoluteDirectoryPath.GetChildFiles*?displayProperty=nameWithType> |
| <xref:System.IO.Directory.GetDirectories*?displayProperty=nameWithType> | <xref:Singulink.IO.IAbsoluteDirectoryPath.GetChildDirectories*?displayProperty=nameWithType> |
| <xref:System.IO.Directory.EnumerateFileSystemEntries*?displayProperty=nameWithType> | <xref:Singulink.IO.IAbsoluteDirectoryPath.GetChildEntries*?displayProperty=nameWithType> |

#### Drives

| `System.IO` | Library |
|-------------|---------|
| <xref:System.IO.DriveInfo.GetDrives*?displayProperty=nameWithType> | <xref:Singulink.IO.DirectoryPath.GetMountingPoints*?displayProperty=nameWithType> |
| <xref:System.IO.DriveInfo.AvailableFreeSpace?displayProperty=nameWithType> | <xref:Singulink.IO.IAbsoluteDirectoryPath.AvailableFreeSpace?displayProperty=nameWithType> (on **any** absolute directory) |
| <xref:System.IO.DriveInfo.TotalFreeSpace?displayProperty=nameWithType> | <xref:Singulink.IO.IAbsoluteDirectoryPath.TotalFreeSpace?displayProperty=nameWithType> |
| <xref:System.IO.DriveInfo.TotalSize?displayProperty=nameWithType> | <xref:Singulink.IO.IAbsoluteDirectoryPath.TotalSize?displayProperty=nameWithType> |
| <xref:System.IO.DriveInfo.DriveType?displayProperty=nameWithType> | <xref:Singulink.IO.IAbsoluteDirectoryPath.DriveType?displayProperty=nameWithType> |
| <xref:System.IO.DriveInfo.DriveFormat?displayProperty=nameWithType> | <xref:Singulink.IO.IAbsoluteDirectoryPath.FileSystem?displayProperty=nameWithType> |

See [Drive and Disk Information](drive-and-disk-info.md).

## Migration Strategy

A pragmatic order of operations:

1. **Adopt at the edges first.** New code parses inputs into path objects; old code keeps working with strings until you touch it.
2. **Use <xref:Singulink.IO.SystemExtensions.ToPath*?displayProperty=nameWithType> to bridge existing <xref:System.IO.FileInfo> / <xref:System.IO.DirectoryInfo> chains** without rewriting the calling code.
3. **Replace `try`/`catch` ladders with the parse-vs-IO split.** Anything inside a parse step catches <xref:System.ArgumentException>; anything inside an I/O step catches <xref:System.IO.IOException>. Where a failed parse is routine, the `Try` parse methods avoid the exception altogether. See [Exception Handling](exception-handling.md).
4. **Convert <xref:System.IO.Directory.GetFiles*?displayProperty=nameWithType> / <xref:System.IO.Directory.EnumerateFiles*?displayProperty=nameWithType> calls** to `GetChild*` enumeration with <xref:Singulink.IO.SearchOptions>. Pay attention to <xref:Singulink.IO.SearchOptions.MatchCasing?displayProperty=nameWithType>: the library's case-insensitive default is consistent across platforms, which is a behavior change from `System.IO` on Unix.
5. **Drop <xref:System.IO.DriveInfo> workarounds.** UNC paths, mounted subdirectories and per-user quotas are all handled by the disk-space members on absolute directories.

## When You Still Need Strings

You'll still encounter APIs that demand a <xref:System.String>. Continue using <xref:Singulink.IO.IAbsolutePath.PathExport?displayProperty=nameWithType>:

```csharp
public void Extract(IAbsoluteFilePath archive, IAbsoluteDirectoryPath targetDir)
{
    ZipFile.ExtractToDirectory(archive.PathExport, targetDir.PathExport);
}
```

Don't use <xref:Singulink.IO.IPath.PathDisplay?displayProperty=nameWithType> for I/O even if it looks the same as <xref:Singulink.IO.IAbsolutePath.PathExport> for typical paths; the difference is invisible until something subtle goes wrong.

## Next Steps

Two guides go deeper on the ideas used here:

- [Path Formats](path-formats.md): the difference between <xref:Singulink.IO.IPath.PathDisplay?displayProperty=nameWithType>, <xref:Singulink.IO.IAbsolutePath.PathExport?displayProperty=nameWithType> and <xref:Singulink.IO.IPath.ToString*?displayProperty=nameWithType>.
- [Exception Handling](exception-handling.md): replace `System.IO` catch ladders with the cleaner two-phase model.

</div>
