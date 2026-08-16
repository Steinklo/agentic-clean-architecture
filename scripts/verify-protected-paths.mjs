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
import { agentMayWrite, classify, loadRules, RULES_FILE } from "./protected-paths.mjs";

const REPO_ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const MATCHER = "scripts/protected-paths.mjs";
const SETTINGS = ".claude/settings.json";

const failures = [];
const check = (name, ok, detail) => {
  if (!ok) failures.push(detail ? `${name}\n    ${detail}` : name);
  console.log(`${ok ? "PASS" : "FAIL"}  ${name}`);
};

const read = (rel) => readFileSync(path.join(REPO_ROOT, rel), "utf8");

// A git worktree under .claude/worktrees/ is a complete second checkout of this
// repository, so walking into one finds .protected-paths.json and this very file
// again and reports them as consumers hardcoding the patterns. They are neither
// consumers nor copies -- they are the same files, checked out twice. The agent
// harness creates these on its own, so anyone running this locally would hit it.
const SKIP_DIRECTORIES = new Set(["worktrees", "node_modules", ".git"]);

function walk(dir, acc = []) {
  if (!existsSync(dir)) return acc;
  for (const entry of readdirSync(dir)) {
    const full = path.join(dir, entry);
    if (statSync(full).isDirectory()) {
      if (SKIP_DIRECTORIES.has(entry)) continue;
      walk(full, acc);
    } else {
      acc.push(path.relative(REPO_ROOT, full).split(path.sep).join("/"));
    }
  }
  return acc;
}

// --- 1. the list exists, in one file, and is complete ------------------------

check(`${RULES_FILE} exists`, existsSync(path.join(REPO_ROOT, RULES_FILE)));

const rules = loadRules(REPO_ROOT);

// Every list, by name. A category added to .protected-paths.json and not added here
// is the quiet failure this file exists to prevent: its patterns would escape the
// copy-detection below, so a consumer could fork them and nothing would say so.
const OWNERSHIP_LISTS = ["agentOwned", "sharedOwned", "humanOwned"];

const listsInFile = Object.keys(rules).filter((key) => Array.isArray(rules[key]?.patterns));

check(
  `every ownership list in ${RULES_FILE} is known to this checker`,
  listsInFile.length === OWNERSHIP_LISTS.length &&
    OWNERSHIP_LISTS.every((name) => listsInFile.includes(name)),
  `this checker knows ${OWNERSHIP_LISTS.join(", ")}; the file declares ${listsInFile.join(", ")}`
);

const patterns = OWNERSHIP_LISTS.flatMap((name) => rules[name]?.patterns ?? []);

// The agent writes maps. Records are shared with people. Nothing else. If agentOwned
// ever grows to include docs/ or .claude/, the agent can rewrite the rules it is
// measured against, which is the arrangement this whole guard exists to refuse.
check(
  "the list names the maps the agent may write, and nothing that states a rule",
  ["AGENTS.md", "**/AGENTS.md", "CLAUDE.md"].every((p) => rules.agentOwned.patterns.includes(p)) &&
    rules.sharedOwned.patterns.includes("docs/adr/**") &&
    rules.humanOwned.patterns.includes("docs/adr/TEMPLATE.md") &&
    !rules.agentOwned.patterns.some((p) => p === "docs/**" || p === ".claude/**") &&
    !rules.sharedOwned.patterns.some((p) => p.startsWith("docs/") && p !== "docs/adr/**"),
  `agentOwned=${rules.agentOwned.patterns} sharedOwned=${rules.sharedOwned.patterns} humanOwned=${rules.humanOwned.patterns}`
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

  // Records: shared, so a human writing one is NOT a violation. This is the pair that
  // makes a record promotable to `accepted` at all -- guarding it would mean only the
  // agent could ever write there, and only a person may promote.
  ["docs/adr/0007-some-decision.md", false, "shared: either party may author a record"],
  ["docs/adr/nested/note.md", false, "docs/adr/** spans segments, and is shared"],

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

// --- 3b. the two consumers ask different questions ---------------------------
//
// The self-check in docs.yml used to compute "may the agent keep this?" by negating
// `protected`. That worked only while ownership was a boolean. It is not: a shared
// path answers "yes" to BOTH questions, and an unguarded path answers "no" to the
// agent's while answering "yes" to the human's. Anything that reintroduces the
// inversion breaks here rather than silently discarding a record the agent was asked
// to write.

const agentWriteCases = [
  ["AGENTS.md", true, "the agent's own map"],
  ["src/Todo.Domain/AGENTS.md", true, "a layer map"],
  ["docs/adr/0007-some-decision.md", true, "shared: the agent may propose a record"],
  ["docs/adr/TEMPLATE.md", false, "the record form is the humans', not the agent's"],
  ["docs/architecture.md", false, "a rule"],
  [".claude/skills/new-feature/SKILL.md", false, "a procedure people author"],
  ["src/Todo.Domain/TodoList.cs", false, "code: unguarded, and still not the agent's"],
  ["README.md", false, "unguarded, and still not the agent's"],
];

const agentWrong = agentWriteCases
  .map(([p, expected, why]) => [p, expected, why, agentMayWrite(classify(p, rules, REPO_ROOT))])
  .filter(([, expected, , actual]) => expected !== actual)
  .map(([p, expected, why, actual]) => `${p}: expected agentMayWrite=${expected} (${why}), got ${actual}`);

check(
  `the agent-write question is answered directly, not by negating "protected" (${agentWriteCases.length} cases)`,
  agentWrong.length === 0,
  agentWrong.join("\n    ")
);

// The proof that the two questions are genuinely different, stated as an assertion so
// that collapsing them back into one boolean cannot pass.
const sharedExample = "docs/adr/0007-some-decision.md";
const unguardedExample = "README.md";
check(
  "a shared path is writable by both parties, and an unguarded path by only one",
  classify(sharedExample, rules, REPO_ROOT).protected === false &&
    agentMayWrite(classify(sharedExample, rules, REPO_ROOT)) === true &&
    classify(unguardedExample, rules, REPO_ROOT).protected === false &&
    agentMayWrite(classify(unguardedExample, rules, REPO_ROOT)) === false,
  `${sharedExample} and ${unguardedExample} are both unprotected, so "protected" alone cannot tell them apart`
);

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
