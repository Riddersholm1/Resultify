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