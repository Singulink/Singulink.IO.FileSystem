using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Singulink.IO.Utilities
{
    internal static class SpanExtensions
    {
        public static Span<T> AsWritableSpan<T>(this ReadOnlySpan<T> readOnlySpan)
        {
            return MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(readOnlySpan), readOnlySpan.Length);
        }
    }
}
