using Resultify.Errors;

namespace Resultify.Tests;

/// <summary>
/// <c>HasError</c>, <c>HasErrorCode</c> and <c>HasException</c> are how callers branch on a failure
/// without pattern-matching the whole error list. Each exists on both result types and on both task
/// receivers; all four surfaces must answer identically, including on a success.
/// </summary>
public sealed class ResultErrorQueryTests
{
    [Fact]
    public void HasError_ShouldDetectTheErrorType()
    {
        Result result = Result.Failure(new ValidationError("Name", "Required"));

        Assert.True(result.HasError<ValidationError>());
        Assert.False(result.HasError<NotFoundError>());
    }

    [Fact]
    public void HasError_ShouldMatchBaseTypes()
    {
        Result result = Result.Failure(new ValidationError("Name", "Required"));

        Assert.True(result.HasError<Error>());
    }

    [Fact]
    public void HasError_WithPredicate_ShouldFilter()
    {
        Result result = Result.Failure(new ValidationError("Email", "bad email"));

        Assert.True(result.HasError<ValidationError>(e => e.PropertyName == "Email"));
        Assert.False(result.HasError<ValidationError>(e => e.PropertyName == "Name"));
    }

    [Fact]
    public void HasErrorCode_ShouldMatchExactlyAndCaseSensitively()
    {
        Result result = Result.Failure("User.NotFound", "not found");

        Assert.True(result.HasErrorCode("User.NotFound"));
        Assert.False(result.HasErrorCode("Other"));
        Assert.False(result.HasErrorCode("user.notfound"));
    }

    [Fact]
    public void HasErrorCode_WithNull_ShouldReturnFalseRatherThanThrow() =>
        Assert.False(Result.Failure("User.NotFound", "not found").HasErrorCode(null!));

    [Fact]
    public void HasException_ShouldDetectTheWrappedExceptionType()
    {
        Result result = Result.Try((Action)(() => throw new InvalidOperationException()));

        Assert.True(result.HasException<InvalidOperationException>());
        Assert.False(result.HasException<ArgumentException>());
    }

    [Fact]
    public void HasException_ShouldMatchBaseExceptionTypes()
    {
        Result result = Result.Try((Action)(() => throw new InvalidOperationException()));

        Assert.True(result.HasException<Exception>());
    }

    [Fact]
    public void HasException_WhenTheFailureIsNotExceptional_ShouldBeFalse() =>
        Assert.False(Result.Failure("plain").HasException<Exception>());

    [Fact]
    public void Queries_OnASuccess_ShouldAllReturnFalse()
    {
        Result success = Result.Success();

        Assert.False(success.HasError<Error>());
        Assert.False(success.HasError<ValidationError>(_ => true));
        Assert.False(success.HasErrorCode("anything"));
        Assert.False(success.HasException<Exception>());
    }

    [Fact]
    public void Queries_ShouldSearchEveryErrorNotJustTheFirst()
    {
        Result result = Result.Failure([new Error("First", "first"), new NotFoundError("Customer", 1)]);

        Assert.True(result.HasError<NotFoundError>());
        Assert.True(result.HasErrorCode("Customer.NotFound"));
    }
}

public sealed class ResultTErrorQueryTests
{
    [Fact]
    public void HasError_ShouldDetectTheErrorType()
    {
        Result<int> result = Result<int>.Failure(new ValidationError("Name", "Required"));

        Assert.True(result.HasError<ValidationError>());
        Assert.False(result.HasError<NotFoundError>());
    }

    [Fact]
    public void HasError_WithPredicate_ShouldFilter()
    {
        Result<int> result = Result<int>.Failure(new ValidationError("Email", "Invalid"));

        Assert.True(result.HasError<ValidationError>(e => e.PropertyName == "Email"));
        Assert.False(result.HasError<ValidationError>(e => e.PropertyName == "Name"));
    }

    [Fact]
    public void HasErrorCode_ShouldMatch()
    {
        Result<int> result = Result<int>.Failure("User.NotFound", "gone");

        Assert.True(result.HasErrorCode("User.NotFound"));
        Assert.False(result.HasErrorCode("Other"));
    }

