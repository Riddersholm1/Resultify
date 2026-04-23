using Resultify.Errors;

namespace Resultify.Tests;

public sealed class ErrorEqualityTests
{
    [Fact]
    public void Errors_WithSameMetadata_ShouldBeEqual()
    {
        Error a = new Error("C", "M").WithMetadata("key", "value");
        Error b = new Error("C", "M").WithMetadata("key", "value");

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Errors_WithDifferentMetadata_ShouldNotBeEqual()
    {
        Error a = new Error("C", "M").WithMetadata("key", "value1");
        Error b = new Error("C", "M").WithMetadata("key", "value2");

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Errors_WithSameCauses_ShouldBeEqual()
    {
        var cause = new Error("cause");
        Error a = new Error("C", "M").CausedBy(cause);
        Error b = new Error("C", "M").CausedBy(cause);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Errors_WithDifferentCauses_ShouldNotBeEqual()
    {
        Error a = new Error("C", "M").CausedBy(new Error("cause1"));
        Error b = new Error("C", "M").CausedBy(new Error("cause2"));

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Errors_WithMultipleMetadata_ShouldBeEqual()
    {
        Error a = new Error("C", "M")
            .WithMetadata("k1", "v1")
            .WithMetadata("k2", 42);
        Error b = new Error("C", "M")
            .WithMetadata("k1", "v1")
            .WithMetadata("k2", 42);

        Assert.Equal(a, b);
    }
}

public sealed class ErrorSubtypeEqualityTests
{
    [Fact]
    public void NotFoundError_ShouldNotEqualPlainErrorWithMatchingCodeAndMessage()
    {
        var notFound = new NotFoundError("Customer", 42);
        var plain = new Error(notFound.Code, notFound.Message);

        Assert.NotEqual(notFound, plain);
        Assert.NotEqual(plain, notFound);
    }

    [Fact]
    public void ValidationError_ShouldNotEqualPlainErrorWithMatchingCodeAndMessage()
    {
        var vErr = new ValidationError("Email", "Required");
        var plain = new Error(vErr.Code, vErr.Message);

        Assert.NotEqual(vErr, plain);
    }

    [Fact]
    public void ExceptionalError_WithDifferentExceptionInstances_ShouldNotBeEqual()
    {
        var a = new ExceptionalError(new InvalidOperationException("same"));
        var b = new ExceptionalError(new InvalidOperationException("same"));

        Assert.NotEqual<Error>(a, b);
    }

    [Fact]
    public void ExceptionalError_WithSameExceptionInstance_ShouldBeEqual()
    {
        var ex = new InvalidOperationException("same");
        var a = new ExceptionalError(ex);
        var b = new ExceptionalError(ex);

        Assert.Equal<Error>(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void NotFoundError_WithSameEntityAndId_ShouldBeEqual()
    {
        var a = new NotFoundError("Customer", 42);
        var b = new NotFoundError("Customer", 42);

        Assert.Equal<Error>(a, b);
    }
}