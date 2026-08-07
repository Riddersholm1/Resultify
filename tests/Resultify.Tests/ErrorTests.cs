using Resultify.Errors;

namespace Resultify.Tests;

/// <summary>
/// The <see cref="Error"/> record itself: construction, the well-known sentinels, and the
/// copy-on-write builders. Null-argument handling lives in <c>NullGuardTests</c>, formatting in
/// <c>ToStringTests</c>, and the built-in subtypes in <c>ErrorTypesTests</c>.
/// </summary>
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
    public void Error_ShouldStartWithNoMetadataOrCauses()
    {
        var error = new Error("code", "message");

        Assert.Empty(error.Metadata);
        Assert.Empty(error.Causes);
    }

    [Fact]
    public void Error_None_ShouldBeEmpty()
    {
        Assert.Empty(Error.None.Code);
        Assert.Empty(Error.None.Message);
    }

    [Fact]
    public void Error_NullValue_ShouldHaveStableCode() =>
        Assert.Equal("General.NullValue", Error.NullValue.Code);

    [Fact]
    public void Error_Unknown_ShouldHaveStableCode() =>
        Assert.Equal("General.Unknown", Error.Unknown.Code);

    [Fact]
    public void Deconstruct_ShouldProduceCodeAndMessage()
    {
        var error = new Error("E.Code", "A message");
        (string code, string message) = error;

        Assert.Equal("E.Code", code);
        Assert.Equal("A message", message);
    }

    [Fact]
    public void Deconstruct_OnMessageOnlyError_CodeShouldBeEmpty()
    {
        var error = new Error("just a message");
        (string code, string message) = error;

        Assert.Equal(string.Empty, code);
        Assert.Equal("just a message", message);
    }
}

/// <summary>
/// <c>WithMetadata</c> and <c>CausedBy</c> are copy-on-write: they must return a new instance and
/// leave the receiver untouched, which is what makes a shared static error registry safe.
/// </summary>
public sealed class ErrorMetadataAndCausesTests
{
    [Fact]
    public void WithMetadata_ShouldNotMutateTheOriginal()
    {
        var original = new Error("base");
        Error withMeta = original.WithMetadata("code", "E001");

        Assert.Empty(original.Metadata);
        Assert.True(withMeta.Metadata.ContainsKey("code"));
    }

    [Fact]
    public void WithMetadata_ShouldAccumulateAcrossCalls()
    {
        Error err = new Error("C", "M")
            .WithMetadata("k1", 1)
            .WithMetadata("k2", 2)
            .WithMetadata("k3", 3);

        Assert.Equal(3, err.Metadata.Count);
        Assert.Equal(1, err.Metadata["k1"]);
        Assert.Equal(2, err.Metadata["k2"]);
        Assert.Equal(3, err.Metadata["k3"]);
    }

    [Fact]
    public void WithMetadata_WithAnExistingKey_ShouldReplaceTheValue()
    {
        Error err = new Error("C", "M")
            .WithMetadata("key", "first")
            .WithMetadata("key", "second");

        Assert.Single(err.Metadata);
        Assert.Equal("second", err.Metadata["key"]);
    }

    [Fact]
    public void WithMetadata_Enumerable_ShouldAppendEveryPair()
    {
        var error = new Error("code", "msg");
        var meta = new List<KeyValuePair<string, object>>
        {
            new("k1", "v1"),
            new("k2", 42),
        };

        Error updated = error.WithMetadata(meta);

        Assert.Equal(2, updated.Metadata.Count);
        Assert.Equal("v1", updated.Metadata["k1"]);
        Assert.Equal(42, updated.Metadata["k2"]);
    }

    [Fact]
    public void WithMetadata_Enumerable_ShouldMergeWithExistingEntries()
    {
        Error error = new Error("code", "msg").WithMetadata("existing", 1);

        Error updated = error.WithMetadata([new KeyValuePair<string, object>("added", 2)]);

        Assert.Equal(2, updated.Metadata.Count);
        Assert.Equal(1, updated.Metadata["existing"]);
    }

