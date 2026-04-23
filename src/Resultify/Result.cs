using System.Diagnostics.CodeAnalysis;
using Resultify.Errors;

namespace Resultify;

/// <summary>
/// Represents the outcome of an operation that has no return value.
/// Value type — zero-allocation on the success path.
/// Instances are immutable and safe to share across threads.
/// </summary>
public readonly struct Result : IEquatable<Result>
{
    // Shared empty list so reading Errors on a successful result is allocation-free.
    private static readonly IReadOnlyList<Error> EmptyErrors = [];

    private readonly IReadOnlyList<Error>? _errors;

    /// <summary>All errors. Empty when successful.</summary>
    public IReadOnlyList<Error> Errors =>
        _errors ?? EmptyErrors;

    /// <summary>
    /// The first error, or <see cref="Error.None"/> if successful.
    /// Use <see cref="Errors"/> when you need all of them (e.g. validation aggregation).
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

    // ── Factory methods ──────────────────────────────────────

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
    public static Result Failure(string errorMessage) =>
        Failure(new Error(errorMessage));

    /// <summary>Create a failed result with a code and message.</summary>
    public static Result Failure(string code, string errorMessage) =>
        Failure(new Error(code, errorMessage));

    /// <summary>Create a failed result from multiple errors.</summary>
    /// <param name="errors">The errors to include. Must be non-null and contain at least one element.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="errors"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="errors"/> is empty.</exception>
    public static Result Failure(IEnumerable<Error> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        Error[] array = errors.ToArray();
        return array.Length == 0
            ? throw new ArgumentException("At least one error is required.", nameof(errors))
            : new Result(array);
    }

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

    // ── Conditional factories ────────────────────────────────

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
    public static Result SuccessIf(bool condition, Func<Error> errorFactory) =>
        condition
            ? Success()
            : Failure(errorFactory());

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
    public static Result FailureIf(bool condition, Func<Error> errorFactory) =>
        condition
            ? Failure(errorFactory())
            : Success();

    // ── Try ──────────────────────────────────────────────────

    /// <summary>Execute an action, catching exceptions as errors.</summary>
    public static Result Try(Action action, Func<Exception, Error>? exceptionHandler = null)
    {
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
            Error error = exceptionHandler?.Invoke(ex) ?? new ExceptionalError(ex);
            return Failure(error);
        }
    }

    /// <summary>Execute an async action, catching exceptions as errors.</summary>
    public static async Task<Result> TryAsync(Func<Task> action, Func<Exception, Error>? exceptionHandler = null)
    {
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
            Error error = exceptionHandler?.Invoke(ex) ?? new ExceptionalError(ex);
            return Failure(error);
        }
    }

    /// <summary>Execute a function returning a Result, catching exceptions.</summary>
    public static Result Try(Func<Result> func, Func<Exception, Error>? exceptionHandler = null)
    {
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

    // ── Merge ────────────────────────────────────────────────

    /// <summary>Merge multiple results into one. Fails if any input failed.</summary>
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
            : new Result(errors.ToArray());
    }

    // ── Combinators ──────────────────────────────────────────

    /// <summary>
    /// If this result is successful, execute <paramref name="bind"/> and return its result.
    /// If this result is failed, propagate the errors without executing <paramref name="bind"/>.
    /// </summary>
    public Result Bind(Func<Result> bind) =>
        IsSuccess
            ? bind()
            : this;

    /// <summary>Async Bind.</summary>
    public Task<Result> BindAsync(Func<Task<Result>> bind) =>
        IsSuccess
            ? bind()
            : Task.FromResult(this);

    /// <summary>
    /// If successful, execute <paramref name="bind"/> returning a <see cref="Result{TValue}"/>.
    /// </summary>
    public Result<TValue> Bind<TValue>(Func<Result<TValue>> bind) =>
        IsSuccess
            ? bind()
            : Result<TValue>.Failure(Errors);

    /// <summary>Async Bind to Result&lt;TValue&gt;.</summary>
    public Task<Result<TValue>> BindAsync<TValue>(Func<Task<Result<TValue>>> bind) =>
        IsSuccess
            ? bind()
            : Task.FromResult(Result<TValue>.Failure(Errors));

    /// <summary>Execute a side effect if successful. Returns this result unchanged.</summary>
    public Result Tap(Action action)
    {
        if (IsSuccess)
        {
            action();
        }

        return this;
    }

    /// <summary>Async Tap.</summary>
    public async Task<Result> TapAsync(Func<Task> action)
    {
        if (IsSuccess)
        {
            await action().ConfigureAwait(false);
        }

        return this;
    }

    /// <summary>Execute a side effect if failed. Returns this result unchanged.</summary>
    public Result TapError(Action<IReadOnlyList<Error>> action)
    {
        if (IsFailure)
        {
            action(Errors);
        }

        return this;
    }

    /// <summary>Add a validation gate. Evaluates the predicate only if currently successful.</summary>
    public Result Ensure(Func<bool> predicate, Error error) =>
        IsFailure
            ? this
            : predicate()
                ? this
                : Failure(error);

    /// <summary>Add a validation gate with a lazy error factory.</summary>
    public Result Ensure(Func<bool> predicate, Func<Error> errorFactory) =>
        IsFailure
            ? this
            : predicate()
                ? this
                : Failure(errorFactory());

    /// <summary>Add a validation gate with a string error message.</summary>
    public Result Ensure(Func<bool> predicate, string errorMessage) =>
        Ensure(predicate, new Error(errorMessage));

    /// <summary>
    /// Pattern match: execute <paramref name="onSuccess"/> or <paramref name="onFailure"/> and return the produced value.
    /// </summary>
    public TOut Match<TOut>(Func<TOut> onSuccess, Func<IReadOnlyList<Error>, TOut> onFailure) =>
        IsSuccess
            ? onSuccess()
            : onFailure(Errors);

    /// <summary>Async Match.</summary>
    public Task<TOut> MatchAsync<TOut>(Func<Task<TOut>> onSuccess, Func<IReadOnlyList<Error>, Task<TOut>> onFailure) =>
        IsSuccess
            ? onSuccess()
            : onFailure(Errors);

    /// <summary>Execute one of two actions depending on success/failure.</summary>
    public void Switch(Action onSuccess, Action<IReadOnlyList<Error>> onFailure)
    {
        if (IsSuccess)
        {
            onSuccess();
        }
        else
        {
            onFailure(Errors);
        }
    }

    // ── Conversion ───────────────────────────────────────────

    /// <summary>Convert to a <see cref="Result{TValue}"/> by supplying a value (success) or propagating errors.</summary>
    public Result<TValue> ToResult<TValue>(TValue value) =>
        IsSuccess
            ? Result<TValue>.Success(value)
            : Result<TValue>.Failure(Errors);

    // ── Deconstruct ──────────────────────────────────────────

    /// <summary>Deconstruct into <paramref name="isSuccess"/> and the full list of errors.</summary>
    /// <param name="isSuccess">True when the result is successful.</param>
    /// <param name="errors">All errors. Empty when successful.</param>
    public void Deconstruct(out bool isSuccess, out IReadOnlyList<Error> errors)
    {
        isSuccess = IsSuccess;
        errors = Errors;
    }

    // ── Implicit conversions ─────────────────────────────────

    /// <summary>Implicitly convert a single <see cref="Error"/> to a failed <see cref="Result"/>.</summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="error"/> is null.</exception>
    public static implicit operator Result(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return Failure(error);
    }

    /// <summary>Implicitly convert a list of <see cref="Error"/>s to a failed <see cref="Result"/>.</summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="errors"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="errors"/> is empty.</exception>
    public static implicit operator Result(List<Error> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        return Failure(errors);
    }

    // ── Equality ─────────────────────────────────────────────

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