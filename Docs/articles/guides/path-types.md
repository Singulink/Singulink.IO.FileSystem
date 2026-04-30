<div class="article">

# Path Types

### Overview

Every path in **Singulink.IO.FileSystem** is represented by an interface. The interface tells you, statically, two independent facts about the path:

1. Is it **absolute** or **relative**?
2. Does it point to a **file** or a **directory**?

The combination produces four concrete interfaces (one of which every path object implements) plus three abstractions you can use when only one of the two facts matters.

## The Interface Hierarchy

The hierarchy splits along two independent axes and combines into four leaf interfaces. Each leaf is the type used in practice; the others exist so your APIs can ask for exactly as much as they need.

| Path kind | Implements |
|-----------|------------|
| <xref:Singulink.IO.IAbsoluteFilePath> | <xref:Singulink.IO.IAbsolutePath> + <xref:Singulink.IO.IFilePath> |
| <xref:Singulink.IO.IAbsoluteDirectoryPath> | <xref:Singulink.IO.IAbsolutePath> + <xref:Singulink.IO.IDirectoryPath> |
| <xref:Singulink.IO.IRelativeFilePath> | <xref:Singulink.IO.IRelativePath> + <xref:Singulink.IO.IFilePath> |
| <xref:Singulink.IO.IRelativeDirectoryPath> | <xref:Singulink.IO.IRelativePath> + <xref:Singulink.IO.IDirectoryPath> |

The two axes:

- **Absolute vs relative**: exposed by <xref:Singulink.IO.IAbsolutePath> and <xref:Singulink.IO.IRelativePath>. Both extend <xref:Singulink.IO.IPath>.
- **File vs directory**: exposed by <xref:Singulink.IO.IFilePath> and <xref:Singulink.IO.IDirectoryPath>. Both extend <xref:Singulink.IO.IPath>.

Every concrete path implements one of the four leaves and inherits members from both axes. Use the most specific interface you can in your APIs; it's how the type system catches mistakes for you.

## Members by Interface

#### IPath

The common base of every path, <xref:Singulink.IO.IPath>. Members:

