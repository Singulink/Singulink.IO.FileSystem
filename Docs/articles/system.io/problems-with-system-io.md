<div class="article">

# Problems with System.IO

## Endless Pitfalls

The following is just a small sampling of the numerous pitfalls present in `System.IO` which make it virtually impossible to use for building reliable applications:

#### Strange Behavior/Bugs

If you get `DirectoryInfo.Attributes` for a path that points to a file or `FileInfo.Attributes` for a path that points to a directory it will happily give you the attributes even though `Exists` is false in both cases. `File.GetAttributes()` will happily return attributes for both files and directories.

On Windows, calling `File.Delete()` on a directory path throws `UnauthorizedAccessException` instead of `FileNotFoundException`. Calling `Directory.Delete()` on a file path throws `IOException: directory name is invalid` instead of `DirectoryNotFoundException`. This behavior is not consistent across other platforms.

Calling `Directory.GetParent(@"C:\temp\")` just returns `"C:\temp"` due to its naive algorithm so you have to be very careful about trailing slashes when manipulating paths.

#### Problematic Handling of Spaces

Silently modifying paths during file operations that contain leading/trailing spaces and dots is a major source of bugs. Here are some examples:

```c#
var userProvidedDir = @"C:\directory \file.txt"; // Oops, user put a space after the directory.
var fileInfo = new FileInfo(userProvidedPath);
fileInfo.ParentDirectory.Create();

using (var stream = fileInfo.Create())
{
    // write some file contents
}

bool exists = File.Exists(userProvidedPath); // FALSE! No file for you!
File.Open(userProvidedPath); // Exception!

// Similar problem:

var dirInfo = new DirectoryInfo(@"C:\directory \subdirectory");
dirInfo.Create();

bool exists = dirInfo.Parent.Exists; // FALSE! No directory for you!
```

If you parsed the path in `Singulink.IO.FileSystem` with `PathOptions.NoUnfriendlyNames` then the user would be notified of this problematic path. If you are opening an existing file then `PathOptions.None` will ensure you can seamlessly deal with any existing path the user throw at you.

#### Unable to Open Existing Files

`System.IO` will have difficulty if the path is "unfriendly" even though the user directly selected an existing file using an open file dialog, i.e. if it contains trailing/leading spaces, trailing dots, reserved device names, etc:

```c#
string filePathString = new OpenFileWindow().FilePath;
File.Open(filePathString); // Possible FileNotFoundException
```

Meanwhile, this "Just Works" (TM) in `Singulink.IO.FileSystem`!

```c#
string filePathString = new OpenFileWindow().FilePath;
var filePath = FilePath.Parse(filePathString, PathOptions.None);
filePath.OpenStream(); 
```

#### DriveInfo

The only way to obtain available/used space information in `System.IO` is via `DriveInfo`. This limits you to only getting information for root directories in Windows and it does not work with UNC paths. The concept of "drives" is not cross-platform applicable and thus is not present in this library. Instead, the functionality of `DriveInfo` is now present in a much more versatile and reliable manner on all `IAbsoluteDirectoryPath` instances.

#### Cross-Platform Concerns

Searching for files/directories with a wildcard pattern using `System.IO` has different case-sensitivity settings by default on Unix and Windows. `Singulink.IO.FileSystem` does case-insensitive searches by default so you get consistent behavior across your app platforms unless you opt into platform-specific behavior.

There is no way to determine whether a path will be cross-platform friendly using `System.IO`, nor is there a way to process and manipulate paths from the platform you aren't currently running on. `Singulink.IO.FileSystem` gives you `PathFormat.Universal` to validate that paths will work everywhere and convert them into a common format. You can also explicitly specify that a path is in Unix or Windows format during parsing and convert relative paths between the two formats as needed.

#### UNC Path Handling

There are numerous methods and operations that throw exceptions or do work correctly with UNC paths.

### And more...

This short list of issues is far from exhaustive, it's just what I could think of off the top of my head. There are countless pitfalls when using `System.IO` but hopefully by this point I have convinced you that it is a bloody minefield that should be avoided for any serious development. It takes immense effort and deep knowledge of all the nuances to use `System.IO` in a reliable manner, especially in cross-platform development. **It's simply too hard to get right**. `Singulink.IO.FileSystem` makes it easy and intuitive to write correct and reliable code every time.

</div>