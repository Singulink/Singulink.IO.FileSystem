using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Singulink.IO
{
    /// <summary>
    /// Provides options that control search behavior in directories.
    /// </summary>
    public class SearchOptions
    {
        internal static readonly SearchOptions Default = new SearchOptions();

        /// <summary>
        /// Gets or sets the attributes that will cause entries to be skipped. Default is none.
        /// </summary>
        public FileAttributes AttributesToSkip { get; set; }

        /// <summary>
        /// Gets or sets the suggested buffer size, in bytes. Default value is 0 (no suggestion).
        /// </summary>
        public int BufferSize { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the search is case sensitive. Default is case insensitive.
        /// </summary>
        public MatchCasing MatchCasing { get; set; } = MatchCasing.CaseInsensitive;

        /// <summary>
        /// Gets or sets a value indicating whether the search is recursive, i.e. continues into child directories. Default is false.
        /// </summary>
        public bool Recursive { get; set; }

        internal EnumerationOptions ToEnumerationOptions() => new EnumerationOptions()
        {
            AttributesToSkip = AttributesToSkip,
            MatchCasing = MatchCasing,
            BufferSize = BufferSize,
            RecurseSubdirectories = Recursive,

            // Inaccessible defaults:
            // MatchType = MatchType.Simple,
            // ReturnSpecialDirectories = false,
            // IgnoreInaccessible = true,
        };
    }
}
