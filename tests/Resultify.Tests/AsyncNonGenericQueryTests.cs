using Resultify.Errors;

namespace Resultify.Tests;

public sealed class AsyncNonGenericHasErrorTests
{
    [Fact]
    public async Task HasError_OnAsyncResult_ShouldDetectErrorType()
    {
        Task<Result> task = Task.FromResult(Result.Failure(new ValidationError("Email", "bad")));

        Assert.True(await task.HasError<ValidationError>());
    }

    [Fact]
    public async Task HasError_OnAsyncResult_ShouldReturnFalseWhenAbsent()
    {
        Task<Result> task = Task.FromResult(Result.Failure("plain"));

        Assert.False(await task.HasError<NotFoundError>());
    }

    [Fact]
    public async Task HasErrorCode_OnAsyncResult_ShouldMatch()
    {
        Task<Result> task = Task.FromResult(Result.Failure("User.NotFound", "gone"));

        Assert.True(await task.HasErrorCode("User.NotFound"));
        Assert.False(await task.HasErrorCode("Other"));
    }

    [Fact]
    public async Task HasException_OnAsyncResult_ShouldDetectWrappedException()
    {
        Task<Result> task = Task.FromResult(Result.Try(() => throw new InvalidOperationException()));

        Assert.True(await task.HasException<InvalidOperationException>());
        Assert.False(await task.HasException<ArgumentException>());
    }
}

public sealed class AsyncNonGenericEnsureLazyFactoryTests
{
    [Fact]
    public async Task Ensure_OnAsyncResult_WithLazyFactory_ShouldNotInvokeFactoryOnPass()
    {
        var called = false;
        Result result = await Task.FromResult(Result.Success())
            .Ensure(() => true, () =>
            {
                called = true;
                return new Error("never");
            });

        Assert.True(result.IsSuccess);
        Assert.False(called);
    }

    [Fact]
    public async Task Ensure_OnAsyncResult_WithLazyFactory_ShouldInvokeFactoryOnFail()
    {
        Result result = await Task.FromResult(Result.Success())
            .Ensure(() => false, () => new Error("lazy fail"));

        Assert.True(result.IsFailure);
        Assert.Equal("lazy fail", result.FirstError.Message);
    }

    [Fact]
    public async Task Ensure_OnAsyncResult_WithError_ShouldGate()
    {
        Result result = await Task.FromResult(Result.Success())
            .Ensure(() => false, new Error("gated"));

        Assert.True(result.IsFailure);
        Assert.Equal("gated", result.FirstError.Message);
    }
}