<div class="article">

# Cached Entry Info

### Overview

<xref:Singulink.IO.CachedEntryInfo> is the library's replacement for <xref:System.IO.FileInfo> and <xref:System.IO.DirectoryInfo>. It captures a consistent snapshot of an entry's metadata in a single file system call. Two concrete types exist:

- <xref:Singulink.IO.CachedFileInfo>: adds <xref:Singulink.IO.CachedFileInfo.Length> and <xref:Singulink.IO.CachedFileInfo.IsReadOnly>.
- <xref:Singulink.IO.CachedDirectoryInfo>: narrows <xref:Singulink.IO.CachedEntryInfo.Path> to <xref:Singulink.IO.IAbsoluteDirectoryPath>.

The design fixes a handful of long-standing pitfalls in the `System.IO` equivalents.

## What's Different

#### Existence-Guaranteed at Construction

You cannot obtain a <xref:Singulink.IO.CachedEntryInfo> for a path that doesn't exist (or is the wrong type). Construction validates both up front and throws otherwise. There's no chance of holding an info object whose properties surprise-throw on first access.

#### Read-Only Properties

All properties are read-only. To **change** a file's attributes or timestamps, go through the <xref:Singulink.IO.CachedEntryInfo.Path> property (which is a real path object) and then call <xref:Singulink.IO.CachedEntryInfo.Refresh*> to update the cached snapshot:

```csharp
CachedFileInfo info = file.GetInfo();

info.Path.Attributes |= FileAttributes.Hidden;
info.Path.LastWriteTimeUtc = DateTime.UtcNow;

info.Refresh();   // re-query the file system
```

#### Type-Stable

If something on disk changes the entry from a file into a directory (or vice versa), <xref:Singulink.IO.CachedEntryInfo.Refresh*> throws <xref:System.IO.IOException> rather than silently returning misleading data.

## Obtaining a CachedEntryInfo

#### From a Path

Call <xref:Singulink.IO.IAbsolutePath.GetInfo*> on any <xref:Singulink.IO.IAbsolutePath>. The static return type matches the path:

```csharp
CachedFileInfo fileInfo = absoluteFilePath.GetInfo();
CachedDirectoryInfo dirInfo = absoluteDirectoryPath.GetInfo();
```

<xref:Singulink.IO.IAbsolutePath.GetInfo*> throws if the entry doesn't exist or is the wrong type.

#### From a Path String (Type Not Known)

When you only have a string and don't statically know whether it refers to a file or a directory, use the <xref:Singulink.IO.CachedEntryInfo.Create*> factory:

```csharp
CachedEntryInfo info = CachedEntryInfo.Create(pathString);

switch (info)
{
    case CachedFileInfo file:      /* handle file */      break;
    case CachedDirectoryInfo dir:  /* handle directory */ break;
}
```

