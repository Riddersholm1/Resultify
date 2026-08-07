using Resultify.Errors;

namespace Resultify.Tests;

/// <summary>
/// <c>Result</c> and <c>Result&lt;T&gt;</c> are structs, so <c>default</c> is reachable however
/// carefully the factories guard their inputs — an uninitialised field, an array element, a
/// <c>default</c> switch arm. These tests pin exactly what that value does, so the behaviour is a
/// documented contract rather than an accident. See the remarks on <see cref="Result{TValue}"/>.
/// </summary>
public sealed class DefaultResultTests
{
    [Fact]
    public void DefaultResult_IsSuccess()
    {
        Result result = default;

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Empty(result.Errors);
        Assert.Equal(Error.None, result.FirstError);
    }

    [Fact]
    public void DefaultResult_ShouldBehaveLikeASuccessInEveryCombinator()
    {
        Result result = default;
        var tapped = false;

        Assert.True(result.Tap(() => tapped = true).IsSuccess);
        Assert.True(tapped);
        Assert.True(result.Bind(Result.Success).IsSuccess);
        Assert.True(result.Ensure(() => true, "nope").IsSuccess);
        Assert.Equal("ok", result.Match(() => "ok", _ => "failed"));
        Assert.True(result.ToResult(42).IsSuccess);
    }

    [Fact]
    public void DefaultResultT_OverValueType_IsSuccessCarryingTheDefaultValue()
    {
        Result<int> result = default;

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value);
        Assert.Equal(0, result.ValueOrDefault);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void DefaultResultT_OverValueType_ShouldFlowThroughCombinators()
    {
        Result<int> result = default;

        Assert.Equal(1, result.Map(v => v + 1).Value);
        Assert.True(result.Ensure(v => v == 0, "nope").IsSuccess);
        Assert.Equal("got 0", result.Match(v => $"got {v}", _ => "failed"));
    }

    [Fact]
    public void DefaultResultT_OverReferenceType_IsSuccessButValueThrows()
    {
        Result<string> result = default;

        Assert.True(result.IsSuccess);
        Assert.Null(result.ValueOrDefault);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void DefaultResultT_OverReferenceType_TotalAccessorsShouldNotThrow()
    {
        Result<string> result = default;

        Assert.False(result.TryGetValue(out string? value));
        Assert.Null(value);
        Assert.Null(result.ValueOrDefault);
        Assert.Empty(result.Errors);
        Assert.Equal(Error.None, result.FirstError);
        Assert.False(result.HasError<Error>());
        Assert.Contains("null", result.ToString());
    }

    /// <summary>
    /// Any combinator that hands the value to a callback must fail loudly rather than pass a null
    /// the type system says cannot be null. This documents the whole set in one place.
    /// </summary>
    public static TheoryData<string, Action> ValueReadingCombinators()
    {
        Result<string> defaulted = default;
        return new TheoryData<string, Action>
        {
            { "Map", () => defaulted.Map(s => s.Length) },
            { "MapAsync", () => defaulted.MapAsync(s => Task.FromResult(s.Length)).GetAwaiter().GetResult() },
            { "Bind", () => defaulted.Bind(s => Result<int>.Success(s.Length)) },
            { "BindAsync", () => defaulted.BindAsync(s => Task.FromResult(Result<int>.Success(s.Length))).GetAwaiter().GetResult() },
            { "Bind to Result", () => defaulted.Bind(_ => Result.Success()) },
            { "Tap", () => defaulted.Tap(_ => { }) },
            { "TapAsync", () => defaulted.TapAsync(_ => Task.CompletedTask).GetAwaiter().GetResult() },
            { "Ensure", () => defaulted.Ensure(_ => true, "nope") },
            { "Ensure (factory)", () => defaulted.Ensure(_ => true, _ => new Error("nope")) },
            { "Match", () => defaulted.Match(s => s.Length, _ => 0) },
            { "MatchAsync", () => defaulted.MatchAsync(s => Task.FromResult(s.Length), _ => Task.FromResult(0)).GetAwaiter().GetResult() },
            { "Switch", () => defaulted.Switch(_ => { }, _ => { }) },
            { "SwitchAsync", () => defaulted.SwitchAsync(_ => Task.CompletedTask, _ => Task.CompletedTask).GetAwaiter().GetResult() },
            { "Merge", () => new[] { defaulted }.Merge() },
        };
    }

    [Theory]
    [MemberData(nameof(ValueReadingCombinators))]
    public void DefaultResultT_OverReferenceType_ValueReadingCombinatorsShouldThrow(string name, Action act)
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(act);

        Assert.Contains("Value", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DefaultResultT_OverReferenceType_ErrorSideCombinatorsShouldStillWork()
    {
        Result<string> defaulted = default;
        var tapped = false;

        Assert.True(defaulted.TapError(_ => tapped = true).IsSuccess);
        Assert.False(tapped);
        Assert.True(defaulted.ToResult().IsSuccess);
    }
}
