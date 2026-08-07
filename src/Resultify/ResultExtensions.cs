using Resultify.Errors;

namespace Resultify;

/// <summary>
/// Extension methods for <see cref="Result"/> and <see cref="Result{TValue}"/>.
/// </summary>
/// <remarks>
/// <para>
/// Beyond <c>Merge</c>, this class mirrors the combinators of both result types onto
/// <see cref="Task{TResult}"/> so an async pipeline reads as one chain without an <c>await</c>
/// between every step:
/// </para>
/// <code>
/// Result&lt;OrderId&gt; result = await GetCustomerAsync(id)
///     .Ensure(c =&gt; c.IsActive, CustomerErrors.Inactive)
///     .BindAsync(c =&gt; CreateOrderAsync(c))
///     .Map(order =&gt; order.Id);
/// </code>
/// <para>
/// Each awaits the antecedent task and then delegates to the identically named instance method, so
/// behaviour — including which callbacks run, and every exception documented there — is exactly the
/// same as the synchronous form. Every continuation uses <c>ConfigureAwait(false)</c>.
/// </para>
/// <para>
/// The mirror is deliberately not exhaustive. Three instance members have no task-based counterpart:
/// <see cref="Result.Ensure(Func{bool}, string)"/> (use the <see cref="Error"/> or factory overload),
/// <see cref="Result.ToResult{TValue}(TValue)"/> and <see cref="Result{TValue}.ToResult()"/>.
/// Await the pipeline and call the instance method for those:
/// <c>(await pipeline).ToResult()</c>.
/// </para>
/// <para>
/// One more limitation is worth knowing before you reach for it. On a <c>Task&lt;Result&lt;T&gt;&gt;</c>
/// receiver, a member that needs an explicit type argument cannot be called in fluent form, because
/// the extension block contributes its own type parameter and C# will not infer only some of them.
/// This affects <c>HasError&lt;TError&gt;()</c> and <c>HasException&lt;TException&gt;()</c>; both
/// compile as <c>(await pipeline).HasError&lt;ValidationError&gt;()</c>. The same members on a
/// <c>Task&lt;Result&gt;</c> receiver are unaffected, and <c>HasErrorCode</c> — which takes no type
/// argument — works everywhere.
/// </para>
/// </remarks>
public static class ResultExtensions
{
    // ───────────────────- Merge for collections ───────────────────- //

    /// <summary>Merge a collection of results into a single result.</summary>
    /// <param name="results">The results to merge. An empty collection merges to a success.</param>
    /// <returns>
    /// A success when every input succeeded; otherwise a failure carrying every error from every
    /// failed input, in enumeration order.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="results"/> is null.</exception>
    public static Result Merge(this IEnumerable<Result> results) =>
        ResultHelper.MergeAll(results);

    /// <summary>
    /// Merges a collection of <see cref="Result{TValue}"/> into a single result.
    /// Returns all values when every result succeeded, or all errors when any failed.
    /// Once a failure is seen, collected values are discarded and only errors are aggregated.
    /// </summary>
    /// <typeparam name="TValue">The value type of the results being merged.</typeparam>
    /// <param name="results">The results to merge. An empty collection merges to a success with an empty value list.</param>
    /// <returns>
    /// A success holding every value in enumeration order, or a failure carrying every error from
    /// every failed input.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="results"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a successful input carries a null value, which only a
    /// <c>default(Result&lt;TValue&gt;)</c> over a reference type can do. See <see cref="Result{TValue}.Value"/>.
    /// </exception>
    public static Result<IReadOnlyList<TValue>> Merge<TValue>(this IEnumerable<Result<TValue>> results)
    {
        ArgumentNullException.ThrowIfNull(results);
        List<TValue>? values = null;
        List<Error>? errors = null;

        foreach (Result<TValue> result in results)
        {
            if (result.IsFailure)
            {
                (errors ??= []).AddRange(result.Errors);
                values = null; // drop any collected values — we're going to fail
                continue;
            }

            if (errors is not null)
            {
                continue; // already failing; ignore successful values
            }

            (values ??= []).Add(result.Value);
        }

        // Errors come from already-validated failed Results, so they need sealing but not revalidating.
        return errors is not null
            ? Result<IReadOnlyList<TValue>>.FailureUnchecked(ResultHelper.Seal([.. errors]))
            : Result<IReadOnlyList<TValue>>.Success(values?.AsReadOnly() ?? (IReadOnlyList<TValue>)[]);
    }

