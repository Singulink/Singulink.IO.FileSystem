using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Singulink.IO;

/// <summary>
/// Specifies that a path property, field or parameter must be valid for the specified path options, as determined by <see
/// cref="IPath.IsValidFor(PathOptions, out string?)"/>.
/// </summary>
/// <remarks>
/// <para>The attribute checks <see cref="IPath"/> values. A <see langword="null"/> value is valid; <see cref="RequiredAttribute"/> is what requires a value. A
/// value of any other type, including a <see cref="string"/>, is invalid and isn't parsed.</para>
/// <para>The default error message is "The {0} field is not a valid path. {1}", where <c>{0}</c> is the display name of the member and <c>{1}</c> is the
/// reason: the error message that parsing the path with the options gives (e.g. "Entry name ends with a space."), or for a value that isn't a path, its type
/// (e.g. "Value of type String is not a path."). <c>{1}</c> is empty in <see cref="FormatErrorMessage(string)"/>, which has no value to give a reason for. <see
/// cref="ValidationAttribute.ErrorMessage"/>, or <see cref="ValidationAttribute.ErrorMessageResourceType"/> and <see
/// cref="ValidationAttribute.ErrorMessageResourceName"/>, replace the message and can use the same placeholders.</para>
/// </remarks>
/// <param name="options">The path options the path must be valid for.</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class ValidPathAttribute(PathOptions options) : ValidationAttribute("The {0} field is not a valid path. {1}")
{
    /// <summary>
    /// Gets the path options the path must be valid for.
    /// </summary>
    public PathOptions Options { get; } = options;

    /// <inheritdoc/>
    public override bool IsValid(object? value) => value switch {
        null => true,
        IPath path => path.IsValidFor(Options),
        _ => false,
    };

    /// <inheritdoc/>
    public override string FormatErrorMessage(string name) => Format(name, string.Empty);

    /// <inheritdoc/>
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        string? reason = value switch {
            null => null,
            IPath path => path.IsValidFor(Options, out string? error) ? null : error,
            _ => $"Value of type {value.GetType().Name} is not a path.",
        };

        if (reason is null)
            return ValidationResult.Success;

        string[]? memberNames = validationContext.MemberName is { } memberName ? [memberName] : null;
        return new ValidationResult(Format(validationContext.DisplayName, reason), memberNames);
    }

    private string Format(string name, string reason)
    {
        string message = string.Format(CultureInfo.CurrentCulture, ErrorMessageString, name, reason);
        return reason.Length is 0 ? message.TrimEnd() : message;
    }
}
