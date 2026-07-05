using Resultify.Errors;

namespace Resultify.Tests;

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