    // ── Async pipeline helpers ───────────────────────────────

    extension<TValue>(Task<Result<TValue>> resultTask)
    {
        /// <summary>Map over an async Result pipeline.</summary>
        /// <typeparam name="TNew">The transformed value type.</typeparam>
        /// <param name="mapper">The transformation, applied to the awaited value on success.</param>
        /// <returns>The mapped result.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the task or <paramref name="mapper"/> is null.</exception>
        public async Task<Result<TNew>> Map<TNew>(Func<TValue, TNew> mapper)
        {
            ArgumentNullException.ThrowIfNull(resultTask);

            Result<TValue> result = await resultTask.ConfigureAwait(false);
            return result.Map(mapper);
        }

        /// <summary>Map over an async Result pipeline with an async mapper.</summary>
        /// <typeparam name="TNew">The transformed value type.</typeparam>
        /// <param name="mapper">The asynchronous transformation, applied to the awaited value on success.</param>
        /// <returns>The mapped result.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the task or <paramref name="mapper"/> is null.</exception>
        public async Task<Result<TNew>> MapAsync<TNew>(Func<TValue, Task<TNew>> mapper)
        {
            ArgumentNullException.ThrowIfNull(resultTask);

            Result<TValue> result = await resultTask.ConfigureAwait(false);
            return await result.MapAsync(mapper).ConfigureAwait(false);
        }

        /// <summary>Bind over an async Result pipeline.</summary>
        /// <typeparam name="TNew">The value type produced by <paramref name="bind"/>.</typeparam>
        /// <param name="bind">The next step, applied to the awaited value on success.</param>
        /// <returns>The bound result.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the task or <paramref name="bind"/> is null.</exception>
        public async Task<Result<TNew>> Bind<TNew>(Func<TValue, Result<TNew>> bind)
        {
            ArgumentNullException.ThrowIfNull(resultTask);

            Result<TValue> result = await resultTask.ConfigureAwait(false);
            return result.Bind(bind);
        }

        /// <summary>Bind over an async Result pipeline with async bind function.</summary>
        /// <typeparam name="TNew">The value type produced by <paramref name="bind"/>.</typeparam>
        /// <param name="bind">The asynchronous next step, applied to the awaited value on success.</param>
        /// <returns>The bound result.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the task or <paramref name="bind"/> is null.</exception>
        public async Task<Result<TNew>> BindAsync<TNew>(Func<TValue, Task<Result<TNew>>> bind)
        {
            ArgumentNullException.ThrowIfNull(resultTask);

            Result<TValue> result = await resultTask.ConfigureAwait(false);
            return await result.BindAsync(bind).ConfigureAwait(false);
        }

        /// <summary>Tap over an async Result pipeline.</summary>
        /// <param name="action">The side effect, given the value on success.</param>
        /// <returns>The awaited result, unchanged.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the task or <paramref name="action"/> is null.</exception>
        public async Task<Result<TValue>> Tap(Action<TValue> action)
        {
            ArgumentNullException.ThrowIfNull(resultTask);

            Result<TValue> result = await resultTask.ConfigureAwait(false);
            return result.Tap(action);
        }

        /// <summary>Tap over an async Result pipeline with an async action.</summary>
        /// <param name="action">The asynchronous side effect, given the value on success.</param>
        /// <returns>The awaited result, unchanged.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the task or <paramref name="action"/> is null.</exception>
        public async Task<Result<TValue>> TapAsync(Func<TValue, Task> action)
        {
            ArgumentNullException.ThrowIfNull(resultTask);

            Result<TValue> result = await resultTask.ConfigureAwait(false);
            return await result.TapAsync(action).ConfigureAwait(false);
        }

