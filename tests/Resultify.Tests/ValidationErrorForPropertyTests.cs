using Resultify.Errors;

namespace Resultify.Tests;

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
        Assert.Throws<ArgumentNullException>(() => new ValidationError(null!, "is invalid"));
    }

    [Fact]
    public void Ctor_PropertyNameAndMessage_NullPropertyName_ShouldNotProduceCorruptCode()
    {
        // Ensures null does not silently produce "Validation." rather than throwing.
        var ex = Assert.Throws<ArgumentNullException>(() => new ValidationError(null!, "is invalid"));

        Assert.Equal("propertyName", ex.ParamName);
    }

    [Fact]
    public void Ctor_PropertyNameAndMessage_NullMessage_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => new ValidationError("Email", null!));
    }
}