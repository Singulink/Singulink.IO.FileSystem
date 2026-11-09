<div class="article">

# File Names and Extensions

Every path has a <xref:Singulink.IO.IPath.Name?displayProperty=nameWithType>: the final segment of the path. File paths additionally split that name into a base portion and an extension. The library exposes helpers to read those parts and to derive new file paths with a different extension, while always validating the result against the path's <xref:Singulink.IO.PathFormat> and <xref:Singulink.IO.PathOptions>.

## Reading the Name

<xref:Singulink.IO.IPath.Name?displayProperty=nameWithType> returns the final segment for any path:

```csharp
FilePath.ParseAbsolute(@"C:\Apps\MyApp\data.json").Name;   // "data.json"
DirectoryPath.ParseAbsolute(@"C:\Apps\MyApp").Name;        // "MyApp"
```

A directory's name does not include the trailing separator that its <xref:Singulink.IO.IPath.PathDisplay?displayProperty=nameWithType> ends with. For roots, <xref:Singulink.IO.IPath.Name> returns the root segment (e.g. `"C:"` for a Windows drive root or `"/"` on Unix).

## Single Extensions and Multi-Extensions

A file name doesn't say where its extension starts: `backup.tar.gz` has the extension `.tar.gz`, but `my.report.v2.pdf` has the extension `.pdf`. The file path members come in two families for that reason, and the member names say which family a member belongs to.

**Single extension** (members named `…Extension`): <xref:Singulink.IO.IFilePath.Extension>, <xref:Singulink.IO.IFilePath.NameWithoutExtension>, <xref:Singulink.IO.IFilePath.HasExtension*>, <xref:Singulink.IO.IFilePath.WithExtension*>, <xref:Singulink.IO.IFilePath.AddExtension*> and <xref:Singulink.IO.IFilePath.RemoveExtension*> work with the last extension, the part of the name from the last dot. The four that take an extension (`HasExtension`, `WithExtension`, `AddExtension` and `RemoveExtension`) take one with a single part (`.gz`, not `.tar.gz`). A successful call keeps <xref:Singulink.IO.IFilePath.Extension> accurate: for any non-empty extension `x` they accept, `path.WithExtension(x).Extension` and `path.AddExtension(x).Extension` are `x`, `path.HasExtension(x)` is `true` exactly when `path.Extension` equals `x` ignoring case, and `path.RemoveExtension(x)` succeeds only when `path.HasExtension(x)` is `true`.

**Multi-extension** (members named `…MultiExtension`): <xref:Singulink.IO.IFilePath.HasMultiExtension*>, <xref:Singulink.IO.IFilePath.GetMultiExtension*>, <xref:Singulink.IO.IFilePath.AddMultiExtension*> and <xref:Singulink.IO.IFilePath.RemoveMultiExtension*> work with a multi-extension, which has one or more parts (`.gz` or `.tar.gz`). For any non-empty extension `x` they accept, `path.AddMultiExtension(x)` gives a path whose `HasMultiExtension(x)` is `true` and whose `GetMultiExtension(x)` returns `x`, and `RemoveMultiExtension(x)` on that path gives back `path`. With an extension of one part, they give the same results as the single extension members.

