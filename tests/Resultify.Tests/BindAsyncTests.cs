namespace Resultify.Tests;

public sealed class ResultBindAsyncTests
{
    [Fact]
    public async Task BindAsync_OnSuccess_ShouldExecuteAndReturnResult()
    {
        Result result = await Result.Success().BindAsync(() => Task.FromResult(Result.Success()));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task BindAsync_OnFailure_ShouldSkipAndPropagateErrors()
    {
        var executed = false;
        Result result = await Result.Failure("err").BindAsync(() =>
        {
            executed = true;
            return Task.FromResult(Result.Success());
        });

        Assert.False(executed);
        Assert.True(result.IsFailure);
        Assert.Equal("err", result.FirstError.Message);
    }

    [Fact]
    public async Task BindAsyncToTyped_OnSuccess_ShouldChain()
    {
        Result<int> result = await Result.Success().BindAsync(() => Task.FromResult(Result<int>.Success(42)));

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task BindAsyncToTyped_OnFailure_ShouldPropagate()
    {
        Result<int> result = await Result.Failure("upstream").BindAsync(() => Task.FromResult(Result<int>.Success(42)));

        Assert.True(result.IsFailure);
        Assert.Equal("upstream", result.FirstError.Message);
    }
}

public sealed class ResultTBindAsyncTests
{
    [Fact]
    public async Task BindAsync_OnSuccess_ShouldChain()
    {
        Result<string> result = await Result<int>.Success(5)
            .BindAsync(v => Task.FromResult(Result<string>.Success($"v={v}")));

        Assert.Equal("v=5", result.Value);
    }

    [Fact]
    public async Task BindAsync_OnFailure_ShouldPropagate()
    {
        var executed = false;
        Result<string> result = await Result<int>.Failure("err")
            .BindAsync(v =>
            {
                executed = true;
                return Task.FromResult(Result<string>.Success($"v={v}"));
            });

        Assert.False(executed);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task BindAsyncToNonGeneric_OnSuccess_ShouldChain()
    {
        Result result = await Result<int>.Success(5)
            .BindAsync(v => Task.FromResult(v > 0 ? Result.Success() : Result.Failure("negative")));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task BindAsyncToNonGeneric_OnFailure_ShouldPropagate()
    {
        Result result = await Result<int>.Failure("err")
            .BindAsync(_ => Task.FromResult(Result.Success()));

        Assert.True(result.IsFailure);
    }
}