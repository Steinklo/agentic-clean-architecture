---
status: proposed
reasoning: authored
---

# Fulfilling a Manifestation returns 501 through a new `NotImplemented` error category, never a 500

`Manifestation` records a request to make a `TodoItem` true in the physical world, and
`IRealityGateway` is the port through which the application would ask the physical world to change.
There is no adapter that could ever make that request succeed, so fulfilling a Manifestation is the
one operation in this solution that cannot succeed. `RealityGateway`, the only implementation,
declines every attempt.

The decision: that decline is returned as a failed `Result` carrying a new `DomainErrorType.NotImplemented`
category, which `ResultExtensions.StatusCodeFor` maps to `501 Not Implemented`, rather than being
allowed to surface as a 500. A thrown exception would be caught by `UnhandledExceptionBehaviour` and
reach the caller as a 500 — reporting a defect where there is none, since the application worked
exactly as designed.

## Considered Options

- **Shrug: reuse `DomainErrorType.Failure`, or let the missing implementation throw and surface as
  a plain 500.** Rejected — a 500 tells the caller a defect occurred and invites a retry, when the
  honest answer is that nothing went wrong and there is nothing to retry. The distinction also
  disappears in any metric that simply counts 5xx responses, which is the thing a dedicated category
  preserves.

## Consequences

- `DomainErrorType` now has a fifth member, and every place that switches on it exhaustively —
  `ResultExtensions.StatusCodeFor` and the regex in `ErrorCodeUniquenessTests` — had to be extended
  in the same change. Adding a category is not a compile error there; an arm left off reaches
  callers as a silent 500.
- The cross-aggregate reaction to a realized Manifestation (`ManifestationRealizedEventHandler`
  completing the `TodoItem`) is proven only at the aggregate seam. No HTTP request can ever reach it
  while `RealityGateway` declines every attempt, so it stays unproven end-to-end unless a real
  adapter is built.
- `reality.not-implemented`, the error code `RealityGateway` raises, is the one lowercase-kebab code
  in the solution — inherited from the original specification for this gateway rather than matching
  the `Aggregate.Guard` style used elsewhere. Changing it means editing `RealityGateway.cs`.
