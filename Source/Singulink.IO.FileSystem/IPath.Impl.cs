using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Singulink.IO;

/// <content>
/// Contains the implementation of IPath.
/// </content>
public partial interface IPath
{
    internal abstract class Impl(string pathDisplay, int rootLength, PathFormat pathFormat, int validity) : IPath
    {
        // The validity bits (see PathValidity) known about the path, starting with those known when it is created. Not part of the path's value. Deliberately
        // not volatile: like the cached name, a race only computes the same value again.
        private int _validity = validity;

        public string PathDisplay { get; } = pathDisplay;

        public int RootLength { get; } = rootLength;

        public PathFormat PathFormat { get; } = pathFormat;

        public string Name => field ??= PathFormat.GetEntryName(PathDisplay, RootLength);

        public bool IsRooted => PathFormat.GetPathKind(PathDisplay) is not PathKind.Relative;

        public abstract bool HasParentDirectory { get; }

        public abstract IDirectoryPath? ParentDirectory { get; }

        /// <summary>
        /// Gets the validity bits a path derived from this one inherits for the names they share: the known bits without <see cref="PathValidity.Computed"/>.
        /// </summary>
        internal int InheritedValidity => _validity & ~PathValidity.Computed;

        /// <summary>
        /// Gets the validity bits currently known, for tests.
        /// </summary>
        internal int KnownValidity => _validity;

        public bool IsValidFor(PathOptions options)
        {
            int required = PathValidity.GetRequired(PathFormat, options, this is IAbsolutePath);
            int validity = _validity;

            if ((validity & required) == required)
                return true;

            if ((validity & PathValidity.Computed) is not 0)
                return false;

            validity = ComputeValidity();
            return (validity & required) == required;
        }

        public bool IsValidFor(PathOptions options, [NotNullWhen(false)] out string? errorMessage)
        {
            if (IsValidFor(options))
            {
                errorMessage = null;
                return true;
            }

            // Parse the path again to get the error that parsing gives.
            return TryParseAgain(options, out errorMessage);
        }

        /// <summary>
        /// Works out every validity bit in one pass over the names and stores them.
        /// </summary>
        internal int ComputeValidity()
        {
            int validity = PathFormat.GetValidity(PathDisplay, RootLength, this is IAbsolutePath, this is IFilePath) | PathValidity.Computed;
            Debug.Assert((InheritedValidity & ~validity) is 0, "The validity known at creation must be a subset of the computed validity.");

            _validity = validity;
            return validity;
        }

        /// <summary>
        /// Parses <see cref="PathDisplay"/> again, in <see cref="PathFormat"/> and as the same kind of path, with the specified options.
        /// </summary>
        private protected abstract bool TryParseAgain(PathOptions options, [NotNullWhen(false)] out string? error);

        public bool Equals(IPath? other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return GetType() == other.GetType() &&
                PathFormat == other.PathFormat &&
                PathDisplay.AsSpan(0, RootLength).Equals(other.PathDisplay.AsSpan(0, other.RootLength), StringComparison.OrdinalIgnoreCase) &&
                PathDisplay.AsSpan(RootLength).Equals(other.PathDisplay.AsSpan(other.RootLength), StringComparison.Ordinal);
        }

        public override bool Equals(object? obj) => Equals(obj as IPath);

        public override int GetHashCode() => HashCode.Combine(
            GetType(),
            PathFormat,
            string.GetHashCode(PathDisplay.AsSpan(0, RootLength), StringComparison.OrdinalIgnoreCase),
            string.GetHashCode(PathDisplay.AsSpan(RootLength)));

        public override string ToString() => @$"[{PathFormat}] ""{PathDisplay}""";
    }
}
