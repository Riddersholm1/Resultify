# Contributing

Thank you for considering a contribution. Resultify sits in the error-handling path of every
application that uses it, so a bug here tends to show up as a failure that reads as a success, or the
other way round. The bar for correctness is set accordingly.

## Getting set up

You need the **.NET 10 SDK**.

```sh
dotnet build -warnaserror   # what CI runs; zero warnings expected
dotnet test
```

The library project also sets `TreatWarningsAsErrors` with `AnalysisLevel` at `latest-recommended`,
and turns on the trimming and NativeAOT analyzers (`IsAotCompatible`), because the README promises
both. A warning is a build failure, not a suggestion.

## What a good change looks like

**Every behavioural change needs a test that fails before it.** Not a test that passes afterwards —
one that demonstrably fails first. A test that asserts the wrong behaviour is worse than no test, and
writing the test first is the only reliable defence.

**Keep the guarantees the README makes.** In particular:

- *Immutability.* Nothing may let a result or an error change after construction.
- *A zero-allocation success path.* `Result` and `Result<T>` stay `readonly struct`, and nothing boxes
  them.
- *Async parity.* A new combinator gets its async overload and its `Task<Result>` / `Task<Result<T>>`
  extension, and the extension awaits with `ConfigureAwait(false)` and delegates to the instance
  method, so every form behaves identically.
- *Null arguments* throw `ArgumentNullException`, including from async members (as a faulted task).

**Explain the "why" in comments, not the "what".** The code says what it does. Comments should say why
it does it that way, and especially why an obvious-looking alternative is wrong.

## Things that will be pushed back on

- Adding a runtime dependency. There are none, and that is a feature.
- Making `Result` or `Result<T>` a class, or adding an interface that would box them — see the FAQ.
- Un-sealing or overriding `Error.ToString()`. The sealed format is what keeps metadata and stack
  traces out of logs.
- Global configuration or static mutable state.
- Catching `OperationCanceledException` in `Try` or `TryAsync`.

If your change contradicts one of these, that is fine — but argue the decision in the issue or pull
request; do not route around it.

## Versioning

Resultify follows semantic versioning, and "breaking" includes behaviour existing code can observe,
not just signatures. 2.0.0 changed no member at all and was still a major version, because three
behaviours changed.

| Change | Bump |
|---|---|
| A fix no correct caller can observe | patch |
| A new public member | minor |
| Removing or re-signing a member, or changing observable behaviour — an output format, an exception thrown, what a cast allows | **major** |

A major version gets an *Upgrading from N.x* section in the README, as 2.0 did. There is no automated
public-API check in this repository, so describe every public API change in the pull request.

## Commit messages

Explain the reasoning, not just the change. A reader six months from now needs to know why, and the
diff already tells them what.

## Releasing

Publishing to NuGet happens one way, and only one way — the same as for Riddersholm.EventBus and
Riddersholm.Money:

1. Merge the change to `main`. The version is not stored in the repository: `<Version>` in
   [`src/Resultify/Resultify.csproj`](src/Resultify/Resultify.csproj) stays `0.0.0`, and the release tag
   supplies the real one.
2. Tag the merge commit — `git tag v2.1.0 && git push origin v2.1.0`.
3. **Publish a GitHub Release for that tag.** This is what triggers the publish job in
   [`pipeline.yml`](.github/workflows/pipeline.yml); the tag alone does nothing. There is no CHANGELOG
   file, so the release notes are the record of what changed. Tick *Set as a pre-release* for an
   `-rc`/`-beta` version.

The release run builds with `-warnaserror` and runs the tests before the publish job pushes anything,
so a release cannot ship something CI would have rejected. The package version is the tag with the
leading `v` stripped, used as-is, so tag plain versions only (`v2.1.0`, `v2.1.0-rc.1`). The push uses
`--skip-duplicate`, so re-running a half-finished release is safe.

There is **no `NUGET_API_KEY` secret**, deliberately. The workflow authenticates with
[Trusted Publishing](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing): GitHub mints an
OIDC token for the run, and NuGet exchanges it for an API key that expires in an hour. It is already set
up; for reference, the nuget.org policy (*your profile → Trusted Publishing*) is:

| Field | Value |
|---|---|
| Policy name | `Resultify-GitHubActions` |
| Repository owner | `Riddersholm1` |
| Repository | `Resultify` |
| Workflow file | `pipeline.yml` |
| Environment | *(empty)* |

The repository **variable** `NUGET_USER` (Settings → Secrets and variables → Actions → *Variables*)
holds the nuget.org profile name that owns the policy. Renaming or moving `pipeline.yml` breaks the
match until the policy is updated.

The optional **`CODECOV_TOKEN`** secret enables the coverage upload; the step never fails the build
without it.

## Reporting a security issue

Please do not open a public issue — see [SECURITY.md](SECURITY.md).

## Code of conduct

Everyone taking part is expected to follow the [Code of Conduct](CODE_OF_CONDUCT.md).
