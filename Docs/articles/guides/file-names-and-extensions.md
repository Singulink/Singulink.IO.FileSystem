<div class="article">

# File Names and Extensions

### Overview

Every path has a <xref:Singulink.IO.IPath.Name>: the final segment of the path. File paths additionally split that name into a base portion and an extension. The library exposes helpers to read those parts and to derive new file paths with a different extension, while always validating the result against the path's <xref:Singulink.IO.PathFormat> and <xref:Singulink.IO.PathOptions>.

## Reading the Name

<xref:Singulink.IO.IPath.Name> returns the final segment for any path:

```csharp
FilePath.ParseAbsolute(@"C:\Apps\MyApp\data.json").Name;   // "data.json"
DirectoryPath.ParseAbsolute(@"C:\Apps\MyApp").Name;        // "MyApp"
```

For roots, <xref:Singulink.IO.IPath.Name> returns the root segment (e.g. `"C:\"` on Windows).

## File Name Without Extension

<xref:Singulink.IO.IFilePath.NameWithoutExtension> returns the file name with the trailing extension and its dot removed:

```csharp
FilePath.ParseRelative("report.pdf").NameWithoutExtension;        // "report"
FilePath.ParseRelative("archive.tar.gz").NameWithoutExtension;    // "archive.tar"
FilePath.ParseRelative("Makefile").NameWithoutExtension;          // "Makefile"
```

Only the **last** dot delimits the extension. A file name like `archive.tar.gz` has extension `.gz` and base name `archive.tar`.

## Extension

<xref:Singulink.IO.IFilePath.Extension> returns the extension **including** the leading dot, or an empty string if the file has none:

```csharp
FilePath.ParseRelative("report.pdf").Extension;        // ".pdf"
FilePath.ParseRelative("archive.tar.gz").Extension;    // ".gz"
FilePath.ParseRelative("Makefile").Extension;          // ""
```

> [!NOTE]
> A file name that ends with a dot (only possible when parsing with <xref:Singulink.IO.PathOptions.None>) has extension `"."`; the trailing dot is preserved verbatim and treated as the extension.

## WithExtension: Replace

<xref:Singulink.IO.IFilePath.WithExtension*> returns a new path with the last extension replaced. The new extension must be empty/null or start with a single `.` and contain no further dots.

```csharp
var report = FilePath.ParseAbsolute(@"C:\out\report.pdf");

report.WithExtension(".html");   // C:\out\report.html
report.WithExtension(null);      // C:\out\report     (extension removed)
report.WithExtension(".tar.gz"); // ArgumentException: extension contains multiple dots
```

For multi-extension files, only the last extension is replaced:

```csharp
var archive = FilePath.ParseRelative("backups/archive.tar.gz");
archive.WithExtension(".bz2");   // backups/archive.tar.bz2
archive.WithExtension(null);     // backups/archive.tar
```

## AddExtension: Append

<xref:Singulink.IO.IFilePath.AddExtension*> appends an extension without removing any existing one:

```csharp
var src = FilePath.ParseRelative("backups/archive.tar");
src.AddExtension(".gz");   // backups/archive.tar.gz
```

Same input rules as <xref:Singulink.IO.IFilePath.WithExtension*> (single leading dot, no embedded dots).

## Validation

Both <xref:Singulink.IO.IFilePath.WithExtension*> and <xref:Singulink.IO.IFilePath.AddExtension*> validate the **new file name** against the supplied <xref:Singulink.IO.PathOptions> (default <xref:Singulink.IO.PathOptions.NoUnfriendlyNames>). The rest of the path is not re-parsed.

```csharp
var path = FilePath.ParseRelative("data");
path.WithExtension(".con");   // throws if path is in Windows format and NoReservedDeviceNames is set
                              // (combined name "data.con" is not reserved, so this case is fine)

path.WithExtension(" .txt");  // throws: leading space in entry name
```

<xref:Singulink.IO.PathFormat.IsValidExtension*> lets you check up front:

```csharp
PathFormat.Universal.IsValidExtension(".tar.gz");   // false: multiple dots
PathFormat.Universal.IsValidExtension(".gz");       // true
PathFormat.Universal.IsValidExtension("");          // true: empty is valid
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

<xref:Singulink.IO.IFilePath.WithExtension*> and <xref:Singulink.IO.IFilePath.AddExtension*> preserve the static type of the path: calling them on <xref:Singulink.IO.IAbsoluteFilePath> returns <xref:Singulink.IO.IAbsoluteFilePath>, and on <xref:Singulink.IO.IRelativeFilePath> returns <xref:Singulink.IO.IRelativeFilePath>. No casts required.

## Next Steps

- [Combining and Navigating Paths](combining-and-navigating.md): combine works hand-in-hand with renaming.
- [Working with Files](file-operations.md): apply renames with <xref:Singulink.IO.IAbsoluteFilePath.MoveTo*>.

</div>
