using System;

namespace Singulink.IO
{
    /// <content>
    /// Contains an implementation of IPath.
    /// </content>
    public partial interface IPath
    {
        internal abstract class Impl : IPath
        {
            protected Impl(string pathDisplay, int rootLength, PathFormat pathFormat)
            {
                PathDisplay = pathDisplay;
                RootLength = rootLength;
                PathFormat = pathFormat;
            }

            public string PathDisplay { get; }

            public int RootLength { get; }

            public PathFormat PathFormat { get; }

            public string Name => PathFormat.GetEntryName(PathDisplay, RootLength);

            public bool IsRooted => PathFormat.GetPathKind(PathDisplay) != PathKind.Relative;

            int IPath.RootLength => RootLength;

            #region Equality and String Formatting

            public bool Equals(IPath? other)
            {
                if (other == null)
                    return false;

                return (this is IFilePath) == (other is IFilePath) &&
                    PathFormat == other.PathFormat &&
                    PathDisplay.AsSpan(0, RootLength).Equals(PathDisplay.AsSpan(0, other.RootLength), StringComparison.OrdinalIgnoreCase) &&
                    PathDisplay.AsSpan(RootLength).Equals(PathDisplay.AsSpan(other.RootLength), StringComparison.Ordinal);
            }

            public override bool Equals(object? obj) => Equals(obj as Impl);

            // TODO: Combine case-insensitive root with case-sensitive remainder hash codes to avoid hash collisions with different case paths when new
            // ReadOnlySpan<char> StringComparer APIs become available: https://github.com/dotnet/runtime/issues/27229
            public override int GetHashCode() => PathDisplay.GetHashCode(StringComparison.OrdinalIgnoreCase);

            #endregion

            #region String Formatting

            public override string ToString()
            {
                // Intentionally thwart users from using ToString() to get a usable path, rather force them to consider whether PathExport or PathDisplay is
                // more suitable.
                return (this is IFilePath ? "[File] " : "[Directory] ") + PathDisplay;
            }

            #endregion
        }
    }
}
