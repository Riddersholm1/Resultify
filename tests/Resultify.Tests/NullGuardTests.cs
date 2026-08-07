using Resultify.Errors;

namespace Resultify.Tests;

/// <summary>
/// Every public entry point guards its arguments with <c>ArgumentNullException.ThrowIfNull</c>, so a
/// null callback fails at the call site with the parameter name rather than as a
/// <see cref="NullReferenceException"/> somewhere further down a pipeline. These tests walk the
/// whole surface so a newly added overload without a guard shows up as a gap.
/// </summary>
public sealed class FactoryNullGuardTests
{
    public static TheoryData<string, Action> Cases() => new()
    {
        { "Result.Failure(Error)", () => Result.Failure((Error)null!) },
        { "Result.Failure(string)", () => Result.Failure((string)null!) },
        { "Result.Failure(null, message)", () => Result.Failure(null!, "message") },
        { "Result.Failure(code, null)", () => Result.Failure("code", null!) },
        { "Result.Failure(IEnumerable)", () => Result.Failure((IEnumerable<Error>)null!) },
        { "Result.Failure<T>(Error)", () => Result.Failure<int>((Error)null!) },
        { "Result.Failure<T>(string)", () => Result.Failure<int>((string)null!) },
        { "Result<T>.Success(null)", () => Result<string>.Success(null!) },
        { "Result<T>.Failure(Error)", () => Result<int>.Failure((Error)null!) },
        { "Result<T>.Failure(string)", () => Result<int>.Failure((string)null!) },
        { "Result<T>.Failure(null, message)", () => Result<int>.Failure(null!, "message") },
        { "Result<T>.Failure(code, null)", () => Result<int>.Failure("code", null!) },
        { "Result<T>.Failure(IEnumerable)", () => Result<int>.Failure((IEnumerable<Error>)null!) },
        { "Result.SuccessIf(false, Error)", () => Result.SuccessIf(false, (Error)null!) },
        { "Result.SuccessIf(false, string)", () => Result.SuccessIf(false, (string)null!) },
        { "Result.SuccessIf(_, factory)", () => Result.SuccessIf(true, (Func<Error>)null!) },
        { "Result.FailureIf(true, Error)", () => Result.FailureIf(true, (Error)null!) },
        { "Result.FailureIf(true, string)", () => Result.FailureIf(true, (string)null!) },
        { "Result.FailureIf(_, factory)", () => Result.FailureIf(false, (Func<Error>)null!) },
        { "Result<T>.SuccessIf(false, v, Error)", () => Result<int>.SuccessIf(false, 1, (Error)null!) },
        { "Result<T>.SuccessIf(true, null, Error)", () => Result<string>.SuccessIf(true, null!, new Error("e")) },
        { "Result<T>.SuccessIf(_, v, factory)", () => Result<int>.SuccessIf(true, 1, (Func<Error>)null!) },
        { "Result<T>.FailureIf(true, v, Error)", () => Result<int>.FailureIf(true, 1, (Error)null!) },
        { "Result<T>.FailureIf(false, null, Error)", () => Result<string>.FailureIf(false, null!, new Error("e")) },
        { "Result<T>.FailureIf(_, v, factory)", () => Result<int>.FailureIf(false, 1, (Func<Error>)null!) },
        { "Result.ToResult<T>(null)", () => Result.Success().ToResult<string>(null!) },
        { "Result.Try(Action)", () => Result.Try((Action)null!) },
        { "Result.Try(Func<Result>)", () => Result.Try((Func<Result>)null!) },
        { "Result<T>.Try(Func<T>)", () => Result<int>.Try((Func<int>)null!) },
        { "Result<T>.Try(Func<Result<T>>)", () => Result<int>.Try((Func<Result<int>>)null!) },
        { "implicit Error -> Result", () => { Result _ = (Error)null!; } },
        { "implicit Error -> Result<T>", () => { Result<int> _ = (Error)null!; } },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void NullArgument_ShouldThrowArgumentNullException(string name, Action act) =>
        Assert.Throws<ArgumentNullException>(act);
}

public sealed class CombinatorNullGuardTests
{
    private static readonly Result Success = Result.Success();
    private static readonly Result Failed = Result.Failure("boom");
    private static readonly Result<int> TypedSuccess = Result<int>.Success(1);
    private static readonly Result<int> TypedFailed = Result<int>.Failure("boom");

