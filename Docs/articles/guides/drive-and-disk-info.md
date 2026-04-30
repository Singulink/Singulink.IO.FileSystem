<div class="article">

# Drive and Disk Information

### Overview

The library does not have a "drive" concept. Drives are a Windows-centric idea, and they get the wrong answer in many real-world scenarios: UNC paths, Unix mount points, per-user quotas, drives mounted into a subdirectory. Disk-space and file-system metadata is exposed instead on every <xref:Singulink.IO.IAbsoluteDirectoryPath>, where it can answer the question for the actual location you care about.

## What's Available

Every absolute directory path exposes:

| Member | Description |
|--------|-------------|
| <xref:Singulink.IO.IAbsoluteDirectoryPath.AvailableFreeSpace> | Bytes available to the current user, taking quotas into account. |
| <xref:Singulink.IO.IAbsoluteDirectoryPath.TotalFreeSpace> | Bytes free on the volume, ignoring per-user quotas. |
| <xref:Singulink.IO.IAbsoluteDirectoryPath.TotalSize> | Total volume size in bytes. |
| <xref:Singulink.IO.IAbsoluteDirectoryPath.DriveType> | <xref:System.IO.DriveType.Fixed>, <xref:System.IO.DriveType.Removable>, <xref:System.IO.DriveType.Network>, <xref:System.IO.DriveType.CDRom>, etc. |
| <xref:Singulink.IO.IAbsoluteDirectoryPath.FileSystem> | The file system name (e.g. `"NTFS"`, `"ext4"`). |
| <xref:Singulink.IO.IAbsolutePath.IsUnc> | `true` if the path is a UNC path (Windows only). |

```csharp
IAbsoluteDirectoryPath installDir = appBase.CombineDirectory("data");

Console.WriteLine($"File system : {installDir.FileSystem}");
Console.WriteLine($"Drive type  : {installDir.DriveType}");
Console.WriteLine($"Free space  : {installDir.AvailableFreeSpace:N0} bytes");
Console.WriteLine($"Total size  : {installDir.TotalSize:N0} bytes");
```

> [!NOTE]
> These properties touch the file system on each access. If you'll read more than one in a tight loop, capture them into local variables.

## Pre-Flight Free-Space Check

A common installer / writer pattern: before writing data, check that the eventual location will have enough room. The trick is that the eventual location may not exist yet. Walk up to the nearest existing directory and ask there:

```csharp
IAbsoluteDirectoryPath target = userInstallPath;
IAbsoluteDirectoryPath checkAt = target.GetLastExistingDirectory();

if (checkAt.AvailableFreeSpace < requiredBytes)
    throw new IOException($"Not enough free space at {checkAt.PathDisplay}.");

target.Create();
```

<xref:Singulink.IO.IAbsolutePath.GetLastExistingDirectory*> walks up the path until it finds a directory that exists, which is always somewhere on the volume the new path would land on.

## Mounting Points

<xref:Singulink.IO.DirectoryPath.GetMountingPoints*> returns the file system roots (drives on Windows, mount points on Unix):

```csharp
foreach (IAbsoluteDirectoryPath mount in DirectoryPath.GetMountingPoints())
{
    if (mount.DriveType is DriveType.Fixed)
    {
        Console.WriteLine(
            $"{mount.PathDisplay,-6} {mount.FileSystem,-6} " +
            $"{mount.AvailableFreeSpace,15:N0} / {mount.TotalSize,15:N0}");
    }
}
```

This is the cross-platform equivalent of <xref:System.IO.DriveInfo.GetDrives*>, except every entry is a fully usable <xref:Singulink.IO.IAbsoluteDirectoryPath>. You can immediately combine it with relative paths, enumerate it, query free space, and so on.

## UNC Paths

UNC paths (`\\server\share\...`) are first-class citizens. All disk-space and file-system properties work on UNC paths the same way they do on local paths:

```csharp
var share = DirectoryPath.ParseAbsolute(@"\\fileserver\projects\my-app");
share.AvailableFreeSpace;   // works
share.IsUnc;                // true
```

> [!IMPORTANT]
> <xref:System.IO.DriveInfo> cannot answer questions about UNC paths. If you previously special-cased UNC handling because of <xref:System.IO.DriveInfo> limitations, those workarounds can be removed when migrating to this library.

## Drive Type and File System

<xref:Singulink.IO.IAbsoluteDirectoryPath.DriveType> and <xref:Singulink.IO.IAbsoluteDirectoryPath.FileSystem> are useful for selecting a behavior based on the underlying volume:

```csharp
if (target.DriveType is DriveType.Network)
    options |= FileOptions.WriteThrough;   // skip OS write cache for network shares
```

```csharp
if (target.FileSystem.Equals("FAT32", StringComparison.OrdinalIgnoreCase) && payload.Length > 4L * 1024 * 1024 * 1024)
    throw new IOException("Single file would exceed FAT32 4 GB limit.");
```

## Next Steps

- [Special Locations](special-locations.md): <xref:Singulink.IO.DirectoryPath.GetMountingPoints*> is one of several "where am I" helpers.
- [Working with Directories](directory-operations.md): once you've picked a location.

</div>
