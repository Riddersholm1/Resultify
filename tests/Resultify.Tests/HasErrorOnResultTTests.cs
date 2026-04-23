using Resultify.Errors;

namespace Resultify.Tests;

public sealed class HasErrorOnResultTTests
{
    [Fact]
    public void HasError_OnTypedResult_ShouldDetectErrorType()
    {
        Result<int> result = Result<int>.Failure(new ValidationError("Name", "Required"));

        Assert.True(result.HasError<ValidationError>());
        Assert.False(result.HasError<NotFoundError>());
    }

    [Fact]
    public void HasError_WithPredicate_OnTypedResult_ShouldWork()
    {
        Result<int> result = Result<int>.Failure(new ValidationError("Email", "Invalid"));

        Assert.True(result.HasError<ValidationError>(e => e.PropertyName == "Email"));
        Assert.False(result.HasError<ValidationError>(e => e.PropertyName == "Name"));
    }

    [Fact]
    public void HasErrorCode_OnTypedResult_ShouldWork()
    {
        Result<int> result = Result<int>.Failure("User.NotFound", "gone");

        Assert.True(result.HasErrorCode("User.NotFound"));
        Assert.False(result.HasErrorCode("Other"));
    }

    [Fact]
    public void HasException_OnTypedResult_ShouldWork()
    {
        Result<int> result = Result<int>.Try(() => throw new InvalidOperationException());

        Assert.True(result.HasException<InvalidOperationException>());
        Assert.False(result.HasException<ArgumentException>());
    }
}