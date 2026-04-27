using Resultify.Errors;

namespace Resultify.Tests;

public sealed class EmptyErrorsListAllocationTests
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
}

public sealed class ImplicitOperatorNullGuardTests
{
    [Fact]
    public void Result_ImplicitFromNullError_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            Error? nullError = null;
            Result _ = nullError!;
        });
    }

    [Fact]
    public void ResultT_ImplicitFromNullError_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            Error? nullError = null;
            Result<int> _ = nullError!;
        });
    }
}