using Resultify.Errors;

namespace Resultify.Tests;

/// <summary>
/// Merging is how validation results are aggregated, so the rules matter: a merge fails if any
/// input failed, it collects <em>every</em> error rather than short-circuiting on the first, and it
/// preserves input order.
/// </summary>
public sealed class MergeTests
{
    [Fact]
    public void Merge_Span_AllSuccess_ShouldSucceed() =>
        Assert.True(Result.Merge(Result.Success(), Result.Success()).IsSuccess);

    [Fact]
    public void Merge_Span_NoArgs_ShouldSucceed() =>
        Assert.True(Result.Merge().IsSuccess);

    [Fact]
    public void Merge_Span_WithFailures_ShouldCombineAllErrorsInOrder()
    {
        Result merged = Result.Merge(
            Result.Failure("Error 1"),
            Result.Success(),
            Result.Failure("Error 2"));

        Assert.True(merged.IsFailure);
        Assert.Equal(2, merged.Errors.Count);
        Assert.Equal("Error 1", merged.Errors[0].Message);
        Assert.Equal("Error 2", merged.Errors[1].Message);
    }

    [Fact]
    public void Merge_Span_ShouldCarryEveryErrorOfAMultiErrorInput()
    {
        Result merged = Result.Merge(
            Result.Failure([new Error("a"), new Error("b")]),
            Result.Failure("c"));

        Assert.Equal(3, merged.Errors.Count);
    }

    [Fact]
    public void Merge_Collection_ShouldCombine()
    {
        var results = new List<Result>
        {
            Result.Success(),
            Result.Failure("err1"),
            Result.Failure("err2"),
        };

        Result merged = results.Merge();

        Assert.True(merged.IsFailure);
        Assert.Equal(2, merged.Errors.Count);
    }

    [Fact]
    public void Merge_EmptyCollection_ShouldSucceed() =>
        Assert.True(Array.Empty<Result>().Merge().IsSuccess);

    [Fact]
    public void Merge_CollectionOfAllSuccesses_ShouldSucceed() =>
        Assert.True(new[] { Result.Success(), Result.Success() }.Merge().IsSuccess);

    [Fact]
    public void Merge_ShouldEnumerateALazySequenceExactlyOnce()
    {
        var enumerations = 0;

        IEnumerable<Result> Lazy()
        {
            enumerations++;
            yield return Result.Failure("one");
            yield return Result.Success();
        }

        Result merged = Lazy().Merge();

        Assert.Equal(1, enumerations);
        Assert.True(merged.IsFailure);
    }
}

public sealed class TypedMergeTests
{
    [Fact]
    public void Merge_AllSuccess_ShouldPreserveValueOrder()
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

    [Fact]
    public void Merge_EmptyCollection_ShouldSucceedWithEmptyValues()
    {
        Result<IReadOnlyList<int>> merged = Array.Empty<Result<int>>().Merge();

        Assert.True(merged.IsSuccess);
        Assert.Empty(merged.Value);
    }

    [Fact]
    public void Merge_WithFailureAfterSuccesses_ShouldDiscardValuesAndKeepEveryError()
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
    public void Merge_WithFailureFirst_ShouldStillCollectLaterErrors()
    {
        Result<int>[] results =
        [
            Result<int>.Failure("first"),
            Result<int>.Success(1),
            Result<int>.Failure("second"),
        ];

        Result<IReadOnlyList<int>> merged = results.Merge();

        Assert.Equal(2, merged.Errors.Count);
    }

    [Fact]
    public void Merge_OfReferenceTypes_ShouldPreserveValues()
    {
        Result<IReadOnlyList<string>> merged = new[]
        {
            Result<string>.Success("a"),
            Result<string>.Success("b"),
        }.Merge();

        Assert.Equal(["a", "b"], merged.Value);
    }

    [Fact]
    public void Merge_ShouldEnumerateALazySequenceExactlyOnce()
    {
        var enumerations = 0;

        IEnumerable<Result<int>> Lazy()
        {
            enumerations++;
            yield return Result<int>.Success(1);
            yield return Result<int>.Success(2);
        }

        Result<IReadOnlyList<int>> merged = Lazy().Merge();

        Assert.Equal(1, enumerations);
        Assert.Equal([1, 2], merged.Value);
    }
}
