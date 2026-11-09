using System.ComponentModel;
using System.Text.Json.Serialization;
using Singulink.IO.Converters;
using Singulink.IO.Json;

namespace Singulink.IO;

/// <summary>
/// Represents an absolute or relative path to a file.
/// </summary>
[TypeConverter(typeof(FilePathConverter))]
[JsonConverter(typeof(FilePathJsonConverter))]
public interface IFilePath : IPath
{
    /// <summary>
    /// Gets the file name without the last extension.
    /// </summary>
    /// <remarks>
    /// The dot in the extension is also removed from the name. File names with no extension are returned without changes. File names with trailing dots will
    /// have the dot removed. Files with multiple extensions will have only the last extension removed, e.g. <c>"archive.tar.gz"</c> becomes
    /// <c>"archive.tar"</c>.
    /// </remarks>
    sealed string NameWithoutExtension => PathFormat.GetFileNameWithoutExtension(PathDisplay);

    /// <summary>
    /// Gets the last extension of the file name including the leading dot, otherwise an empty string.
    /// </summary>
    /// <remarks>
    /// A file name that ends with a dot has the extension <c>"."</c>.
    /// </remarks>
    sealed string Extension => PathFormat.GetFileNameExtension(PathDisplay);

    /// <summary>
    /// Determines whether the last extension of the file name is the specified single extension. Case is ignored on every platform.
    /// </summary>
    /// <param name="extension">The single extension, which starts with a dot '.' character and has no other dots (e.g. <c>".gz"</c>). A lone dot is
    /// accepted.</param>
    /// <returns><see langword="true"/> if <see cref="Extension"/> equals the extension, ignoring case, otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentException">The extension is empty, isn't shaped as described for <paramref name="extension"/> or has a character that names
    /// can't have in the path format.</exception>
    /// <remarks>
    /// <see cref="Extension"/> is an empty string when the file name has no extension.
    /// </remarks>
    sealed bool HasExtension(string extension) => HasExtensionCore(extension, allowMulti: false);

    /// <summary>
    /// Determines whether the file name ends with the specified multi-extension, which has one or more parts (e.g. <c>".gz"</c> or <c>".tar.gz"</c>).
    /// Case is ignored on every platform.
    /// </summary>
    /// <param name="extension">The multi-extension, starting with a dot '.' character (e.g. <c>".gz"</c> or <c>".tar.gz"</c>). Every dot must be followed
    /// by a character other than a dot, except in an extension that is a lone dot.</param>
    /// <returns><see langword="true"/> if the file name ends with the extension, otherwise <see langword="false"/>. A file name that is the extension,
    /// such as <c>".gitignore"</c> with the extension <c>".gitignore"</c>, ends with it.</returns>
    /// <exception cref="ArgumentException">The extension is empty, isn't shaped as described for <paramref name="extension"/> or has a character that names
    /// can't have in the path format.</exception>
    /// <remarks>
    /// <see cref="Extension"/> is an empty string when the file name has no extension.
    /// </remarks>
    sealed bool HasMultiExtension(string extension) => HasExtensionCore(extension, allowMulti: true);

