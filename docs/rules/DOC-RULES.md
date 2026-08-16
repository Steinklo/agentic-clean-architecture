# Documentation rules

**This file is written by humans and read by the documentation agent.** It is the contract: what
the agent writes, what it derives each thing from, the form it must write in, what it may never
touch, and when it may propose a decision record.

## Three kinds of document

Everything written down here is one of these, and which one decides who may write it.

| | | |
|---|---|---|
| **Rules** | `docs/` | How things must be built. Authored by people. Change when a *decision* changes, never because code changed. |
| **Maps** | the `AGENTS.md` files | What exists and where it lives. Change every time a feature lands — which is why the agent maintains them. |
| **Records** | `docs/adr/` | Why a decision was taken. Written once and left alone. **Either may author one**; only a person may promote one to `accepted`. |

**The agent writes maps. It does not write rules.** It reads `docs/` to know what is true and
never edits a word of it, with exactly one exception: records, under the gate below.

That asymmetry is the point. An agent that could edit the rules could rewrite the standard it is
measured against, and the drift would be invisible because both sides moved together.

If you are a human and a map says something wrong, **change the code** — the map follows. If a
*rule* is wrong, edit it here in `docs/`; that whole directory is yours.

## The maps the agent maintains

Exactly these, and no others. The agent must not invent a new `AGENTS.md` anywhere.

| Map | What it is | Derived from |
| --- | --- | --- |
| `AGENTS.md` | The root map: which features exist, where each one's parts live, the entry points, and the shared building blocks. | The feature folders across `src/`, the `ConfigureServices` files, `Program.cs`, and `tests/Todo.ArchitectureTests/Rules.cs` for the pointer to the rule inventory. |
| `<directory>/AGENTS.md` | One map per directory that has one — today that is each project under `src/`. What that directory holds right now: the types, folders and routes actually present. | That directory's own source tree. Nothing else. |
| `CLAUDE.md` | A pointer to `AGENTS.md`. It has no content of its own. | — |

Any `AGENTS.md` anywhere in the repository is a map and is yours to maintain — the guard treats
them all the same, so a map you do not maintain would belong to nobody.

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
of truth for the guard and is never the agent's to edit — and `docs/adr/TEMPLATE.md` is the record
*form*, which the agent fills in and never redesigns.

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
  reason belong in `Directory.Packages.props` and `docs/rules/gotchas.md`, not in an ADR.

**Records do not restate the template's existing choices.** Why the dependency rule points inward,
why there are two testing seams, why `DomainError` is not called `Error` — all of that is already
stated as a rule in `docs/` and beside the code it constrains, and an ADR re-explaining it is a
second copy that will drift.

**Changing one of those choices is a different matter, and does qualify.** Widening
`DomainErrorType`, moving a layer boundary, altering what a `Result` means to the transport layer —
these are decisions taken *now*, with alternatives that were live *now*, and the rule they change
records what is true rather than why it was chosen. Read the exclusion above as "do not restate",
never as "the template's own machinery is out of bounds".

*This paragraph used to say only "records are for decisions taken on top of this template, not
about it", which read as the second thing while the list above said the first. The documentation
agent hit exactly that conflict on a change that widened an error contract, had to pick a side, and
filed nothing. If you find yourself resolving a contradiction here rather than applying a rule,
that is a bug in this file.*

When the gate is not met but the reasoning is still worth keeping, put it where it will be read: a
comment at the code it explains, or a gotcha in `docs/rules/gotchas.md` — and since that is a rule file,
propose it in the pull request rather than writing it.

## Decision records are the one document either may write

Everything else here splits cleanly: the agent owns maps, people own rules. Records are the
exception, and `.protected-paths.json` lists `docs/adr/**` as `sharedOwned` for that reason. Guard
it against people and a record can never be promoted; guard it against the agent and it cannot file
the proposal it was asked for.

**The agent proposes. It never promotes.** An ADR the agent writes carries `status: proposed`, and
nothing else. An agent writing `status: accepted` would be inventing rationale nobody held, and it
would be indistinguishable in the record from rationale someone did. A person authoring a record
may write `accepted` directly — holding the reasoning is precisely what `proposed` exists to mark
the absence of.

**And it says where the reasoning came from.** `reasoning: authored` when a human stated the
trade-off — in the record, or in the pull request body for the agent to transcribe.
`reasoning: reconstructed` when the agent inferred it from the diff because nobody wrote it down.

That second field is not bureaucracy. Every other rule here rests on maps being *re-derivable from
the code alone* — and the one section that makes an ADR worth reading, `## Considered Options`, is
the one thing that is not. A diff shows what was chosen and never what was rejected. So the agent
may reconstruct, and must never let the reconstruction pass as testimony. A `reconstructed` record
is an invitation to correct it.

Form, matching [`../adr/TEMPLATE.md`](../adr/TEMPLATE.md), which is human-owned and not the agent's to
redesign:

- File name `NNNN-kebab-case-title.md`, with **`NNNN` the number of the pull request that took the
  decision**, zero-padded. Not the next unused number: two open pull requests compute the same one
  and collide on merge, and the pull request number says where the discussion is.
- Front matter containing `status:` and `reasoning:`.
- A title stating the decision as a sentence, not a topic.
- The decision and its reasoning in prose.
- **A `## Considered Options` section naming what was rejected and why.** An ADR with no rejected
  alternative is evidence there was no trade-off, and therefore that the gate above was not met. A
  person who cannot name one must not file the record; the agent may file with
  `reasoning: reconstructed`, and must say in that section that the alternative is its inference.
- A `## Consequences` section: what this now costs, constrains or obliges.

The agent may revise **the record it created on the pull request it is currently running on** — to
upgrade `reconstructed` to `authored` once a human fills the section in, for instance. It never
touches a record from an earlier pull request, never re-numbers, and never edits an existing record
to agree with a new one.

## When regeneration would be wrong

If the sources contradict each other — the architecture tests assert something the reference graph
denies, this file names a map whose sources no longer exist — the agent must **not** paper over
it. It regenerates what it can, states the contradiction plainly in the pull request, and leaves
the resolution to a human. A confident document over an unresolved contradiction is the failure
this whole arrangement exists to prevent.