The factory inspects the trailing segment of the string: a trailing separator, `.` or `..` makes it directory-shaped and the result is a <xref:Singulink.IO.CachedDirectoryInfo>; otherwise the path is probed and the correct concrete type is returned based on what actually exists on disk. If the entry is missing, <xref:System.IO.FileNotFoundException> (or <xref:System.IO.DirectoryNotFoundException> if its parent doesn't exist) is thrown. A directory-shaped path that resolves to a file is a hard error and throws <xref:System.IO.IOException>.

When you know the expected kind, prefer the typed shadows <xref:Singulink.IO.CachedFileInfo.Create*> and <xref:Singulink.IO.CachedDirectoryInfo.Create*>. They have the same dispatch logic but throw <xref:System.IO.IOException> if the path resolves to the other kind:

```csharp
CachedFileInfo file = CachedFileInfo.Create(pathString);          // throws if path is a directory
CachedDirectoryInfo dir = CachedDirectoryInfo.Create(pathString); // throws if path is a file
```

#### From a Relative Sub-Path

<xref:Singulink.IO.IAbsoluteDirectoryPath.GetInfo*> on an <xref:Singulink.IO.IAbsoluteDirectoryPath> also accepts a relative path string. The overload combines the relative path with the directory and resolves the resulting entry in a single call, returning the correct concrete type:

```csharp
CachedEntryInfo info = appBase.GetInfo("config/users.json");

// Trailing separator forces a directory:
CachedDirectoryInfo logsDir = (CachedDirectoryInfo)appBase.GetInfo("logs/");
```

Like <xref:Singulink.IO.CachedEntryInfo.Create*>, this overload uses the trailing segment to disambiguate file vs directory shape. Pass a <xref:Singulink.IO.RelativePathFormat> to control how the relative segment is parsed (see [Combining and Navigating Paths](combining-and-navigating.md)).

#### From a System.IO Info Object

Use the <xref:Singulink.IO.SystemExtensions.ToCachedInfo*> extension methods to bridge from <xref:System.IO.FileInfo> / <xref:System.IO.DirectoryInfo> / <xref:System.IO.FileSystemInfo>:

```csharp
FileInfo fi = new(@"C:\some\file.txt");
CachedFileInfo file = fi.ToCachedInfo();

FileSystemInfo fsi = picker.SelectedItem;          // could be either
CachedEntryInfo info = fsi.ToCachedInfo();         // returns the right concrete type
```

#### From Enumeration

Use the `*Info` enumeration variants (see [Searching and Enumeration](searching-and-enumeration.md)) to get cached info objects directly without an extra stat per entry:

```csharp
foreach (CachedFileInfo info in dir.GetChildFilesInfo("*.log"))
    Console.WriteLine($"{info.Path.Name,-30} {info.Length,12:N0} {info.LastWriteTimeUtc:O}");
```

## Properties

<xref:Singulink.IO.CachedEntryInfo> (base):

- <xref:Singulink.IO.CachedEntryInfo.Path>: the underlying <xref:Singulink.IO.IAbsolutePath>.
- <xref:Singulink.IO.CachedEntryInfo.Attributes>.
- <xref:Singulink.IO.CachedEntryInfo.CreationTime> / <xref:Singulink.IO.CachedEntryInfo.CreationTimeUtc>.
- <xref:Singulink.IO.CachedEntryInfo.LastAccessTime> / <xref:Singulink.IO.CachedEntryInfo.LastAccessTimeUtc>.
- <xref:Singulink.IO.CachedEntryInfo.LastWriteTime> / <xref:Singulink.IO.CachedEntryInfo.LastWriteTimeUtc>.

<xref:Singulink.IO.CachedFileInfo> adds:

- <xref:Singulink.IO.CachedFileInfo.Path> narrowed to <xref:Singulink.IO.IAbsoluteFilePath>.
- <xref:Singulink.IO.CachedFileInfo.IsReadOnly>.
- <xref:Singulink.IO.CachedFileInfo.Length>.

<xref:Singulink.IO.CachedDirectoryInfo> adds only the narrowed <xref:Singulink.IO.CachedDirectoryInfo.Path> to <xref:Singulink.IO.IAbsoluteDirectoryPath>.

## Refresh

<xref:Singulink.IO.CachedEntryInfo.Refresh*> re-queries the file system and updates the cached values. Call it whenever you suspect the underlying entry may have changed:

```csharp
info.Refresh();
```

If the entry no longer exists or has changed type (a file became a directory, etc.), <xref:Singulink.IO.CachedEntryInfo.Refresh*> throws <xref:System.IO.IOException>.

## When to Use Cached Info

Use cached info when you need **multiple** pieces of metadata for the same entry:

```csharp
// Multiple property accesses on a path each touch the file system:
file.Length;            // stat
file.LastWriteTimeUtc;  // stat
file.Attributes;        // stat

// One stat, multiple reads:
CachedFileInfo info = file.GetInfo();
info.Length;
info.LastWriteTimeUtc;
info.Attributes;
```

Use cached info during enumeration (`Get*Info` methods) whenever you'll read metadata from the results. The file system call that produced the entry already had the metadata, and the `*Info` variants surface it without a second round-trip.

## When Not to Use Cached Info

For one-off checks where you only need a single property, going through the path directly is fine and slightly cheaper than constructing a snapshot:

```csharp
if (file.LastWriteTimeUtc > cutoff) { /* ... */ }
```

## Next Steps

- [Searching and Enumeration](searching-and-enumeration.md): the `*Info` variants are designed to pair with this article.
- [Working with Files](file-operations.md) and [Working with Directories](directory-operations.md): the path-side counterparts.

</div>
