using Resultify.Errors;

namespace Resultify.Tests;

public sealed class ResultExtensionTests
{
    [Fact]
    public void Merge_CollectionOfResults_ShouldCombine()
    {
        var results = new List<Result>
        {
            Result.Success(),
            Result.Failure("err1"),
            Result.Failure("err2")
        };

        Result merged = results.Merge();

        Assert.True(merged.IsFailure);
        Assert.Equal(2, merged.Errors.Count);
    }

    [Fact]
    public void HasError_ShouldDetectSpecificErrorType()
    {
        Result result = Result.Failure(new ValidationError("Name", "Required"));

        Assert.True(result.HasError<ValidationError>());
        Assert.False(result.HasError<NotFoundError>());
    }

    [Fact]
    public void HasErrorCode_ShouldMatchCode()
    {
        Result result = Result.Failure("User.NotFound", "not found");

        Assert.True(result.HasErrorCode("User.NotFound"));
        Assert.False(result.HasErrorCode("Other"));
    }

    [Fact]
    public void HasException_ShouldDetectWrappedException()
    {
        Result result = Result.Try(() => throw new InvalidOperationException());

        Assert.True(result.HasException<InvalidOperationException>());
        Assert.False(result.HasException<ArgumentException>());
    }
}