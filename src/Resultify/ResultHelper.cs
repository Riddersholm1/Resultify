using System.Collections.ObjectModel;
using Resultify.Errors;

namespace Resultify;

/// <summary>
/// Internal plumbing shared by <see cref="Result"/>, <see cref="Result{TValue}"/> and
/// <see cref="ResultExtensions"/>. Centralises the rules that must hold for every failure,
/// regardless of which factory produced it:
/// <list type="bullet">
///   <item><description>the error list is non-empty and contains no null elements;</description></item>
///   <item><description>the error list cannot be mutated after construction, including by casting
///   the exposed <see cref="IReadOnlyList{T}"/> back to its backing store.</description></item>
/// </list>
/// </summary>
internal static class ResultHelper
{
    /// <summary>
    /// Shared empty error list handed out by every successful result, so reading <c>Errors</c> on a
    /// success neither allocates nor differs between closed generic types.
    /// </summary>
    internal static readonly IReadOnlyList<Error> EmptyErrors = new ReadOnlyCollection<Error>([]);

    /// <summary>
    /// Wraps a freshly built, privately owned array so the public <see cref="IReadOnlyList{T}"/>
    /// cannot be cast back to <c>Error[]</c> and written through. A result is a struct, so every
    /// copy shares the same list instance — one such write would corrupt all of them at once.
    /// </summary>
    /// <param name="errors">An array that no other code holds a reference to.</param>
    /// <returns>A read-only view that does not expose the array.</returns>
    internal static IReadOnlyList<Error> Seal(Error[] errors) =>
        errors.Length == 0
            ? EmptyErrors
            : new ReadOnlyCollection<Error>(errors);

    /// <summary>
    /// Validates and materialises an error sequence for <c>Failure(IEnumerable&lt;Error&gt;)</c>.
    /// The sequence is enumerated exactly once and copied, so later mutation of the caller's
    /// collection cannot reach into the result.
    /// </summary>
    /// <param name="errors">The errors to validate.</param>
    /// <returns>A sealed, non-empty error list.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="errors"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="errors"/> is empty or contains a null element.
    /// </exception>
    internal static IReadOnlyList<Error> ValidateErrors(IEnumerable<Error> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        List<Error> list = [];
        foreach (Error error in errors)
        {
            if (error is null)
            {
                throw new ArgumentException("Error elements must not be null.", nameof(errors));
            }

            list.Add(error);
        }

        return list.Count == 0
            ? throw new ArgumentException("At least one error is required.", nameof(errors))
            : Seal([.. list]);
    }

    /// <summary>
    /// The merge policy for <see cref="Result"/> values: the merge fails when any input failed, and
    /// carries every error from every failed input, in encounter order.
    /// </summary>
    /// <param name="results">The results to merge. An empty span merges to a success.</param>
    internal static Result MergeAll(ReadOnlySpan<Result> results)
    {
        List<Error>? errors = null;
        foreach (Result result in results)
        {
            if (result.IsFailure)
            {
                (errors ??= []).AddRange(result.Errors);
            }
        }

        return Merged(errors);
    }

    /// <inheritdoc cref="MergeAll(ReadOnlySpan{Result})"/>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="results"/> is null.</exception>
    internal static Result MergeAll(IEnumerable<Result> results)
    {
        ArgumentNullException.ThrowIfNull(results);
        List<Error>? errors = null;
        foreach (Result result in results)
        {
            if (result.IsFailure)
            {
                (errors ??= []).AddRange(result.Errors);
            }
        }

        return Merged(errors);
    }

    /// <summary>
    /// Turns the collected errors into the merged result. They already came from validated
    /// failures, so they need sealing but not revalidating.
    /// </summary>
    private static Result Merged(List<Error>? errors) =>
        errors is null
            ? Result.Success()
            : Result.FailureUnchecked(Seal([.. errors]));
}
