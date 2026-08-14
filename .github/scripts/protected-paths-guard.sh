#!/usr/bin/env bash
#
# CI half of the documentation guard (issue 14).
#
# WHAT THIS DOES NOT DO: it does not know which paths are guarded, and it must
# never learn. The list lives in exactly one file, .protected-paths.json, and is
# interpreted by exactly one matcher, scripts/protected-paths.mjs, which
# the local PreToolUse hook uses too. This script only decides *which files to
# hand that matcher*, then reports what it says. The guard's design calls out two copies
# of the list drifting apart as the failure mode to avoid, and
# scripts/verify-protected-paths.mjs fails the build if a pattern literal is
# ever pasted into this directory.
#
# Inputs (environment):
#   BASE_SHA     required. The commit the pull request is based on.
#   HEAD_SHA     optional, default HEAD. The tip being proposed.
#   BOT_AUTHORS  optional, default '\[bot\]'. Case-insensitive extended regex
#                matched against "Author Name <author@email>" of each commit.
#
# ------------------------------------------------------------------ issue 15
# HOW THE DOCUMENTATION AGENT STAYS UNBLOCKED
#
# Exemption here is decided **per commit, by author identity** -- not by
# PROTECTED_PATHS_BYPASS. A commit whose author matches BOT_AUTHORS is skipped
# entirely; its files are never offered to the matcher. Every other commit in
# the pull request is still checked, so a human cannot smuggle an edit in behind
# the agent's push.
#
# So the documentation agent must *author* (not merely push) its regeneration
# commits as its bot identity. Committing with the GitHub App identity does this
# for free -- e.g. `my-app[bot] <1234567+my-app[bot]@users.noreply.github.com>`
# -- because the default pattern matches the "[bot]" suffix that GitHub App and
# github-actions identities carry. If the agent commits under a plain human
# identity instead, this check will fail its own pull request. If it needs a
# different identity, set BOT_AUTHORS in the workflow rather than changing code
# here.
#
# PROTECTED_PATHS_BYPASS=1 is the *other* half of the agent's exemption: it
# belongs in the agent's own workflow, where Claude Code edits agent-owned files
# and the local hook would otherwise deny the write. Do not set it in this
# workflow -- the matcher honours it in --check mode too, so setting it here
# would silently turn this guard into a no-op.
# -----------------------------------------------------------------------------

set -euo pipefail

MATCHER="scripts/protected-paths.mjs"

BASE_SHA="${BASE_SHA:?BASE_SHA is required (the base commit of the pull request)}"
HEAD_SHA="${HEAD_SHA:-HEAD}"
BOT_AUTHORS="${BOT_AUTHORS:-\[bot\]}"

cd "$(git rev-parse --show-toplevel)"

# A shallow clone has neither the base commit nor the pull request's own
# commits, so the workflow checks out full history. Recover anyway if it did not.
if ! git cat-file -e "${BASE_SHA}^{commit}" 2>/dev/null; then
  echo "Base commit ${BASE_SHA} is missing from this clone; fetching it."
  git fetch --no-tags --quiet origin "${BASE_SHA}"
fi

# Commits proposed by this pull request: reachable from the head, not from the
# base tip. Anything already on the base branch -- including commits merged in
# from it after the branch started -- is excluded, so the pull request is only
# ever judged on its own work. Merge commits carry no authorship of their own
# and their content is attributed to the commits they merge, so they are skipped.
commits=()
while IFS= read -r line; do
  [ -n "${line}" ] && commits+=("${line}")
done < <(git rev-list --no-merges "${BASE_SHA}..${HEAD_SHA}")

if [ ${#commits[@]} -eq 0 ]; then
  echo "No non-merge commits between ${BASE_SHA} and ${HEAD_SHA}; nothing to check."
  exit 0
fi

echo "Commits in this pull request:"
changed=()
human_commits=0
for sha in "${commits[@]}"; do
  author="$(git log -1 --pretty='%an <%ae>' "${sha}")"
  subject="$(git log -1 --pretty='%s' "${sha}")"
  if printf '%s' "${author}" | grep -qiE "${BOT_AUTHORS}"; then
    printf '  exempt  %.8s  %s  [bot author: %s]\n' "${sha}" "${subject}" "${author}"
    continue
  fi
  printf '  human   %.8s  %s  [%s]\n' "${sha}" "${subject}" "${author}"
  human_commits=$((human_commits + 1))
  while IFS= read -r -d '' file; do
    [ -n "${file}" ] && changed+=("${file}")
  done < <(git log -1 --no-renames --pretty=format: --name-only -z "${sha}")
done

if [ ${human_commits} -eq 0 ]; then
  echo
  echo "Every commit is bot-authored; the documentation agent is exempt from its own rule."
  exit 0
fi

if [ ${#changed[@]} -eq 0 ]; then
  echo
  echo "No files touched by a human-authored commit; nothing to check."
  exit 0
fi

files=()
while IFS= read -r line; do
  [ -n "${line}" ] && files+=("${line}")
done < <(printf '%s\n' "${changed[@]}" | sort -u)

echo
echo "Files touched by human-authored commits (${#files[@]}):"
printf '  %s\n' "${files[@]}"
echo

# The single source of truth decides. Exit 1 and a per-path report on stderr
# means at least one path is protected; the report already names the path, its
# owner and what to do instead, taken from .protected-paths.json, so both this
# check and the local hook say exactly the same thing.
report="$(mktemp)"
if node "${MATCHER}" --check "${files[@]}" 2>"${report}"; then
  echo "OK: no protected path was touched by a human-authored commit."
  exit 0
fi

cat "${report}" >&2

if [ -n "${GITHUB_STEP_SUMMARY:-}" ]; then
  {
    echo '### Protected-path guard failed'
    echo
    echo 'A human-authored commit in this pull request edits a file the documentation agent owns.'
    echo
    echo '```'
    cat "${report}"
    echo '```'
  } >>"${GITHUB_STEP_SUMMARY}"
fi

exit 1
