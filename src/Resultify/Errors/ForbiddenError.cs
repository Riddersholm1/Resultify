namespace Resultify.Errors;

/// <summary>
/// Indicates the operation was understood but refused due to insufficient permissions.
/// </summary>
public sealed record ForbiddenError : Error
{
    public ForbiddenError(string message) : base("Forbidden", message) { }
    public ForbiddenError(string code, string message) : base(code, message) { }
}
