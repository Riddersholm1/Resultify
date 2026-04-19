namespace Resultify.Errors;

/// <summary>
/// Indicates the operation was understood but refused due to insufficient permissions.
/// Maps naturally to HTTP 403. Code defaults to <c>"Forbidden"</c>.
/// </summary>
public sealed record ForbiddenError : Error
{
    /// <summary>Create a forbidden error with the default code <c>"Forbidden"</c>.</summary>
    /// <param name="message">A human-readable description of why the action was refused.</param>
    public ForbiddenError(string message) : base("Forbidden", message) { }

    /// <summary>Create a forbidden error with an explicit code.</summary>
    /// <param name="code">A stable, machine-readable identifier.</param>
    /// <param name="message">A human-readable description of why the action was refused.</param>
    public ForbiddenError(string code, string message) : base(code, message) { }
}