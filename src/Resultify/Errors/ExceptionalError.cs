namespace Resultify.Errors;

/// <summary>
/// An error that wraps a caught <see cref="System.Exception"/>. Produced automatically by
/// <c>Result.Try</c>/<c>Result.TryAsync</c>; the code is <c>"Exception.{ExceptionTypeName}"</c>
/// so consumers can query by exception type without accessing the raw <see cref="Exception"/>.
/// </summary>
public sealed record ExceptionalError : Error
{
    /// <summary>The underlying exception that caused this error.</summary>
    public Exception Exception { get; }

    /// <summary>
    /// Wrap an exception as an error. The code is set to <c>"Exception.{exception.GetType().Name}"</c>
    /// and the message is copied from <see cref="Exception.Message"/>.
    /// </summary>
    /// <param name="exception">The exception to wrap. Must not be null.</param>
    public ExceptionalError(Exception exception)
        : base(BuildCode(ValidateException(exception)), exception.Message)
    {
        Exception = exception;
    }

    /// <summary>
    /// Wrap an exception as an error with a custom message. The code is set to
    /// <c>"Exception.{exception.GetType().Name}"</c>.
    /// </summary>
    /// <param name="message">A human-readable description of the error.</param>
    /// <param name="exception">The exception to wrap. Must not be null.</param>
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
}