        /// <summary>Ensure over an async Result pipeline.</summary>
        /// <param name="predicate">The gate, given the value on success.</param>
        /// <param name="error">The error to fail with when the gate rejects the value.</param>
        /// <returns>The gated result.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the task, <paramref name="predicate"/> or <paramref name="error"/> is null.</exception>
        public async Task<Result<TValue>> Ensure(Func<TValue, bool> predicate, Error error)
        {
            ArgumentNullException.ThrowIfNull(resultTask);

            Result<TValue> result = await resultTask.ConfigureAwait(false);
            return result.Ensure(predicate, error);
        }

        /// <summary>Ensure over an async Result pipeline with string error.</summary>
        /// <param name="predicate">The gate, given the value on success.</param>
        /// <param name="errorMessage">The message to fail with when the gate rejects the value.</param>
        /// <returns>The gated result.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the task, <paramref name="predicate"/> or <paramref name="errorMessage"/> is null.</exception>
        public async Task<Result<TValue>> Ensure(Func<TValue, bool> predicate, string errorMessage)
        {
            ArgumentNullException.ThrowIfNull(resultTask);

            Result<TValue> result = await resultTask.ConfigureAwait(false);
            return result.Ensure(predicate, errorMessage);
        }

        /// <summary>Ensure over an async Result pipeline with a lazy error factory.</summary>
        /// <param name="predicate">The gate, given the value on success.</param>
        /// <param name="errorFactory">Produces the error from the value. Invoked only when the gate fails.</param>
        /// <returns>The gated result.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the task, <paramref name="predicate"/> or <paramref name="errorFactory"/> is null.</exception>
        public async Task<Result<TValue>> Ensure(Func<TValue, bool> predicate, Func<TValue, Error> errorFactory)
        {
            ArgumentNullException.ThrowIfNull(resultTask);

            Result<TValue> result = await resultTask.ConfigureAwait(false);
            return result.Ensure(predicate, errorFactory);
        }

        /// <summary>Match over an async Result pipeline.</summary>
        /// <typeparam name="TOut">The type both branches produce.</typeparam>
        /// <param name="onSuccess">Invoked with the value when the awaited result is successful.</param>
        /// <param name="onFailure">Invoked with all errors when the awaited result failed.</param>
        /// <returns>Whatever the taken branch returned.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the task or either branch is null.</exception>
        public async Task<TOut> Match<TOut>(Func<TValue, TOut> onSuccess, Func<IReadOnlyList<Error>, TOut> onFailure)
        {
            ArgumentNullException.ThrowIfNull(resultTask);

            Result<TValue> result = await resultTask.ConfigureAwait(false);
            return result.Match(onSuccess, onFailure);
        }

        /// <summary>Async Match over an async Result pipeline.</summary>
        /// <typeparam name="TOut">The type both branches produce.</typeparam>
        /// <param name="onSuccess">Invoked with the value when the awaited result is successful.</param>
        /// <param name="onFailure">Invoked with all errors when the awaited result failed.</param>
        /// <returns>Whatever the taken branch returned.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the task or either branch is null.</exception>
        public async Task<TOut> MatchAsync<TOut>(Func<TValue, Task<TOut>> onSuccess, Func<IReadOnlyList<Error>, Task<TOut>> onFailure)
        {
            ArgumentNullException.ThrowIfNull(resultTask);

            Result<TValue> result = await resultTask.ConfigureAwait(false);
            return await result.MatchAsync(onSuccess, onFailure).ConfigureAwait(false);
        }

        /// <summary>TapError over an async Result pipeline.</summary>
        /// <param name="action">The side effect, given all errors on failure.</param>
        /// <returns>The awaited result, unchanged.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the task or <paramref name="action"/> is null.</exception>
        public async Task<Result<TValue>> TapError(Action<IReadOnlyList<Error>> action)
        {
            ArgumentNullException.ThrowIfNull(resultTask);

            Result<TValue> result = await resultTask.ConfigureAwait(false);
            return result.TapError(action);
        }

        /// <summary>TapError over an async Result pipeline with an async action.</summary>
        /// <param name="action">The asynchronous side effect, given all errors on failure.</param>
        /// <returns>The awaited result, unchanged.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the task or <paramref name="action"/> is null.</exception>
        public async Task<Result<TValue>> TapErrorAsync(Func<IReadOnlyList<Error>, Task> action)
        {
            ArgumentNullException.ThrowIfNull(resultTask);

            Result<TValue> result = await resultTask.ConfigureAwait(false);
            return await result.TapErrorAsync(action).ConfigureAwait(false);
        }

