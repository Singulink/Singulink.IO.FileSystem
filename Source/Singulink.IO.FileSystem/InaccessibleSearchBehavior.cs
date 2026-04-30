namespace Singulink.IO;

/// <summary>
/// Specifies the behavior when encountering inaccessible directories during searches.
/// </summary>
public enum InaccessibleSearchBehavior
{
    /// <summary>
    /// Indicates that an <see cref="UnauthorizedIOAccessException"/> should be thrown if the directory being searched is inaccessible, but inaccessible child
    /// directories should be ignored. This is the default behavior as it has the least surprising behavior, but it incurs an extra file system call when the
    /// search yields no matches.
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
