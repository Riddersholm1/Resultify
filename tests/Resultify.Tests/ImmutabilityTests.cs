using System.Collections.Concurrent;
using Resultify.Errors;

namespace Resultify.Tests;

/// <summary>
/// The library advertises immutable, freely shareable results. These tests pin that promise at the
/// representation level: no construction path may hand out an error list a caller can write through,
/// and no caller-held collection may reach inside a result after the fact.
/// </summary>
public sealed class ErrorListImmutabilityTests
{
    /// <summary>
    /// Every way of building a failure, so the "cannot be mutated" check below cannot miss a path.
    /// A result is a struct: all copies share one error list, so a single successful write here
    /// would corrupt every copy in the program.
    /// </summary>
    public static TheoryData<string, IReadOnlyList<Error>> AllFailureErrorLists() => new()
    {
        { "Failure(Error)", Result.Failure(new Error("one")).Errors },
        { "Failure(string)", Result.Failure("one").Errors },
        { "Failure(code, message)", Result.Failure("C", "one").Errors },
        { "Failure(IEnumerable) single", Result.Failure([new Error("one")]).Errors },
        { "Failure(IEnumerable) multiple", Result.Failure([new Error("a"), new Error("b")]).Errors },
        { "Merge(span)", Result.Merge(Result.Failure("x"), Result.Failure("y")).Errors },
        { "Merge(IEnumerable)", new[] { Result.Failure("x"), Result.Failure("y") }.Merge().Errors },
        { "Merge(typed)", new[] { Result<int>.Failure("x"), Result<int>.Failure("y") }.Merge().Errors },
        { "Result<T>.Failure(Error)", Result<int>.Failure(new Error("one")).Errors },
        { "Result<T>.Failure(IEnumerable)", Result<int>.Failure([new Error("a"), new Error("b")]).Errors },
        { "success (shared empty)", Result.Success().Errors },
        { "propagated through Bind", Result.Failure([new Error("a"), new Error("b")]).Bind(Result.Success).Errors },
        { "propagated through Map", Result<int>.Failure([new Error("a"), new Error("b")]).Map(v => v).Errors },
    };

    [Theory]
    [MemberData(nameof(AllFailureErrorLists))]
    public void Errors_ShouldNotBeCastableToAWritableCollection(string path, IReadOnlyList<Error> errors)
    {
        Assert.False(errors is Error[], $"{path}: Errors can be cast to Error[] and written through.");
        Assert.False(errors is IList<Error> { IsReadOnly: false }, $"{path}: Errors exposes a writable IList<Error>.");
    }

    [Theory]
    [MemberData(nameof(AllFailureErrorLists))]
    public void Errors_WhenMutationIsAttemptedThroughIList_ShouldThrow(string path, IReadOnlyList<Error> errors)
    {
        if (errors is not IList<Error> list)
        {
            return; // not reachable as IList at all, which is even stronger
        }

        Assert.ThrowsAny<NotSupportedException>(() => list.Add(new Error("injected")));
        if (list.Count > 0)
        {
            Assert.ThrowsAny<NotSupportedException>(() => list[0] = new Error("injected"));
        }
    }