    public static TheoryData<string, Action> Cases() => new()
    {
        { "Result.Bind", () => Success.Bind((Func<Result>)null!) },
        { "Result.Bind<T>", () => Success.Bind((Func<Result<int>>)null!) },
        { "Result.BindAsync", () => Success.BindAsync((Func<Task<Result>>)null!) },
        { "Result.BindAsync<T>", () => Success.BindAsync((Func<Task<Result<int>>>)null!) },
        { "Result.Tap", () => Success.Tap(null!) },
        { "Result.TapError", () => Failed.TapError(null!) },
        { "Result.Ensure(predicate, Error)", () => Success.Ensure(null!, new Error("x")) },
        { "Result.Ensure(_, null Error)", () => Success.Ensure(() => true, (Error)null!) },
        { "Result.Ensure(_, null factory)", () => Success.Ensure(() => true, (Func<Error>)null!) },
        { "Result.Ensure(_, null message)", () => Success.Ensure(() => true, (string)null!) },
        { "Result.Match(null, _)", () => Success.Match<int>(null!, _ => 0) },
        { "Result.Match(_, null)", () => Success.Match(() => 0, null!) },
        { "Result.MatchAsync(null, _)", () => Success.MatchAsync<int>(null!, _ => Task.FromResult(0)) },
        { "Result.MatchAsync(_, null)", () => Success.MatchAsync(() => Task.FromResult(0), null!) },
        { "Result.Switch(null, _)", () => Success.Switch(null!, _ => { }) },
        { "Result.Switch(_, null)", () => Success.Switch(() => { }, null!) },
        { "Result.HasError(null)", () => Success.HasError<Error>(null!) },

        { "Result<T>.Map", () => TypedSuccess.Map<int>(null!) },
        { "Result<T>.Bind<TNew>", () => TypedSuccess.Bind((Func<int, Result<int>>)null!) },
        { "Result<T>.Bind(Result)", () => TypedSuccess.Bind((Func<int, Result>)null!) },
        { "Result<T>.BindAsync<TNew>", () => TypedSuccess.BindAsync((Func<int, Task<Result<int>>>)null!) },
        { "Result<T>.BindAsync(Result)", () => TypedSuccess.BindAsync((Func<int, Task<Result>>)null!) },
        { "Result<T>.Tap", () => TypedSuccess.Tap(null!) },
        { "Result<T>.TapError", () => TypedFailed.TapError(null!) },
        { "Result<T>.Ensure(predicate, Error)", () => TypedSuccess.Ensure(null!, new Error("x")) },
        { "Result<T>.Ensure(_, null Error)", () => TypedSuccess.Ensure(_ => true, (Error)null!) },
        { "Result<T>.Ensure(_, null factory)", () => TypedSuccess.Ensure(_ => true, (Func<int, Error>)null!) },
        { "Result<T>.Ensure(_, null message)", () => TypedSuccess.Ensure(_ => true, (string)null!) },
        { "Result<T>.Match(null, _)", () => TypedSuccess.Match<int>(null!, _ => 0) },
        { "Result<T>.Match(_, null)", () => TypedSuccess.Match(_ => 0, null!) },
        { "Result<T>.MatchAsync(null, _)", () => TypedSuccess.MatchAsync<int>(null!, _ => Task.FromResult(0)) },
        { "Result<T>.MatchAsync(_, null)", () => TypedSuccess.MatchAsync(_ => Task.FromResult(0), null!) },
        { "Result<T>.Switch(null, _)", () => TypedSuccess.Switch(null!, _ => { }) },
        { "Result<T>.Switch(_, null)", () => TypedSuccess.Switch(_ => { }, null!) },
        { "Result<T>.HasError(null)", () => TypedSuccess.HasError<Error>(null!) },

        { "Merge(null IEnumerable<Result>)", () => ((IEnumerable<Result>)null!).Merge() },
        { "Merge(null IEnumerable<Result<T>>)", () => ((IEnumerable<Result<int>>)null!).Merge() },
    };

