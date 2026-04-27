using Resultify.Errors;

namespace Resultify.Tests;

public sealed class ErrorDeconstructTests
{
    [Fact]
    public void Deconstruct_ShouldProduceCodeAndMessage()
    {
        var error = new Error("E.Code", "A message");
        (string code, string message) = error;

        Assert.Equal("E.Code", code);
        Assert.Equal("A message", message);
    }

    [Fact]
    public void Deconstruct_OnMessageOnlyError_CodeShouldBeEmpty()
    {
        var error = new Error("just a message");
        (string code, string message) = error;

        Assert.Equal(string.Empty, code);
        Assert.Equal("just a message", message);
    }
}

public sealed class ExceptionalErrorCustomMessageTests
{
    [Fact]
    public void ExceptionalError_WithCustomMessage_ShouldUseProvidedMessage()
    {
        var ex = new InvalidOperationException("inner message");
        var error = new ExceptionalError("custom message", ex);

        Assert.Equal("custom message", error.Message);
        Assert.Equal("Exception.InvalidOperationException", error.Code);
        Assert.Same(ex, error.Exception);
    }
}

public sealed class ResultFailureNullElementTests
{
    [Fact]
    public void Failure_EnumerableWithNullElement_ShouldThrow()
    {
        IEnumerable<Error> errors = [new("e1"), null!, new("e2")];

        Assert.Throws<ArgumentException>(() => Result.Failure(errors));
    }

    [Fact]
    public void FailureT_EnumerableWithNullElement_ShouldThrow()
    {
        IEnumerable<Error> errors = [new("e1"), null!];

        Assert.Throws<ArgumentException>(() => Result<int>.Failure(errors));
    }
}

public sealed class ResultHasErrorWithPredicateTests
{
    [Fact]
    public void HasError_WithMatchingPredicate_ShouldReturnTrue()
    {
        Result result = Result.Failure(new ValidationError("Email", "bad email"));

        Assert.True(result.HasError<ValidationError>(e => e.PropertyName == "Email"));
    }

    [Fact]
    public void HasError_WithNonMatchingPredicate_ShouldReturnFalse()
    {
        Result result = Result.Failure(new ValidationError("Email", "bad email"));

        Assert.False(result.HasError<ValidationError>(e => e.PropertyName == "Name"));
    }
}