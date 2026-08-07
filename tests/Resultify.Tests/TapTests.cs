namespace Resultify.Tests;

public sealed class TapIdentityTests
{
    [Fact]
    public void Tap_OnResult_ReturnsEquivalentResult()
    {
        Result original = Result.Success();
        Result tapped = original.Tap(() => { });

        Assert.Equal(original, tapped);
        Assert.True(tapped.IsSuccess);
    }

    [Fact]
    public void Tap_OnResultT_ReturnsEquivalentResult()
    {
        Result<int> original = Result<int>.Success(42);
        Result<int> tapped = original.Tap(_ => { });

        Assert.Equal(original, tapped);
        Assert.Equal(42, tapped.Value);
    }

    [Fact]
    public void TapError_OnFailedResult_ReturnsEquivalentResult()
    {
        Result original = Result.Failure("err");
        Result tapped = original.TapError(_ => { });

        Assert.Equal(original, tapped);
    }

    [Fact]
    public void TapError_OnFailedResultT_ReturnsEquivalentResult()
    {
        Result<int> original = Result<int>.Failure("err");
        Result<int> tapped = original.TapError(_ => { });

        Assert.Equal(original, tapped);
    }
}

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

public sealed class TapErrorAsyncSyncTests
{
    [Fact]
    public async Task Result_TapErrorAsync_OnFailure_ShouldExecute()
    {
        var tapped = false;
        Result result = await Result.Failure("err").TapErrorAsync(_ =>
        {
            tapped = true;
            return Task.CompletedTask;
        });

        Assert.True(tapped);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Result_TapErrorAsync_OnSuccess_ShouldNotExecute()
    {
        var tapped = false;
        Result result = await Result.Success().TapErrorAsync(_ =>
        {
            tapped = true;
            return Task.CompletedTask;
        });

        Assert.False(tapped);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ResultT_TapErrorAsync_OnFailure_ShouldExecute()
    {
        var tapped = false;
        Result<int> result = await Result<int>.Failure("err").TapErrorAsync(_ =>
        {
            tapped = true;
            return Task.CompletedTask;
        });

        Assert.True(tapped);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task ResultT_TapErrorAsync_OnSuccess_ShouldNotExecute()
    {
        var tapped = false;
        Result<int> result = await Result<int>.Success(1).TapErrorAsync(_ =>
        {
            tapped = true;
            return Task.CompletedTask;
        });

        Assert.False(tapped);
        Assert.True(result.IsSuccess);
    }
}
