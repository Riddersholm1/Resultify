using Resultify.Errors;

namespace Resultify.Tests;

public sealed class ErrorNullValidationTests
{
    [Fact]
    public void Error_WithNullCode_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => new Error(null!, "message"));
    }

    [Fact]
    public void Error_WithNullMessage_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => new Error("code", null!));
    }
}

public sealed class WithMetadataValidationTests
{
    [Fact]
    public void WithMetadata_Enumerable_WithNullKey_ShouldThrowArgumentException()
    {
        var error = new Error("code", "msg");
        var meta = new List<KeyValuePair<string, object>>
        {
            new(null!, "value"),
        };

        var ex = Assert.Throws<ArgumentException>(() => error.WithMetadata(meta));
        Assert.Equal("metadata", ex.ParamName);
    }

    [Fact]
    public void WithMetadata_Enumerable_WithNullValue_ShouldThrowArgumentException()
    {
        var error = new Error("code", "msg");
        var meta = new List<KeyValuePair<string, object>>
        {
            new("key", null!),
        };

        var ex = Assert.Throws<ArgumentException>(() => error.WithMetadata(meta));
        Assert.Equal("metadata", ex.ParamName);
        Assert.Contains("key", ex.Message);
    }

    [Fact]
    public void WithMetadata_Enumerable_WithNullEnumerable_ShouldThrowArgumentNullException()
    {
        var error = new Error("code", "msg");

        Assert.Throws<ArgumentNullException>(() => error.WithMetadata(null!));
    }

    [Fact]
    public void WithMetadata_Enumerable_WithValidPairs_ShouldAppend()
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
}

public sealed class CausedByNullGuardTests
{
    [Fact]
    public void CausedBy_NullError_ShouldThrow()
    {
        var error = new Error("code", "msg");
        Error? nullCause = null;

        Assert.Throws<ArgumentNullException>(() => error.CausedBy(nullCause!));
    }

    [Fact]
    public void CausedBy_NullEnumerable_ShouldThrow()
    {
        var error = new Error("code", "msg");
        IEnumerable<Error>? nullCauses = null;

        Assert.Throws<ArgumentNullException>(() => error.CausedBy(nullCauses!));
    }

    [Fact]
    public void CausedBy_EnumerableWithNullElement_ShouldThrow()
    {
        var error = new Error("code", "msg");
        IEnumerable<Error> causes = [new("c1"), null!, new("c3")];

        var ex = Assert.Throws<ArgumentException>(() => error.CausedBy(causes));
        Assert.Equal("causes", ex.ParamName);
    }

    [Fact]
    public void CausedBy_NullException_ShouldThrow()
    {
        var error = new Error("code", "msg");
        Exception? nullEx = null;

        Assert.Throws<ArgumentNullException>(() => error.CausedBy(nullEx!));
    }

    [Fact]
    public void CausedBy_ValidCauses_ShouldAppendAll()
    {
        Error error = new Error("top")
            .CausedBy([new Error("c1"), new Error("c2")]);

        Assert.Equal(2, error.Causes.Count);
        Assert.Equal("c1", error.Causes[0].Message);
        Assert.Equal("c2", error.Causes[1].Message);
    }
}

public sealed class FailureNullElementTests
{
    [Fact]
    public void Result_Failure_WithNullErrorElement_ShouldThrow()
    {
        IEnumerable<Error> errors = [new("ok"), null!];

        var ex = Assert.Throws<ArgumentException>(() => Result.Failure(errors));
        Assert.Equal("errors", ex.ParamName);
    }

    [Fact]
    public void ResultT_Failure_WithNullErrorElement_ShouldThrow()
    {
        IEnumerable<Error> errors = [new("ok"), null!];

        var ex = Assert.Throws<ArgumentException>(() => Result<int>.Failure(errors));
        Assert.Equal("errors", ex.ParamName);
    }
}