using Resultify.Errors;

namespace Resultify.Tests;

public sealed class ResultSuccessIfFailureIfLazyTests
{
    [Fact]
    public void SuccessIf_True_WithLazyErrorFactory_ShouldNotCallFactory()
    {
        var called = false;
        Result result = Result.SuccessIf(true, () =>
        {
            called = true;
            return new Error("never");
        });

        Assert.True(result.IsSuccess);
        Assert.False(called);
    }

    [Fact]
    public void SuccessIf_False_WithLazyErrorFactory_ShouldCallFactoryAndFail()
    {
        Result result = Result.SuccessIf(false, () => new Error("lazy"));

        Assert.True(result.IsFailure);
        Assert.Equal("lazy", result.FirstError.Message);
    }

    [Fact]
    public void FailureIf_True_WithLazyErrorFactory_ShouldCallFactoryAndFail()
    {
        Result result = Result.FailureIf(true, () => new Error("lazy fail"));

        Assert.True(result.IsFailure);
        Assert.Equal("lazy fail", result.FirstError.Message);
    }

    [Fact]
    public void FailureIf_False_WithLazyErrorFactory_ShouldNotCallFactory()
    {
        var called = false;
        Result result = Result.FailureIf(false, () =>
        {
            called = true;
            return new Error("never");
        });

        Assert.True(result.IsSuccess);
        Assert.False(called);
    }
}

public sealed class ResultTSuccessIfFailureIfTests
{
    [Fact]
    public void SuccessIf_True_WithError_ShouldSucceed()
    {
        Result<int> result = Result<int>.SuccessIf(true, 42, new Error("never"));

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void SuccessIf_False_WithError_ShouldFail()
    {
        Result<int> result = Result<int>.SuccessIf(false, 42, new Error("fail"));

        Assert.True(result.IsFailure);
        Assert.Equal("fail", result.FirstError.Message);
    }

    [Fact]
    public void SuccessIf_True_WithString_ShouldSucceed()
    {
        Result<int> result = Result<int>.SuccessIf(true, 10, "should not fail");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void SuccessIf_False_WithString_ShouldFail()
    {
        Result<int> result = Result<int>.SuccessIf(false, 10, "condition failed");

        Assert.True(result.IsFailure);
        Assert.Equal("condition failed", result.FirstError.Message);
    }

    [Fact]
    public void FailureIf_True_WithError_ShouldFail()
    {
        Result<int> result = Result<int>.FailureIf(true, 42, new Error("bad"));

        Assert.True(result.IsFailure);
        Assert.Equal("bad", result.FirstError.Message);
    }

    [Fact]
    public void FailureIf_False_WithError_ShouldSucceed()
    {
        Result<int> result = Result<int>.FailureIf(false, 42, new Error("should not fail"));

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void FailureIf_True_WithString_ShouldFail()
    {
        Result<int> result = Result<int>.FailureIf(true, 42, "is bad");

        Assert.True(result.IsFailure);
        Assert.Equal("is bad", result.FirstError.Message);
    }

    [Fact]
    public void FailureIf_False_WithString_ShouldSucceed()
    {
        Result<int> result = Result<int>.FailureIf(false, 42, "should not fail");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void FailureIf_True_WithLazyFactory_ShouldFail()
    {
        Result<int> result = Result<int>.FailureIf(true, 1, () => new Error("lazy bad"));

        Assert.True(result.IsFailure);
        Assert.Equal("lazy bad", result.FirstError.Message);
    }

    [Fact]
    public void FailureIf_False_WithLazyFactory_ShouldNotCallFactory()
    {
        var called = false;
        Result<int> result = Result<int>.FailureIf(false, 1, () =>
        {
            called = true;
            return new Error("never");
        });

        Assert.True(result.IsSuccess);
        Assert.False(called);
    }
}