    [Fact]
    public void CausedBy_Error_ShouldChain()
    {
        var root = new Error("root cause");
        Error error = new Error("wrapper").CausedBy(root);

        Error cause = Assert.Single(error.Causes);
        Assert.Equal("root cause", cause.Message);
    }

    [Fact]
    public void CausedBy_Error_ShouldNotMutateTheOriginal()
    {
        var original = new Error("wrapper");

        _ = original.CausedBy(new Error("root"));

        Assert.Empty(original.Causes);
    }

    [Fact]
    public void CausedBy_Enumerable_ShouldAppendAllInOrder()
    {
        Error error = new Error("top")
            .CausedBy([new Error("c1"), new Error("c2")]);

        Assert.Equal(2, error.Causes.Count);
        Assert.Equal("c1", error.Causes[0].Message);
        Assert.Equal("c2", error.Causes[1].Message);
    }

    [Fact]
    public void CausedBy_Exception_ShouldWrapItAsAnExceptionalError()
    {
        var exception = new InvalidOperationException("boom");

        Error error = new Error("top").CausedBy(exception);

        ExceptionalError cause = Assert.IsType<ExceptionalError>(Assert.Single(error.Causes));
        Assert.Same(exception, cause.Exception);
    }

    [Fact]
    public void CausedBy_ShouldAccumulateAcrossCalls()
    {
        Error error = new Error("top")
            .CausedBy(new Error("c1"))
            .CausedBy(new Error("c2"));

        Assert.Equal(2, error.Causes.Count);
    }

    [Fact]
    public void MetadataThenCausedBy_ShouldKeepBoth()
    {
        Error err = new Error("C", "M")
            .WithMetadata("key", "value")
            .CausedBy(new Error("cause"));

        Assert.Equal("value", err.Metadata["key"]);
        Assert.Equal("cause", Assert.Single(err.Causes).Message);
    }

    [Fact]
    public void CausedByThenMetadata_ShouldKeepBoth()
    {
        Error err = new Error("C", "M")
            .CausedBy(new Error("cause"))
            .WithMetadata("key", "value");

        Assert.Equal("value", err.Metadata["key"]);
        Assert.Single(err.Causes);
    }

    [Fact]
    public void BuildersOnASubtype_ShouldPreserveTheRuntimeType()
    {
        Error error = new NotFoundError("Customer", 42).WithMetadata("tenant", "acme");

        NotFoundError notFound = Assert.IsType<NotFoundError>(error);
        Assert.Equal("Customer", notFound.EntityName);
        Assert.Equal("acme", error.Metadata["tenant"]);
    }
}

/// <summary>
/// Errors are value objects: two errors describing the same failure must be interchangeable, which
/// is what lets a domain error registry expose <c>static readonly</c> instances and lets callers
/// compare against them.
/// </summary>
public sealed class ErrorEqualityTests
{
    [Fact]
    public void Errors_WithSameCodeAndMessage_ShouldBeEqual()
    {
        var a = new Error("CODE", "message");
        var b = new Error("CODE", "message");

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Errors_WithDifferentCodes_ShouldNotBeEqual() =>
        Assert.NotEqual(new Error("A", "message"), new Error("B", "message"));

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
    public void Errors_WithAndWithoutMetadata_ShouldNotBeEqual() =>
        Assert.NotEqual(new Error("C", "M"), new Error("C", "M").WithMetadata("key", "value"));

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

    [Fact]
    public void Errors_WithSameMetadataAddedInDifferentOrder_ShouldBeEqual()
    {
        Error a = new Error("C", "M").WithMetadata("k1", 1).WithMetadata("k2", 2);
        Error b = new Error("C", "M").WithMetadata("k2", 2).WithMetadata("k1", 1);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
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
    public void Errors_BuiltInEitherOrder_ShouldBeEqual()
    {
        Error a = new Error("C", "M")
            .WithMetadata("key", "value")
            .CausedBy(new Error("cause"));

        Error b = new Error("C", "M")
            .CausedBy(new Error("cause"))
            .WithMetadata("key", "value");

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }
}
