using Resultify.Errors;

namespace Resultify.Tests;

/// <summary>
/// Pin the contract that user-supplied callbacks (Map, Bind, Ensure, Tap, TapError, Match, Switch)
/// propagate exceptions unchanged rather than being silently caught and wrapped. The combinators
/// are not <c>Try</c>; only <c>Try</c>/<c>TryAsync</c> swallow exceptions into <see cref="ExceptionalError"/>.
/// </summary>
public sealed class CallbackExceptionTests
{
    private sealed class Boom() : Exception("boom");

    [Fact]
    public void Map_CallbackException_ShouldPropagate()
    {
        Assert.Throws<Boom>(() => Result<int>.Success(1).Map<int>(_ => throw new Boom()));
    }

    [Fact]
    public void Bind_TypedCallbackException_ShouldPropagate()
    {
        Assert.Throws<Boom>(() => Result<int>.Success(1).Bind<int>(_ => throw new Boom()));
    }

    [Fact]
    public void BindNonGeneric_CallbackException_ShouldPropagate()
    {
        Assert.Throws<Boom>(() => Result.Success().Bind(() => throw new Boom()));
    }

    [Fact]
    public void Ensure_PredicateException_ShouldPropagate()
    {
        Assert.Throws<Boom>(() => Result<int>.Success(1).Ensure(_ => throw new Boom(), new Error("x")));
    }

    [Fact]
    public void Tap_CallbackException_ShouldPropagate()
    {
        Assert.Throws<Boom>(() => Result.Success().Tap(() => throw new Boom()));
    }

    [Fact]
    public void TapError_CallbackException_ShouldPropagate()
    {
        Assert.Throws<Boom>(() => Result.Failure("err").TapError(_ => throw new Boom()));
    }

    [Fact]
    public void Match_OnSuccessException_ShouldPropagate()
    {
        Assert.Throws<Boom>(() => Result.Success().Match(() => throw new Boom(), _ => 0));
    }

    [Fact]
    public void Match_OnFailureException_ShouldPropagate()
    {
        Assert.Throws<Boom>(() => Result.Failure("err").Match(() => 0, _ => throw new Boom()));
    }

    [Fact]
    public void Switch_OnSuccessException_ShouldPropagate()
    {
        Assert.Throws<Boom>(() => Result.Success().Switch(() => throw new Boom(), _ => { }));
    }

    [Fact]
    public async Task MapAsync_CallbackException_ShouldPropagate()
    {
        await Assert.ThrowsAsync<Boom>(() =>
            Result<int>.Success(1).MapAsync<int>(_ => throw new Boom()));
    }
}