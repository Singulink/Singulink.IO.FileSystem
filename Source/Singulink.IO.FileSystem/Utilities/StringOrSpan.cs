namespace Singulink.IO.Utilities;

internal ref struct StringOrSpan : ISpanFormattable
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

    public StringOrSpan Replace(char oldChar, char newChar)
    {
        if (_string is null && Span.IndexOf(oldChar) < 0)
            return this;

        return String.Replace(oldChar, newChar);
    }

    public override string ToString() => String;

    public string ToString(string? format, IFormatProvider? formatProvider) => String;

    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (destination.Length < Span.Length)
        {
            charsWritten = 0;
            return false;
        }

        Span.CopyTo(destination);
        charsWritten = Span.Length;
        return true;
    }
}
