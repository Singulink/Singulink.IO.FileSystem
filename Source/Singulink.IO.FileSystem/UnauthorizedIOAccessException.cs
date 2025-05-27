using System;
using System.IO;
using System.Runtime.Serialization;

namespace Singulink.IO;

/// <summary>
/// The exception that is thrown when the operating system denies access because of an I/O error.
/// </summary>
[Serializable]
public class UnauthorizedIOAccessException : IOException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnauthorizedIOAccessException"/> class.
    /// </summary>
    public UnauthorizedIOAccessException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnauthorizedIOAccessException"/> class.
    /// </summary>
    public UnauthorizedIOAccessException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnauthorizedIOAccessException"/> class.
    /// </summary>
    public UnauthorizedIOAccessException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnauthorizedIOAccessException"/> class.
    /// </summary>
    public UnauthorizedIOAccessException(string message, int hresult) : base(message, hresult)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnauthorizedIOAccessException"/> class.
    /// </summary>
    protected UnauthorizedIOAccessException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}