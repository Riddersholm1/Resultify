using Resultify.Errors;

namespace Resultify.UnitTests;

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
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void Failure_WithMessage_ShouldContainError()
    {
        Result result = Result.Failure("Something went wrong");

        Assert.True(result.IsFailure);
        Assert.Equal("Something went wrong", result.Error.Message);
    }

    [Fact]
    public void Failure_WithCodeAndMessage_ShouldContainBoth()
    {
        Result result = Result.Failure("User.NotFound", "User does not exist");

        Assert.Equal("User.NotFound", result.Error.Code);
        Assert.Equal("User does not exist", result.Error.Message);
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
        Assert.Equal("Condition failed", result.Error.Message);
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
        Assert.IsType<ExceptionalError>(result.Error);
    }

    [Fact]
    public void Try_ExceptionCode_ShouldBeExceptionName()
    {
        Result result = Result.Try(() => throw new InvalidOperationException("boom"));

        Assert.Equal("Exception.InvalidOperationException", result.Error.Code);
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
        Assert.Equal("Validation failed", result.Error.Message);
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
        Assert.Equal(Error.NullValue, result.Error);
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
        Assert.Equal(Error.NullValue, result.Error);
    }

    [Fact]
    public void Result_Create_GenericHelper_ShouldWork()
    {
        var result = Result.Create<string>(null);

        Assert.True(result.IsFailure);
        Assert.Equal(Error.NullValue, result.Error);
    }

    // ── Try ──────────────────────────────────────────────────

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

        Result<IReadOnlyList<int>> merged = results.Merge();

        Assert.True(merged.IsSuccess);
        Assert.Equivalent(new[] { 1, 2, 3 }, merged.Value);
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
}
