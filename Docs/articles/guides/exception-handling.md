<div class="article">

# Exception Handling

### Overview

The library is designed to make exception handling boringly predictable:

- **Parsing throws <xref:System.ArgumentException>** (and subtypes). Every input-validation error during path construction comes from this family.
- **I/O throws <xref:System.IO.IOException>** (and subtypes). Every error from a file system operation comes from this family: including permission errors, which are surfaced as <xref:Singulink.IO.UnauthorizedIOAccessException> (a subclass of <xref:System.IO.IOException>).

That separation lets you write tidy `try`/`catch` blocks without resorting to `catch (Exception)` to corral the mix of unrelated exception types `System.IO` would otherwise throw.

## Parse-Time Errors

<xref:System.ArgumentException> (or <xref:System.ArgumentNullException> / <xref:System.ArgumentOutOfRangeException> for null or invalid arguments) is the only exception family thrown by parsing and path-manipulation methods. Catch it where you accept untrusted input:

```csharp
IFilePath file;
try
{
    file = FilePath.Parse(userInput);
}
catch (ArgumentException ex)
{
    log.Warn($"Invalid path: {ex.Message}");
    return;
}
```

Exception messages are detailed enough to act on directly. They name the exact rule that failed (e.g. "Entry name ends with a dot.", "Attempt to navigate past root directory.").

## I/O-Time Errors

Every method that touches the file system throws only <xref:System.IO.IOException> and its subtypes, across both Windows and Unix. The full set you can encounter:

| Exception | Typical cause |
|-----------|---------------|
| <xref:System.IO.FileNotFoundException> | A file expected to exist is missing. |
| <xref:System.IO.DirectoryNotFoundException> | A directory expected to exist is missing. |
| <xref:System.IO.PathTooLongException> | The fully-qualified path exceeds the OS limit. |
| <xref:Singulink.IO.UnauthorizedIOAccessException> | The OS denied access. |
| <xref:System.IO.IOException> | Anything else (file in use, disk full, network failure, etc.). |

A single base-type catch handles all of them:

```csharp
try
{
    using FileStream s = file.OpenStream(FileMode.Open, FileAccess.Read, FileShare.Read);
    return Read(s);
}
catch (IOException ex)
{
    log.Warn($"Could not read {file.PathDisplay}: {ex.Message}");
    return null;
}
```

## UnauthorizedIOAccessException

`System.IO` surfaces permission errors as <xref:System.UnauthorizedAccessException>, which does **not** derive from <xref:System.IO.IOException>. That forces you to catch two unrelated base types to handle "any I/O failure".

This library raises <xref:Singulink.IO.UnauthorizedIOAccessException> instead (a direct <xref:System.IO.IOException> subclass), so a single `catch (IOException)` covers every failure mode. If you specifically need to distinguish access denial:

```csharp
try
{
    file.Delete();
}
catch (UnauthorizedIOAccessException) { /* show "permission denied" UI */ }
catch (IOException)                    { /* something else went wrong */ }
```

> [!NOTE]
> The library converts <xref:System.UnauthorizedAccessException> thrown by underlying `System.IO` calls into <xref:Singulink.IO.UnauthorizedIOAccessException>. You should never need to catch <xref:System.UnauthorizedAccessException> from this library's surface.

## Cross-Platform Consistency

`System.IO` throws different exception types on Windows and Unix for the same operation. For example, <xref:System.IO.File.Delete*> on a directory throws <xref:System.UnauthorizedAccessException> on Windows but <xref:System.IO.IOException> (or different) on Unix. This library normalizes those: the exception type for a given failure is the same across platforms.

> [!IMPORTANT]
> If you're catching specific exception types in code that needs to behave the same on Windows and Unix, use this library's surface; `System.IO`'s exception types are not portable.

## Search-Time Errors

Enumeration is lazy. Exceptions can be thrown during the iteration of a `foreach`, not just from the call that produced the enumerable. Catch around the iteration site:

```csharp
try
{
    foreach (var f in dir.GetChildFiles("*", new SearchOptions { Recursive = true }))
        Process(f);
}
catch (UnauthorizedIOAccessException ex)
{
    log.Warn($"Search hit a forbidden directory: {ex.Message}");
}
```

The behavior on inaccessible directories is configurable. See [Searching and Enumeration](searching-and-enumeration.md#inaccessible-directories) and <xref:Singulink.IO.InaccessibleSearchBehavior>.

## Putting It Together

A typical input-handling pattern that keeps parsing and I/O concerns clean:

```csharp
// Phase 1: validate input. Only ArgumentException is possible here.
IAbsoluteFilePath file;
try
{
    file = FilePath.ParseAbsolute(userInput);
}
catch (ArgumentException ex)
{
    return Result.Invalid(ex.Message);
}

// Phase 2: do I/O. Only IOException family is possible here.
try
{
    using FileStream s = file.OpenStream(FileMode.Open, FileAccess.Read, FileShare.Read);
    return Result.Ok(Read(s));
}
catch (FileNotFoundException)             { return Result.NotFound(); }
catch (UnauthorizedIOAccessException ex)  { return Result.Forbidden(ex.Message); }
catch (IOException ex)                    { return Result.Error(ex.Message); }
```

## Next Steps

- [Searching and Enumeration](searching-and-enumeration.md): search-time error handling and <xref:Singulink.IO.InaccessibleSearchBehavior>.
- [Working with Files](file-operations.md) and [Working with Directories](directory-operations.md): the operations covered by the I/O-side catches.

</div>
