using Resultify.Errors;

namespace Resultify.Tests;

/// <summary>
/// The five built-in error subtypes. Each is a <c>record</c> deriving from <see cref="Error"/>, so
/// beyond its own code and payload it must keep working as an <see cref="Error"/>: value equality
/// that distinguishes it from a plain error with the same code, and the inherited log format.
/// </summary>
public sealed class ValidationErrorTests
{
    [Fact]
    public void Ctor_Message_ShouldUseTheDefaultCodeAndNoProperty()
    {
        var error = new ValidationError("Something is invalid.");

        Assert.Equal("Validation.Invalid", error.Code);
        Assert.Equal("Something is invalid.", error.Message);
        Assert.Null(error.PropertyName);
    }

    [Fact]
    public void Ctor_PropertyAndMessage_ShouldScopeTheCodeToTheProperty()
    {
        var error = new ValidationError("Email", "Email is required");

        Assert.Equal("Email", error.PropertyName);
        Assert.Equal("Validation.Email", error.Code);
        Assert.Equal("Email is required", error.Message);
    }

    [Fact]
    public void ForProperty_ShouldSetPropertyNameCodeAndDefaultMessage()
    {
        ValidationError error = ValidationError.ForProperty("Email");

        Assert.Equal("Email", error.PropertyName);
        Assert.Equal("Validation.Email", error.Code);
        Assert.Contains("Email", error.Message);
    }

    [Fact]
    public void ValidationErrors_DifferingOnlyByProperty_ShouldNotBeEqual() =>
        Assert.NotEqual(new ValidationError("Email", "required"), new ValidationError("Name", "required"));

    [Fact]
    public void ValidationError_ShouldNotEqualAPlainErrorWithMatchingCodeAndMessage()
    {
        var validation = new ValidationError("Email", "Required");
        var plain = new Error(validation.Code, validation.Message);

        Assert.NotEqual<Error>(validation, plain);
        Assert.NotEqual<Error>(plain, validation);
    }
}

public sealed class NotFoundErrorTests
{
    [Fact]
    public void Ctor_Message_ShouldUseTheDefaultCode()
    {
        var error = new NotFoundError("User was not found.");

        Assert.Equal("NotFound", error.Code);
        Assert.Equal("User was not found.", error.Message);
        Assert.Null(error.EntityName);
        Assert.Null(error.EntityId);
    }

    [Fact]
    public void Ctor_EntityAndId_ShouldFormatCodeMessageAndPayload()
    {
        var error = new NotFoundError("Customer", 42);

        Assert.Equal("Customer.NotFound", error.Code);
        Assert.Equal("Customer with id '42' was not found.", error.Message);
        Assert.Equal("Customer", error.EntityName);
        Assert.Equal(42, error.EntityId);
    }

    [Fact]
    public void Ctor_EntityAndId_WithStringId_ShouldFormatCorrectly()
    {
        var error = new NotFoundError("Order", "ORD-999");

        Assert.Equal("Order.NotFound", error.Code);
        Assert.Equal("Order with id 'ORD-999' was not found.", error.Message);
        Assert.Equal("ORD-999", error.EntityId);
    }

    [Fact]
    public void NotFoundError_IsAnError() =>
        Assert.IsAssignableFrom<Error>(new NotFoundError("Not found."));

    [Fact]
    public void NotFoundErrors_WithSameValues_ShouldBeEqual() =>
        Assert.Equal<Error>(new NotFoundError("Customer", 42), new NotFoundError("Customer", 42));

    [Fact]
    public void NotFoundErrors_WithDifferentEntityNames_ShouldNotBeEqual() =>
        Assert.NotEqual<Error>(new NotFoundError("Customer", 42), new NotFoundError("Order", 42));

    [Fact]
    public void NotFoundErrors_WithDifferentIds_ShouldNotBeEqual() =>
        Assert.NotEqual<Error>(new NotFoundError("Customer", 1), new NotFoundError("Customer", 2));

    [Fact]
    public void NotFoundError_ShouldNotEqualAPlainErrorWithMatchingCodeAndMessage()
    {
        var notFound = new NotFoundError("Customer", 42);
        var plain = new Error(notFound.Code, notFound.Message);

        Assert.NotEqual<Error>(notFound, plain);
        Assert.NotEqual<Error>(plain, notFound);
    }
}

public sealed class ConflictErrorTests
{
    [Fact]
    public void Ctor_Message_ShouldUseTheDefaultCode()
    {
        var error = new ConflictError("Duplicate resource.");

        Assert.Equal("Conflict", error.Code);
        Assert.Equal("Duplicate resource.", error.Message);
    }

