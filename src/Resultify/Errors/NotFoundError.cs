namespace Resultify.Errors;

/// <summary>
/// Indicates that a requested entity was not found (HTTP 404).
/// The code defaults to <c>"NotFound"</c>, or <c>"{EntityName}.NotFound"</c> when an entity and id are supplied.
/// </summary>
public sealed record NotFoundError : Error
{
    /// <summary>The type name of the entity that was not found, if supplied.</summary>
    public string? EntityName { get; }

    /// <summary>The identifier that was used in the lookup, if supplied.</summary>
    public object? EntityId { get; }

    /// <summary>Create a not-found error with the default code <c>"NotFound"</c>.</summary>
    /// <param name="message">A human-readable description of what was not found.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="message"/> is null.</exception>
    public NotFoundError(string message)
        : base("NotFound", message) { }

    /// <summary>
    /// Create a not-found error for a specific entity and id. The code becomes
    /// <c>"{entityName}.NotFound"</c> and the message describes the lookup.
    /// </summary>
    /// <param name="entityName">The entity type name, e.g. <c>"Customer"</c>.</param>
    /// <param name="entityId">The identifier used in the lookup.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entityName"/> or <paramref name="entityId"/> is null.</exception>
    public NotFoundError(string entityName, object entityId)
        : base(BuildCode(entityName), BuildMessage(entityName, entityId))
    {
        EntityName = entityName;
        EntityId = entityId;
    }

    private static string BuildCode(string entityName)
    {
        ArgumentNullException.ThrowIfNull(entityName);
        return $"{entityName}.NotFound";
    }

    private static string BuildMessage(string entityName, object entityId)
    {
        ArgumentNullException.ThrowIfNull(entityId);
        return $"{entityName} with id '{entityId}' was not found.";
    }
}