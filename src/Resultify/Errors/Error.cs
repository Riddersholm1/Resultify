using System.Collections.Immutable;

namespace Resultify.Errors;

/// <summary>
/// Represents an error with a machine-readable <see cref="Code"/> and a human-readable <see cref="Message"/>.
/// Immutable — use <c>With*</c> methods or <c>with</c> expressions to produce new instances.
/// Instances are safe to share across threads.
/// </summary>
public record Error
{
    private readonly string _code;
    private readonly string _message;

    /// <summary>The empty error — used as a sentinel when an API needs to express "no error" as a value.</summary>
    public static readonly Error None = new(string.Empty, string.Empty);

    /// <summary>A standard error for when a null value was supplied where one was not expected.</summary>
    public static readonly Error NullValue = new("General.NullValue", "A null value was provided where one was not expected.");

    /// <summary>A generic unknown error for use as a last-resort fallback.</summary>
    public static readonly Error Unknown = new("General.Unknown", "An unknown error occurred.");

    /// <summary>A stable, machine-readable identifier, e.g. <c>"User.NotFound"</c>. Useful for i18n, logs, API responses.</summary>
    /// <exception cref="ArgumentNullException">Thrown when set to <c>null</c> via a <c>with</c> expression.</exception>
    public string Code
    {
        get => _code;
        init => _code = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>A human-readable description of what went wrong.</summary>
    /// <exception cref="ArgumentNullException">Thrown when set to <c>null</c> via a <c>with</c> expression.</exception>
    public string Message
    {
        get => _message;
        init => _message = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>Create an error with a machine-readable code and a human-readable message.</summary>
    /// <param name="code">A stable, machine-readable identifier, e.g. <c>"User.NotFound"</c>.</param>
    /// <param name="message">A human-readable description of what went wrong.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="code"/> or <paramref name="message"/> is null.</exception>
    public Error(string code, string message)
    {
        _code = code ?? throw new ArgumentNullException(nameof(code));
        _message = message ?? throw new ArgumentNullException(nameof(message));
    }

    /// <summary>Convenience constructor: create an error with just a message and an empty code.</summary>
    /// <param name="message">A human-readable description of what went wrong.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="message"/> is null.</exception>
    public Error(string message) : this(string.Empty, message) { }

    /// <summary>Deconstruct into the machine-readable code and human-readable message.</summary>
    public void Deconstruct(out string code, out string message)
    {
        code = Code;
        message = Message;
    }

    /// <summary>Structured metadata attached to this error.</summary>
    public IReadOnlyDictionary<string, object> Metadata { get; private init; } = ImmutableDictionary<string, object>.Empty;

    /// <summary>Causal chain of errors or exceptions that led to this error.</summary>
    public IReadOnlyList<Error> Causes { get; private init; } = [];

    /// <summary>Attach a key-value metadata pair. Returns a new instance.</summary>
    /// <param name="key">The metadata key. Must not be null.</param>
    /// <param name="value">The metadata value. Must not be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> or <paramref name="value"/> is null.</exception>
    public Error WithMetadata(string key, object value)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);
        ImmutableDictionary<string, object> current = Metadata as ImmutableDictionary<string, object>
            ?? Metadata.ToImmutableDictionary();
        return this with { Metadata = current.SetItem(key, value) };
    }

    /// <summary>Attach multiple metadata pairs. Returns a new instance.</summary>
    /// <param name="metadata">The metadata pairs to attach. Must not be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="metadata"/> is null.</exception>
    public Error WithMetadata(IEnumerable<KeyValuePair<string, object>> metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ImmutableDictionary<string, object> current = Metadata as ImmutableDictionary<string, object>
            ?? Metadata.ToImmutableDictionary();
        ImmutableDictionary<string, object>.Builder builder = current.ToBuilder();
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