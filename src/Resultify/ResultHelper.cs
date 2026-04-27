using Resultify.Errors;

namespace Resultify;

internal static class ResultHelper
{
    // Shared empty list so reading Errors on a successful result is allocation-free
    // and not duplicated per closed generic type.
    internal static readonly IReadOnlyList<Error> EmptyErrors = [];

    /// <summary>
    /// Validates and materializes an error sequence for <c>Failure(IEnumerable&lt;Error&gt;)</c>.
    /// Throws on null sequence, null elements, or empty sequence.
    /// </summary>
    internal static Error[] ValidateErrors(IEnumerable<Error> errors)
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
            : list.ToArray();
    }
}