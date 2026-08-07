using Resultify.Errors;

namespace Resultify.Tests;

/// <summary>
/// <c>Try</c>/<c>TryAsync</c> is the only part of the library that converts exceptions into
/// failures. There are eight overloads across the two result types; each is covered here for the
/// normal path, the throwing path, the custom <c>exceptionHandler</c>, and cancellation.
/// </summary>
/// <remarks>
/// Note the explicit casts on the throwing lambdas: a throw-only lambda body is convertible to both
/// <c>Action</c> and <c>Func&lt;Result&gt;</c>, and overload resolution picks the <c>Func</c> form.
/// Without the cast, the "action" tests would silently exercise the wrong overload.
/// </remarks>
public sealed class ResultTryTests
{
    [Fact]
    public void Try_Action_WhenNoException_ShouldSucceed()
    {
        var ran = false;

        Result result = Result.Try(() => { ran = true; });

        Assert.True(result.IsSuccess);
        Assert.True(ran);
    }

    [Fact]
    public void Try_Action_WhenException_ShouldWrapAsExceptionalError()
    {
        Result result = Result.Try((Action)(() => throw new InvalidOperationException("boom")));

        Assert.True(result.IsFailure);
        ExceptionalError error = Assert.IsType<ExceptionalError>(result.FirstError);
        Assert.Equal("Exception.InvalidOperationException", error.Code);
        Assert.Equal("boom", error.Message);
    }

    [Fact]
    public void Try_FuncResult_WhenSuccessful_ShouldReturnInnerSuccess() =>
        Assert.True(Result.Try(Result.Success).IsSuccess);

    [Fact]
    public void Try_FuncResult_WhenInnerFailure_ShouldPassTheFailureThroughUntouched()
    {
        Result result = Result.Try(() => Result.Failure("inner"));

        Assert.True(result.IsFailure);
        Assert.Equal("inner", result.FirstError.Message);
        Assert.IsNotType<ExceptionalError>(result.FirstError);
    }

    [Fact]
    public void Try_FuncResult_WhenException_ShouldWrapAsExceptionalError()
    {
        Result result = Result.Try((Func<Result>)(() => throw new InvalidOperationException("boom")));

        Assert.True(result.IsFailure);
        Assert.IsType<ExceptionalError>(result.FirstError);
    }

    [Fact]
    public async Task TryAsync_FuncTask_WhenNoException_ShouldSucceed() =>
        Assert.True((await Result.TryAsync(() => Task.CompletedTask)).IsSuccess);

    [Fact]
    public async Task TryAsync_FuncTask_WhenException_ShouldWrapAsExceptionalError()
    {
        Result result = await Result.TryAsync((Func<Task>)(() => throw new InvalidOperationException("async boom")));

        Assert.True(result.IsFailure);
        Assert.IsType<ExceptionalError>(result.FirstError);
        Assert.Equal("async boom", result.FirstError.Message);
    }

    [Fact]
    public async Task TryAsync_FuncTask_WhenTheTaskFaults_ShouldWrapAsExceptionalError()
    {
        Result result = await Result.TryAsync(() => Task.FromException(new TimeoutException("timed out")));

        Assert.True(result.IsFailure);
        Assert.True(result.HasException<TimeoutException>());
    }

    [Fact]
    public async Task TryAsync_FuncTaskResult_WhenSuccessful_ShouldReturnSuccess() =>
        Assert.True((await Result.TryAsync(() => Task.FromResult(Result.Success()))).IsSuccess);

    [Fact]
    public async Task TryAsync_FuncTaskResult_WhenInnerFailure_ShouldReturnFailure()
    {
        Result result = await Result.TryAsync(() => Task.FromResult(Result.Failure("inner")));

        Assert.True(result.IsFailure);
        Assert.Equal("inner", result.FirstError.Message);
    }

    [Fact]
    public async Task TryAsync_FuncTaskResult_WhenException_ShouldWrapAsExceptionalError()
    {
        Result result = await Result.TryAsync((Func<Task<Result>>)(() => throw new InvalidOperationException("boom")));

        Assert.True(result.IsFailure);
        Assert.IsType<ExceptionalError>(result.FirstError);
    }
}