    /// <summary>
    /// The <c>async</c> combinators are compiled state machines, so their guard faults the returned
    /// task instead of throwing at the call site. The exception type and parameter name are the same;
    /// only the timing differs, and it is observable the moment the task is awaited.
    /// </summary>
    public static TheoryData<string, Func<Task>> AsyncCases() => new()
    {
        { "Result.TapAsync", () => Success.TapAsync(null!) },
        { "Result.TapErrorAsync", () => Failed.TapErrorAsync(null!) },
        { "Result.SwitchAsync(null, _)", () => Success.SwitchAsync(null!, _ => Task.CompletedTask) },
        { "Result.SwitchAsync(_, null)", () => Success.SwitchAsync(() => Task.CompletedTask, null!) },
        { "Result<T>.MapAsync", () => TypedSuccess.MapAsync<int>(null!) },
        { "Result<T>.TapAsync", () => TypedSuccess.TapAsync(null!) },
        { "Result<T>.TapErrorAsync", () => TypedFailed.TapErrorAsync(null!) },
        { "Result<T>.SwitchAsync(null, _)", () => TypedSuccess.SwitchAsync(null!, _ => Task.CompletedTask) },
        { "Result<T>.SwitchAsync(_, null)", () => TypedSuccess.SwitchAsync(_ => Task.CompletedTask, null!) },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void NullArgument_ShouldThrowArgumentNullException(string name, Action act) =>
        Assert.Throws<ArgumentNullException>(act);

    [Theory]
    [MemberData(nameof(AsyncCases))]
    public async Task NullArgument_OnAsyncCombinator_ShouldFaultTheReturnedTask(string name, Func<Task> act) =>
        await Assert.ThrowsAsync<ArgumentNullException>(act);

    [Fact]
    public void Guards_ShouldFireEvenWhenTheCallbackWouldNotHaveRun()
    {
        // The receiver has already failed, so Tap would never invoke the action — the guard must
        // still reject the null rather than quietly returning the failure.
        Assert.Throws<ArgumentNullException>(() => Failed.Tap(null!));
        Assert.Throws<ArgumentNullException>(() => TypedFailed.Map<int>(null!));
        Assert.Throws<ArgumentNullException>(() => Success.TapError(null!));
    }
}

/// <summary>
/// The async pipeline helpers are extension methods, so the receiving task itself can be null.
/// Because they are <c>async</c>, the guard surfaces on the returned task rather than synchronously.
/// </summary>
public sealed class AsyncPipelineNullGuardTests
{
    public static TheoryData<string, Func<Task>> TypedCases() => new()
    {
        { "Map", () => ((Task<Result<int>>)null!).Map(v => v) },
        { "MapAsync", () => ((Task<Result<int>>)null!).MapAsync(v => Task.FromResult(v)) },
        { "Bind", () => ((Task<Result<int>>)null!).Bind(Result<int>.Success) },
        { "BindAsync", () => ((Task<Result<int>>)null!).BindAsync(v => Task.FromResult(Result<int>.Success(v))) },
        { "Bind to Result", () => ((Task<Result<int>>)null!).Bind(_ => Result.Success()) },
        { "BindAsync to Result", () => ((Task<Result<int>>)null!).BindAsync(_ => Task.FromResult(Result.Success())) },
        { "Tap", () => ((Task<Result<int>>)null!).Tap(_ => { }) },
        { "TapAsync", () => ((Task<Result<int>>)null!).TapAsync(_ => Task.CompletedTask) },
        { "TapError", () => ((Task<Result<int>>)null!).TapError(_ => { }) },
        { "TapErrorAsync", () => ((Task<Result<int>>)null!).TapErrorAsync(_ => Task.CompletedTask) },
        { "Ensure(Error)", () => ((Task<Result<int>>)null!).Ensure(_ => true, new Error("x")) },
        { "Ensure(string)", () => ((Task<Result<int>>)null!).Ensure(_ => true, "x") },
        { "Ensure(factory)", () => ((Task<Result<int>>)null!).Ensure(_ => true, _ => new Error("x")) },
        { "Match", () => ((Task<Result<int>>)null!).Match(_ => 0, _ => 0) },
        { "MatchAsync", () => ((Task<Result<int>>)null!).MatchAsync(_ => Task.FromResult(0), _ => Task.FromResult(0)) },
        { "Switch", () => ((Task<Result<int>>)null!).Switch(_ => { }, _ => { }) },
        { "SwitchAsync", () => ((Task<Result<int>>)null!).SwitchAsync(_ => Task.CompletedTask, _ => Task.CompletedTask) },
        // HasError<TError>() and HasException<TException>() need the static call form on this
        // receiver — see the remarks on ResultExtensions.
        { "HasError", () => ResultExtensions.HasError<int, Error>(null!) },
        { "HasError(predicate)", () => ((Task<Result<int>>)null!).HasError((Error _) => true) },
        { "HasErrorCode", () => ((Task<Result<int>>)null!).HasErrorCode("x") },
        { "HasException", () => ResultExtensions.HasException<int, Exception>(null!) },
    };

