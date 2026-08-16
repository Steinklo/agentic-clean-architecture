---
status: proposed
reasoning: authored
---

# `docs/adr/**` is shared between people and the documentation agent, rather than owned by either

Records were agent-owned, which made them writable by nobody who could finish one: the agent
may only ever leave a record `proposed`, and promotion to `accepted` is a human edit — but the
guard blocked humans from `docs/adr/` entirely. `docs/adr/**` is now `sharedOwned`, answered by a
second protected-path matcher mode so the guard's question ("may a human write this?") and the
documentation agent's self-check ("may the agent keep this?") are each answered directly instead
of one being derived by negating the other. The agent may also file a record whose rejected
alternative it reconstructed from the diff, marked `reasoning: reconstructed` in the front
matter, rather than declining to file at all.

## Considered Options

- **Keep records agent-authored only, and forbid the agent from filing when it cannot find a
  stated alternative.** Rejected because it produces no records at all: the agent declines
  whenever nobody wrote the reasoning down, which is exactly the case where a record would have
  been worth having, and the human who could supply that reasoning cannot write to the directory.
  The honesty concern it was protecting against is real, but it is answered better by a
  provenance field than by silence — a labelled guess can be corrected, whereas a missing record
  is indistinguishable from a decision nobody took.

## Consequences

- The protected-path guard and the documentation agent's self-check must keep asking their two
  questions separately; collapsing `sharedOwned` back into a boolean silently discards every
  record the agent is asked to propose. `verify-protected-paths.mjs` asserts the two questions
  differ, so that regression fails loudly instead of quietly.
- A `reasoning: reconstructed` record is an invitation to correct it, not a finished account —
  its `## Considered Options` section is the agent's inference until a person corrects it.
- Promotion to `status: accepted` stays a human edit. The agent may revise a record it created on
  the pull request it is running on, but never writes `accepted` itself.
