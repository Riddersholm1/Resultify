using Resultify.Errors;

namespace Resultify.Tests;

public sealed class DefaultResultBehaviorTests
{
    [Fact]
    public void DefaultResultT_IsSuccess_WithDefaultValue()
    {
        Result<int> result = default;

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void DefaultResultT_String_IsSuccess_ButValueThrows()
    {
        Result<string> result = default;

        Assert.True(result.IsSuccess);
        Assert.Null(result.ValueOrDefault);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void DefaultResult_IsSuccess()
    {
        Result result = default;

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Errors);
    }
}

public sealed class DefensiveCopyTests
{
    [Fact]
    public void Failure_WithListOfErrors_ShouldNotMutateWhenCallerMutatesSource()
    {
        var errors = new List<Error> { new("err1"), new("err2") };
        Result result = Result.Failure(errors);

        errors.Add(new Error("err3"));
        errors.Clear();

        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public void FailureT_WithListOfErrors_ShouldNotMutateWhenCallerMutatesSource()
    {
        var errors = new List<Error> { new("err1") };
        Result<int> result = Result<int>.Failure(errors);

        errors.Add(new Error("err2"));

        Assert.Single(result.Errors);
    }
}