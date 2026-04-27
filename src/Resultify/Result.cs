using System.Diagnostics.CodeAnalysis;
using Resultify.Errors;

namespace Resultify;

/// <summary>
/// The outcome of an operation with no return value.
/// A <c>readonly struct</c> — zero allocation on the success path.
/// </summary>
public readonly struct Result : IEquatable<Result>
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

    private Result(IReadOnlyList<Error>? errors)
    {
        _errors = errors;
    }

    // ───────────────────- Factory methods ───────────────────- //

    /// <summary>Create a successful result.</summary>
    public static Result Success() =>
        new(null);

    /// <summary>Create a failed result from a single error.</summary>
    public static Result Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result([error]);
    }

    /// <summary>Create a failed result from an error message.</summary>
    public static Result Failure(string errorMessage)
    {
        ArgumentNullException.ThrowIfNull(errorMessage);
        return Failure(new Error(errorMessage));
    }

    /// <summary>Create a failed result with a code and message.</summary>
    public static Result Failure(string code, string errorMessage)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(errorMessage);
        return Failure(new Error(code, errorMessage));
    }

    /// <summary>Create a failed result from multiple errors.</summary>
    /// <param name="errors">The errors to include. Must be non-null, contain at least one element, and no null elements.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="errors"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="errors"/> is empty or contains null elements.</exception>
    public static Result Failure(IEnumerable<Error> errors) =>
        FailureUnchecked(ResultHelper.ValidateErrors(errors));

    // Internal factory used by combinators to forward an already-validated, immutable error list
    // without re-running the public Failure(IEnumerable<Error>) validation. Callers must guarantee
    // the list is non-null, non-empty, contains no null elements, and will not be mutated afterwards.
    internal static Result FailureUnchecked(IReadOnlyList<Error> errors) =>
        new(errors);

    /// <summary>Create a successful <see cref="Result{TValue}"/> with the given value.</summary>
    public static Result<TValue> Success<TValue>(TValue value) =>
        Result<TValue>.Success(value);

    /// <summary>Create a failed <see cref="Result{TValue}"/>.</summary>
    public static Result<TValue> Failure<TValue>(Error error) =>
        Result<TValue>.Failure(error);

    /// <summary>Create a failed <see cref="Result{TValue}"/>.</summary>
    public static Result<TValue> Failure<TValue>(string errorMessage) =>
        Result<TValue>.Failure(errorMessage);

    /// <summary>
    /// Null-safe factory: returns <see cref="Result{TValue}.Success(TValue)"/> if the value is non-null,
    /// otherwise returns a failure with <see cref="Error.NullValue"/>.
    /// </summary>
    public static Result<TValue> Create<TValue>(TValue? value) =>
        value is not null
            ? Result<TValue>.Success(value)
            : Result<TValue>.Failure(Error.NullValue);

    // ───────────────────- Conditional factories ───────────────────- //

    /// <summary>Returns Success if the condition is true; otherwise Failure.</summary>
    public static Result SuccessIf(bool condition, Error error) =>
        condition
            ? Success()
            : Failure(error);

    /// <summary>Returns Success if the condition is true; otherwise Failure.</summary>
    public static Result SuccessIf(bool condition, string errorMessage) =>
        condition
            ? Success()
            : Failure(errorMessage);

    /// <summary>Returns Success if the condition is true; otherwise Failure with lazy error.</summary>
    public static Result SuccessIf(bool condition, Func<Error> errorFactory)
    {
        ArgumentNullException.ThrowIfNull(errorFactory);
        return condition
            ? Success()
            : Failure(errorFactory());
    }

    /// <summary>Returns Failure if the condition is true; otherwise Success.</summary>
    public static Result FailureIf(bool condition, Error error) =>
        condition
            ? Failure(error)
            : Success();

    /// <summary>Returns Failure if the condition is true; otherwise Success.</summary>
    public static Result FailureIf(bool condition, string errorMessage) =>
        condition
            ? Failure(errorMessage)
            : Success();

    /// <summary>Returns Failure if the condition is true; otherwise Success with lazy error.</summary>
    public static Result FailureIf(bool condition, Func<Error> errorFactory)
    {
        ArgumentNullException.ThrowIfNull(errorFactory);
        return condition
            ? Failure(errorFactory())
            : Success();
    }

    // ───────────────────- Try ───────────────────- //

    /// <summary>Execute an action, catching exceptions as errors.</summary>
    public static Result Try(Action action, Func<Exception, Error>? exceptionHandler = null)
    {
        ArgumentNullException.ThrowIfNull(action);
        try
        {
            action();
            return Success();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Failure(exceptionHandler?.Invoke(ex) ?? new ExceptionalError(ex));
        }
    }

    /// <summary>Execute an async action, catching exceptions as errors.</summary>
    public static async Task<Result> TryAsync(Func<Task> action, Func<Exception, Error>? exceptionHandler = null)
    {
        ArgumentNullException.ThrowIfNull(action);
        try
        {
            await action().ConfigureAwait(false);
            return Success();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Failure(exceptionHandler?.Invoke(ex) ?? new ExceptionalError(ex));
        }
    }

    /// <summary>Execute a function returning a Result, catching exceptions.</summary>
    public static Result Try(Func<Result> func, Func<Exception, Error>? exceptionHandler = null)
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
            return Failure(exceptionHandler?.Invoke(ex) ?? new ExceptionalError(ex));
        }
    }

    /// <summary>Execute an async function returning a Result, catching exceptions.</summary>
    public static async Task<Result> TryAsync(Func<Task<Result>> func, Func<Exception, Error>? exceptionHandler = null)
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
            return Failure(exceptionHandler?.Invoke(ex) ?? new ExceptionalError(ex));
        }
    }

    // ───────────────────- Merge ───────────────────- //

    /// <summary>Merges multiple results into one. Returns failure if any input failed, aggregating all errors.</summary>
    public static Result Merge(params ReadOnlySpan<Result> results)
    {
        List<Error>? errors = null;
        foreach (Result r in results)
        {
            if (!r.IsFailure)
            {
                continue;
            }

            errors ??= [];
            errors.AddRange(r.Errors);
        }

        // Materialize as an array so the mutable List is not exposed through the public IReadOnlyList<Error>.
        return errors is null
            ? Success()
            : FailureUnchecked(errors.ToArray());
    }

    // ───────────────────- Combinators ───────────────────- //

    /// <summary>If successful, executes <paramref name="bind"/> and returns its result. Propagates errors on failure.</summary>
    public Result Bind(Func<Result> bind)
    {
        ArgumentNullException.ThrowIfNull(bind);
        return IsSuccess
            ? bind()
            : this;
    }

    /// <summary>Async Bind.</summary>
    public Task<Result> BindAsync(Func<Task<Result>> bind)
    {
        ArgumentNullException.ThrowIfNull(bind);
        return IsSuccess
            ? bind()
            : Task.FromResult(this);
    }

    /// <summary>If successful, executes <paramref name="bind"/> returning a <see cref="Result{TValue}"/>. Propagates errors on failure.</summary>
    public Result<TValue> Bind<TValue>(Func<Result<TValue>> bind)
    {
        ArgumentNullException.ThrowIfNull(bind);
        return IsSuccess
            ? bind()
            : Result<TValue>.FailureUnchecked(Errors);
    }

    /// <summary>Async Bind to Result&lt;TValue&gt;.</summary>
    public Task<Result<TValue>> BindAsync<TValue>(Func<Task<Result<TValue>>> bind)
    {
        ArgumentNullException.ThrowIfNull(bind);
        return IsSuccess
            ? bind()
            : Task.FromResult(Result<TValue>.FailureUnchecked(Errors));
    }

    /// <summary>Execute a side effect if successful. Returns this result unchanged.</summary>
    public Result Tap(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (IsSuccess)
        {
            action();
        }

        return this;
    }

    /// <summary>Async Tap.</summary>
    public async Task<Result> TapAsync(Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (IsSuccess)
        {
            await action().ConfigureAwait(false);
        }

        return this;
    }

    /// <summary>Execute a side effect if failed. Returns this result unchanged.</summary>
    public Result TapError(Action<IReadOnlyList<Error>> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (IsFailure)
        {
            action(Errors);
        }

        return this;
    }

    /// <summary>Async TapError.</summary>
    public async Task<Result> TapErrorAsync(Func<IReadOnlyList<Error>, Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (IsFailure)
        {
            await action(Errors).ConfigureAwait(false);
        }

        return this;
    }

    /// <summary>Add a validation gate. Evaluates the predicate only if currently successful.</summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="predicate"/> or <paramref name="error"/> is null.</exception>
    public Result Ensure(Func<bool> predicate, Error error)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(error);
        return IsFailure
            ? this
            : predicate()
                ? this
                : Failure(error);
    }

    /// <summary>Add a validation gate with a lazy error factory.</summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="predicate"/> or <paramref name="errorFactory"/> is null.</exception>
    public Result Ensure(Func<bool> predicate, Func<Error> errorFactory)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(errorFactory);
        return IsFailure
            ? this
            : predicate()
                ? this
                : Failure(errorFactory());
    }

    /// <summary>Add a validation gate with a string error message.</summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="predicate"/> or <paramref name="errorMessage"/> is null.</exception>
    public Result Ensure(Func<bool> predicate, string errorMessage)
    {
        ArgumentNullException.ThrowIfNull(errorMessage);
        return Ensure(predicate, new Error(errorMessage));
    }

    /// <summary>Executes <paramref name="onSuccess"/> or <paramref name="onFailure"/> and returns the produced value.</summary>
    public TOut Match<TOut>(Func<TOut> onSuccess, Func<IReadOnlyList<Error>, TOut> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return IsSuccess ? onSuccess() : onFailure(Errors);
    }

    /// <summary>Async Match.</summary>
    public Task<TOut> MatchAsync<TOut>(Func<Task<TOut>> onSuccess, Func<IReadOnlyList<Error>, Task<TOut>> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return IsSuccess ? onSuccess() : onFailure(Errors);
    }

    /// <summary>Execute one of two actions depending on success/failure.</summary>
    public void Switch(Action onSuccess, Action<IReadOnlyList<Error>> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        if (IsSuccess)
        {
            onSuccess();
        }
        else
        {
            onFailure(Errors);
        }
    }

    /// <summary>Execute one of two actions depending on success/failure.</summary>
    public async Task SwitchAsync(Func<Task> onSuccess, Func<IReadOnlyList<Error>, Task> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        if (IsSuccess)
        {
            await onSuccess().ConfigureAwait(false);
        }
        else
        {
            await onFailure(Errors).ConfigureAwait(false);
        }
    }

    // ───────────────────- Conversion ───────────────────- //

    /// <summary>Converts to <see cref="Result{TValue}"/> by supplying a value on success or propagating errors on failure.</summary>
    public Result<TValue> ToResult<TValue>(TValue value) =>
        IsSuccess
            ? Result<TValue>.Success(value)
            : Result<TValue>.FailureUnchecked(Errors);

    // ── Deconstruct ──────────────────────────────────────────

    /// <summary>Deconstruct into <paramref name="isSuccess"/> and the full list of errors.</summary>
    /// <param name="isSuccess">True when the result is successful.</param>
    /// <param name="errors">All errors. Empty when successful.</param>
    public void Deconstruct(out bool isSuccess, out IReadOnlyList<Error> errors)
    {
        isSuccess = IsSuccess;
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

    /// <summary>Implicitly convert a single <see cref="Error"/> to a failed <see cref="Result"/>.</summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="error"/> is null.</exception>
    public static implicit operator Result(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return Failure(error);
    }

    // ───────────────────- Equality

    /// <inheritdoc />
    public bool Equals(Result other) =>
        IsSuccess == other.IsSuccess && Errors.SequenceEqual(other.Errors);

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is Result other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        if (IsSuccess)
        {
            return HashCode.Combine(true);
        }

        var hc = new HashCode();
        hc.Add(false);
        foreach (Error e in Errors)
        {
            hc.Add(e);
        }

        return hc.ToHashCode();
    }

    /// <summary>Equality operator — returns true when both results have the same success state and errors.</summary>
    public static bool operator ==(Result left, Result right) =>
        left.Equals(right);

    /// <summary>Inequality operator — negation of <c>==</c>.</summary>
    public static bool operator !=(Result left, Result right) =>
        !left.Equals(right);

    /// <summary>Returns a human-readable representation of the result for logs and debugging.</summary>
    public override string ToString() =>
        IsSuccess
            ? "Result: Success"
            : $"Result: Failure ({string.Join("; ", Errors)})";
}