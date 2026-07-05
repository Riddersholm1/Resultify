using Resultify.Errors;

namespace Resultify.Tests;

public sealed class ResultTryFuncOverloadTests
{
    [Fact]
    public void Try_FuncResult_WhenSuccessful_ShouldReturnInnerSuccess()
    {
        Result result = Result.Try(Result.Success);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Try_FuncResult_WhenInnerFailure_ShouldReturnInnerFailure()
    {
        Result result = Result.Try(() => Result.Failure("inner"));

        Assert.True(result.IsFailure);
        Assert.Equal("inner", result.FirstError.Message);
    }

    [Fact]
    public void Try_FuncResult_WhenException_ShouldWrapAsExceptionalError()
    {
        Result result = Result.Try(() => throw new InvalidOperationException("boom"));

        Assert.True(result.IsFailure);
        Assert.IsType<ExceptionalError>(result.FirstError);
    }

    [Fact]
    public async Task TryAsync_FuncTaskResult_WhenSuccessful_ShouldReturnSuccess()
    {
        Result result = await Result.TryAsync(() => Task.FromResult(Result.Success()));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task TryAsync_FuncTaskResult_WhenInnerFailure_ShouldReturnFailure()
    {
        Result result = await Result.TryAsync(() => Task.FromResult(Result.Failure("inner")));

        Assert.True(result.IsFailure);
        Assert.Equal("inner", result.FirstError.Message);
    }

    [Fact]
    public async Task TryAsync_FuncTaskResult_WhenException_ShouldWrapAsExceptionalError()
    {
        Result result = await Result.TryAsync(() => throw new InvalidOperationException("boom"));

        Assert.True(result.IsFailure);
        Assert.IsType<ExceptionalError>(result.FirstError);
    }
}

public sealed class ResultTTryFuncOverloadTests
{
    [Fact]
    public void Try_FuncResultT_WhenSuccessful_ShouldReturnInnerSuccess()
    {
        Result<int> result = Result<int>.Try(() => Result<int>.Success(42));

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Try_FuncResultT_WhenInnerFailure_ShouldReturnInnerFailure()
    {
        Result<int> result = Result<int>.Try(() => Result<int>.Failure("inner"));

        Assert.True(result.IsFailure);
        Assert.Equal("inner", result.FirstError.Message);
    }

    [Fact]
    public void Try_FuncResultT_WhenException_ShouldWrapAsExceptionalError()
    {
        Result<int> result = Result<int>.Try((Func<Result<int>>)(() => throw new InvalidOperationException("boom")));

        Assert.True(result.IsFailure);
        Assert.IsType<ExceptionalError>(result.FirstError);
    }

    [Fact]
    public async Task TryAsync_FuncTaskResultT_WhenSuccessful_ShouldReturnSuccess()
    {
        Result<int> result = await Result<int>.TryAsync(() => Task.FromResult(Result<int>.Success(7)));

        Assert.True(result.IsSuccess);
        Assert.Equal(7, result.Value);
    }

    [Fact]
    public async Task TryAsync_FuncTaskResultT_WhenInnerFailure_ShouldReturnFailure()
    {
        Result<int> result = await Result<int>.TryAsync(() => Task.FromResult(Result<int>.Failure("inner")));

        Assert.True(result.IsFailure);
        Assert.Equal("inner", result.FirstError.Message);
    }

    [Fact]
    public async Task TryAsync_FuncTaskResultT_WhenException_ShouldWrapAsExceptionalError()
    {
        Result<int> result = await Result<int>.TryAsync((Func<Task<Result<int>>>)(() => throw new InvalidOperationException("boom")));

        Assert.True(result.IsFailure);
        Assert.IsType<ExceptionalError>(result.FirstError);
    }
}