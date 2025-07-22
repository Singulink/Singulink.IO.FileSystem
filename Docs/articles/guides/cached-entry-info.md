<div class="article">

# Cached Entry Info

#### System.IO Problems

The `FileSystemInfo` subclasses in `System.IO` (`FileInfo`, `DirectoryInfo`) have a rather peculiar design. They can be constructed with a path that does not exist and later throw exceptions when you try to access properties since they only query the file system on first property access. Some properties like `Attributes` will return successfully even if the type of entry (i.e. file or directory) does not match the path you provided, which can lead to confusion and bugs.

Additionally, the properties are mutable and cause the cached information to clear when they are set, which causes the next access to re-query the file system. If the file system entry is no longer valid then the info object becomes unusable. This can lead to consistency issues and unexpected behavior if you are not careful.

#### The Singulink.IO.FileSystem Solution

The `CachedEntryInfo` subclasses (i.e. `CachedFileInfo`, `CachedDirectoryInfo`) can only be obtained for paths that exist and they are always populated with the entry information. Obtain an entry info object for a path by calling `path.GetInfo()`, or when enumerating directories with methods like `directory.GetChildEntriesInfo()`.

The properties of cached info objects are read-only. To modify file or directory attributes, use the associated `IAbsolutePath` object available via the `Path` property. For example, you can update file attributes with `cachedInfo.Path.Attributes = ...` and once changes are made, you can call `cachedInfo.Refresh()` to update the cached information. This approach ensures the cached data remains valid and consistent with the underlying file system and re-querying the file system is explicit.

</div>
