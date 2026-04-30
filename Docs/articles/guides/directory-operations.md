<div class="article">

# Working with Directories

### Overview

Directory operations live on <xref:Singulink.IO.IAbsoluteDirectoryPath>. Like files, relative directory paths can describe a location but cannot perform I/O; combine them with an absolute base directory first.

This article covers existence, creation, deletion and the small set of directory-specific properties. Enumeration and searching live in their own article. See [Searching and Enumeration](searching-and-enumeration.md).

## Existence Checks

Same model as files: <xref:Singulink.IO.IAbsolutePath.Exists> for a quick boolean, <xref:Singulink.IO.IAbsolutePath.State> for a richer answer:

```csharp
if (!dir.Exists)
    dir.Create();

switch (dir.State)
{
    case EntryState.Exists:             /* good to go */ break;
    case EntryState.ParentExists:       /* parent is there, can create */ break;
    case EntryState.ParentDoesNotExist: /* Create() will still work */ break;
    case EntryState.WrongType:          /* a file is here */ break;
}
```

See the table in [Working with Files](file-operations.md#state) for the full meaning of each <xref:Singulink.IO.EntryState> value.

## Creating Directories

<xref:Singulink.IO.IAbsoluteDirectoryPath.Create*> creates the directory and any missing parent directories. It is idempotent; calling it on an already-existing directory does nothing.

```csharp
IAbsoluteDirectoryPath logsDir = appBase.CombineDirectory("logs/2026/04");
logsDir.Create();   // creates logs, 2026 and 04 as needed
```

> [!TIP]
> Before writing a file at a new location, the simplest pattern is `file.ParentDirectory.Create()`, a single call that ensures every directory in the chain exists.

## Deleting Directories

<xref:Singulink.IO.IAbsoluteDirectoryPath.Delete*> has the following signature:

```csharp
void Delete(bool recursive = false, bool ignoreNotFound = true);
```

Default behavior:

- Non-recursive (the directory must be empty, otherwise <xref:System.IO.IOException> is thrown).
- Missing directories are ignored silently.

```csharp
dir.Delete();                             // empty directories only
dir.Delete(recursive: true);              // remove children too
dir.Delete(ignoreNotFound: false);        // throw if absent
dir.Delete(recursive: true, ignoreNotFound: false);
```

> [!CAUTION]
> `recursive: true` is destructive: every file and subdirectory below the path is removed. Confirm the path is what you expect before calling it on user-influenced input.

## Useful Properties

#### IsRoot

<xref:Singulink.IO.IAbsoluteDirectoryPath.IsRoot> is `true` for a root directory (`C:\`, `/`, a UNC share root). Roots have no parent; <xref:Singulink.IO.IPath.HasParentDirectory> is `false`.

```csharp
file.RootDirectory.IsRoot;   // true
```

#### IsEmpty

<xref:Singulink.IO.IAbsoluteDirectoryPath.IsEmpty> returns `true` if the directory exists and contains no entries:

```csharp
if (dir.Exists && dir.IsEmpty)
    dir.Delete();
```

<xref:Singulink.IO.IAbsoluteDirectoryPath.IsEmpty> requires the directory to exist; otherwise the underlying API throws.

## Attributes and Timestamps

Inherited from <xref:Singulink.IO.IAbsolutePath>. Same surface as files:

```csharp
dir.Attributes;
dir.CreationTimeUtc;
dir.LastWriteTimeUtc = DateTime.UtcNow;
```

For a single consistent snapshot, call <xref:Singulink.IO.IAbsoluteDirectoryPath.GetInfo*> to obtain a <xref:Singulink.IO.CachedDirectoryInfo>. See [Cached Entry Info](cached-entry-info.md).

## Common Patterns

#### Clear a directory

```csharp
dir.Delete(recursive: true);
dir.Create();
```

This is the simplest reliable way to start from an empty directory. It avoids enumerate-and-delete races.

#### Mirror a structure

```csharp
foreach (IRelativeFilePath rel in source.GetRelativeChildFiles("*", new SearchOptions { Recursive = true }))
{
    IAbsoluteFilePath dest = target + rel;
    dest.ParentDirectory.Create();
    (source + rel).CopyTo(dest, overwrite: true);
}
```

See [Searching and Enumeration](searching-and-enumeration.md) for the full enumeration surface.

#### Preserve only certain content

```csharp
foreach (IAbsoluteFilePath log in dir.GetChildFiles("*.log"))
{
    if (log.LastWriteTimeUtc < DateTime.UtcNow.AddDays(-7))
        log.Delete();
}
```

## Disk Space and Drive Type

Every absolute directory exposes disk-space information for the volume it lives on via <xref:Singulink.IO.IAbsoluteDirectoryPath.AvailableFreeSpace>, <xref:Singulink.IO.IAbsoluteDirectoryPath.TotalFreeSpace>, <xref:Singulink.IO.IAbsoluteDirectoryPath.TotalSize>, <xref:Singulink.IO.IAbsoluteDirectoryPath.DriveType> and <xref:Singulink.IO.IAbsoluteDirectoryPath.FileSystem>:

```csharp
dir.AvailableFreeSpace;
dir.TotalFreeSpace;
dir.TotalSize;
dir.DriveType;
dir.FileSystem;   // e.g. "NTFS"
```

See [Drive and Disk Information](drive-and-disk-info.md).

## Next Steps

- [Searching and Enumeration](searching-and-enumeration.md): query the contents of a directory.
- [Cached Entry Info](cached-entry-info.md): work with consistent metadata snapshots.
- [Drive and Disk Information](drive-and-disk-info.md): pre-flight free-space checks and drive metadata.

</div>
