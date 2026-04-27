using Resultify.Errors;

namespace Resultify.Tests;

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

public sealed class ValidationErrorNullTests
{

    [Fact]
    public void Ctor_Message_NullMessage_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => new ValidationError(null!));
    }

    [Fact]
    public void Ctor_PropertyNameAndMessage_NullPropertyName_ShouldThrow()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new ValidationError(null!, "is invalid"));

        Assert.Equal("propertyName", ex.ParamName);
    }

    [Fact]
    public void Ctor_PropertyNameAndMessage_NullMessage_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => new ValidationError("Email", null!));
    }
}