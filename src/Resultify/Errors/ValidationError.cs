namespace Resultify.Errors;

/// <summary>
/// Represents a validation failure, optionally scoped to a specific property.
/// Code defaults to <c>"Validation.Invalid"</c>, or <c>"Validation.{PropertyName}"</c> when a property is supplied.
/// </summary>
public sealed record ValidationError : Error
{
    /// <summary>The name of the property that failed validation, if applicable.</summary>
    public string? PropertyName { get; }

    /// <summary>Create a validation error not tied to a specific property.</summary>
    /// <param name="message">A human-readable description of the validation failure.</param>
    public ValidationError(string message)
        : base("Validation.Invalid", message) { }

    /// <summary>Create a validation error scoped to a specific property.</summary>
    /// <param name="propertyName">The name of the property that failed validation.</param>
    /// <param name="message">A human-readable description of the validation failure.</param>
    public ValidationError(string propertyName, string message)
        : base($"Validation.{propertyName}", message)
    {
        PropertyName = propertyName;
    }
}