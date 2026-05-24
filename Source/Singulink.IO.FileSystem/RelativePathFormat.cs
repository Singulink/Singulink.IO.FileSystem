namespace Singulink.IO;

/// <summary>
/// Specifies the format used to parse a relative path that is being appended to a base path.
/// </summary>
public enum RelativePathFormat
{
    /// <summary>
    /// Parse the appended relative path using the base path's format.
    /// </summary>
    MatchBase = 0,

    /// <summary>
    /// Parse the appended relative path using the universal format.
    /// </summary>
    Universal = 1,
}
