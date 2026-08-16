#!/usr/bin/env node
// Protected-path guard -- the matcher the documentation guard is built on.
//
// The pattern list is NOT in this file. It is read from .protected-paths.json at
// the repository root, which is the single source of truth shared with the CI
// check (issue 14). Do not paste the patterns in here -- scripts/verify-protected-paths.mjs
// fails the moment a second copy appears.
//
// Two modes, and they answer DIFFERENT questions. Do not derive one from the other:
//   --check <path>...     -- "may a HUMAN write these?" Prints a violation report for
//                            each protected path. Exit 0 clean, 1 violation, 2 could not run.
//   --classify <path>...  -- "who owns each of these?" Prints one verdict per line, in
//                            argument order: agent-owned, shared-owned, human-owned,
//                            unguarded or outside-repo. Exit 0 always, 2 could not run.
//
// The second mode exists because ownership stopped being a boolean when decision records
// became shared. The documentation agent's self-check used to ask --check and invert the
// answer -- reading "not protected" as "the agent trespassed here". That inversion is
// wrong for a shared path, which both parties may write, and it is unreadable besides.
// Ask --classify and branch on the name.
//
// There is NO local-hook mode, and adding one would be a mistake. AGENTS.md states
// the rule: this guard is owned by CI and nothing else, because a hook binds Claude
// Code alone -- not another editor, not a colleague on another tool, not a plain
// `git commit` -- so a hook-only rule is a habit rather than a rule. A hook here
// would be a second enforcement point with its own definition, which is exactly
// the drift this guard exists to prevent. Read the guard's verdict, do not re-implement it:
// .github/scripts/protected-paths-guard.sh shells out to this script, and so does
// the git-hygiene skill.
//
// Escape hatch: PROTECTED_PATHS_BYPASS=1 turns a violation into a pass. The
// documentation agent sets it in .github/workflows/docs.yml; a maintainer sets it
// when setting the harness up for the first time. It never suppresses an exit 2.

import { readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";
import path from "node:path";

const HERE = path.dirname(fileURLToPath(import.meta.url));
// This file lives in <repo>/scripts, so the root is exactly one level up. Going
// up two lands outside the repository, where .protected-paths.json does not
// exist -- which fails every --check with an ENOENT that reads nothing like a
// path problem. verify-protected-paths.mjs pins this by calling the exported
// helpers with no repoRoot argument, so the default below is under test.
const REPO_ROOT = path.resolve(HERE, "..");
export const RULES_FILE = ".protected-paths.json";

export function loadRules(repoRoot = REPO_ROOT) {
  return JSON.parse(readFileSync(path.join(repoRoot, RULES_FILE), "utf8"));
}

/** Glob -> RegExp. `**` spans separators, `*` and `?` stay inside one segment. */
function globToRegExp(glob) {
  let out = "";
  for (let i = 0; i < glob.length; i++) {
    const c = glob[i];
    if (c === "*") {
      if (glob[i + 1] === "*") {
        i++;
        if (glob[i + 1] === "/") {
          i++;
          out += "(?:.*/)?"; // a doubled star plus separator also matches zero segments
        } else {
          out += ".*";
        }
      } else {
        out += "[^/]*";
      }
    } else if (c === "?") {
      out += "[^/]";
    } else {
      out += c.replace(/[.+^${}()|[\]\\]/g, "\\$&");
    }
  }
  return new RegExp(`^${out}$`, "i"); // case-insensitive: Windows paths, and over-blocking is the safe direction
}

const matchesAny = (relPath, patterns) =>
  patterns.find((p) => globToRegExp(p).test(relPath));

/** Absolute or relative path -> forward-slash repo-relative path, or null if outside the repo. */
export function toRepoRelative(filePath, repoRoot = REPO_ROOT) {
  if (!filePath) return null;
  const rel = path.relative(repoRoot, path.resolve(repoRoot, filePath));
  if (!rel || rel.startsWith("..") || path.isAbsolute(rel)) return null;
  return rel.split(path.sep).join("/");
}

/**
 * The whole rule, in one place, for both consumers.
 *
 * Precedence is humanOwned, then sharedOwned, then agentOwned, first match deciding.
 * `protected` answers only the human question -- "may a person write this?" -- so it is
 * false for a shared path. A caller asking what the AGENT may write must read `reason`
 * and accept both "agent-owned" and "shared-owned"; it must never negate `protected`.
 */
export function classify(filePath, rules = loadRules(), repoRoot = REPO_ROOT) {
  const relPath = toRepoRelative(filePath, repoRoot);
  if (relPath === null) return { relPath: filePath, protected: false, reason: "outside-repo" };

  const carveOut = matchesAny(relPath, rules.humanOwned.patterns);
  if (carveOut) return { relPath, protected: false, reason: "human-owned", pattern: carveOut };

  // Shared before agent-owned, because docs/adr/** matches both lists conceptually and
  // the shared answer is the permissive one. Ordering it after would re-protect records
  // against the people who have to promote them.
  const shared = matchesAny(relPath, rules.sharedOwned.patterns);
  if (shared) return { relPath, protected: false, reason: "shared-owned", pattern: shared };

  const guarded = matchesAny(relPath, rules.agentOwned.patterns);
  if (guarded) return { relPath, protected: true, reason: "agent-owned", pattern: guarded };

  return { relPath, protected: false, reason: "unguarded" };
}

/** Whether the documentation agent is allowed to leave a write at this path. */
export function agentMayWrite(verdict) {
  return verdict.reason === "agent-owned" || verdict.reason === "shared-owned";
}

export function explain(verdict, rules) {
  return [
    `Blocked: ${verdict.relPath} is owned by ${rules.agentOwned.owner}, not by you.`,
    ``,
    `It matched the agent-owned pattern \`${verdict.pattern}\` in ${RULES_FILE}.`,
    rules.agentOwned.reason,
    ``,
    `What to do instead:`,
    ...rules.guidance.map((g) => `  - ${g}`),
    ``,
    `Human-owned carve-outs you can always edit: ${rules.humanOwned.patterns.join(", ")}`,
    `Shared with the agent, and also yours to write: ${rules.sharedOwned.patterns.join(", ")}`,
  ].join("\n");
}

// ---------------------------------------------------------------- entry point

const bypassed = process.env.PROTECTED_PATHS_BYPASS === "1";

function main() {
  const args = process.argv.slice(2);
  const mode = args[0];

  if (mode !== "--check" && mode !== "--classify") {
    console.error("usage: node scripts/protected-paths.mjs --check|--classify <path> [<path> ...]");
    process.exit(2);
  }

  const rules = loadRules();

  // --classify reports ownership and never fails. It is not a guard, so
  // PROTECTED_PATHS_BYPASS does not apply to it -- there is no verdict to bypass.
  if (mode === "--classify") {
    for (const candidate of args.slice(1)) {
      console.log(classify(candidate, rules).reason);
    }
    process.exit(0);
  }

  let violations = 0;
  for (const candidate of args.slice(1)) {
    const verdict = classify(candidate, rules);
    if (verdict.protected) {
      violations++;
      console.error(explain(verdict, rules));
      console.error("");
    }
  }
  process.exit(violations > 0 && !bypassed ? 1 : 0);
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  try {
    main();
  } catch (err) {
    // Exit 2, not 1: the caller must be able to tell "a protected path was
    // touched" (1) from "this script could not run" (2). They looked identical
    // once already -- a bad REPO_ROOT made every invocation exit 1, which the
    // CI guard would have reported as a violation on every pull request.
    console.error(`protected-paths: ${err.message}`);
    process.exit(2);
  }
}
