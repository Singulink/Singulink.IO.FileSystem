using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Singulink.IO
{
    /// <summary>
    /// Contains methods for parsing directory paths and working with special directories.
    /// </summary>
    public static class DirectoryPath
    {
        #region Directory Parsing

        /// <summary>
        /// Parses an absolute or relative directory path using the specified options and the current platform's format.
        /// </summary>
        /// <param name="path">A directory path.</param>
        /// <param name="options">Specifies the path parsing options.</param>
        public static IDirectoryPath Parse(ReadOnlySpan<char> path, PathOptions options = PathOptions.NoUnfriendlyNames)
        {
            return Parse(path, PathFormat.Current, options);
        }

        /// <summary>
        /// Parses an absolute or relative directory path using the specified format and options.
        /// </summary>
        /// <param name="path">A directory path.</param>
        /// <param name="format">The path's format.</param>
        /// <param name="options">Specifies the path parsing options.</param>
        public static IDirectoryPath Parse(ReadOnlySpan<char> path, PathFormat format, PathOptions options = PathOptions.NoUnfriendlyNames)
        {
            if (format.GetPathKind(path) == PathKind.Absolute)
                return ParseAbsolute(path, format, options);

            return ParseRelative(path, format, options);
        }

        #endregion

        #region Absolute Directory Parsing

        /// <summary>
        /// Parses an absolute directory path using the specified options and the current platform's format.
        /// </summary>
        /// <param name="path">An absolute directory path.</param>
        /// <param name="options">Specifies the path parsing options.</param>
        public static IAbsoluteDirectoryPath ParseAbsolute(ReadOnlySpan<char> path, PathOptions options = PathOptions.NoUnfriendlyNames)
        {
            return ParseAbsolute(path, PathFormat.Current, options);
        }

        /// <summary>
        /// Parses an absolute directory path using the specified format and options.
        /// </summary>
        /// <param name="path">An absolute directory path.</param>
        /// <param name="format">The path's format.</param>
        /// <param name="options">Specifies the path parsing options.</param>
        public static IAbsoluteDirectoryPath ParseAbsolute(ReadOnlySpan<char> path, PathFormat format, PathOptions options = PathOptions.NoUnfriendlyNames)
        {
            path = format.NormalizeSeparators(path);
            string finalPath = format.NormalizeAbsolutePath(path, options, out int rootLength);
            return new IAbsoluteDirectoryPath.Impl(finalPath, rootLength, format);
        }

        #endregion

        #region Relative Directory Parsing

        /// <summary>
        /// Parses a relative directory path using the specified options and the current platform's format.
        /// </summary>
        /// <param name="path">A relative directory path.</param>
        /// <param name="options">Specifies the path parsing options.</param>
        public static IRelativeDirectoryPath ParseRelative(ReadOnlySpan<char> path, PathOptions options = PathOptions.NoUnfriendlyNames)
        {
            return ParseRelative(path, PathFormat.Current, options);
        }

        /// <summary>
        /// Parses a relative directory path using the specified format and options.
        /// </summary>
        /// <param name="path">A relative directory path.</param>
        /// <param name="format">The path's format.</param>
        /// <param name="options">Specifies the path parsing options.</param>
        public static IRelativeDirectoryPath ParseRelative(ReadOnlySpan<char> path, PathFormat format, PathOptions options = PathOptions.NoUnfriendlyNames)
        {
            path = format.NormalizeSeparators(path);
            string finalPath = format.NormalizeRelativePath(path, options, out int rootLength);
            return new IRelativeDirectoryPath.Impl(finalPath, rootLength, format);
        }

        #endregion

        #region Special Directories

        /// <summary>
        /// Gets the directory path of the specified assembly.
        /// </summary>
        public static IAbsoluteDirectoryPath GetAssemblyLocation(Assembly assembly) => FilePath.GetAssemblyLocation(assembly).ParentDirectory;

        /// <summary>
        /// Gets the current working directory.
        /// </summary>
        public static IAbsoluteDirectoryPath GetCurrent() => ParseAbsolute(Directory.GetCurrentDirectory(), PathOptions.None);

        /// <summary>
        /// Sets the current working directory.
        /// </summary>
        public static void SetCurrent(IAbsoluteDirectoryPath dir)
        {
            dir.PathFormat.EnsureCurrent(nameof(dir));
            Directory.SetCurrentDirectory(dir.PathExport);
        }

        /// <summary>
        /// Returns the current user's temporary directory.
        /// </summary>
        public static IAbsoluteDirectoryPath GetTemp() => ParseAbsolute(Path.GetTempPath(), PathOptions.None);

        /// <summary>
        /// Returns the special system folder directory path that is identified by the provided enumeration.
        /// </summary>
        /// <param name="specialFolder">The special system folder to get.</param>
        public static IAbsoluteDirectoryPath GetSpecialFolder(Environment.SpecialFolder specialFolder)
        {
            return ParseAbsolute(Environment.GetFolderPath(specialFolder), PathOptions.None);
        }

        /// <summary>
        /// Gets the list of directory paths that represent mounting points (drives in Windows).
        /// </summary>
        public static IEnumerable<IAbsoluteDirectoryPath> GetMountingPoints()
        {
            foreach (string d in Environment.GetLogicalDrives())
                yield return ParseAbsolute(d, PathOptions.None);
        }

        #endregion
    }
}
