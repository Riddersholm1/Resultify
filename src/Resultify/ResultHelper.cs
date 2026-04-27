using Resultify.Errors;

namespace Resultify;

/// <summary>
/// Shared constants for <see cref="Result"/> and <see cref="Result{TValue}"/>.
/// A single allocation is reused across all closed generic types.
/// </summary>
internal static class ResultHelper
{
    // Shared empty list so reading Errors on a successful result is allocation-free
    // and not duplicated per closed generic type.
    internal static readonly IReadOnlyList<Error> EmptyErrors = [];
}