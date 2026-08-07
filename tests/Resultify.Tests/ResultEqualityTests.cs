using Resultify.Errors;

namespace Resultify.Tests;

/// <summary>
/// <see cref="Result"/> and <see cref="Result{TValue}"/> implement <see cref="IEquatable{T}"/> and
/// the equality operators, so they can be compared, asserted on and used as dictionary keys. These
/// pin the contract, including the requirement that equal values produce equal hash codes.
/// </summary>
public sealed class ResultEqualityTests
{
    [Fact]
    public void TwoSuccesses_ShouldBeEqual()
    {
        Result a = Result.Success();
        Result b = Result.Success();

        Assert.True(a.Equals(b));
        Assert.True(a == b);
        Assert.False(a != b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void DefaultResult_ShouldEqualAnExplicitSuccess()
    {
        Result defaulted = default;

        Assert.True(defaulted == Result.Success());
        Assert.Equal(defaulted.GetHashCode(), Result.Success().GetHashCode());
    }

    [Fact]
    public void FailuresWithEqualErrors_ShouldBeEqual()
    {
        Result a = Result.Failure("E.Code", "message");
        Result b = Result.Failure("E.Code", "message");

        Assert.True(a == b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void FailuresWithDifferentErrors_ShouldNotBeEqual()
    {
        Result a = Result.Failure("E.Code", "message");
        Result b = Result.Failure("E.Other", "message");

        Assert.True(a != b);
        Assert.False(a.Equals(b));
    }

    [Fact]
    public void FailuresWithDifferentErrorCounts_ShouldNotBeEqual()
    {
        Result one = Result.Failure([new Error("a")]);
        Result two = Result.Failure([new Error("a"), new Error("b")]);

        Assert.True(one != two);
    }

    [Fact]
    public void FailuresWithSameErrorsInDifferentOrder_ShouldNotBeEqual()
    {
        Result ab = Result.Failure([new Error("a"), new Error("b")]);
        Result ba = Result.Failure([new Error("b"), new Error("a")]);

        Assert.True(ab != ba);
    }

    [Fact]
    public void SuccessAndFailure_ShouldNotBeEqual()
    {
        Assert.True(Result.Success() != Result.Failure("boom"));
        Assert.NotEqual(Result.Success().GetHashCode(), Result.Failure("boom").GetHashCode());
    }

    [Fact]
    public void Equals_Object_ShouldMatchTypedEquals()
    {
        object boxed = Result.Failure("E.Code", "message");

        Assert.True(Result.Failure("E.Code", "message").Equals(boxed));
        Assert.False(Result.Success().Equals(boxed));
    }

    [Fact]
    public void Equals_Object_WithUnrelatedType_ShouldBeFalse()
    {
        Assert.False(Result.Success().Equals("not a result"));
        Assert.False(Result.Success().Equals((object?)null));
    }

    [Fact]
    public void Equals_ShouldIgnoreHowTheFailureWasBuilt()
    {
        Result viaMessage = Result.Failure("boom");
        Result viaError = Result.Failure(new Error("boom"));
        Result viaEnumerable = Result.Failure([new Error("boom")]);
        Result viaImplicit = new Error("boom");

        Assert.True(viaMessage == viaError);
        Assert.True(viaError == viaEnumerable);
        Assert.True(viaEnumerable == viaImplicit);
    }

    [Fact]
    public void Result_ShouldWorkAsADictionaryKey()
    {
        var seen = new Dictionary<Result, int>
        {
            [Result.Success()] = 1,
            [Result.Failure("E.Code", "message")] = 2,
        };

        Assert.Equal(1, seen[Result.Success()]);
        Assert.Equal(2, seen[Result.Failure("E.Code", "message")]);
        Assert.False(seen.ContainsKey(Result.Failure("Other", "message")));
    }
}

public sealed class ResultTEqualityTests
{
    [Fact]
    public void SuccessesWithEqualValues_ShouldBeEqual()
    {
        Result<int> a = Result<int>.Success(42);
        Result<int> b = Result<int>.Success(42);

        Assert.True(a == b);
        Assert.False(a != b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void SuccessesWithDifferentValues_ShouldNotBeEqual()
    {
        Assert.True(Result<int>.Success(1) != Result<int>.Success(2));
        Assert.False(Result<string>.Success("a").Equals(Result<string>.Success("b")));
    }

    [Fact]
    public void SuccessesWithEqualReferenceValues_ShouldUseValueEquality()
    {
        Result<string> a = Result<string>.Success(new string(['h', 'i']));
        Result<string> b = Result<string>.Success("hi");

        Assert.True(a == b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void FailuresWithEqualErrors_ShouldBeEqual()
    {
        Result<int> a = Result<int>.Failure("E.Code", "message");
        Result<int> b = Result<int>.Failure("E.Code", "message");

        Assert.True(a == b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void FailuresWithDifferentErrors_ShouldNotBeEqual() =>
        Assert.True(Result<int>.Failure("a") != Result<int>.Failure("b"));

    [Fact]
    public void SuccessAndFailure_ShouldNotBeEqual() =>
        Assert.True(Result<int>.Success(0) != Result<int>.Failure("boom"));

    [Fact]
    public void DefaultResultT_ShouldEqualASuccessCarryingTheDefaultValue()
    {
        Result<int> defaulted = default;

        Assert.True(defaulted == Result<int>.Success(0));
    }

    [Fact]
    public void DefaultResultT_OverAReferenceType_ShouldNotEqualAnyRealSuccess()
    {
        Result<string> defaulted = default;

        Assert.True(defaulted != Result<string>.Success("x"));
        Assert.True(defaulted == default(Result<string>));
    }

    [Fact]
    public void Equals_Object_ShouldMatchTypedEquals()
    {
        object boxed = Result<int>.Success(42);

        Assert.True(Result<int>.Success(42).Equals(boxed));
        Assert.False(Result<int>.Success(1).Equals(boxed));
    }

    [Fact]
    public void Equals_Object_WithADifferentClosedGeneric_ShouldBeFalse()
    {
        object otherClosedType = Result<long>.Success(42L);

        Assert.False(Result<int>.Success(42).Equals(otherClosedType));
    }

    [Fact]
    public void Equals_Object_WithUnrelatedType_ShouldBeFalse()
    {
        Assert.False(Result<int>.Success(1).Equals("not a result"));
        Assert.False(Result<int>.Success(1).Equals((object?)null));
    }

    [Fact]
    public void ResultT_ShouldWorkAsADictionaryKey()
    {
        var seen = new Dictionary<Result<int>, string>
        {
            [Result<int>.Success(1)] = "one",
            [Result<int>.Failure("boom")] = "failed",
        };

        Assert.Equal("one", seen[Result<int>.Success(1)]);
        Assert.Equal("failed", seen[Result<int>.Failure("boom")]);
    }

    [Fact]
    public void Tap_ShouldReturnAnEqualResult()
    {
        Result<int> original = Result<int>.Success(42);

        Assert.True(original == original.Tap(_ => { }));
        Assert.True(original == original.TapError(_ => { }));
    }
}
