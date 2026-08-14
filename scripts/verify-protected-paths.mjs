#!/usr/bin/env node
// Anti-drift check for the protected-path guard.
//
// The specific failure this exists to prevent: "The guarded path
// list is read by the CI workflow, which owns this rule outright, and must
// be read from one shared source, or the local and remote guards will disagree."
//
// So this script asserts four things:
//   1. .protected-paths.json is the only file that spells the patterns out.
//   2. Every consumer reaches the rule through the shared module, not a copy.
//   3. The precedence rule (carve-outs beat guarded patterns) actually holds.
//   4. The shared module finds that file the way the shell callers invoke it,
//      with no repoRoot injected -- see the note above check 4.
//
// Run it: node scripts/verify-protected-paths.mjs
// CI (issue 14) should run it too, as a step in the protected-path workflow.

import { readFileSync, existsSync, readdirSync, statSync } from "node:fs";
import { fileURLToPath } from "node:url";
import path from "node:path";
import { classify, loadRules, RULES_FILE } from "./protected-paths.mjs";

const REPO_ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const MATCHER = "scripts/protected-paths.mjs";
const SETTINGS = ".claude/settings.json";

const failures = [];
const check = (name, ok, detail) => {
  if (!ok) failures.push(detail ? `${name}\n    ${detail}` : name);
  console.log(`${ok ? "PASS" : "FAIL"}  ${name}`);
};

const read = (rel) => readFileSync(path.join(REPO_ROOT, rel), "utf8");

function walk(dir, acc = []) {
  if (!existsSync(dir)) return acc;
  for (const entry of readdirSync(dir)) {
    const full = path.join(dir, entry);
    if (statSync(full).isDirectory()) walk(full, acc);
    else acc.push(path.relative(REPO_ROOT, full).split(path.sep).join("/"));
  }
  return acc;
}

// --- 1. the list exists, in one file, and is complete ------------------------

check(`${RULES_FILE} exists`, existsSync(path.join(REPO_ROOT, RULES_FILE)));

const rules = loadRules(REPO_ROOT);
const patterns = [...rules.agentOwned.patterns, ...rules.humanOwned.patterns];

// The agent writes maps, and adds decision records. Nothing else. If this list
// ever grows to include docs/ or .claude/, the agent can rewrite the rules it is
// measured against, which is the arrangement this whole guard exists to refuse.
check(
  "the list names the maps the agent may write, and nothing that states a rule",
  ["AGENTS.md", "**/AGENTS.md", "CLAUDE.md", "docs/adr/**"].every((p) => rules.agentOwned.patterns.includes(p)) &&
    rules.humanOwned.patterns.includes("docs/adr/TEMPLATE.md") &&
    !rules.agentOwned.patterns.some((p) => p === "docs/**" || p === ".claude/**"),
  `agentOwned=${rules.agentOwned.patterns} humanOwned=${rules.humanOwned.patterns}`
);

// --- 2. nobody keeps a second copy -------------------------------------------

const consumerFiles = [...walk(path.join(REPO_ROOT, ".claude")), ...walk(path.join(REPO_ROOT, ".github"))].filter(
  (f) => /\.(mjs|js|cjs|json|ya?ml|ps1|sh)$/i.test(f)
);

const copies = [];
for (const file of consumerFiles) {
  const body = read(file);
  // A glob pattern quoted verbatim in a consumer is a fork of the rule.
  const found = patterns.filter((p) => p.includes("*") && body.includes(p));
  if (found.length) copies.push(`${file}: ${found.join(", ")}`);
}
check(
  `no consumer hardcodes a copy of the patterns (checked ${consumerFiles.length} files)`,
  copies.length === 0,
  copies.join("\n    ")
);

check(
  `the matcher reads ${RULES_FILE} instead of embedding the list`,
  read(MATCHER).includes(RULES_FILE),
  `${MATCHER} never mentions ${RULES_FILE}`
);

// The matcher has exactly one mode, --check, and CI owns this rule outright.
// Registering it as a hook of any kind would make Claude Code a second
// enforcement point with its own definition -- the drift this guard exists to
// prevent. Every hook event is checked, not just PreToolUse: the mistake this
// guards against is re-adding the local mirror, and which event it hangs off
// makes no difference to that.
check(
  `${SETTINGS} does not re-add a local mirror of the guard`,
  (() => {
    const settings = JSON.parse(read(SETTINGS));
    const cmds = Object.values(settings.hooks ?? {})
      .flat()
      .flatMap((g) => g.hooks ?? [])
      .map((h) => h.command ?? "");
    return !cmds.some((c) => c.includes("protected-paths.mjs"));
  })(),
  "a hook invokes protected-paths.mjs; CI owns this rule, and a hook binds Claude Code only"
);

