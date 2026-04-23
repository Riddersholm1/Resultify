using Resultify.Errors;

namespace Resultify.Tests;

public sealed class MetadataAndCausesCombinationTests
{
    [Fact]
    public void Error_WithMetadataThenCausedBy_ShouldKeepBoth()
    {
        Error err = new Error("C", "M")
            .WithMetadata("key", "value")
            .CausedBy(new Error("cause"));

        Assert.Equal("value", err.Metadata["key"]);
        Assert.Single(err.Causes);
        Assert.Equal("cause", err.Causes[0].Message);
    }

    [Fact]
    public void Error_CausedByThenWithMetadata_ShouldKeepBoth()
    {
        Error err = new Error("C", "M")
            .CausedBy(new Error("cause"))
            .WithMetadata("key", "value");

        Assert.Equal("value", err.Metadata["key"]);
        Assert.Single(err.Causes);
    }

    [Fact]
    public void Error_MetadataAndCauses_ShouldBeOrderIndependentForEquality()
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

    [Fact]
    public void Error_MultipleWithMetadataChains_ShouldAccumulate()
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
}