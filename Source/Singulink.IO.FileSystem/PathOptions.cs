using System;

#pragma warning disable RCS1154 // Sort enum members.

namespace Singulink.IO;

/// <summary>
/// Provides options to control how path parsing is handled.
/// </summary>
/// <remarks>
/// <para>Most applications should not attempt to process unfriendly paths as the pitfalls and edge cases are numerous and difficult to predict. <see
/// cref="NoUnfriendlyNames"/> is generally the recommended option to use. Applications like file managers that must work with all possible paths including
/// those that are likely to be buggy/problematic should use <see cref="None"/> instead and take great care to ensure they are handled correctly. It is
/// safe to use <see cref="None"/> for paths that are obtained by directly querying the file system (as opposed to user input or file data) and never
/// stored for later use.
/// </para>
/// </remarks>
[Flags]
public enum PathOptions
{
    /// <summary>
    /// Default value with no options set which allows all possible valid file system paths without wildcard characters.
    /// </summary>
    None = 0,

    /// <summary>
    /// <para>Allows paths with empty directories to be processed without throwing an exception by removing them from the path.</para>
    /// <para>If this flag is set then paths like <c>some///path</c> get parsed to <c>some/path</c>.</para>
    /// </summary>
    AllowEmptyDirectories = 1,

    /// <summary>
    /// <para>Disallows entry names that match reserved device names. This flag has no effect on the <see cref="PathFormat.Unix"/> path format.</para>
    /// <para>Reserved device names in paths can cause problems for many Windows applications and are not supported by File Explorer. Reserved device
    /// names include CON, PRN, AUX, NUL, COM1 to COM9 and LPT1 to LPT9.</para>
    /// </summary>
    NoReservedDeviceNames = 1 << 8,

    /// <summary>
    /// <para>Disallows entry names with a leading space.</para>
    /// <para>Leading spaces can cause problems for many Windows applications and are not fully supported by File Explorer. They can be difficult to handle
    /// correctly in application code, i.e. trimming input from users/data needs to be handled with care and <see cref="System.IO"/> often doesn't play
    /// nice with them on Windows.</para>
    /// </summary>
    NoLeadingSpaces = 1 << 9,

    /// <summary>
    /// <para>Disallows entry names with a trailing space.</para>
    /// <para>Trailing spaces can cause problems for many Windows applications and are not supported by File Explorer. They can be difficult to handle
    /// correctly in application code, i.e. trimming input from users/data needs to be handled with care and <see cref="System.IO"/> often doesn't play
    /// nice with them on Windows.</para>
    /// </summary>
    NoTrailingSpaces = 1 << 10,

    /// <summary>
    /// <para>Disallows entry names with a trailing dot. This flag has no effect on the <see cref="PathFormat.Unix"/> path format.</para>
    /// <para>Trailing dots can cause problems for many Windows applications, are not supported by File Explorer and <see cref="System.IO"/> often doesn't
    /// play nice with them on Windows. Trailing dots do not pose any problems in Unix-based file systems and they don't pose potential trimming bugs so
    /// this flag has no effect when the <see cref="PathFormat.Unix"/> path format is used.</para>
    /// </summary>
    NoTrailingDots = 1 << 11,

    /// <summary>
    /// Disallows navigational path segments (i.e. <c>.</c> or <c>..</c>) and rooted relative paths (i.e. <c>/Some/Path</c> when using the <see
    /// cref="PathFormat.Windows"/> path format). Regular non-rooted relative paths are permitted.
    /// </summary>
    NoNavigation = 1 << 12,

    /// <summary>
    /// A combination of the <see cref="NoReservedDeviceNames"/>, <see cref="NoLeadingSpaces"/>, <see cref="NoTrailingSpaces"/> and <see
    /// cref="NoTrailingDots"/> flags. This is the default value used for all parsing operations if no value is specified.
    /// </summary>
    NoUnfriendlyNames = NoReservedDeviceNames | NoLeadingSpaces | NoTrailingSpaces | NoTrailingDots,

    /// <summary>
    /// <para>Effectively causes the <see cref="NoUnfriendlyNames"/> flags to be appended when using the <see cref="PathFormat.Windows"/> and <see
    /// cref="PathFormat.Universal"/> path formats.</para>
    /// <para>Unix-based file systems tend to handle "unfriendly" paths much better than Windows-based file systems, so you can use this flag if you only want
    /// to disallow unfriendly paths on Windows and universal paths.</para>
    /// </summary>
    PathFormatDependent = 1 << 31,
}