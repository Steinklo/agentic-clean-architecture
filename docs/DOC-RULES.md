# Documentation rules

**This file is written by humans and read by the documentation agent.** It is the contract: what
the agent writes, what it derives each thing from, the form it must write in, what it may never
touch, and when it may propose a decision record.

## Two kinds of document

Everything written down here is one or the other, and the split decides who may write it.

| | | |
|---|---|---|
| **Rules** | `docs/` | How things must be built. Authored by people. Change when a *decision* changes, never because code changed. |
| **Maps** | the `AGENTS.md` files | What exists and where it lives. Change every time a feature lands — which is why the agent maintains them. |

**The agent writes maps. It does not write rules.** It reads `docs/` to know what is true and
never edits a word of it, with exactly one exception: it may add a record under `docs/adr/`, under
the gate below.

That asymmetry is the point. An agent that could edit the rules could rewrite the standard it is
measured against, and the drift would be invisible because both sides moved together.

If you are a human and a map says something wrong, **change the code** — the map follows. If a
*rule* is wrong, edit it here in `docs/`; that whole directory is yours.

## The maps the agent maintains

Exactly these, and no others. The agent must not invent a new `AGENTS.md` anywhere.

| Map | What it is | Derived from |
| --- | --- | --- |
| `AGENTS.md` | The root map: which features exist, where each one's parts live, the entry points, and the shared building blocks. | The feature folders across `src/`, the `ConfigureServices` files, `Program.cs`, and `tests/Todo.ArchitectureTests/Rules.cs` for the pointer to the rule inventory. |
| `src/<project>/AGENTS.md` | One map per project: what that layer holds right now — the types, folders and routes actually present. | That project's own source tree. Nothing else. |
| `CLAUDE.md` | A pointer to `AGENTS.md`. It has no content of its own. | — |

**Every map is re-derivable from the code alone.** A regeneration run should reconstruct it
without reading the previous version. If the agent cannot derive a statement from the source
named, that statement does not belong in a map — it is probably a rule, and rules live in `docs/`.

## The form a map is written in

**Short. A map is an index, not an essay.** These are the constraints, and they are rules:

- **Never explain why.** The reason a thing is done this way is a rule and lives in `docs/`. A map
  that starts justifying itself has become a second copy of the rules, and the two will drift.
- **Never restate a rule.** Link to the `docs/` file that owns it and stop. One sentence at the
  top of each map pointing at its rules file is enough.
- **Prefer a table to a paragraph.** Most of a map is *name → location*, which is a table.
- **Name what exists, not what should.** "Five commands under `TodoLists/Commands/`" is a map.
  "Commands belong in their own folder" is a rule.
- **Directory trees are allowed here and nowhere else.** They go stale faster than any other
  content, which is exactly the objection a regenerated file answers — but keep them to the shape
  of a feature, never a full listing that `git ls-files` answers better.
- No screenshots, no badges, no changelog-by-accretion. A map is rewritten, not appended to.
- House spelling is British English (`behaviour`, `serialisation`, `initialise`).

## What the agent must never touch

Everything except the maps and `docs/adr/`. In particular: `docs/` itself, `.claude/`,
`README.md`, `src/`, `tests/`, and the build files. `.protected-paths.json` is the single source
of truth for the guard and is never the agent's to edit.

Fixing a typo is not an exception. If a never-touch file is wrong, say so in the pull request and
leave it to a human.

## When to propose a decision record

Default to **not** writing one. A repository of ADRs nobody trusts is worse than no ADRs, and the
fastest way to get there is an agent that files one per pull request.

**The gate is all three of these at once:**

1. **Hard to reverse.** Undoing it later means changing code that is not the code being changed now.
2. **Surprising without context.** A competent reader of the diff would ask "why on earth is it
   done this way?" and the diff would not answer.
3. **The result of a real trade-off.** There was a genuine alternative that a reasonable person
   would have picked, and it was rejected for a reason that can be stated.

Miss any one of the three and there is no ADR.

**Propose one only for feature-level architectural decisions**, which in practice means:

- a **public contract changes** — a route, its request or response shape, or an error contract;
- a **dependency is added or swapped** — a new package, or one library replacing another;
- a **layer boundary moves** — something changes about what a layer may reference or contain;
- there is a **genuine choice between alternatives** and one was taken.

**Never propose one for:**

- **bug fixes** — restoring intended behaviour is not a decision;
- **refactors with no behaviour change** — including renames, file moves and extractions;
- **test-only changes** — new tests, fixtures or harness work;
- **dependency version bumps** — including security bumps and transitive pins. The pin and its
  reason belong in `Directory.Packages.props` and `docs/gotchas.md`, not in an ADR.

**Records are for decisions taken on top of this template, not about it.** The template's own
choices are stated as rules in `docs/` and beside the code each one constrains.

When the gate is not met but the reasoning is still worth keeping, put it where it will be read: a
comment at the code it explains, or a gotcha in `docs/gotchas.md` — and since that is a rule file,
propose it in the pull request rather than writing it.

## Proposed decision records are proposed, and only proposed

An ADR the agent writes carries `status: proposed` in its front matter. **Nothing else.**

Promotion to `accepted` is a human edit, made deliberately, by someone who holds the reasoning.
This is the whole point: an agent that wrote `status: accepted` would be inventing rationale that
nobody actually held, and it would be indistinguishable in the record from rationale someone did.
The proposed state is the agent saying *"this looks like a decision, and here is my reconstruction
of it"* — which is useful, and is not the same claim.

Form, matching [`adr/TEMPLATE.md`](adr/TEMPLATE.md), which is human-owned and not the agent's to
redesign:

- File name `NNNN-kebab-case-title.md`, with `NNNN` the next unused number.
- Front matter containing `status: proposed`.
- A title stating the decision as a sentence, not a topic.
- The decision and its reasoning in prose.
- **A `## Considered Options` section naming what was rejected and why.** An ADR with no rejected
  alternative is evidence there was no trade-off, and therefore that the gate above was not met —
  if the agent cannot name one, it must not file the record.
- A `## Consequences` section: what this now costs, constrains or obliges.

The agent proposes; it never promotes, never re-numbers, and never edits an existing record to
agree with a new one.

## When regeneration would be wrong

If the sources contradict each other — the architecture tests assert something the reference graph
denies, this file names a map whose sources no longer exist — the agent must **not** paper over
it. It regenerates what it can, states the contradiction plainly in the pull request, and leaves
the resolution to a human. A confident document over an unresolved contradiction is the failure
this whole arrangement exists to prevent.
