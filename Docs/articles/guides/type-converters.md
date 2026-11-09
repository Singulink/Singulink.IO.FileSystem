<div class="article">

# Type Converters, JSON and Configuration Binding

The path interfaces have a <xref:System.ComponentModel.TypeConverter> attached with <xref:System.ComponentModel.TypeConverterAttribute>, so anything that converts strings through <xref:System.ComponentModel.TypeDescriptor.GetConverter*?displayProperty=nameWithType> can create paths from strings and turn them back into strings. The same interfaces also have a `System.Text.Json` converter, described in [JSON Serialization](#json-serialization) below.

## What Gets Converted

| Type | String it accepts |
| --- | --- |
| <xref:Singulink.IO.IAbsoluteDirectoryPath> | An absolute directory path. |
| <xref:Singulink.IO.IAbsoluteFilePath> | An absolute file path. |
| <xref:Singulink.IO.IRelativeDirectoryPath> | A relative directory path. |
| <xref:Singulink.IO.IRelativeFilePath> | A relative file path. |
| <xref:Singulink.IO.IDirectoryPath> | An absolute or relative directory path; the result is an <xref:Singulink.IO.IAbsoluteDirectoryPath> or an <xref:Singulink.IO.IRelativeDirectoryPath>. |
| <xref:Singulink.IO.IFilePath> | An absolute or relative file path; the result is an <xref:Singulink.IO.IAbsoluteFilePath> or an <xref:Singulink.IO.IRelativeFilePath>. |
| <xref:Singulink.IO.IPath> | An absolute or relative file or directory path; the result is a file or directory path as described below. |
| <xref:Singulink.IO.IAbsolutePath> | An absolute file or directory path; the result is an <xref:Singulink.IO.IAbsoluteDirectoryPath> or an <xref:Singulink.IO.IAbsoluteFilePath>. |
| <xref:Singulink.IO.IRelativePath> | A relative file or directory path; the result is an <xref:Singulink.IO.IRelativeDirectoryPath> or an <xref:Singulink.IO.IRelativeFilePath>. |

For <xref:Singulink.IO.IPath>, <xref:Singulink.IO.IAbsolutePath> and <xref:Singulink.IO.IRelativePath>, the shape of the string decides between a directory and a file. A string that is empty, ends with a separator, or ends with a `.` or `..` segment (such as `.`, `..` or `data/..`) is read as a directory path; any other string is read as a file path. Whether it is absolute or relative is decided the same way as by the `Parse` methods. Directory paths are always written with a trailing separator (except the empty relative directory, which is `""`) and file paths never are, so every path converted to a string reads back as the same path. A hand-written directory path without its trailing separator, such as `C:\data\uploads`, reads as a file path.

Strings are parsed in the current platform's path format (<xref:Singulink.IO.PathFormat.Current?displayProperty=nameWithType>) with <xref:Singulink.IO.PathOptions.None?displayProperty=nameWithType>, as the `Parse` methods on <xref:Singulink.IO.FilePath> and <xref:Singulink.IO.DirectoryPath> parse them with that option, so names that the `Parse` default <xref:Singulink.IO.PathOptions.NoUnfriendlyNames?displayProperty=nameWithType> rejects read too; [Path Formats](#path-formats) describes which written paths read back. <xref:Singulink.IO.IPath.IsValidFor*> and <xref:Singulink.IO.ValidPathAttribute> check a path that was read against other options (see [Checking a Path Against Options](path-options.md#checking-a-path-against-options)). A string that doesn't parse as the target type, such as a relative path for an absolute type or a path ending in a separator for a file type, throws a <xref:System.FormatException> whose message contains the string and the reason, e.g. `Invalid absolute directory path 'data\uploads': Path is not an absolute path.`

Paths convert back to strings as described in [Path Formats](#path-formats): absolute paths as their <xref:Singulink.IO.IPath.PathDisplay>, relative paths in the universal format.

```csharp
var converter = TypeDescriptor.GetConverter(typeof(IAbsoluteDirectoryPath));
var dir = (IAbsoluteDirectoryPath)converter.ConvertFromInvariantString(@"C:\data\uploads")!;
string text = converter.ConvertToInvariantString(dir)!; // "C:\data\uploads\"
```

## Path Formats

Strings are read in the format of the platform the app runs on, exactly as the `Parse` methods read them there. On Windows, both `\` and `/` are separators, and a string that starts with a single `/`, such as `/data/`, is a rooted relative path (relative to the root of the current drive). On Unix (Linux, macOS etc.), only `/` is a separator: a `\` is part of a name, and a Windows absolute path such as `C:\data\` is a relative path whose name contains `:` and `\`.

When an absolute path is expected (<xref:Singulink.IO.IAbsoluteDirectoryPath>, <xref:Singulink.IO.IAbsoluteFilePath> and <xref:Singulink.IO.IAbsolutePath>), a string in the other platform's absolute form fails with a message that says so. On Unix, a Windows absolute path, with a drive letter (`C:`, `C:\data\`, `C:/data/`) or a `\\` prefix (`\\server\share\`, `\\?\C:\data\`), fails with `Path is a Windows absolute path. Absolute paths are read in the format of the platform the app runs on (Unix).` On Windows, a string that starts with a single `/` fails with `Path is a Unix absolute path, which is a rooted relative path on Windows. Absolute paths are read in the format of the platform the app runs on (Windows).`

Absolute paths are written as their <xref:Singulink.IO.IPath.PathDisplay>. Relative paths are written in the universal format (<xref:Singulink.IO.PathFormat.Universal?displayProperty=nameWithType>), with `/` separators, so that they also read on the other platform: `data/uploads/` whether the path was created on Windows or on Unix. A relative path that can't be converted to the universal format is written as its <xref:Singulink.IO.IPath.PathDisplay>: a Windows rooted relative path such as `\data\`, or a Unix path with a name that isn't valid on Windows, such as `a:b.txt`, `a\b.txt` or a name with a control character.

Every path in the format of the platform the app runs on that a converter writes reads back as the same path on that platform, including paths with names that <xref:Singulink.IO.PathOptions.NoUnfriendlyNames?displayProperty=nameWithType> rejects, such as `data \file.` or `con` on Windows; paths with names that the platform's parse rejects even with <xref:Singulink.IO.PathOptions.None?displayProperty=nameWithType>, which only a directory listing can produce, don't read back. A relative path also reads on the other platform as the same names when its names are valid there. A universal-format path reads back as a path in the platform's format.

| String | Read as | On Windows | On Unix |
| --- | --- | --- | --- |
| `data/uploads/` | <xref:Singulink.IO.IRelativeDirectoryPath> | `data\uploads\` | `data/uploads/` |
| `../data/file.txt` | <xref:Singulink.IO.IPath> | relative file `..\data\file.txt` | relative file `../data/file.txt` |
| `data\uploads\` | <xref:Singulink.IO.IRelativeDirectoryPath> | `data\uploads\` | one directory named `data\uploads\` |
| `C:\data\` | <xref:Singulink.IO.IAbsoluteDirectoryPath> | `C:\data\` | error |
| `C:\data\` | <xref:Singulink.IO.IPath> | absolute directory `C:\data\` | relative file named `C:\data\` |
| `C:/data/` | <xref:Singulink.IO.IPath> | absolute directory `C:\data\` | relative directory `C:/data/` (names `C:` and `data`) |
| `/data/` | <xref:Singulink.IO.IAbsoluteDirectoryPath> | error | `/data/` |
| `/data/` | <xref:Singulink.IO.IPath> | rooted relative directory `\data\` | absolute directory `/data/` |

| Path | Written as |
| --- | --- |
| Absolute, e.g. `C:\data\` or `/data/` | Its <xref:Singulink.IO.IPath.PathDisplay>: `C:\data\` or `/data/` |
| Relative Windows path `..\data\file.txt` | `../data/file.txt` |
| Relative Unix path `data/uploads/` | `data/uploads/` |
| The empty relative directory | `""` |
| Windows rooted relative path `\data\` | `\data\` |
| Unix path `data/a:b.txt` | `data/a:b.txt` |
| Unix path `a\b.txt` (one name) | `a\b.txt` |
| Windows path `data \file.` (created with <xref:Singulink.IO.PathOptions.None?displayProperty=nameWithType>) | `data /file.` |

## JSON Serialization

Each of the path interfaces above has a `System.Text.Json` converter attached with <xref:System.Text.Json.Serialization.JsonConverterAttribute>, so properties, collection elements, dictionary keys and values of these types serialize as JSON strings with no setup. The converters are public types in the `Singulink.IO.Json` namespace:

| Type | Converter |
| --- | --- |
| <xref:Singulink.IO.IAbsoluteDirectoryPath> | <xref:Singulink.IO.Json.AbsoluteDirectoryPathJsonConverter> |
| <xref:Singulink.IO.IAbsoluteFilePath> | <xref:Singulink.IO.Json.AbsoluteFilePathJsonConverter> |
| <xref:Singulink.IO.IRelativeDirectoryPath> | <xref:Singulink.IO.Json.RelativeDirectoryPathJsonConverter> |
| <xref:Singulink.IO.IRelativeFilePath> | <xref:Singulink.IO.Json.RelativeFilePathJsonConverter> |
| <xref:Singulink.IO.IDirectoryPath> | <xref:Singulink.IO.Json.DirectoryPathJsonConverter> |
| <xref:Singulink.IO.IFilePath> | <xref:Singulink.IO.Json.FilePathJsonConverter> |
| <xref:Singulink.IO.IPath> | <xref:Singulink.IO.Json.PathJsonConverter> |
| <xref:Singulink.IO.IAbsolutePath> | <xref:Singulink.IO.Json.AbsolutePathJsonConverter> |
| <xref:Singulink.IO.IRelativePath> | <xref:Singulink.IO.Json.RelativePathJsonConverter> |

Paths are written and read with the same defaults and rules as the type converters (including the directory or file rule for <xref:Singulink.IO.IPath>, <xref:Singulink.IO.IAbsolutePath> and <xref:Singulink.IO.IRelativePath> and the [path formats](#path-formats)), both as values and as dictionary keys. A JSON `null` reads as `null` and a `null` path is written as `null`. The empty relative directory is written as `""` and reads back as the empty relative directory. A value that isn't a string, or a string that doesn't parse as the target type, throws a <xref:System.Text.Json.JsonException> whose message contains the string and the reason and whose <xref:System.Text.Json.JsonException.Path> points at the value, e.g. `Invalid absolute directory path 'data\uploads': Path is not an absolute path.` at `$.Directory`.

```csharp
public class UploadState
{
    public IAbsoluteDirectoryPath? Directory { get; set; }

    public IRelativeFilePath? LastFile { get; set; }
}

string json = JsonSerializer.Serialize(state);
// Windows: {"Directory":"C:\\data\\uploads\\","LastFile":"2026/upload.bin"}
// Unix:    {"Directory":"/data/uploads/","LastFile":"2026/upload.bin"}
```

Source-generated contexts (<xref:System.Text.Json.Serialization.JsonSerializerContext>) use the attached converters as well, including in trimmed and native AOT apps. The converters can also be listed explicitly, for example in <xref:System.Text.Json.Serialization.JsonSourceGenerationOptionsAttribute.Converters?displayProperty=nameWithType> or <xref:System.Text.Json.JsonSerializerOptions.Converters?displayProperty=nameWithType>.

A path stored in a property, collection element or dictionary value of type `object` is also written as a string, since the converters are attached to the library's path classes as well. Reading such a value gives what the serializer reads for `object` (a <xref:System.Text.Json.JsonElement>), not a path.

## Configuration Binding

The reflection-based configuration binder in `Microsoft.Extensions.Configuration.Binder` converts string values through type converters, so options properties of the types above bind from configuration values with `Get<T>()`, `Bind()`, `services.Configure<T>(section)` and `AddOptions<T>().Bind(section)`. This includes nested options objects, collections and dictionaries.

```json
{
  "Storage": {
    "Directory": "D:\\data\\uploads",
    "SavedRangesPath": "D:\\data\\ranges.bin"
  }
}
```

```csharp
public class StorageOptions
{
    public IAbsoluteDirectoryPath? Directory { get; set; }

    public IAbsoluteFilePath? SavedRangesPath { get; set; }
}

services.Configure<StorageOptions>(configuration.GetSection("Storage"));
```

A value that doesn't parse makes the binder throw an <xref:System.InvalidOperationException> naming the value, the configuration key and the target type, with the <xref:System.FormatException> described above as its inner exception.

Options validation with DataAnnotations (`ValidateDataAnnotations()` or the `[OptionsValidator]` source generator) checks path properties marked with <xref:Singulink.IO.ValidPathAttribute> against the options it names; see [Checking a Path Against Options](path-options.md#checking-a-path-against-options).

### Configuration Binding Source Generator

The configuration binding source generator (`EnableConfigurationBindingGenerator`, used for trimmed and native AOT apps) does not use type converters. It reports warnings `SYSLIB1100` and `SYSLIB1101` for properties of path types and leaves them unset. With the source generator, path values can be read from configuration as strings and parsed with <xref:Singulink.IO.FilePath> and <xref:Singulink.IO.DirectoryPath>, for example:

```csharp
var section = configuration.GetSection("Storage");

services.AddOptions<StorageOptions>().Configure(o => {
    o.Directory = DirectoryPath.ParseAbsolute(section["Directory"]!);
    o.SavedRangesPath = FilePath.ParseAbsolute(section["SavedRangesPath"]!);
});
```

</div>
