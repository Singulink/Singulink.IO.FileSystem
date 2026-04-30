<div class="article">

# Working with Files

### Overview

File operations live on <xref:Singulink.IO.IAbsoluteFilePath>. Relative file paths can describe a file but cannot perform I/O; combine them with an absolute directory first (see [Combining and Navigating Paths](combining-and-navigating.md)).

Every method described here can throw <xref:System.IO.IOException> (or one of its subtypes: <xref:System.IO.FileNotFoundException>, <xref:System.IO.DirectoryNotFoundException>, <xref:Singulink.IO.UnauthorizedIOAccessException>, etc.). See [Exception Handling](exception-handling.md) for patterns.

## Existence Checks

#### Exists

<xref:Singulink.IO.IAbsolutePath.Exists> gives a quick boolean:

```csharp
if (file.Exists)
    file.Delete();
```

#### State

For richer information, use <xref:Singulink.IO.IAbsolutePath.State>. It returns one of four <xref:Singulink.IO.EntryState> values:

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
> Use <xref:Singulink.IO.IAbsolutePath.State> over <xref:Singulink.IO.IAbsolutePath.Exists> when your error handling needs to distinguish between "missing" and "wrong type". <xref:Singulink.IO.EntryState.WrongType> in particular catches a class of bugs that `System.IO`'s boolean checks silently obscure.

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

<xref:Singulink.IO.IAbsoluteFilePath.OpenAsyncStream*> has an identical signature to <xref:Singulink.IO.IAbsoluteFilePath.OpenStream*> but always sets <xref:System.IO.FileOptions.Asynchronous>:

```csharp
using FileStream stream = file.OpenAsyncStream(FileMode.Create, FileAccess.Write);
await stream.WriteAsync(buffer);
```

> [!NOTE]
> The OS may not actually support asynchronous I/O for the underlying handle, in which case the runtime falls back to synchronous internally. The <xref:System.IO.FileOptions.Asynchronous> option only opts in to true async when the platform allows it.

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
> <xref:Singulink.IO.IAbsoluteFilePath.MoveTo*> and <xref:Singulink.IO.IAbsoluteFilePath.CopyTo*> accept any <xref:Singulink.IO.IAbsoluteFilePath> as the destination, including across directories or drives. The destination's parent must already exist; call <xref:Singulink.IO.IAbsoluteDirectoryPath.Create*> on <xref:Singulink.IO.IAbsoluteFilePath.ParentDirectory> first if needed.

#### Replace

<xref:Singulink.IO.IAbsoluteFilePath.Replace*> atomically replaces the contents of an existing file with this file, optionally producing a backup of the replaced file:

```csharp
newFile.Replace(originalFile, backupFile: null);
newFile.Replace(originalFile, backupFile: backupPath, ignoreMetadataErrors: true);
```

<xref:Singulink.IO.IAbsoluteFilePath.Replace*> is the recommended way to atomically commit changes: write to a temporary file, then call it to swap the temp file into place.

## Deleting

Use <xref:Singulink.IO.IAbsoluteFilePath.Delete*>:

```csharp
file.Delete();                   // ignores not-found by default
file.Delete(ignoreNotFound: false); // throws FileNotFoundException if absent
```

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

- [Working with Directories](directory-operations.md): many file workflows start by creating a directory.
- [Searching and Enumeration](searching-and-enumeration.md): find the files you want to operate on.
- [Exception Handling](exception-handling.md): catch I/O errors cleanly.

</div>
