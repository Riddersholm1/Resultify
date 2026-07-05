using Resultify.Errors;

namespace Resultify.Tests;

public sealed class ResultTTests
{
    // ── Success / Failure ────────────────────────────────────

    [Fact]
    public void Success_ShouldHoldValue()
    {
        Result<int> result = Result<int>.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.Equal(42, result.ValueOrDefault);
    }

    [Fact]
    public void Failure_AccessingValue_ShouldThrow()
    {
        Result<int> result = Result<int>.Failure("err");

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Failure_ValueOrDefault_ShouldReturnDefault()
    {
        Result<int> result = Result<int>.Failure("err");

        Assert.Equal(0, result.ValueOrDefault);
    }

    // ── Create (null-safe) ───────────────────────────────────

    [Fact]
    public void Create_WithNonNullValue_ShouldSucceed()
    {
        var result = Result<string>.Create("hello");

        Assert.True(result.IsSuccess);
        Assert.Equal("hello", result.Value);
    }

    [Fact]
    public void Create_WithNull_ShouldReturnNullValueError()
    {
        var result = Result<string>.Create(null);

        Assert.True(result.IsFailure);
        Assert.Equal(Error.NullValue, result.FirstError);
    }

    [Fact]
    public void Create_ViaImplicitConversion_WithNonNull_ShouldSucceed()
    {
        Result<string> result = "hello";

        Assert.True(result.IsSuccess);
        Assert.Equal("hello", result.Value);
    }

    [Fact]
    public void Create_ViaImplicitConversion_WithNull_ShouldFail()
    {
        string? value = null;
        Result<string> result = value;

        Assert.True(result.IsFailure);
        Assert.Equal(Error.NullValue, result.FirstError);
    }

    [Fact]
    public void Result_Create_GenericHelper_ShouldWork()
    {
        var result = Result.Create<string>(null);

        Assert.True(result.IsFailure);
        Assert.Equal(Error.NullValue, result.FirstError);
    }

    // ── Try ──────────────────────────────────────────────────

    [Fact]
    public void Try_WhenNoException_ShouldReturnValue()
    {
        Result<int> result = Result<int>.Try(() => 42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Try_WhenException_ShouldFail()
    {
        Result<int> result = Result<int>.Try(() => throw new Exception("boom"));

        Assert.True(result.IsFailure);
    }

    // ── Map / Bind ───────────────────────────────────────────

    [Fact]
    public void Map_WhenSuccess_ShouldTransformValue()
    {
        Result<int> result = Result<int>.Success(5).Map(v => v * 2);

        Assert.Equal(10, result.Value);
    }

    [Fact]
    public void Map_WhenFailure_ShouldPropagateErrors()
    {
        Result<int> result = Result<int>.Failure("err").Map(v => v * 2);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Bind_WhenSuccess_ShouldChain()
    {
        Result<string> result = Result<int>.Success(5)
            .Bind(v => Result<string>.Success($"Value is {v}"));

        Assert.Equal("Value is 5", result.Value);
    }

    [Fact]
    public void Bind_WhenFirstFails_ShouldPropagateErrors()
    {
        Result<string> result = Result<int>.Failure("err")
            .Bind(v => Result<string>.Success($"Value is {v}"));

        Assert.True(result.IsFailure);
    }

    // ── Ensure ───────────────────────────────────────────────

    [Fact]
    public void Ensure_WhenPredicatePassesOnValue_ShouldSucceed()
    {
        Result<int> result = Result<int>.Success(10).Ensure(v => v > 5, "Must be > 5");

        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Value);
    }

    [Fact]
    public void Ensure_WhenPredicateFails_ShouldFail()
    {
        Result<int> result = Result<int>.Success(3).Ensure(v => v > 5, "Must be > 5");

        Assert.True(result.IsFailure);
    }

    // ── Match ────────────────────────────────────────────────

    [Fact]
    public void Match_WhenSuccess_ShouldUseValue()
    {
        string output = Result<int>.Success(42).Match(v => $"Got {v}", _ => "failed");

        Assert.Equal("Got 42", output);
    }

    // ── ToResult ─────────────────────────────────────────────

    [Fact]
    public void ToResult_ShouldDropValue()
    {
        Result<int> typed = Result<int>.Success(42);
        var untyped = typed.ToResult();

        Assert.True(untyped.IsSuccess);
    }

    // ── Implicit conversions ─────────────────────────────────

    [Fact]
    public void ImplicitConversion_ErrorToResult_ShouldFail()
    {
        Result<int> result = new Error("implicit fail");

        Assert.True(result.IsFailure);
    }

    // ── Deconstruct ──────────────────────────────────────────

    [Fact]
    public void Deconstruct_ShouldWork()
    {
        (bool isSuccess, int value, IReadOnlyList<Error> errors) = Result<int>.Success(42);

        Assert.True(isSuccess);
        Assert.Equal(42, value);
        Assert.Empty(errors);
    }
}