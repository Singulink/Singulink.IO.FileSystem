namespace Singulink.IO;

/// <summary>
/// Specifies the behavior when encountering inaccessible directories during searches.
/// </summary>
public enum InaccessibleSearchBehavior
{
    /// <summary>
    /// Indicates that an <see cref="UnauthorizedIOAccessException"/> should be thrown if the directory being searched is inaccessible but inaccessible child
    /// directories are ignored. This is the default behavior as it has the least surprising behavior, but it requires an extra file system call for recursive
    /// searches where no matches are returned to check if the search root is accessible.
    /// </summary>
    ThrowForSearchDir,

    /// <summary>
    /// Indicates that <see cref="UnauthorizedIOAccessException"/> should be thrown if any inaccessible directories are encountered during the search.
    /// </summary>
    ThrowForAll,

    /// <summary>
    /// Indicates that all unauthorized accesses should be ignored, including the directory being searched.
    /// </summary>
    IgnoreAll,
}
