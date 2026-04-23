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