    public static TheoryData<string, Func<Task>> NonGenericCases() => new()
    {
        { "Match", () => ((Task<Result>)null!).Match(() => 0, _ => 0) },
        { "MatchAsync", () => ((Task<Result>)null!).MatchAsync(() => Task.FromResult(0), _ => Task.FromResult(0)) },
        { "Bind", () => ((Task<Result>)null!).Bind(Result.Success) },
        { "BindAsync", () => ((Task<Result>)null!).BindAsync(() => Task.FromResult(Result.Success())) },
        { "Bind<T>", () => ((Task<Result>)null!).Bind(() => Result<int>.Success(1)) },
        { "BindAsync<T>", () => ((Task<Result>)null!).BindAsync(() => Task.FromResult(Result<int>.Success(1))) },
        { "Tap", () => ((Task<Result>)null!).Tap(() => { }) },
        { "TapAsync", () => ((Task<Result>)null!).TapAsync(() => Task.CompletedTask) },
        { "TapError", () => ((Task<Result>)null!).TapError(_ => { }) },
        { "TapErrorAsync", () => ((Task<Result>)null!).TapErrorAsync(_ => Task.CompletedTask) },
        { "Ensure(Error)", () => ((Task<Result>)null!).Ensure(() => true, new Error("x")) },
        { "Ensure(factory)", () => ((Task<Result>)null!).Ensure(() => true, () => new Error("x")) },
        { "Switch", () => ((Task<Result>)null!).Switch(() => { }, _ => { }) },
        { "SwitchAsync", () => ((Task<Result>)null!).SwitchAsync(() => Task.CompletedTask, _ => Task.CompletedTask) },
        { "HasError", () => ((Task<Result>)null!).HasError<Error>() },
        { "HasError(predicate)", () => ((Task<Result>)null!).HasError<Error>(_ => true) },
        { "HasErrorCode", () => ((Task<Result>)null!).HasErrorCode("x") },
        { "HasException", () => ((Task<Result>)null!).HasException<Exception>() },
    };

    [Theory]
    [MemberData(nameof(TypedCases))]
    public async Task TypedPipeline_WithNullTask_ShouldThrowArgumentNullException(string name, Func<Task> act) =>
        await Assert.ThrowsAsync<ArgumentNullException>(act);

