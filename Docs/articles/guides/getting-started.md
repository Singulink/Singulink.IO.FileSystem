<div class="article">

# Getting Started

## Usage

### Path Creation

Your main entry points into creating file and directory paths are the `DirectoryPath` and `FilePath` classes. They contain parsing methods such as `ParseAbsolute()` and `ParseRelative()` as well as conveniece methods to retrieve special files and folders such as a temporary file or the current directory.

### Strong Typing

Paths are always strongly typed to the kind of path they represent. Every path implements `IPath`, but there are two main sub-branches of interface hierarchies that represent the possible path types. The first branches on whether the path is relative or absolute via `IRelativePath` and `IAbsolutePath`, and the second branches on whether the path points to a file or a directory via `IFilePath` and `IDirectoryPath`. These are then combined into all possible specific combinations with `IRelativeFilePath`, `IRelativeDirectoryPath`, `IAbsoluteFilePath` and `IAbsoluteDirectoryPath`. Every instance of a path implements one of those final 4 specific interfaces. 

Some methods or operations may return a less specific interface if all the information is not statically available about the resulting path, but the result can always be cast to one of the 4 specific interfaces.

### Explicit Intent

Consider the following example:

```c#
string userEnteredPath = GetPathFromUser();

// Parses both relative and absolute file paths:

IFilePath filePath = FilePath.Parse(userEnteredPath, PathOptions.NoUnfriendlyNames);

IAbsoluteFilePath finalFilePath;

if (filePath is IAbsoluteFilePath absFilePath)
{
    finalFilePath = absFilePath;
}
else
{
    // Since it wasn't absolute, we know we have a relative file path so 
    // add it to the current working directory to get an absolute directory
    // that we can do file system operations on:

    IAbsoluteDirectoryPath currentDirectory = DirectoryPath.GetCurrent();
    finalFilePath = currentDirectory + (IRelativeFilePath)filePath;
}

// Create the file and do something with it:

FileStream stream = finalFilePath.OpenStream(FileMode.Create);
```

This example highlights several important aspects of the library. First, file system operations can only be performed on absolute paths. This forces you to consider and make explicit your intent about what relative paths should actually be relative to - should it be relative to the current directory, the executing assembly, the application folder, or something else?

Secondly, you can see that `PathOptions.NoUnfriendlyNames` has been specified during parsing. `PathOptions` controls the parsing behavior and the default value on all methods that accept string paths is `NoUnfriendlyNames` if left unspecified. This is to ensure that you consider and explicitly state if you are prepared to handle unfriendly paths. Most applications should not attempt to handle unfriendly paths so sticking with `NoUnfriendlyNames` is recommended unless the need to process unfriendly paths is established and you are prepared to consider the steps you need to take to ensure proper handling of them. See [Advanced PathOptions Handling](../advanced/pathoptions-handling.md) for more details on the topic.

### No Silent Path Modification

Input paths are normalized (i.e. `..` and `.` are resolved and removed when possible), but no named component of the path is modified at any point in time as that is a huge source of bugs. If the path contains a trailing space, trailing dot, leading space, etc, it is always preserved. Using `PathOptions` you can detect errors in paths that are likely malformed so the user can handle the situation instead of silently modifying the path during file system operations and creating bugs. If you want to be friendly to users and trim the input path then you are free to do so prior to passing the path for parsing, in which case the modifications are explicit and the path string inside your application maintains its integrity.

Furthermore, navigating past the root element of a path, i.e. `C:\dir1\..\..\dir2` is always an error as it indicates a malformed path. Countless bugs have resulted from paths such as that silently pointing to incorrect locations after files are copied/pasted or moved.

### Export and Display Paths

The `ToString()` method on paths purposely returns an unusable path by prepending the string with either `[Directory]` or `[File]` depending on the kind of path it is. This is because there are two different kinds of path strings that can be obtained from a path instance which you should explicitly pick depending on the circumstances: the `PathDisplay` string and the `PathExport` string.

The `PathDisplay` string is friendly for display in interfaces and it can also be used for parsing/storing and serializing/deserializing in this library with proper round tripping. This string should NOT be used anywhere outside of this library, including `System.IO` methods that accept string paths or anything else outside of this library.

If you need to "export" a path for use outside of this library, i.e. to call `new FileStream(path)`, then the proper string value to use is `PathExport`. This is a specially formatted string that ensures the underlying file system does not silently modify the path to be anything other than what was parsed. This string is also safe to use for parsing/storing and serializing/deserializing in this library but takes a less user friendly form so you may want to consider using `PathDisplay` instead if the value will be visible to users, i.e. in a configuration file.

