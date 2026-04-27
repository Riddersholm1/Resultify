# Resultify

A modern, **immutable** Result pattern library for **.NET 10**.

- Zero-allocation success path — `Result` and `Result<TValue>` are `readonly struct`
- Immutable and thread-safe — all types can be freely shared across threads
- Errors with machine-readable codes, structured metadata, and causal chains
- Functional combinators — `Map`, `Bind`, `Match`, `Tap`, `Ensure`, `Switch`
- First-class async — every combinator has an async overload and fluent `Task<Result<T>>` extensions
- Multi-error support for validation aggregation
- AOT and trimming compatible
- Zero runtime dependencies
- .NET 10 / C# 14
- MIT licensed

## Installation

```sh
dotnet add package Resultify
```

## Quick start

```csharp
using Resultify;
using Resultify.Errors;

// Success
Result<Customer> result = Result<Customer>.Success(customer);

// Failure with a code + message
Result<Customer> result = Result<Customer>.Failure("Customer.NotFound", "Customer does not exist");

// Null-safe creation — non-null becomes Success, null becomes Failure(Error.NullValue)
Result<Customer> result = customer;                         // implicit conversion
Result<Customer> result = Result<Customer>.Create(customer); // explicit

// Conditional factories
Result result = Result.SuccessIf(age >= 18, "Must be at least 18");
Result result = Result.FailureIf(string.IsNullOrEmpty(name), "Name is required");
```

## Errors

Errors are immutable `record` types with a machine-readable `Code` and a
human-readable `Message`. The code is useful for i18n, structured logs, and API responses.

```csharp
// Full form
var error = new Error("Payment.InsufficientFunds", "Insufficient funds for the transaction");

// Convenience — message only, empty code
var error = new Error("Something went wrong");

// Well-known sentinels
Error.None        // ("", "")
Error.NullValue   // ("General.NullValue", "...")
Error.Unknown     // ("General.Unknown", "...")
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

// Usage
if (existing is not null)
    return Result<Customer>.Failure(CustomerErrors.EmailAlreadyInUse);
```

### Built-in error types

The library ships with five subtypes that set sensible codes out of the box:

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

### Bind — chain dependent operations

```csharp
Result<Order> result = GetCustomer(id)
    .Bind(customer => CreateOrder(customer))
    .Bind(order => ValidateOrder(order));
```

### Ensure — add validation gates

```csharp
Result<int> result = Result<int>.Success(age)
    .Ensure(a => a >= 0, "Age cannot be negative")
    .Ensure(a => a <= 150, "Age seems unrealistic");
```

### Match — collapse into a value

```csharp
IActionResult response = result.Match(
    onSuccess: value => Ok(value),
    onFailure: errors => BadRequest(errors[0].Message));
```

### Switch — side effects based on outcome

Like `Match` but returns `void` (or `Task` for the async overload).

```csharp
result.Switch(
    onSuccess: value => logger.LogInformation("Got {Value}", value),
    onFailure: errors => logger.LogWarning("Failed: {Errors}", errors));
```

### Tap

```csharp
var result = GetCustomer(id)
    .Tap(c => logger.LogInformation("Found customer {Id}", c.Id))
    .Bind(c => CreateOrder(c));
```

### TapError — side effects on failure

```csharp
var result = GetCustomer(id)
    .TapError(errors => logger.LogWarning("Lookup failed: {Errors}", errors));
```

## Async pipelines

Every combinator works seamlessly with `Task<Result<T>>`:

```csharp
Result<OrderConfirmation> result = await GetCustomerAsync(id)
    .Map(c => c.Email)
    .BindAsync(email => ValidateEmailAsync(email))
    .BindAsync(email => CreateOrderAsync(email))
    .Ensure(order => order.Total > 0, "Order total must be positive");
```

## Try — catch exceptions as errors

```csharp
Result result = Result.Try(() => riskyOperation());

Result<int> result = Result<int>.Try(() => int.Parse(input));

// With custom exception handler
Result<Data> result = Result<Data>.Try(
    () => LoadData(),
    ex => new Error("DataLoad.Failed", "Data load failed").CausedBy(ex));

// Async
Result<int> result = await Result<int>.TryAsync(
    () => httpClient.GetFromJsonAsync<int>(url));
```

`OperationCanceledException` and `TaskCanceledException` are never caught —
they propagate as-is in **both sync and async** variants:

```csharp
// Sync — OperationCanceledException flows through Try unchanged
Result.Try(() => throw new OperationCanceledException()); // throws — does NOT become a Result

// Async — same behaviour
await Result.TryAsync(() => Task.FromCanceled(token));    // throws TaskCanceledException
```

