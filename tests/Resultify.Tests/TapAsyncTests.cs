namespace Resultify.Tests;

public sealed class ResultTapAsyncTests
{
    [Fact]
    public async Task TapAsync_OnSuccess_ShouldExecuteSideEffect()
    {
        var tapped = false;
        await Result.Success().TapAsync(() =>
        {
            tapped = true;
            return Task.CompletedTask;
        });

        Assert.True(tapped);
    }

    [Fact]
    public async Task TapAsync_OnFailure_ShouldNotExecute()
    {
        var tapped = false;
        await Result.Failure("err").TapAsync(() =>
        {
            tapped = true;
            return Task.CompletedTask;
        });

        Assert.False(tapped);
    }

    [Fact]
    public async Task TapAsync_ShouldReturnSameResult()
    {
        Result original = Result.Success();
        Result returned = await original.TapAsync(() => Task.CompletedTask);

        Assert.Equal(original, returned);
    }
}

public sealed class ResultTTapAsyncTests
{
    [Fact]
    public async Task TapAsync_OnSuccess_ShouldExecuteWithValue()
    {
        var received = 0;
        await Result<int>.Success(99).TapAsync(v =>
        {
            received = v;
            return Task.CompletedTask;
        });

        Assert.Equal(99, received);
    }

    [Fact]
    public async Task TapAsync_OnFailure_ShouldNotExecute()
    {
        var tapped = false;
        await Result<int>.Failure("err").TapAsync(_ =>
        {
            tapped = true;
            return Task.CompletedTask;
        });

        Assert.False(tapped);
    }

    [Fact]
    public async Task TapAsync_ShouldReturnSameResult()
    {
        Result<int> original = Result<int>.Success(5);
        Result<int> returned = await original.TapAsync(_ => Task.CompletedTask);

        Assert.Equal(original, returned);
    }
}
