using Resultify.Errors;

namespace Resultify.Tests;

/// <summary>
/// The <c>Task</c>-based helpers are what make a pipeline read as one chain. Each awaits the
/// antecedent and delegates to the instance method of the same name, so these tests check the
/// wiring: the right callback runs, the value and errors flow through, and failures short-circuit.
/// </summary>
public sealed class TypedAsyncPipelineTests
{
    private static Task<Result<int>> Success(int value = 5) => Task.FromResult(Result<int>.Success(value));

    private static Task<Result<int>> Failure(string message = "err") => Task.FromResult(Result<int>.Failure(message));

    [Fact]
    public async Task Map_ShouldTransformTheValue() =>
        Assert.Equal(10, (await Success().Map(v => v * 2)).Value);

    [Fact]
    public async Task Map_OnFailure_ShouldPropagate() =>
        Assert.True((await Failure().Map(v => v * 2)).IsFailure);

    [Fact]
    public async Task MapAsync_ShouldTransformTheValue() =>
        Assert.Equal(10, (await Success().MapAsync(v => Task.FromResult(v * 2))).Value);

    [Fact]
    public async Task MapAsync_OnFailure_ShouldPropagate() =>
        Assert.True((await Failure().MapAsync(v => Task.FromResult(v * 2))).IsFailure);

    [Fact]
    public async Task Bind_ShouldChain() =>
        Assert.Equal("v=5", (await Success().Bind(v => Result<string>.Success($"v={v}"))).Value);

    [Fact]
    public async Task BindAsync_ShouldChain() =>
        Assert.Equal("v=5", (await Success().BindAsync(v => Task.FromResult(Result<string>.Success($"v={v}")))).Value);

