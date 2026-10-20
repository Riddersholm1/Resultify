# Security policy

## Supported versions

| Version | Supported |
|---|---|
| 2.x | ✅ |
| 1.x | ❌ — upgrade to 2.x; see [Upgrading from 1.x](README.md#upgrading-from-1x) |

Fixes ship as a patch release of the latest version.

## Reporting a vulnerability

Please report security issues privately through
[GitHub's private vulnerability reporting](https://github.com/Riddersholm1/Resultify/security/advisories/new)
rather than by opening a public issue.

You can expect an acknowledgement within a few days and an assessment shortly after. If the report is
accepted, you will be credited in the advisory unless you would rather not be.

## What counts as a vulnerability here

Resultify is a value-type library with no network, file, or process access, so the realistic surface
is narrower than for most packages. Applications decide what to do — grant access, commit a payment,
return a 404 — by asking a result whether it succeeded, so reports in these areas are especially
welcome:

- **A result that reports the wrong outcome.** Any way, through the public API, to get a result that
  was built as a failure to report `IsSuccess`, or the reverse. The documented exception is
  `default(Result<T>)`, covered in the README's FAQ.
- **Mutation after construction.** Any way to change a `Result`, `Result<T>` or `Error` after it was
  created. Results are structs, so every copy shares the same error list; 2.0.0 closed one such path,
  casting `Errors` back to `Error[]` and writing through it.
- **More in log output than documented.** `Error.ToString()` renders the code, the message and the
  causes, and nothing else; it is sealed so that metadata and exception stack traces stay out of logs.
  A path that makes it, or `Result.ToString()`, include metadata values, stack traces or other state is
  an information-disclosure issue.
- **Crashes or runaway cost from ordinary input.** A stack overflow, or unbounded memory or CPU, in
  equality, hashing, `ToString()` or `Merge` for inputs of ordinary size.

## What does not count

- `InvalidOperationException` from `.Value` on a failure, or on `default(Result<T>)` for a reference
  type. That is the documented behaviour: the library fails loudly rather than hand you a null.
- Exceptions thrown by your own callbacks propagating out of `Map`, `Bind`, `Ensure`, `Tap` and the
  rest. Only `Try` and `TryAsync` turn exceptions into failures.
- `OperationCanceledException` passing straight through `Try` and `TryAsync`. That is deliberate, so
  cancellation always reaches your handler.
- Text you put in an error's `Message`, or the message of an exception wrapped by `Try`, appearing in
  `ToString()`. That is what `ToString()` is documented to contain, and it is meant for logs, not for
  end users.
