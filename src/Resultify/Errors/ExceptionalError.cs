using System.Runtime.CompilerServices;

namespace Resultify.Errors;

/// <summary>
/// An error that wraps a caught <see cref="System.Exception"/>.
/// The code is set to <c>"Exception.{ExceptionTypeName}"</c>, making it queryable by exception type.
/// Produced automatically by <c>Result.Try</c> / <c>Result.TryAsync</c>.
/// </summary>
public sealed record ExceptionalError : Error
{
    /// <summary>The underlying exception that caused this error.</summary>
    public Exception Exception { get; }

    /// <summary>
    /// Wraps an exception as an error. The code is set to <c>"Exception.{exception.GetType().Name}"</c>
    /// and the message is copied from <see cref="Exception.Message"/>.
    /// </summary>
    /// <param name="exception">The exception to wrap. Must not be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
    public ExceptionalError(Exception exception)
        : base(BuildCode(ValidateException(exception)), exception.Message ?? string.Empty)
    {
        Exception = exception;
    }

    /// <summary>
    /// Wraps an exception as an error with a custom message.
    /// The code is set to <c>"Exception.{exception.GetType().Name}"</c>.
    /// </summary>
    /// <param name="message">A human-readable description of the error.</param>
    /// <param name="exception">The exception to wrap. Must not be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
    public ExceptionalError(string message, Exception exception)
        : base(BuildCode(ValidateException(exception)), message)
    {
        Exception = exception;
    }

    private static Exception ValidateException(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return exception;
    }

    private static string BuildCode(Exception exception) =>
        $"Exception.{exception.GetType().Name}";

    /// <summary>
    /// Two <see cref="ExceptionalError"/>s are equal only when the base <see cref="Error"/> parts match,
    /// and they wrap the exact same exception instance.
    /// </summary>
    public bool Equals(ExceptionalError? other) =>
        base.Equals(other) && ReferenceEquals(Exception, other.Exception);

    /// <inheritdoc />
    public override int GetHashCode() =>
        HashCode.Combine(base.GetHashCode(), RuntimeHelpers.GetHashCode(Exception));
}