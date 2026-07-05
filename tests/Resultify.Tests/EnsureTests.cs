using Resultify.Errors;

namespace Resultify.Tests;

public sealed class LazyEnsureAndSuccessIfTests
{
    [Fact]
    public void Result_Ensure_WithLazyErrorFactory_ShouldNotCallFactoryOnSuccess()
    {
        var called = false;
        Result result = Result.Success().Ensure(() => true, () =>
        {
            called = true;
            return new Error("should not be created");
        });

        Assert.True(result.IsSuccess);
        Assert.False(called);
    }

    [Fact]
    public void Result_Ensure_WithLazyErrorFactory_ShouldCallFactoryOnPredicateFalse()
    {
        Result result = Result.Success().Ensure(() => false, () => new Error("lazy error"));

        Assert.True(result.IsFailure);
        Assert.Equal("lazy error", result.FirstError.Message);
    }

    [Fact]
    public void ResultT_SuccessIf_WithLazyErrorFactory_ShouldSucceed()
    {
        var called = false;
        Result<int> result = Result<int>.SuccessIf(true, 42, () =>
        {
            called = true;
            return new Error("should not be created");
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.False(called);
    }

    [Fact]
    public void ResultT_SuccessIf_WithLazyErrorFactory_ShouldFail()
    {
        Result<int> result = Result<int>.SuccessIf(false, 42, () => new Error("lazy"));

        Assert.True(result.IsFailure);
        Assert.Equal("lazy", result.FirstError.Message);
    }
}

public sealed class ResultTEnsureLazyFactoryTests
{
    [Fact]
    public void ResultT_Ensure_WithLazyErrorFactory_ShouldNotCallFactoryOnPass()
    {
        var called = false;
        Result<int> result = Result<int>.Success(42).Ensure(
            v => v > 0,
            v =>
            {
                called = true;
                return new Error($"v={v}");
            });

        Assert.True(result.IsSuccess);
        Assert.False(called);
    }

    [Fact]
    public void ResultT_Ensure_WithLazyErrorFactory_ShouldCallFactoryOnFail()
    {
        Result<int> result = Result<int>.Success(3).Ensure(
            v => v > 5,
            v => new Error($"v={v} too small"));

        Assert.True(result.IsFailure);
        Assert.Equal("v=3 too small", result.FirstError.Message);
    }

    [Fact]
    public void ResultT_Ensure_WithLazyErrorFactory_ShouldSkipOnAlreadyFailed()
    {
        var called = false;
        Result<int> result = Result<int>.Failure("earlier").Ensure(
            v => v > 0,
            _ =>
            {
                called = true;
                return new Error("should not run");
            });

        Assert.True(result.IsFailure);
        Assert.Equal("earlier", result.FirstError.Message);
        Assert.False(called);
    }
}