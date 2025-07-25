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
    /// <para>The dot in the extension is also removed from the name. File names with no extension are returned without changes. File names with trailing dots
    /// will have the dot removed.</para>
    /// </remarks>
    sealed string NameWithoutExtension => PathFormat.GetFileNameWithoutExtension(PathDisplay);

    /// <summary>
    /// Gets the file extension including the leading dot, otherwise an empty string.
    /// </summary>
    /// Files names with trailing dots will return an extension which is just a dot.
    sealed string Extension => PathFormat.GetFileNameExtension(PathDisplay);

    #region Path Manipulation

    /// <summary>
    /// Adds a new extension or changes the existing extension of the file.
    /// </summary>
    /// <param name="newExtension">The new extension that should be applied to the file. Must either be null/empty or start with a dot '.' character and contain
    /// no additional dots. (e.g. <c>".txt"</c>).</param>
    /// <param name="options">The options to apply when parsing the new file name. The rest of the path is not reparsed.</param>
    IFilePath WithExtension(string? newExtension, PathOptions options = PathOptions.NoUnfriendlyNames);

    #endregion
}
