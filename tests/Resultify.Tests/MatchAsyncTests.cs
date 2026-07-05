namespace Resultify.Tests;

public sealed class ResultMatchAsyncTests
{
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

public sealed class ResultTMatchAsyncTests
{
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