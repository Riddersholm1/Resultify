using Resultify.Errors;

namespace Resultify.Tests;

public sealed class ResultBindToTypedTests
{
    [Fact]
    public void Bind_OnSuccess_ShouldReturnTypedResult()
    {
        Result<int> result = Result.Success().Bind(() => Result<int>.Success(7));

        Assert.True(result.IsSuccess);
        Assert.Equal(7, result.Value);
    }

    [Fact]
    public void Bind_OnFailure_ShouldPropagateErrors()
    {
        var executed = false;
        Result<int> result = Result.Failure("upstream").Bind(() =>
        {
            executed = true;
            return Result<int>.Success(7);
        });

        Assert.False(executed);
        Assert.True(result.IsFailure);
        Assert.Equal("upstream", result.FirstError.Message);
    }
}

public sealed class ResultTBindToNonGenericTests
{
    [Fact]
    public void Bind_OnSuccess_ShouldChainToNonGenericResult()
    {
        Result result = Result<int>.Success(5).Bind(v => v > 0 ? Result.Success() : Result.Failure("neg"));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Bind_OnFailure_ShouldPropagateWithoutCalling()
    {
        var executed = false;
        Result result = Result<int>.Failure("err").Bind(_ =>
        {
            executed = true;
            return Result.Success();
        });

        Assert.False(executed);
        Assert.True(result.IsFailure);
    }
}

public sealed class ResultToResultTTests
{
    [Fact]
    public void ToResult_WithValue_OnSuccess_ShouldProduceTypedSuccess()
    {
        Result<string> result = Result.Success().ToResult("hello");

        Assert.True(result.IsSuccess);
        Assert.Equal("hello", result.Value);
    }

    [Fact]
    public void ToResult_WithValue_OnFailure_ShouldPropagateErrors()
    {
        Result<string> result = Result.Failure("err").ToResult("hello");

        Assert.True(result.IsFailure);
        Assert.Equal("err", result.FirstError.Message);
    }
}

public sealed class ResultDeconstructTests
{
    [Fact]
    public void Deconstruct_OnSuccess_ShouldReturnTrueAndEmptyErrors()
    {
        (bool isSuccess, IReadOnlyList<Error> errors) = Result.Success();

        Assert.True(isSuccess);
        Assert.Empty(errors);
    }

    [Fact]
    public void Deconstruct_OnFailure_ShouldReturnFalseAndErrors()
    {
        (bool isSuccess, IReadOnlyList<Error> errors) = Result.Failure("boom");

        Assert.False(isSuccess);
        Assert.Single(errors);
        Assert.Equal("boom", errors[0].Message);
    }
}