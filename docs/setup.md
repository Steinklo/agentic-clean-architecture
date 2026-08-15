# Setting up the agent workflows

Four of the six workflows need nothing: `architecture`, `pr-build`, `protected-paths` and `build`
run on a fresh clone, on forks, for anyone. Everything in [`architecture.md`](architecture.md) is
enforced without any of what follows.

Two need credentials, because they run an AI agent: **`docs`** regenerates the maps, and
**`pr-review`** reviews the diff. This file is what those two need and why.

## The GitHub App, and why the built-in token will not do

Every workflow gets a `GITHUB_TOKEN` for free. It is not enough here, for one specific reason:

> **A push made with `GITHUB_TOKEN` does not retrigger workflows.**

So when the documentation agent pushes its commit onto a pull request branch, the build, the tests
and the architecture rules would not run again. They would sit green, reporting on the *previous*
commit, while a different commit is what merges. A green tick describing code you are not shipping
is worse than no tick at all.

An App's installation token has no such restriction. That is the whole reason one is required —
one narrow problem, one narrow fix. **This is independent of which agent you run**; it is a
property of GitHub, not of the model.

Create an App on your own account with exactly three permissions:

| Permission | Level | Why |
|---|---|---|
| Contents | Read and write | check out the branch, push the regenerated maps |
| Pull requests | Read and write | read pull request context, post review comments |
| Metadata | Read-only | mandatory for every GitHub App |

Install it on **only** this repository, and leave webhooks off — nothing here consumes them.

Then store its id and private key as repository secrets:

```bash
gh secret set AGENT_APP_ID          # the App's numeric id
gh secret set AGENT_APP_PRIVATE_KEY < path/to/key.pem
```

Delete the downloaded `.pem` afterwards; GitHub has the secret and the file is a spare copy.

## The agent credential

`docs.yml` and `pr-review.yml` currently run Claude through `anthropics/claude-code-action`, which
needs one of:

- `CLAUDE_CODE_OAUTH_TOKEN` — a subscription token, minted by `claude setup-token`. Bills against
  one person's plan.
- `ANTHROPIC_API_KEY` — billed per token against organisation credits. Swap the
  `claude_code_oauth_token:` line in each workflow for `anthropic_api_key:`.

They are **alternatives, not a pair**; setting both is not a fallback.

> **A known unknown worth weighing.** Whether a GitHub Actions run authenticated by
> `CLAUDE_CODE_OAUTH_TOKEN` shares a rate-limit budget with your *interactive* sessions is not
> publicly documented. The documentation agent triggers on every pull request commit, so this is
> the likeliest unpleasant surprise here. An API key avoids the question.

If you swap these two workflows to a different agent, this section is the only part that changes.
The App above stays, because the retrigger problem stays.

## Checking what is actually configured

Nothing here verifies itself, so check by hand before wondering why a workflow declined:

```bash
gh secret list                      # AGENT_APP_ID, AGENT_APP_PRIVATE_KEY, and one agent credential
gh api repos/{owner}/{repo}/installation --jq .app_slug   # the App, if installed
```

Both agent workflows also say what they are missing in their job summary on every pull request, so
a run that declined is usually quicker to read than either command.

## Optional: make the guard required

The protected-path check ships as an ordinary check rather than branch protection, so it works the
moment someone clones the template with no configuration at all. Promoting it to a required check
is a repository ruleset on the default branch — reversible, and entirely your call.
