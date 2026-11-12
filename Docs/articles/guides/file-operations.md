<div class="article">

# Working with Files

File operations live on <xref:Singulink.IO.IAbsoluteFilePath>. Relative file paths can describe a file but cannot perform I/O; combine them with an absolute directory first (see [Combining and Navigating Paths](combining-and-navigating.md)).

Every method described here can throw <xref:System.IO.IOException> (or one of its subtypes: <xref:System.IO.FileNotFoundException>, <xref:System.IO.DirectoryNotFoundException>, <xref:Singulink.IO.UnauthorizedIOAccessException>, etc.). See [Exception Handling](exception-handling.md) for patterns.

## Existence Checks

#### Exists

<xref:Singulink.IO.IAbsolutePath.Exists?displayProperty=nameWithType> gives a quick boolean:

```csharp
if (file.Exists)
    file.Delete();
```

#### State

For richer information, use <xref:Singulink.IO.IAbsolutePath.State?displayProperty=nameWithType>. It returns one of four <xref:Singulink.IO.EntryState> values:

| Value | Meaning |
|-------|---------|
| <xref:Singulink.IO.EntryState.Exists> | The file exists. |
| <xref:Singulink.IO.EntryState.ParentExists> | The file does not exist, but its parent directory does. |
| <xref:Singulink.IO.EntryState.ParentDoesNotExist> | Neither the file nor its parent directory exist. |
| <xref:Singulink.IO.EntryState.WrongType> | A different kind of entry (e.g. a directory) exists at this path. |

```csharp
switch (file.State)
{
    case EntryState.Exists:             /* open and read */ break;
    case EntryState.ParentExists:       /* prompt user, can create */ break;
    case EntryState.ParentDoesNotExist: /* would need to create the directory tree */ break;
    case EntryState.WrongType:          /* a directory is here: refuse */ break;
}
```

> [!TIP]
> Use <xref:Singulink.IO.IAbsolutePath.State?displayProperty=nameWithType> over <xref:Singulink.IO.IAbsolutePath.Exists?displayProperty=nameWithType> when your error handling needs to distinguish between "missing" and "wrong type". <xref:Singulink.IO.EntryState.WrongType?displayProperty=nameWithType> in particular catches a class of bugs that `System.IO`'s boolean checks silently obscure.

## Opening Streams

#### OpenStream

<xref:Singulink.IO.IAbsoluteFilePath.OpenStream*> has the following signature:

```csharp
FileStream OpenStream(
    FileMode mode = FileMode.Open,
    FileAccess access = FileAccess.ReadWrite,
    FileShare share = FileShare.None,
    int bufferSize = 4096,
    FileOptions options = FileOptions.None);
```

Always wrap the returned <xref:System.IO.FileStream> in `using`:

```csharp
using FileStream stream = configFile.OpenStream(FileMode.Open, FileAccess.Read, FileShare.Read);
using StreamReader reader = new(stream);
string text = reader.ReadToEnd();
```

For writes, choose the appropriate <xref:System.IO.FileMode>:

| <xref:System.IO.FileMode> | Behavior |
|---|---|
| <xref:System.IO.FileMode.Open> | File must exist; throws otherwise. |
| <xref:System.IO.FileMode.OpenOrCreate> | Open if exists, create if not. |
| <xref:System.IO.FileMode.Create> | Always create (truncates if exists). |
| <xref:System.IO.FileMode.CreateNew> | Create; throw if exists. |
| <xref:System.IO.FileMode.Append> | Open for append (write only); create if missing. |
| <xref:System.IO.FileMode.Truncate> | Open and truncate; file must exist. |

#### OpenAsyncStream

<xref:Singulink.IO.IAbsoluteFilePath.OpenAsyncStream*> has an identical signature to <xref:Singulink.IO.IAbsoluteFilePath.OpenStream*> but always sets <xref:System.IO.FileOptions.Asynchronous?displayProperty=nameWithType>:

```csharp
using FileStream stream = file.OpenAsyncStream(FileMode.Create, FileAccess.Write);
await stream.WriteAsync(buffer);
```

> [!NOTE]
> The OS may not actually support asynchronous I/O for the underlying handle, in which case the runtime falls back to synchronous internally. The <xref:System.IO.FileOptions.Asynchronous?displayProperty=nameWithType> option only opts in to true async when the platform allows it.

## Opening Handles

A <xref:System.IO.FileStream> keeps a position and a buffer. Code that reads or writes a file in a few large pieces at offsets it already knows needs neither, and can work with the file's handle directly through the static methods of <xref:System.IO.RandomAccess>, which take the handle and an offset.

#### OpenHandle

<xref:Singulink.IO.IAbsoluteFilePath.OpenHandle*> returns a <xref:Microsoft.Win32.SafeHandles.SafeFileHandle>. It takes the same arguments as <xref:Singulink.IO.IAbsoluteFilePath.OpenStream*> with the same defaults, except that it has no buffer size and adds a preallocation size:

