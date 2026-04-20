using Resultify.Errors;

namespace Resultify;

/// <summary>
/// Extension methods for <see cref="Result"/> and <see cref="Result{TValue}"/>.
/// </summary>
public static class ResultExtensions
{
    // ── Merge for collections ────────────────────────────────

    /// <summary>Merge a collection of results into a single result.</summary>
    public static Result Merge(this IEnumerable<Result> results)
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
        return errors is null ? Result.Success() : Result.Failure(errors);
    }

    /// <summary>
    /// Merge a collection of <see cref="Result{TValue}"/> into a single result
    /// containing all values (if all succeeded) or all errors.
    /// </summary>
    public static Result<IReadOnlyList<TValue>> Merge<TValue>(this IEnumerable<Result<TValue>> results)
    {
        List<TValue>? values = null;
        List<Error>? errors = null;

        foreach (Result<TValue> r in results)
        {
            if (r.IsSuccess)
            {
                values ??= [];
                values.Add(r.Value);
            }
            else
            {
                errors ??= [];
                errors.AddRange(r.Errors);
            }
        }

        return errors is not null
            ? Result<IReadOnlyList<TValue>>.Failure(errors)
            : Result<IReadOnlyList<TValue>>.Success(values?.AsReadOnly() ?? (IReadOnlyList<TValue>)[]);
    }

    // ── Error querying ───────────────────────────────────────

    extension(Result result)
    {
        /// <summary>Check if the result contains an error of the specified type.</summary>
        public bool HasError<TError>() where TError : Error =>
            result.Errors.OfType<TError>().Any();

        /// <summary>Check if the result contains an error of the specified type matching a predicate.</summary>
        public bool HasError<TError>(Func<TError, bool> predicate) where TError : Error =>
            result.Errors.OfType<TError>().Any(predicate);

        /// <summary>Check if the result contains an error with the specified code.</summary>
        public bool HasErrorCode(string code) =>
            result.Errors.Any(e => e.Code == code);

        /// <summary>Check if any error in the result was caused by a specific exception type.</summary>
        public bool HasException<TException>() where TException : Exception =>
            result.Errors.OfType<ExceptionalError>().Any(e => e.Exception is TException);
    }

    extension<TValue>(Result<TValue> result)
    {
        /// <summary>Check if the result contains an error with the specified code.</summary>
        public bool HasErrorCode(string code) =>
            result.Errors.Any(e => e.Code == code);
    }

    /// <summary>Check if the result contains an error of the specified type.</summary>
    public static bool HasError<TError, TValue>(this Result<TValue> result) where TError : Error =>
        result.Errors.OfType<TError>().Any();

    /// <summary>Check if the result contains an error of the specified type matching a predicate.</summary>
    public static bool HasError<TError, TValue>(this Result<TValue> result, Func<TError, bool> predicate) where TError : Error =>
        result.Errors.OfType<TError>().Any(predicate);

    /// <summary>Check if any error in the result was caused by a specific exception type.</summary>
    public static bool HasException<TException, TValue>(this Result<TValue> result) where TException : Exception =>
        result.Errors.OfType<ExceptionalError>().Any(e => e.Exception is TException);

    // ── Async pipeline helpers ───────────────────────────────

    extension<TValue>(Task<Result<TValue>> resultTask)
    {
        /// <summary>Map over an async Result pipeline.</summary>
        public async Task<Result<TNew>> Map<TNew>(Func<TValue, TNew> mapper)
        {
            Result<TValue> result = await resultTask.ConfigureAwait(false);
            return result.Map(mapper);
        }

        /// <summary>Bind over an async Result pipeline.</summary>
        public async Task<Result<TNew>> Bind<TNew>(Func<TValue, Result<TNew>> bind)
        {
            Result<TValue> result = await resultTask.ConfigureAwait(false);
            return result.Bind(bind);
        }

        /// <summary>Bind over an async Result pipeline with async bind function.</summary>
        public async Task<Result<TNew>> BindAsync<TNew>(Func<TValue, Task<Result<TNew>>> bind)
        {
            Result<TValue> result = await resultTask.ConfigureAwait(false);
            return await result.BindAsync(bind).ConfigureAwait(false);
        }

        /// <summary>Tap over an async Result pipeline.</summary>
        public async Task<Result<TValue>> Tap(Action<TValue> action)
        {
            Result<TValue> result = await resultTask.ConfigureAwait(false);
            return result.Tap(action);
        }

        /// <summary>Ensure over an async Result pipeline.</summary>
        public async Task<Result<TValue>> Ensure(Func<TValue, bool> predicate, Error error)
        {
            Result<TValue> result = await resultTask.ConfigureAwait(false);
            return result.Ensure(predicate, error);
        }

        /// <summary>Ensure over an async Result pipeline with string error.</summary>
        public async Task<Result<TValue>> Ensure(Func<TValue, bool> predicate, string errorMessage)
        {
            Result<TValue> result = await resultTask.ConfigureAwait(false);
            return result.Ensure(predicate, errorMessage);
        }

        /// <summary>Match over an async Result pipeline.</summary>
        public async Task<TOut> Match<TOut>(Func<TValue, TOut> onSuccess, Func<IReadOnlyList<Error>, TOut> onFailure)
        {
            Result<TValue> result = await resultTask.ConfigureAwait(false);
            return result.Match(onSuccess, onFailure);
        }

        /// <summary>Async Match over an async Result pipeline.</summary>
        public async Task<TOut> MatchAsync<TOut>(Func<TValue, Task<TOut>> onSuccess, Func<IReadOnlyList<Error>, Task<TOut>> onFailure)
        {
            Result<TValue> result = await resultTask.ConfigureAwait(false);
            return await result.MatchAsync(onSuccess, onFailure).ConfigureAwait(false);
        }

        /// <summary>TapError over an async Result pipeline.</summary>
        public async Task<Result<TValue>> TapError(Action<IReadOnlyList<Error>> action)
        {
            Result<TValue> result = await resultTask.ConfigureAwait(false);
            return result.TapError(action);
        }

        /// <summary>Switch over an async Result pipeline.</summary>
        public async Task Switch(Action<TValue> onSuccess, Action<IReadOnlyList<Error>> onFailure)
        {
            Result<TValue> result = await resultTask.ConfigureAwait(false);
            result.Switch(onSuccess, onFailure);
        }

        /// <summary>Bind to non-generic Result over an async Result pipeline.</summary>
        public async Task<Result> Bind(Func<TValue, Result> bind)
        {
            Result<TValue> result = await resultTask.ConfigureAwait(false);
            return result.Bind(bind);
        }

        /// <summary>Async Bind to non-generic Result over an async Result pipeline.</summary>
        public async Task<Result> BindAsync(Func<TValue, Task<Result>> bind)
        {
            Result<TValue> result = await resultTask.ConfigureAwait(false);
            return await result.BindAsync(bind).ConfigureAwait(false);
        }
    }

    extension(Task<Result> resultTask)
    {
        /// <summary>Match over an async non-generic Result pipeline.</summary>
        public async Task<TOut> Match<TOut>(Func<TOut> onSuccess,
            Func<IReadOnlyList<Error>, TOut> onFailure)
        {
            Result result = await resultTask.ConfigureAwait(false);
            return result.Match(onSuccess, onFailure);
        }

        /// <summary>Bind over an async non-generic Result pipeline.</summary>
        public async Task<Result> Bind(Func<Result> bind)
        {
            Result result = await resultTask.ConfigureAwait(false);
            return result.Bind(bind);
        }

        /// <summary>Bind async over an async non-generic Result pipeline.</summary>
        public async Task<Result> BindAsync(Func<Task<Result>> bind)
        {
            Result result = await resultTask.ConfigureAwait(false);
            return await result.BindAsync(bind).ConfigureAwait(false);
        }

        /// <summary>Tap over an async non-generic Result pipeline.</summary>
        public async Task<Result> Tap(Action action)
        {
            Result result = await resultTask.ConfigureAwait(false);
            return result.Tap(action);
        }

        /// <summary>TapError over an async non-generic Result pipeline.</summary>
        public async Task<Result> TapError(Action<IReadOnlyList<Error>> action)
        {
            Result result = await resultTask.ConfigureAwait(false);
            return result.TapError(action);
        }

        /// <summary>Switch over an async non-generic Result pipeline.</summary>
        public async Task Switch(Action onSuccess, Action<IReadOnlyList<Error>> onFailure)
        {
            Result result = await resultTask.ConfigureAwait(false);
            result.Switch(onSuccess, onFailure);
        }
    }
}