    [Theory]
    [MemberData(nameof(NonGenericCases))]
    public async Task NonGenericPipeline_WithNullTask_ShouldThrowArgumentNullException(string name, Func<Task> act) =>
        await Assert.ThrowsAsync<ArgumentNullException>(act);
}

public sealed class ErrorNullGuardTests
{
    public static TheoryData<string, Action> Cases() => new()
    {
        { "new Error(null, message)", () => new Error(null!, "message") },
        { "new Error(code, null)", () => new Error("code", null!) },
        { "new Error(null)", () => new Error(null!) },
        { "WithMetadata(null, value)", () => new Error("e").WithMetadata(null!, "value") },
        { "WithMetadata(key, null)", () => new Error("e").WithMetadata("key", null!) },
        { "WithMetadata(null enumerable)", () => new Error("e").WithMetadata(null!) },
        { "CausedBy((Error)null)", () => new Error("e").CausedBy((Error)null!) },
        { "CausedBy((IEnumerable)null)", () => new Error("e").CausedBy((IEnumerable<Error>)null!) },
        { "CausedBy((Exception)null)", () => new Error("e").CausedBy((Exception)null!) },
        { "new ValidationError(null)", () => new ValidationError(null!) },
        { "new ValidationError(null, message)", () => new ValidationError(null!, "message") },
        { "new ValidationError(property, null)", () => new ValidationError("Email", null!) },
        { "ValidationError.ForProperty(null)", () => ValidationError.ForProperty(null!) },
        { "new NotFoundError(null)", () => new NotFoundError(null!) },
        { "new NotFoundError(null, id)", () => new NotFoundError(null!, 42) },
        { "new NotFoundError(entity, null)", () => new NotFoundError("Customer", null!) },
        { "new ConflictError(null)", () => new ConflictError(null!) },
        { "new ConflictError(null, message)", () => new ConflictError(null!, "message") },
        { "new ForbiddenError(null)", () => new ForbiddenError(null!) },
        { "new ForbiddenError(null, message)", () => new ForbiddenError(null!, "message") },
        { "new ExceptionalError(null)", () => new ExceptionalError((Exception)null!) },
        { "new ExceptionalError(message, null)", () => new ExceptionalError("message", null!) },
        { "Code = null via with", () => { _ = new Error("c", "m") with { Code = null! }; } },
        { "Message = null via with", () => { _ = new Error("c", "m") with { Message = null! }; } },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void NullArgument_ShouldThrowArgumentNullException(string name, Action act) =>
        Assert.Throws<ArgumentNullException>(act);

    [Fact]
    public void ParameterNames_ShouldBeReported()
    {
        Assert.Equal("code", Assert.Throws<ArgumentNullException>(() => new Error(null!, "m")).ParamName);
        Assert.Equal("message", Assert.Throws<ArgumentNullException>(() => new Error("c", null!)).ParamName);
        Assert.Equal("propertyName", Assert.Throws<ArgumentNullException>(() => new ValidationError(null!, "m")).ParamName);
        Assert.Equal("entityName", Assert.Throws<ArgumentNullException>(() => new NotFoundError(null!, 42)).ParamName);
        Assert.Equal("entityId", Assert.Throws<ArgumentNullException>(() => new NotFoundError("Customer", null!)).ParamName);
        Assert.Equal("exception", Assert.Throws<ArgumentNullException>(() => new ExceptionalError((Exception)null!)).ParamName);
    }
}

/// <summary>
/// Failure lists have two invariants beyond non-nullness: at least one error, and no null elements.
/// </summary>
public sealed class ErrorCollectionValidationTests
{
    [Fact]
    public void Failure_WithEmptyErrorList_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() => Result.Failure([]));
        Assert.Throws<ArgumentException>(() => Result<int>.Failure([]));
    }

    [Fact]
    public void Failure_WithNullErrorElement_ShouldThrowAndNameTheParameter()
    {
        IEnumerable<Error> errors = [new("ok"), null!];

        Assert.Equal("errors", Assert.Throws<ArgumentException>(() => Result.Failure(errors)).ParamName);
        Assert.Equal("errors", Assert.Throws<ArgumentException>(() => Result<int>.Failure(errors)).ParamName);
    }

    [Fact]
    public void CausedBy_WithNullElement_ShouldThrowAndNameTheParameter()
    {
        IEnumerable<Error> causes = [new("c1"), null!, new("c3")];

        Assert.Equal("causes", Assert.Throws<ArgumentException>(() => new Error("e").CausedBy(causes)).ParamName);
    }

    [Fact]
    public void WithMetadata_WithNullKeyOrValue_ShouldThrowAndNameTheParameter()
    {
        var error = new Error("code", "msg");

        ArgumentException nullKey = Assert.Throws<ArgumentException>(
            () => error.WithMetadata([new KeyValuePair<string, object>(null!, "value")]));
        Assert.Equal("metadata", nullKey.ParamName);

        ArgumentException nullValue = Assert.Throws<ArgumentException>(
            () => error.WithMetadata([new KeyValuePair<string, object>("key", null!)]));
        Assert.Equal("metadata", nullValue.ParamName);
        Assert.Contains("key", nullValue.Message, StringComparison.Ordinal);
    }
}
