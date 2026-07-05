namespace Resultify.Tests;

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