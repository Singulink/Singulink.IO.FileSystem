namespace Singulink.IO;

/// <summary>
/// Represents the state of an entry in the file system.
/// </summary>
public enum EntryState
{
    /// <summary>
    /// The entry exists.
    /// </summary>
    Exists,

    /// <summary>
    /// The entry does not exist, but its parent directory exists.
    /// </summary>
    ParentExists,

    /// <summary>
    /// Neither the entry nor its parent directory exist.
    /// </summary>
    ParentDoesNotExist,

    /// <summary>
    /// The entry exists, but it is not of the expected type (e.g., a file where a directory is expected, or vice versa).
    /// </summary>
    WrongType,
}
