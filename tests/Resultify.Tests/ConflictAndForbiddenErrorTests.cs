using Resultify.Errors;

namespace Resultify.Tests;

public sealed class ConflictErrorTests
{
    [Fact]
    public void Ctor_Message_SetsDefaultCode()
    {
        var error = new ConflictError("Duplicate resource.");

        Assert.Equal("Conflict", error.Code);
    }

    [Fact]
    public void Ctor_Message_SetsMessage()
    {
        var error = new ConflictError("Duplicate resource.");

        Assert.Equal("Duplicate resource.", error.Message);
    }

    [Fact]
    public void Ctor_CodeAndMessage_SetsExplicitCode()
    {
        var error = new ConflictError("Order.DuplicateId", "Order already exists.");

        Assert.Equal("Order.DuplicateId", error.Code);
        Assert.Equal("Order already exists.", error.Message);
    }

    [Fact]
    public void ConflictError_IsAnError()
    {
        var error = new ConflictError("conflict");

        Assert.IsAssignableFrom<Error>(error);
    }

    [Fact]
    public void TwoConflictErrors_WithSameValues_AreEqual()
    {
        var a = new ConflictError("X", "msg");
        var b = new ConflictError("X", "msg");

        Assert.Equal(a, b);
    }
}

public sealed class ForbiddenErrorTests
{
    [Fact]
    public void Ctor_Message_SetsDefaultCode()
    {
        var error = new ForbiddenError("Access denied.");

        Assert.Equal("Forbidden", error.Code);
    }

    [Fact]
    public void Ctor_Message_SetsMessage()
    {
        var error = new ForbiddenError("Access denied.");

        Assert.Equal("Access denied.", error.Message);
    }

    [Fact]
    public void Ctor_CodeAndMessage_SetsExplicitCode()
    {
        var error = new ForbiddenError("User.Forbidden", "You cannot do that.");

        Assert.Equal("User.Forbidden", error.Code);
        Assert.Equal("You cannot do that.", error.Message);
    }

    [Fact]
    public void ForbiddenError_IsAnError()
    {
        var error = new ForbiddenError("forbidden");

        Assert.IsAssignableFrom<Error>(error);
    }

    [Fact]
    public void TwoForbiddenErrors_WithSameValues_AreEqual()
    {
        var a = new ForbiddenError("F", "msg");
        var b = new ForbiddenError("F", "msg");

        Assert.Equal(a, b);
    }
}