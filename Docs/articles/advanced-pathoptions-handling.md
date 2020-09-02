# Advanced PathOptions Handling

More detailed article coming soon...for now, see the [PathOptions API documentation](../api/Singulink.IO.PathOptions.yml) for a detailed description of its values.

The two most important things to consider when using path options other than `PathOptions.NoUnfriendlyNames` are:
1) The paths may not be usable from Windows Explorer or other Windows applications, i.e. if they contain trailing spaces, reserved device names or end with a dot. For example, users may be stuck not being able to delete the files/directories without resorting to advanced command line operations.
2) Serializing/deserializing the paths must be handled with a high degree of care to ensure that leading and trailing spaces are preseved, otherwise round tripping the value will result in a path that points to a different file or directory.

If you are receiving the path from something like an `OpenFileWindow` and simply opening the existing file without storing the path for later use then it is safe to use `PathOptions.None` to allow access to all existing files in the file system, even if the path is "unfriendly."

By default, it is an error to parse a path that contains empty directories such as `path/to//some/dir` (notice the double slash resulting in an empty directory between them) as this indicates a malformed path. If you would like the parser to ignore empty directories then you can use the `PathOptions.AllowEmptyDirectories` option.