using Singulink.Enums;

namespace Singulink.IO;

#pragma warning disable SA1513 // Closing brace should be followed by blank line

/// <summary>
/// Provides options that control search behavior in directories.
/// </summary>
public class SearchOptions
{
    private static readonly EnumerationOptions DefaultEnumerationOptions = ToEnumerationOptions(new(), out _);

    /// <summary>
    /// Gets or sets the attributes that will cause entries to be skipped. Default is <see cref="FileAttributes.None"/>.
    /// </summary>
    public FileAttributes AttributesToSkip {
        get;
        set {
            value.ThrowIfFlagsAreNotDefined(nameof(value));
            field = value;
        }
    }

    /// <summary>
    /// Gets or sets the suggested buffer size in bytes. Default value is <c>0</c>, indicating no suggestion.
    /// </summary>
    public int BufferSize { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the search is case-sensitive. Default is <see cref="MatchCasing.CaseInsensitive"/>.
    /// </summary>
    public MatchCasing MatchCasing {
        get;
        set {
            value.ThrowIfNotDefined(nameof(value));
            field = value;
        }
    } = MatchCasing.CaseInsensitive;

    /// <summary>
    /// Gets or sets the maximum directory recursion depth for recursive searches. Default is <see cref="int.MaxValue"/>.
    /// </summary>
    public int MaxRecursionDepth {
        get;
        set {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            field = value;
        }
    } = int.MaxValue;

    /// <summary>
    /// Gets or sets a value indicating whether the search is recursive, i.e. continues into child directories. Default is <see langword="false"/>.
    /// </summary>
    public bool Recursive { get; set; }

    /// <summary>
    /// Gets or sets the behavior when encountering inaccessible directories during searches. Defaults to <see
    /// cref="InaccessibleSearchBehavior.ThrowForSearchDir"/>.
    /// </summary>
    public InaccessibleSearchBehavior InaccessibleSearchBehavior {
        get;
        set {
            value.ThrowIfNotDefined(nameof(value));
            field = value;
        }
    }

    internal static EnumerationOptions ToEnumerationOptions(SearchOptions? searchOptions, out bool requiresExtraAccessCheck)
    {
        if (searchOptions is null) {
            requiresExtraAccessCheck = false;
            return DefaultEnumerationOptions;
        }

        requiresExtraAccessCheck = searchOptions.InaccessibleSearchBehavior is InaccessibleSearchBehavior.ThrowForSearchDir && searchOptions.Recursive;

        return new() {
            AttributesToSkip = searchOptions.AttributesToSkip,
            MatchCasing = searchOptions.MatchCasing,
            BufferSize = searchOptions.BufferSize,
            RecurseSubdirectories = searchOptions.Recursive,
            IgnoreInaccessible = searchOptions.InaccessibleSearchBehavior switch {
                InaccessibleSearchBehavior.IgnoreAll => true,
                InaccessibleSearchBehavior.ThrowForAll => false,
                InaccessibleSearchBehavior.ThrowForSearchDir => searchOptions.Recursive,
                _ => throw new ArgumentException("Invalid inaccessible search behavior.", nameof(searchOptions)),
            },

            MaxRecursionDepth = searchOptions.MaxRecursionDepth,

            // Can't be changed:
            MatchType = MatchType.Simple,
            ReturnSpecialDirectories = false,
        };
    }
}
