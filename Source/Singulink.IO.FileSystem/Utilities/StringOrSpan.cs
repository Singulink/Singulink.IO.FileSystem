using System;
using System.Collections.Generic;
using System.Text;

namespace Singulink.IO.Utilities
{
    internal ref struct StringOrSpan
    {
        public static StringOrSpan Empty => new StringOrSpan(string.Empty);

        private readonly ReadOnlySpan<char> _span;
        private string? _string;

        public ReadOnlySpan<char> Span => _span;

        public string String => _string ??= _span.ToString();

        public int Length => Span.Length;

        public StringOrSpan(string value)
        {
            _string = value;
            _span = value;
        }

        public StringOrSpan(ReadOnlySpan<char> value)
        {
            _string = null;
            _span = value;
        }

        public static implicit operator string(StringOrSpan value) => value.String;

        public static implicit operator ReadOnlySpan<char>(StringOrSpan value) => value.Span;

        public static implicit operator StringOrSpan(string value) => new StringOrSpan(value);

        public static implicit operator StringOrSpan(ReadOnlySpan<char> value) => new StringOrSpan(value);

        public unsafe StringOrSpan Replace(char oldChar, char newChar)
        {
            if (_string == null && Span.IndexOf(oldChar) < 0)
                return this;

            return String.Replace(oldChar, newChar);
        }

        public override string ToString() => String;
    }
}