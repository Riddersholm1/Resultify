namespace Resultify.Tests;

public sealed class EmptyMergeTests
{
    [Fact]
    public void Merge_EmptyCollection_ShouldSucceed()
    {
        Result merged = Array.Empty<Result>().Merge();

        Assert.True(merged.IsSuccess);
    }

    [Fact]
    public void Merge_EmptyTypedCollection_ShouldSucceedWithEmptyValues()
    {
        Result<IReadOnlyList<int>> merged = Array.Empty<Result<int>>().Merge();

        Assert.True(merged.IsSuccess);
        Assert.Empty(merged.Value);
    }

    [Fact]
    public void Merge_Span_NoArgs_ShouldSucceed()
    {
        Result merged = Result.Merge();

        Assert.True(merged.IsSuccess);
    }
}

public sealed class MergeShortCircuitTests
{
    [Fact]
    public void Merge_Typed_WithFailureAfterSuccesses_ShouldProduceFailureOnly()
    {
        Result<int>[] results =
        [
            Result<int>.Success(1),
            Result<int>.Success(2),
            Result<int>.Failure("boom"),
            Result<int>.Success(3),
            Result<int>.Failure("boom2"),
        ];

        Result<IReadOnlyList<int>> merged = results.Merge();

        Assert.True(merged.IsFailure);
        Assert.Equal(2, merged.Errors.Count);
        Assert.Equal("boom", merged.Errors[0].Message);
        Assert.Equal("boom2", merged.Errors[1].Message);
    }

    [Fact]
    public void Merge_Typed_AllSuccess_ShouldPreserveOrder()
    {
        Result<int>[] results =
        [
            Result<int>.Success(1),
            Result<int>.Success(2),
            Result<int>.Success(3),
        ];

        Result<IReadOnlyList<int>> merged = results.Merge();

        Assert.True(merged.IsSuccess);
        Assert.Equal([1, 2, 3], merged.Value);
    }
}