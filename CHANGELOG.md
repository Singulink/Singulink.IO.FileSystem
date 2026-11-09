# Singulink.IO.FileSystem Version History

## Version 3.2.1

- Fixed: on Linux and macOS, `GetChildEntriesInfo`, `GetChildFilesInfo` and `GetChildDirectoriesInfo` threw when an entry was deleted during the enumeration; such entries are now left out

## Version 3.2

Paths now work with configuration binding, `System.Text.Json` and DataAnnotations validation, and file paths gain methods for extensions such as `.tar.gz`. There are no breaking changes.

### Fixes

- Combining a relative directory with a rooted relative path (e.g. `a\` with `\b` on Windows) gave a path that wasn't equal to the same path parsed directly
- `WithExtension` could give a file name of `.` or `..`; it now throws `ArgumentException` for those names

### New

- Type converters for all path types, so the configuration binder (`Get<T>()`, `Bind()`, `Configure<T>(section)`; not the configuration binding source generator) can create paths from configuration values; see the [Type Converters, JSON and Configuration Binding](https://www.singulink.com/Docs/Singulink.IO.FileSystem/articles/guides/type-converters.html) guide
- `System.Text.Json` converters for all path types (in the `Singulink.IO.Json` namespace), attached to the types so paths serialize as strings
- `IPath.IsValidFor(options)` methods that check whether an existing path is valid for a set of path options
- `ValidPathAttribute` to validate path members with DataAnnotations, e.g. `[ValidPath(PathOptions.NoUnfriendlyNames)]`
- Methods to check for and remove a file extension: `HasExtension`, `RemoveExtension`
- Methods to handle multi-extensions such as `.tar.gz`: `HasMultiExtension`, `GetMultiExtension`, `AddMultiExtension`, `RemoveMultiExtension` and `PathFormat.IsValidMultiExtension`

## Version 3.1.1

- Fixed: `IAbsoluteDirectoryPath.IsEmpty` returned the opposite value

## Version 3.1

- New `TryParse`, `TryParseAbsolute` and `TryParseRelative` methods on `FilePath` and `DirectoryPath` that return `false` instead of throwing when the path is invalid, with the same format / options overloads as the `Parse` methods (options default to `PathOptions.NoUnfriendlyNames` when omitted) - thanks to [@volkmnv](https://github.com/volkmnv) for the contribution ([#6](https://github.com/Singulink/Singulink.IO.FileSystem/pull/6))

## Version 3.0

The breaking changes in this release should have zero to minimal impact for the vast majority of expected usage patterns. Code that relies on the strongly-typed path API and uses separator-aware helpers (such as `Path.Combine`) when it does drop down to strings needs no changes. Consumers that were directly manipulating string paths obtained from `PathDisplay` / `PathExport` should audit those sites to ensure the new trailing separator on directory paths doesn't break anything downstream.

On the upside, projects referencing v3+ can rely on the new codified invariants: an absolute directory string can be concatenated with any number of relative directory strings and an optional trailing relative file path string to produce a valid path with no separator handling required. `absDir + relDir + relDir + relFile` (as `PathDisplay` / `PathExport` strings) always "just works" now.

- **Breaking:** `IAbsolutePath.PathExport` and `IPath.PathDisplay` now formalize new path invariants: non-empty directory paths always end with the separator and file paths never do (previously, both normalized to paths without any trailing separators, requiring explicit separator handling to combine them)
- New `CachedEntryInfo.Create(path, options)` factory, plus typed `Create` shadows on `CachedFileInfo` / `CachedDirectoryInfo` that throw `IOException` on a type mismatch
- New `IAbsoluteDirectoryPath.GetInfo(relativePath, ...)` overloads that combine and resolve an entry under the directory in a single call
- New `MoveTo(IAbsoluteDirectoryPath)` method on `IAbsoluteDirectoryPath`
- New `ToCachedInfo()` extensions on `FileInfo`, `DirectoryInfo` and `FileSystemInfo`, plus a `ToPath(this FileSystemInfo)` overload that returns the correct concrete `IAbsolutePath` type for the runtime type of the supplied info
- Various internal cleanups, perf improvements and a substantially expanded test suite.

## Version 2.0

- **Breaking:** Drop .NET Standard 2.1 - library now targets .NET 8.0+
- **Breaking:** `IsAbsolute` / `IsRelative` / `IsDirectory` / `IsFile` helpers removed in favor of direct type tests
- Restructured solution and cleaned up / modernized code significantly
- Added dependency on `Singulink.Enums` for enum handling
- Numerous internal cleanups and consistency improvements across the `IPath` hierarchy
