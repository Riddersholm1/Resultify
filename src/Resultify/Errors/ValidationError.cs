namespace Resultify.Errors;

/// <summary>
/// A validation failure, optionally scoped to a specific property.
/// The code defaults to <c>"Validation.Invalid"</c>, or <c>"Validation.{PropertyName}"</c> when a property is supplied.
/// </summary>
public sealed record ValidationError : Error
{
    /// <summary>The name of the property that failed validation, if applicable.</summary>
    public string? PropertyName { get; }

    /// <summary>Create a validation error not tied to a specific property.</summary>
    /// <param name="message">A human-readable description of the validation failure.</param>
    /// /// <exception cref="ArgumentNullException">Thrown when <paramref name="message"/> is null.</exception>
    public ValidationError(string message)
        : base("Validation.Invalid", message) { }

    /// <summary>Create a validation error scoped to a specific property.</summary>
    /// <param name="propertyName">The name of the property that failed validation.</param>
    /// <param name="message">A human-readable description of the validation failure.</param>
    /// /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> or <paramref name="message"/> is null.</exception>
    public ValidationError(string propertyName, string message)
        : base(BuildCode(propertyName), message)
    {
        PropertyName = propertyName;
    }

    /// <summary>
    /// Create a validation error for a property with the default message <c>"'{propertyName}' is invalid."</c>.
    /// </summary>
    /// <param name="propertyName">The name of the property that failed validation.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is null.</exception>
    public static ValidationError ForProperty(string propertyName)
    {
        ArgumentNullException.ThrowIfNull(propertyName);
        return new ValidationError(propertyName, $"'{propertyName}' is invalid.");
    }

    private static string BuildCode(string propertyName)
    {
        ArgumentNullException.ThrowIfNull(propertyName);
        return $"Validation.{propertyName}";
    }
}