    /// <summary>
    /// Gets the longest of the known multi-extensions, which have one or more parts (e.g. <c>".gz"</c> or <c>".tar.gz"</c>), that the file name ends with,
    /// otherwise <see cref="Extension"/>. Case is ignored on every platform.
    /// </summary>
    /// <param name="knownExtensions">The known multi-extensions. Each one is shaped as described for the extension of <see
    /// cref="HasMultiExtension(string)"/>.</param>
    /// <returns>The longest matching known extension as it was passed in (the first one if several match with the same length), otherwise <see
    /// cref="Extension"/>, which is an empty string if the file name has no extension.</returns>
    /// <exception cref="ArgumentException">A known extension is null or empty, isn't shaped as described for <see cref="HasMultiExtension(string)"/> or has
    /// a character that names can't have in the path format.</exception>
    /// <remarks>
    /// <para>For example, with <c>".tar.gz"</c> and <c>".tar.bz2"</c>, <c>"backup.tar.gz"</c> gives <c>".tar.gz"</c> and <c>"photo.jpg"</c> gives
    /// <c>".jpg"</c>.</para>
    /// <para>The result can be passed to <see cref="RemoveMultiExtension(string, PathOptions)"/>, which leaves a file name without an extension
    /// unchanged.</para>
    /// </remarks>
    sealed string GetMultiExtension(params ReadOnlySpan<string> knownExtensions)
    {
        string name = Name;
        string? match = null;

        foreach (string? extension in knownExtensions)
        {
            if (extension is null)
                throw new ArgumentException("Known extensions can't contain null.", nameof(knownExtensions));

            PathFormat.ValidateExtension(extension, allowMulti: true, checkParts: true, nameof(knownExtensions));

            if (extension.Length > (match?.Length ?? 0) && name.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                match = extension;
        }

        return match ?? Extension;
    }

    private bool HasExtensionCore(string extension, bool allowMulti)
    {
        PathFormat.ValidateExtension(extension, allowMulti, checkParts: true, nameof(extension));
        return Name.EndsWith(extension, StringComparison.OrdinalIgnoreCase);
    }

    #region Path Manipulation

    /// <summary>
    /// Changes the last extension of the file name to a new single extension.
    /// </summary>
    /// <param name="extension">The new single extension that should be applied to the file. Must either be null/empty or start with a dot '.' character and
    /// contain no additional dots (e.g. <c>".txt"</c>).</param>
    /// <param name="options">The options to apply when parsing the new file name. The rest of the path is not reparsed.</param>
    /// <remarks>
    /// Only the last extension of the file name is replaced, preserving any previous extensions before it, e.g. <c>"archive.tar.gz"</c> with extension
    /// <c>".zip"</c> becomes <c>"archive.tar.zip"</c>. If the file name has multiple extensions and the new extension is null/empty, only the last extension is
    /// removed, e.g. <c>"archive.tar.gz"</c> becomes <c>"archive.tar"</c>.
    /// </remarks>
    IFilePath WithExtension(string? extension, PathOptions options = PathOptions.NoUnfriendlyNames);

    /// <summary>
    /// Adds a single extension to the file name, preserving any existing extensions before it.
    /// </summary>
    /// <param name="extension">The new single extension that should be applied to the file. Must either be null/empty or start with a dot '.' character and
    /// contain no additional dots (e.g. <c>".txt"</c>).</param>
    /// <param name="options">The options to apply when parsing the new file name. The rest of the path is not reparsed.</param>
    /// <remarks>
    /// The extension becomes the last extension of the new file name, so <see cref="Extension"/> returns it. A null or empty extension leaves the path
    /// unchanged.
    /// </remarks>
    IFilePath AddExtension(string? extension, PathOptions options = PathOptions.NoUnfriendlyNames);

    /// <summary>
    /// Adds a multi-extension, which has one or more parts (e.g. <c>".gz"</c> or <c>".tar.gz"</c>), to the file name, preserving any existing extensions
    /// before it.
    /// </summary>
    /// <param name="extension">The new multi-extension that should be applied to the file. Must either be null/empty or start with a dot '.' character, and
    /// every dot must be followed by a character other than a dot, except in an extension that is a lone dot.</param>
    /// <param name="options">The options to apply when parsing the new file name. The rest of the path is not reparsed.</param>
    /// <remarks>
    /// The new file name ends with the extension, so <see cref="HasMultiExtension(string)"/> returns <see langword="true"/> for it. A null or empty
    /// extension leaves the path unchanged.
    /// </remarks>
    IFilePath AddMultiExtension(string? extension, PathOptions options = PathOptions.NoUnfriendlyNames);

    /// <summary>
    /// Removes the specified single extension, which is the last extension of the file name. Case is ignored on every platform.
    /// </summary>
    /// <param name="extension">The single extension to remove, shaped as described for <see cref="HasExtension(string)"/>. An empty extension leaves the
    /// path unchanged.</param>
    /// <param name="options">The options to apply when parsing the new file name. The rest of the path is not reparsed.</param>
    /// <exception cref="ArgumentException">The extension isn't shaped as described for <see cref="HasExtension(string)"/> or has a character that names
    /// can't have in the path format, the file name doesn't end with it, it is the whole file name, or the remaining file name is invalid for <paramref
    /// name="options"/> or is <c>.</c> or <c>..</c>.</exception>
    IFilePath RemoveExtension(string extension, PathOptions options = PathOptions.NoUnfriendlyNames);

    /// <summary>
    /// Removes the specified multi-extension, which has one or more parts (e.g. <c>".gz"</c> or <c>".tar.gz"</c>), from the end of the file name. Case is
    /// ignored on every platform.
    /// </summary>
    /// <param name="extension">The multi-extension to remove, shaped as described for <see cref="HasMultiExtension(string)"/>. An empty extension leaves the
    /// path unchanged.</param>
    /// <param name="options">The options to apply when parsing the new file name. The rest of the path is not reparsed.</param>
    /// <exception cref="ArgumentException">The extension isn't shaped as described for <see cref="HasMultiExtension(string)"/> or has a character that
    /// names can't have in the path format, the file name doesn't end with it, it is the whole file name, or the remaining file name is invalid for
    /// <paramref name="options"/> or is <c>.</c> or <c>..</c>.</exception>
    IFilePath RemoveMultiExtension(string extension, PathOptions options = PathOptions.NoUnfriendlyNames);

    #endregion
}
