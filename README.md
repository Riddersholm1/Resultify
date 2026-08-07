# Resultify

[![Build](https://github.com/Riddersholm1/Resultify/actions/workflows/pipeline.yml/badge.svg)](https://github.com/Riddersholm1/Resultify/actions)
[![Coverage](https://codecov.io/gh/Riddersholm1/Resultify/branch/main/graph/badge.svg)](https://codecov.io/gh/Riddersholm1/Resultify)
[![Latest Release](https://img.shields.io/github/v/release/Riddersholm1/Resultify?include_prereleases)](https://github.com/Riddersholm1/Resultify/releases)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Riddersholm.Resultify)](https://www.nuget.org/packages/Riddersholm.Resultify)
[![Stars](https://img.shields.io/github/stars/Riddersholm1/Resultify)](https://github.com/Riddersholm1/Resultify/stargazers)
[![Contributors](https://img.shields.io/github/contributors/Riddersholm1/Resultify)](https://github.com/Riddersholm1/Resultify/graphs/contributors)
[![Last Commit](https://img.shields.io/github/last-commit/Riddersholm1/Resultify)](https://github.com/Riddersholm1/Resultify/commits/main)
[![Commit Activity](https://img.shields.io/github/commit-activity/m/Riddersholm1/Resultify)](https://github.com/Riddersholm1/Resultify/graphs/commit-activity)
[![Issues](https://img.shields.io/github/issues/Riddersholm1/Resultify)](https://github.com/Riddersholm1/Resultify/issues)
[![Release Strategy](https://img.shields.io/badge/release%20strategy-githubflow-orange)](https://githubflow.github.io)

A modern, **immutable** Result pattern library for **.NET 10**.

- Zero-allocation success path — `Result` and `Result<TValue>` are `readonly struct`
- Immutable and thread-safe — share instances freely across threads
- Errors with machine-readable codes, structured metadata, and causal chains
- Functional combinators — `Map`, `Bind`, `Ensure`, `Match`, `Switch`, `Tap`, `TapError`
- First-class async — every combinator has an async overload and fluent `Task<Result<T>>` extensions
- Multi-error support for validation aggregation
- AOT and trimming compatible
- Zero runtime dependencies
- .NET 10 / C# 14
- MIT licensed

## Installation

```sh
dotnet add package Riddersholm.Resultify
```

## Quick start

```csharp
using Resultify;
using Resultify.Errors;

// Success / failure
Result<Customer> ok   = Result<Customer>.Success(customer);
Result<Customer> bad  = Result<Customer>.Failure("Customer.NotFound", "Customer does not exist");

// Null-safe creation — non-null becomes Success, null becomes Failure(Error.NullValue)
Result<Customer> r1 = customer;                          // implicit conversion
Result<Customer> r2 = Result<Customer>.Create(customer); // explicit

// Conditional factories
Result a = Result.SuccessIf(age >= 18, "Must be at least 18");
Result b = Result.FailureIf(string.IsNullOrEmpty(name), "Name is required");
```

## Errors

Errors are immutable `record` types with a machine-readable `Code` and a
human-readable `Message`. Codes are useful for i18n, structured logs, and API responses.

```csharp
// Full form
var error = new Error("Payment.InsufficientFunds", "Insufficient funds for the transaction");

// Convenience — message only, empty code
var error = new Error("Something went wrong");

// Well-known sentinels
Error.None        // ("", "")
Error.NullValue   // ("General.NullValue", ...)
Error.Unknown     // ("General.Unknown",  ...)
```

### Metadata and causes

```csharp
var error = new Error("Payment.Failed", "Payment gateway rejected the transaction")
    .WithMetadata("TransactionId", txId)
    .WithMetadata("Amount", amount)
    .CausedBy(new Error("Gateway.Timeout", "Gateway did not respond"))
    .CausedBy(httpRequestException);
```

### Domain error registries (DDD pattern)

Group your domain errors as `static readonly` fields:

```csharp
public static class CustomerErrors
{
    public static readonly Error NotFound = new(
        "Customer.NotFound", "The specified customer was not found.");

    public static readonly Error EmailAlreadyInUse = new(
        "Customer.EmailAlreadyInUse", "This email is already registered.");

    public static Error InvalidAge(int age) => new(
        "Customer.InvalidAge", $"Age {age} is outside the valid range.");
}

if (existing is not null)
    return Result<Customer>.Failure(CustomerErrors.EmailAlreadyInUse);
```

### Built-in error types

Five subtypes ship with sensible default codes:

| Type | Default Code | Use case |
|------|-------------|----------|
| `ValidationError` | `Validation.Invalid` or `Validation.{Property}` | Input / business-rule validation |
| `NotFoundError` | `NotFound` or `{Entity}.NotFound` | Lookup misses (HTTP 404) |
| `ConflictError` | `Conflict` | Concurrency / duplicate conflicts (HTTP 409) |
| `ForbiddenError` | `Forbidden` | Insufficient permissions (HTTP 403) |
| `ExceptionalError` | `Exception.{TypeName}` | Wrapped exceptions from `Try` / `TryAsync` |

### Custom error types

```csharp
public sealed record InsufficientFundsError : Error
{
    public decimal Required { get; }
    public decimal Available { get; }

    public InsufficientFundsError(decimal required, decimal available)
        : base("Payment.InsufficientFunds",
               $"Insufficient funds: required {required:C}, available {available:C}")
    {
        Required = required;
        Available = available;
    }
}
```

## Combinators

### Map — transform the value

```csharp
Result<string> name = Result<Customer>.Success(customer).Map(c => c.FullName);
```

A `null` mapped value becomes a failure with `Error.NullValue`.

### Bind — chain dependent operations

```csharp
Result<Order> result = GetCustomer(id)
    .Bind(customer => CreateOrder(customer))
    .Bind(order    => ValidateOrder(order));
```

### Ensure — add validation gates

```csharp
Result<int> result = Result<int>.Success(age)
    .Ensure(a => a >= 0,   "Age cannot be negative")
    .Ensure(a => a <= 150, "Age seems unrealistic");
```

### Match — collapse into a value

```csharp
IActionResult response = result.Match(
    onSuccess: value  => Ok(value),
    onFailure: errors => BadRequest(errors[0].Message));
```

### Switch — side effects based on outcome

Like `Match` but returns `void` (or `Task` for the async overload).

```csharp
result.Switch(
    onSuccess: value  => logger.LogInformation("Got {Value}", value),
    onFailure: errors => logger.LogWarning("Failed: {Errors}", errors));
```

### Tap — side effect on success

```csharp
var result = GetCustomer(id)
    .Tap(c  => logger.LogInformation("Found customer {Id}", c.Id))
    .Bind(c => CreateOrder(c));
```

### TapError — side effect on failure

```csharp
var result = GetCustomer(id)
    .TapError(errors => logger.LogWarning("Lookup failed: {Errors}", errors));
```

## Async pipelines

Nearly every combinator is also an extension method on `Task<Result<T>>` and
`Task<Result>`, so you can chain sync and async steps fluently without intermediate
`await`s:

```csharp
Result<OrderId> result = await GetCustomerAsync(id)
    .Ensure(c        => c.IsActive, CustomerErrors.Inactive)
    .Map(c           => c.Email)
    .BindAsync(email => CreateOrderAsync(email))
    .Tap(order       => logger.LogInformation("Order {Id} created", order.Id))
    .Map(order       => order.Id);
```

Each helper awaits the antecedent task and delegates to the instance method of the
same name, always with `ConfigureAwait(false)`, so behaviour matches the synchronous
form exactly. Two gaps are worth knowing:

- **Three instance members have no task-based counterpart**: `Ensure(predicate, string)`
  on `Task<Result>`, `ToResult<T>(value)` on `Task<Result>`, and `ToResult()` on
  `Task<Result<T>>`. Await first: `(await pipeline).ToResult()`.
- **On a `Task<Result<T>>` receiver, members that need an explicit type argument can't
  be called fluently.** The extension block contributes its own type parameter and C#
  won't infer only some of them, so `pipeline.HasError<ValidationError>()` fails to
  compile (CS1929). Either await first — `(await pipeline).HasError<ValidationError>()` —
  or let inference do the work by typing the lambda:
  `pipeline.HasError((ValidationError e) => e.PropertyName == "Email")`.
  `HasErrorCode` takes no type argument and works everywhere, as do all of these on a
  `Task<Result>` receiver.

Null arguments are rejected with `ArgumentNullException` throughout. On `async` members
that surfaces as a faulted task rather than a synchronous throw — same exception, same
parameter name, observed as soon as you await.

## Try — catch exceptions as errors

```csharp
Result        a = Result.Try(() => riskyOperation());
Result<int>   b = Result<int>.Try(() => int.Parse(input));

// Async
Result<int>   c = await Result<int>.TryAsync(() => httpClient.GetFromJsonAsync<int>(url));

// Custom exception handler
Result<Data>  d = Result<Data>.Try(
    () => LoadData(),
    ex => new Error("DataLoad.Failed", "Data load failed").CausedBy(ex));
```

`OperationCanceledException` and `TaskCanceledException` are **never** caught —
they propagate as-is in both sync and async variants. This means `Try` is safe
to use inside `using var cts = new CancellationTokenSource()` patterns:
cancellation always reaches your outer handler instead of being silently
converted into a failure result.

`Result<T>.Try` and `Result<T>.TryAsync` route a `null` return value through
`Create(...)`, so it becomes a failure with `Error.NullValue` rather than an
`Exception.ArgumentNullException`:

```csharp
Result<string> result = Result<string>.Try(() => LookupName(id)); // may return null
// If LookupName returns null: result.IsFailure && result.FirstError == Error.NullValue
```

Wrapped exceptions get a stable code like `Exception.InvalidOperationException`,
so you can query by exception type:

```csharp
if (result.HasException<TimeoutException>()) { /* retry */ }
```

## Merge

Combine multiple results:

```csharp
// Non-generic — params ReadOnlySpan, combines all errors
Result merged = Result.Merge(result1, result2, result3);

// Extension on IEnumerable<Result>
Result merged = results.Merge();

// Extension on IEnumerable<Result<<T>> — either all values or all errors
Result<IReadOnlyList<int>> merged = new[]
{
    Result<int>.Success(1),
    Result<int>.Success(2),
    Result<int>.Success(3)
}.Merge();
// merged.Value == [1, 2, 3]
```

When any element fails, the typed `Merge` discards collected values and returns
a failure aggregating every observed error in input order.

## Error querying

```csharp

if (result.HasError<ValidationError>())                               // by type
if (result.HasError<ValidationError>(e => e.PropertyName == "Email")) // by type + predicate
if (result.HasErrorCode("Customer.NotFound"))                         // by code
if (result.HasException<TimeoutException>())                          // wrapped exception
```

## Deconstruction

```csharp
var (isSuccess, errors)        = Result.Failure("err");
var (isSuccess, value, errors) = Result<int>.Success(42);
```

## Implicit conversions

```csharp
Result<string> r = "hello";           // Success (via Create)
Result<string> r = (string?)null;     // Failure with Error.NullValue
Result         r = new Error("fail"); // Failure
Result<int>    r = new Error("fail"); // Failure
```

`Result<T>` declares conversions from both `T` and `Error`, which overlap for some
type arguments:

```csharp
Result<object> r = new Error("fail"); // Failure — the Error conversion is more specific and wins
Result<Error>  r = new Error("fail"); // Does not compile: CS0457, ambiguous user defined conversions
```

For `Result<Error>` — and for storing an error *as a value* in a `Result<object>` —
use the explicit factories: `Result<Error>.Success(error)`, `Result<Error>.Failure(error)`.

## Equality

`Result` and `Result<T>` implement `IEquatable<T>` and the `==` / `!=` operators, so
they compare by value and work as dictionary keys:

- All successes of `Result` are equal to each other.
- Two `Result<T>` successes are equal when their values are (via `EqualityComparer<T>.Default`).
- Two failures are equal when their error sequences are equal, in order.

`Error` is a record with value equality that also accounts for `Metadata` and `Causes`.
Equality is type-sensitive: a `NotFoundError` never equals a plain `Error` with the same
code and message. `ExceptionalError` additionally requires the *same exception instance*,
since two exceptions with equal messages are not interchangeable.

## Logging

`Error.ToString()` renders as `[Code] Message`, with ` (caused by: …)` appended when the
error has causes. It is **sealed**, so every error — the built-in subtypes and your own —
renders the same way; without that, each derived `record` would substitute the compiler's
generated format and dump `Metadata`, `Causes` and entire exception stack traces into your
logs. Carry extra detail in `Metadata` rather than by overriding `ToString`.

```csharp
Result.Failure(new NotFoundError("Customer", 42)).ToString();
// Result: Failure ([Customer.NotFound] Customer with id '42' was not found.)
```

## Thread safety

`Result`, `Result<T>`, `Error`, and all built-in error subtypes are **immutable**.
Instances can be shared across threads without any synchronization:

- `Result` / `Result<T>` are `readonly struct` — fields cannot change after construction.
- `Error.Code`, `Error.Message`, `Error.Metadata`, and `Error.Causes` are init-only.
  `WithMetadata` / `CausedBy` return *new* instances rather than mutating.
- The `Errors` list is a defensive copy exposed through a read-only view, so neither
  mutating the collection you passed to `Failure(...)` nor casting `.Errors` back to
  `Error[]` can change a result after construction. Since results are structs, every
  copy shares that one list — which is exactly why it must be unwritable.
- Successful results share a single empty `IReadOnlyList<Error>` instance — reading
  `.Errors` on a success never allocates and is safe under concurrent readers.

The library declares no static, mutable state and no global configuration.

## Clean Architecture / CQS integration

```csharp
public sealed class CreateOrderHandler
{
    public async Task<Result<OrderId>> Handle(CreateOrderCommand command, CancellationToken ct)
    {
        return await ValidateCommand(command)
            .BindAsync(cmd      => FindCustomer(cmd.CustomerId, ct))
            .Ensure   (customer => customer.IsActive, CustomerErrors.Inactive)
            .BindAsync(customer => CreateOrder(customer, command, ct))
            .Tap      (order    => logger.LogInformation("Order {Id} created", order.Id))
            .Map      (order    => order.Id);
    }
}
```

## FAQ

**Why structs instead of classes?**
A class-based result allocates on every call. Structs make the success path
zero-alloc, which matters in tight loops and high-throughput handlers.

**What happens with `default(Result<string>)`?**
Because `Result<T>` is a `readonly struct`, the runtime can produce `default`
instances that no factory validated — an uninitialised field, an array element, a
`default` switch arm. Such a value carries no errors, so it reports
`IsSuccess = true`, and what happens next depends on `T`:

- For a value type, `default(Result<int>)` is a success carrying `0` — a legitimate
  value, and it flows through every combinator normally.
- For a reference type, `default(Result<string>)` has a null value. The factories
  guarantee a success is never null, so rather than hand you a null the library fails
  loudly: `.Value` throws `InvalidOperationException`, and so does any combinator that
  passes the value to your callback (`Map`, `Bind`, `Tap`, `Ensure`, `Match`, `Switch`,
  and `Merge` over a sequence containing one).

Prefer the factory methods (`Success`, `Failure`, `Create`) and never hand out
`default`. On the read side, `TryGetValue(out T value)` and `ValueOrDefault` are total
— neither throws for any state, including `default`.

**Why no `IResult` interface?**
An interface would box the struct, defeating the zero-allocation goal.
Pattern-match or use generics constrained to the concrete types instead.

**Can I use this with FluentValidation?**
Yes. Collect `ValidationFailure` results into `ValidationError` instances
and pass them to `Result.Failure(errors)`.

## License

[MIT](LICENSE) © 2026 Jesper Bruhn Riddersholm