const workflows = walk(path.join(REPO_ROOT, ".github", "workflows"));
check(
  workflows.length
    ? "the CI check delegates to the shared script (issue 14)"
    : "the CI check delegates to the shared script (issue 14) -- skipped, no workflows yet",
  workflows.length === 0 ||
    workflows.some((w) => read(w).includes("protected-paths.mjs") || read(w).includes(RULES_FILE)),
  "a workflow exists but none reads the shared list; it must call `node scripts/protected-paths.mjs --check <paths>`"
);

// --- 3. precedence: carve-outs win -------------------------------------------

const cases = [
  // Maps: the agent's to write, so a human editing one fails.
  ["AGENTS.md", true, "the root map, guarded verbatim"],
  ["src/Todo.Domain/AGENTS.md", true, "a layer map, guarded by **/AGENTS.md"],
  ["src/Todo.Api/AGENTS.md", true, "a layer map, guarded by **/AGENTS.md"],
  ["CLAUDE.md", true, "guarded verbatim"],
  ["docs/adr/0007-some-decision.md", true, "the agent proposes records"],
  ["docs/adr/nested/note.md", true, "docs/adr/** spans segments"],

  // Rules: people's to write, so the agent must not touch them.
  ["docs/architecture.md", false, "a rule, not a map"],
  ["docs/conventions.md", false, "a rule, not a map"],
  ["docs/gotchas.md", false, "a rule, not a map"],
  ["docs/layers/Todo.Domain.md", false, "a layer's rules, not its map"],
  ["docs/DOC-RULES.md", false, "the form the agent writes maps in"],
  ["docs/adr/TEMPLATE.md", false, "the record form; the agent fills it in, never redesigns it"],
  [".claude/settings.json", false, "the hooks that constrain the agent"],
  [".claude/skills/new-feature/SKILL.md", false, "a procedure people author"],
  [RULES_FILE, false, "the rules file itself must stay human-editable"],
  ["src/Todo.Domain/TodoList.cs", false, "code"],
  ["README.md", false, "unguarded"],
  ["docs/adrs/notes.md", false, "docs/adr/** must not match a sibling prefix"],
  ["src/Todo.Domain/AGENTS.md.bak", false, "**/AGENTS.md must match the whole segment"],
];

const wrong = cases
  .map(([p, expected, why]) => [p, expected, why, classify(p, rules, REPO_ROOT).protected])
  .filter(([, expected, , actual]) => expected !== actual)
  .map(([p, expected, why, actual]) => `${p}: expected protected=${expected} (${why}), got ${actual}`);

check(`precedence holds across ${cases.length} cases (carve-outs beat guarded patterns)`, wrong.length === 0, wrong.join("\n    "));

// --- 4. the matcher finds the rules file on its own --------------------------
//
// Every check above passes an explicit REPO_ROOT, which is what a caller inside
// this repository can do and what neither real consumer does: the git-hygiene
// skill and .github/scripts/protected-paths-guard.sh both shell out to
// `node scripts/protected-paths.mjs --check <paths>`, where the module's own
// default decides where the rules file is. That default was wrong -- it resolved
// two levels up, landing outside the repository -- and every check here still
// passed, because injecting a correct root is exactly what hid it. The result
// was an ENOENT on every invocation, reported by the CI guard as if a protected
// path had been touched.
//
// So this calls the exported helpers the way the shell does: with no root at all.

let defaultRootWorks = false;
let defaultRootDetail = "";
try {
  const viaDefault = loadRules();
  defaultRootWorks =
    classify("AGENTS.md", viaDefault).protected === true &&
    classify("docs/architecture.md", viaDefault).protected === false;
  if (!defaultRootWorks) defaultRootDetail = "the rules file was found, but classification through the default root disagrees";
} catch (err) {
  defaultRootDetail = `${err.message}\n    REPO_ROOT in ${MATCHER} does not point at this repository`;
}

check("the matcher resolves the rules file with no repoRoot argument, as the shell callers invoke it", defaultRootWorks, defaultRootDetail);

// -----------------------------------------------------------------------------

if (failures.length) {
  console.error(`\n${failures.length} check(s) failed:\n  - ${failures.join("\n  - ")}`);
  process.exit(1);
}
console.log("\nProtected-path guard is wired to a single source of truth.");
