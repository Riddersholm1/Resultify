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
        { "Result.Failure(Error)", () => Result.Failure(Null.Of<Error>()) },
        { "Result.Failure(string)", () => Result.Failure(Null.Of<string>()) },
        { "Result.Failure(null, message)", () => Result.Failure(null!, "message") },
        { "Result.Failure(code, null)", () => Result.Failure("code", null!) },
        { "Result.Failure(IEnumerable)", () => Result.Failure(Null.Of<IEnumerable<Error>>()) },
        { "Result.Failure<T>(Error)", () => Result.Failure<int>(Null.Of<Error>()) },
        { "Result.Failure<T>(string)", () => Result.Failure<int>(Null.Of<string>()) },
        { "Result<T>.Success(null)", () => Result<string>.Success(null!) },
        { "Result<T>.Failure(Error)", () => Result<int>.Failure(Null.Of<Error>()) },
        { "Result<T>.Failure(string)", () => Result<int>.Failure(Null.Of<string>()) },
        { "Result<T>.Failure(null, message)", () => Result<int>.Failure(null!, "message") },
        { "Result<T>.Failure(code, null)", () => Result<int>.Failure("code", null!) },
        { "Result<T>.Failure(IEnumerable)", () => Result<int>.Failure(Null.Of<IEnumerable<Error>>()) },
        { "Result.SuccessIf(false, Error)", () => Result.SuccessIf(false, Null.Of<Error>()) },
        { "Result.SuccessIf(false, string)", () => Result.SuccessIf(false, Null.Of<string>()) },
        { "Result.SuccessIf(_, factory)", () => Result.SuccessIf(true, Null.Of<Func<Error>>()) },
        { "Result.FailureIf(true, Error)", () => Result.FailureIf(true, Null.Of<Error>()) },
        { "Result.FailureIf(true, string)", () => Result.FailureIf(true, Null.Of<string>()) },
        { "Result.FailureIf(_, factory)", () => Result.FailureIf(false, Null.Of<Func<Error>>()) },
        { "Result<T>.SuccessIf(false, v, Error)", () => Result<int>.SuccessIf(false, 1, Null.Of<Error>()) },
        { "Result<T>.SuccessIf(true, null, Error)", () => Result<string>.SuccessIf(true, null!, new Error("e")) },
        { "Result<T>.SuccessIf(_, v, factory)", () => Result<int>.SuccessIf(true, 1, Null.Of<Func<Error>>()) },
        { "Result<T>.FailureIf(true, v, Error)", () => Result<int>.FailureIf(true, 1, Null.Of<Error>()) },
        { "Result<T>.FailureIf(false, null, Error)", () => Result<string>.FailureIf(false, null!, new Error("e")) },
        { "Result<T>.FailureIf(_, v, factory)", () => Result<int>.FailureIf(false, 1, Null.Of<Func<Error>>()) },
        { "Result.ToResult<T>(null)", () => Result.Success().ToResult<string>(null!) },
        { "Result.Try(Action)", () => Result.Try(Null.Of<Action>()) },
        { "Result.Try(Func<Result>)", () => Result.Try(Null.Of<Func<Result>>()) },
        { "Result<T>.Try(Func<T>)", () => Result<int>.Try(Null.Of<Func<int>>()) },
        { "Result<T>.Try(Func<Result<T>>)", () => Result<int>.Try(Null.Of<Func<Result<int>>>()) },
        { "implicit Error -> Result", () => { Result _ = Null.Of<Error>(); } },
        { "implicit Error -> Result<T>", () => { Result<int> _ = Null.Of<Error>(); } },
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
        { "Result.Bind", () => Success.Bind(Null.Of<Func<Result>>()) },
        { "Result.Bind<T>", () => Success.Bind(Null.Of<Func<Result<int>>>()) },
        { "Result.BindAsync", () => Success.BindAsync(Null.Of<Func<Task<Result>>>()) },
        { "Result.BindAsync<T>", () => Success.BindAsync(Null.Of<Func<Task<Result<int>>>>()) },
        { "Result.Tap", () => Success.Tap(null!) },
        { "Result.TapError", () => Failed.TapError(null!) },
        { "Result.Ensure(predicate, Error)", () => Success.Ensure(null!, new Error("x")) },
        { "Result.Ensure(_, null Error)", () => Success.Ensure(() => true, Null.Of<Error>()) },
        { "Result.Ensure(_, null factory)", () => Success.Ensure(() => true, Null.Of<Func<Error>>()) },
        { "Result.Ensure(_, null message)", () => Success.Ensure(() => true, Null.Of<string>()) },
        { "Result.Match(null, _)", () => Success.Match(Null.Of<Func<int>>(), _ => 0) },
        { "Result.Match(_, null)", () => Success.Match(() => 0, null!) },
        { "Result.MatchAsync(null, _)", () => Success.MatchAsync(Null.Of<Func<Task<int>>>(), _ => Task.FromResult(0)) },
        { "Result.MatchAsync(_, null)", () => Success.MatchAsync(() => Task.FromResult(0), null!) },
        { "Result.Switch(null, _)", () => Success.Switch(null!, _ => { }) },
        { "Result.Switch(_, null)", () => Success.Switch(() => { }, null!) },
        { "Result.HasError(null)", () => Success.HasError<Error>(null!) },

        { "Result<T>.Map", () => TypedSuccess.Map<int>(null!) },
        { "Result<T>.Bind<TNew>", () => TypedSuccess.Bind(Null.Of<Func<int, Result<int>>>()) },
        { "Result<T>.Bind(Result)", () => TypedSuccess.Bind(Null.Of<Func<int, Result>>()) },
        { "Result<T>.BindAsync<TNew>", () => TypedSuccess.BindAsync(Null.Of<Func<int, Task<Result<int>>>>()) },
        { "Result<T>.BindAsync(Result)", () => TypedSuccess.BindAsync(Null.Of<Func<int, Task<Result>>>()) },
        { "Result<T>.Tap", () => TypedSuccess.Tap(null!) },
        { "Result<T>.TapError", () => TypedFailed.TapError(null!) },
        { "Result<T>.Ensure(predicate, Error)", () => TypedSuccess.Ensure(null!, new Error("x")) },
        { "Result<T>.Ensure(_, null Error)", () => TypedSuccess.Ensure(_ => true, Null.Of<Error>()) },
        { "Result<T>.Ensure(_, null factory)", () => TypedSuccess.Ensure(_ => true, Null.Of<Func<int, Error>>()) },
        { "Result<T>.Ensure(_, null message)", () => TypedSuccess.Ensure(_ => true, Null.Of<string>()) },
        { "Result<T>.Match(null, _)", () => TypedSuccess.Match(Null.Of<Func<int, int>>(), _ => 0) },
        { "Result<T>.Match(_, null)", () => TypedSuccess.Match(_ => 0, null!) },
        { "Result<T>.MatchAsync(null, _)", () => TypedSuccess.MatchAsync(Null.Of<Func<int, Task<int>>>(), _ => Task.FromResult(0)) },
        { "Result<T>.MatchAsync(_, null)", () => TypedSuccess.MatchAsync(_ => Task.FromResult(0), null!) },
        { "Result<T>.Switch(null, _)", () => TypedSuccess.Switch(null!, _ => { }) },
        { "Result<T>.Switch(_, null)", () => TypedSuccess.Switch(_ => { }, null!) },
        { "Result<T>.HasError(null)", () => TypedSuccess.HasError<Error>(null!) },

        { "Merge(null IEnumerable<Result>)", () => Null.Of<IEnumerable<Result>>().Merge() },
        { "Merge(null IEnumerable<Result<T>>)", () => Null.Of<IEnumerable<Result<int>>>().Merge() },
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
        { "Map", () => Null.Of<Task<Result<int>>>().Map(v => v) },
        { "MapAsync", () => Null.Of<Task<Result<int>>>().MapAsync(v => Task.FromResult(v)) },
        { "Bind", () => Null.Of<Task<Result<int>>>().Bind(Result<int>.Success) },
        { "BindAsync", () => Null.Of<Task<Result<int>>>().BindAsync(v => Task.FromResult(Result<int>.Success(v))) },
        { "Bind to Result", () => Null.Of<Task<Result<int>>>().Bind(_ => Result.Success()) },
        { "BindAsync to Result", () => Null.Of<Task<Result<int>>>().BindAsync(_ => Task.FromResult(Result.Success())) },
        { "Tap", () => Null.Of<Task<Result<int>>>().Tap(_ => { }) },
        { "TapAsync", () => Null.Of<Task<Result<int>>>().TapAsync(_ => Task.CompletedTask) },
        { "TapError", () => Null.Of<Task<Result<int>>>().TapError(_ => { }) },
        { "TapErrorAsync", () => Null.Of<Task<Result<int>>>().TapErrorAsync(_ => Task.CompletedTask) },
        { "Ensure(Error)", () => Null.Of<Task<Result<int>>>().Ensure(_ => true, new Error("x")) },
        { "Ensure(string)", () => Null.Of<Task<Result<int>>>().Ensure(_ => true, "x") },
        { "Ensure(factory)", () => Null.Of<Task<Result<int>>>().Ensure(_ => true, _ => new Error("x")) },
        { "Match", () => Null.Of<Task<Result<int>>>().Match(_ => 0, _ => 0) },
        { "MatchAsync", () => Null.Of<Task<Result<int>>>().MatchAsync(_ => Task.FromResult(0), _ => Task.FromResult(0)) },
        { "Switch", () => Null.Of<Task<Result<int>>>().Switch(_ => { }, _ => { }) },
        { "SwitchAsync", () => Null.Of<Task<Result<int>>>().SwitchAsync(_ => Task.CompletedTask, _ => Task.CompletedTask) },
        // HasError<TError>() and HasException<TException>() need the static call form on this
        // receiver — see the remarks on ResultExtensions.
        { "HasError", () => ResultExtensions.HasError<int, Error>(null!) },
        { "HasError(predicate)", () => Null.Of<Task<Result<int>>>().HasError((Error _) => true) },
        { "HasErrorCode", () => Null.Of<Task<Result<int>>>().HasErrorCode("x") },
        { "HasException", () => ResultExtensions.HasException<int, Exception>(null!) },
    };

    public static TheoryData<string, Func<Task>> NonGenericCases() => new()
    {
        { "Match", () => Null.Of<Task<Result>>().Match(() => 0, _ => 0) },
        { "MatchAsync", () => Null.Of<Task<Result>>().MatchAsync(() => Task.FromResult(0), _ => Task.FromResult(0)) },
        { "Bind", () => Null.Of<Task<Result>>().Bind(Result.Success) },
        { "BindAsync", () => Null.Of<Task<Result>>().BindAsync(() => Task.FromResult(Result.Success())) },
        { "Bind<T>", () => Null.Of<Task<Result>>().Bind(() => Result<int>.Success(1)) },
        { "BindAsync<T>", () => Null.Of<Task<Result>>().BindAsync(() => Task.FromResult(Result<int>.Success(1))) },
        { "Tap", () => Null.Of<Task<Result>>().Tap(() => { }) },
        { "TapAsync", () => Null.Of<Task<Result>>().TapAsync(() => Task.CompletedTask) },
        { "TapError", () => Null.Of<Task<Result>>().TapError(_ => { }) },
        { "TapErrorAsync", () => Null.Of<Task<Result>>().TapErrorAsync(_ => Task.CompletedTask) },
        { "Ensure(Error)", () => Null.Of<Task<Result>>().Ensure(() => true, new Error("x")) },
        { "Ensure(factory)", () => Null.Of<Task<Result>>().Ensure(() => true, () => _ = new Error("x")) },
        { "Switch", () => Null.Of<Task<Result>>().Switch(() => { }, _ => { }) },
        { "SwitchAsync", () => Null.Of<Task<Result>>().SwitchAsync(() => Task.CompletedTask, _ => Task.CompletedTask) },
        { "HasError", () => Null.Of<Task<Result>>().HasError<Error>() },
        { "HasError(predicate)", () => Null.Of<Task<Result>>().HasError<Error>(_ => true) },
        { "HasErrorCode", () => Null.Of<Task<Result>>().HasErrorCode("x") },
        { "HasException", () => Null.Of<Task<Result>>().HasException<Exception>() },
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
        { "new Error(null, message)", () => _ = new Error(null!, "message") },
        { "new Error(code, null)", () => _ = new Error("code", null!) },
        { "new Error(null)", () => _ = new Error(null!) },
        { "WithMetadata(null, value)", () => _ = new Error("e").WithMetadata(null!, "value") },
        { "WithMetadata(key, null)", () => _ = new Error("e").WithMetadata("key", null!) },
        { "WithMetadata(null enumerable)", () => _ = new Error("e").WithMetadata(null!) },
        { "CausedBy((Error)null)", () => _ = new Error("e").CausedBy(Null.Of<Error>()) },
        { "CausedBy((IEnumerable)null)", () => _ = new Error("e").CausedBy(Null.Of<IEnumerable<Error>>()) },
        { "CausedBy((Exception)null)", () => _ = new Error("e").CausedBy(Null.Of<Exception>()) },
        { "new ValidationError(null)", () => _ = new ValidationError(null!) },
        { "new ValidationError(null, message)", () => _ = new ValidationError(null!, "message") },
        { "new ValidationError(property, null)", () => _ = new ValidationError("Email", null!) },
        { "ValidationError.ForProperty(null)", () => ValidationError.ForProperty(null!) },
        { "new NotFoundError(null)", () => _ = new NotFoundError(null!) },
        { "new NotFoundError(null, id)", () => _ = new NotFoundError(null!, 42) },
        { "new NotFoundError(entity, null)", () => _ = new NotFoundError("Customer", null!) },
        { "new ConflictError(null)", () => _ = new ConflictError(null!) },
        { "new ConflictError(null, message)", () => _ = new ConflictError(null!, "message") },
        { "new ForbiddenError(null)", () => _ = new ForbiddenError(null!) },
        { "new ForbiddenError(null, message)", () => _ = new ForbiddenError(null!, "message") },
        { "new ExceptionalError(null)", () => _ = new ExceptionalError(Null.Of<Exception>()) },
        { "new ExceptionalError(message, null)", () => _ = new ExceptionalError("message", null!) },
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
        Assert.Equal("code", Assert.Throws<ArgumentNullException>(() => _ = new Error(null!, "m")).ParamName);
        Assert.Equal("message", Assert.Throws<ArgumentNullException>(() => _ = new Error("c", null!)).ParamName);
        Assert.Equal("propertyName", Assert.Throws<ArgumentNullException>(() => _ = new ValidationError(null!, "m")).ParamName);
        Assert.Equal("entityName", Assert.Throws<ArgumentNullException>(() => _ = new NotFoundError(null!, 42)).ParamName);
        Assert.Equal("entityId", Assert.Throws<ArgumentNullException>(() => _ = new NotFoundError("Customer", null!)).ParamName);
        Assert.Equal("exception", Assert.Throws<ArgumentNullException>(() => _ = new ExceptionalError(Null.Of<Exception>())).ParamName);
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

        Assert.Equal("causes", Assert.Throws<ArgumentException>(() => _ = new Error("e").CausedBy(causes)).ParamName);
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
