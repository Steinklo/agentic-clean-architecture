---
status: proposed
reasoning: authored
---

# An operation this system understands but has not built answers 501, rather than throwing and surfacing as 500

`FulfilManifestation`'s only implementation, `RealityGateway`, can never succeed — there is no
adapter to the physical world, and none is planned. The system had no way for a legitimate,
understood operation to say "not built" without borrowing the vocabulary of an actual defect. This
PR adds `DomainErrorType.NotImplemented`, returned by `RealityGateway.MakeTrueAsync` as an ordinary
failed `Result` and mapped to `501 Not Implemented` in the one status ladder,
`ResultExtensions.StatusCodeFor`. The gateway declines by returning, never by throwing.

## Considered Options

- **Throw `NotImplementedException` and let `UnhandledExceptionBehaviour` surface it as a 500** —
  the common default, and the alternative the pull request weighed. Rejected because it reports a
  defect where there is none and gives the caller nothing stable to branch on; 501 is the true
  statement — the request was understood, and this application chose not to build it.

## Consequences

- `DomainErrorType` now has a fifth member, and every exhaustive switch over it —
  `ResultExtensions.StatusCodeFor` foremost — must carry an arm for it or fall through to its
  `_ => 500` discard, silently mapping a future member to 500 instead of failing the build.
- `reality.not-implemented` is the first, and so far only, lowercase-kebab error code in the
  solution; every other code is PascalCase and dotted. It marks the refusal as the gateway's rather
  than a domain guard's, but is now a precedent the next adapter can either follow or ignore.
- Any future adapter fronting a genuinely unbuilt integration has a stated pattern to follow:
  decline by returning a `NotImplemented` failure, never by throwing.
