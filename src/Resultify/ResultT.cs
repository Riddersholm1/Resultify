using System.Diagnostics.CodeAnalysis;
using Resultify.Errors;

namespace Resultify;

/// <summary>
/// Represents the outcome of an operation that returns a value of type <typeparamref name="TValue"/>.
/// Value type — zero-allocation on the success path when <typeparamref name="TValue"/> is a value type.
/// Instances are immutable and safe to share across threads.
/// </summary>
public readonly struct Result<TValue> : IEquatable<Result<TValue>>
{

    private readonly IReadOnlyList<Error>? _errors;

    /// <summary>All errors. Empty when successful.</summary>
    public IReadOnlyList<Error> Errors =>
        _errors ?? ResultHelper.EmptyErrors;

    /// <summary>
    /// The first error when <see cref="IsFailure"/>; otherwise the sentinel <see cref="Error.None"/>.
    /// <para>
    /// ⚠️ Only meaningful when <see cref="IsFailure"/> is true. On a successful result this returns
    /// <see cref="Error.None"/>, so <c>result.FirstError == someError</c> without a failure check can
    /// match the sentinel unintentionally. Prefer <c>if (result.IsFailure) { ... result.FirstError ... }</c>.
    /// </para>
    /// Use <see cref="Errors"/> when you need all of them.
    /// </summary>
    public Error FirstError =>
        _errors is { Count: > 0 }
        ? _errors[0]
        : Error.None;

    /// <summary>True when the operation completed without errors.</summary>
    [MemberNotNullWhen(false, nameof(_errors))]
    public bool IsSuccess =>
        _errors is null or { Count: 0 };

    /// <summary>True when the operation failed with at least one error.</summary>
    [MemberNotNullWhen(true, nameof(_errors))]
    public bool IsFailure =>
        !IsSuccess;

    /// <summary>
    /// The value of the result.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="IsFailure"/> is true, or when <see cref="ValueOrDefault"/> is <c>null</c>
    /// (e.g. on <c>default(Result&lt;T&gt;)</c> for reference-type <typeparamref name="TValue"/>,
    /// which reports <see cref="IsSuccess"/> = <c>true</c> but has no meaningful value).
    /// Use <see cref="ValueOrDefault"/> or <see cref="TryGetValue"/> for exception-free access.
    /// </exception>
    [NotNull]
    public TValue Value
    {
        get
        {
            if (IsFailure)
            {
                throw new InvalidOperationException(
                    $"Cannot access Value on a failed result. Errors: {string.Join("; ", Errors)}");
            }

            if (ValueOrDefault is null)
            {
                throw new InvalidOperationException(
                    "Cannot access Value when it is null (e.g. on default(Result<T>) for reference types). Use ValueOrDefault or TryGetValue for nullable access.");
            }

            return ValueOrDefault;
        }
    }

    /// <summary>The value if successful; otherwise <c>default(TValue)</c>.</summary>
    public TValue? ValueOrDefault { get; }

    /// <summary>
    /// Try to retrieve the success value without throwing.
    /// Returns <c>true</c> when the result is a success with a non-null value; otherwise <c>false</c>.
    /// </summary>
    /// <param name="value">When this method returns <c>true</c>, contains the non-null success value.</param>
    public bool TryGetValue([MaybeNullWhen(false)] out TValue value)
    {
        if (IsSuccess && ValueOrDefault is not null)
        {
            value = ValueOrDefault;
            return true;
        }

        value = default(TValue);
        return false;
    }

    private Result(TValue value)
    {
        ValueOrDefault = value;
        _errors = null;
    }

    private Result(IReadOnlyList<Error> errors)
    {
        ValueOrDefault = default(TValue?);
        _errors = errors;
    }

    // ── Factory methods ──────────────────────────────────────

    /// <summary>Create a successful result with the given value.</summary>
    /// <param name="value">The value to wrap. Must not be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static Result<TValue> Success(TValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new Result<TValue>(value);
    }

    /// <summary>Create a failed result from a single error.</summary>
    public static Result<TValue> Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result<TValue>([error]);
    }

    /// <summary>Create a failed result from an error message.</summary>
    public static Result<TValue> Failure(string errorMessage) =>
        Failure(new Error(errorMessage));

    /// <summary>Create a failed result with a code and message.</summary>
    public static Result<TValue> Failure(string code, string errorMessage) =>
        Failure(new Error(code, errorMessage));

    /// <summary>Create a failed result from multiple errors.</summary>
    /// <param name="errors">The errors to include. Must be non-null, contain at least one element, and no null elements.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="errors"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="errors"/> is empty or contains null elements.</exception>
    public static Result<TValue> Failure(IEnumerable<Error> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        Error[] array = errors.ToArray();
        if (array.Length == 0)
        {
            throw new ArgumentException("At least one error is required.", nameof(errors));
        }

        foreach (Error e in array)
        {
            if (e is null)
            {
                throw new ArgumentException("Error elements must not be null.", nameof(errors));
            }
        }

        return new Result<TValue>(array);
    }

    /// <summary>
    /// Null-safe factory: returns <see cref="Success(TValue)"/> if the value is non-null,
    /// otherwise returns a failure with <see cref="Error.NullValue"/>.
    /// </summary>
    public static Result<TValue> Create(TValue? value) =>
        value is not null
            ? Success(value)
            : Failure(Error.NullValue);

    // ── Conditional factories ────────────────────────────────

    /// <summary>Returns Success if the condition is true; otherwise Failure.</summary>
    public static Result<TValue> SuccessIf(bool condition, TValue value, Error error) =>
        condition
            ? Success(value)
            : Failure(error);

    /// <summary>Returns Success if the condition is true; otherwise Failure.</summary>
    public static Result<TValue> SuccessIf(bool condition, TValue value, string errorMessage) =>
        condition
            ? Success(value)
            : Failure(errorMessage);

    /// <summary>Returns Success if the condition is true; otherwise Failure with lazy error.</summary>
    public static Result<TValue> SuccessIf(bool condition, TValue value, Func<Error> errorFactory) =>
        condition
            ? Success(value)
            : Failure(errorFactory());

    // ── Try

    /// <summary>
    /// Execute a function, catching exceptions as errors.
    /// A <c>null</c> return value produces a failure with <see cref="Error.NullValue"/>
    /// rather than wrapping the internal null-check as an <see cref="ExceptionalError"/>.
    /// </summary>
    public static Result<TValue> Try(Func<TValue> func, Func<Exception, Error>? exceptionHandler = null)
    {
        try
        {
            return Create(func());
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Error error = exceptionHandler?.Invoke(ex) ?? new ExceptionalError(ex);
            return Failure(error);
        }
    }

    /// <summary>
    /// Execute an async function, catching exceptions as errors.
    /// A <c>null</c> return value produces a failure with <see cref="Error.NullValue"/>
    /// rather than wrapping the internal null-check as an <see cref="ExceptionalError"/>.
    /// </summary>
    public static async Task<Result<TValue>> TryAsync(Func<Task<TValue>> func, Func<Exception, Error>? exceptionHandler = null)
    {
        try
        {
            return Create(await func().ConfigureAwait(false));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Error error = exceptionHandler?.Invoke(ex) ?? new ExceptionalError(ex);
            return Failure(error);
        }
    }

    // ── Combinators ──────────────────────────────────────────

    /// <summary>
    /// Transform the value if successful. Errors are propagated unchanged.
    /// A <c>null</c> mapped value produces a failure with <see cref="Error.NullValue"/>,
    /// matching the semantics of <see cref="Create(TValue)"/> and <see cref="Try(Func{TValue}, Func{Exception, Error}?)"/>.
    /// </summary>
    public Result<TNew> Map<TNew>(Func<TValue, TNew> mapper) =>
        IsSuccess
            ? Result<TNew>.Create(mapper(Value))
            : Result<TNew>.Failure(Errors);

    /// <summary>
    /// Async Map. A <c>null</c> mapped value produces a failure with <see cref="Error.NullValue"/>.
    /// </summary>
    public async Task<Result<TNew>> MapAsync<TNew>(Func<TValue, Task<TNew>> mapper) =>
        IsSuccess
            ? Result<TNew>.Create(await mapper(Value).ConfigureAwait(false))
            : Result<TNew>.Failure(Errors);

    /// <summary>Chain a dependent operation that also returns a Result.</summary>
    public Result<TNew> Bind<TNew>(Func<TValue, Result<TNew>> bind) =>
        IsSuccess
            ? bind(Value)
            : Result<TNew>.Failure(Errors);

    /// <summary>Async Bind.</summary>
    public Task<Result<TNew>> BindAsync<TNew>(Func<TValue, Task<Result<TNew>>> bind) =>
        IsSuccess
            ? bind(Value)
            : Task.FromResult(Result<TNew>.Failure(Errors));

    /// <summary>Bind to a non-generic Result (e.g. for void operations).</summary>
    public Result Bind(Func<TValue, Result> bind) =>
        IsSuccess
            ? bind(Value)
            : Result.Failure(Errors);

    /// <summary>Async Bind to non-generic Result.</summary>
    public Task<Result> BindAsync(Func<TValue, Task<Result>> bind) =>
        IsSuccess
            ? bind(Value)
            : Task.FromResult(Result.Failure(Errors));

    /// <summary>Execute a side effect if successful. Returns this result unchanged.</summary>
    public Result<TValue> Tap(Action<TValue> action)
    {
        if (IsSuccess)
        {
            action(Value);
        }

        return this;
    }

    /// <summary>Async Tap.</summary>
    public async Task<Result<TValue>> TapAsync(Func<TValue, Task> action)
    {
        if (IsSuccess)
        {
            await action(Value).ConfigureAwait(false);
        }

        return this;
    }

    /// <summary>Execute a side effect if failed. Returns this result unchanged.</summary>
    public Result<TValue> TapError(Action<IReadOnlyList<Error>> action)
    {
        if (IsFailure)
        {
            action(Errors);
        }

        return this;
    }

    /// <summary>Async TapError.</summary>
    public async Task<Result<TValue>> TapErrorAsync(Func<IReadOnlyList<Error>, Task> action)
    {
        if (IsFailure)
        {
            await action(Errors).ConfigureAwait(false);
        }

        return this;
    }

    /// <summary>Add a validation gate on the value.</summary>
    /// /// <exception cref="ArgumentNullException">Thrown when <paramref name="predicate"/> or <paramref name="error"/> is null.</exception>
    public Result<TValue> Ensure(Func<TValue, bool> predicate, Error error)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(error);
        if (IsFailure)
        {
            return this;
        }

        return predicate(Value)
            ? this
            : Failure(error);
    }

    /// <summary>Add a validation gate on the value with a string error message.</summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="predicate"/> or <paramref name="errorMessage"/> is null.</exception>
    public Result<TValue> Ensure(Func<TValue, bool> predicate, string errorMessage)
    {
        ArgumentNullException.ThrowIfNull(errorMessage);
        return Ensure(predicate, new Error(errorMessage));
    }

    /// <summary>Add a validation gate with a lazy error factory.</summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="predicate"/> or <paramref name="errorFactory"/> is null.</exception>
    public Result<TValue> Ensure(Func<TValue, bool> predicate, Func<TValue, Error> errorFactory)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(errorFactory);
        if (IsFailure)
        {
            return this;
        }

        return predicate(Value)
            ? this
            : Failure(errorFactory(Value));
    }

    /// <summary>Pattern match: execute one of two functions depending on success/failure.</summary>
    public TOut Match<TOut>(Func<TValue, TOut> onSuccess, Func<IReadOnlyList<Error>, TOut> onFailure) =>
        IsSuccess
            ? onSuccess(Value)
            : onFailure(Errors);

    /// <summary>Async Match.</summary>
    public Task<TOut> MatchAsync<TOut>(Func<TValue, Task<TOut>> onSuccess, Func<IReadOnlyList<Error>, Task<TOut>> onFailure) =>
        IsSuccess
            ? onSuccess(Value)
            : onFailure(Errors);

    /// <summary>Execute one of two actions depending on success/failure.</summary>
    public void Switch(Action<TValue> onSuccess, Action<IReadOnlyList<Error>> onFailure)
    {
        if (IsSuccess)
        {
            onSuccess(Value);
        }
        else
        {
            onFailure(Errors);
        }
    }

    // ── Conversion ───────────────────────────────────────────

    /// <summary>Drop the value, keeping only the success/failure state and errors.</summary>
    public Result ToResult() =>
        IsSuccess
            ? Result.Success()
            : Result.Failure(Errors);

    /// <summary>
    /// Convert to a Result with a different value type.
    /// A <c>null</c> converted value produces a failure with <see cref="Error.NullValue"/>,
    /// matching the semantics of <see cref="Map{TNew}(Func{TValue, TNew})"/>.
    /// </summary>
    public Result<TNew> ToResult<TNew>(Func<TValue, TNew> converter) =>
        IsSuccess
            ? Result<TNew>.Create(converter(Value))
            : Result<TNew>.Failure(Errors);

    // ── Deconstruct ──────────────────────────────────────────

    /// <summary>Deconstructs the result into its components.</summary>
    /// <param name="isSuccess">Whether the result is successful.</param>
    /// <param name="value">The value if successful; otherwise <c>default</c>.</param>
    /// <param name="errors">The list of errors.</param>
    public void Deconstruct(out bool isSuccess, out TValue? value, out IReadOnlyList<Error> errors)
    {
        isSuccess = IsSuccess;
        value = ValueOrDefault;
        errors = Errors;
    }

    // ── Error querying ───────────────────────────────────────

    /// <summary>Check if the result contains an error of the specified type.</summary>
    public bool HasError<TError>() where TError : Error =>
        Errors.OfType<TError>().Any();

    /// <summary>Check if the result contains an error of the specified type matching a predicate.</summary>
    public bool HasError<TError>(Func<TError, bool> predicate) where TError : Error =>
        Errors.OfType<TError>().Any(predicate);

    /// <summary>Check if the result contains an error with the specified code.</summary>
    public bool HasErrorCode(string code) =>
        Errors.Any(e => e.Code == code);

    /// <summary>Check if any error in the result was caused by a specific exception type.</summary>
    public bool HasException<TException>() where TException : Exception =>
        Errors.OfType<ExceptionalError>().Any(e => e.Exception is TException);

    // ── Implicit conversions ─────────────────────────────────

    /// <summary>
    /// Implicitly convert a value to a Result. Non-null becomes <see cref="Success(TValue)"/>;
    /// null becomes a failure with <see cref="Error.NullValue"/>.
    /// </summary>
    public static implicit operator Result<TValue>(TValue? value) =>
        Create(value);

    /// <summary>Implicitly convert a single Error to a failed Result.</summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="error"/> is null.</exception>
    public static implicit operator Result<TValue>(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return Failure(error);
    }

    /// <summary>Implicitly convert a list of Errors to a failed Result.</summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="errors"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="errors"/> is empty.</exception>
    public static implicit operator Result<TValue>(List<Error> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        return Failure(errors);
    }

    // ── Equality ─────────────────────────────────────────────

    /// <inheritdoc />
    public bool Equals(Result<TValue> other)
    {
        if (IsSuccess != other.IsSuccess)
        {
            return false;
        }

        return IsSuccess
            ? EqualityComparer<TValue>.Default.Equals(ValueOrDefault, other.ValueOrDefault)
            : Errors.SequenceEqual(other.Errors);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is Result<TValue> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        if (IsSuccess)
        {
            return HashCode.Combine(true, ValueOrDefault);
        }

        var hc = new HashCode();
        hc.Add(false);
        foreach (Error e in Errors)
        {
            hc.Add(e);
        }

        return hc.ToHashCode();
    }

    /// <summary>Equality operator.</summary>
    public static bool operator ==(Result<TValue> left, Result<TValue> right) =>
        left.Equals(right);
    /// <summary>Inequality operator.</summary>
    public static bool operator !=(Result<TValue> left, Result<TValue> right) =>
        !left.Equals(right);

    /// <inheritdoc />
    public override string ToString() =>
        IsSuccess
            ? $"Result<{typeof(TValue).Name}>: Success ({ValueOrDefault?.ToString() ?? "null"})"
            : $"Result<{typeof(TValue).Name}>: Failure ({string.Join("; ", Errors)})";
}

/// <summary>
/// Holds constants shared across all <c>Result&lt;TValue&gt;</c> closed types,
/// ensuring a single allocation regardless of how many type parameters are used.
/// </summary>
internal static class ResultHelper
{
    // Shared empty list so reading Errors on a successful result is allocation-free
    // and not duplicated per closed generic type.
    internal static readonly IReadOnlyList<Error> EmptyErrors = [];
}