```csharp
SafeFileHandle OpenHandle(
    FileMode mode = FileMode.Open,
    FileAccess access = FileAccess.ReadWrite,
    FileShare share = FileShare.None,
    FileOptions options = FileOptions.None,
    long preallocationSize = 0);
```

Dispose the handle like a stream:

```csharp
using SafeFileHandle handle = file.OpenHandle(FileMode.Open, FileAccess.Read, FileShare.Read);

byte[] header = new byte[16];
int read = RandomAccess.Read(handle, header, fileOffset: 0);
long length = RandomAccess.GetLength(handle);
```

```csharp
using SafeFileHandle handle = file.OpenHandle(FileMode.CreateNew, FileAccess.Write, preallocationSize: data.Length);

RandomAccess.Write(handle, data, fileOffset: 0);
RandomAccess.FlushToDisk(handle);   // see Flushing to Disk below
```

The preallocation size is a hint to the file system that only has an effect when a file is created or overwritten with write access. It does not change the length of the file.

> [!TIP]
> Each call on <xref:System.IO.RandomAccess> goes straight to the operating system, so a handle suits a few large reads or writes. For many small ones, the buffer of a <xref:System.IO.FileStream> is what makes them fast.

#### OpenAsyncHandle

<xref:Singulink.IO.IAbsoluteFilePath.OpenAsyncHandle*> has an identical signature to <xref:Singulink.IO.IAbsoluteFilePath.OpenHandle*> but always sets <xref:System.IO.FileOptions.Asynchronous?displayProperty=nameWithType>, for use with <xref:System.IO.RandomAccess.ReadAsync*?displayProperty=nameWithType> and <xref:System.IO.RandomAccess.WriteAsync*?displayProperty=nameWithType>:

```csharp
using SafeFileHandle handle = file.OpenAsyncHandle(FileMode.Create, FileAccess.Write);
await RandomAccess.WriteAsync(handle, buffer, fileOffset: 0);
```

## File Properties

#### Length and IsReadOnly

Use <xref:Singulink.IO.IAbsoluteFilePath.Length> and <xref:Singulink.IO.IAbsoluteFilePath.IsReadOnly>:

```csharp
long bytes = file.Length;
bool readOnly = file.IsReadOnly;
file.IsReadOnly = true;   // toggle the read-only attribute
```

#### Attributes and Timestamps

Inherited from <xref:Singulink.IO.IAbsolutePath>:

```csharp
file.Attributes;            // FileAttributes
file.CreationTime;          // local time
file.CreationTimeUtc;
file.LastAccessTime;
file.LastWriteTime;
file.LastWriteTimeUtc;

file.Attributes |= FileAttributes.Hidden;
file.LastWriteTimeUtc = DateTime.UtcNow;
```

> [!TIP]
> Each attribute/timestamp access touches the file system. If you'll read several at once, prefer <xref:Singulink.IO.IAbsoluteFilePath.GetInfo*> which fetches everything in a single call. See [Cached Entry Info](cached-entry-info.md).

## Copy, Move, Replace

#### CopyTo

Use <xref:Singulink.IO.IAbsoluteFilePath.CopyTo*>:

```csharp
file.CopyTo(destination);                 // throws if destination exists
file.CopyTo(destination, overwrite: true);
```

#### MoveTo

Use <xref:Singulink.IO.IAbsoluteFilePath.MoveTo*>:

```csharp
file.MoveTo(destination);
file.MoveTo(destination, overwrite: true);
```

> [!NOTE]
> <xref:Singulink.IO.IAbsoluteFilePath.MoveTo*> and <xref:Singulink.IO.IAbsoluteFilePath.CopyTo*> accept any <xref:Singulink.IO.IAbsoluteFilePath> as the destination, including across directories or drives. The destination's parent must already exist; call <xref:Singulink.IO.IAbsoluteDirectoryPath.Create*?displayProperty=nameWithType> on the destination's <xref:Singulink.IO.IAbsoluteFilePath.ParentDirectory?displayProperty=nameWithType> first if needed.

#### Replace

<xref:Singulink.IO.IAbsoluteFilePath.Replace*> atomically replaces the contents of an existing file with this file, optionally producing a backup of the replaced file:

```csharp
newFile.Replace(originalFile, backupFile: null);
newFile.Replace(originalFile, backupFile: backupPath, ignoreMetadataErrors: true);
```

<xref:Singulink.IO.IAbsoluteFilePath.Replace*> is the recommended way to atomically commit changes: write to a temporary file, then call it to swap the temp file into place.

#### MoveToExclusive

A move can decide between parties: workers that each claim a job file by moving it to a name of their own, or two processes that each want to change the state of a file by renaming it. That only works if one of them wins. <xref:Singulink.IO.IAbsoluteFilePath.MoveTo*> does not ensure it: on Windows, when the same file is moved to two different names at the same time, both moves report success and the file ends up under the name that was applied last.

<xref:Singulink.IO.IAbsoluteFilePath.MoveToExclusive*> moves the file in such a way that, of several attempts to move the same file at the same time, at most one succeeds:

```csharp
try
{
    job.MoveToExclusive(claimedJob);
    // This worker has the job.
}
catch (FileNotFoundException)
{
    // Another worker has it.
}
```

A party that loses gets a <xref:System.IO.FileNotFoundException> once the file has been moved. On Windows, a party whose attempt overlaps the winner's gets an <xref:System.IO.IOException> for a sharing violation instead.

> [!IMPORTANT]
> On Windows, any handle that is open on the file without sharing deletion causes the same sharing violation, so that exception alone does not say that another party moved the file. A caller that tries again has lost when the attempt throws <xref:System.IO.FileNotFoundException>.

It differs from <xref:Singulink.IO.IAbsoluteFilePath.MoveTo*> in a few other ways:

- The file is never copied, so the destination must be on the same volume. A destination on another volume throws <xref:System.IO.IOException>.
- On Windows, handles that are open on the file and share deletion (<xref:System.IO.FileShare.Delete?displayProperty=nameWithType>), such as those of readers and of a writer appending to it, do not block the move and stay valid. On Unix, open handles never block it.
- With `overwrite: false`, an existing destination is refused as part of the rename on Windows and on Linux. On other Unix platforms, and on Linux file systems that do not support that (NFS, for example), the destination is checked before the rename, so a file that is created there between the two is replaced.

The guarantee is between callers of <xref:Singulink.IO.IAbsoluteFilePath.MoveToExclusive*>.

## Deleting

Use <xref:Singulink.IO.IAbsoluteFilePath.Delete*>:

```csharp
file.Delete();                      // ignores not-found by default
file.Delete(ignoreNotFound: false); // throws FileNotFoundException if absent
```

## Flushing to Disk

Writing to a file and closing its stream only hands the data to the operating system, which writes it to the disk some time later. Until then, a power loss or an operating system crash can lose it. <xref:Singulink.IO.IAbsoluteFilePath.FlushToDisk*> forces the data and metadata of an existing file onto the disk before it returns:

```csharp
using (FileStream s = file.OpenStream(FileMode.Create, FileAccess.Write))
    s.Write(data);

file.FlushToDisk();
```

The file is opened for writing while it is flushed, because Windows only flushes a file through a handle with write access. A read-only file therefore cannot be flushed, and other handles that are open on the file must share writing.

> [!NOTE]
> Only data that the operating system already has is flushed. If a <xref:System.IO.FileStream> on the file is still open, flush or dispose it first, or call <xref:System.IO.FileStream.Flush(System.Boolean)?displayProperty=nameWithType> on it with `true` instead, which writes the stream's buffer and then flushes the file to disk.

#### New, Renamed and Moved Files

The name of a file is an entry of its directory and not part of the file. A file that was just created, renamed or moved is only durable under its name once its directory is flushed as well, with <xref:Singulink.IO.IAbsoluteDirectoryPath.FlushToDisk*?displayProperty=nameWithType>:

```csharp
file.FlushToDisk();                   // the contents
file.ParentDirectory.FlushToDisk();   // the name
```

What flushing a directory does differs between Windows and Unix. See [Flushing to Disk](directory-operations.md#flushing-to-disk) in Working with Directories, which also shows how to make a renamed file durable on both.

#### Missing Files

A file that does not exist throws <xref:System.IO.FileNotFoundException> by default. That differs from <xref:Singulink.IO.IAbsoluteFilePath.Delete*> on purpose: if the file is missing then nothing was made durable, which is usually a mistake. Pass `ignoreNotFound: true` where a missing file is expected, such as a file that another process may delete at any time:

```csharp
file.FlushToDisk(ignoreNotFound: true);
```

What is ignored is the same as what <xref:Singulink.IO.IAbsoluteFilePath.Delete*> ignores: a file that does not exist in a parent directory that does. A parent directory that does not exist still throws <xref:System.IO.DirectoryNotFoundException>, and a path that points to a directory still throws <xref:System.IO.IOException>.

## Putting It Together

Atomic write pattern:

```csharp
IAbsoluteFilePath finalPath = appBase.CombineFile("config/app.json");
IAbsoluteFilePath tempPath = finalPath.AddExtension(".tmp");

finalPath.ParentDirectory.Create();

using (FileStream s = tempPath.OpenStream(FileMode.Create, FileAccess.Write))
using (StreamWriter w = new(s))
{
    w.Write(serializedJson);
}

if (finalPath.Exists)
    tempPath.Replace(finalPath, backupFile: null);
else
    tempPath.MoveTo(finalPath);
```

## Cached Snapshots

If you want a single consistent view of a file's metadata (size, attributes, timestamps), call <xref:Singulink.IO.IAbsoluteFilePath.GetInfo*> to obtain a <xref:Singulink.IO.CachedFileInfo>. See [Cached Entry Info](cached-entry-info.md).

## Next Steps

File work usually goes hand in hand with these topics:

- [Working with Directories](directory-operations.md): many file workflows start by creating a directory.
- [Searching and Enumeration](searching-and-enumeration.md): find the files you want to operate on.
- [Exception Handling](exception-handling.md): catch I/O errors cleanly.

</div>
