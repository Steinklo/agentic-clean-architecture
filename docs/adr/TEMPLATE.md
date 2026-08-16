---
status: proposed
---

<!--
  Copy to NNNN-kebab-case-title.md, next unused number. Delete these comments.

  Before writing: the gate is ALL THREE at once.
    1. Hard to reverse   - undoing it later means changing code that is not this code.
    2. Surprising        - a competent reader of the diff asks "why on earth this way?"
                           and the diff does not answer.
    3. A real trade-off  - a reasonable person would have picked the alternative.

  Miss one and there is no ADR. Put the reasoning in a code comment or an AGENTS.md
  gotcha instead.

  An agent writes `status: proposed` and never `accepted`. Promotion is a human edit,
  made by someone who holds the reasoning.
-->

# The decision, stated as a sentence

<!--
  A sentence, not a topic. "Value objects map through value converters" - not
  "Value object mapping".
-->

The context and the decision, in prose. What was true that made this necessary, what was decided, and why — enough that a reader six months from now does not have to reconstruct it.

## Considered Options

<!--
  Required. An ADR with no rejected alternative is evidence there was no trade-off,
  and therefore that the gate was not met. If you cannot name one, do not file this.
-->

- **The alternative.** Rejected because…

## Consequences

<!--
  What this now costs, constrains or obliges. The non-obvious downstream effects,
  not a restatement of the decision. Include what would make you revisit it.
-->
