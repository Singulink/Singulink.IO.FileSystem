namespace Singulink.IO;

/// <content>
/// Contains the implementation of IPath.
/// </content>
public partial interface IPath
{
    internal abstract class Impl(string pathDisplay, int rootLength, PathFormat pathFormat) : IPath
    {
        public string PathDisplay { get; } = pathDisplay;

        public int RootLength { get; } = rootLength;

        public PathFormat PathFormat { get; } = pathFormat;

        public string Name => field ??= PathFormat.GetEntryName(PathDisplay, RootLength);

        public bool IsRooted => PathFormat.GetPathKind(PathDisplay) is not PathKind.Relative;

        public abstract bool HasParentDirectory { get; }

        public abstract IDirectoryPath? ParentDirectory { get; }

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
