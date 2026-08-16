---
status: proposed
reasoning: authored
---

<!--
  Copy to NNNN-kebab-case-title.md, where NNNN is the number of the pull request that
  took the decision, zero-padded. Not "the next unused number" - two open pull
  requests compute the same next number and collide on merge. Delete these comments.

  Before writing: the gate is ALL THREE at once.
    1. Hard to reverse   - undoing it later means changing code that is not this code.
    2. Surprising        - a competent reader of the diff asks "why on earth this way?"
                           and the diff does not answer.
    3. A real trade-off  - a reasonable person would have picked the alternative.

  Miss one and there is no ADR. Put the reasoning in a code comment or an AGENTS.md
  gotcha instead.

  TWO FRONT-MATTER FIELDS, and they answer different questions.

  status:     proposed | accepted. An agent writes `proposed` and never `accepted`.
              Promotion is a human edit, made by someone who holds the reasoning.
              A person authoring a record may write `accepted` directly -- holding
              the reasoning is the whole distinction `proposed` marks.

  reasoning:  authored | reconstructed. `authored` means a person stated the
              trade-off, either in this file or in the pull request body for the
              agent to transcribe. `reconstructed` means the agent inferred it from
              the diff, because nobody wrote it down.

              This field exists because the one section that matters, Considered
              Options, is the one thing NOT derivable from code: a diff shows what
              was chosen and never what was rejected. Without the field, a plausible
              guess and somebody's real reasoning are indistinguishable six months
              later. A `reconstructed` record is an invitation to correct it, not a
              finding -- fix the section and change this to `authored`.
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
  and therefore that the gate was not met.

  If you are a person and cannot name one, do not file this.

  If you are the agent and cannot find one stated anywhere, you may still file with
  `reasoning: reconstructed` -- but say IN THIS SECTION that the alternative is your
  inference and not the author's account. Never let a guess read like testimony.
-->

- **The alternative.** Rejected because…

## Consequences

<!--
  What this now costs, constrains or obliges. The non-obvious downstream effects,
  not a restatement of the decision. Include what would make you revisit it.
-->
