using System.Diagnostics.CodeAnalysis;
using Resultify.Errors;

namespace Resultify;

/// <summary>
/// The outcome of an operation with no return value.
/// A <c>readonly struct</c> — zero allocation on the success path.
/// </summary>
/// <remarks>
/// <para>
/// A result is either a success carrying no errors, or a failure carrying at least one
/// <see cref="Error"/>. Both states are immutable: the error list is sealed at construction and
/// cannot be written through, so instances are safe to share across threads without
/// synchronisation and safe to copy freely (copies share one read-only error list).
/// </para>
/// <para>
/// Combinators never swallow exceptions. If a callback you pass to <see cref="Bind(Func{Result})"/>,
/// <see cref="Tap(Action)"/>, <see cref="Ensure(Func{bool}, Error)"/>, <see cref="Match{TOut}"/> or
/// <see cref="Switch"/> throws, the exception propagates to the caller unchanged — use
/// <see cref="Try(Action, Func{Exception, Error})"/> when you want an exception turned into a failure.
/// </para>
/// <para>
/// Because this is a struct, <c>default(Result)</c> is reachable and reports
/// <see cref="IsSuccess"/> — a result with no errors is a success by definition, so this is
/// well defined rather than a trap.
/// </para>
/// <para>
/// Every member rejects null arguments with <see cref="ArgumentNullException"/>. On the
/// <c>async</c> members that rejection surfaces as a faulted task rather than a synchronous throw,
/// because they are compiled state machines; the exception and parameter name are the same, and it
/// is observed as soon as the task is awaited.
/// </para>
/// </remarks>
public readonly struct Result : IEquatable<Result>
{
    private readonly IReadOnlyList<Error>? _errors;

    /// <summary>All errors. Empty when successful.</summary>
    /// <value>
    /// A read-only, immutable list. Successful results share one empty instance, so reading this
    /// on a success never allocates.
    /// </value>
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
    /// <returns>A result with no errors.</returns>
    public static Result Success() =>
        new(null);

    /// <summary>Create a failed result from a single error.</summary>
    /// <param name="error">The error describing the failure. Must not be null.</param>
    /// <returns>A failed result carrying <paramref name="error"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="error"/> is null.</exception>
    public static Result Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result([error]);
    }

    /// <summary>Create a failed result from an error message.</summary>
    /// <param name="errorMessage">A human-readable description of the failure. Must not be null.</param>
    /// <returns>A failed result carrying an <see cref="Error"/> with an empty code.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="errorMessage"/> is null.</exception>
    public static Result Failure(string errorMessage)
    {
        ArgumentNullException.ThrowIfNull(errorMessage);
        return Failure(new Error(errorMessage));
    }

    /// <summary>Create a failed result with a code and message.</summary>
    /// <param name="code">A stable, machine-readable identifier, e.g. <c>"User.NotFound"</c>. Must not be null.</param>
    /// <param name="errorMessage">A human-readable description of the failure. Must not be null.</param>
    /// <returns>A failed result carrying an <see cref="Error"/> with the given code and message.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="code"/> or <paramref name="errorMessage"/> is null.
    /// </exception>
    public static Result Failure(string code, string errorMessage)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(errorMessage);
        return Failure(new Error(code, errorMessage));
    }

    /// <summary>Create a failed result from multiple errors.</summary>
    /// <param name="errors">The errors to include. Must be non-null, contain at least one element, and no null elements.</param>
    /// <returns>A failed result carrying a private, read-only copy of <paramref name="errors"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="errors"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="errors"/> is empty or contains null elements.</exception>
    public static Result Failure(IEnumerable<Error> errors) =>
        FailureUnchecked(ResultHelper.ValidateErrors(errors));

    // Internal factory used by combinators to forward an already-validated, immutable error list
    // without re-running the public Failure(IEnumerable<Error>) validation. Callers must guarantee
    // the list is non-null, non-empty, contains no null elements, and is not writable through any
    // reference they retain — see ResultHelper.Seal.
    internal static Result FailureUnchecked(IReadOnlyList<Error> errors) =>
        new(errors);

    /// <summary>Create a successful <see cref="Result{TValue}"/> with the given value.</summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="value">The value to wrap. Must not be null.</param>
    /// <returns>A successful result carrying <paramref name="value"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static Result<TValue> Success<TValue>(TValue value) =>
        Result<TValue>.Success(value);

    /// <summary>Create a failed <see cref="Result{TValue}"/>.</summary>
    /// <typeparam name="TValue">The value type the failed result stands in for.</typeparam>
    /// <param name="error">The error describing the failure. Must not be null.</param>
    /// <returns>A failed result carrying <paramref name="error"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="error"/> is null.</exception>
    public static Result<TValue> Failure<TValue>(Error error) =>
        Result<TValue>.Failure(error);

    /// <summary>Create a failed <see cref="Result{TValue}"/>.</summary>
    /// <typeparam name="TValue">The value type the failed result stands in for.</typeparam>
    /// <param name="errorMessage">A human-readable description of the failure. Must not be null.</param>
    /// <returns>A failed result carrying an <see cref="Error"/> with an empty code.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="errorMessage"/> is null.</exception>
    public static Result<TValue> Failure<TValue>(string errorMessage) =>
        Result<TValue>.Failure(errorMessage);

    /// <summary>
    /// Null-safe factory: returns <see cref="Result{TValue}.Success(TValue)"/> if the value is non-null,
    /// otherwise returns a failure with <see cref="Error.NullValue"/>.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="value">The value to wrap. May be null.</param>
    /// <returns>A success carrying <paramref name="value"/>, or a failure with <see cref="Error.NullValue"/>.</returns>
    public static Result<TValue> Create<TValue>(TValue? value) =>
        value is not null
            ? Result<TValue>.Success(value)
            : Result<TValue>.Failure(Error.NullValue);

    // ───────────────────- Conditional factories ───────────────────- //

    /// <summary>Returns Success if the condition is true; otherwise Failure.</summary>
    /// <param name="condition">The condition to test.</param>
    /// <param name="error">The error to fail with when <paramref name="condition"/> is false.</param>
    /// <returns>A success when <paramref name="condition"/> holds; otherwise a failure.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="condition"/> is false and <paramref name="error"/> is null.
    /// </exception>
    public static Result SuccessIf(bool condition, Error error) =>
        condition
            ? Success()
            : Failure(error);

    /// <summary>Returns Success if the condition is true; otherwise Failure.</summary>
    /// <param name="condition">The condition to test.</param>
    /// <param name="errorMessage">The message to fail with when <paramref name="condition"/> is false.</param>
    /// <returns>A success when <paramref name="condition"/> holds; otherwise a failure.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="condition"/> is false and <paramref name="errorMessage"/> is null.
    /// </exception>
    public static Result SuccessIf(bool condition, string errorMessage) =>
        condition
            ? Success()
            : Failure(errorMessage);

    /// <summary>Returns Success if the condition is true; otherwise Failure with lazy error.</summary>
    /// <param name="condition">The condition to test.</param>
    /// <param name="errorFactory">
    /// Produces the error. Invoked only when <paramref name="condition"/> is false, so building an
    /// expensive error costs nothing on the success path.
    /// </param>
    /// <returns>A success when <paramref name="condition"/> holds; otherwise a failure.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="errorFactory"/> is null.</exception>
    public static Result SuccessIf(bool condition, Func<Error> errorFactory)
    {
        ArgumentNullException.ThrowIfNull(errorFactory);
        return condition
            ? Success()
            : Failure(errorFactory());
    }

    /// <summary>Returns Failure if the condition is true; otherwise Success.</summary>
    /// <param name="condition">The condition to test.</param>
    /// <param name="error">The error to fail with when <paramref name="condition"/> is true.</param>
    /// <returns>A failure when <paramref name="condition"/> holds; otherwise a success.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="condition"/> is true and <paramref name="error"/> is null.
    /// </exception>
    public static Result FailureIf(bool condition, Error error) =>
        condition
            ? Failure(error)
            : Success();

    /// <summary>Returns Failure if the condition is true; otherwise Success.</summary>
    /// <param name="condition">The condition to test.</param>
    /// <param name="errorMessage">The message to fail with when <paramref name="condition"/> is true.</param>
    /// <returns>A failure when <paramref name="condition"/> holds; otherwise a success.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="condition"/> is true and <paramref name="errorMessage"/> is null.
    /// </exception>
    public static Result FailureIf(bool condition, string errorMessage) =>
        condition
            ? Failure(errorMessage)
            : Success();

    /// <summary>Returns Failure if the condition is true; otherwise Success with lazy error.</summary>
    /// <param name="condition">The condition to test.</param>
    /// <param name="errorFactory">
    /// Produces the error. Invoked only when <paramref name="condition"/> is true.
    /// </param>
    /// <returns>A failure when <paramref name="condition"/> holds; otherwise a success.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="errorFactory"/> is null.</exception>
    public static Result FailureIf(bool condition, Func<Error> errorFactory)
    {
        ArgumentNullException.ThrowIfNull(errorFactory);
        return condition
            ? Failure(errorFactory())
            : Success();
    }

    // ───────────────────- Try ───────────────────- //

    /// <summary>Execute an action, catching exceptions as errors.</summary>
    /// <param name="action">The action to run. Must not be null.</param>
    /// <param name="exceptionHandler">
    /// Converts a caught exception into an <see cref="Error"/>. When null — or when it returns null —
    /// the exception is wrapped in an <see cref="ExceptionalError"/>.
    /// </param>
    /// <returns>A success if <paramref name="action"/> returned normally; otherwise a failure.</returns>
    /// <remarks>
    /// <see cref="OperationCanceledException"/> (including <see cref="TaskCanceledException"/>) is
    /// never caught — cancellation always reaches your own handler instead of being silently turned
    /// into a failed result. An exception thrown by <paramref name="exceptionHandler"/> itself
    /// propagates and replaces the original.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is null.</exception>
    /// <exception cref="OperationCanceledException">Rethrown as-is when the action is cancelled.</exception>
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
    /// <param name="action">The asynchronous action to run. Must not be null.</param>
    /// <param name="exceptionHandler">
    /// Converts a caught exception into an <see cref="Error"/>. When null — or when it returns null —
    /// the exception is wrapped in an <see cref="ExceptionalError"/>.
    /// </param>
    /// <returns>A success if <paramref name="action"/> completed; otherwise a failure.</returns>
    /// <inheritdoc cref="Try(Action, Func{Exception, Error})" path="/remarks"/>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is null.</exception>
    /// <exception cref="OperationCanceledException">Rethrown as-is when the action is cancelled.</exception>
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
    /// <param name="func">The function to run. Must not be null.</param>
    /// <param name="exceptionHandler">
    /// Converts a caught exception into an <see cref="Error"/>. When null — or when it returns null —
    /// the exception is wrapped in an <see cref="ExceptionalError"/>.
    /// </param>
    /// <returns>
    /// The result <paramref name="func"/> returned — failures it produces itself are passed through
    /// untouched — or a failure describing the exception it threw.
    /// </returns>
    /// <inheritdoc cref="Try(Action, Func{Exception, Error})" path="/remarks"/>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="func"/> is null.</exception>
    /// <exception cref="OperationCanceledException">Rethrown as-is when the function is cancelled.</exception>
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
    /// <param name="func">The asynchronous function to run. Must not be null.</param>
    /// <param name="exceptionHandler">
    /// Converts a caught exception into an <see cref="Error"/>. When null — or when it returns null —
    /// the exception is wrapped in an <see cref="ExceptionalError"/>.
    /// </param>
    /// <returns>
    /// The result <paramref name="func"/> returned, or a failure describing the exception it threw.
    /// </returns>
    /// <inheritdoc cref="Try(Action, Func{Exception, Error})" path="/remarks"/>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="func"/> is null.</exception>
    /// <exception cref="OperationCanceledException">Rethrown as-is when the function is cancelled.</exception>
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
    /// <param name="results">The results to merge. Merging nothing yields a success.</param>
    /// <returns>
    /// A success when every input succeeded; otherwise a failure carrying every error from every
    /// failed input, in argument order.
    /// </returns>
    public static Result Merge(params ReadOnlySpan<Result> results) =>
        ResultHelper.MergeAll(results);

    // ───────────────────- Combinators ───────────────────- //

    /// <summary>If successful, executes <paramref name="bind"/> and returns its result. Propagates errors on failure.</summary>
    /// <param name="bind">The next step. Invoked only when this result is successful.</param>
    /// <returns>The result of <paramref name="bind"/>, or this result's errors unchanged.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="bind"/> is null.</exception>
    public Result Bind(Func<Result> bind)
    {
        ArgumentNullException.ThrowIfNull(bind);
        return IsSuccess
            ? bind()
            : this;
    }

    /// <summary>Async <see cref="Bind(Func{Result})"/>.</summary>
    /// <param name="bind">The next step. Invoked only when this result is successful.</param>
    /// <returns>The result of <paramref name="bind"/>, or this result's errors unchanged.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="bind"/> is null.</exception>
    public Task<Result> BindAsync(Func<Task<Result>> bind)
    {
        ArgumentNullException.ThrowIfNull(bind);
        return IsSuccess
            ? bind()
            : Task.FromResult(this);
    }

    /// <summary>If successful, executes <paramref name="bind"/> returning a <see cref="Result{TValue}"/>. Propagates errors on failure.</summary>
    /// <typeparam name="TValue">The value type produced by <paramref name="bind"/>.</typeparam>
    /// <param name="bind">The next step. Invoked only when this result is successful.</param>
    /// <returns>The result of <paramref name="bind"/>, or this result's errors unchanged.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="bind"/> is null.</exception>
    public Result<TValue> Bind<TValue>(Func<Result<TValue>> bind)
    {
        ArgumentNullException.ThrowIfNull(bind);
        return IsSuccess
            ? bind()
            : Result<TValue>.FailureUnchecked(Errors);
    }

    /// <summary>Async <see cref="Bind{TValue}(Func{Result{TValue}})"/>.</summary>
    /// <typeparam name="TValue">The value type produced by <paramref name="bind"/>.</typeparam>
    /// <param name="bind">The next step. Invoked only when this result is successful.</param>
    /// <returns>The result of <paramref name="bind"/>, or this result's errors unchanged.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="bind"/> is null.</exception>
    public Task<Result<TValue>> BindAsync<TValue>(Func<Task<Result<TValue>>> bind)
    {
        ArgumentNullException.ThrowIfNull(bind);
        return IsSuccess
            ? bind()
            : Task.FromResult(Result<TValue>.FailureUnchecked(Errors));
    }

    /// <summary>Execute a side effect if successful. Returns this result unchanged.</summary>
    /// <param name="action">The side effect. Invoked only when this result is successful.</param>
    /// <returns>This result, unchanged.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is null.</exception>
    public Result Tap(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (IsSuccess)
        {
            action();
        }

        return this;
    }

    /// <summary>Async <see cref="Tap(Action)"/>.</summary>
    /// <param name="action">The side effect. Invoked only when this result is successful.</param>
    /// <returns>This result, unchanged.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is null.</exception>
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
    /// <param name="action">The side effect, given all errors. Invoked only when this result failed.</param>
    /// <returns>This result, unchanged.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is null.</exception>
    public Result TapError(Action<IReadOnlyList<Error>> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (IsFailure)
        {
            action(Errors);
        }

        return this;
    }

    /// <summary>Async <see cref="TapError(Action{IReadOnlyList{Error}})"/>.</summary>
    /// <param name="action">The side effect, given all errors. Invoked only when this result failed.</param>
    /// <returns>This result, unchanged.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is null.</exception>
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
    /// <param name="predicate">The gate. Invoked only when this result is successful.</param>
    /// <param name="error">The error to fail with when <paramref name="predicate"/> returns false.</param>
    /// <returns>This result when the gate passes or it had already failed; otherwise a failure carrying <paramref name="error"/>.</returns>
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
    /// <param name="predicate">The gate. Invoked only when this result is successful.</param>
    /// <param name="errorFactory">Produces the error. Invoked only when the gate fails.</param>
    /// <returns>This result when the gate passes or it had already failed; otherwise the produced failure.</returns>
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
    /// <param name="predicate">The gate. Invoked only when this result is successful.</param>
    /// <param name="errorMessage">The message to fail with when <paramref name="predicate"/> returns false.</param>
    /// <returns>This result when the gate passes or it had already failed; otherwise a failure.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="predicate"/> or <paramref name="errorMessage"/> is null.</exception>
    public Result Ensure(Func<bool> predicate, string errorMessage)
    {
        ArgumentNullException.ThrowIfNull(errorMessage);
        return Ensure(predicate, new Error(errorMessage));
    }

    /// <summary>Executes <paramref name="onSuccess"/> or <paramref name="onFailure"/> and returns the produced value.</summary>
    /// <typeparam name="TOut">The type both branches produce.</typeparam>
    /// <param name="onSuccess">Invoked when this result is successful.</param>
    /// <param name="onFailure">Invoked with all errors when this result failed.</param>
    /// <returns>Whatever the taken branch returned.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="onSuccess"/> or <paramref name="onFailure"/> is null.</exception>
    public TOut Match<TOut>(Func<TOut> onSuccess, Func<IReadOnlyList<Error>, TOut> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return IsSuccess ? onSuccess() : onFailure(Errors);
    }

    /// <summary>Async <see cref="Match{TOut}"/>.</summary>
    /// <typeparam name="TOut">The type both branches produce.</typeparam>
    /// <param name="onSuccess">Invoked when this result is successful.</param>
    /// <param name="onFailure">Invoked with all errors when this result failed.</param>
    /// <returns>Whatever the taken branch returned.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="onSuccess"/> or <paramref name="onFailure"/> is null.</exception>
    public Task<TOut> MatchAsync<TOut>(Func<Task<TOut>> onSuccess, Func<IReadOnlyList<Error>, Task<TOut>> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return IsSuccess ? onSuccess() : onFailure(Errors);
    }

    /// <summary>Execute one of two actions depending on success/failure.</summary>
    /// <param name="onSuccess">Invoked when this result is successful.</param>
    /// <param name="onFailure">Invoked with all errors when this result failed.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="onSuccess"/> or <paramref name="onFailure"/> is null.</exception>
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

    /// <summary>Async <see cref="Switch"/>.</summary>
    /// <param name="onSuccess">Invoked when this result is successful.</param>
    /// <param name="onFailure">Invoked with all errors when this result failed.</param>
    /// <returns>A task that completes when the taken branch has completed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="onSuccess"/> or <paramref name="onFailure"/> is null.</exception>
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
    /// <typeparam name="TValue">The type of the value to attach.</typeparam>
    /// <param name="value">The value the successful result should carry. Must not be null when this result is successful.</param>
    /// <returns>A success carrying <paramref name="value"/>, or this result's errors unchanged.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when this result is successful and <paramref name="value"/> is null. Use
    /// <see cref="Create{TValue}(TValue)"/> beforehand if null should become a failure instead.
    /// </exception>
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
    /// <typeparam name="TError">The error type to look for. Subtypes count as matches.</typeparam>
    /// <returns>True when at least one error is a <typeparamref name="TError"/>; false on a success.</returns>
    public bool HasError<TError>() where TError : Error =>
        Errors.OfType<TError>().Any();

    /// <summary>Check if the result contains an error of the specified type matching a predicate.</summary>
    /// <typeparam name="TError">The error type to look for. Subtypes count as matches.</typeparam>
    /// <param name="predicate">The condition the matching error must satisfy.</param>
    /// <returns>True when at least one <typeparamref name="TError"/> satisfies <paramref name="predicate"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="predicate"/> is null.</exception>
    public bool HasError<TError>(Func<TError, bool> predicate) where TError : Error =>
        Errors.OfType<TError>().Any(predicate);

    /// <summary>Check if the result contains an error with the specified code.</summary>
    /// <param name="code">The exact, case-sensitive <see cref="Error.Code"/> to look for.</param>
    /// <returns>True when at least one error carries that code; false on a success.</returns>
    public bool HasErrorCode(string code) =>
        Errors.Any(e => e.Code == code);

    /// <summary>Check if any error in the result was caused by a specific exception type.</summary>
    /// <typeparam name="TException">The exception type to look for. Subtypes count as matches.</typeparam>
    /// <returns>True when an <see cref="ExceptionalError"/> wraps a <typeparamref name="TException"/>.</returns>
    public bool HasException<TException>() where TException : Exception =>
        Errors.OfType<ExceptionalError>().Any(e => e.Exception is TException);

    // ───────────────────- Implicit conversions ───────────────────- //

    /// <summary>Implicitly convert a single <see cref="Error"/> to a failed <see cref="Result"/>.</summary>
    /// <param name="error">The error to wrap. Must not be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="error"/> is null.</exception>
    public static implicit operator Result(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return Failure(error);
    }

    // ───────────────────- Equality ───────────────────- //

    /// <summary>
    /// Two results are equal when they share a success state and, for failures, an equal sequence of
    /// errors. All successes are equal to each other.
    /// </summary>
    /// <param name="other">The result to compare with.</param>
    /// <returns>True when the two results are equal.</returns>
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
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>True when the two results are equal.</returns>
    public static bool operator ==(Result left, Result right) =>
        left.Equals(right);

    /// <summary>Inequality operator — negation of <c>==</c>.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>True when the two results differ.</returns>
    public static bool operator !=(Result left, Result right) =>
        !left.Equals(right);

    /// <summary>Returns a human-readable representation of the result for logs and debugging.</summary>
    /// <returns><c>"Result: Success"</c>, or <c>"Result: Failure (…)"</c> listing every error.</returns>
    public override string ToString() =>
        IsSuccess
            ? "Result: Success"
            : $"Result: Failure ({string.Join("; ", Errors)})";
}
