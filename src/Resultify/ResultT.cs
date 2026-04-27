using System.Diagnostics.CodeAnalysis;
using Resultify.Errors;

namespace Resultify;

/// <summary>
/// The outcome of an operation that returns a value of type <typeparamref name="TValue"/>.
/// A <c>readonly struct</c> — zero allocation on the success path for value types.
/// </summary>
public readonly struct Result<TValue> : IEquatable<Result<TValue>>
{
    private readonly IReadOnlyList<Error>? _errors;

    /// <summary>All errors. Empty when successful.</summary>
    public IReadOnlyList<Error> Errors => _errors ?? ResultHelper.EmptyErrors;

    /// <summary>
    /// The first error when failed; otherwise <see cref="Error.None"/>.
    /// Use <see cref="Errors"/> when you need all errors.
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
    /// The success value.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the result is failed, or when <see cref="ValueOrDefault"/> is <c>null</c>
    /// (e.g. on <c>default(Result&lt;T&gt;)</c> for reference types).
    /// Use <see cref="ValueOrDefault"/> or <see cref="TryGetValue"/> for exception-free access.
    /// </exception>
    [NotNull]
    public TValue Value
    {
        get
        {
            if (IsFailure)
            {
                throw new InvalidOperationException($"Cannot access Value on a failed result. Errors: {string.Join("; ", Errors)}");
            }

            return ValueOrDefault ?? throw new InvalidOperationException("Cannot access Value when it is null (e.g. on default(Result<T>) for reference types). Use ValueOrDefault or TryGetValue for nullable access.");
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

    // ───────────────────- Factory methods ───────────────────- //

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
    public static Result<TValue> Failure(string errorMessage)
    {
        ArgumentNullException.ThrowIfNull(errorMessage);
        return Failure(new Error(errorMessage));
    }

    /// <summary>Create a failed result with a code and message.</summary>
    public static Result<TValue> Failure(string code, string errorMessage)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(errorMessage);
        return Failure(new Error(code, errorMessage));
    }

    /// <summary>Create a failed result from multiple errors.</summary>
    /// <param name="errors">The errors to include. Must be non-null, contain at least one element, and no null elements.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="errors"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="errors"/> is empty or contains null elements.</exception>
    public static Result<TValue> Failure(IEnumerable<Error> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        List<Error> list = [];
        foreach (Error e in errors)
        {
            if (e is null)
            {
                throw new ArgumentException("Error elements must not be null.", nameof(errors));
            }

            list.Add(e);
        }

        return list.Count == 0
            ? throw new ArgumentException("At least one error is required.", nameof(errors))
            : new Result<TValue>(list.ToArray());
    }

    // Internal factory used by combinators to forward an already-validated, immutable error list
    // without re-running the public Failure(IEnumerable<Error>) validation. Callers must guarantee
    // the list is non-null, non-empty, contains no null elements, and will not be mutated afterwards.
    internal static Result<TValue> FailureUnchecked(IReadOnlyList<Error> errors) =>
        new(errors);

    /// <summary>
    /// Null-safe factory: returns <see cref="Success(TValue)"/> if the value is non-null,
    /// otherwise returns a failure with <see cref="Error.NullValue"/>.
    /// </summary>
    public static Result<TValue> Create(TValue? value) =>
        value is not null
            ? Success(value)
            : Failure(Error.NullValue);

    // ───────────────────- Conditional factories ───────────────────- //

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
    public static Result<TValue> SuccessIf(bool condition, TValue value, Func<Error> errorFactory)
    {
        ArgumentNullException.ThrowIfNull(errorFactory);
        return condition
            ? Success(value)
            : Failure(errorFactory());
    }

    /// <summary>Returns Failure if the condition is true; otherwise Success.</summary>
    public static Result<TValue> FailureIf(bool condition, TValue value, Error error) =>
        condition
            ? Failure(error)
            : Success(value);

    /// <summary>Returns Failure if the condition is true; otherwise Success.</summary>
    public static Result<TValue> FailureIf(bool condition, TValue value, string errorMessage) =>
        condition
            ? Failure(errorMessage)
            : Success(value);

    /// <summary>Returns Failure if the condition is true; otherwise Success with lazy error.</summary>
    public static Result<TValue> FailureIf(bool condition, TValue value, Func<Error> errorFactory)
    {
        ArgumentNullException.ThrowIfNull(errorFactory);
        return condition
            ? Failure(errorFactory())
            : Success(value);
    }

    // ───────────────────- Try ───────────────────- //

    /// <summary>
    /// Executes a function, catching exceptions as errors.
    /// A <c>null</c> return value produces a failure with <see cref="Error.NullValue"/>.
    /// </summary>
    public static Result<TValue> Try(Func<TValue> func, Func<Exception, Error>? exceptionHandler = null)
    {
        ArgumentNullException.ThrowIfNull(func);
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
    /// Executes an async function, catching exceptions as errors.
    /// A <c>null</c> return value produces a failure with <see cref="Error.NullValue"/>.
    /// </summary>
    public static async Task<Result<TValue>> TryAsync(Func<Task<TValue>> func, Func<Exception, Error>? exceptionHandler = null)
    {
        ArgumentNullException.ThrowIfNull(func);
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

    /// <summary>Execute a function returning a <see cref="Result{TValue}"/>, catching exceptions.</summary>
    public static Result<TValue> Try(Func<Result<TValue>> func, Func<Exception, Error>? exceptionHandler = null)
    {
        ArgumentNullException.ThrowIfNull(func);
        try
        {
            return func();
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

    /// <summary>Execute an async function returning a <see cref="Result{TValue}"/>, catching exceptions.</summary>
    public static async Task<Result<TValue>> TryAsync(Func<Task<Result<TValue>>> func, Func<Exception, Error>? exceptionHandler = null)
    {
        ArgumentNullException.ThrowIfNull(func);
        try
        {
            return await func().ConfigureAwait(false);
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

    // ───────────────────- Combinators ───────────────────- //

    /// <summary>
    /// Transforms the value if successful. Errors are propagated unchanged.
    /// A <c>null</c> mapped value produces a failure with <see cref="Error.NullValue"/>.
    /// </summary>
    public Result<TNew> Map<TNew>(Func<TValue, TNew> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        return IsSuccess
            ? Result<TNew>.Create(mapper(Value))
            : Result<TNew>.FailureUnchecked(Errors);
    }

    /// <summary>
    /// Async Map. A <c>null</c> mapped value produces a failure with <see cref="Error.NullValue"/>.
    /// </summary>
    public async Task<Result<TNew>> MapAsync<TNew>(Func<TValue, Task<TNew>> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        return IsSuccess
            ? Result<TNew>.Create(await mapper(Value).ConfigureAwait(false))
            : Result<TNew>.FailureUnchecked(Errors);
    }

    /// <summary>Chain a dependent operation that also returns a Result.</summary>
    public Result<TNew> Bind<TNew>(Func<TValue, Result<TNew>> bind)
    {
        ArgumentNullException.ThrowIfNull(bind);
        return IsSuccess
            ? bind(Value)
            : Result<TNew>.FailureUnchecked(Errors);
    }

    /// <summary>Async Bind.</summary>
    public Task<Result<TNew>> BindAsync<TNew>(Func<TValue, Task<Result<TNew>>> bind)
    {
        ArgumentNullException.ThrowIfNull(bind);
        return IsSuccess
            ? bind(Value)
            : Task.FromResult(Result<TNew>.FailureUnchecked(Errors));
    }

    /// <summary>Bind to a non-generic Result (e.g. for void operations).</summary>
    public Result Bind(Func<TValue, Result> bind)
    {
        ArgumentNullException.ThrowIfNull(bind);
        return IsSuccess
            ? bind(Value)
            : Result.FailureUnchecked(Errors);
    }

    /// <summary>Async Bind to non-generic Result.</summary>
    public Task<Result> BindAsync(Func<TValue, Task<Result>> bind)
    {
        ArgumentNullException.ThrowIfNull(bind);
        return IsSuccess
            ? bind(Value)
            : Task.FromResult(Result.FailureUnchecked(Errors));
    }

    /// <summary>Execute a side effect if successful. Returns this result unchanged.</summary>
    public Result<TValue> Tap(Action<TValue> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (IsSuccess)
        {
            action(Value);
        }

        return this;
    }

    /// <summary>Async Tap.</summary>
    public async Task<Result<TValue>> TapAsync(Func<TValue, Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (IsSuccess)
        {
            await action(Value).ConfigureAwait(false);
        }

        return this;
    }

    /// <summary>Execute a side effect if failed. Returns this result unchanged.</summary>
    public Result<TValue> TapError(Action<IReadOnlyList<Error>> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (IsFailure)
        {
            action(Errors);
        }

        return this;
    }

    /// <summary>Async TapError.</summary>
    public async Task<Result<TValue>> TapErrorAsync(Func<IReadOnlyList<Error>, Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (IsFailure)
        {
            await action(Errors).ConfigureAwait(false);
        }

        return this;
    }

    /// <summary>Add a validation gate on the value.</summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="predicate"/> or <paramref name="error"/> is null.</exception>
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
    public TOut Match<TOut>(Func<TValue, TOut> onSuccess, Func<IReadOnlyList<Error>, TOut> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return IsSuccess ? onSuccess(Value) : onFailure(Errors);
    }

    /// <summary>Async Match.</summary>
    public Task<TOut> MatchAsync<TOut>(Func<TValue, Task<TOut>> onSuccess, Func<IReadOnlyList<Error>, Task<TOut>> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return IsSuccess ? onSuccess(Value) : onFailure(Errors);
    }

    /// <summary>Execute one of two actions depending on success/failure.</summary>
    public void Switch(Action<TValue> onSuccess, Action<IReadOnlyList<Error>> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        if (IsSuccess)
        {
            onSuccess(Value);
        }
        else
        {
            onFailure(Errors);
        }
    }

    /// <summary>Execute one of two actions depending on success/failure.</summary>
    public async Task SwitchAsync(Func<TValue, Task> onSuccess, Func<IReadOnlyList<Error>, Task> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        if (IsSuccess)
        {
            await onSuccess(Value).ConfigureAwait(false);
        }
        else
        {
            await onFailure(Errors).ConfigureAwait(false);
        }
    }

    // ───────────────────- Conversion ───────────────────- //

    /// <summary>Drop the value, keeping only the success/failure state and errors.</summary>
    public Result ToResult() =>
        IsSuccess
            ? Result.Success()
            : Result.FailureUnchecked(Errors);

    // ───────────────────- Deconstruct ───────────────────- //

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

    // ───────────────────- Error querying ───────────────────- //

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

    // ───────────────────- Implicit conversions ───────────────────- //

    /// <summary>
    /// Implicitly converts a value to a Result. Non-null becomes <see cref="Success(TValue)"/>;
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

    // ───────────────────- Equality ───────────────────- //

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