        /// <summary>Switch over an async Result pipeline.</summary>
        /// <param name="onSuccess">Invoked with the value when the awaited result is successful.</param>
        /// <param name="onFailure">Invoked with all errors when the awaited result failed.</param>
        /// <returns>A task that completes when the taken branch has run.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the task or either branch is null.</exception>
        public async Task Switch(Action<TValue> onSuccess, Action<IReadOnlyList<Error>> onFailure)
        {
            ArgumentNullException.ThrowIfNull(resultTask);

            Result<TValue> result = await resultTask.ConfigureAwait(false);
            result.Switch(onSuccess, onFailure);
        }

        /// <summary>SwitchAsync over an async Result pipeline.</summary>
        /// <param name="onSuccess">Invoked with the value when the awaited result is successful.</param>
        /// <param name="onFailure">Invoked with all errors when the awaited result failed.</param>
        /// <returns>A task that completes when the taken branch has completed.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the task or either branch is null.</exception>
        public async Task SwitchAsync(Func<TValue, Task> onSuccess, Func<IReadOnlyList<Error>, Task> onFailure)
        {
            ArgumentNullException.ThrowIfNull(resultTask);

            Result<TValue> result = await resultTask.ConfigureAwait(false);
            await result.SwitchAsync(onSuccess, onFailure).ConfigureAwait(false);
        }

        /// <summary>Bind to non-generic Result over an async Result pipeline.</summary>
        /// <param name="bind">The next step, applied to the awaited value on success.</param>
        /// <returns>The bound result, without a value.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the task or <paramref name="bind"/> is null.</exception>
        public async Task<Result> Bind(Func<TValue, Result> bind)
        {
            ArgumentNullException.ThrowIfNull(resultTask);

            Result<TValue> result = await resultTask.ConfigureAwait(false);
            return result.Bind(bind);
        }

        /// <summary>Async Bind to non-generic Result over an async Result pipeline.</summary>
        /// <param name="bind">The asynchronous next step, applied to the awaited value on success.</param>
        /// <returns>The bound result, without a value.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the task or <paramref name="bind"/> is null.</exception>
        public async Task<Result> BindAsync(Func<TValue, Task<Result>> bind)
        {
            ArgumentNullException.ThrowIfNull(resultTask);

            Result<TValue> result = await resultTask.ConfigureAwait(false);
            return await result.BindAsync(bind).ConfigureAwait(false);
        }

        /// <summary>Check if the awaited result contains an error of the specified type.</summary>
        /// <typeparam name="TError">The error type to look for. Subtypes count as matches.</typeparam>
        /// <returns>True when at least one error is a <typeparamref name="TError"/>.</returns>
        /// <remarks>
        /// <b>Not callable in fluent form.</b> This extension block declares its own
        /// <typeparamref name="TValue"/>, so the member effectively takes two type parameters, and
        /// C# has no partial type-argument inference: <c>pipeline.HasError&lt;ValidationError&gt;()</c>
        /// does not compile (CS1929). Await first — <c>(await pipeline).HasError&lt;ValidationError&gt;()</c>
        /// — or supply both arguments in static form,
        /// <c>ResultExtensions.HasError&lt;int, ValidationError&gt;(pipeline)</c>.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when the task is null.</exception>
        public async Task<bool> HasError<TError>() where TError : Error
        {
            ArgumentNullException.ThrowIfNull(resultTask);

            Result<TValue> result = await resultTask.ConfigureAwait(false);
            return result.HasError<TError>();
        }

