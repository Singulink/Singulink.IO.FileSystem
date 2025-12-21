namespace Singulink.IO;

/// <summary>
/// Represents an absolute or relative path to a file.
/// </summary>
public interface IFilePath : IPath
{
    /// <summary>
    /// Gets the file name without the extension.
    /// </summary>
    /// <remarks>
    /// The dot in the extension is also removed from the name. File names with no extension are returned without changes. File names with trailing dots will
    /// have the dot removed. Files with multiple extensions will have only the last extension removed, e.g. <c>"archive.tar.gz"</c> becomes
    /// <c>"archive.tar"</c>.
    /// </remarks>
    sealed string NameWithoutExtension => PathFormat.GetFileNameWithoutExtension(PathDisplay);

    /// <summary>
    /// Gets the file extension including the leading dot, otherwise an empty string.
    /// </summary>
    /// Files names with trailing dots will return an extension which is just a dot.
    sealed string Extension => PathFormat.GetFileNameExtension(PathDisplay);

    #region Path Manipulation

    /// <summary>
    /// Changes the current extension of the file name to a new extension.
    /// </summary>
    /// <param name="extension">The new extension that should be applied to the file. Must either be null/empty or start with a dot '.' character and contain no
    /// additional dots. (e.g. <c>".txt"</c>).</param>
    /// <param name="options">The options to apply when parsing the new file name. The rest of the path is not reparsed.</param>
    /// <remarks>
    /// Only the last extension of the file name is replaced, preserving any previous extensions before it, e.g. <c>"archive.tar.gz"</c> with extension
    /// <c>".zip"</c> becomes <c>"archive.tar.zip"</c>. If the file name has multiple extensions and the new extension is null/empty, only the last extension is
    /// removed, e.g. <c>"archive.tar.gz"</c> becomes <c>"archive.tar"</c>.
    /// </remarks>
    IFilePath WithExtension(string? extension, PathOptions options = PathOptions.NoUnfriendlyNames);

    /// <summary>
    /// Adds an extension to the file name, preserving any existing extensions before it.
    /// </summary>
    /// <param name="extension">The new extension that should be applied to the file. Must either be null/empty or start with a dot '.' character and contain
    /// no additional dots. (e.g. <c>".txt"</c>).</param>
    /// <param name="options">The options to apply when parsing the new file name. The rest of the path is not reparsed.</param>
    IFilePath AddExtension(string? extension, PathOptions options = PathOptions.NoUnfriendlyNames);

    #endregion
}