<xref:Singulink.IO.IFilePath.HasExtension*>, <xref:Singulink.IO.IFilePath.RemoveExtension*>, <xref:Singulink.IO.IFilePath.HasMultiExtension*>, <xref:Singulink.IO.IFilePath.GetMultiExtension*> and <xref:Singulink.IO.IFilePath.RemoveMultiExtension*> ignore case on every platform, so `photo.JPG` has the extension `.jpg` on Unix too. The [extension rules](#extension-rules) say which extensions each family takes.

## File Name Without Extension

<xref:Singulink.IO.IFilePath.NameWithoutExtension?displayProperty=nameWithType> returns the file name with the trailing extension and its dot removed:

```csharp
FilePath.ParseRelative("report.pdf").NameWithoutExtension;        // "report"
FilePath.ParseRelative("archive.tar.gz").NameWithoutExtension;    // "archive.tar"
FilePath.ParseRelative("Makefile").NameWithoutExtension;          // "Makefile"
```

Only the **last** dot delimits the extension. A file name like `archive.tar.gz` has extension `.gz` and base name `archive.tar`.

## Extension

<xref:Singulink.IO.IFilePath.Extension?displayProperty=nameWithType> returns the extension **including** the leading dot, or an empty string if the file has none:

```csharp
FilePath.ParseRelative("report.pdf").Extension;        // ".pdf"
FilePath.ParseRelative("archive.tar.gz").Extension;    // ".gz"
FilePath.ParseRelative("Makefile").Extension;          // ""
FilePath.ParseRelative(".gitignore").Extension;        // ".gitignore"
```

> [!NOTE]
> The last-dot rule has two edge cases. A name whose only dot is the leading one, such as `.gitignore`, is all extension: <xref:Singulink.IO.IFilePath.Extension?displayProperty=nameWithType> returns the whole name and <xref:Singulink.IO.IFilePath.NameWithoutExtension?displayProperty=nameWithType> returns an empty string. A file name that ends with a dot (only possible when parsing with <xref:Singulink.IO.PathOptions.None?displayProperty=nameWithType>) has extension `"."`; the trailing dot is preserved verbatim and treated as the extension.

## HasExtension: Check

<xref:Singulink.IO.IFilePath.HasExtension*?displayProperty=nameWithType> tells whether the last extension is the given single extension, ignoring case:

```csharp
var archive = FilePath.ParseRelative("backups/archive.TAR.GZ");

archive.HasExtension(".gz");       // true
archive.HasExtension(".tar");      // false: not the last extension
archive.HasExtension(".tar.gz");   // ArgumentException: more than one part (see HasMultiExtension)
archive.HasExtension("");          // ArgumentException: the extension is empty
```

A name that consists of the extension has it: `.gitignore` has the extension `.gitignore`, as <xref:Singulink.IO.IFilePath.Extension> also gives. <xref:Singulink.IO.IFilePath.Extension> is an empty string for a name without an extension.

## WithExtension: Replace

<xref:Singulink.IO.IFilePath.WithExtension*?displayProperty=nameWithType> returns a new path with the last extension replaced. The new extension must be empty/null or start with a single `.` and contain no further dots.

```csharp
var report = FilePath.ParseAbsolute(@"C:\out\report.pdf");

report.WithExtension(".html");   // C:\out\report.html
report.WithExtension(null);      // C:\out\report     (extension removed)
report.WithExtension(".tar.gz"); // ArgumentException: more than one part
```

For multi-extension files, only the last extension is replaced:

```csharp
var archive = FilePath.ParseRelative("backups/archive.tar.gz");
archive.WithExtension(".bz2");   // backups/archive.tar.bz2
archive.WithExtension(null);     // backups/archive.tar
```

## AddExtension: Append

<xref:Singulink.IO.IFilePath.AddExtension*?displayProperty=nameWithType> appends a single extension without removing any existing one:

```csharp
var src = FilePath.ParseRelative("backups/archive.tar");
src.AddExtension(".gz");        // backups/archive.tar.gz
src.AddExtension(".tar.gz");    // ArgumentException: more than one part (see AddMultiExtension)
```

A null or empty extension leaves the path unchanged. The extension becomes the last extension, so `path.AddExtension(x).Extension` is `x` for any non-empty extension it accepts.

## RemoveExtension: Strip

<xref:Singulink.IO.IFilePath.RemoveExtension*?displayProperty=nameWithType> returns a new path with a single extension removed from the end of the file name, ignoring case:

```csharp
var archive = FilePath.ParseAbsolute(@"C:\out\archive.TAR.GZ");

archive.RemoveExtension(".gz");       // C:\out\archive.TAR
archive.RemoveExtension(".zip");      // ArgumentException: the name doesn't end with the extension
archive.RemoveExtension(".tar.gz");   // ArgumentException: more than one part (see RemoveMultiExtension)
archive.RemoveExtension("");          // C:\out\archive.TAR.GZ (unchanged)
```

It throws <xref:System.ArgumentException> if the name doesn't end with the extension, if the extension is the whole name (a file named `.gitignore`), or if the remaining name is invalid for the supplied <xref:Singulink.IO.PathOptions> (see [Validation](#validation)).

## HasMultiExtension: Check

<xref:Singulink.IO.IFilePath.HasMultiExtension*?displayProperty=nameWithType> tells whether the file name ends with a multi-extension, ignoring case:

```csharp
var archive = FilePath.ParseRelative("backups/archive.TAR.GZ");

archive.HasMultiExtension(".tar.gz");   // true
archive.HasMultiExtension(".gz");       // true
archive.HasMultiExtension(".tar");      // false
archive.HasMultiExtension("");          // ArgumentException: the extension is empty
```

A name that consists of the extension ends with it: `.gitignore` has the multi-extension `.gitignore`, and `.tar.gz` has the multi-extension `.tar.gz`.

## GetMultiExtension: Match

<xref:Singulink.IO.IFilePath.GetMultiExtension*?displayProperty=nameWithType> gives the longest of the known multi-extensions that the file name ends with, as it was passed in, otherwise <xref:Singulink.IO.IFilePath.Extension>:

```csharp
FilePath.ParseRelative("backup.tar.gz").GetMultiExtension(".tar.gz", ".tar.bz2");    // ".tar.gz"
FilePath.ParseRelative("BACKUP.TAR.GZ").GetMultiExtension(".tar.gz", ".tar.bz2");    // ".tar.gz"
FilePath.ParseRelative("photo.jpg").GetMultiExtension(".tar.gz", ".tar.bz2");        // ".jpg"
FilePath.ParseRelative("Makefile").GetMultiExtension(".tar.gz", ".tar.bz2");         // ""
FilePath.ParseRelative("backup.tar.gz").GetMultiExtension(".gz", ".tar.gz");         // ".tar.gz" (the longest match)
```

Its result can be passed to <xref:Singulink.IO.IFilePath.RemoveMultiExtension*>, which leaves a name without an extension unchanged:

```csharp
var file = FilePath.ParseRelative("backups/archive.tar.gz");
file.RemoveMultiExtension(file.GetMultiExtension(".tar.gz", ".tar.bz2"));   // backups/archive
```

For a name that is all extension, such as `.gitignore`, <xref:Singulink.IO.IFilePath.RemoveMultiExtension*> throws, because no name would remain.

## AddMultiExtension: Append

<xref:Singulink.IO.IFilePath.AddMultiExtension*?displayProperty=nameWithType> appends a multi-extension without removing any existing one:

```csharp
var data = FilePath.ParseRelative("backups/archive");
data.AddMultiExtension(".tar.gz");   // backups/archive.tar.gz
data.AddMultiExtension(".gz");       // backups/archive.gz
```

A null or empty extension leaves the path unchanged. The new name ends with the extension, so `path.AddMultiExtension(x).HasMultiExtension(x)` is `true` for any non-empty extension it accepts.

## RemoveMultiExtension: Strip

<xref:Singulink.IO.IFilePath.RemoveMultiExtension*?displayProperty=nameWithType> returns a new path with a multi-extension removed from the end of the file name, ignoring case:

```csharp
var archive = FilePath.ParseAbsolute(@"C:\out\archive.TAR.GZ");

archive.RemoveMultiExtension(".tar.gz");   // C:\out\archive
archive.RemoveMultiExtension(".gz");       // C:\out\archive.TAR
archive.RemoveMultiExtension(".zip");      // ArgumentException: the name doesn't end with the extension
archive.RemoveMultiExtension("");          // C:\out\archive.TAR.GZ (unchanged)
```

It throws <xref:System.ArgumentException> if the name doesn't end with the extension, if the extension is the whole name (a file named `.tar.gz`), or if the remaining name is invalid for the supplied <xref:Singulink.IO.PathOptions> (see [Validation](#validation)).

## Extension Rules

A **single extension** starts with a dot and has no other dots: `.gz` is accepted, `gz`, `.tar.gz` and `..` are not. <xref:Singulink.IO.IFilePath.HasExtension*>, <xref:Singulink.IO.IFilePath.WithExtension*>, <xref:Singulink.IO.IFilePath.AddExtension*> and <xref:Singulink.IO.IFilePath.RemoveExtension*> take single extensions and throw <xref:System.ArgumentException> for any other shape, including a multi-extension with several parts.

A **multi-extension** starts with a dot, and every dot is followed by a character other than a dot: `.gz` and `.tar.gz` are accepted, `gz`, `..gz`, `.tar..gz` and `.tar.` are not. <xref:Singulink.IO.IFilePath.HasMultiExtension*>, <xref:Singulink.IO.IFilePath.GetMultiExtension*>, <xref:Singulink.IO.IFilePath.AddMultiExtension*> and <xref:Singulink.IO.IFilePath.RemoveMultiExtension*> take multi-extensions and throw <xref:System.ArgumentException> for any other shape.

A lone dot `.` is accepted by both families, since that is the <xref:Singulink.IO.IFilePath.Extension> of a name that ends with a dot. Characters that names can't have in the path format (such as `?` on Windows) are rejected. The extensions each family takes are the ones for which `format.IsValidExtension(extension, PathOptions.None)` (single) or `format.IsValidMultiExtension(extension, PathOptions.None)` (multi) returns `true`; the members that make a new name also check it with the options they are given (see [Validation](#validation)).

An empty extension:

- leaves the path unchanged in <xref:Singulink.IO.IFilePath.AddExtension*>, <xref:Singulink.IO.IFilePath.AddMultiExtension*>, <xref:Singulink.IO.IFilePath.RemoveExtension*> and <xref:Singulink.IO.IFilePath.RemoveMultiExtension*>;
- removes the last extension in <xref:Singulink.IO.IFilePath.WithExtension*>;
- throws in <xref:Singulink.IO.IFilePath.HasExtension*>, <xref:Singulink.IO.IFilePath.HasMultiExtension*> and <xref:Singulink.IO.IFilePath.GetMultiExtension*>.

## Validation

<xref:Singulink.IO.IFilePath.WithExtension*>, <xref:Singulink.IO.IFilePath.AddExtension*>, <xref:Singulink.IO.IFilePath.AddMultiExtension*>, <xref:Singulink.IO.IFilePath.RemoveExtension*> and <xref:Singulink.IO.IFilePath.RemoveMultiExtension*> validate the **new file name** against the supplied <xref:Singulink.IO.PathOptions> (default <xref:Singulink.IO.PathOptions.NoUnfriendlyNames?displayProperty=nameWithType>). The rest of the path is not re-parsed.

```csharp
var path = FilePath.ParseRelative("data");

path.WithExtension(".txt ");                     // throws: new name "data.txt " ends with a space
path.WithExtension(".");                         // throws: new name "data." ends with a dot
path.WithExtension(".txt ", PathOptions.None);   // "data.txt " (validation relaxed)
```

The new name also can't be `.` or `..`, which would make the path a directory path.

<xref:Singulink.IO.PathFormat.IsValidExtension*?displayProperty=nameWithType> checks a single extension, the kind the single extension members take:

```csharp
PathFormat.Universal.IsValidExtension(".tar.gz");   // false: multiple dots
PathFormat.Universal.IsValidExtension("gz");        // false: no leading dot
PathFormat.Universal.IsValidExtension(".gz");       // true
PathFormat.Universal.IsValidExtension("");          // true: empty is valid
```

<xref:Singulink.IO.PathFormat.IsValidMultiExtension*?displayProperty=nameWithType> checks a multi-extension, checking each part as <xref:Singulink.IO.PathFormat.IsValidExtension*> checks a single extension with the given options (<xref:Singulink.IO.PathOptions.NoUnfriendlyNames?displayProperty=nameWithType> when none are given):

```csharp
PathFormat.Universal.IsValidMultiExtension(".tar.gz");                     // true
PathFormat.Universal.IsValidMultiExtension(".tar .gz");                    // false: ".tar " ends with a space
PathFormat.Universal.IsValidMultiExtension(".tar .gz", PathOptions.None);  // true
PathFormat.Universal.IsValidMultiExtension(".tar..gz");                    // false: empty part
```

## Renaming

The library does not expose a "rename" method on file paths directly. Renaming is just combining the parent directory with a new file name:

```csharp
var oldPath = FilePath.ParseAbsolute(@"C:\out\report.pdf");
var newPath = oldPath.ParentDirectory.CombineFile("report-final.pdf");

oldPath.MoveTo(newPath);
```

This pattern composes nicely with extension changes:

```csharp
var temp = FilePath.ParseAbsolute(@"C:\out\report.pdf.tmp");
var final = temp.WithExtension(null);   // strip ".tmp"
temp.MoveTo(final);
```

## Return Types

<xref:Singulink.IO.IFilePath.WithExtension*>, <xref:Singulink.IO.IFilePath.AddExtension*>, <xref:Singulink.IO.IFilePath.AddMultiExtension*>, <xref:Singulink.IO.IFilePath.RemoveExtension*> and <xref:Singulink.IO.IFilePath.RemoveMultiExtension*> preserve the static type of the path: calling them on <xref:Singulink.IO.IAbsoluteFilePath> returns <xref:Singulink.IO.IAbsoluteFilePath>, and on <xref:Singulink.IO.IRelativeFilePath> returns <xref:Singulink.IO.IRelativeFilePath>. No casts required.

## Next Steps

These guides pick up where renaming leaves off:

- [Combining and Navigating Paths](combining-and-navigating.md): combine works hand-in-hand with renaming.
- [Working with Files](file-operations.md): apply renames with <xref:Singulink.IO.IAbsoluteFilePath.MoveTo*?displayProperty=nameWithType>.

</div>
