namespace Resultify.Tests;

public sealed class TapIdentityTests
{
    [Fact]
    public void Tap_OnResult_ReturnsEquivalentResult()
    {
        Result original = Result.Success();
        Result tapped = original.Tap(() => { });

        Assert.Equal(original, tapped);
        Assert.True(tapped.IsSuccess);
    }

    [Fact]
    public void Tap_OnResultT_ReturnsEquivalentResult()
    {
        Result<int> original = Result<int>.Success(42);
        Result<int> tapped = original.Tap(_ => { });

        Assert.Equal(original, tapped);
        Assert.Equal(42, tapped.Value);
    }

    [Fact]
    public void TapError_OnFailedResult_ReturnsEquivalentResult()
    {
        Result original = Result.Failure("err");
        Result tapped = original.TapError(_ => { });

        Assert.Equal(original, tapped);
    }

    [Fact]
    public void TapError_OnFailedResultT_ReturnsEquivalentResult()
    {
        Result<int> original = Result<int>.Failure("err");
        Result<int> tapped = original.TapError(_ => { });

        Assert.Equal(original, tapped);
    }
}