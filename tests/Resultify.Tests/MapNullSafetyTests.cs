using Resultify.Errors;

namespace Resultify.Tests;

public sealed class MapNullSafetyTests
{
    [Fact]
    public void Map_WhenMapperReturnsNull_ShouldFailWithNullValue()
    {
        Result<string> result = Result<int>.Success(1).Map<string>(_ => null!);

        Assert.True(result.IsFailure);
        Assert.Equal(Error.NullValue, result.FirstError);
    }

    [Fact]
    public async Task MapAsync_WhenMapperReturnsNull_ShouldFailWithNullValue()
    {
        Result<string> result = await Result<int>.Success(1)
            .MapAsync(_ => Task.FromResult<string>(null!));

        Assert.True(result.IsFailure);
        Assert.Equal(Error.NullValue, result.FirstError);
    }

    [Fact]
    public void ToResult_Converter_WhenConverterReturnsNull_ShouldFailWithNullValue()
    {
        Result<string> result = Result<int>.Success(1).ToResult<string>(_ => null!);

        Assert.True(result.IsFailure);
        Assert.Equal(Error.NullValue, result.FirstError);
    }

    [Fact]
    public void Map_WithValueType_ShouldStillSucceed()
    {
        Result<int> result = Result<int>.Success(5).Map(v => v * 2);

        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Value);
    }
}

public sealed class ToResultConverterTests
{
    [Fact]
    public void ToResult_Converter_WhenSuccess_ShouldTransformValue()
    {
        Result<string> converted = Result<int>.Success(7).ToResult(v => $"v={v}");

        Assert.True(converted.IsSuccess);
        Assert.Equal("v=7", converted.Value);
    }

    [Fact]
    public void ToResult_Converter_WhenFailure_ShouldPropagateErrors()
    {
        Result<string> converted = Result<int>.Failure("earlier").ToResult(v => $"v={v}");

        Assert.True(converted.IsFailure);
        Assert.Equal("earlier", converted.FirstError.Message);
    }
}