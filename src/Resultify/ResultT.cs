using System.Diagnostics.CodeAnalysis;
using Resultify.Errors;

namespace Resultify;

/// <summary>
/// The outcome of an operation that returns a value of type <typeparamref name="TValue"/>.
/// A <c>readonly struct</c> — zero allocation on the success path for value types.
/// </summary>
/// <typeparam name="TValue">The type of the value carried on success.</typeparam>
/// <remarks>
/// <para>
/// A result is either a success carrying a non-null value, or a failure carrying at least one
/// <see cref="Error"/>. Both states are immutable: the error list is sealed at construction and
/// cannot be written through, so instances are safe to share across threads without
/// synchronisation and safe to copy freely (copies share one read-only error list).
/// </para>
/// <para>
/// Combinators never swallow exceptions. If a callback you pass to <see cref="Map{TNew}"/>,
/// <see cref="Bind{TNew}(Func{TValue, Result{TNew}})"/>, <see cref="Tap(Action{TValue})"/>,
/// <see cref="Ensure(Func{TValue, bool}, Error)"/>, <see cref="Match{TOut}"/> or
/// <see cref="Switch"/> throws, the exception propagates unchanged — use
/// <see cref="Try(Func{TValue}, Func{Exception, Error})"/> when you want it turned into a failure.
/// </para>
/// <para>
/// <b>Beware <c>default(Result&lt;TValue&gt;)</c>.</b> Being a struct, this type has a zero value
/// that no factory produced, and it reports <see cref="IsSuccess"/> because it carries no errors.
/// The factories guarantee a success always has a non-null value, but the default value cannot:
/// </para>
/// <list type="bullet">
///   <item><description>
///   For a value type (say <c>Result&lt;int&gt;</c>) it behaves as a success carrying
///   <c>default(TValue)</c> — e.g. <c>0</c> — which is a legitimate value.
///   </description></item>
///   <item><description>
///   For a reference type (say <c>Result&lt;string&gt;</c>) the value is null, so reading
///   <see cref="Value"/> — directly, or via any combinator that passes the value to your callback —
///   throws <see cref="InvalidOperationException"/> rather than handing you a null.
///   </description></item>
/// </list>
/// <para>
/// Prefer the factories (<see cref="Success(TValue)"/>, <see cref="Failure(Error)"/>,
/// <see cref="Create(TValue)"/>) and never hand out <c>default</c>. On the reading side,
/// <see cref="TryGetValue(out TValue)"/> and <see cref="ValueOrDefault"/> are total: neither throws
/// for any state, including <c>default</c>.
/// </para>
/// <para>
/// Every member rejects null arguments with <see cref="ArgumentNullException"/>. On the
/// <c>async</c> members that rejection surfaces as a faulted task rather than a synchronous throw,
/// because they are compiled state machines; the exception and parameter name are the same, and it
/// is observed as soon as the task is awaited.
/// </para>
/// </remarks>
public readonly struct Result<TValue> : IEquatable<Result<TValue>>
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

    /// <summary>
    /// The success value.
    /// </summary>
    /// <value>The non-null value produced by the operation.</value>
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
    /// <remarks>Total — never throws, for any state of the result.</remarks>
    public TValue? ValueOrDefault { get; }

    /// <summary>
    /// Try to retrieve the success value without throwing.
    /// Returns <c>true</c> when the result is a success with a non-null value; otherwise <c>false</c>.
    /// </summary>
    /// <param name="value">When this method returns <c>true</c>, contains the non-null success value.</param>
    /// <returns>
    /// True for a success carrying a value. False for a failure and for a
    /// <c>default(Result&lt;TValue&gt;)</c> whose value is null.
    /// </returns>
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
    /// <returns>A successful result carrying <paramref name="value"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static Result<TValue> Success(TValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new Result<TValue>(value);
    }

    /// <summary>Create a failed result from a single error.</summary>
    /// <param name="error">The error describing the failure. Must not be null.</param>
    /// <returns>A failed result carrying <paramref name="error"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="error"/> is null.</exception>
    public static Result<TValue> Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result<TValue>([error]);
    }

    /// <summary>Create a failed result from an error message.</summary>
    /// <param name="errorMessage">A human-readable description of the failure. Must not be null.</param>
    /// <returns>A failed result carrying an <see cref="Error"/> with an empty code.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="errorMessage"/> is null.</exception>
    public static Result<TValue> Failure(string errorMessage)
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
    public static Result<TValue> Failure(string code, string errorMessage)
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
    public static Result<TValue> Failure(IEnumerable<Error> errors) =>
        new(ResultHelper.ValidateErrors(errors));

    // Internal factory used by combinators to forward an already-validated, immutable error list
    // without re-running the public Failure(IEnumerable<Error>) validation. Callers must guarantee
    // the list is non-null, non-empty, contains no null elements, and is not writable through any
    // reference they retain — see ResultHelper.Seal.
    internal static Result<TValue> FailureUnchecked(IReadOnlyList<Error> errors) =>
        new(errors);

    /// <summary>
    /// Null-safe factory: returns <see cref="Success(TValue)"/> if the value is non-null,
    /// otherwise returns a failure with <see cref="Error.NullValue"/>.
    /// </summary>
    /// <param name="value">The value to wrap. May be null.</param>
    /// <returns>A success carrying <paramref name="value"/>, or a failure with <see cref="Error.NullValue"/>.</returns>
    public static Result<TValue> Create(TValue? value) =>
        value is not null
            ? Success(value)
            : Failure(Error.NullValue);

    // ───────────────────- Conditional factories ───────────────────- //

    /// <summary>Returns Success if the condition is true; otherwise Failure.</summary>
    /// <param name="condition">The condition to test.</param>
    /// <param name="value">The value to carry when <paramref name="condition"/> holds.</param>
    /// <param name="error">The error to fail with otherwise.</param>
    /// <returns>A success carrying <paramref name="value"/>, or a failure.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the taken branch's argument is null — <paramref name="value"/> when
    /// <paramref name="condition"/> holds, <paramref name="error"/> when it does not.
    /// </exception>
    public static Result<TValue> SuccessIf(bool condition, TValue value, Error error) =>
        condition
            ? Success(value)
            : Failure(error);

    /// <summary>Returns Success if the condition is true; otherwise Failure.</summary>
    /// <param name="condition">The condition to test.</param>
    /// <param name="value">The value to carry when <paramref name="condition"/> holds.</param>
    /// <param name="errorMessage">The message to fail with otherwise.</param>
    /// <returns>A success carrying <paramref name="value"/>, or a failure.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the taken branch's argument is null.</exception>
    public static Result<TValue> SuccessIf(bool condition, TValue value, string errorMessage) =>
        condition
            ? Success(value)
            : Failure(errorMessage);

    /// <summary>Returns Success if the condition is true; otherwise Failure with lazy error.</summary>
    /// <param name="condition">The condition to test.</param>
    /// <param name="value">The value to carry when <paramref name="condition"/> holds.</param>
    /// <param name="errorFactory">Produces the error. Invoked only when <paramref name="condition"/> is false.</param>
    /// <returns>A success carrying <paramref name="value"/>, or a failure.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="errorFactory"/> is null.</exception>
    public static Result<TValue> SuccessIf(bool condition, TValue value, Func<Error> errorFactory)
    {
        ArgumentNullException.ThrowIfNull(errorFactory);
        return condition
            ? Success(value)
            : Failure(errorFactory());
    }

    /// <summary>Returns Failure if the condition is true; otherwise Success.</summary>
    /// <param name="condition">The condition to test.</param>
    /// <param name="value">The value to carry when <paramref name="condition"/> does not hold.</param>
    /// <param name="error">The error to fail with when it does.</param>
    /// <returns>A failure, or a success carrying <paramref name="value"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the taken branch's argument is null.</exception>
    public static Result<TValue> FailureIf(bool condition, TValue value, Error error) =>
        condition
            ? Failure(error)
            : Success(value);

    /// <summary>Returns Failure if the condition is true; otherwise Success.</summary>
    /// <param name="condition">The condition to test.</param>
    /// <param name="value">The value to carry when <paramref name="condition"/> does not hold.</param>
    /// <param name="errorMessage">The message to fail with when it does.</param>
    /// <returns>A failure, or a success carrying <paramref name="value"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the taken branch's argument is null.</exception>
    public static Result<TValue> FailureIf(bool condition, TValue value, string errorMessage) =>
        condition
            ? Failure(errorMessage)
            : Success(value);

    /// <summary>Returns Failure if the condition is true; otherwise Success with lazy error.</summary>
    /// <param name="condition">The condition to test.</param>
    /// <param name="value">The value to carry when <paramref name="condition"/> does not hold.</param>
    /// <param name="errorFactory">Produces the error. Invoked only when <paramref name="condition"/> is true.</param>
    /// <returns>A failure, or a success carrying <paramref name="value"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="errorFactory"/> is null.</exception>
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
    /// <param name="func">The function to run. Must not be null.</param>
    /// <param name="exceptionHandler">
    /// Converts a caught exception into an <see cref="Error"/>. When null — or when it returns null —
    /// the exception is wrapped in an <see cref="ExceptionalError"/>.
    /// </param>
    /// <returns>A success carrying the returned value, or a failure.</returns>
    /// <remarks>
    /// <see cref="OperationCanceledException"/> (including <see cref="TaskCanceledException"/>) is
    /// never caught — cancellation always reaches your own handler instead of being silently turned
    /// into a failed result. An exception thrown by <paramref name="exceptionHandler"/> itself
    /// propagates and replaces the original.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="func"/> is null.</exception>
    /// <exception cref="OperationCanceledException">Rethrown as-is when the function is cancelled.</exception>
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
            return Failure(exceptionHandler?.Invoke(ex) ?? new ExceptionalError(ex));
        }
    }

    /// <summary>
    /// Executes an async function, catching exceptions as errors.
    /// A <c>null</c> return value produces a failure with <see cref="Error.NullValue"/>.
    /// </summary>
    /// <param name="func">The asynchronous function to run. Must not be null.</param>
    /// <param name="exceptionHandler">
    /// Converts a caught exception into an <see cref="Error"/>. When null — or when it returns null —
    /// the exception is wrapped in an <see cref="ExceptionalError"/>.
    /// </param>
    /// <returns>A success carrying the returned value, or a failure.</returns>
    /// <inheritdoc cref="Try(Func{TValue}, Func{Exception, Error})" path="/remarks"/>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="func"/> is null.</exception>
    /// <exception cref="OperationCanceledException">Rethrown as-is when the function is cancelled.</exception>
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
            return Failure(exceptionHandler?.Invoke(ex) ?? new ExceptionalError(ex));
        }
    }

    /// <summary>Execute a function returning a <see cref="Result{TValue}"/>, catching exceptions.</summary>
    /// <param name="func">The function to run. Must not be null.</param>
    /// <param name="exceptionHandler">
    /// Converts a caught exception into an <see cref="Error"/>. When null — or when it returns null —
    /// the exception is wrapped in an <see cref="ExceptionalError"/>.
    /// </param>
    /// <returns>
    /// The result <paramref name="func"/> returned — failures it produces itself are passed through
    /// untouched — or a failure describing the exception it threw.
    /// </returns>
    /// <inheritdoc cref="Try(Func{TValue}, Func{Exception, Error})" path="/remarks"/>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="func"/> is null.</exception>
    /// <exception cref="OperationCanceledException">Rethrown as-is when the function is cancelled.</exception>
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
            return Failure(exceptionHandler?.Invoke(ex) ?? new ExceptionalError(ex));
        }
    }

    /// <summary>Execute an async function returning a <see cref="Result{TValue}"/>, catching exceptions.</summary>
    /// <param name="func">The asynchronous function to run. Must not be null.</param>
    /// <param name="exceptionHandler">
    /// Converts a caught exception into an <see cref="Error"/>. When null — or when it returns null —
    /// the exception is wrapped in an <see cref="ExceptionalError"/>.
    /// </param>
    /// <returns>The result <paramref name="func"/> returned, or a failure describing the exception it threw.</returns>
    /// <inheritdoc cref="Try(Func{TValue}, Func{Exception, Error})" path="/remarks"/>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="func"/> is null.</exception>
    /// <exception cref="OperationCanceledException">Rethrown as-is when the function is cancelled.</exception>
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
            return Failure(exceptionHandler?.Invoke(ex) ?? new ExceptionalError(ex));
        }
    }

    // ───────────────────- Combinators ───────────────────- //

    /// <summary>
    /// Transforms the value if successful. Errors are propagated unchanged.
    /// A <c>null</c> mapped value produces a failure with <see cref="Error.NullValue"/>.
    /// </summary>
    /// <typeparam name="TNew">The transformed value type.</typeparam>
    /// <param name="mapper">The transformation. Invoked only when this result is successful.</param>
    /// <returns>A success carrying the mapped value, or this result's errors unchanged.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="mapper"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when invoked on a <c>default(Result&lt;TValue&gt;)</c> over a reference type, whose
    /// value is null — see the remarks on <see cref="Result{TValue}"/>.
    /// </exception>
    public Result<TNew> Map<TNew>(Func<TValue, TNew> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        return IsSuccess
            ? Result<TNew>.Create(mapper(Value))
            : Result<TNew>.FailureUnchecked(Errors);
    }

    /// <summary>
    /// Async <see cref="Map{TNew}"/>. A <c>null</c> mapped value produces a failure with <see cref="Error.NullValue"/>.
    /// </summary>
    /// <typeparam name="TNew">The transformed value type.</typeparam>
    /// <param name="mapper">The transformation. Invoked only when this result is successful.</param>
    /// <returns>A success carrying the mapped value, or this result's errors unchanged.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="mapper"/> is null.</exception>
    /// <inheritdoc cref="Map{TNew}" path="/exception[@cref='T:System.InvalidOperationException']"/>
    public async Task<Result<TNew>> MapAsync<TNew>(Func<TValue, Task<TNew>> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        return IsSuccess
            ? Result<TNew>.Create(await mapper(Value).ConfigureAwait(false))
            : Result<TNew>.FailureUnchecked(Errors);
    }

    /// <summary>Chain a dependent operation that also returns a Result.</summary>
    /// <typeparam name="TNew">The value type produced by <paramref name="bind"/>.</typeparam>
    /// <param name="bind">The next step. Invoked only when this result is successful.</param>
    /// <returns>The result of <paramref name="bind"/>, or this result's errors unchanged.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="bind"/> is null.</exception>
    /// <inheritdoc cref="Map{TNew}" path="/exception[@cref='T:System.InvalidOperationException']"/>
    public Result<TNew> Bind<TNew>(Func<TValue, Result<TNew>> bind)
    {
        ArgumentNullException.ThrowIfNull(bind);
        return IsSuccess
            ? bind(Value)
            : Result<TNew>.FailureUnchecked(Errors);
    }

    /// <summary>Async <see cref="Bind{TNew}(Func{TValue, Result{TNew}})"/>.</summary>
    /// <typeparam name="TNew">The value type produced by <paramref name="bind"/>.</typeparam>
    /// <param name="bind">The next step. Invoked only when this result is successful.</param>
    /// <returns>The result of <paramref name="bind"/>, or this result's errors unchanged.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="bind"/> is null.</exception>
    /// <inheritdoc cref="Map{TNew}" path="/exception[@cref='T:System.InvalidOperationException']"/>
    public Task<Result<TNew>> BindAsync<TNew>(Func<TValue, Task<Result<TNew>>> bind)
    {
        ArgumentNullException.ThrowIfNull(bind);
        return IsSuccess
            ? bind(Value)
            : Task.FromResult(Result<TNew>.FailureUnchecked(Errors));
    }

    /// <summary>Bind to a non-generic Result (e.g. for void operations).</summary>
    /// <param name="bind">The next step. Invoked only when this result is successful.</param>
    /// <returns>The result of <paramref name="bind"/>, or this result's errors unchanged.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="bind"/> is null.</exception>
    /// <inheritdoc cref="Map{TNew}" path="/exception[@cref='T:System.InvalidOperationException']"/>
    public Result Bind(Func<TValue, Result> bind)
    {
        ArgumentNullException.ThrowIfNull(bind);
        return IsSuccess
            ? bind(Value)
            : Result.FailureUnchecked(Errors);
    }

    /// <summary>Async <see cref="Bind(Func{TValue, Result})"/>.</summary>
    /// <param name="bind">The next step. Invoked only when this result is successful.</param>
    /// <returns>The result of <paramref name="bind"/>, or this result's errors unchanged.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="bind"/> is null.</exception>
    /// <inheritdoc cref="Map{TNew}" path="/exception[@cref='T:System.InvalidOperationException']"/>
    public Task<Result> BindAsync(Func<TValue, Task<Result>> bind)
    {
        ArgumentNullException.ThrowIfNull(bind);
        return IsSuccess
            ? bind(Value)
            : Task.FromResult(Result.FailureUnchecked(Errors));
    }

    /// <summary>Execute a side effect if successful. Returns this result unchanged.</summary>
    /// <param name="action">The side effect, given the value. Invoked only when this result is successful.</param>
    /// <returns>This result, unchanged.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is null.</exception>
    /// <inheritdoc cref="Map{TNew}" path="/exception[@cref='T:System.InvalidOperationException']"/>
    public Result<TValue> Tap(Action<TValue> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (IsSuccess)
        {
            action(Value);
        }

        return this;
    }

    /// <summary>Async <see cref="Tap(Action{TValue})"/>.</summary>
    /// <param name="action">The side effect, given the value. Invoked only when this result is successful.</param>
    /// <returns>This result, unchanged.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is null.</exception>
    /// <inheritdoc cref="Map{TNew}" path="/exception[@cref='T:System.InvalidOperationException']"/>
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
    /// <param name="action">The side effect, given all errors. Invoked only when this result failed.</param>
    /// <returns>This result, unchanged.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is null.</exception>
    public Result<TValue> TapError(Action<IReadOnlyList<Error>> action)
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
    /// <param name="predicate">The gate, given the value. Invoked only when this result is successful.</param>
    /// <param name="error">The error to fail with when <paramref name="predicate"/> returns false.</param>
    /// <returns>This result when the gate passes or it had already failed; otherwise a failure carrying <paramref name="error"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="predicate"/> or <paramref name="error"/> is null.</exception>
    /// <inheritdoc cref="Map{TNew}" path="/exception[@cref='T:System.InvalidOperationException']"/>
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
    /// <param name="predicate">The gate, given the value. Invoked only when this result is successful.</param>
    /// <param name="errorMessage">The message to fail with when <paramref name="predicate"/> returns false.</param>
    /// <returns>This result when the gate passes or it had already failed; otherwise a failure.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="predicate"/> or <paramref name="errorMessage"/> is null.</exception>
    /// <inheritdoc cref="Map{TNew}" path="/exception[@cref='T:System.InvalidOperationException']"/>
    public Result<TValue> Ensure(Func<TValue, bool> predicate, string errorMessage)
    {
        ArgumentNullException.ThrowIfNull(errorMessage);
        return Ensure(predicate, new Error(errorMessage));
    }

    /// <summary>Add a validation gate with a lazy error factory.</summary>
    /// <param name="predicate">The gate, given the value. Invoked only when this result is successful.</param>
    /// <param name="errorFactory">Produces the error from the value. Invoked only when the gate fails.</param>
    /// <returns>This result when the gate passes or it had already failed; otherwise the produced failure.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="predicate"/> or <paramref name="errorFactory"/> is null.</exception>
    /// <inheritdoc cref="Map{TNew}" path="/exception[@cref='T:System.InvalidOperationException']"/>
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
    /// <typeparam name="TOut">The type both branches produce.</typeparam>
    /// <param name="onSuccess">Invoked with the value when this result is successful.</param>
    /// <param name="onFailure">Invoked with all errors when this result failed.</param>
    /// <returns>Whatever the taken branch returned.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="onSuccess"/> or <paramref name="onFailure"/> is null.</exception>
    /// <inheritdoc cref="Map{TNew}" path="/exception[@cref='T:System.InvalidOperationException']"/>
    public TOut Match<TOut>(Func<TValue, TOut> onSuccess, Func<IReadOnlyList<Error>, TOut> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return IsSuccess ? onSuccess(Value) : onFailure(Errors);
    }

    /// <summary>Async <see cref="Match{TOut}"/>.</summary>
    /// <typeparam name="TOut">The type both branches produce.</typeparam>
    /// <param name="onSuccess">Invoked with the value when this result is successful.</param>
    /// <param name="onFailure">Invoked with all errors when this result failed.</param>
    /// <returns>Whatever the taken branch returned.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="onSuccess"/> or <paramref name="onFailure"/> is null.</exception>
    /// <inheritdoc cref="Map{TNew}" path="/exception[@cref='T:System.InvalidOperationException']"/>
    public Task<TOut> MatchAsync<TOut>(Func<TValue, Task<TOut>> onSuccess, Func<IReadOnlyList<Error>, Task<TOut>> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return IsSuccess ? onSuccess(Value) : onFailure(Errors);
    }

    /// <summary>Execute one of two actions depending on success/failure.</summary>
    /// <param name="onSuccess">Invoked with the value when this result is successful.</param>
    /// <param name="onFailure">Invoked with all errors when this result failed.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="onSuccess"/> or <paramref name="onFailure"/> is null.</exception>
    /// <inheritdoc cref="Map{TNew}" path="/exception[@cref='T:System.InvalidOperationException']"/>
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

    /// <summary>Async <see cref="Switch"/>.</summary>
    /// <param name="onSuccess">Invoked with the value when this result is successful.</param>
    /// <param name="onFailure">Invoked with all errors when this result failed.</param>
    /// <returns>A task that completes when the taken branch has completed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="onSuccess"/> or <paramref name="onFailure"/> is null.</exception>
    /// <inheritdoc cref="Map{TNew}" path="/exception[@cref='T:System.InvalidOperationException']"/>
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
    /// <returns>A non-generic result with the same state and errors.</returns>
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

    /// <summary>
    /// Implicitly converts a value to a Result. Non-null becomes <see cref="Success(TValue)"/>;
    /// null becomes a failure with <see cref="Error.NullValue"/>.
    /// </summary>
    /// <param name="value">The value to wrap. May be null.</param>
    /// <remarks>
    /// This operator and the <see cref="Error"/> one below overlap for some type arguments:
    /// <list type="bullet">
    ///   <item><description>
    ///   When an <see cref="Error"/> is assignable to <typeparamref name="TValue"/> but is not
    ///   <see cref="Error"/> itself (e.g. <c>Result&lt;object&gt;</c>), the <see cref="Error"/>
    ///   operator is the more specific match and wins, so the result is a <em>failure</em>. Call
    ///   <see cref="Success(TValue)"/> explicitly to store the error as a value instead.
    ///   </description></item>
    ///   <item><description>
    ///   When <typeparamref name="TValue"/> is exactly <see cref="Error"/>, the two operators become
    ///   indistinguishable and the compiler rejects the conversion outright with
    ///   <c>CS0457: Ambiguous user defined conversions</c>. <c>Result&lt;Error&gt;</c> is therefore
    ///   usable only through the explicit factories (<see cref="Success(TValue)"/>,
    ///   <see cref="Failure(Error)"/>), never through assignment.
    ///   </description></item>
    /// </list>
    /// </remarks>
    public static implicit operator Result<TValue>(TValue? value) =>
        Create(value);

    /// <summary>Implicitly convert a single <see cref="Error"/> to a failed Result.</summary>
    /// <param name="error">The error to wrap. Must not be null.</param>
    /// <remarks>See the value operator above for how the two overlapping operators resolve.</remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="error"/> is null.</exception>
    public static implicit operator Result<TValue>(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return Failure(error);
    }

    // ───────────────────- Equality ───────────────────- //

    /// <summary>
    /// Two results are equal when they share a success state and either equal values (compared with
    /// <see cref="EqualityComparer{T}.Default"/>) or an equal sequence of errors.
    /// </summary>
    /// <param name="other">The result to compare with.</param>
    /// <returns>True when the two results are equal.</returns>
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

    /// <summary>Equality operator — returns true when both results have the same state and contents.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>True when the two results are equal.</returns>
    public static bool operator ==(Result<TValue> left, Result<TValue> right) =>
        left.Equals(right);

    /// <summary>Inequality operator — negation of <c>==</c>.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>True when the two results differ.</returns>
    public static bool operator !=(Result<TValue> left, Result<TValue> right) =>
        !left.Equals(right);

    /// <summary>Returns a human-readable representation of the result for logs and debugging.</summary>
    /// <returns>
    /// <c>"Result&lt;T&gt;: Success (value)"</c> or <c>"Result&lt;T&gt;: Failure (…)"</c> listing every error.
    /// Never throws, even for a <c>default(Result&lt;TValue&gt;)</c> whose value is null.
    /// </returns>
    public override string ToString() =>
        IsSuccess
            ? $"Result<{typeof(TValue).Name}>: Success ({ValueOrDefault?.ToString() ?? "null"})"
            : $"Result<{typeof(TValue).Name}>: Failure ({string.Join("; ", Errors)})";
}