### Cross-Platform Abstraction

The only way to get information about available space or used space with `System.IO` is with `DriveInfo`, but that has a few problems:
1) It is inherently flawed as a cross-platform concept since Unix has no "drives".
2) It does not work for UNC paths
3) The actual folder you are working with may not have the same available space as the root drive. For example, there could be quotas attached to a directory for the current user, or a drive/network share might be mounted in a subfolder.

For these reasons, there is no concept of a "drive" in this library. Instead, the fuctionality provided by `DriveInfo` is exposed in a much more reliable manner directly on all absolute directory paths. You can use `DirectoryPath.GetMountingPoints()` to get a list of directories that represent drives on Windows and mounting points in Unix.

If you want to get available free space for an installation path, for example, you would do it like this:

```c#
string installPathString = GetInstallPathFromUser();

IAbsoluteDirectoryPath installPath = DirectoryPath.ParseAbsolute(installPathString);

// Check available free space in the last directory in the path that exists:

long availableSpaceBytes = installPath.GetLastExistingDirectory().AvailableFreeSpace;

if (availableSpaceBytes < InstallSizeBytes)
    throw new Exception("Not enough available space in the path for installation.");

// Create the installation dir:

installPath.Create();
```

### Cross-Platform Path Handling

All methods that accept string path parameters have an optional `PathFormat` parameter. If this parameter is not specified then the default is to use the path format of the current system, i.e. `PathFormat.Windows` or `PathFormat.Unix`. 

There is one additional special path format called `PathFormat.Universal`. The universal path format is how you should store all paths in databases or files that are expected to work cross-platform. It uses `/` as the separator character and due to the platform-specific nature of absolute paths, only relative paths are allowed. The universal path format ensures that the paths are portable across both Unix and Windows file systems - it is a common denominator format. Platform specific relative paths can be converted to/from universal path formats and universal paths can always be combined with platform specific paths to produce a resulting platform specific path.

If, for example, you are manipulating Unix paths from Windows, you can simply specify that you want to use `PathFormat.Unix` for parsing the path and everything works how you would expect it to. File system operations can only be performed on paths that are in the appropriate path format for the current operating system.

## Example

```c#
string usersFilePathString = "../Data/Users.json";

IRelativeFilePath usersFileRelativePath = 
    FilePath.ParseRelative(userPathString, PathFormat.Universal);

// dataDirRelativePath = "../Data" in universal format since usersFileRelativePath is
// in universal format:
IRelativeDirectoryPath dataDirRelativePath = usersFileRelativePath.ParentDirectory;

// DirectoryPath.GetCurrent() returns path in current platform path format, 
// i.e. Windows or Unix:
var currentDirectory = DirectoryPath.GetCurrent();

// usersFilePath path format will be platform specific since a platform specific path 
// was added to a universal path:
IAbsoluteFilePath usersFilePath = currentDirectory + usersFileRelativePath;

using (Stream usersStream = usersFilePath.OpenStream())
{
    var usersData = new UsersData(usersStream);

    // passwordFileRelativePath = "../Data/Passwords.enc" in universal format since
    // dataDirectoryPath is in universal format
    IRelativeFilePath passwordFileRelativePath = 
        dataDirRelativePath.CombineFile("Passwords.enc");

    // PathDisplay is okay to use for storage/serialization
    usersData.SetPasswordDataPath(passwordDataPath.PathDisplay);

    // This will be platform specific since usersFilePath is a platform specific
    // absolute path.
    var parentDirPath = usersFilePath.ParentDirectory;

    // relativeDataFile will be platform specific since all file system operations
    // use platform specific path formats.
    foreach (var relativeDataFile in parentDirPath.GetRelativeChildFiles("*.data"))
    {
        // Try to convert the format to universal:
        try
        {
            var universalPath = relativeDataFile.ToPathFormat(PathFormat.Universal);

            // Conversion to relative universal path was successful
            usersData.DataFiles.Add(universalPath.PathDisplay);
        }
        catch (ArgumentException ex)
        {
            // The path could not be converted to universal format due to invalid 
            // characters or an unfriendly name - ex.Message contains a detailed 
            // message as to what caused it to fail.
            LogWarning($"Skipping data file '{relativeDataFile.PathDisplay}': {ex.Message}");
        }
    }

    usersData.Save();
}
```

</div>