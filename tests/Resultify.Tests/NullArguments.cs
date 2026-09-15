namespace Resultify.Tests;

/// <summary>
/// Supplies a null whose static type is stated explicitly.
/// </summary>
/// <remarks>
/// Most of the guarded members are overloaded, so a bare <c>null!</c> argument would let overload
/// resolution — not the test — decide which guard is exercised, and two cases named after different
/// overloads could quietly collapse onto the same one. <c>Null.Of&lt;Action&gt;()</c> puts the
/// overload in the expression's type, where it is visible and cannot be dropped by accident.
/// </remarks>
internal static class Null
{
    /// <summary>A null typed as <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">The static type the null should carry.</typeparam>
    internal static T Of<T>()
        where T : class => null!;
}
