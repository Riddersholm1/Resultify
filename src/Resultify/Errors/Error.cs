using System.Collections.Immutable;

namespace Resultify.Errors;

/// <summary>
/// An immutable error with a machine-readable <see cref="Code"/> and a human-readable <see cref="Message"/>.
/// Use <c>With*</c> methods or <c>with</c> expressions to produce modified copies.
/// </summary>
public record Error
{
    /// <summary>The empty error — used as a sentinel when an API needs to express "no error" as a value.</summary>
    public static readonly Error None = new(string.Empty, string.Empty);

    /// <summary>A standard error for when a null value was supplied where one was not expected.</summary>
    public static readonly Error NullValue = new("General.NullValue", "A null value was provided where one was not expected.");

    /// <summary>A generic unknown error for use as a last-resort fallback.</summary>
    public static readonly Error Unknown = new("General.Unknown", "An unknown error occurred.");

    /// <summary>A stable, machine-readable identifier, e.g. <c>"User.NotFound"</c>.</summary>
    /// <exception cref="ArgumentNullException">Thrown when set to <c>null</c> via a <c>with</c> expression.</exception>
    public string Code
    {
        get;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            field = value;
        }
    }

    /// <summary>A human-readable description of what went wrong.</summary>
    /// <exception cref="ArgumentNullException">Thrown when set to <c>null</c> via a <c>with</c> expression.</exception>
    public string Message
    {
        get;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            field = value;
        }
    }

    /// <summary>
    /// Structured key-value metadata attached to this error.
    /// Use <see cref="WithMetadata(string, object)"/> to add entries — it returns a new instance.
    /// </summary>
    /// <remarks>
    /// Backed by an <see cref="ImmutableDictionary{TKey, TValue}"/>. The setter is private, so this
    /// is the only representation the property can ever hold — entries cannot be added or replaced
    /// through the exposed <see cref="IReadOnlyDictionary{TKey, TValue}"/>.
    /// </remarks>
    public IReadOnlyDictionary<string, object> Metadata => MetadataStore;

    /// <summary>Strongly typed backing store for <see cref="Metadata"/>.</summary>
    private ImmutableDictionary<string, object> MetadataStore
    {
        get;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            field = value;
        }
    } = ImmutableDictionary<string, object>.Empty;

    /// <summary>
    /// Causal chain of errors or exceptions that led to this error.
    /// Use <see cref="CausedBy(Error)"/> to append causes — it returns a new instance.
    /// </summary>
    public IReadOnlyList<Error> Causes
    {
        get;
        private init
        {
            ArgumentNullException.ThrowIfNull(value);
            field = value;
        }
    } = [];

    /// <summary>Create an error with a machine-readable code and a human-readable message.</summary>
    /// <param name="code">A stable, machine-readable identifier, e.g. <c>"User.NotFound"</c>.</param>
    /// <param name="message">A human-readable description of what went wrong.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="code"/> or <paramref name="message"/> is null.</exception>
    public Error(string code, string message)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(message);
        Code = code;
        Message = message;
    }

    /// <summary>Convenience constructor: create an error with just a message and an empty code.</summary>
    /// <param name="message">A human-readable description of what went wrong.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="message"/> is null.</exception>
    public Error(string message)
        : this(string.Empty, message) { }

    /// <summary>Deconstruct into the machine-readable code and human-readable message.</summary>
    public void Deconstruct(out string code, out string message)
    {
        code = Code;
        message = Message;
    }

    /// <summary>Attach a key-value metadata pair. Returns a new instance.</summary>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> or <paramref name="value"/> is null.</exception>
    public Error WithMetadata(string key, object value)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);
        return this with { MetadataStore = MetadataStore.SetItem(key, value) };
    }

    /// <summary>Attach multiple metadata pairs. Returns a new instance.</summary>
    /// <param name="metadata">The metadata pairs to attach. No key or value may be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="metadata"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when any key or value in <paramref name="metadata"/> is null.</exception>
    public Error WithMetadata(IEnumerable<KeyValuePair<string, object>> metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ImmutableDictionary<string, object>.Builder builder = MetadataStore.ToBuilder();
        foreach (KeyValuePair<string, object> kvp in metadata)
        {
            if (kvp.Key is null)
            {
                throw new ArgumentException("Metadata keys must not be null.", nameof(metadata));
            }

            builder[kvp.Key] = kvp.Value ?? throw new ArgumentException($"Metadata value for key '{kvp.Key}' must not be null.", nameof(metadata));
        }

        return this with { MetadataStore = builder.ToImmutable() };
    }

    /// <summary>Add a cause to the causal chain. Returns a new instance.</summary>
    /// <param name="cause">The causing error.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="cause"/> is null.</exception>
    public Error CausedBy(Error cause)
    {
        ArgumentNullException.ThrowIfNull(cause);
        return this with { Causes = [.. Causes, cause] };
    }

    /// <summary>Add multiple causes. Returns a new instance.</summary>
    /// <param name="causes">The causing errors. Must not contain null elements.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="causes"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when any element in <paramref name="causes"/> is null.</exception>
    public Error CausedBy(IEnumerable<Error> causes)
    {
        ArgumentNullException.ThrowIfNull(causes);
        List<Error> combined = [.. Causes];
        foreach (Error cause in causes)
        {
            if (cause is null)
            {
                throw new ArgumentException("Cause elements must not be null.", nameof(causes));
            }

            combined.Add(cause);
        }

        // A collection expression (rather than ToArray) so the result is a read-only view:
        // callers must not be able to cast Causes back to Error[] and write through it.
        return this with { Causes = [.. combined] };
    }

    /// <summary>Add an exception as a cause. Returns a new instance.</summary>
    /// <param name="exception">The causing exception.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
    public Error CausedBy(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return this with { Causes = [.. Causes, new ExceptionalError(exception)] };
    }

    /// <summary>
    /// Returns a human-readable representation of the error, including its code (when present)
    /// and any causal chain. Intended for logs and debugging — not for end-user display.
    /// </summary>
    /// <returns>
    /// <c>"[Code] Message"</c>, or just <c>"Message"</c> when <see cref="Code"/> is empty,
    /// followed by <c>" (caused by: …)"</c> when <see cref="Causes"/> is non-empty.
    /// </returns>
    /// <remarks>
    /// Sealed deliberately. A <c>record</c> synthesises its own <c>ToString</c> unless the base
    /// declares a sealed one, so without <c>sealed</c> every derived error — including the built-in
    /// subtypes and any custom error a consumer writes — would silently fall back to the record
    /// format (<c>"NotFoundError { Code = …, Metadata = System.Collections.Immutable… }"</c>) and
    /// dump its whole state into logs. Sealing guarantees one stable log format for every
    /// <see cref="Error"/>; carry extra detail in <see cref="Metadata"/> or <see cref="Causes"/>
    /// rather than by overriding this method.
    /// </remarks>
    public sealed override string ToString()
    {
        string codePrefix = string.IsNullOrEmpty(Code) ? string.Empty : $"[{Code}] ";
        string causesSuffix = Causes.Count > 0 ? $" (caused by: {string.Join(", ", Causes)})" : string.Empty;
        return $"{codePrefix}{Message}{causesSuffix}";
    }

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