# Resultify

A modern, **immutable** Result pattern library for **.NET 10**.

- Zero-allocation success path — `Result` and `Result<TValue>` are `readonly struct`
- Immutable errors with machine-readable codes, metadata, and causal chains
- Functional combinators — `Map`, `Bind`, `Match`, `Tap`, `Ensure`, `Switch`
- First-class async — every combinator has an async overload + fluent `Task<Result<T>>` extensions
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
human-readable `Message`. The code is useful for i18n, structured logs, and
API responses.

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

```csharp
result.Switch(
    onSuccess: value => logger.LogInformation("Got {Value}", value),
    onFailure: errors => logger.LogWarning("Failed: {Errors}", errors));
```

### Tap — side effects without changing the result

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
they propagate as-is in both sync and async variants.

Exceptional errors get a stable code like `Exception.InvalidOperationException`
so you can query by exception type:

```csharp
if (result.HasException<TimeoutException>()) { /* retry */ }
```

## Merge

Combine multiple results:

```csharp
// Non-generic — combines errors
Result merged = Result.Merge(result1, result2, result3);

// Extension on collection
Result merged = results.Merge();

// Generic — either all values or all errors
Result<IReadOnlyList<int>> merged = new[]
{
    Result<int>.Success(1),
    Result<int>.Success(2),
    Result<int>.Success(3)
}.Merge();
// merged.Value == [1, 2, 3]
```

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

## API at a glance

### Result (non-generic)

| Method | Description |
|--------|-------------|
| `Result.Success()` | Create a successful result |
| `Result.Failure(error)` | Create a failed result |
| `Result.SuccessIf(condition, error)` | Conditional success |
| `Result.FailureIf(condition, error)` | Conditional failure |
| `Result.Try(action)` | Catch exceptions as errors |
| `Result.TryAsync(func)` | Async variant of `Try` |
| `Result.Merge(...)` | Combine multiple results |
| `result.FirstError` | The first error, or `Error.None` if successful |
| `result.Bind(func)` | Chain to another `Result` |
| `result.BindAsync(func)` | Async variant of `Bind` |
| `result.Tap(action)` | Side effect on success |
| `result.TapAsync(func)` | Async variant of `Tap` |
| `result.TapError(action)` | Side effect on failure |
| `result.Ensure(predicate, error)` | Validation gate |
| `result.Match(onSuccess, onFailure)` | Fold into a value |
| `result.MatchAsync(onSuccess, onFailure)` | Async variant of `Match` |
| `result.Switch(onSuccess, onFailure)` | Execute one of two actions |
| `result.ToResult<T>(value)` | Convert to `Result<T>` |

### Result&lt;TValue&gt;

All of the above, plus:

| Method | Description |
|--------|-------------|
| `Result<T>.Success(value)` | Create a successful result with a value |
| `Result<T>.Create(value?)` | Null-safe factory |
| `Result<T>.SuccessIf(condition, value, error)` | Conditional success with a value |
| `result.Map(func)` | Transform the value |
| `result.MapAsync(func)` | Async variant of `Map` |
| `result.ValueOrDefault` | Value or `default(T)` |
| `result.ToResult()` | Drop the value, keep success/failure state |
| `result.ToResult<TNew>(converter)` | Convert to a different value type |

## Design decisions

| Decision | Rationale |
|---|---|
| `readonly struct` for Result types | Zero-allocation success path; value semantics |
| `record` for Error types | Immutability via `with`; structural equality; inheritance for custom errors |
| `Success` / `Failure` naming | Matches `IsSuccess` / `IsFailure` and common DDD convention |
| `Code` on every error | Machine-readable identifiers for i18n, logs, and API responses |
| Multi-error via `IReadOnlyList<Error>` | Validation aggregation is common and painful with single-error designs |
| `Create<T>(T?)` with null-handling | Ergonomic implicit conversion that defends against null references |
| `Success(value)` rejects null | Prevents creating a "successful" result that throws on `.Value` access |
| `Try` re-throws `OperationCanceledException` | Cancellation should propagate, not become an error |
| No global static configuration | Thread-safe by default; no hidden coupling |
| No `IError` / `IReason` interfaces | `record` inheritance covers extensibility without interface ceremony |
| No `Success` reason objects | YAGNI for most codebases; success is the absence of errors |

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
`default`.

**Why no `IResult` interface?**
An interface would box the struct, defeating the zero-allocation goal.
Pattern-match or use generics constrained to the concrete types instead.

**Can I use this with FluentValidation?**
Yes. Collect `ValidationFailure` results into `ValidationError` instances
and pass them to `Result.Failure(errors)`.

## License

[MIT](LICENSE) © 2026 Jesper Bruhn Riddersholm
