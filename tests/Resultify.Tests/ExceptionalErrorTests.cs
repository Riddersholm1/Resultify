using Resultify.Errors;

namespace Resultify.Tests;

public sealed class ExceptionalErrorNullTests
{
    [Fact]
    public void ExceptionalError_WithNullException_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => new ExceptionalError(null!));
    }

    [Fact]
    public void ExceptionalError_WithNullExceptionAndMessage_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => new ExceptionalError("msg", null!));
    }
}

public sealed class ExceptionalErrorNullMessageTests
{
    private sealed class NullMessageException : Exception
    {
        public override string Message => null!;
    }

    [Fact]
    public void ExceptionalError_WithNullMessageException_ShouldCoerceToEmpty()
    {
        var ex = new NullMessageException();

        var error = new ExceptionalError(ex);

        Assert.Equal(string.Empty, error.Message);
        Assert.Equal($"Exception.{nameof(NullMessageException)}", error.Code);
        Assert.Same(ex, error.Exception);
    }

    [Fact]
    public void Try_WhenThrowingNullMessageException_ShouldNotThrow()
    {
        Result result = Result.Try(() => throw new NullMessageException());

        Assert.True(result.IsFailure);
        Assert.IsType<ExceptionalError>(result.FirstError);
        Assert.Equal(string.Empty, result.FirstError.Message);
    }
}