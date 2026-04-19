namespace Resultify.Errors;

/// <summary>
/// Indicates that a requested entity was not found. Maps naturally to HTTP 404.
/// Code defaults to <c>"NotFound"</c>, or <c>"{EntityName}.NotFound"</c> when an entity and id are supplied.
/// </summary>
public sealed record NotFoundError : Error
{
    /// <summary>The type name of the entity that was not found, if supplied.</summary>
    public string? EntityName { get; }

    /// <summary>The identifier that was used in the lookup, if supplied.</summary>
    public object? EntityId { get; }

    /// <summary>Create a not-found error with the default code <c>"NotFound"</c>.</summary>
    /// <param name="message">A human-readable description of what was not found.</param>
    public NotFoundError(string message)
        : base("NotFound", message) { }

    /// <summary>
    /// Create a not-found error for a specific entity and id. The code becomes
    /// <c>"{entityName}.NotFound"</c> and the message describes the lookup.
    /// </summary>
    /// <param name="entityName">The name of the entity type (e.g. <c>"Customer"</c>).</param>
    /// <param name="entityId">The identifier used in the lookup.</param>
    public NotFoundError(string entityName, object entityId)
        : base($"{entityName}.NotFound", $"{entityName} with id '{entityId}' was not found.")
    {
        EntityName = entityName;
        EntityId = entityId;
    }
}