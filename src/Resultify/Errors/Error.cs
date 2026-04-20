using System.Collections.Immutable;

namespace Resultify.Errors;

/// <summary>
/// Represents an error with a machine-readable <paramref name="Code"/> and a human-readable <paramref name="Message"/>.
/// Immutable — use <c>With*</c> methods or <c>with</c> expressions to produce new instances.
/// </summary>
/// <param name="Code">A stable, machine-readable identifier, e.g. <c>"User.NotFound"</c>. Useful for i18n, logs, API responses.</param>
/// <param name="Message">A human-readable description of what went wrong.</param>
public record Error(string Code, string Message)
{
    /// <summary>The empty error — used as a sentinel when an API needs to express "no error" as a value.</summary>
    public static readonly Error None = new(string.Empty, string.Empty);

    /// <summary>A standard error for when a null value was supplied where one was not expected.</summary>
    public static readonly Error NullValue = new("General.NullValue", "A null value was provided where one was not expected.");

    /// <summary>A generic unknown error for use as a last-resort fallback.</summary>
    public static readonly Error Unknown = new("General.Unknown", "An unknown error occurred.");

    /// <summary>Convenience constructor: create an error with just a message and an empty code.</summary>
    public Error(string message) : this(string.Empty, message) { }

    // Validate positional parameters are not null.
    private readonly string _code = Code ?? throw new ArgumentNullException(nameof(Code));
    private readonly string _message = Message ?? throw new ArgumentNullException(nameof(Message));

    /// <summary>Structured metadata attached to this error.</summary>
    public IReadOnlyDictionary<string, object> Metadata { get; private init; } = ImmutableDictionary<string, object>.Empty;

    /// <summary>Causal chain of errors or exceptions that led to this error.</summary>
    public IReadOnlyList<Error> Causes { get; private init; } = [];

    /// <summary>Attach a key-value metadata pair. Returns a new instance.</summary>
    public Error WithMetadata(string key, object value)
    {
        ImmutableDictionary<string, object> builder = Metadata.ToImmutableDictionary().SetItem(key, value);
        return this with { Metadata = builder };
    }

    /// <summary>Attach multiple metadata pairs. Returns a new instance.</summary>
    public Error WithMetadata(IEnumerable<KeyValuePair<string, object>> metadata)
    {
        ImmutableDictionary<string, object>.Builder builder = Metadata.ToImmutableDictionary().ToBuilder();
        foreach (KeyValuePair<string, object> kvp in metadata)
        {
            builder[kvp.Key] = kvp.Value;
        }

        return this with { Metadata = builder.ToImmutable() };
    }

    /// <summary>Add a cause to the causal chain. Returns a new instance.</summary>
    public Error CausedBy(Error cause) =>
        this with { Causes = [.. Causes, cause] };

    /// <summary>Add multiple causes. Returns a new instance.</summary>
    public Error CausedBy(IEnumerable<Error> causes) =>
        this with { Causes = [.. Causes, .. causes] };

    /// <summary>Add an exception as a cause. Returns a new instance.</summary>
    public Error CausedBy(Exception exception) =>
        this with { Causes = [.. Causes, new ExceptionalError(exception)] };

    /// <summary>
    /// Returns a human-readable representation of the error, including its code (when present)
    /// and any causal chain. Intended for logs and debugging — not for end-user display.
    /// </summary>
    public override string ToString() =>
        string.IsNullOrEmpty(Code)
            ? Causes.Count > 0 ? $"{Message} (caused by: {string.Join(", ", Causes)})" : Message
            : Causes.Count > 0 ? $"[{Code}] {Message} (caused by: {string.Join(", ", Causes)})" : $"[{Code}] {Message}";

    /// <inheritdoc />
    public virtual bool Equals(Error? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (EqualityContract != other.EqualityContract)
        {
            return false;
        }

        if (Code != other.Code || Message != other.Message)
        {
            return false;
        }

        if (!Causes.SequenceEqual(other.Causes))
        {
            return false;
        }

        if (Metadata.Count != other.Metadata.Count)
        {
            return false;
        }

        foreach (KeyValuePair<string, object> kvp in Metadata)
        {
            if (!other.Metadata.TryGetValue(kvp.Key, out object? otherValue) ||
                !Equals(kvp.Value, otherValue))
            {
                return false;
            }
        }
        return true;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(EqualityContract);
        hash.Add(Code);
        hash.Add(Message);
        foreach (Error cause in Causes)
        {
            hash.Add(cause);
        }

        foreach (KeyValuePair<string, object> kvp in Metadata.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            hash.Add(kvp.Key);
            hash.Add(kvp.Value);
        }
        return hash.ToHashCode();
    }
}