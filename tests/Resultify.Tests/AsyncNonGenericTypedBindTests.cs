using Resultify.Errors;

namespace Resultify.Tests;

public sealed class AsyncNonGenericTypedBindTests
{
    [Fact]
    public async Task Bind_FromAsyncNonGenericSuccess_ToTypedResult_ShouldChain()
    {
        Result<int> result = await Task.FromResult(Result.Success())
            .Bind(() => Result<int>.Success(42));

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task Bind_FromAsyncNonGenericFailure_ToTypedResult_ShouldPropagate()
    {
        var executed = false;
        Result<int> result = await Task.FromResult(Result.Failure("upstream"))
            .Bind(() =>
            {
                executed = true;
                return Result<int>.Success(42);
            });

        Assert.False(executed);
        Assert.True(result.IsFailure);
        Assert.Equal("upstream", result.FirstError.Message);
    }

    [Fact]
    public async Task BindAsync_FromAsyncNonGenericSuccess_ToTypedResult_ShouldChain()
    {
        Result<string> result = await Task.FromResult(Result.Success())
            .BindAsync(() => Task.FromResult(Result<string>.Success("yo")));

        Assert.True(result.IsSuccess);
        Assert.Equal("yo", result.Value);
    }

    [Fact]
    public async Task BindAsync_FromAsyncNonGenericFailure_ToTypedResult_ShouldPropagate()
    {
        var executed = false;
        Result<string> result = await Task.FromResult(Result.Failure("upstream"))
            .BindAsync(() =>
            {
                executed = true;
                return Task.FromResult(Result<string>.Success("yo"));
            });

        Assert.False(executed);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Bind_FromAsyncNonGeneric_ChainedWithTypedPipeline_ShouldFlow()
    {
        Result<int> result = await Task.FromResult(Result.Success())
            .Bind(() => Result<int>.Success(5))
            .Map(v => v * 2);

        Assert.Equal(10, result.Value);
    }

    [Fact]
    public async Task BindAsync_FromAsyncNonGeneric_TypedFailureFromInner_ShouldPropagate()
    {
        Result<int> result = await Task.FromResult(Result.Success())
            .BindAsync(() => Task.FromResult(Result<int>.Failure(new Error("inner"))));

        Assert.True(result.IsFailure);
        Assert.Equal("inner", result.FirstError.Message);
    }
}