This means `Try` is safe to use inside `using var cts = new CancellationTokenSource()`
patterns: cancellation always reaches your outer handler instead of being silently
converted into a failure result.

`Result<T>.Try` and `Result<T>.TryAsync` route a `null` return value through
`Create(...)`, so it becomes a failure with `Error.NullValue` rather than being
surfaced as an `Exception.ArgumentNullException`:

```csharp
Result<string> result = Result<string>.Try(() => LookupName(id)); // may return null
// If LookupName returns null: result.IsFailure && result.FirstError == Error.NullValue
```

Exceptional errors get a stable code like `Exception.InvalidOperationException`
so you can query by exception type:

```csharp
if (result.HasException<TimeoutException>()) { /* retry */ }
```

## Merge

Combine multiple results:

```csharp
// Non-generic — accepts a params ReadOnlySpan, combines all errors
Result merged = Result.Merge(result1, result2, result3);

// Extension on IEnumerable<Result>
Result merged = results.Merge();

// Extension on IEnumerable<Result<TValue>> — either all values or all errors
Result<IReadOnlyList<int>> merged = new[]
{
    Result<int>.Success(1),
    Result<int>.Success(2),
    Result<int>.Success(3)
}.Merge();
// merged.Value == [1, 2, 3]
```

When any element fails, the typed `Merge` discards collected values and returns a
failure aggregating every observed error in input order.

## Error querying

```csharp
if (result.HasError<ValidationError>())              // by type
if (result.HasError<ValidationError>(e => e.PropertyName == "Email"))  // by type + predicate
if (result.HasErrorCode("Customer.NotFound"))         // by code
if (result.HasException<TimeoutException>())          // wrapped exception
```

## Deconstruction

```csharp
var (isSuccess, errors) = Result.Failure("err");
var (isSuccess, value, errors) = Result<int>.Success(42);
```

## Implicit conversions

```csharp
Result<string> r = "hello";           // Success (via Create)
Result<string> r = (string?)null;     // Failure with Error.NullValue
Result r = new Error("fail");         // Failure
Result<int> r = new Error("fail");    // Failure
```

## Thread safety

`Result`, `Result<TValue>`, `Error`, and all built-in error subtypes are **immutable**.
Instances can be shared across threads without any synchronization:

- `Result` / `Result<T>` are `readonly struct` — fields cannot change after construction.
- `Error.Code`, `Error.Message`, `Error.Metadata`, and `Error.Causes` are init-only.
  `WithMetadata` / `CausedBy` return *new* instances rather than mutating.
- The internal `Errors` list is stored as an `Error[]` copied from the source collection,
  so external mutation of the source never affects the result.
- Successful results share a single empty `IReadOnlyList<Error>` instance — reading
  `.Errors` on a success never allocates and is safe under concurrent readers.

The library declares no static, mutable state and no global configuration, so there
are no hidden hazards when used in highly parallel handlers.

## Clean Architecture / CQS integration

```csharp
public sealed class CreateOrderHandler
{
    public async Task<Result<OrderId>> Handle(CreateOrderCommand command, CancellationToken ct)
    {
        return await ValidateCommand(command)
            .BindAsync(cmd => FindCustomer(cmd.CustomerId, ct))
            .Ensure(customer => customer.IsActive, CustomerErrors.Inactive)
            .BindAsync(customer => CreateOrder(customer, command, ct))
            .Tap(order => logger.LogInformation("Order {Id} created", order.Id))
            .Map(order => order.Id);
    }
}
```

## FAQ

**Why structs instead of classes?**
A class-based result allocates on every call. Structs make the success path
zero-alloc, which matters in tight loops and high-throughput handlers.

**What happens with `default(Result<string>)`?**
Because `Result<T>` is a `readonly struct`, the runtime can produce
`default` instances. A `default(Result<string>)` reports `IsSuccess = true`
and `ValueOrDefault = null`, but accessing `.Value` throws
`InvalidOperationException`. This is inherent to value types in .NET —
prefer the factory methods (`Success`, `Failure`, `Create`) and avoid
`default`. If you must defend against it on the read side, use
`TryGetValue(out T value)` — it returns `false` for both failures and
`default(Result<T>)` with a null value, so the happy path is exception-free.

**Why no `IResult` interface?**
An interface would box the struct, defeating the zero-allocation goal.
Pattern-match or use generics constrained to the concrete types instead.

**Can I use this with FluentValidation?**
Yes. Collect `ValidationFailure` results into `ValidationError` instances
and pass them to `Result.Failure(errors)`.

## License

[MIT](LICENSE) © 2026 Jesper Bruhn Riddersholm