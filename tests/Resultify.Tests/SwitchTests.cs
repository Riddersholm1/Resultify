namespace Resultify.Tests;

public sealed class ResultSwitchTests
{
    [Fact]
    public void Switch_OnSuccess_ShouldCallOnSuccess()
    {
        var called = false;
        Result.Success().Switch(() => called = true, _ => { });

        Assert.True(called);
    }

    [Fact]
    public void Switch_OnFailure_ShouldCallOnFailure()
    {
        var called = false;
        Result.Failure("err").Switch(() => { }, _ => called = true);

        Assert.True(called);
    }

    [Fact]
    public void Switch_OnSuccess_ShouldNotCallOnFailure()
    {
        var called = false;
        Result.Success().Switch(() => { }, _ => called = true);

        Assert.False(called);
    }

    [Fact]
    public async Task SwitchAsync_OnSuccess_ShouldCallOnSuccess()
    {
        var called = false;
        await Result.Success().SwitchAsync(
            () => { called = true; return Task.CompletedTask; },
            _ => Task.CompletedTask);

        Assert.True(called);
    }

    [Fact]
    public async Task SwitchAsync_OnFailure_ShouldCallOnFailure()
    {
        var called = false;
        await Result.Failure("err").SwitchAsync(
            () => Task.CompletedTask,
            _ => { called = true; return Task.CompletedTask; });

        Assert.True(called);
    }
}

public sealed class ResultTSwitchTests
{
    [Fact]
    public void Switch_OnSuccess_ShouldCallOnSuccess()
    {
        var received = 0;
        Result<int>.Success(42).Switch(v => received = v, _ => { });

        Assert.Equal(42, received);
    }

    [Fact]
    public void Switch_OnFailure_ShouldCallOnFailure()
    {
        var called = false;
        Result<int>.Failure("err").Switch(_ => { }, _ => called = true);

        Assert.True(called);
    }

    [Fact]
    public async Task SwitchAsync_OnSuccess_ShouldCallOnSuccess()
    {
        var received = 0;
        await Result<int>.Success(10).SwitchAsync(
            v => { received = v; return Task.CompletedTask; },
            _ => Task.CompletedTask);

        Assert.Equal(10, received);
    }

    [Fact]
    public async Task SwitchAsync_OnFailure_ShouldCallOnFailure()
    {
        var called = false;
        await Result<int>.Failure("err").SwitchAsync(
            _ => Task.CompletedTask,
            _ => { called = true; return Task.CompletedTask; });

        Assert.True(called);
    }
}