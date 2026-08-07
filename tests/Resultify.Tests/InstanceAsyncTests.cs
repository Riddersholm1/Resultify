namespace Resultify.Tests;

/// <summary>
/// The <c>*Async</c> methods declared directly on the result types, exercised on the receiver rather
/// than through the <c>Task</c> extension helpers. The helpers delegate here, but these assert the
/// instance contract on its own so a change to either layer is caught where it happens.
/// </summary>
public sealed class ResultInstanceAsyncTests
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
    public async Task BindAsync_ToTyped_OnSuccess_ShouldChain()
    {
        Result<int> result = await Result.Success().BindAsync(() => Task.FromResult(Result<int>.Success(42)));

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task BindAsync_ToTyped_OnFailure_ShouldPropagate()
    {
        Result<int> result = await Result.Failure("upstream").BindAsync(() => Task.FromResult(Result<int>.Success(42)));

        Assert.True(result.IsFailure);
        Assert.Equal("upstream", result.FirstError.Message);
    }

    [Fact]
    public async Task MatchAsync_OnSuccess_ShouldReturnOnSuccessValue()
    {
        string output = await Result.Success().MatchAsync(
            () => Task.FromResult("ok"),
            _ => Task.FromResult("fail"));

        Assert.Equal("ok", output);
    }

    [Fact]
    public async Task MatchAsync_OnFailure_ShouldReturnOnFailureValue()
    {
        string output = await Result.Failure("err").MatchAsync(
            () => Task.FromResult("ok"),
            _ => Task.FromResult("fail"));

        Assert.Equal("fail", output);
    }
}

public sealed class ResultTInstanceAsyncTests
{
    [Fact]
    public async Task MapAsync_ShouldTransformValue()
    {
        Result<int> result = await Result<int>.Success(5).MapAsync(v => Task.FromResult(v * 2));

        Assert.Equal(10, result.Value);
    }

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
    public async Task BindAsync_ToNonGeneric_OnSuccess_ShouldChain()
    {
        Result result = await Result<int>.Success(5)
            .BindAsync(v => Task.FromResult(v > 0 ? Result.Success() : Result.Failure("negative")));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task BindAsync_ToNonGeneric_OnFailure_ShouldPropagate()
    {
        Result result = await Result<int>.Failure("err").BindAsync(_ => Task.FromResult(Result.Success()));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task MatchAsync_OnSuccess_ShouldPassValueToOnSuccess()
    {
        string output = await Result<int>.Success(7).MatchAsync(
            v => Task.FromResult($"val:{v}"),
            _ => Task.FromResult("fail"));

        Assert.Equal("val:7", output);
    }

    [Fact]
    public async Task MatchAsync_OnFailure_ShouldCallOnFailure()
    {
        string output = await Result<int>.Failure("err").MatchAsync(
            v => Task.FromResult($"val:{v}"),
            _ => Task.FromResult("fail"));

        Assert.Equal("fail", output);
    }
}
