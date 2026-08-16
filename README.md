# 🛡️ Agentic Clean Architecture

**Clean architecture for .NET 10 — shipped with the agent harness that keeps it that way.**

---

## 😐 The problem

Every project starts with good conventions and a document describing them. A few months later that
document is fiction — and nothing ever broke.

That's the point. Layering erodes one reasonable-looking commit at a time, CI stays green
throughout, and the file explaining the architecture quietly stops describing it.

So this repository ships a harness with exactly two jobs:

> - ✅ **Keep the documentation true.**
> - ✅ **Keep changes inside the architecture.**

Neither is a convention anyone has to remember. Both are checks that fail.

---

## ⚙️ How it works

### 📘 Rules, and 🗺️ maps

|  |  |  |
|---|---|---|
| **Rules** | `docs/` | How things must be built. **Yours** — the agent reads them and never writes them. |
| **Maps** | `AGENTS.md` | What exists and where. **The agent's** — rewritten whenever code changes. |

An agent that could edit the rules could rewrite the standard it's measured against. It can't, and
that's enforced both ways: CI fails a human commit that edits a map, and the documentation agent
refuses to push if it wrote outside one.

### 🧪 The rules are tests

Not prose — assertions. *No ORM type outside Infrastructure. Every validator is public. Every
request answers with `Result`.* A rule that couldn't be written as a test was demoted to advice,
and labelled as advice.

> [!TIP]
> **A rule can't pass by accident, either.** Each one declares how much it currently proves. A rule
> examining nothing reports `VACUOUS` rather than green. A rule examining a single example calls
> itself `Thin`, instead of implying it proved a convention.
>
> Green means something specific here.

### 🚦 On every pull request

<details>
<summary><b>The checks that run, and what each is for</b></summary>

<br>

|  |  |
|---|---|
| `architecture` | The rules, plus a check that every rule named in prose still exists. No container, seconds. |
| `pr-build` | Builds and runs every test suite. |
| `protected-paths` | Fails when a human-authored commit edits a map. |
| `docs` | Regenerates the maps from the code — and refuses to push if it wrote anywhere else. |
| `pr-review` | Reviews the diff against `docs/`, before a human spends attention on it. |
| `build` | The single build definition the others call. |

`protected-paths` and `docs` are two halves of one idea: the map can't be hand-edited, and can't go
stale either, because it's rewritten on the same pull request that changed the code. A reviewer
sees both together, and they can't merge contradicting each other.

</details>

### 🧰 While you work

<details>
<summary><b>Skills, hooks, and the guards behind the guards</b></summary>

<br>

**Skills** carry the procedures that would otherwise be reinvented every time — adding a feature,
writing a migration, committing work.

**Hooks** run on every edit: one formats the file, the other refuses to change a migration that has
already been applied.

**And the guards are guarded.** One script checks the protected-path list exists in exactly one
copy. Another checks that every rule cited in prose still resolves. A third is the documentation
agent running its own output past that same list before pushing — so *"the agent may not edit the
rules"* is a check, not a sentence in a prompt.

</details>

---

<details>
<summary>🧱 <b>The example app</b> — deliberately small</summary>

<br>

A `TodoList` owns `TodoItem`s and refuses to be archived while any of them is incomplete. That's
the whole domain: one real cross-item invariant, so the aggregate boundary earns its keep.

It's here to demonstrate the patterns, not to be a product. Rename it and it's yours.

</details>

<details>
<summary>🍴 <b>Using this as a template</b></summary>

<br>

The code builds and every test passes the moment you clone, and most of the harness runs with no
setup at all — the architecture rules, the build, the tests and the protected-path guard need no
credentials and work on forks.

Only the two agent workflows need wiring up: a GitHub App, and a credential for whichever agent
you point them at. [`docs/setup.md`](docs/setup.md) covers both, including why the built-in
`GITHUB_TOKEN` cannot be used for the push.

⚠️ **On ARM** (Apple Silicon, Snapdragon X) — SQL Server containers are x86-64 only and Microsoft
supports no emulation, so the integration suite can't run natively. The other suites are fine.

</details>
