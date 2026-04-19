# Resultify

A modern, **immutable** Result pattern library for **.NET 10 / C# 14**.

Designed for Clean Architecture, DDD, and CQS codebases that want:

- **Zero-allocation success path** — `Result` and `Result<TValue>` are structs
- **Immutable errors with codes** — `Error(Code, Message)` records with metadata and causal chains
- **Functional combinators** — `Map`, `Bind`, `Match`, `Tap`, `Ensure`, `Switch`
- **First-class async** — every combinator has async overloads + fluent `Task<Result<T>>` extensions
- **Multi-error support** — essential for validation aggregation, not just single errors
- **No hidden state** — no global static configuration, no thread-safety pitfalls

## Installation

```shell
dotnet add package Resultify
```

## Quick Start

```csharp
using Resultify;

// Success
var result = Result<Customer>.Success(customer);

// Failure with a code + message
var result = Result<Customer>.Failure("Customer.NotFound", "Customer does not exist");

// Null-safe creation — non-null becomes Success, null becomes a failure with Error.NullValue
Result<Customer> result = customer;        // implicit conversion via Create
Result<Customer> result = Result<Customer>.Create(customer);

// Conditional factories
var result = Result.SuccessIf(age >= 18, "Must be at least 18");
var result = Result.FailureIf(string.IsNullOrEmpty(name), "Name is required");
```

## Errors

Errors are immutable `record` types with a machine-readable `Code` and a human-readable `Message`. The `Code` is useful for i18n, structured logs, and API responses.

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

### Metadata and Causes

```csharp
var error = new Error("Payment.Failed", "Payment gateway rejected the transaction")
    .WithMetadata("TransactionId", txId)
    .WithMetadata("Amount", amount)
    .CausedBy(new Error("Gateway.Timeout", "Gateway did not respond"))
    .CausedBy(httpRequestException);
```

### Domain Error Registries (DDD pattern)

Group your domain errors as static readonly fields, just like in Bookify-style codebases:

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

### Custom Error Types

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

The library ships with five useful subtypes that set sensible codes: `ValidationError`, `NotFoundError`, `ConflictError`, `ForbiddenError`, and `ExceptionalError`.

## Functional Combinators

### Map — transform the value

```csharp
Result<string> name = Result<Customer>.Success(customer).Map(c => c.FullName);
```

### Bind — chain dependent operations (aka flatMap, SelectMany)

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

### Match — collapse a Result into a value

```csharp
IActionResult response = result.Match(
    onSuccess: value => Ok(value),
    onFailure: errors => BadRequest(errors[0].Message));
```

### Tap — side effects without changing the result

```csharp
var result = GetCustomer(id)
    .Tap(c => logger.LogInformation("Found customer {Id}", c.Id))
    .Bind(c => CreateOrder(c));
```

## Async Pipelines

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

Exceptional errors get a stable code like `Exception.InvalidOperationException` so you can query by exception type:

```csharp
if (result.HasException<TimeoutException>()) { /* ... */ }
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

## Error Querying

```csharp
// By type
if (result.HasError<ValidationError>())
    /* ... */

// By type + predicate
if (result.HasError<ValidationError>(e => e.PropertyName == "Email"))
    /* ... */

// By code (stable machine-readable identifier)
if (result.HasErrorCode("Customer.NotFound"))
    /* ... */

// Wrapped exception
if (result.HasException<TimeoutException>())
    /* ... */
```

## Deconstruction

```csharp
var (isSuccess, errors) = Result.Failure("err");
var (isSuccess, value, errors) = Result<int>.Success(42);
```

## Implicit Conversions

```csharp
Result<string> r = "hello";                  // via Create — Success when non-null
Result<string> r = (string?)null;            // via Create — Failure with Error.NullValue
Result r = new Error("fail");                // Failure
Result<int> r = new Error("fail");           // Failure
```

## Integration with Clean Architecture / CQS

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

## Design Decisions

| Decision | Rationale |
|---|---|
| `struct` for Result types | Zero-allocation success path; value semantics |
| `record` for Error types with positional `(Code, Message)` | Immutability via `with`; structural equality; inheritance for custom errors |
| `Success` / `Failure` naming | Matches `IsSuccess` / `IsFailure` properties and common DDD/Bookify convention |
| `Code` on every error | Machine-readable identifiers for i18n, logs, and API responses |
| Multi-error support via `IReadOnlyList<Error>` | Validation aggregation is common and painful with single-error designs |
| `Create<T>(T?)` with null-handling | Ergonomic implicit conversion that defends against null references |
| No global static configuration | Thread-safe by default; no hidden coupling |
| No `IError` / `IReason` interfaces | `record` inheritance covers extensibility without interface ceremony |
| No `Success` reason objects | YAGNI for most codebases; success is the absence of errors |

## Comparison with Bookify's Result pattern

The Bookify-style `Result` is a great teaching implementation. Resultify keeps its ergonomics (`Success`/`Failure`, `Create`, `Error` with code+message, implicit conversions) while addressing two practical limitations:

- **Multiple errors** — Bookify's single `Error` slot makes validation aggregation awkward (you end up inventing comma-separated messages or concatenating Code strings). Resultify uses `IReadOnlyList<Error>` so `FluentValidation`-style aggregation is natural.
- **Heap allocations** — Bookify's `Result` is a class, so every handler invocation allocates. Resultify is a struct, making the success path zero-alloc. This matters in tight loops like list projection or EF Core query translation.

If you already have Bookify-style handlers, migration is mostly mechanical — rename `Result.Success/Failure` stays the same, `result.Error` still works (returns the first error or `Error.None`), and `Result.Create(value)` is identical.

## License

MIT
