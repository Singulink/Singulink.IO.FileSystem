using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Singulink.IO.Utilities
{
    internal ref struct ValueStringBuilder
    {
        private readonly string _value;
        private Span<char> _valueSpan;

        public ValueStringBuilder(int length)
        {
            _value = new string('\0', length);
            _valueSpan = _value.AsSpan().AsWritableSpan();
        }

        public string Value {
            get {
                if (_valueSpan.Length != 0)
                    throw new InvalidOperationException("String building is incomplete.");

                return _value;
            }
        }

        public void Append(ReadOnlySpan<char> value)
        {
            value.CopyTo(_valueSpan);
            _valueSpan = _valueSpan.Slice(value.Length);
        }

        #region Static Builder Methods

        public static string Concat(ReadOnlySpan<char> s1, ReadOnlySpan<char> s2)
        {
            var builder = new ValueStringBuilder(s1.Length + s2.Length);
            builder.Append(s1);
            builder.Append(s2);

            return builder.Value;
        }

        public static string Concat(ReadOnlySpan<char> s1, ReadOnlySpan<char> s2, ReadOnlySpan<char> s3)
        {
            var builder = new ValueStringBuilder(s1.Length + s2.Length + s3.Length);
            builder.Append(s1);
            builder.Append(s2);
            builder.Append(s3);

            return builder.Value;
        }

        public static string Concat(ReadOnlySpan<char> s1, ReadOnlySpan<char> s2, ReadOnlySpan<char> s3, ReadOnlySpan<char> s4)
        {
            var builder = new ValueStringBuilder(s1.Length + s2.Length + s3.Length + s4.Length);
            builder.Append(s1);
            builder.Append(s2);
            builder.Append(s3);
            builder.Append(s4);

            return builder.Value;
        }

        public static string Concat(ReadOnlySpan<char> s1, ReadOnlySpan<char> s2, ReadOnlySpan<char> s3, ReadOnlySpan<char> s4, ReadOnlySpan<char> s5)
        {
            var builder = new ValueStringBuilder(s1.Length + s2.Length + s3.Length + s4.Length + s5.Length);
            builder.Append(s1);
            builder.Append(s2);
            builder.Append(s3);
            builder.Append(s4);
            builder.Append(s5);

            return builder.Value;
        }

        #endregion
    }
}
