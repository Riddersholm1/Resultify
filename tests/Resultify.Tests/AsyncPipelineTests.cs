namespace Resultify.Tests;

public sealed class AsyncPipelineTests
{
    [Fact]
    public async Task MapAsync_ShouldTransformValue()
    {
        Result<int> result = await Result<int>.Success(5)
            .MapAsync(v => Task.FromResult(v * 2));

        Assert.Equal(10, result.Value);
    }

    [Fact]
    public async Task BindAsync_ShouldChain()
    {
        Result<string> result = await Result<int>.Success(5)
            .BindAsync(v => Task.FromResult(Result<string>.Success($"Value: {v}")));

        Assert.Equal("Value: 5", result.Value);
    }

    [Fact]
    public async Task TryAsync_WhenNoException_ShouldSucceed()
    {
        Result result = await Result.TryAsync(() => Task.CompletedTask);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task TryAsync_WhenException_ShouldFail()
    {
        Result result = await Result.TryAsync(
            () => throw new InvalidOperationException("async boom"));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task TryAsync_Typed_ShouldReturnValue()
    {
        Result<int> result = await Result<int>.TryAsync(() => Task.FromResult(42));

        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task AsyncPipeline_MapThenBind_ShouldWork()
    {
        Result<string> result = await Task.FromResult(Result<int>.Success(5))
            .Map(v => v * 2)
            .Bind(v => v > 5
                ? Result<string>.Success($"Big: {v}")
                : Result<string>.Failure("Too small"));

        Assert.Equal("Big: 10", result.Value);
    }

    [Fact]
    public async Task TryAsync_WhenOperationCanceled_ShouldRethrow()
    {
        await Assert.ThrowsAsync<OperationCanceledException>(() => Result.TryAsync(() => throw new OperationCanceledException()));
    }

    [Fact]
    public async Task TryAsync_Typed_WhenOperationCanceled_ShouldRethrow()
    {
        await Assert.ThrowsAsync<OperationCanceledException>(() => Result<int>.TryAsync(() => throw new OperationCanceledException()));
    }

    [Fact]
    public async Task TryAsync_WhenTaskCanceled_ShouldRethrow()
    {
        await Assert.ThrowsAsync<TaskCanceledException>(() => Result.TryAsync(() => Task.FromCanceled(new CancellationToken(true))));
    }

    [Fact]
    public async Task AsyncTyped_TapError_ShouldExecuteOnFailure()
    {
        var tapped = false;
        await Task.FromResult(Result<int>.Failure("err"))
            .TapError(_ => tapped = true);

        Assert.True(tapped);
    }

    [Fact]
    public async Task AsyncTyped_TapError_ShouldNotExecuteOnSuccess()
    {
        var tapped = false;
        await Task.FromResult(Result<int>.Success(1))
            .TapError(_ => tapped = true);

        Assert.False(tapped);
    }

    [Fact]
    public async Task AsyncTyped_Switch_ShouldCallOnSuccess()
    {
        var called = false;
        await Task.FromResult(Result<int>.Success(42))
            .Switch(v => called = v == 42, _ => { });

        Assert.True(called);
    }

    [Fact]
    public async Task AsyncTyped_Switch_ShouldCallOnFailure()
    {
        var called = false;
        await Task.FromResult(Result<int>.Failure("err"))
            .Switch(_ => { }, _ => called = true);

        Assert.True(called);
    }

    [Fact]
    public async Task AsyncTyped_MatchAsync_ShouldWork()
    {
        string output = await Task.FromResult(Result<int>.Success(5))
            .MatchAsync(
                v => Task.FromResult($"ok:{v}"),
                _ => Task.FromResult("fail"));

        Assert.Equal("ok:5", output);
    }

    [Fact]
    public async Task AsyncTyped_BindToNonGenericResult_ShouldWork()
    {
        Result result = await Task.FromResult(Result<int>.Success(5))
            .Bind(v => v > 0 ? Result.Success() : Result.Failure("negative"));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task AsyncTyped_BindAsyncToNonGenericResult_ShouldWork()
    {
        Result result = await Task.FromResult(Result<int>.Success(5))
            .BindAsync(v => Task.FromResult(v > 0 ? Result.Success() : Result.Failure("negative")));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task AsyncNonGeneric_TapError_ShouldExecuteOnFailure()
    {
        var tapped = false;
        await Task.FromResult(Result.Failure("err"))
            .TapError(_ => tapped = true);

        Assert.True(tapped);
    }

    [Fact]
    public async Task AsyncNonGeneric_Switch_ShouldCallOnSuccess()
    {
        var called = false;
        await Task.FromResult(Result.Success())
            .Switch(() => called = true, _ => { });

        Assert.True(called);
    }
}