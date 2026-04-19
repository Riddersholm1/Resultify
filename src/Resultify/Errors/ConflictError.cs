namespace Resultify.Errors;

/// <summary>
/// Indicates a concurrency or state conflict (e.g. optimistic concurrency violation).
/// </summary>
public sealed record ConflictError : Error
{
    public ConflictError(string message) : base("Conflict", message) { }
    public ConflictError(string code, string message) : base(code, message) { }
}
