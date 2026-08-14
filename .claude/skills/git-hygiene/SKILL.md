---
name: git-hygiene
description: The two git rules specific to this repository - the protected-path check to run before staging, and how to work alongside the documentation agent's [bot] commits on a pull request branch. Use before staging or committing, when a commit would touch a guarded documentation path, or when [bot] commits appear on your branch.
---

# Git hygiene

Branch naming, commit message format and what belongs in one commit are ordinary practice and are not written down here. Two things in this repository are not ordinary. Every command runs from the repository root.

Never commit on `main` — every workflow in this repository triggers on `pull_request`, so work that never becomes a pull request is work no check has ever seen.

When a GitHub issue authorises the work, name the branch `<issue-number>-<kebab-title>`: issue 7, "Archive a TodoList", becomes `7-archive-a-todolist`. The number is what lets `gh pr create` and the issue link themselves together, so keep it first.

## 1. Check the paths before you stage

Run the matcher over exactly what you are about to stage. It is the same matcher CI runs, so its verdict is CI's verdict:

```bash
node scripts/protected-paths.mjs --check $(git diff --cached --name-only)
```

- Exit 0: stage and commit.
- Exit 1: it prints, per path, the owner, why it is guarded, and what to do instead. Usually: change the code, or change `docs/DOC-RULES.md`, and let the documentation agent regenerate the file on the pull request.

`.protected-paths.json` is the single source of truth for which paths are guarded and which are carved out for humans. **Read it there. Copy no pattern from it into any other file** — `node scripts/verify-protected-paths.mjs` fails the build when a pattern literal appears a second time, and a second copy that drifts is the exact failure the guard exists to prevent.

There is deliberately **no local hook** mirroring this, so an edit to an agent-owned file succeeds locally and fails on the pull request. Never set `PROTECTED_PATHS_BYPASS` to get past it; that variable is the documentation agent's, set in `.github/workflows/docs.yml` and nowhere else.

**Write no `Docs-Agent:` trailer.** It is written by exactly one producer, the commit step in `.github/workflows/docs.yml`, and it is the loop guard's marker. A commit of yours carrying it is a false provenance claim on the branch.

## 2. The documentation agent's commits on your branch

On every push to a pull request branch, `.github/workflows/docs.yml` regenerates documentation and pushes **its own commit onto your branch**, authored as `<app-slug>[bot] <id+<app-slug>[bot]@users.noreply.github.com>` and carrying the `Docs-Agent: regenerated` trailer. One human push produces at most one agent commit.

`.github/scripts/protected-paths-guard.sh` decides exemption **per commit, by author identity**: an author matching `[bot]` is skipped, every other commit is checked. The guarded files are therefore legal in the agent's commits and illegal in yours, and that distinction survives only while the agent's commits keep their authorship.

So: **pull merge-style before every push** (`git pull --no-rebase origin <branch>`), so the agent's commit is a parent rather than something to replay; **add new commits on top**, merging to bring in `main`, because the guard skips merge commits; and **push without force** — the agent does not force-push either, and a force-push discards its commit or makes its run fail mid-flight. If an agent commit says something wrong, fix the code or `docs/DOC-RULES.md` and push; the next run regenerates it.

Anything that rewrites, absorbs or reverses one of the agent's commits makes **you** the author of an agent-owned file, and `protected-paths / guard` then fails the pull request. That rules out `git rebase` over them, `git commit --amend` onto one, squash or fixup absorbing one, `git revert` of one — the revert is a new commit of yours touching guarded paths — and squash-merging the pull request. Merge or rebase-merge instead, so no single commit both is yours and carries the agent's files.

## 3. Failure modes

| Symptom | Cause | Fix |
|---|---|---|
| `protected-paths / guard` red, `human <sha>` listed with a documentation file | You authored a guarded path, or absorbed a bot commit into yours | Drop the file from your commit; restore the bot commit from the reflog or let the next agent run recreate it |
| `docs / regenerate` red at the push step | You pushed while the agent was working; its non-fast-forward push failed | Nothing. Your push already started the next run |
| `docs / regenerate` says "already regenerated" | The head commit is the agent's | Expected. Push a commit of your own to trigger the next run |
| No `docs` commit appears at all | The pull request is from a fork; secrets are withheld | Expected. Documentation is regenerated after merge |