    [Fact]
    public void Ctor_CodeAndMessage_ShouldUseTheExplicitCode()
    {
        var error = new ConflictError("Order.DuplicateId", "Order already exists.");

        Assert.Equal("Order.DuplicateId", error.Code);
        Assert.Equal("Order already exists.", error.Message);
    }

    [Fact]
    public void ConflictError_IsAnError() =>
        Assert.IsAssignableFrom<Error>(new ConflictError("conflict"));

    [Fact]
    public void ConflictErrors_WithSameValues_ShouldBeEqual() =>
        Assert.Equal<Error>(new ConflictError("X", "msg"), new ConflictError("X", "msg"));

    [Fact]
    public void ConflictError_ShouldNotEqualAForbiddenErrorWithMatchingCodeAndMessage() =>
        Assert.NotEqual<Error>(new ConflictError("X", "msg"), new ForbiddenError("X", "msg"));
}

public sealed class ForbiddenErrorTests
{
    [Fact]
    public void Ctor_Message_ShouldUseTheDefaultCode()
    {
        var error = new ForbiddenError("Access denied.");

        Assert.Equal("Forbidden", error.Code);
        Assert.Equal("Access denied.", error.Message);
    }

    [Fact]
    public void Ctor_CodeAndMessage_ShouldUseTheExplicitCode()
    {
        var error = new ForbiddenError("User.Forbidden", "You cannot do that.");

        Assert.Equal("User.Forbidden", error.Code);
        Assert.Equal("You cannot do that.", error.Message);
    }

    [Fact]
    public void ForbiddenError_IsAnError() =>
        Assert.IsAssignableFrom<Error>(new ForbiddenError("forbidden"));

    [Fact]
    public void ForbiddenErrors_WithSameValues_ShouldBeEqual() =>
        Assert.Equal<Error>(new ForbiddenError("F", "msg"), new ForbiddenError("F", "msg"));
}

public sealed class ExceptionalErrorTests
{
    [Fact]
    public void Ctor_Exception_ShouldDeriveCodeAndMessageFromIt()
    {
        var exception = new InvalidOperationException("inner message");

        var error = new ExceptionalError(exception);

        Assert.Equal("Exception.InvalidOperationException", error.Code);
        Assert.Equal("inner message", error.Message);
        Assert.Same(exception, error.Exception);
    }

    [Fact]
    public void Ctor_MessageAndException_ShouldUseTheProvidedMessage()
    {
        var exception = new InvalidOperationException("inner message");

        var error = new ExceptionalError("custom message", exception);

        Assert.Equal("custom message", error.Message);
        Assert.Equal("Exception.InvalidOperationException", error.Code);
        Assert.Same(exception, error.Exception);
    }

    [Fact]
    public void Ctor_ShouldUseTheRuntimeExceptionTypeForTheCode()
    {
        Exception exception = new TimeoutException("timed out");

        Assert.Equal("Exception.TimeoutException", new ExceptionalError(exception).Code);
    }

    [Fact]
    public void ExceptionalErrors_WrappingTheSameInstance_ShouldBeEqual()
    {
        var exception = new InvalidOperationException("same");
        var a = new ExceptionalError(exception);
        var b = new ExceptionalError(exception);

        Assert.Equal<Error>(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void ExceptionalErrors_WrappingDifferentInstances_ShouldNotBeEqual()
    {
        var a = new ExceptionalError(new InvalidOperationException("same"));
        var b = new ExceptionalError(new InvalidOperationException("same"));

        Assert.NotEqual<Error>(a, b);
    }

    [Fact]
    public void ExceptionalError_ShouldNotEqualNull() =>
        Assert.False(new ExceptionalError(new InvalidOperationException("x")).Equals(null));
}

/// <summary>
/// <see cref="Exception.Message"/> is declared non-nullable but a custom exception can still
/// override it to return null, which would otherwise take down the <see cref="Error"/> constructor's
/// null guard at the worst possible moment — while handling another failure.
/// </summary>
public sealed class ExceptionalErrorNullMessageTests
{
    private sealed class NullMessageException : Exception
    {
        public override string Message => null!;
    }

    [Fact]
    public void ExceptionalError_WithNullMessageException_ShouldCoerceToEmpty()
    {
        var ex = new NullMessageException();

        var error = new ExceptionalError(ex);

        Assert.Equal(string.Empty, error.Message);
        Assert.Equal($"Exception.{nameof(NullMessageException)}", error.Code);
        Assert.Same(ex, error.Exception);
    }

    [Fact]
    public void Try_WhenThrowingNullMessageException_ShouldNotThrow()
    {
        Result result = Result.Try((Action)(() => throw new NullMessageException()));

        Assert.True(result.IsFailure);
        Assert.IsType<ExceptionalError>(result.FirstError);
        Assert.Equal(string.Empty, result.FirstError.Message);
    }
}
