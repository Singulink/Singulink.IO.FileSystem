namespace Singulink.IO;

/// <summary>
/// Contains the validity bits that paths store for <see cref="IPath.IsValidFor(PathOptions)"/>: the <see cref="Restrictions"/> flags a path is known to
/// satisfy, <see cref="Basic"/> when it is known to be valid with <see cref="PathOptions.None"/>, and <see cref="Computed"/> when every bit has been worked
/// out, so that an unset bit means the path fails that check.
/// </summary>
/// <remarks>
/// <see cref="Basic"/> and <see cref="Computed"/> use bit positions 1 and 2, which <see cref="PathOptions"/> reserves for them, so they can be stored next to
/// the option flags. A test checks that no <see cref="PathOptions"/> value uses them.
/// </remarks>
internal static class PathValidity
{
    /// <summary>
    /// The options that restrict which paths are valid.
    /// </summary>
    public const PathOptions Restrictions = PathOptions.NoReservedDeviceNames | PathOptions.NoLeadingSpaces | PathOptions.NoTrailingSpaces |
        PathOptions.NoTrailingDots | PathOptions.NoNavigation | PathOptions.NoControlCharacters;

    /// <summary>
    /// The rules that apply regardless of options. Name checks use it for their failures, and paths for being valid with <see cref="PathOptions.None"/>.
    /// </summary>
    public const PathOptions Basic = (PathOptions)(1 << 1);

    /// <summary>
    /// Set when every bit has been worked out.
    /// </summary>
    public const int Computed = 1 << 2;

    /// <summary>
    /// Nothing is known, e.g. for paths made from names that the file system returned.
    /// </summary>
    public const int Unknown = 0;

    /// <summary>
    /// Gets the validity bits a successful parse with the specified options proves.
    /// </summary>
    public static int FromParse(PathFormat format, PathOptions options) => (int)(Basic | (format.GetEffectiveOptions(options) & Restrictions));

    /// <summary>
    /// Gets the validity bits that the new name of a file path proves after it was validated with the specified options. The name is never <c>.</c> or
    /// <c>..</c>, so it satisfies <see cref="PathOptions.NoNavigation"/>.
    /// </summary>
    public static int FromNewFileName(PathFormat format, PathOptions options) => FromParse(format, options) | (int)PathOptions.NoNavigation;

    /// <summary>
    /// Gets the validity bits a path must have to be valid for the specified options.
    /// </summary>
    public static int GetRequired(PathFormat format, PathOptions options, bool isAbsolute)
    {
        var required = Basic | (format.GetEffectiveOptions(options) & Restrictions);

        // Absolute paths are never rooted relative paths and have no navigational segments.
        if (isAbsolute)
            required = required.ClearFlags(PathOptions.NoNavigation);

        return (int)required;
    }
}