public sealed class ResultTTryTests
{
    [Fact]
    public void Try_Func_WhenNoException_ShouldReturnValue()
    {
        Result<int> result = Result<int>.Try(() => 42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Try_Func_WhenException_ShouldWrapAsExceptionalError()
    {
        Result<int> result = Result<int>.Try((Func<int>)(() => throw new InvalidOperationException("boom")));

        Assert.True(result.IsFailure);
        Assert.IsType<ExceptionalError>(result.FirstError);
    }

    [Fact]
    public void Try_Func_WhenReturningNull_ShouldFailWithNullValue()
    {
        Result<string> result = Result<string>.Try(() => null!);

        Assert.True(result.IsFailure);
        Assert.Equal(Error.NullValue, result.FirstError);
    }

    [Fact]
    public void Try_Func_WhenReturningANonNullReference_ShouldSucceed()
    {
        Result<string> result = Result<string>.Try(() => "hello");

        Assert.True(result.IsSuccess);
        Assert.Equal("hello", result.Value);
    }

    [Fact]
    public void Try_Func_WithValueTypeDefault_ShouldSucceed()
    {
        Result<int> result = Result<int>.Try(() => 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value);
    }

    [Fact]
    public void Try_FuncResultT_WhenSuccessful_ShouldReturnInnerSuccess()
    {
        Result<int> result = Result<int>.Try(() => Result<int>.Success(42));

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Try_FuncResultT_WhenInnerFailure_ShouldPassTheFailureThroughUntouched()
    {
        Result<int> result = Result<int>.Try(() => Result<int>.Failure("inner"));

        Assert.True(result.IsFailure);
        Assert.Equal("inner", result.FirstError.Message);
        Assert.IsNotType<ExceptionalError>(result.FirstError);
    }

    [Fact]
    public void Try_FuncResultT_WhenException_ShouldWrapAsExceptionalError()
    {
        Result<int> result = Result<int>.Try((Func<Result<int>>)(() => throw new InvalidOperationException("boom")));

        Assert.True(result.IsFailure);
        Assert.IsType<ExceptionalError>(result.FirstError);
    }

    [Fact]
    public async Task TryAsync_FuncTaskT_ShouldReturnValue()
    {
        Result<int> result = await Result<int>.TryAsync(() => Task.FromResult(42));

        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task TryAsync_FuncTaskT_WhenReturningNull_ShouldFailWithNullValue()
    {
        Result<string> result = await Result<string>.TryAsync(() => Task.FromResult<string>(null!));

        Assert.True(result.IsFailure);
        Assert.Equal(Error.NullValue, result.FirstError);
    }

    [Fact]
    public async Task TryAsync_FuncTaskT_WhenException_ShouldWrapAsExceptionalError()
    {
        Result<int> result = await Result<int>.TryAsync((Func<Task<int>>)(() => throw new InvalidOperationException("boom")));

        Assert.True(result.IsFailure);
        Assert.IsType<ExceptionalError>(result.FirstError);
    }

    [Fact]
    public async Task TryAsync_FuncTaskResultT_WhenSuccessful_ShouldReturnSuccess()
    {
        Result<int> result = await Result<int>.TryAsync(() => Task.FromResult(Result<int>.Success(7)));

        Assert.Equal(7, result.Value);
    }

    [Fact]
    public async Task TryAsync_FuncTaskResultT_WhenInnerFailure_ShouldReturnFailure()
    {
        Result<int> result = await Result<int>.TryAsync(() => Task.FromResult(Result<int>.Failure("inner")));

        Assert.Equal("inner", result.FirstError.Message);
    }

    [Fact]
    public async Task TryAsync_FuncTaskResultT_WhenException_ShouldWrapAsExceptionalError()
    {
        Result<int> result = await Result<int>.TryAsync((Func<Task<Result<int>>>)(() => throw new InvalidOperationException("boom")));

        Assert.True(result.IsFailure);
        Assert.IsType<ExceptionalError>(result.FirstError);
    }
}

/// <summary>
/// The optional <c>exceptionHandler</c> lets callers map an exception onto a domain error instead of
/// an <see cref="ExceptionalError"/>. It is applied by all eight overloads, and each of them falls
/// back to <see cref="ExceptionalError"/> when the handler returns null.
/// </summary>
public sealed class TryExceptionHandlerTests
{
    private static readonly Func<Exception, Error> Handler =
        ex => new Error("Mapped.Failure", $"mapped: {ex.Message}").CausedBy(ex);

    private static readonly Func<Exception, Error> NullReturningHandler = _ => null!;

    private static void AssertMapped(Result result)
    {
        Assert.True(result.IsFailure);
        Assert.Equal("Mapped.Failure", result.FirstError.Code);
        Assert.Equal("mapped: boom", result.FirstError.Message);
        Assert.Single(result.FirstError.Causes);
        Assert.IsNotType<ExceptionalError>(result.FirstError);
    }

    [Fact]
    public void Try_Action_WithHandler_ShouldUseTheMappedError() =>
        AssertMapped(Result.Try((Action)(() => throw new InvalidOperationException("boom")), Handler));

    [Fact]
    public void Try_FuncResult_WithHandler_ShouldUseTheMappedError() =>
        AssertMapped(Result.Try((Func<Result>)(() => throw new InvalidOperationException("boom")), Handler));

    [Fact]
    public async Task TryAsync_FuncTask_WithHandler_ShouldUseTheMappedError() =>
        AssertMapped(await Result.TryAsync((Func<Task>)(() => throw new InvalidOperationException("boom")), Handler));

    [Fact]
    public async Task TryAsync_FuncTaskResult_WithHandler_ShouldUseTheMappedError() =>
        AssertMapped(await Result.TryAsync((Func<Task<Result>>)(() => throw new InvalidOperationException("boom")), Handler));

    [Fact]
    public void TryT_Func_WithHandler_ShouldUseTheMappedError() =>
        AssertMapped(Result<int>.Try((Func<int>)(() => throw new InvalidOperationException("boom")), Handler).ToResult());

    [Fact]
    public void TryT_FuncResultT_WithHandler_ShouldUseTheMappedError() =>
        AssertMapped(Result<int>.Try((Func<Result<int>>)(() => throw new InvalidOperationException("boom")), Handler).ToResult());

    [Fact]
    public async Task TryAsyncT_FuncTaskT_WithHandler_ShouldUseTheMappedError() =>
        AssertMapped((await Result<int>.TryAsync((Func<Task<int>>)(() => throw new InvalidOperationException("boom")), Handler)).ToResult());

    [Fact]
    public async Task TryAsyncT_FuncTaskResultT_WithHandler_ShouldUseTheMappedError() =>
        AssertMapped((await Result<int>.TryAsync((Func<Task<Result<int>>>)(() => throw new InvalidOperationException("boom")), Handler)).ToResult());

    [Fact]
    public void Try_WithHandler_ShouldNotRunWhenNothingThrows()
    {
        var handlerCalls = 0;

        Result result = Result.Try(() => { }, _ => { handlerCalls++; return new Error("never"); });

        Assert.True(result.IsSuccess);
        Assert.Equal(0, handlerCalls);
    }

    [Fact]
    public void Try_WhenHandlerReturnsNull_ShouldFallBackToExceptionalError()
    {
        Result result = Result.Try((Action)(() => throw new InvalidOperationException("boom")), NullReturningHandler);

        Assert.True(result.IsFailure);
        Assert.IsType<ExceptionalError>(result.FirstError);
        Assert.Equal("boom", result.FirstError.Message);
    }

    [Fact]
    public async Task TryAsync_WhenHandlerReturnsNull_ShouldFallBackToExceptionalError()
    {
        Result result = await Result.TryAsync(
            (Func<Task>)(() => throw new InvalidOperationException("boom")),
            NullReturningHandler);

        Assert.IsType<ExceptionalError>(result.FirstError);
    }

    [Fact]
    public void TryT_WhenHandlerReturnsNull_ShouldFallBackToExceptionalError()
    {
        Result<int> result = Result<int>.Try((Func<int>)(() => throw new InvalidOperationException("boom")), NullReturningHandler);

        Assert.IsType<ExceptionalError>(result.FirstError);
    }

    [Fact]
    public void Try_WhenHandlerItselfThrows_ShouldPropagateThatException()
    {
        Exception ex = Assert.ThrowsAny<Exception>(() =>
            Result.Try(
                (Action)(() => throw new InvalidOperationException("original")),
                _ => throw new NotSupportedException("from handler")));

        Assert.IsType<NotSupportedException>(ex);
        Assert.Equal("from handler", ex.Message);
    }
}

/// <summary>
/// Cancellation must never be swallowed into a failed result: it has to reach the caller's own
/// handler, or a cancelled operation would look like an ordinary domain failure.
/// </summary>
public sealed class TryCancellationTests
{
    [Fact]
    public void Try_Action_WhenOperationCanceled_ShouldRethrow() =>
        Assert.Throws<OperationCanceledException>(() =>
            Result.Try((Action)(() => throw new OperationCanceledException())));

    [Fact]
    public void Try_FuncResult_WhenOperationCanceled_ShouldRethrow() =>
        Assert.Throws<OperationCanceledException>(() =>
            Result.Try((Func<Result>)(() => throw new OperationCanceledException())));

    [Fact]
    public void TryT_Func_WhenOperationCanceled_ShouldRethrow() =>
        Assert.Throws<OperationCanceledException>(() =>
            Result<int>.Try((Func<int>)(() => throw new OperationCanceledException())));

    [Fact]
    public void TryT_FuncResultT_WhenOperationCanceled_ShouldRethrow() =>
        Assert.Throws<OperationCanceledException>(() =>
            Result<int>.Try((Func<Result<int>>)(() => throw new OperationCanceledException())));

    [Fact]
    public async Task TryAsync_WhenOperationCanceled_ShouldRethrow() =>
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            Result.TryAsync((Func<Task>)(() => throw new OperationCanceledException())));

    [Fact]
    public async Task TryAsyncT_WhenOperationCanceled_ShouldRethrow() =>
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            Result<int>.TryAsync((Func<Task<int>>)(() => throw new OperationCanceledException())));

    [Fact]
    public async Task TryAsync_WhenTaskCanceled_ShouldRethrow() =>
        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            Result.TryAsync(() => Task.FromCanceled(new CancellationToken(true))));

    [Fact]
    public async Task TryAsync_WhenCancelledViaToken_ShouldRethrowEvenWithAHandler()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Result.TryAsync(
                async () => await Task.Delay(Timeout.Infinite, cts.Token),
                _ => new Error("should.not.be.used")));
    }
}

public sealed class SuccessNullGuardTests
{
    [Fact]
    public void ResultT_Success_WithNull_ShouldThrow() =>
        Assert.Throws<ArgumentNullException>(() => Result<string>.Success(null!));
}