    [Fact]
    public void Failure_WithListOfErrors_ShouldNotObserveLaterCallerMutation()
    {
        var errors = new List<Error> { new("err1"), new("err2") };
        Result result = Result.Failure(errors);

        errors.Add(new Error("err3"));
        errors.Clear();

        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public void FailureT_WithListOfErrors_ShouldNotObserveLaterCallerMutation()
    {
        var errors = new List<Error> { new("err1") };
        Result<int> result = Result<int>.Failure(errors);

        errors.Add(new Error("err2"));

        Assert.Single(result.Errors);
    }

    [Fact]
    public void Causes_ShouldNotBeCastableToAWritableCollection()
    {
        Error fromSingle = new Error("top").CausedBy(new Error("c1"));
        Error fromEnumerable = new Error("top").CausedBy([new Error("c1"), new Error("c2")]);
        Error fromException = new Error("top").CausedBy(new InvalidOperationException("boom"));

        Assert.False(fromSingle.Causes is Error[]);
        Assert.False(fromEnumerable.Causes is Error[]);
        Assert.False(fromException.Causes is Error[]);
    }

    [Fact]
    public void CausedBy_WithList_ShouldNotObserveLaterCallerMutation()
    {
        var causes = new List<Error> { new("c1") };
        Error error = new Error("top").CausedBy(causes);

        causes.Add(new Error("c2"));

        Assert.Single(error.Causes);
    }

    [Fact]
    public void Metadata_ShouldNotBeCastableToAWritableDictionary()
    {
        Error error = new Error("code", "msg").WithMetadata("k", "v");

        Assert.False(error.Metadata is IDictionary<string, object> { IsReadOnly: false });
    }
}

public sealed class SharedEmptyErrorListTests
{
    [Fact]
    public void Result_SuccessErrors_ShouldReturnSameInstanceAcrossCalls()
    {
        Result r1 = Result.Success();
        Result r2 = Result.Success();

        Assert.Same(r1.Errors, r2.Errors);
    }

    [Fact]
    public void ResultT_SuccessErrors_ShouldReturnSameInstanceAcrossCalls()
    {
        Result<int> r1 = Result<int>.Success(1);
        Result<int> r2 = Result<int>.Success(2);

        Assert.Same(r1.Errors, r2.Errors);
    }

    [Fact]
    public void ResultT_DefaultErrors_ShouldReturnSameSharedEmptyList()
    {
        Result<int> defaulted = default;
        Result<int> succeeded = Result<int>.Success(1);

        Assert.Same(defaulted.Errors, succeeded.Errors);
    }

    [Fact]
    public void EmptyErrorList_ShouldBeSharedAcrossClosedGenericTypes()
    {
        Assert.Same(Result.Success().Errors, Result<int>.Success(1).Errors);
        Assert.Same(Result<int>.Success(1).Errors, Result<string>.Success("x").Errors);
    }
}

/// <summary>
/// The README states instances may be shared across threads without synchronisation. These exercise
/// concurrent readers over one shared failure to back that claim.
/// </summary>
public sealed class ConcurrentReaderTests
{
    [Fact]
    public void SharedFailedResult_ReadConcurrently_ShouldStayConsistent()
    {
        Result shared = Result.Failure([new Error("A.Code", "a"), new Error("B.Code", "b")]);
        var observations = new ConcurrentBag<string>();

        Parallel.For(0, 500, _ =>
        {
            observations.Add($"{shared.IsFailure}|{shared.Errors.Count}|{shared.FirstError.Code}|{shared.HasErrorCode("B.Code")}");
        });

        Assert.Equal(500, observations.Count);
        Assert.Single(observations.Distinct());
        Assert.Equal("True|2|A.Code|True", observations.First());
    }

    [Fact]
    public void SharedError_MetadataAndCausesReadConcurrently_ShouldStayConsistent()
    {
        Error shared = new Error("E.Code", "message")
            .WithMetadata("key", "value")
            .CausedBy(new Error("root", "root cause"));
        var observations = new ConcurrentBag<string>();

        Parallel.For(0, 500, _ =>
        {
            observations.Add($"{shared.Metadata.Count}|{shared.Metadata["key"]}|{shared.Causes.Count}|{shared}");
        });

        Assert.Single(observations.Distinct());
    }

    [Fact]
    public void DerivingNewErrorsConcurrently_ShouldNotDisturbTheOriginal()
    {
        var original = new Error("E.Code", "message");

        Parallel.For(0, 500, i =>
        {
            Error derived = original.WithMetadata($"k{i}", i).CausedBy(new Error($"c{i}"));

            Assert.Single(derived.Metadata);
            Assert.Single(derived.Causes);
        });

        Assert.Empty(original.Metadata);
        Assert.Empty(original.Causes);
    }
}
