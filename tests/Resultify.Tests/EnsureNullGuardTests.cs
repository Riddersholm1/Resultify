using Resultify.Errors;

namespace Resultify.Tests;

public sealed class EnsureNullGuardTests
{
    [Fact]
    public void Result_Ensure_WithNullError_ShouldThrow()
    {
        Error? nullError = null;
        Assert.Throws<ArgumentNullException>(() => Result.Success().Ensure(() => true, nullError!));
    }

    [Fact]
    public void Result_Ensure_WithNullPredicate_ShouldThrow()
    {
        Func<bool>? nullPredicate = null;
        Assert.Throws<ArgumentNullException>(() => Result.Success().Ensure(nullPredicate!, new Error("x")));
    }

    [Fact]
    public void Result_Ensure_WithNullErrorFactory_ShouldThrow()
    {
        Func<Error>? nullFactory = null;
        Assert.Throws<ArgumentNullException>(() => Result.Success().Ensure(() => true, nullFactory!));
    }

    [Fact]
    public void Result_Ensure_WithNullErrorMessage_ShouldThrow()
    {
        string? nullMsg = null;
        Assert.Throws<ArgumentNullException>(() => Result.Success().Ensure(() => true, nullMsg!));
    }

    [Fact]
    public void ResultT_Ensure_WithNullError_ShouldThrow()
    {
        Error? nullError = null;
        Assert.Throws<ArgumentNullException>(() =>
            Result<int>.Success(1).Ensure(_ => true, nullError!));
    }

    [Fact]
    public void ResultT_Ensure_WithNullPredicate_ShouldThrow()
    {
        Func<int, bool>? nullPredicate = null;
        Assert.Throws<ArgumentNullException>(() =>
            Result<int>.Success(1).Ensure(nullPredicate!, new Error("x")));
    }

    [Fact]
    public void ResultT_Ensure_WithNullErrorFactory_ShouldThrow()
    {
        Func<int, Error>? nullFactory = null;
        Assert.Throws<ArgumentNullException>(() =>
            Result<int>.Success(1).Ensure(_ => true, nullFactory!));
    }

    [Fact]
    public void ResultT_Ensure_WithNullErrorMessage_ShouldThrow()
    {
        string? nullMsg = null;
        Assert.Throws<ArgumentNullException>(() =>
            Result<int>.Success(1).Ensure(_ => true, nullMsg!));
    }
}