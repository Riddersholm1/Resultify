namespace Resultify.Errors;

/// <summary>
/// Indicates a concurrency or state conflict, e.g. a duplicate resource or optimistic concurrency violation (HTTP 409).
/// The code defaults to <c>"Conflict"</c> when only a message is supplied.
/// </summary>
public sealed record ConflictError : Error
{
    /// <summary>Create a conflict error with the default code <c>"Conflict"</c>.</summary>
    /// <param name="message">A human-readable description of the conflict.</param>
    public ConflictError(string message)
        : base("Conflict", message) { }

    /// <summary>Create a conflict error with an explicit code.</summary>
    /// <param name="code">A stable, machine-readable identifier for the conflict.</param>
    /// <param name="message">A human-readable description of the conflict.</param>
    public ConflictError(string code, string message)
        : base(code, message) { }
}