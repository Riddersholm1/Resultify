namespace Resultify.Errors;

/// <summary>
/// Represents a validation failure, optionally scoped to a specific property.
/// </summary>
public sealed record ValidationError : Error
{
    /// <summary>The name of the property that failed validation, if applicable.</summary>
    public string? PropertyName { get; }

    public ValidationError(string message)
        : base("Validation.Invalid", message) { }

    public ValidationError(string propertyName, string message)
        : base($"Validation.{propertyName}", message)
    {
        PropertyName = propertyName;
    }
}
