---
status: proposed
reasoning: authored
---

# A legitimate operation with no adapter behind it answers 501, not 500

The Manifestations slice adds `FulfilManifestation`, the one use case in this solution that can
never succeed: it asks `IRealityGateway` to make a `TodoItem` true in the physical world, and the
only adapter to that gateway, `RealityGateway`, has nothing behind it and always declines. The
request is understood and well-formed, and the application has correctly recognised that it
cannot do the work.

The decision is to give that outcome its own category. `DomainErrorType` gained a `NotImplemented`
member, `RealityGateway` returns a failed `Result` carrying it instead of throwing
`NotImplementedException`, and `Todo.Api/Common/ResultExtensions.cs`'s single status ladder maps it
to `501 Not Implemented`. A thrown exception would instead be caught by
`UnhandledExceptionBehaviour` and reach the caller as a 500 — reporting a defect where none exists.

## Considered Options

- **Reuse `DomainErrorType.Failure`, mapped to 500 — "shrug, return a 500."** This is the
  alternative the pull request body names explicitly as rejected: it was the honest default before
  this change, and reusing it would have meant no new category and no new arm to maintain. Rejected
  because a 500 tells the caller the application is broken when it worked exactly as designed, and
  because the distinction between "something went wrong" and "this was never built" disappears in
  any monitoring that counts 5xx responses as incidents.

## Consequences

- Every switch over `DomainErrorType` — today only `ResultExtensions.StatusCodeFor` — must carry an
  arm for `NotImplemented`, or `TreatWarningsAsErrors` fails the build on CS8524 rather than letting
  the category silently fall through to 500.
- `ErrorCodeUniquenessTests`'s regex must keep matching `DomainError.NotImplemented(...)` alongside
  the other four factories, or a duplicate code in that category would go unnoticed.
- Any future adapter with nothing behind it now has a precedent to follow: return
  `DomainError.NotImplemented(...)` rather than throw, and it reaches callers as 501.
