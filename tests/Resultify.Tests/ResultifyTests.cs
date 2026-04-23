using Resultify.Errors;

namespace Resultify.Tests;

public sealed class ResultTests
{
    // ── Success / Failure ────────────────────────────────────

    [Fact]
    public void Success_ShouldBeSuccessful()
    {
        Result result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Empty(result.Errors);
        Assert.Equal(Error.None, result.FirstError);
    }

    [Fact]
    public void Failure_WithMessage_ShouldContainError()
    {
        Result result = Result.Failure("Something went wrong");

        Assert.True(result.IsFailure);
        Assert.Equal("Something went wrong", result.FirstError.Message);
    }

    [Fact]
    public void Failure_WithCodeAndMessage_ShouldContainBoth()
    {
        Result result = Result.Failure("User.NotFound", "User does not exist");

        Assert.Equal("User.NotFound", result.FirstError.Code);
        Assert.Equal("User does not exist", result.FirstError.Message);
    }

    [Fact]
    public void Failure_WithEmptyErrorList_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() => Result.Failure([]));
    }

    // ── SuccessIf / FailureIf ────────────────────────────────

    [Fact]
    public void SuccessIf_WhenTrue_ShouldSucceed()
    {
        Result result = Result.SuccessIf(true, "Should not appear");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void SuccessIf_WhenFalse_ShouldFail()
    {
        Result result = Result.SuccessIf(false, "Condition failed");

        Assert.True(result.IsFailure);
        Assert.Equal("Condition failed", result.FirstError.Message);
    }

    [Fact]
    public void FailureIf_WhenTrue_ShouldFail()
    {
        Result result = Result.FailureIf(true, "Bad");

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void FailureIf_WhenFalse_ShouldSucceed()
    {
        Result result = Result.FailureIf(false, "Should not appear");

        Assert.True(result.IsSuccess);
    }

    // ── Try ──────────────────────────────────────────────────

    [Fact]
    public void Try_WhenNoException_ShouldSucceed()
    {
        Result result = Result.Try(() => { });

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Try_WhenException_ShouldFail()
    {
        Result result = Result.Try(() => throw new InvalidOperationException("boom"));

        Assert.True(result.IsFailure);
        Assert.IsType<ExceptionalError>(result.FirstError);
    }

    [Fact]
    public void Try_ExceptionCode_ShouldBeExceptionName()
    {
        Result result = Result.Try(() => throw new InvalidOperationException("boom"));

        Assert.Equal("Exception.InvalidOperationException", result.FirstError.Code);
    }

    // ── Merge ────────────────────────────────────────────────

    [Fact]
    public void Merge_AllSuccess_ShouldSucceed()
    {
        Result merged = Result.Merge(Result.Success(), Result.Success());

        Assert.True(merged.IsSuccess);
    }

    [Fact]
    public void Merge_WithFailures_ShouldCombineErrors()
    {
        Result merged = Result.Merge(
            Result.Failure("Error 1"),
            Result.Success(),
            Result.Failure("Error 2"));

        Assert.True(merged.IsFailure);
        Assert.Equal(2, merged.Errors.Count);
    }

    // ── Bind ─────────────────────────────────────────────────

    [Fact]
    public void Bind_WhenSuccess_ShouldExecuteNext()
    {
        Result result = Result.Success().Bind(Result.Success);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Bind_WhenFailure_ShouldSkipNext()
    {
        var executed = false;
        Result result = Result.Failure("err").Bind(() =>
        {
            executed = true;
            return Result.Success();
        });

        Assert.False(executed);
        Assert.True(result.IsFailure);
    }

    // ── Tap ──────────────────────────────────────────────────

    [Fact]
    public void Tap_WhenSuccess_ShouldExecuteSideEffect()
    {
        var tapped = false;
        Result.Success().Tap(() => tapped = true);

        Assert.True(tapped);
    }

    [Fact]
    public void Tap_WhenFailure_ShouldNotExecute()
    {
        var tapped = false;
        Result.Failure("err").Tap(() => tapped = true);

        Assert.False(tapped);
    }

    // ── Ensure ───────────────────────────────────────────────

    [Fact]
    public void Ensure_WhenPredicateTrue_ShouldRemainSuccess()
    {
        Result result = Result.Success().Ensure(() => true, "Should not fail");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Ensure_WhenPredicateFalse_ShouldFail()
    {
        Result result = Result.Success().Ensure(() => false, "Validation failed");

        Assert.True(result.IsFailure);
        Assert.Equal("Validation failed", result.FirstError.Message);
    }

    // ── Match ────────────────────────────────────────────────

    [Fact]
    public void Match_WhenSuccess_ShouldReturnOnSuccess()
    {
        string output = Result.Success().Match(() => "yes", _ => "no");

        Assert.Equal("yes", output);
    }

    [Fact]
    public void Match_WhenFailure_ShouldReturnOnFailure()
    {
        string output = Result.Failure("err").Match(
            () => "yes",
            errors => $"no: {errors[0].Message}");

        Assert.Equal("no: err", output);
    }

    // ── Implicit conversion ──────────────────────────────────

    [Fact]
    public void ImplicitConversion_ErrorToResult_ShouldFail()
    {
        Result result = new Error("implicit fail");

        Assert.True(result.IsFailure);
    }

    // ── Deconstruct ──────────────────────────────────────────

    [Fact]
    public void Deconstruct_ShouldWork()
    {
        (bool isSuccess, IReadOnlyList<Error> errors) = Result.Failure("err");

        Assert.False(isSuccess);
        Assert.Single(errors);
    }
}

public sealed class ResultTTests
{
    // ── Success / Failure ────────────────────────────────────

    [Fact]
    public void Success_ShouldHoldValue()
    {
        Result<int> result = Result<int>.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.Equal(42, result.ValueOrDefault);
    }

    [Fact]
    public void Failure_AccessingValue_ShouldThrow()
    {
        Result<int> result = Result<int>.Failure("err");

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Failure_ValueOrDefault_ShouldReturnDefault()
    {
        Result<int> result = Result<int>.Failure("err");

        Assert.Equal(0, result.ValueOrDefault);
    }

    // ── Create (null-safe) ───────────────────────────────────

    [Fact]
    public void Create_WithNonNullValue_ShouldSucceed()
    {
        var result = Result<string>.Create("hello");

        Assert.True(result.IsSuccess);
        Assert.Equal("hello", result.Value);
    }

    [Fact]
    public void Create_WithNull_ShouldReturnNullValueError()
    {
        var result = Result<string>.Create(null);

        Assert.True(result.IsFailure);
        Assert.Equal(Error.NullValue, result.FirstError);
    }

    [Fact]
    public void Create_ViaImplicitConversion_WithNonNull_ShouldSucceed()
    {
        Result<string> result = "hello";

        Assert.True(result.IsSuccess);
        Assert.Equal("hello", result.Value);
    }

    [Fact]
    public void Create_ViaImplicitConversion_WithNull_ShouldFail()
    {
        string? value = null;
        Result<string> result = value;

        Assert.True(result.IsFailure);
        Assert.Equal(Error.NullValue, result.FirstError);
    }

    [Fact]
    public void Result_Create_GenericHelper_ShouldWork()
    {
        var result = Result.Create<string>(null);

        Assert.True(result.IsFailure);
        Assert.Equal(Error.NullValue, result.FirstError);
    }

    // ── Try

    [Fact]
    public void Try_WhenNoException_ShouldReturnValue()
    {
        Result<int> result = Result<int>.Try(() => 42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Try_WhenException_ShouldFail()
    {
        Result<int> result = Result<int>.Try(() => throw new Exception("boom"));

        Assert.True(result.IsFailure);
    }

    // ── Map / Bind ───────────────────────────────────────────

    [Fact]
    public void Map_WhenSuccess_ShouldTransformValue()
    {
        Result<int> result = Result<int>.Success(5).Map(v => v * 2);

        Assert.Equal(10, result.Value);
    }

    [Fact]
    public void Map_WhenFailure_ShouldPropagateErrors()
    {
        Result<int> result = Result<int>.Failure("err").Map(v => v * 2);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Bind_WhenSuccess_ShouldChain()
    {
        Result<string> result = Result<int>.Success(5)
            .Bind(v => Result<string>.Success($"Value is {v}"));

        Assert.Equal("Value is 5", result.Value);
    }

    [Fact]
    public void Bind_WhenFirstFails_ShouldPropagateErrors()
    {
        Result<string> result = Result<int>.Failure("err")
            .Bind(v => Result<string>.Success($"Value is {v}"));

        Assert.True(result.IsFailure);
    }

    // ── Ensure ───────────────────────────────────────────────

    [Fact]
    public void Ensure_WhenPredicatePassesOnValue_ShouldSucceed()
    {
        Result<int> result = Result<int>.Success(10).Ensure(v => v > 5, "Must be > 5");

        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Value);
    }

    [Fact]
    public void Ensure_WhenPredicateFails_ShouldFail()
    {
        Result<int> result = Result<int>.Success(3).Ensure(v => v > 5, "Must be > 5");

        Assert.True(result.IsFailure);
    }

    // ── Match ────────────────────────────────────────────────

    [Fact]
    public void Match_WhenSuccess_ShouldUseValue()
    {
        string output = Result<int>.Success(42).Match(v => $"Got {v}", _ => "failed");

        Assert.Equal("Got 42", output);
    }

    // ── ToResult ─────────────────────────────────────────────

    [Fact]
    public void ToResult_ShouldDropValue()
    {
        Result<int> typed = Result<int>.Success(42);
        var untyped = typed.ToResult();

        Assert.True(untyped.IsSuccess);
    }

    // ── Implicit conversions ─────────────────────────────────

    [Fact]
    public void ImplicitConversion_ErrorToResult_ShouldFail()
    {
        Result<int> result = new Error("implicit fail");

        Assert.True(result.IsFailure);
    }

    // ── Deconstruct ──────────────────────────────────────────

    [Fact]
    public void Deconstruct_ShouldWork()
    {
        (bool isSuccess, int value, IReadOnlyList<Error> errors) = Result<int>.Success(42);

        Assert.True(isSuccess);
        Assert.Equal(42, value);
        Assert.Empty(errors);
    }
}

public sealed class ErrorTests
{
    [Fact]
    public void Error_WithCodeAndMessage_ShouldHaveBoth()
    {
        var error = new Error("Test.Code", "Test message");

        Assert.Equal("Test.Code", error.Code);
        Assert.Equal("Test message", error.Message);
    }

    [Fact]
    public void Error_WithMessageOnly_ShouldHaveEmptyCode()
    {
        var error = new Error("Just a message");

        Assert.Empty(error.Code);
        Assert.Equal("Just a message", error.Message);
    }

    [Fact]
    public void Error_None_ShouldBeEmpty()
    {
        Assert.Empty(Error.None.Code);
        Assert.Empty(Error.None.Message);
    }

    [Fact]
    public void Error_NullValue_ShouldHaveStableCode()
    {
        Assert.Equal("General.NullValue", Error.NullValue.Code);
    }

    [Fact]
    public void Error_WithMetadata_ShouldBeImmutable()
    {
        var original = new Error("base");
        Error withMeta = original.WithMetadata("code", "E001");

        Assert.Empty(original.Metadata);
        Assert.True(withMeta.Metadata.ContainsKey("code"));
    }

    [Fact]
    public void Error_CausedBy_ShouldChain()
    {
        var root = new Error("root cause");
        Error error = new Error("wrapper").CausedBy(root);

        Error cause = Assert.Single(error.Causes);
        Assert.Equal("root cause", cause.Message);
    }

    [Fact]
    public void Error_RecordEquality_ShouldWorkByValue()
    {
        var a = new Error("CODE", "message");
        var b = new Error("CODE", "message");

        Assert.Equal(a, b);
    }

    [Fact]
    public void ValidationError_ShouldHavePropertyName()
    {
        var error = new ValidationError("Email", "Email is required");

        Assert.Equal("Email", error.PropertyName);
        Assert.Equal("Validation.Email", error.Code);
        Assert.Equal("Email is required", error.Message);
    }

    [Fact]
    public void NotFoundError_ShouldFormatMessageAndCode()
    {
        var error = new NotFoundError("Customer", 42);

        Assert.Equal("Customer", error.EntityName);
        Assert.Equal(42, error.EntityId);
        Assert.Equal("Customer.NotFound", error.Code);
        Assert.Contains("Customer", error.Message);
        Assert.Contains("42", error.Message);
    }
}

public sealed class ResultExtensionTests
{
    [Fact]
    public void Merge_CollectionOfResults_ShouldCombine()
    {
        var results = new List<Result>
        {
            Result.Success(),
            Result.Failure("err1"),
            Result.Failure("err2")
        };

        Result merged = results.Merge();

        Assert.True(merged.IsFailure);
        Assert.Equal(2, merged.Errors.Count);
    }

    [Fact]
    public void Merge_TypedResults_ShouldCollectValues()
    {
        Result<int>[] results =
        [
            Result<int>.Success(1),
            Result<int>.Success(2),
            Result<int>.Success(3)
        ];

        int[] expected = [1, 2, 3];


        Result<IReadOnlyList<int>> merged = results.Merge();

        Assert.True(merged.IsSuccess);
        Assert.Equivalent(expected, merged.Value);
    }

    [Fact]
    public void HasError_ShouldDetectSpecificErrorType()
    {
        Result result = Result.Failure(new ValidationError("Name", "Required"));

        Assert.True(result.HasError<ValidationError>());
        Assert.False(result.HasError<NotFoundError>());
    }

    [Fact]
    public void HasErrorCode_ShouldMatchCode()
    {
        Result result = Result.Failure("User.NotFound", "not found");

        Assert.True(result.HasErrorCode("User.NotFound"));
        Assert.False(result.HasErrorCode("Other"));
    }

    [Fact]
    public void HasException_ShouldDetectWrappedException()
    {
        Result result = Result.Try(() => throw new InvalidOperationException());

        Assert.True(result.HasException<InvalidOperationException>());
        Assert.False(result.HasException<ArgumentException>());
    }
}

public sealed class AsyncPipelineTests
{
    [Fact]
    public async Task MapAsync_ShouldTransformValue()
    {
        Result<int> result = await Result<int>.Success(5)
            .MapAsync(v => Task.FromResult(v * 2));

        Assert.Equal(10, result.Value);
    }

    [Fact]
    public async Task BindAsync_ShouldChain()
    {
        Result<string> result = await Result<int>.Success(5)
            .BindAsync(v => Task.FromResult(Result<string>.Success($"Value: {v}")));

        Assert.Equal("Value: 5", result.Value);
    }

    [Fact]
    public async Task TryAsync_WhenNoException_ShouldSucceed()
    {
        Result result = await Result.TryAsync(() => Task.CompletedTask);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task TryAsync_WhenException_ShouldFail()
    {
        Result result = await Result.TryAsync(
            () => throw new InvalidOperationException("async boom"));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task TryAsync_Typed_ShouldReturnValue()
    {
        Result<int> result = await Result<int>.TryAsync(() => Task.FromResult(42));

        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task AsyncPipeline_MapThenBind_ShouldWork()
    {
        Result<string> result = await Task.FromResult(Result<int>.Success(5))
            .Map(v => v * 2)
            .Bind(v => v > 5
                ? Result<string>.Success($"Big: {v}")
                : Result<string>.Failure("Too small"));

        Assert.Equal("Big: 10", result.Value);
    }

    [Fact]
    public async Task TryAsync_WhenOperationCanceled_ShouldRethrow()
    {
        await Assert.ThrowsAsync<OperationCanceledException>(() => Result.TryAsync(() => throw new OperationCanceledException()));
    }

    [Fact]
    public async Task TryAsync_Typed_WhenOperationCanceled_ShouldRethrow()
    {
        await Assert.ThrowsAsync<OperationCanceledException>(() => Result<int>.TryAsync(() => throw new OperationCanceledException()));
    }

    [Fact]
    public async Task TryAsync_WhenTaskCanceled_ShouldRethrow()
    {
        await Assert.ThrowsAsync<TaskCanceledException>(() => Result.TryAsync(() => Task.FromCanceled(new CancellationToken(true))));
    }

    [Fact]
    public async Task AsyncTyped_TapError_ShouldExecuteOnFailure()
    {
        var tapped = false;
        await Task.FromResult(Result<int>.Failure("err"))
            .TapError(_ => tapped = true);

        Assert.True(tapped);
    }

    [Fact]
    public async Task AsyncTyped_TapError_ShouldNotExecuteOnSuccess()
    {
        var tapped = false;
        await Task.FromResult(Result<int>.Success(1))
            .TapError(_ => tapped = true);

        Assert.False(tapped);
    }

    [Fact]
    public async Task AsyncTyped_Switch_ShouldCallOnSuccess()
    {
        var called = false;
        await Task.FromResult(Result<int>.Success(42))
            .Switch(v => called = v == 42, _ => { });

        Assert.True(called);
    }

    [Fact]
    public async Task AsyncTyped_Switch_ShouldCallOnFailure()
    {
        var called = false;
        await Task.FromResult(Result<int>.Failure("err"))
            .Switch(_ => { }, _ => called = true);

        Assert.True(called);
    }

    [Fact]
    public async Task AsyncTyped_MatchAsync_ShouldWork()
    {
        string output = await Task.FromResult(Result<int>.Success(5))
            .MatchAsync(
                v => Task.FromResult($"ok:{v}"),
                _ => Task.FromResult("fail"));

        Assert.Equal("ok:5", output);
    }

    [Fact]
    public async Task AsyncTyped_BindToNonGenericResult_ShouldWork()
    {
        Result result = await Task.FromResult(Result<int>.Success(5))
            .Bind(v => v > 0 ? Result.Success() : Result.Failure("negative"));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task AsyncTyped_BindAsyncToNonGenericResult_ShouldWork()
    {
        Result result = await Task.FromResult(Result<int>.Success(5))
            .BindAsync(v => Task.FromResult(v > 0 ? Result.Success() : Result.Failure("negative")));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task AsyncNonGeneric_TapError_ShouldExecuteOnFailure()
    {
        var tapped = false;
        await Task.FromResult(Result.Failure("err"))
            .TapError(_ => tapped = true);

        Assert.True(tapped);
    }

    [Fact]
    public async Task AsyncNonGeneric_Switch_ShouldCallOnSuccess()
    {
        var called = false;
        await Task.FromResult(Result.Success())
            .Switch(() => called = true, _ => { });

        Assert.True(called);
    }
}

public sealed class DefaultResultBehaviorTests
{
    [Fact]
    public void DefaultResultT_IsSuccess_WithDefaultValue()
    {
        Result<int> result = default;

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void DefaultResultT_String_IsSuccess_ButValueThrows()
    {
        Result<string> result = default;

        Assert.True(result.IsSuccess);
        Assert.Null(result.ValueOrDefault);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void DefaultResult_IsSuccess()
    {
        Result result = default;

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Errors);
    }
}

public sealed class DefensiveCopyTests
{
    [Fact]
    public void Failure_WithListOfErrors_ShouldNotMutateWhenCallerMutatesSource()
    {
        var errors = new List<Error> { new("err1"), new("err2") };
        Result result = Result.Failure(errors);

        errors.Add(new Error("err3"));
        errors.Clear();

        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public void FailureT_WithListOfErrors_ShouldNotMutateWhenCallerMutatesSource()
    {
        var errors = new List<Error> { new("err1") };
        Result<int> result = Result<int>.Failure(errors);

        errors.Add(new Error("err2"));

        Assert.Single(result.Errors);
    }
}

public sealed class ErrorEqualityTests
{
    [Fact]
    public void Errors_WithSameMetadata_ShouldBeEqual()
    {
        Error a = new Error("C", "M").WithMetadata("key", "value");
        Error b = new Error("C", "M").WithMetadata("key", "value");

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Errors_WithDifferentMetadata_ShouldNotBeEqual()
    {
        Error a = new Error("C", "M").WithMetadata("key", "value1");
        Error b = new Error("C", "M").WithMetadata("key", "value2");

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Errors_WithSameCauses_ShouldBeEqual()
    {
        var cause = new Error("cause");
        Error a = new Error("C", "M").CausedBy(cause);
        Error b = new Error("C", "M").CausedBy(cause);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Errors_WithDifferentCauses_ShouldNotBeEqual()
    {
        Error a = new Error("C", "M").CausedBy(new Error("cause1"));
        Error b = new Error("C", "M").CausedBy(new Error("cause2"));

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Errors_WithMultipleMetadata_ShouldBeEqual()
    {
        Error a = new Error("C", "M")
            .WithMetadata("k1", "v1")
            .WithMetadata("k2", 42);
        Error b = new Error("C", "M")
            .WithMetadata("k1", "v1")
            .WithMetadata("k2", 42);

        Assert.Equal(a, b);
    }
}

public sealed class HasErrorOnResultTTests
{
    [Fact]
    public void HasError_OnTypedResult_ShouldDetectErrorType()
    {
        Result<int> result = Result<int>.Failure(new ValidationError("Name", "Required"));

        Assert.True(result.HasError<ValidationError>());
        Assert.False(result.HasError<NotFoundError>());
    }

    [Fact]
    public void HasError_WithPredicate_OnTypedResult_ShouldWork()
    {
        Result<int> result = Result<int>.Failure(new ValidationError("Email", "Invalid"));

        Assert.True(result.HasError<ValidationError>(e => e.PropertyName == "Email"));
        Assert.False(result.HasError<ValidationError>(e => e.PropertyName == "Name"));
    }

    [Fact]
    public void HasErrorCode_OnTypedResult_ShouldWork()
    {
        Result<int> result = Result<int>.Failure("User.NotFound", "gone");

        Assert.True(result.HasErrorCode("User.NotFound"));
        Assert.False(result.HasErrorCode("Other"));
    }

    [Fact]
    public void HasException_OnTypedResult_ShouldWork()
    {
        Result<int> result = Result<int>.Try(() => throw new InvalidOperationException());

        Assert.True(result.HasException<InvalidOperationException>());
        Assert.False(result.HasException<ArgumentException>());
    }
}

public sealed class EmptyMergeTests
{
    [Fact]
    public void Merge_EmptyCollection_ShouldSucceed()
    {
        Result merged = Array.Empty<Result>().Merge();

        Assert.True(merged.IsSuccess);
    }

    [Fact]
    public void Merge_EmptyTypedCollection_ShouldSucceedWithEmptyValues()
    {
        Result<IReadOnlyList<int>> merged = Array.Empty<Result<int>>().Merge();

        Assert.True(merged.IsSuccess);
        Assert.Empty(merged.Value);
    }

    [Fact]
    public void Merge_Span_NoArgs_ShouldSucceed()
    {
        Result merged = Result.Merge();

        Assert.True(merged.IsSuccess);
    }
}

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

public sealed class ErrorNullValidationTests
{
    [Fact]
    public void Error_WithNullCode_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => new Error(null!, "message"));
    }

    [Fact]
    public void Error_WithNullMessage_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => new Error("code", null!));
    }
}

public sealed class LazyEnsureAndSuccessIfTests
{
    [Fact]
    public void Result_Ensure_WithLazyErrorFactory_ShouldNotCallFactoryOnSuccess()
    {
        var called = false;
        Result result = Result.Success().Ensure(() => true, () =>
        {
            called = true;
            return new Error("should not be created");
        });

        Assert.True(result.IsSuccess);
        Assert.False(called);
    }

    [Fact]
    public void Result_Ensure_WithLazyErrorFactory_ShouldCallFactoryOnPredicateFalse()
    {
        Result result = Result.Success().Ensure(() => false, () => new Error("lazy error"));

        Assert.True(result.IsFailure);
        Assert.Equal("lazy error", result.FirstError.Message);
    }

    [Fact]
    public void ResultT_SuccessIf_WithLazyErrorFactory_ShouldSucceed()
    {
        var called = false;
        Result<int> result = Result<int>.SuccessIf(true, 42, () =>
        {
            called = true;
            return new Error("should not be created");
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.False(called);
    }

    [Fact]
    public void ResultT_SuccessIf_WithLazyErrorFactory_ShouldFail()
    {
        Result<int> result = Result<int>.SuccessIf(false, 42, () => new Error("lazy"));

        Assert.True(result.IsFailure);
        Assert.Equal("lazy", result.FirstError.Message);
    }
}

public sealed class SuccessNullGuardTests
{
    [Fact]
    public void ResultT_Success_WithNull_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => Result<string>.Success(null!));
    }
}

public sealed class SyncTryCancellationTests
{
    [Fact]
    public void Try_Action_WhenOperationCanceled_ShouldRethrow()
    {
        Assert.Throws<OperationCanceledException>(() => Result.Try(() => throw new OperationCanceledException()));
    }

    [Fact]
    public void Try_FuncResult_WhenOperationCanceled_ShouldRethrow()
    {
        Assert.Throws<OperationCanceledException>(() => Result.Try(() => throw new OperationCanceledException()));
    }

    [Fact]
    public void TryT_WhenOperationCanceled_ShouldRethrow()
    {
        Assert.Throws<OperationCanceledException>(() => Result<int>.Try(() => throw new OperationCanceledException()));
    }
}

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

public sealed class ResultTEnsureLazyFactoryTests
{
    [Fact]
    public void ResultT_Ensure_WithLazyErrorFactory_ShouldNotCallFactoryOnPass()
    {
        var called = false;
        Result<int> result = Result<int>.Success(42).Ensure(
            v => v > 0,
            v =>
            {
                called = true;
                return new Error($"v={v}");
            });

        Assert.True(result.IsSuccess);
        Assert.False(called);
    }

    [Fact]
    public void ResultT_Ensure_WithLazyErrorFactory_ShouldCallFactoryOnFail()
    {
        Result<int> result = Result<int>.Success(3).Ensure(
            v => v > 5,
            v => new Error($"v={v} too small"));

        Assert.True(result.IsFailure);
        Assert.Equal("v=3 too small", result.FirstError.Message);
    }

    [Fact]
    public void ResultT_Ensure_WithLazyErrorFactory_ShouldSkipOnAlreadyFailed()
    {
        var called = false;
        Result<int> result = Result<int>.Failure("earlier").Ensure(
            v => v > 0,
            _ =>
            {
                called = true;
                return new Error("should not run");
            });

        Assert.True(result.IsFailure);
        Assert.Equal("earlier", result.FirstError.Message);
        Assert.False(called);
    }
}

public sealed class ToStringTests
{
    [Fact]
    public void Result_Success_ToString_ShouldBeReadable()
    {
        Assert.Equal("Result: Success", Result.Success().ToString());
    }

    [Fact]
    public void Result_Failure_ToString_ShouldContainErrorText()
    {
        var text = Result.Failure("code", "bad").ToString();

        Assert.Contains("Failure", text);
        Assert.Contains("code", text);
        Assert.Contains("bad", text);
    }

    [Fact]
    public void ResultT_Success_ToString_ShouldContainValue()
    {
        var text = Result<int>.Success(42).ToString();

        Assert.Contains("Success", text);
        Assert.Contains("42", text);
        Assert.Contains("Int32", text);
    }

    [Fact]
    public void ResultT_DefaultForReferenceType_ToString_ShouldNotThrow()
    {
        Result<string> result = default;

        var text = result.ToString();

        Assert.Contains("Success", text);
        Assert.Contains("null", text);
    }

    [Fact]
    public void ResultT_Failure_ToString_ShouldContainErrors()
    {
        var text = Result<int>.Failure("boom").ToString();

        Assert.Contains("Failure", text);
        Assert.Contains("boom", text);
    }

    [Fact]
    public void Error_ToString_WithCausesAndCode_ShouldIncludeAll()
    {
        Error err = new Error("Top.Code", "top").CausedBy(new Error("root"));

        var text = err.ToString();

        Assert.Contains("Top.Code", text);
        Assert.Contains("top", text);
        Assert.Contains("root", text);
        Assert.Contains("caused by", text);
    }
}

public sealed class ErrorSubtypeEqualityTests
{
    [Fact]
    public void NotFoundError_ShouldNotEqualPlainErrorWithMatchingCodeAndMessage()
    {
        var notFound = new NotFoundError("Customer", 42);
        var plain = new Error(notFound.Code, notFound.Message);

        Assert.NotEqual(notFound, plain);
        Assert.NotEqual(plain, notFound);
    }

    [Fact]
    public void ValidationError_ShouldNotEqualPlainErrorWithMatchingCodeAndMessage()
    {
        var vErr = new ValidationError("Email", "Required");
        var plain = new Error(vErr.Code, vErr.Message);

        Assert.NotEqual(vErr, plain);
    }

    [Fact]
    public void ExceptionalError_WithDifferentExceptionInstances_ShouldNotBeEqual()
    {
        var a = new ExceptionalError(new InvalidOperationException("same"));
        var b = new ExceptionalError(new InvalidOperationException("same"));

        Assert.NotEqual<Error>(a, b);
    }

    [Fact]
    public void ExceptionalError_WithSameExceptionInstance_ShouldBeEqual()
    {
        var ex = new InvalidOperationException("same");
        var a = new ExceptionalError(ex);
        var b = new ExceptionalError(ex);

        Assert.Equal<Error>(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void NotFoundError_WithSameEntityAndId_ShouldBeEqual()
    {
        var a = new NotFoundError("Customer", 42);
        var b = new NotFoundError("Customer", 42);

        Assert.Equal<Error>(a, b);
    }
}

public sealed class MetadataAndCausesCombinationTests
{
    [Fact]
    public void Error_WithMetadataThenCausedBy_ShouldKeepBoth()
    {
        Error err = new Error("C", "M")
            .WithMetadata("key", "value")
            .CausedBy(new Error("cause"));

        Assert.Equal("value", err.Metadata["key"]);
        Assert.Single(err.Causes);
        Assert.Equal("cause", err.Causes[0].Message);
    }

    [Fact]
    public void Error_CausedByThenWithMetadata_ShouldKeepBoth()
    {
        Error err = new Error("C", "M")
            .CausedBy(new Error("cause"))
            .WithMetadata("key", "value");

        Assert.Equal("value", err.Metadata["key"]);
        Assert.Single(err.Causes);
    }

    [Fact]
    public void Error_MetadataAndCauses_ShouldBeOrderIndependentForEquality()
    {
        Error a = new Error("C", "M")
            .WithMetadata("key", "value")
            .CausedBy(new Error("cause"));

        Error b = new Error("C", "M")
            .CausedBy(new Error("cause"))
            .WithMetadata("key", "value");

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Error_MultipleWithMetadataChains_ShouldAccumulate()
    {
        Error err = new Error("C", "M")
            .WithMetadata("k1", 1)
            .WithMetadata("k2", 2)
            .WithMetadata("k3", 3);

        Assert.Equal(3, err.Metadata.Count);
        Assert.Equal(1, err.Metadata["k1"]);
        Assert.Equal(2, err.Metadata["k2"]);
        Assert.Equal(3, err.Metadata["k3"]);
    }
}

public sealed class TryNullReturnTests
{
    [Fact]
    public void Try_WhenFuncReturnsNull_ShouldReturnNullValueError()
    {
        Result<string> result = Result<string>.Try(() => null!);

        Assert.True(result.IsFailure);
        Assert.Equal(Error.NullValue, result.FirstError);
    }

    [Fact]
    public async Task TryAsync_WhenFuncReturnsNull_ShouldReturnNullValueError()
    {
        Result<string> result = await Result<string>.TryAsync(() => Task.FromResult<string>(null!));

        Assert.True(result.IsFailure);
        Assert.Equal(Error.NullValue, result.FirstError);
    }

    [Fact]
    public void Try_WhenFuncReturnsNonNull_ShouldSucceed()
    {
        Result<string> result = Result<string>.Try(() => "hello");

        Assert.True(result.IsSuccess);
        Assert.Equal("hello", result.Value);
    }

    [Fact]
    public void Try_WithValueType_ShouldSucceed()
    {
        Result<int> result = Result<int>.Try(() => 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value);
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

public sealed class WithMetadataValidationTests
{
    [Fact]
    public void WithMetadata_Enumerable_WithNullKey_ShouldThrowArgumentException()
    {
        var error = new Error("code", "msg");
        var meta = new List<KeyValuePair<string, object>>
        {
            new(null!, "value"),
        };

        var ex = Assert.Throws<ArgumentException>(() => error.WithMetadata(meta));
        Assert.Equal("metadata", ex.ParamName);
    }

    [Fact]
    public void WithMetadata_Enumerable_WithNullValue_ShouldThrowArgumentException()
    {
        var error = new Error("code", "msg");
        var meta = new List<KeyValuePair<string, object>>
        {
            new("key", null!),
        };

        var ex = Assert.Throws<ArgumentException>(() => error.WithMetadata(meta));
        Assert.Equal("metadata", ex.ParamName);
        Assert.Contains("key", ex.Message);
    }

    [Fact]
    public void WithMetadata_Enumerable_WithNullEnumerable_ShouldThrowArgumentNullException()
    {
        var error = new Error("code", "msg");

        Assert.Throws<ArgumentNullException>(() => error.WithMetadata(null!));
    }

    [Fact]
    public void WithMetadata_Enumerable_WithValidPairs_ShouldAppend()
    {
        var error = new Error("code", "msg");
        var meta = new List<KeyValuePair<string, object>>
        {
            new("k1", "v1"),
            new("k2", 42),
        };

        Error updated = error.WithMetadata(meta);

        Assert.Equal(2, updated.Metadata.Count);
        Assert.Equal("v1", updated.Metadata["k1"]);
        Assert.Equal(42, updated.Metadata["k2"]);
    }
}

public sealed class EmptyErrorsListAllocationTests
{
    [Fact]
    public void Result_SuccessErrors_ShouldReturnSameInstanceAcrossCalls()
    {
        Result r1 = Result.Success();
        Result r2 = Result.Success();

        Assert.Same(r1.Errors, r2.Errors);
    }

    [Fact]
    public void ResultT_SuccessErrors_ShouldReturnSameInstanceAcrossCalls()
    {
        Result<int> r1 = Result<int>.Success(1);
        Result<int> r2 = Result<int>.Success(2);

        Assert.Same(r1.Errors, r2.Errors);
    }

    [Fact]
    public void ResultT_DefaultErrors_ShouldReturnSameSharedEmptyList()
    {
        Result<int> defaulted = default;
        Result<int> succeeded = Result<int>.Success(1);

        Assert.Same(defaulted.Errors, succeeded.Errors);
    }
}

public sealed class MergeShortCircuitTests
{
    [Fact]
    public void Merge_Typed_WithFailureAfterSuccesses_ShouldProduceFailureOnly()
    {
        Result<int>[] results =
        [
            Result<int>.Success(1),
            Result<int>.Success(2),
            Result<int>.Failure("boom"),
            Result<int>.Success(3),
            Result<int>.Failure("boom2"),
        ];

        Result<IReadOnlyList<int>> merged = results.Merge();

        Assert.True(merged.IsFailure);
        Assert.Equal(2, merged.Errors.Count);
        Assert.Equal("boom", merged.Errors[0].Message);
        Assert.Equal("boom2", merged.Errors[1].Message);
    }

    [Fact]
    public void Merge_Typed_AllSuccess_ShouldPreserveOrder()
    {
        Result<int>[] results =
        [
            Result<int>.Success(1),
            Result<int>.Success(2),
            Result<int>.Success(3),
        ];

        Result<IReadOnlyList<int>> merged = results.Merge();

        Assert.True(merged.IsSuccess);
        Assert.Equal([1, 2, 3], merged.Value);
    }
}

public sealed class ImplicitOperatorNullGuardTests
{
    [Fact]
    public void Result_ImplicitFromNullError_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            Error? nullError = null;
            Result _ = nullError!;
        });
    }

    [Fact]
    public void Result_ImplicitFromNullList_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            List<Error>? nullList = null;
            Result _ = nullList!;
        });
    }

    [Fact]
    public void ResultT_ImplicitFromNullError_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            Error? nullError = null;
            Result<int> _ = nullError!;
        });
    }

    [Fact]
    public void ResultT_ImplicitFromNullList_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            List<Error>? nullList = null;
            Result<int> _ = nullList!;
        });
    }
}