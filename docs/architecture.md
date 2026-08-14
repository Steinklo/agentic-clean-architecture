# Architecture

The dependency rule points inward: `Domain <- Application <- Infrastructure <- Api`.

This file is a **rule**, not a description. It says how the solution must be built. What the
solution currently contains is in the `AGENTS.md` map files, which are regenerated as code lands.

## The rules, and the tests that enforce them

Every layering rule is a test. Prose explains *why*; the test decides.
**`tests/Todo.ArchitectureTests/Rules.cs` is the inventory** — read it to learn what the suite
guarantees today, rather than trusting a list in prose. Each `docs/layers/<project>.md` names the
rules that bite in that layer.

**Every rule declares how much it is proving — `Live`, `Thin` or `Dormant` — plus the population
it is meaningful at, and the suite fails in every direction against both.** A `Live` rule
examining zero types reports `VACUOUS`; examining fewer than it claims to need,
`THINNER THAN DECLARED`. A `Thin` rule reaching that number reports `NO LONGER THIN`, and a
`Dormant` rule examining anything at all reports `NO LONGER DORMANT`.

`Thin` is the state worth understanding. A rule can examine several types and still prove nothing,
because those types were written in the same change as the rule: "every feature places its
commands in a use-case folder" passes by construction while there is one feature. Such a rule
still runs and still fails on a violation — it simply stops implying breadth it has not got, and
`Rules.cs` says so where people read it.

Growing past a declared state is *meant* to turn the suite red: promote the rule and record in its
XML comment which slice did it. **Narrowing a selector to keep a rule dormant, or raising
`MeaningfulAt` to keep it thin, is the ratchet running backwards.**

The ORM rules forbid the **namespaces** (`Microsoft.EntityFrameworkCore`,
`Microsoft.Data.SqlClient`), not the project reference — which is what makes
`AddInfrastructureServices(...)` legal in `Program.cs` while a stray `AddDbContext<T>()` there is
not. The two reference-graph rules read the **`.csproj`, not the compiled assembly**, because the
compiler elides a `ProjectReference` that is declared but unused, and "declared but not yet used"
is exactly the violation worth catching early.

## Testing seams — two, and only two

- **HTTP seam** (`tests/Todo.IntegrationTests`) — the primary one. Real requests against the app
  hosted in process, backed by a real SQL Server container: routing, serialisation, validation,
  handlers, repositories, mapping, migrations, event dispatch and result translation in one test.
- **Aggregate seam** (`tests/Todo.UnitTests`) — construct the aggregate directly, assert on the
  returned `Result`. For invariant matrices: many cases, no I/O.

**Do not write handler tests with mocked repositories.** That seam was considered and deliberately
rejected: it re-tests what the HTTP seam already covers and couples every test to a handler's
constructor signature. The bugs that matter here — a value object that does not survive the round
trip, a validator registered but never run, a wrong status code — are invisible from inside a
handler and obvious from the HTTP seam. The review agent reports such a test as a finding even
when it passes.

## Who enforces what

**One rule, one owner.** GitHub owns every rule that has to bind everyone; a hook is at most a
*cached mirror* of a CI rule for speed, never a second rule with its own definition. A hook binds
Claude Code and nothing else — not your editor, not a colleague on another tool, not a plain
`git commit` — so a hook-only rule is a habit, not a rule.

- **CI owns**: the build and warnings-as-errors, all three test suites, the architecture rules,
  the rule-citation check, and the protected-path guard. These bind everyone regardless of editor,
  and fork pull requests too.
- **Hooks own**: formatting the edited file, and blocking edits to a migration that already
  exists. That second one is the only rule with no CI owner — the reasoning is at the top of
  `.claude/hooks/applied-migrations.mjs`.

There are **no exceptions**. Protected paths are enforced in CI only; there is deliberately no
local hook mirroring them, so an edit to an agent-owned file succeeds locally and fails on the
pull request. `scripts/protected-paths.mjs` is the shared matcher the CI check calls — it is not a
hook — and `scripts/verify-protected-paths.mjs` fails the build if a hook ever re-adds it, or if
any consumer hardcodes a second copy of the patterns.

**Do not enforce the same rule twice.** Two enforcement points is how they drift apart.

## Who owns which files

Two kinds of document, and the distinction decides who may write each one.

- **`docs/` is the rules.** How things must be built, and how they must be written down. Authored
  by people, stable, changed only when a decision changes. **The documentation agent never writes
  here** — with one exception, `docs/adr/`, where it may propose a record it must leave as
  `proposed`.
- **`AGENTS.md` files are the map.** What features exist and where they live. These change every
  time code lands, which is exactly why the agent maintains them and humans do not hand-edit them.

The path list lives in exactly one file, `.protected-paths.json`; never write a second copy of
those patterns anywhere.
