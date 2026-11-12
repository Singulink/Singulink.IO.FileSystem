<div class="article">

# Working with Directories

Directory operations live on <xref:Singulink.IO.IAbsoluteDirectoryPath>. Like files, relative directory paths can describe a location but cannot perform I/O; combine them with an absolute base directory first. This article covers existence checks, creation, deletion, moving, flushing to disk and the small set of directory-specific properties, while enumeration lives in [Searching and Enumeration](searching-and-enumeration.md).

## Existence Checks

Same model as files: <xref:Singulink.IO.IAbsolutePath.Exists?displayProperty=nameWithType> for a quick boolean, <xref:Singulink.IO.IAbsolutePath.State?displayProperty=nameWithType> for a richer answer:

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
> Before writing a file at a new location, the simplest pattern is to call <xref:Singulink.IO.IAbsoluteDirectoryPath.Create*> on the file's <xref:Singulink.IO.IAbsoluteFilePath.ParentDirectory?displayProperty=nameWithType>. That single call ensures every directory in the chain exists.

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

## Moving Directories

Use <xref:Singulink.IO.IAbsoluteDirectoryPath.MoveTo*> to rename or relocate a directory:

```csharp
dir.MoveTo(targetDir);
```

The argument is the new path of the directory itself, not its parent. The destination must be on the same volume and must not already exist; otherwise <xref:System.IO.IOException> is thrown. To move across volumes, enumerate the contents and copy them instead (see the [Mirror a Structure](#mirror-a-structure) pattern below).

## Flushing to Disk

Creating, renaming, moving or deleting a file or directory changes the entries of the directory it is in, and the operating system writes those changes to the disk some time later, as it does with file data. <xref:Singulink.IO.IAbsoluteDirectoryPath.FlushToDisk*> forces the entries of an existing directory onto the disk, so that what was created in, renamed into or removed from it survives a power loss or an operating system crash:

```csharp
IAbsoluteFilePath file = dir.CombineFile("journal.dat");

using (FileStream s = file.OpenStream(FileMode.CreateNew, FileAccess.Write))
{
    s.Write(data);
    s.Flush(flushToDisk: true);   // the contents
}

dir.FlushToDisk();                // the name
```

Only the directory's own entries are flushed: not the contents of the files in it (see [Flushing to Disk](file-operations.md#flushing-to-disk) in Working with Files), and not the entries of its subdirectories or of its parent directory. A directory's own name is an entry of its parent directory.

#### On Unix

The directory is opened and flushed with `fsync`. On Apple platforms `F_FULLFSYNC` is tried first, as it also flushes the drive's write cache, and `fsync` is used if it fails.

Some file systems, network mounts in particular, refuse to flush a directory. That is tolerated: if `fsync` fails with `EINVAL`, `EBADF` or `ENOTSUP`, the call returns normally without anything having been flushed, and the entries are then only as durable as that file system makes them by itself. Any other failure throws <xref:System.IO.IOException>.

#### On Windows

Nothing is flushed, and the call only checks that the directory exists (see [Missing Directories](#missing-directories) for skipping that check). NTFS records changes to directories in its log, and flushing any file on the volume forces that log to disk. A change to a directory is therefore durable once a file on the same volume has been flushed after the change was made. Windows file systems that keep no such log, such as FAT32 and exFAT, give no equivalent guarantee.

> [!IMPORTANT]
> On Windows the file flush has to come after the change to the directory. Flushing a file and then renaming it leaves the rename unflushed there, no matter which directories are flushed afterwards.

#### Durable on Both

Flush a file after the change to its directory, then flush the directory. For a file that is written under a temporary name and then renamed:

```csharp
tempFile.FlushToDisk();                         // the contents, before the file gets its final name
tempFile.MoveTo(finalFile, overwrite: true);

finalFile.FlushToDisk();                        // Windows: forces the rename to disk
finalFile.ParentDirectory.FlushToDisk();        // Unix: flushes the entries of the directory
```

#### Missing Directories

A directory that does not exist throws <xref:System.IO.DirectoryNotFoundException> by default. That differs from <xref:Singulink.IO.IAbsoluteDirectoryPath.Delete*> on purpose: if the directory is missing then nothing was made durable, which is usually a mistake. Pass `ignoreNotFound: true` where a missing directory is expected:

```csharp
dir.FlushToDisk(ignoreNotFound: true);
```

On Unix, what is ignored is the same as what <xref:Singulink.IO.IAbsoluteDirectoryPath.Delete*> ignores: a directory that does not exist in a parent directory that does. A parent directory that does not exist still throws <xref:System.IO.DirectoryNotFoundException>, and a path that points to a file still throws <xref:System.IO.IOException>.

On Windows, the existence of the directory is all that a flush checks, so with `ignoreNotFound: true` nothing is checked and no system call is made.

> [!NOTE]
> Because nothing is checked on Windows with `ignoreNotFound: true`, a parent directory that does not exist and a path that points to a file pass there as well, where they throw on Unix.

## Useful Properties

#### IsRoot

<xref:Singulink.IO.IAbsoluteDirectoryPath.IsRoot> is `true` for a root directory (`C:\`, `/`, a UNC share root). Roots have no parent, so <xref:Singulink.IO.IPath.HasParentDirectory?displayProperty=nameWithType> is `false`.

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

#### Clear a Directory

```csharp
dir.Delete(recursive: true);
dir.Create();
```

This is the simplest reliable way to start from an empty directory. It avoids enumerate-and-delete races.

#### Mirror a Structure

```csharp
foreach (IRelativeFilePath rel in source.GetRelativeChildFiles("*", new SearchOptions { Recursive = true }))
{
    IAbsoluteFilePath dest = target + rel;
    dest.ParentDirectory.Create();
    (source + rel).CopyTo(dest, overwrite: true);
}
```

See [Searching and Enumeration](searching-and-enumeration.md) for the full enumeration surface.

#### Prune Old Files

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

These guides extend what you can do with a directory:

- [Searching and Enumeration](searching-and-enumeration.md): query the contents of a directory.
- [Cached Entry Info](cached-entry-info.md): work with consistent metadata snapshots.
- [Drive and Disk Information](drive-and-disk-info.md): pre-flight free-space checks and drive metadata.

</div>