        /// <summary>Check if the awaited result contains an error of the specified type matching a predicate.</summary>
        /// <typeparam name="TError">The error type to look for. Subtypes count as matches.</typeparam>
        /// <param name="predicate">The condition the matching error must satisfy.</param>
        /// <returns>True when at least one <typeparamref name="TError"/> satisfies <paramref name="predicate"/>.</returns>
        /// <remarks>
        /// Give the lambda an explicit parameter type so <typeparamref name="TError"/> is inferred and
        /// no type-argument list is needed:
        /// <c>pipeline.HasError((ValidationError e) =&gt; e.PropertyName == "Email")</c>. Spelling the
        /// type argument out instead does not compile, for the reason given on the overload above.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when the task or <paramref name="predicate"/> is null.</exception>
        public async Task<bool> HasError<TError>(Func<TError, bool> predicate) where TError : Error
        {
            ArgumentNullException.ThrowIfNull(resultTask);

            Result<TValue> result = await resultTask.ConfigureAwait(false);
            return result.HasError(predicate);
        }

        /// <summary>Check if the awaited result contains an error with the specified code.</summary>
        /// <param name="code">The exact, case-sensitive <see cref="Error.Code"/> to look for.</param>
        /// <returns>True when at least one error carries that code.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the task is null.</exception>
        public async Task<bool> HasErrorCode(string code)
        {
            ArgumentNullException.ThrowIfNull(resultTask);

            Result<TValue> result = await resultTask.ConfigureAwait(false);
            return result.HasErrorCode(code);
        }

        /// <summary>Check if the awaited result contains an error caused by the specified exception type.</summary>
        /// <typeparam name="TException">The exception type to look for. Subtypes count as matches.</typeparam>
        /// <returns>True when an <see cref="ExceptionalError"/> wraps a <typeparamref name="TException"/>.</returns>
        /// <remarks>
        /// <b>Not callable in fluent form</b>, for the same reason as the parameterless
        /// <c>HasError&lt;TError&gt;()</c> above: await first, or use the static call form.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when the task is null.</exception>
        public async Task<bool> HasException<TException>() where TException : Exception
        {
            ArgumentNullException.ThrowIfNull(resultTask);

            Result<TValue> result = await resultTask.ConfigureAwait(false);
            return result.HasException<TException>();
        }
    }

    extension(Task<Result> resultTask)
    {
        /// <summary>Match over an async non-generic Result pipeline.</summary>
        /// <typeparam name="TOut">The type both branches produce.</typeparam>
        /// <param name="onSuccess">Invoked when the awaited result is successful.</param>
        /// <param name="onFailure">Invoked with all errors when the awaited result failed.</param>
        /// <returns>Whatever the taken branch returned.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the task or either branch is null.</exception>
        public async Task<TOut> Match<TOut>(Func<TOut> onSuccess,
            Func<IReadOnlyList<Error>, TOut> onFailure)
        {
            ArgumentNullException.ThrowIfNull(resultTask);

            Result result = await resultTask.ConfigureAwait(false);
            return result.Match(onSuccess, onFailure);
        }

        /// <summary>Async Match over an async non-generic Result pipeline.</summary>
        /// <typeparam name="TOut">The type both branches produce.</typeparam>
        /// <param name="onSuccess">Invoked when the awaited result is successful.</param>
        /// <param name="onFailure">Invoked with all errors when the awaited result failed.</param>
        /// <returns>Whatever the taken branch returned.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the task or either branch is null.</exception>
        public async Task<TOut> MatchAsync<TOut>(
            Func<Task<TOut>> onSuccess,
            Func<IReadOnlyList<Error>, Task<TOut>> onFailure)
        {
            ArgumentNullException.ThrowIfNull(resultTask);

            Result result = await resultTask.ConfigureAwait(false);
            return await result.MatchAsync(onSuccess, onFailure).ConfigureAwait(false);
        }

        /// <summary>Bind over an async non-generic Result pipeline.</summary>
        /// <param name="bind">The next step. Invoked only when the awaited result is successful.</param>
        /// <returns>The bound result.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the task or <paramref name="bind"/> is null.</exception>
        public async Task<Result> Bind(Func<Result> bind)
        {
            ArgumentNullException.ThrowIfNull(resultTask);

            Result result = await resultTask.ConfigureAwait(false);
            return result.Bind(bind);
        }

        /// <summary>Bind async over an async non-generic Result pipeline.</summary>
        /// <param name="bind">The asynchronous next step. Invoked only when the awaited result is successful.</param>
        /// <returns>The bound result.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the task or <paramref name="bind"/> is null.</exception>
        public async Task<Result> BindAsync(Func<Task<Result>> bind)
        {
            ArgumentNullException.ThrowIfNull(resultTask);

            Result result = await resultTask.ConfigureAwait(false);
            return await result.BindAsync(bind).ConfigureAwait(false);
        }