    [Fact]
    public void HasException_ShouldDetectTheWrappedExceptionType()
    {
        Result<int> result = Result<int>.Try((Func<int>)(() => throw new InvalidOperationException()));

        Assert.True(result.HasException<InvalidOperationException>());
        Assert.False(result.HasException<ArgumentException>());
    }

    [Fact]
    public void Queries_OnASuccess_ShouldAllReturnFalse()
    {
        Result<int> success = Result<int>.Success(1);

        Assert.False(success.HasError<Error>());
        Assert.False(success.HasErrorCode("anything"));
        Assert.False(success.HasException<Exception>());
    }
}

/// <summary>
/// The same queries reached through a <c>Task</c> receiver. Note the two call styles forced by
/// C# extension-block type inference — see the remarks on <c>ResultExtensions</c>.
/// </summary>
public sealed class AsyncErrorQueryTests
{
    [Fact]
    public async Task NonGeneric_HasError_ShouldDetectTheErrorType()
    {
        Task<Result> task = Task.FromResult(Result.Failure(new ValidationError("Email", "bad")));

        Assert.True(await task.HasError<ValidationError>());
    }

    [Fact]
    public async Task NonGeneric_HasError_ShouldReturnFalseWhenAbsent()
    {
        Task<Result> task = Task.FromResult(Result.Failure("plain"));

        Assert.False(await task.HasError<NotFoundError>());
    }

    [Fact]
    public async Task NonGeneric_HasError_WithPredicate_ShouldFilter()
    {
        Task<Result> task = Task.FromResult(Result.Failure(new ValidationError("Email", "bad")));

        Assert.True(await task.HasError<ValidationError>(e => e.PropertyName == "Email"));
        Assert.False(await task.HasError<ValidationError>(e => e.PropertyName == "Name"));
    }

    [Fact]
    public async Task NonGeneric_HasErrorCode_ShouldMatch()
    {
        Task<Result> task = Task.FromResult(Result.Failure("User.NotFound", "gone"));

        Assert.True(await task.HasErrorCode("User.NotFound"));
        Assert.False(await task.HasErrorCode("Other"));
    }

    [Fact]
    public async Task NonGeneric_HasException_ShouldDetectWrappedException()
    {
        Task<Result> task = Task.FromResult(Result.Try((Action)(() => throw new InvalidOperationException())));

        Assert.True(await task.HasException<InvalidOperationException>());
        Assert.False(await task.HasException<ArgumentException>());
    }

    [Fact]
    public async Task Typed_HasErrorCode_ShouldMatch()
    {
        Task<Result<int>> task = Task.FromResult(Result<int>.Failure("User.NotFound", "gone"));

        Assert.True(await task.HasErrorCode("User.NotFound"));
        Assert.False(await task.HasErrorCode("Other"));
    }

    [Fact]
    public async Task Typed_HasError_WithExplicitlyTypedLambda_ShouldInferAndFilter()
    {
        Task<Result<int>> task = Task.FromResult(Result<int>.Failure(new ValidationError("Email", "bad")));

        Assert.True(await task.HasError((ValidationError e) => e.PropertyName == "Email"));
        Assert.False(await task.HasError((ValidationError e) => e.PropertyName == "Name"));
    }

    /// <summary>
    /// On a <c>Task&lt;Result&lt;T&gt;&gt;</c> receiver these two need either the static call form or
    /// an await first — <c>task.HasError&lt;ValidationError&gt;()</c> does not compile. Pinning both
    /// documented workarounds so the guidance cannot drift from reality.
    /// </summary>
    [Fact]
    public async Task Typed_TypeArgumentOnlyQueries_ShouldWorkViaTheDocumentedForms()
    {
        Task<Result<int>> task = Task.FromResult(Result<int>.Failure(new ValidationError("Email", "bad")));

        Assert.True(await ResultExtensions.HasError<int, ValidationError>(task));
        Assert.True((await task).HasError<ValidationError>());

        Task<Result<int>> faulted = Task.FromResult(Result<int>.Try((Func<int>)(() => throw new TimeoutException())));

        Assert.True(await ResultExtensions.HasException<int, TimeoutException>(faulted));
        Assert.True((await faulted).HasException<TimeoutException>());
    }
}
