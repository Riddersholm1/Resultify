namespace Resultify.Errors;

/// <summary>
/// An error that wraps a caught <see cref="System.Exception"/>.
/// </summary>
public sealed record ExceptionalError : Error
{
    /// <summary>The underlying exception.</summary>
    public Exception Exception { get; }

    public ExceptionalError(Exception exception)
        : base($"Exception.{exception?.GetType().Name ?? "Unknown"}", exception?.Message ?? string.Empty)
    {
        ArgumentNullException.ThrowIfNull(exception);
        Exception = exception;
    }

    public ExceptionalError(string message, Exception exception)
        : base($"Exception.{exception?.GetType().Name ?? "Unknown"}", message)
    {
        ArgumentNullException.ThrowIfNull(exception);
        Exception = exception;
    }
}
