# Singulink.IO.FileSystem Version History

## Version 3.0

- **Breaking:** `CombineDirectory,CombineFile` overloads that previously took a `PathFormat` now take a new `RelativePathFormat` enum (`MatchBase` / `Universal`), representing the only valid options for an appended relative path. Callers that passed `PathFormat.Universal` should pass `RelativePathFormat.Universal`; callers that passed the base path's `PathFormat` should pass `RelativePathFormat.MatchBase` (or omit the argument)
- **Breaking:** `IAbsolutePath.PathExport` and `IPath.PathDisplay` now formalize new path invariants: non-empty directory paths always end with the separator and file paths never do (previously, both normalized to paths without any trailing separators)
- New `CachedEntryInfo.Create(path, options)` factory, plus typed `Create` shadows on `CachedFileInfo` / `CachedDirectoryInfo` that throw `IOException` on a type mismatch
- New `IAbsoluteDirectoryPath.GetInfo()` returning a `CachedDirectoryInfo` for the directory itself, plus `GetInfo(relativePath, ...)` overloads that combine and resolve an entry under the directory in a single call
- New `MoveTo(IAbsoluteDirectoryPath)` method on `IAbsoluteDirectoryPath`
- New `ToCachedInfo()` extensions on `FileInfo`, `DirectoryInfo` and `FileSystemInfo`, plus a `ToPath(this FileSystemInfo)` overload that returns the correct concrete `IAbsolutePath` type for the runtime type of the supplied info
- Various internal cleanups, perf improvements and a substantially expanded test suite.

## Version 2.0

- **Breaking:** Drop .NET Standard 2.1 - library now targets .NET 8.0+
- **Breaking:** `IsAbsolute` / `IsRelative` / `IsDirectory` / `IsFile` helpers removed in favor of direct type tests
- Restructured solution and cleaned up / modernized code significantly
- Added dependency on `Singulink.Enums` for enum handling
- Numerous internal cleanups and consistency improvements across the `IPath` hierarchy
