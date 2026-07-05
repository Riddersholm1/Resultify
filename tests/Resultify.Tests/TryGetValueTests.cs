namespace Resultify.Tests;

public sealed class TryGetValueTests
{
    [Fact]
    public void TryGetValue_OnSuccess_ShouldReturnTrueAndValue()
    {
        Result<int> result = Result<int>.Success(42);

        bool ok = result.TryGetValue(out int value);

        Assert.True(ok);
        Assert.Equal(42, value);
    }

    [Fact]
    public void TryGetValue_OnFailure_ShouldReturnFalseAndDefault()
    {
        Result<int> result = Result<int>.Failure("boom");

        bool ok = result.TryGetValue(out int value);

        Assert.False(ok);
        Assert.Equal(0, value);
    }

    [Fact]
    public void TryGetValue_OnDefaultReferenceType_ShouldReturnFalse()
    {
        Result<string> result = default;

        bool ok = result.TryGetValue(out string? value);

        Assert.False(ok);
        Assert.Null(value);
    }

    [Fact]
    public void TryGetValue_OnSuccessWithReferenceType_ShouldReturnTrueAndValue()
    {
        Result<string> result = Result<string>.Success("hello");

        bool ok = result.TryGetValue(out string? value);

        Assert.True(ok);
        Assert.Equal("hello", value);
    }
}