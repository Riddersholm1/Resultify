namespace Resultify.Tests;

/// <summary>
/// Callbacks that throw, with one member per delegate shape the <c>Try</c> overloads accept.
/// </summary>
/// <remarks>
/// A throw-only lambda body is convertible to several of those shapes at once, and overload
/// resolution does not pick the obvious one: <c>Result.Try(() =&gt; throw ...)</c> binds to
/// <c>Func&lt;Result&gt;</c>, not <c>Action</c>. Writing a cast at every call site to steer that
/// works but is easy to get wrong and reads as noise. These method groups pin the overload by their
/// declared return type instead — <c>void</c> can only become an <c>Action</c>, <c>Result</c> only a
/// <c>Func&lt;Result&gt;</c> — so each test provably exercises the overload its name claims.
/// </remarks>
internal static class Throwing
{
    internal const string Message = "boom";

    internal static void Action() => throw new InvalidOperationException(Message);

    internal static Result FuncResult() => throw new InvalidOperationException(Message);

    internal static int FuncInt() => throw new InvalidOperationException(Message);

    internal static Result<int> FuncResultOfInt() => throw new InvalidOperationException(Message);

    internal static Task FuncTask() => throw new InvalidOperationException(Message);

    internal static Task<Result> FuncTaskOfResult() => throw new InvalidOperationException(Message);

    internal static Task<int> FuncTaskOfInt() => throw new InvalidOperationException(Message);

    internal static Task<Result<int>> FuncTaskOfResultOfInt() => throw new InvalidOperationException(Message);

    internal static int FuncIntTimeout() => throw new TimeoutException("timed out");
}

/// <summary>
/// The same shapes, throwing <see cref="OperationCanceledException"/> — the one exception
/// <c>Try</c> must never convert into a failed result.
/// </summary>
internal static class Canceling
{
    internal static void Action() => throw new OperationCanceledException();

    internal static Result FuncResult() => throw new OperationCanceledException();

    internal static int FuncInt() => throw new OperationCanceledException();

    internal static Result<int> FuncResultOfInt() => throw new OperationCanceledException();

    internal static Task FuncTask() => throw new OperationCanceledException();

    internal static Task<int> FuncTaskOfInt() => throw new OperationCanceledException();

    internal static Task<Result<int>> FuncTaskOfResultOfInt() => throw new OperationCanceledException();
}