- <xref:Singulink.IO.IPath.Name>: the final segment (file or directory name).
- <xref:Singulink.IO.IPath.PathDisplay>: friendly string suitable for display, logs and round-trippable serialization.
- <xref:Singulink.IO.IPath.PathFormat>: the <xref:Singulink.IO.PathFormat> of the path (Windows, Unix or Universal).
- <xref:Singulink.IO.IPath.HasParentDirectory>, <xref:Singulink.IO.IPath.ParentDirectory>: walk upward.
- <xref:Singulink.IO.IPath.IsRooted>: `true` for absolute paths and Windows rooted-relative paths (e.g. `\Some\Path`).
- <xref:Singulink.IO.IPath.Equals*>, <xref:Singulink.IO.IPath.op_Equality*> and <xref:Singulink.IO.IPath.op_Inequality*>: see [Equality](#equality) below.
- <xref:Singulink.IO.IPath.ToString*>: diagnostic only; **never use this for I/O**.

#### IAbsolutePath

<xref:Singulink.IO.IAbsolutePath> adds members that only make sense for fully-qualified paths:

- <xref:Singulink.IO.IAbsolutePath.PathExport>: the only string form safe to hand to non-library APIs.
- <xref:Singulink.IO.IAbsolutePath.IsUnc>: `true` for UNC paths (Windows only).
- <xref:Singulink.IO.IAbsolutePath.Exists>: convenience boolean.
- <xref:Singulink.IO.IAbsolutePath.State>: richer status, returns one of the <xref:Singulink.IO.EntryState> values: <xref:Singulink.IO.EntryState.Exists>, <xref:Singulink.IO.EntryState.ParentExists>, <xref:Singulink.IO.EntryState.ParentDoesNotExist> or <xref:Singulink.IO.EntryState.WrongType>.
- <xref:Singulink.IO.IAbsolutePath.Attributes> (get/set), <xref:Singulink.IO.IAbsolutePath.CreationTime>, <xref:Singulink.IO.IAbsolutePath.CreationTimeUtc>, <xref:Singulink.IO.IAbsolutePath.LastAccessTime>, <xref:Singulink.IO.IAbsolutePath.LastAccessTimeUtc>, <xref:Singulink.IO.IAbsolutePath.LastWriteTime>, <xref:Singulink.IO.IAbsolutePath.LastWriteTimeUtc>.
- <xref:Singulink.IO.IAbsolutePath.RootDirectory>, <xref:Singulink.IO.IAbsolutePath.ParentDirectory> (narrowed to <xref:Singulink.IO.IAbsoluteDirectoryPath>).
- <xref:Singulink.IO.IAbsolutePath.GetInfo*>: returns a <xref:Singulink.IO.CachedEntryInfo>.
- <xref:Singulink.IO.IAbsolutePath.GetLastExistingDirectory*>: walks up the path until a directory that exists is found.

#### IRelativePath

<xref:Singulink.IO.IRelativePath> adds:

- <xref:Singulink.IO.IRelativePath.ToPathFormat*>: convert a relative path between formats (e.g. Windows to/from Universal).
- <xref:Singulink.IO.IRelativePath.ParentDirectory> (narrowed to <xref:Singulink.IO.IRelativeDirectoryPath>).

#### IFilePath

<xref:Singulink.IO.IFilePath> adds:

- <xref:Singulink.IO.IFilePath.NameWithoutExtension>, <xref:Singulink.IO.IFilePath.Extension>.
- <xref:Singulink.IO.IFilePath.WithExtension*>: replace the trailing extension.
- <xref:Singulink.IO.IFilePath.AddExtension*>: append an extension, preserving any existing one.

See [File Names and Extensions](file-names-and-extensions.md).

#### IDirectoryPath

<xref:Singulink.IO.IDirectoryPath> adds path-combination members and the `+` operator:

- <xref:Singulink.IO.IDirectoryPath.Combine*> with <xref:Singulink.IO.IRelativeDirectoryPath>, <xref:Singulink.IO.IRelativeFilePath> or <xref:Singulink.IO.IRelativePath>.
- <xref:Singulink.IO.IDirectoryPath.CombineDirectory*>, <xref:Singulink.IO.IDirectoryPath.CombineFile*>.

See [Combining and Navigating Paths](combining-and-navigating.md).

#### IAbsoluteDirectoryPath

<xref:Singulink.IO.IAbsoluteDirectoryPath> combines <xref:Singulink.IO.IAbsolutePath> and <xref:Singulink.IO.IDirectoryPath>. Adds:

- <xref:Singulink.IO.IAbsoluteDirectoryPath.IsRoot>, <xref:Singulink.IO.IAbsoluteDirectoryPath.IsEmpty>.
- <xref:Singulink.IO.IAbsoluteDirectoryPath.DriveType>, <xref:Singulink.IO.IAbsoluteDirectoryPath.FileSystem>, <xref:Singulink.IO.IAbsoluteDirectoryPath.AvailableFreeSpace>, <xref:Singulink.IO.IAbsoluteDirectoryPath.TotalFreeSpace>, <xref:Singulink.IO.IAbsoluteDirectoryPath.TotalSize>: see [Drive and Disk Information](drive-and-disk-info.md).
- <xref:Singulink.IO.IAbsoluteDirectoryPath.Create*>, <xref:Singulink.IO.IAbsoluteDirectoryPath.Delete*>.
- <xref:Singulink.IO.IAbsoluteDirectoryPath.GetInfo*> returning <xref:Singulink.IO.CachedDirectoryInfo>.
- A full set of enumeration methods: see [Searching and Enumeration](searching-and-enumeration.md).

#### IAbsoluteFilePath

<xref:Singulink.IO.IAbsoluteFilePath> combines <xref:Singulink.IO.IAbsolutePath> and <xref:Singulink.IO.IFilePath>. Adds:

- <xref:Singulink.IO.IAbsoluteFilePath.IsReadOnly>, <xref:Singulink.IO.IAbsoluteFilePath.Length>.
- <xref:Singulink.IO.IAbsoluteFilePath.OpenStream*>, <xref:Singulink.IO.IAbsoluteFilePath.OpenAsyncStream*>.
- <xref:Singulink.IO.IAbsoluteFilePath.CopyTo*>, <xref:Singulink.IO.IAbsoluteFilePath.MoveTo*>, <xref:Singulink.IO.IAbsoluteFilePath.Replace*>, <xref:Singulink.IO.IAbsoluteFilePath.Delete*>.
- <xref:Singulink.IO.IAbsoluteFilePath.GetInfo*> returning <xref:Singulink.IO.CachedFileInfo>.

See [Working with Files](file-operations.md).

#### IRelativeFilePath / IRelativeDirectoryPath

<xref:Singulink.IO.IRelativeFilePath> and <xref:Singulink.IO.IRelativeDirectoryPath> combine <xref:Singulink.IO.IRelativePath> with <xref:Singulink.IO.IFilePath> / <xref:Singulink.IO.IDirectoryPath>. Their members are the union of the two parents, narrowed to relative return types.

## Why Strong Typing

Static typing catches at compile time the bugs you'd otherwise hit at runtime. A few examples:

```csharp
void OpenLog(IAbsoluteFilePath path);

OpenLog(someDirectoryPath);          // compile error: directory is not a file
OpenLog(someRelativeFilePath);       // compile error: relative is not absolute
```

The library applies the same discipline internally: file system operations only exist on absolute paths, so a relative path can never accidentally be opened against the current working directory.

## Pattern Matching

When you don't know statically which kind of path you have (e.g. you used <xref:Singulink.IO.FilePath.Parse*> which returns <xref:Singulink.IO.IFilePath>), use pattern matching:

```csharp
IFilePath file = FilePath.Parse(userInput);

if (file is IAbsoluteFilePath absolute)
{
    using FileStream s = absolute.OpenStream();
    // ...
}
else
{
    IRelativeFilePath relative = (IRelativeFilePath)file;
    IAbsoluteFilePath resolved = DirectoryPath.GetCurrent() + relative;
    // ...
}
```

> [!TIP]
> If you know up front that a string must be absolute (or must be relative), call <xref:Singulink.IO.FilePath.ParseAbsolute*> or <xref:Singulink.IO.FilePath.ParseRelative*> directly. The return type is the specific interface, so no cast or pattern match is needed.

## Equality

Two paths are equal when:

- They implement the same concrete type (e.g. both are <xref:Singulink.IO.IAbsoluteFilePath>).
- Their <xref:Singulink.IO.IPath.PathFormat> is the same.
- Their root segments compare equal **case-insensitively** (drive letter or UNC name).
- The remainder of the path compares equal **case-sensitively**.

```csharp
var a = FilePath.ParseAbsolute(@"C:\Apps\MyApp\config.json");
var b = FilePath.ParseAbsolute(@"c:\Apps\MyApp\config.json");
var c = FilePath.ParseAbsolute(@"C:\apps\MyApp\config.json");

a == b;   // true : root casing differs, but the root is case-insensitive
a == c;   // false: non-root segments are case-sensitive
```

> [!NOTE]
> Equality is **textual**. Two different paths that resolve to the same physical entry through symbolic links or case-insensitive file systems are not equal; equality reflects the path, not what it points to.

## Next Steps

- [Parsing Paths](parsing-paths.md): turn strings into instances of these interfaces.
- [Path Formats](path-formats.md): understand the third dimension, <xref:Singulink.IO.PathFormat>, that affects every path.
- [Combining and Navigating Paths](combining-and-navigating.md): build new paths from existing ones.

</div>
