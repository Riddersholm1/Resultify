using Resultify.Errors;

namespace Resultify.Tests;

public sealed class NotFoundErrorTests
{
    // ── NotFoundError(string message) ────────────────────────

    [Fact]
    public void Ctor_Message_SetsDefaultCode()
    {
        var error = new NotFoundError("User was not found.");

        Assert.Equal("NotFound", error.Code);
    }

    [Fact]
    public void Ctor_Message_SetsMessage()
    {
        var error = new NotFoundError("User was not found.");

        Assert.Equal("User was not found.", error.Message);
    }

    [Fact]
    public void Ctor_Message_EntityNameAndEntityIdAreNull()
    {
        var error = new NotFoundError("User was not found.");

        Assert.Null(error.EntityName);
        Assert.Null(error.EntityId);
    }

    [Fact]
    public void Ctor_Message_NullMessage_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new NotFoundError(null!));
    }

    // ── NotFoundError(string entityName, object entityId) ───

    [Fact]
    public void Ctor_EntityAndId_SetsCode()
    {
        var error = new NotFoundError("Customer", 42);

        Assert.Equal("Customer.NotFound", error.Code);
    }

    [Fact]
    public void Ctor_EntityAndId_SetsMessage()
    {
        var error = new NotFoundError("Customer", 42);

        Assert.Equal("Customer with id '42' was not found.", error.Message);
    }

    [Fact]
    public void Ctor_EntityAndId_SetsEntityName()
    {
        var error = new NotFoundError("Customer", 42);

        Assert.Equal("Customer", error.EntityName);
    }

    [Fact]
    public void Ctor_EntityAndId_SetsEntityId()
    {
        var error = new NotFoundError("Customer", 42);

        Assert.Equal(42, error.EntityId);
    }

    [Fact]
    public void Ctor_EntityAndId_NullEntityName_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new NotFoundError(null!, 42));
    }

    [Fact]
    public void Ctor_EntityAndId_NullEntityId_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new NotFoundError("Customer", null!));
    }

    [Fact]
    public void Ctor_EntityAndId_StringEntityId_FormatsCorrectly()
    {
        var error = new NotFoundError("Order", "ORD-999");

        Assert.Equal("Order.NotFound", error.Code);
        Assert.Equal("Order with id 'ORD-999' was not found.", error.Message);
        Assert.Equal("ORD-999", error.EntityId);
    }

    // ── IsA ──────────────────────────────────────────────────

    [Fact]
    public void NotFoundError_IsAnError()
    {
        var error = new NotFoundError("Not found.");

        Assert.IsAssignableFrom<Error>(error);
    }

    // ── Equality ─────────────────────────────────────────────

    [Fact]
    public void TwoNotFoundErrors_WithSameValues_AreEqual()
    {
        var a = new NotFoundError("Customer", 42);
        var b = new NotFoundError("Customer", 42);

        Assert.Equal(a, b);
    }

    [Fact]
    public void TwoNotFoundErrors_WithDifferentEntityNames_AreNotEqual()
    {
        var a = new NotFoundError("Customer", 42);
        var b = new NotFoundError("Order", 42);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void TwoNotFoundErrors_WithDifferentIds_AreNotEqual()
    {
        var a = new NotFoundError("Customer", 1);
        var b = new NotFoundError("Customer", 2);

        Assert.NotEqual(a, b);
    }
}