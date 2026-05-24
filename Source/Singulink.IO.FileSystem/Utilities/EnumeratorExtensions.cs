using System.Collections;

namespace Singulink.IO.Utilities;

internal static class EnumeratorExtensions
{
    public static EnumerableEnumerator<TEnumerator> AsEnumerable<TEnumerator>(this TEnumerator enumerator) where TEnumerator : IEnumerator
    {
        return new EnumerableEnumerator<TEnumerator>(enumerator);
    }
}

internal readonly ref struct EnumerableEnumerator<TEnumerator>(TEnumerator enumerator)
#if NET9_0_OR_GREATER
    where TEnumerator : allows ref struct
#endif
{
    private readonly TEnumerator _enumerator = enumerator;

    public TEnumerator GetEnumerator() => _enumerator;
}
