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