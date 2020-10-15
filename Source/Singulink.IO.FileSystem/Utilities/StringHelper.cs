using System;

namespace Singulink.IO.Utilities
{
    internal static class StringHelper
    {
        public static unsafe string Concat(ReadOnlySpan<char> s1, ReadOnlySpan<char> s2)
        {
            fixed (char* p1 = s1)
            fixed (char* p2 = s2) {
                IntPtr* buffer = stackalloc IntPtr[]
                {
                    (IntPtr)s1.Length,
                    (IntPtr)s2.Length,
                    (IntPtr)p1,
                    (IntPtr)p2,
                };

                return string.Create(s1.Length + s2.Length, (IntPtr)buffer, (span, state) =>
                {
                    int length1 = (int)((IntPtr*)state)[0];
                    int length2 = (int)((IntPtr*)state)[1];

                    char* buffer1 = (char*)((IntPtr*)state)[2];
                    char* buffer2 = (char*)((IntPtr*)state)[3];

                    new ReadOnlySpan<char>(buffer1, length1).CopyTo(span);
                    new ReadOnlySpan<char>(buffer2, length2).CopyTo(span.Slice(length1));
                });
            }
        }

        public static unsafe string Concat(ReadOnlySpan<char> s1, ReadOnlySpan<char> s2, ReadOnlySpan<char> s3)
        {
            fixed (char* p1 = s1)
            fixed (char* p2 = s2)
            fixed (char* p3 = s3) {
                IntPtr* buffer = stackalloc IntPtr[] {
                    (IntPtr)s1.Length,
                    (IntPtr)s2.Length,
                    (IntPtr)s3.Length,
                    (IntPtr)p1,
                    (IntPtr)p2,
                    (IntPtr)p3,
                };

                return string.Create(s1.Length + s2.Length + s3.Length, (IntPtr)buffer, (span, state) => {
                    int length1 = (int)((IntPtr*)state)[0];
                    int length2 = (int)((IntPtr*)state)[1];
                    int length3 = (int)((IntPtr*)state)[2];

                    char* buffer1 = (char*)((IntPtr*)state)[3];
                    char* buffer2 = (char*)((IntPtr*)state)[4];
                    char* buffer3 = (char*)((IntPtr*)state)[5];

                    new ReadOnlySpan<char>(buffer1, length1).CopyTo(span);
                    new ReadOnlySpan<char>(buffer2, length2).CopyTo(span = span.Slice(length1));
                    new ReadOnlySpan<char>(buffer3, length3).CopyTo(span.Slice(length2));
                });
            }
        }
    }
}