        /// <summary>Bind an async non-generic Result to a typed <see cref="Result{TValue}"/>.</summary>
        /// <typeparam name="TValue">The value type produced by <paramref name="bind"/>.</typeparam>
        /// <param name="bind">The next step. Invoked only when the awaited result is successful.</param>
        /// <returns>The bound typed result.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the task or <paramref name="bind"/> is null.</exception>
        public async Task<Result<TValue>> Bind<TValue>(Func<Result<TValue>> bind)
        {
            ArgumentNullException.ThrowIfNull(resultTask);

            Result result = await resultTask.ConfigureAwait(false);
            return result.Bind(bind);
        }

        /// <summary>Async bind an async non-generic Result to a typed <see cref="Result{TValue}"/>.</summary>
        /// <typeparam name="TValue">The value type produced by <paramref name="bind"/>.</typeparam>
        /// <param name="bind">The asynchronous next step. Invoked only when the awaited result is successful.</param>
        /// <returns>The bound typed result.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the task or <paramref name="bind"/> is null.</exception>
        public async Task<Result<TValue>> BindAsync<TValue>(Func<Task<Result<TValue>>> bind)
        {
            ArgumentNullException.ThrowIfNull(resultTask);

            Result result = await resultTask.ConfigureAwait(false);
            return await result.BindAsync(bind).ConfigureAwait(false);
        }

        /// <summary>Tap over an async non-generic Result pipeline.</summary>
        /// <param name="action">The side effect. Invoked only when the awaited result is successful.</param>
        /// <returns>The awaited result, unchanged.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the task or <paramref name="action"/> is null.</exception>
        public async Task<Result> Tap(Action action)
        {
            ArgumentNullException.ThrowIfNull(resultTask);

            Result result = await resultTask.ConfigureAwait(false);
            return result.Tap(action);
        }

        /// <summary>Tap over an async non-generic Result pipeline with an async action.</summary>
        /// <param name="action">The asynchronous side effect. Invoked only when the awaited result is successful.</param>
        /// <returns>The awaited result, unchanged.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the task or <paramref name="action"/> is null.</exception>
        public async Task<Result> TapAsync(Func<Task> action)
        {
            ArgumentNullException.ThrowIfNull(resultTask);

            Result result = await resultTask.ConfigureAwait(false);
            return await result.TapAsync(action).ConfigureAwait(false);
        }

        /// <summary>TapError over an async non-generic Result pipeline.</summary>
        /// <param name="action">The side effect, given all errors on failure.</param>
        /// <returns>The awaited result, unchanged.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the task or <paramref name="action"/> is null.</exception>
        public async Task<Result> TapError(Action<IReadOnlyList<Error>> action)
        {
            ArgumentNullException.ThrowIfNull(resultTask);

            Result result = await resultTask.ConfigureAwait(false);
            return result.TapError(action);
        }

        /// <summary>TapError over an async non-generic Result pipeline with an async action.</summary>
        /// <param name="action">The asynchronous side effect, given all errors on failure.</param>
        /// <returns>The awaited result, unchanged.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the task or <paramref name="action"/> is null.</exception>
        public async Task<Result> TapErrorAsync(Func<IReadOnlyList<Error>, Task> action)
        {
            ArgumentNullException.ThrowIfNull(resultTask);

            Result result = await resultTask.ConfigureAwait(false);
            return await result.TapErrorAsync(action).ConfigureAwait(false);
        }

        /// <summary>Ensure over an async non-generic Result pipeline.</summary>
        /// <param name="predicate">The gate. Invoked only when the awaited result is successful.</param>
        /// <param name="error">The error to fail with when the gate returns false.</param>
        /// <returns>The gated result.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the task, <paramref name="predicate"/> or <paramref name="error"/> is null.</exception>
        public async Task<Result> Ensure(Func<bool> predicate, Error error)
        {
            ArgumentNullException.ThrowIfNull(resultTask);

            Result result = await resultTask.ConfigureAwait(false);
            return result.Ensure(predicate, error);
        }