    [Fact]
    public async Task BindAsync_OnFailure_ShouldNotInvokeTheContinuation()
    {
        var invoked = false;

        Result<string> result = await Failure().BindAsync(v =>
        {
            invoked = true;
            return Task.FromResult(Result<string>.Success("nope"));
        });

        Assert.False(invoked);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Bind_ToNonGenericResult_ShouldChain() =>
        Assert.True((await Success().Bind(v => v > 0 ? Result.Success() : Result.Failure("negative"))).IsSuccess);

    [Fact]
    public async Task BindAsync_ToNonGenericResult_ShouldChain() =>
        Assert.True((await Success().BindAsync(v => Task.FromResult(v > 0 ? Result.Success() : Result.Failure("negative")))).IsSuccess);

    [Fact]
    public async Task Tap_ShouldRunOnSuccessAndReturnTheResult()
    {
        var seen = 0;

        Result<int> result = await Success(42).Tap(v => seen = v);

        Assert.Equal(42, seen);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task Tap_OnFailure_ShouldNotRun()
    {
        var ran = false;

        await Failure().Tap(_ => ran = true);

        Assert.False(ran);
    }

    [Fact]
    public async Task TapAsync_ShouldRunOnSuccess()
    {
        var seen = 0;

        Result<int> result = await Success(42).TapAsync(v => { seen = v; return Task.CompletedTask; });

        Assert.Equal(42, seen);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task TapAsync_OnFailure_ShouldNotRun()
    {
        var ran = false;

        await Failure().TapAsync(_ => { ran = true; return Task.CompletedTask; });

        Assert.False(ran);
    }

    [Fact]
    public async Task TapError_ShouldRunOnFailure()
    {
        var ran = false;

        await Failure().TapError(_ => ran = true);

        Assert.True(ran);
    }

    [Fact]
    public async Task TapError_OnSuccess_ShouldNotRun()
    {
        var ran = false;

        await Success().TapError(_ => ran = true);

        Assert.False(ran);
    }

    [Fact]
    public async Task TapErrorAsync_ShouldRunOnFailure()
    {
        var ran = false;

        Result<int> result = await Failure().TapErrorAsync(_ => { ran = true; return Task.CompletedTask; });

        Assert.True(ran);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task TapErrorAsync_OnSuccess_ShouldNotRun()
    {
        var ran = false;

        await Success().TapErrorAsync(_ => { ran = true; return Task.CompletedTask; });

        Assert.False(ran);
    }

    [Fact]
    public async Task Ensure_WithError_ShouldGate()
    {
        Assert.True((await Success().Ensure(v => v > 0, new Error("gated"))).IsSuccess);

        Result<int> failed = await Success().Ensure(v => v < 0, new Error("gated"));

        Assert.True(failed.IsFailure);
        Assert.Equal("gated", failed.FirstError.Message);
    }

    [Fact]
    public async Task Ensure_WithMessage_ShouldGate()
    {
        Assert.True((await Success().Ensure(v => v > 0, "gated")).IsSuccess);

        Result<int> failed = await Success().Ensure(v => v < 0, "gated");

        Assert.True(failed.IsFailure);
        Assert.Equal("gated", failed.FirstError.Message);
    }

    [Fact]
    public async Task Ensure_WithLazyFactory_ShouldGateAndSeeTheValue()
    {
        var factoryCalls = 0;

        Assert.True((await Success().Ensure(v => v > 0, v => { factoryCalls++; return new Error($"v={v}"); })).IsSuccess);
        Assert.Equal(0, factoryCalls);

        Result<int> failed = await Success(3).Ensure(v => v > 5, v => new Error($"v={v} too small"));

        Assert.Equal("v=3 too small", failed.FirstError.Message);
    }

    [Fact]
    public async Task Match_ShouldSelectTheBranch()
    {
        Assert.Equal("ok:5", await Success().Match(v => $"ok:{v}", _ => "failed"));
        Assert.Equal("failed", await Failure().Match(v => $"ok:{v}", _ => "failed"));
    }

    [Fact]
    public async Task MatchAsync_ShouldSelectTheBranch()
    {
        Assert.Equal("ok:5", await Success().MatchAsync(v => Task.FromResult($"ok:{v}"), _ => Task.FromResult("failed")));
        Assert.Equal("failed", await Failure().MatchAsync(v => Task.FromResult($"ok:{v}"), _ => Task.FromResult("failed")));
    }

    [Fact]
    public async Task Switch_ShouldSelectTheBranch()
    {
        var onSuccess = 0;
        var onFailure = 0;

        await Success(42).Switch(v => onSuccess = v, _ => onFailure++);
        await Failure().Switch(_ => onSuccess = -1, _ => onFailure++);

        Assert.Equal(42, onSuccess);
        Assert.Equal(1, onFailure);
    }

    [Fact]
    public async Task SwitchAsync_ShouldSelectTheBranch()
    {
        var onSuccess = 0;
        var onFailure = 0;

        await Success(42).SwitchAsync(v => { onSuccess = v; return Task.CompletedTask; }, _ => Task.CompletedTask);
        await Failure().SwitchAsync(_ => Task.CompletedTask, _ => { onFailure++; return Task.CompletedTask; });

        Assert.Equal(42, onSuccess);
        Assert.Equal(1, onFailure);
    }

    [Fact]
    public async Task ChainedPipeline_ShouldFlowEndToEnd()
    {
        Result<string> result = await Success()
            .Map(v => v * 2)
            .Ensure(v => v > 5, "too small")
            .BindAsync(v => Task.FromResult(Result<string>.Success($"Big: {v}")))
            .Tap(_ => { });

        Assert.Equal("Big: 10", result.Value);
    }

    [Fact]
    public async Task ChainedPipeline_ShouldShortCircuitAtTheFirstFailure()
    {
        var stepsRun = 0;

        Result<string> result = await Success()
            .Ensure(v => v < 0, "gate failed")
            .Map(v => { stepsRun++; return v * 2; })
            .BindAsync(v => { stepsRun++; return Task.FromResult(Result<string>.Success($"{v}")); });

        Assert.True(result.IsFailure);
        Assert.Equal("gate failed", result.FirstError.Message);
        Assert.Equal(0, stepsRun);
    }
}

public sealed class NonGenericAsyncPipelineTests
{
    private static Task<Result> Success() => Task.FromResult(Result.Success());

    private static Task<Result> Failure(string message = "err") => Task.FromResult(Result.Failure(message));

    [Fact]
    public async Task Match_ShouldSelectTheBranch()
    {
        Assert.Equal("ok", await Success().Match(() => "ok", _ => "failed"));
        Assert.Equal("failed", await Failure().Match(() => "ok", _ => "failed"));
    }

    [Fact]
    public async Task MatchAsync_ShouldSelectTheBranch()
    {
        Assert.Equal("ok", await Success().MatchAsync(() => Task.FromResult("ok"), _ => Task.FromResult("failed")));
        Assert.Equal("failed", await Failure().MatchAsync(() => Task.FromResult("ok"), _ => Task.FromResult("failed")));
    }

    [Fact]
    public async Task Bind_ShouldChain()
    {
        Assert.True((await Success().Bind(Result.Success)).IsSuccess);
        Assert.True((await Failure().Bind(Result.Success)).IsFailure);
    }

    [Fact]
    public async Task BindAsync_ShouldChain()
    {
        Assert.True((await Success().BindAsync(() => Task.FromResult(Result.Success()))).IsSuccess);
        Assert.True((await Failure().BindAsync(() => Task.FromResult(Result.Success()))).IsFailure);
    }

    [Fact]
    public async Task Bind_ToTypedResult_ShouldChain()
    {
        Result<int> result = await Success().Bind(() => Result<int>.Success(42));

        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task Bind_ToTypedResult_OnFailure_ShouldPropagateWithoutInvoking()
    {
        var invoked = false;

        Result<int> result = await Failure("upstream").Bind(() =>
        {
            invoked = true;
            return Result<int>.Success(42);
        });

        Assert.False(invoked);
        Assert.Equal("upstream", result.FirstError.Message);
    }

    [Fact]
    public async Task BindAsync_ToTypedResult_ShouldChain()
    {
        Result<string> result = await Success().BindAsync(() => Task.FromResult(Result<string>.Success("yo")));

        Assert.Equal("yo", result.Value);
    }

    [Fact]
    public async Task BindAsync_ToTypedResult_OnFailure_ShouldPropagateWithoutInvoking()
    {
        var invoked = false;

        Result<string> result = await Failure("upstream").BindAsync(() =>
        {
            invoked = true;
            return Task.FromResult(Result<string>.Success("yo"));
        });

        Assert.False(invoked);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task BindAsync_ToTypedResult_ShouldPropagateAnInnerFailure()
    {
        Result<int> result = await Success().BindAsync(() => Task.FromResult(Result<int>.Failure(new Error("inner"))));

        Assert.Equal("inner", result.FirstError.Message);
    }

    [Fact]
    public async Task Tap_ShouldRunOnSuccessOnly()
    {
        var runs = 0;

        Result result = await Success().Tap(() => runs++);
        await Failure().Tap(() => runs++);

        Assert.Equal(1, runs);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task TapAsync_ShouldRunOnSuccessOnly()
    {
        var runs = 0;

        Result result = await Success().TapAsync(() => { runs++; return Task.CompletedTask; });
        await Failure().TapAsync(() => { runs++; return Task.CompletedTask; });

        Assert.Equal(1, runs);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task TapError_ShouldRunOnFailureOnly()
    {
        var runs = 0;

        await Failure().TapError(_ => runs++);
        await Success().TapError(_ => runs++);

        Assert.Equal(1, runs);
    }

    [Fact]
    public async Task TapErrorAsync_ShouldRunOnFailureOnly()
    {
        var runs = 0;

        Result result = await Failure().TapErrorAsync(_ => { runs++; return Task.CompletedTask; });
        await Success().TapErrorAsync(_ => { runs++; return Task.CompletedTask; });

        Assert.Equal(1, runs);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Switch_ShouldSelectTheBranch()
    {
        var onSuccess = 0;
        var onFailure = 0;

        await Success().Switch(() => onSuccess++, _ => onFailure++);
        await Failure().Switch(() => onSuccess++, _ => onFailure++);

        Assert.Equal(1, onSuccess);
        Assert.Equal(1, onFailure);
    }

    [Fact]
    public async Task SwitchAsync_ShouldSelectTheBranch()
    {
        var onSuccess = 0;
        var onFailure = 0;

        await Success().SwitchAsync(() => { onSuccess++; return Task.CompletedTask; }, _ => Task.CompletedTask);
        await Failure().SwitchAsync(() => Task.CompletedTask, _ => { onFailure++; return Task.CompletedTask; });

        Assert.Equal(1, onSuccess);
        Assert.Equal(1, onFailure);
    }

    [Fact]
    public async Task Ensure_ShouldGate()
    {
        Assert.True((await Success().Ensure(() => true, new Error("gated"))).IsSuccess);
        Assert.Equal("gated", (await Success().Ensure(() => false, new Error("gated"))).FirstError.Message);
    }

    [Fact]
    public async Task Ensure_WithLazyFactory_ShouldGate()
    {
        var factoryCalls = 0;

        Assert.True((await Success().Ensure(() => true, () => { factoryCalls++; return new Error("never"); })).IsSuccess);
        Assert.Equal(0, factoryCalls);
        Assert.Equal("lazy fail", (await Success().Ensure(() => false, () => new Error("lazy fail"))).FirstError.Message);
    }

    [Fact]
    public async Task ChainedPipeline_ShouldFlowFromNonGenericIntoTyped()
    {
        Result<int> result = await Success()
            .Bind(() => Result<int>.Success(5))
            .Map(v => v * 2);

        Assert.Equal(10, result.Value);
    }
}
