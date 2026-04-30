<div class="article">

# Special Locations

### Overview

Most applications need to resolve a handful of well-known paths: the application base, the current working directory, the temp folder, OS-defined special folders (Documents, AppData, etc.) and the location of a loaded assembly. The library exposes these as static helpers on <xref:Singulink.IO.DirectoryPath> and <xref:Singulink.IO.FilePath> that return strongly-typed, ready-to-use absolute paths.

> [!NOTE]
> Every special-location helper parses the underlying OS string with <xref:Singulink.IO.PathOptions.None> so that whatever the operating system returns is accepted verbatim, even if it contains characters or names that would be rejected with <xref:Singulink.IO.PathOptions.NoUnfriendlyNames>.

## Application Base

<xref:Singulink.IO.DirectoryPath.GetAppBase*> returns the directory the runtime probes for assemblies. For most apps this is where the executable lives:

```csharp
IAbsoluteDirectoryPath baseDir = DirectoryPath.GetAppBase();
IAbsoluteFilePath bundledData = baseDir.CombineFile("Resources/defaults.json");
```

> [!TIP]
> Prefer <xref:Singulink.IO.DirectoryPath.GetAppBase*> over <xref:Singulink.IO.DirectoryPath.GetCurrent*> for resolving files that ship with your application. The current working directory can change at runtime; the app base does not.

## Current Working Directory

Use <xref:Singulink.IO.DirectoryPath.GetCurrent*> and <xref:Singulink.IO.DirectoryPath.SetCurrent*>:

```csharp
IAbsoluteDirectoryPath cwd = DirectoryPath.GetCurrent();
DirectoryPath.SetCurrent(cwd.ParentDirectory!);
```

<xref:Singulink.IO.DirectoryPath.SetCurrent*> requires a path whose <xref:Singulink.IO.IPath.PathFormat> matches <xref:Singulink.IO.PathFormat.Current>.

## Temporary Files and Directories

#### GetTemp

<xref:Singulink.IO.DirectoryPath.GetTemp*> returns the user's temporary directory (the equivalent of <xref:System.IO.Path.GetTempPath*>):

```csharp
IAbsoluteDirectoryPath tempDir = DirectoryPath.GetTemp();
```

#### CreateTempFile

<xref:Singulink.IO.FilePath.CreateTempFile*> creates a uniquely named, zero-byte temporary file and returns its path. The file already exists when this returns:

```csharp
IAbsoluteFilePath workFile = FilePath.CreateTempFile();

try
{
    using FileStream s = workFile.OpenStream(FileMode.Truncate, FileAccess.Write);
    s.Write(payload);
}
finally
{
    workFile.Delete();
}
```

> [!IMPORTANT]
> <xref:Singulink.IO.FilePath.CreateTempFile*> actually creates the file; there's no race window. Always make sure something deletes it later, even on the failure path.

## OS Special Folders

<xref:Singulink.IO.DirectoryPath.GetSpecialFolder*> resolves any of the <xref:System.Environment.SpecialFolder> values:

```csharp
IAbsoluteDirectoryPath appData = DirectoryPath.GetSpecialFolder(Environment.SpecialFolder.ApplicationData);
IAbsoluteDirectoryPath docs    = DirectoryPath.GetSpecialFolder(Environment.SpecialFolder.MyDocuments);

IAbsoluteDirectoryPath profileDir = appData.CombineDirectory("MyApp");
profileDir.Create();
```

Some special folders may not be defined on every platform; in that case the underlying API returns an empty string and the helper throws.

## Assembly Locations

Resolve the file path or directory of a loaded <xref:System.Reflection.Assembly> with <xref:Singulink.IO.FilePath.GetAssemblyLocation*> or <xref:Singulink.IO.DirectoryPath.GetAssemblyLocation*>:

```csharp
IAbsoluteFilePath thisDll = FilePath.GetAssemblyLocation(typeof(MyType).Assembly);
IAbsoluteDirectoryPath thisDir = DirectoryPath.GetAssemblyLocation(typeof(MyType).Assembly);
```

> [!CAUTION]
> Assembly location is unavailable when an app is published as a single file. The helpers throw <xref:System.InvalidOperationException> in that case; use <xref:Singulink.IO.DirectoryPath.GetAppBase*> instead for resources that ship with your app.

## Mounting Points

<xref:Singulink.IO.DirectoryPath.GetMountingPoints*> returns the file system roots (drives on Windows, mount points on Unix):

```csharp
foreach (IAbsoluteDirectoryPath mount in DirectoryPath.GetMountingPoints())
{
    Console.WriteLine($"{mount.PathDisplay}  {mount.DriveType}  {mount.AvailableFreeSpace:N0} bytes free");
}
```

See [Drive and Disk Information](drive-and-disk-info.md) for the full set of disk-space members available on every absolute directory.

## Next Steps

- [Working with Files](file-operations.md) and [Working with Directories](directory-operations.md): what to do once you have a path.
- [Drive and Disk Information](drive-and-disk-info.md): query disk space and drive type from any absolute directory.

</div>
