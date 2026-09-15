using System.Diagnostics.CodeAnalysis;

// CA1000: Static factory methods on Result<T> (Success, Failure, Try, ...) are the entire
// point of the Result pattern. They cannot be redesigned away without breaking the library's
// public API — the non-generic Result.Success<T>(...) forms already exist for callers who
// want to avoid the generic instantiation. Suppress at the assembly level.
[assembly: SuppressMessage(
    "Design",
    "CA1000:Do not declare static members on generic types",
    Justification = "Factory methods on Result<T> are the core of the Result pattern.",
    Scope = "type",
    Target = "~T:Resultify.Result`1")]

// CA1716: 'Error' is a reserved keyword in VB.NET, but the domain-driven design convention
// is to name the error record exactly 'Error'. The type lives in the Resultify.Errors
// namespace, so consumers who need to disambiguate can alias. Renaming to 'ErrorInfo' or
// similar would hurt the ergonomics this library is explicitly designed for.
[assembly: SuppressMessage(
    "Naming",
    "CA1716:Identifiers should not match keywords",
    Justification = "'Error' matches the DDD convention and is a deliberate API choice.",
    Scope = "type",
    Target = "~T:Resultify.Errors.Error")]
