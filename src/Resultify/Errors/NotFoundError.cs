namespace Resultify.Errors;

/// <summary>
/// Indicates that a requested entity was not found.
/// </summary>
public sealed record NotFoundError : Error
{
    /// <summary>The type name of the entity that was not found.</summary>
    public string? EntityName { get; }

    /// <summary>The identifier that was used in the lookup.</summary>
    public object? EntityId { get; }

    public NotFoundError(string message)
        : base("NotFound", message) { }

    public NotFoundError(string entityName, object entityId)
        : base($"{entityName}.NotFound", $"{entityName} with id '{entityId}' was not found.")
    {
        EntityName = entityName;
        EntityId = entityId;
    }
}
