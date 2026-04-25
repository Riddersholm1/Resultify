using Resultify.Errors;

namespace Resultify.Tests;

public sealed class NotFoundErrorCodeMessageCtorTests
{
    [Fact]
    public void NotFoundError_WithCodeAndMessage_ShouldUseProvidedValues()
    {
        var error = new NotFoundError("Cart.Empty", "Cart contains no items");

        Assert.Equal("Cart.Empty", error.Code);
        Assert.Equal("Cart contains no items", error.Message);
        Assert.Null(error.EntityName);
        Assert.Null(error.EntityId);
    }

    [Fact]
    public void NotFoundError_WithEntityAndId_ShouldStillWork()
    {
        var error = new NotFoundError("Customer", 42);

        Assert.Equal("Customer.NotFound", error.Code);
        Assert.Equal("Customer", error.EntityName);
        Assert.Equal(42, error.EntityId);
    }
}

public sealed class ValidationErrorForPropertyTests
{
    [Fact]
    public void ForProperty_ShouldSetPropertyNameAndCode()
    {
        ValidationError error = ValidationError.ForProperty("Email");

        Assert.Equal("Email", error.PropertyName);
        Assert.Equal("Validation.Email", error.Code);
        Assert.Contains("Email", error.Message);
    }

    [Fact]
    public void ForProperty_WithNullPropertyName_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => ValidationError.ForProperty(null!));
    }
}