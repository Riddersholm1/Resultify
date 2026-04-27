using Resultify.Errors;

namespace Resultify.Tests;

public sealed class SuccessNullGuardTests
{
    [Fact]
    public void ResultT_Success_WithNull_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => Result<string>.Success(null!));
    }
}

public sealed class SyncTryCancellationTests
{
    [Fact]
    public void Try_Action_WhenOperationCanceled_ShouldRethrow()
    {
        Assert.Throws<OperationCanceledException>(() => Result.Try(() => throw new OperationCanceledException()));
    }

    [Fact]
    public void TryT_WhenOperationCanceled_ShouldRethrow()
    {
        Assert.Throws<OperationCanceledException>(() => Result<int>.Try(() => throw new OperationCanceledException()));
    }
}

public sealed class TryNullReturnTests
{
    [Fact]
    public void Try_WhenFuncReturnsNull_ShouldReturnNullValueError()
    {
        Result<string> result = Result<string>.Try(() => null!);

        Assert.True(result.IsFailure);
        Assert.Equal(Error.NullValue, result.FirstError);
    }

    [Fact]
    public async Task TryAsync_WhenFuncReturnsNull_ShouldReturnNullValueError()
    {
        Result<string> result = await Result<string>.TryAsync(() => Task.FromResult<string>(null!));

        Assert.True(result.IsFailure);
        Assert.Equal(Error.NullValue, result.FirstError);
    }

    [Fact]
    public void Try_WhenFuncReturnsNonNull_ShouldSucceed()
    {
        Result<string> result = Result<string>.Try(() => "hello");

        Assert.True(result.IsSuccess);
        Assert.Equal("hello", result.Value);
    }

    [Fact]
    public void Try_WithValueType_ShouldSucceed()
    {
        Result<int> result = Result<int>.Try(() => 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value);
    }
}