        /// <summary>Ensure over an async non-generic Result pipeline with a lazy error factory.</summary>
        /// <param name="predicate">The gate. Invoked only when the awaited result is successful.</param>
        /// <param name="errorFactory">Produces the error. Invoked only when the gate fails.</param>
        /// <returns>The gated result.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the task, <paramref name="predicate"/> or <paramref name="errorFactory"/> is null.</exception>
        public async Task<Result> Ensure(Func<bool> predicate, Func<Error> errorFactory)
        {
            ArgumentNullException.ThrowIfNull(resultTask);

            Result result = await resultTask.ConfigureAwait(false);
            return result.Ensure(predicate, errorFactory);
        }

        /// <summary>Switch over an async non-generic Result pipeline.</summary>
        /// <param name="onSuccess">Invoked when the awaited result is successful.</param>
        /// <param name="onFailure">Invoked with all errors when the awaited result failed.</param>
        /// <returns>A task that completes when the taken branch has run.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the task or either branch is null.</exception>
        public async Task Switch(Action onSuccess, Action<IReadOnlyList<Error>> onFailure)
        {
            ArgumentNullException.ThrowIfNull(resultTask);

            Result result = await resultTask.ConfigureAwait(false);
            result.Switch(onSuccess, onFailure);
        }

        /// <summary>SwitchAsync over an async non-generic Result pipeline.</summary>
        /// <param name="onSuccess">Invoked when the awaited result is successful.</param>
        /// <param name="onFailure">Invoked with all errors when the awaited result failed.</param>
        /// <returns>A task that completes when the taken branch has completed.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the task or either branch is null.</exception>
        public async Task SwitchAsync(Func<Task> onSuccess, Func<IReadOnlyList<Error>, Task> onFailure)
        {
            ArgumentNullException.ThrowIfNull(resultTask);

            Result result = await resultTask.ConfigureAwait(false);
            await result.SwitchAsync(onSuccess, onFailure).ConfigureAwait(false);
        }

        /// <summary>Check if the awaited result contains an error of the specified type.</summary>
        /// <typeparam name="TError">The error type to look for. Subtypes count as matches.</typeparam>
        /// <returns>True when at least one error is a <typeparamref name="TError"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the task is null.</exception>
        public async Task<bool> HasError<TError>() where TError : Error
        {
            ArgumentNullException.ThrowIfNull(resultTask);

            Result result = await resultTask.ConfigureAwait(false);
            return result.HasError<TError>();
        }

        /// <summary>Check if the awaited result contains an error of the specified type matching a predicate.</summary>
        /// <typeparam name="TError">The error type to look for. Subtypes count as matches.</typeparam>
        /// <param name="predicate">The condition the matching error must satisfy.</param>
        /// <returns>True when at least one <typeparamref name="TError"/> satisfies <paramref name="predicate"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the task or <paramref name="predicate"/> is null.</exception>
        public async Task<bool> HasError<TError>(Func<TError, bool> predicate) where TError : Error
        {
            ArgumentNullException.ThrowIfNull(resultTask);

            Result result = await resultTask.ConfigureAwait(false);
            return result.HasError(predicate);
        }

        /// <summary>Check if the awaited result contains an error with the specified code.</summary>
        /// <param name="code">The exact, case-sensitive <see cref="Error.Code"/> to look for.</param>
        /// <returns>True when at least one error carries that code.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the task is null.</exception>
        public async Task<bool> HasErrorCode(string code)
        {
            ArgumentNullException.ThrowIfNull(resultTask);

            Result result = await resultTask.ConfigureAwait(false);
            return result.HasErrorCode(code);
        }

        /// <summary>Check if the awaited result contains an error caused by the specified exception type.</summary>
        /// <typeparam name="TException">The exception type to look for. Subtypes count as matches.</typeparam>
        /// <returns>True when an <see cref="ExceptionalError"/> wraps a <typeparamref name="TException"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the task is null.</exception>
        public async Task<bool> HasException<TException>() where TException : Exception
        {
            ArgumentNullException.ThrowIfNull(resultTask);

            Result result = await resultTask.ConfigureAwait(false);
            return result.HasException<TException>();
        }
    }
}
