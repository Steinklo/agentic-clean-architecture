---
name: git-hygiene
description: Git rules for this repository - the type vocabulary for branches, commits and PR titles, how issues are linked, the protected-path check to run before staging, the merge strategy and why squash is forbidden here, and how to work alongside the documentation agent's [bot] commits. Use before staging or committing, when naming a branch, when opening a PR, or when [bot] commits appear on your branch.
---

# Git hygiene

Two of these rules are ordinary practice and stated briefly. Two are specific to this repository and
are the reason this file exists: **squash-merge is forbidden**, and **the documentation agent commits
to your branch**. Every command runs from the repository root.

## Type vocabulary

One of these prefixes on branches, commit subjects and PR titles:

| Type | For |
|---|---|
| `feature` | New user-visible functionality or a new capability. |
| `bug` | A defect in existing behaviour. |
| `refactor` | Internal restructuring with no behaviour change. |
| `docs` | Documentation only. |
| `chore` | Maintenance, dependency bumps, build and CI work. |

A change spanning types takes the dominant one.

## Branches

`<type>/<short-slug>`, lowercase and dash-separated. Never commit on `main` — every workflow here
triggers on `pull_request`, so work that never becomes one is work no check has ever seen.

```
feature/todoitem-added-event
bug/archive-allows-incomplete-items
docs/rules-and-maps-split
```

**The issue number is optional and buys less than it looks.** GitHub links an issue to a pull
request through body keywords or a branch created from the issue page — never by parsing a branch
name. Put the number in front (`feature/12-todoitem-added-event`) if it helps you scan; the linking
that matters happens in the pull request body.

## Commits

Subject: `<type>: short subject`, imperative, no trailing period, **72 characters or fewer**.

```
feature: raise an event when a TodoItem is added
bug: stop the archive guard passing on an empty list
docs: split the rules out of AGENTS.md
```

The body carries the *why*, and is where this repository's commits earn their keep — what broke,
what was ruled out, what a reader would otherwise rediscover. Footers:

- `Refs: #12` — an issue this relates to.
- `Co-authored-by: …` — **required on any commit an AI agent wrote**, naming that agent.

**Write no `Docs-Agent:` trailer.** It has exactly one producer, the commit step in
`.github/workflows/docs.yml`, and it is the loop guard's marker. A commit of yours carrying it is a
false provenance claim.

## Pull requests

Title in the same form as a commit subject: `<type>: short subject`.

Body from `.github/PULL_REQUEST_TEMPLATE.md`. **What** and **Why** are required; delete the optional
sections rather than leaving them empty. **This is where an issue gets linked** — `Closes #12` to
close it on merge, `Refs #12` to relate without closing.

## 1. Check the paths before you stage

Run the matcher over exactly what you are about to stage. It is the same matcher CI runs, so its
verdict is CI's verdict:

```bash
node scripts/protected-paths.mjs --check $(git diff --cached --name-only)
```

- Exit 0: stage and commit.
- Exit 1: it prints, per path, the owner, why it is guarded, and what to do instead. Usually:
  change the code and let the documentation agent regenerate the map on the pull request.
- Exit 2: the matcher could not run. That is not a verdict — fix the script rather than reading it
  as either answer.

**A decision record is yours to write.** `docs/adr/**` is *shared*, not agent-owned, so `--check`
passes on it: write one yourself, or state the decision and the alternative you rejected in the
pull request body and let the agent transcribe it. Only you can promote one to `accepted`.

`--check` answers "may **I** write this?". There is a second mode, `--classify`, which names the
owner instead — the documentation agent's self-check uses it, because "the agent may keep this" is
a different question with a different answer on a shared path. You want `--check`.

`.protected-paths.json` is the single source of truth for which paths are guarded. **Read it there.
Copy no pattern from it into any other file** — `node scripts/verify-protected-paths.mjs` fails the
build when a pattern literal appears a second time, and a second copy that drifts is the exact
failure the guard exists to prevent.

There is deliberately **no local hook** mirroring this, so an edit to an agent-owned file succeeds
locally and fails on the pull request. Never set `PROTECTED_PATHS_BYPASS` to get past it; that
variable belongs to the documentation agent's own workflow.

## 2. Merge strategy — merge commit, never squash

**Squash-merge is forbidden here**, and this is not a style preference.

The documentation agent pushes `[bot]`-authored commits onto your branch carrying `AGENTS.md` maps.
`.github/scripts/protected-paths-guard.sh` exempts them **by author identity**. Squashing collapses
them into one commit authored by *you* — so you become the author of agent-owned files, and
`protected-paths` fails the pull request.

So: **merge or rebase-merge**, never squash. One commit that is both yours and carries the agent's
files cannot exist.

## 3. The documentation agent's commits on your branch

On every push to a pull request branch, `.github/workflows/docs.yml` regenerates the maps and pushes
**its own commits onto your branch**, authored as `<app-slug>[bot]`. There are at most two per human
push, and they carry different trailers because they are different kinds of work:

| Commit | Trailer | What it is |
|---|---|---|
| `docs: regenerate for #N` | `Docs-Agent: regenerated` | Maps re-derived from the code. |
| `docs: propose a decision record for #N` | `Docs-Agent: proposed-record` | Newly authored prose, which can be wrong in ways a map cannot. Read it. |

The loop guard matches the `Docs-Agent:` **prefix**, so either one stops the next run from
regenerating this one's work.

The guarded files are legal in the agent's commits and illegal in yours, and that distinction
survives only while its commits keep their authorship. So:

- **Pull merge-style before every push** (`git pull --no-rebase origin <branch>`), so the agent's
  commit is a parent rather than something to replay.
- **Add new commits on top**, merging to bring in `main`, because the guard skips merge commits.
- **Push without force.** The agent does not force-push either, and a force-push discards its commit
  or makes its run fail mid-flight.

Anything that rewrites, absorbs or reverses one of its commits makes **you** the author of an
agent-owned file. That rules out `git rebase` over them, `git commit --amend` onto one, squash or
fixup absorbing one, and `git revert` of one — the revert is a new commit of yours touching guarded
paths. If an agent commit says something wrong, fix the code or `docs/DOC-RULES.md` and push; the
next run regenerates it.

## 4. History rewriting

- **Never on `main`.** A repository ruleset blocks force-push and deletion, so this is enforced
  rather than trusted.
- **Fine on your own branch** — rebase, squash, amend — **until the agent has pushed to it.** After
  that, see section 3.
- **Avoid force-pushing a branch under review.** If you must, say so in the pull request.

## 5. Failure modes

| Symptom | Cause | Fix |
|---|---|---|
| `protected-paths / guard` red, `human <sha>` listed with a map | You authored a guarded path, or absorbed a bot commit into yours | Drop the file from your commit; restore the bot commit from the reflog or let the next agent run recreate it |
| `docs / regenerate` red at the push step | You pushed while the agent was working; its non-fast-forward push failed | Nothing. Your push already started the next run |
| `docs / regenerate` says "already regenerated" | The head commit is the agent's | Expected. Push a commit of your own to trigger the next run |
| No `docs` commit appears at all | The pull request is from a fork, or no agent credential is configured | Expected. The job summary says which |
| An agent workflow passes in about a second | It refused to run: the branch's copy of the workflow differs from `main`, and that workflow lets the action authenticate itself | Expected for `pr-review` on a pull request that edits it. `docs` is unaffected — see `docs/gotchas.md` |
