using Resultify.Errors;

namespace Resultify.Tests;

public sealed class ToStringTests
{
    [Fact]
    public void Result_Success_ToString_ShouldBeReadable()
    {
        Assert.Equal("Result: Success", Result.Success().ToString());
    }

    [Fact]
    public void Result_Failure_ToString_ShouldContainErrorText()
    {
        var text = Result.Failure("code", "bad").ToString();

        Assert.Contains("Failure", text);
        Assert.Contains("code", text);
        Assert.Contains("bad", text);
    }

    [Fact]
    public void ResultT_Success_ToString_ShouldContainValue()
    {
        var text = Result<int>.Success(42).ToString();

        Assert.Contains("Success", text);
        Assert.Contains("42", text);
        Assert.Contains("Int32", text);
    }

    [Fact]
    public void ResultT_DefaultForReferenceType_ToString_ShouldNotThrow()
    {
        Result<string> result = default;

        var text = result.ToString();

        Assert.Contains("Success", text);
        Assert.Contains("null", text);
    }

    [Fact]
    public void ResultT_Failure_ToString_ShouldContainErrors()
    {
        var text = Result<int>.Failure("boom").ToString();

        Assert.Contains("Failure", text);
        Assert.Contains("boom", text);
    }

    [Fact]
    public void Error_ToString_WithCausesAndCode_ShouldIncludeAll()
    {
        Error err = new Error("Top.Code", "top").CausedBy(new Error("root"));

        var text = err.ToString();

        Assert.Contains("Top.Code", text);
        Assert.Contains("top", text);
        Assert.Contains("root", text);
        Assert.Contains("caused by", text);
    }
}