---
status: proposed
reasoning: authored
---

# An operation the system understands but has not built answers 501 Not Implemented

`Manifestation.Fulfil` asks `IRealityGateway` to change the physical world, and no adapter to
reality exists or ever will. The decision: a new `DomainErrorType.NotImplemented` category, which
`IRealityGateway`'s only implementation, `RealityGateway`, *returns* as a failed `Result` rather
than throwing, and which `ResultExtensions.StatusCodeFor` maps to `501 Not Implemented`.

## Considered Options

- **Throw `NotImplementedException` and let `UnhandledExceptionBehaviour` surface it as a 500** —
  the common default for an operation with nothing behind it. Rejected because it reports a defect
  where there is none — the application worked exactly as designed — and gives a caller nothing
  stable to act on, where a `DomainErrorType` category gives it a status code and a stable error
  code to branch on.

## Consequences

- `DomainErrorType` now has a fifth member, and every place that switches on it exhaustively —
  `ResultExtensions.StatusCodeFor` in particular — must gain an arm in the same change a new
  category is added; the switch's own remarks explain why that is not a compile error and cannot
  be made into one.
- `ErrorCodeUniquenessTests`'s regex must keep listing every `DomainError` factory, including
  `NotImplemented`, even though nothing in `Todo.Domain` raises that category today — the category
  belongs to adapters, not to domain guards.
- Any future adapter with nothing behind it — not just `RealityGateway` — now has a precedent to
  follow: decline by returning `DomainError.NotImplemented(...)`, never by throwing.
