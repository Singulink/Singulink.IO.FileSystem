using System;
using System.IO;
using System.Reflection;

namespace Singulink.IO
{
    /// <summary>
    /// Contains methods for parsing file paths and working with special files.
    /// </summary>
    public static class FilePath
    {
        #region File Parsing

        /// <summary>
        /// Parses an absolute or relative file path using the specified options and the current platform's format.
        /// </summary>
        /// <param name="path">A file path.</param>
        /// <param name="options">Specifies the path parsing options.</param>
        public static IFilePath Parse(ReadOnlySpan<char> path, PathOptions options = PathOptions.NoUnfriendlyNames)
        {
            return Parse(path, PathFormat.Current, options);
        }

        /// <summary>
        /// Parses an absolute or relative file path using the specified format and options.
        /// </summary>
        /// <param name="path">A file path.</param>
        /// <param name="format">The path's format.</param>
        /// <param name="options">Specifies the path parsing options.</param>
        public static IFilePath Parse(ReadOnlySpan<char> path, PathFormat format, PathOptions options = PathOptions.NoUnfriendlyNames)
        {
            if (format.GetPathKind(path) == PathKind.Absolute)
                return ParseAbsolute(path, format, options);

            return ParseRelative(path, format, options);
        }

        #endregion

        #region Absolute File Parsing

        /// <summary>
        /// Parses an absolute file path using the specified options and the current platform's format.
        /// </summary>
        /// <param name="path">An absolute file path.</param>
        /// <param name="options">Specifies the path parsing options.</param>
        public static IAbsoluteFilePath ParseAbsolute(ReadOnlySpan<char> path, PathOptions options = PathOptions.NoUnfriendlyNames)
        {
            return ParseAbsolute(path, PathFormat.Current, options);
        }

        /// <summary>
        /// Parses an absolute file path using the specified format and options.
        /// </summary>
        /// <param name="path">An absolute file path.</param>
        /// <param name="format">The path's format.</param>
        /// <param name="options">Specifies the path parsing options.</param>
        public static IAbsoluteFilePath ParseAbsolute(ReadOnlySpan<char> path, PathFormat format, PathOptions options = PathOptions.NoUnfriendlyNames)
        {
            path = format.NormalizeSeparators(path);
            string finalPath = format.NormalizeAbsolutePath(path, options, true, out int rootLength);

            if (path.EndsWith(format.SeparatorString, StringComparison.Ordinal) || rootLength == finalPath.Length)
                throw new ArgumentException("No file name in path.", nameof(path));

            return new IAbsoluteFilePath.Impl(finalPath, rootLength, format);
        }

        #endregion

        #region Relative Parsing

        /// <summary>
        /// Parses a relative file path using the specified options and the current platform's format.
        /// </summary>
        /// <param name="path">A relative file path.</param>
        /// <param name="options">Specifies the path parsing options.</param>
        public static IRelativeFilePath ParseRelative(ReadOnlySpan<char> path, PathOptions options = PathOptions.NoUnfriendlyNames)
        {
            return ParseRelative(path, PathFormat.Current, options);
        }

        /// <summary>
        /// Parses a relative file path using the specified format and options.
        /// </summary>
        /// <param name="path">A relative file path.</param>
        /// <param name="format">The path's format.</param>
        /// <param name="options">Specifies the path parsing options.</param>
        public static IRelativeFilePath ParseRelative(ReadOnlySpan<char> path, PathFormat format, PathOptions options = PathOptions.NoUnfriendlyNames)
        {
            path = format.NormalizeSeparators(path);

            if (path.Length == 0 || path.EndsWith(format.SeparatorString, StringComparison.Ordinal))
                throw new ArgumentException("No file name in path.", nameof(path));

            string finalPath = format.NormalizeRelativePath(path, options, true, out int rootLength);
            return new IRelativeFilePath.Impl(finalPath, rootLength, format);
        }

        #endregion

        #region Special Files

        /// <summary>
        /// Gets the file path to the specified assembly.
        /// </summary>
        public static IAbsoluteFilePath GetAssemblyLocation(Assembly assembly)
        {
            string location = assembly.Location;

            if (string.IsNullOrEmpty(location))
                throw new InvalidOperationException("Assembly does not have a location.");

            return ParseAbsolute(location, PathOptions.None);
        }

        /// <summary>
        /// Creates a new uniquely named zero-byte temporary file.
        /// </summary>
        /// <returns>The path to the newly created file.</returns>
        public static IAbsoluteFilePath CreateTempFile() => ParseAbsolute(Path.GetTempFileName(), PathOptions.None);

        #endregion
    }
}
