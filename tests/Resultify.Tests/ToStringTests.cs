using Resultify.Errors;

namespace Resultify.Tests;

/// <summary>
/// <see cref="Error.ToString"/> is what ends up in production logs, so its format is a contract.
/// It is <c>sealed</c> precisely so that derived records cannot replace it with the compiler's
/// synthesised record format — which would dump <c>Metadata</c>, <c>Causes</c> and, for
/// <see cref="ExceptionalError"/>, an entire exception with its stack trace into every log line.
/// </summary>
public sealed class ErrorToStringTests
{
    public static TheoryData<Error, string> AllErrorTypes() => new()
    {
        { new Error("Plain.Code", "plain message"), "[Plain.Code] plain message" },
        { new Error("just a message"), "just a message" },
        { new ValidationError("Email", "Email is required"), "[Validation.Email] Email is required" },
        { new ValidationError("is invalid"), "[Validation.Invalid] is invalid" },
        { new NotFoundError("Customer", 42), "[Customer.NotFound] Customer with id '42' was not found." },
        { new NotFoundError("nope"), "[NotFound] nope" },
        { new ConflictError("Duplicate resource."), "[Conflict] Duplicate resource." },
        { new ForbiddenError("Access denied."), "[Forbidden] Access denied." },
        { new ExceptionalError(new InvalidOperationException("boom")), "[Exception.InvalidOperationException] boom" },
    };

    /// <summary>
    /// Asserting the exact rendering also pins the regression this type exists for: if a derived
    /// record ever supplies its own <c>ToString</c> again, the output becomes
    /// <c>"NotFoundError { Code = …, Metadata = … }"</c> and this fails.
    /// </summary>
    [Theory]
    [MemberData(nameof(AllErrorTypes))]
    public void ToString_ShouldUseTheCodeAndMessageFormat(Error error, string expected) =>
        Assert.Equal(expected, error.ToString());

    [Fact]
    public void ToString_WithCausesAndCode_ShouldIncludeAll()
    {
        Error err = new Error("Top.Code", "top").CausedBy(new Error("root"));

        var text = err.ToString();

        Assert.Contains("Top.Code", text);
        Assert.Contains("top", text);
        Assert.Contains("root", text);
        Assert.Contains("caused by", text);
    }

    [Fact]
    public void ToString_OnSubtypeWithCauses_ShouldStillIncludeTheCausalChain()
    {
        Error err = new NotFoundError("Customer", 7).CausedBy(new Error("Db.Timeout", "database timed out"));

        Assert.Equal(
            "[Customer.NotFound] Customer with id '7' was not found. (caused by: [Db.Timeout] database timed out)",
            err.ToString());
    }

    [Fact]
    public void ToString_OnExceptionalError_ShouldNotIncludeTheStackTrace()
    {
        Exception thrown;
        try
        {
            throw new InvalidOperationException("boom");
        }
        catch (InvalidOperationException ex)
        {
            thrown = ex;
        }

        var text = new ExceptionalError(thrown).ToString();

        Assert.Equal("[Exception.InvalidOperationException] boom", text);
        Assert.DoesNotContain("at ", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ToString_OnCustomErrorSubtype_ShouldInheritTheFormat()
    {
        var error = new InsufficientFundsError(100m, 25m);

        Assert.StartsWith("[Payment.InsufficientFunds] ", error.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("Metadata", error.ToString(), StringComparison.Ordinal);

        // The subtype's own payload stays readable; only the rendering is inherited.
        Assert.Equal(100m, error.Required);
        Assert.Equal(25m, error.Available);
        Assert.Contains("100", error.Message, StringComparison.Ordinal);
        Assert.Contains("25", error.Message, StringComparison.Ordinal);
    }

    /// <summary>Mirrors the "Custom error types" example in the README.</summary>
    private sealed record InsufficientFundsError : Error
    {
        public InsufficientFundsError(decimal required, decimal available)
            : base("Payment.InsufficientFunds", $"Insufficient funds: required {required}, available {available}")
        {
            Required = required;
            Available = available;
        }

        public decimal Required { get; }

        public decimal Available { get; }
    }
}

public sealed class ResultToStringTests
{
    [Fact]
    public void Result_Success_ToString_ShouldBeReadable() =>
        Assert.Equal("Result: Success", Result.Success().ToString());

    [Fact]
    public void Result_Failure_ToString_ShouldContainErrorText()
    {
        var text = Result.Failure("code", "bad").ToString();

        Assert.Contains("Failure", text);
        Assert.Contains("code", text);
        Assert.Contains("bad", text);
    }

    [Fact]
    public void Result_Failure_WithMultipleErrors_ToString_ShouldListAll()
    {
        var text = Result.Failure([new Error("A", "first"), new Error("B", "second")]).ToString();

        Assert.Equal("Result: Failure ([A] first; [B] second)", text);
    }

    [Fact]
    public void Result_FailureWithSubtype_ToString_ShouldUseTheErrorFormat() =>
        Assert.Equal(
            "Result: Failure ([Customer.NotFound] Customer with id '42' was not found.)",
            Result.Failure(new NotFoundError("Customer", 42)).ToString());

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
}
