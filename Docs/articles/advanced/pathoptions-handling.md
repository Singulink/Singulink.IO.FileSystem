<div class="article">

# PathOptions Handling

## Summary

More detailed article coming soon. See the [PathOptions API documentation](../../api/Singulink.IO.PathOptions.yml) for descriptions of the possible options that can be used when parsing paths.

### Unfriendly Names

The two most important things to consider when using path options other than `PathOptions.NoUnfriendlyNames` are:
1) The paths may not be usable from Windows Explorer or other Windows applications, i.e. if they contain trailing spaces, reserved device names or end with a dot. For example, users may be stuck not being able to delete the files/directories without resorting to advanced command line operations.
2) Serializing/deserializing the paths must be handled with a high degree of care to ensure that leading and trailing spaces are preseved, otherwise round tripping the value will result in a path that points to a different file or directory.

If you are receiving the path from something like an `OpenFileWindow` and simply opening the existing file without storing the path for later use then it is safe to use `PathOptions.None` to allow access to all existing files in the file system, even if the path is "unfriendly."

### Empty Directory Names

By default, it is an error to parse a path that contains empty directory names such as `path/to//some/dir` (notice the double slash resulting in an empty path segment between them) as this indicates a malformed path. If you would like the parser to normalize out empty directories instead then you can use the `PathOptions.AllowEmptyDirectories` option, which would cause the above path to be parsed as `path/to/some/dir`.

</div>