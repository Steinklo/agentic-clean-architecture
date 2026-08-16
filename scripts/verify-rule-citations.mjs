#!/usr/bin/env node
// Every `Rules.<Name>` cited in prose names a rule that actually exists.
//
// WHY THIS EXISTS. The instruction files used to restate each rule in their own
// words, which meant the same rule was written in four or five places and drifted:
// .github/workflows/pr-review.yml came to tell the review agent that error codes
// live in a central `*Errors` class, which this repository explicitly rejects.
// The fix was to stop restating and start citing -- prose names the rule, and
// tests/Todo.ArchitectureTests/Rules.cs owns what it means.
//
// That trade swaps one failure mode for another. A restated rule goes stale
// silently; a citation goes stale silently too, if the rule is renamed or
// removed and the prose is not. This closes that, and only that: it checks the
// name resolves, never that the surrounding sentence is true.
//
// Run it:  node scripts/verify-rule-citations.mjs
// CI runs it in .github/workflows/architecture.yml, beside the suite it guards.

import { readFileSync, readdirSync, statSync, existsSync } from "node:fs";
import { fileURLToPath } from "node:url";
import path from "node:path";

const REPO_ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const RULES_FILE = "tests/Todo.ArchitectureTests/Rules.cs";

// Where a citation may appear. Anything an agent or a reviewer reads as instruction.
const CITING_PLACES = ["AGENTS.md", "CLAUDE.md", "README.md", "src", ".claude", ".github", "docs"];
const CITING_EXTENSIONS = /\.(md|ya?ml)$/i;

const read = (relative) => readFileSync(path.join(REPO_ROOT, relative), "utf8");

function walk(absolute, found = []) {
  if (!existsSync(absolute)) return found;

  if (!statSync(absolute).isDirectory()) {
    found.push(path.relative(REPO_ROOT, absolute).split(path.sep).join("/"));
    return found;
  }

  for (const entry of readdirSync(absolute)) {
    if (entry === "node_modules" || entry === "bin" || entry === "obj") continue;
    walk(path.join(absolute, entry), found);
  }

  return found;
}

// `public static ArchitectureRule <Name> { get; }` -- the declaration form Rules.cs uses.
const declared = new Set(
  [...read(RULES_FILE).matchAll(/public\s+static\s+ArchitectureRule\s+(\w+)\s*\{/g)].map((m) => m[1])
);

if (declared.size === 0) {
  console.error(`FAIL  no rules found in ${RULES_FILE}.`);
  console.error("      The declaration pattern this script matches has changed, and every");
  console.error("      citation would now look broken. Fix the pattern, not the citations.");
  process.exit(1);
}

const files = CITING_PLACES.flatMap((place) => walk(path.join(REPO_ROOT, place))).filter((file) =>
  CITING_EXTENSIONS.test(file)
);

const broken = [];
const cited = new Set();

for (const file of files) {
  const lines = read(file).split(/\r?\n/);

  lines.forEach((line, index) => {
    for (const [, name] of line.matchAll(/\bRules\.([A-Z]\w*)/g)) {
      cited.add(name);
      if (!declared.has(name)) broken.push(`${file}:${index + 1}  Rules.${name}`);
    }
  });
}

console.log(`Checked ${files.length} files against ${declared.size} rules in ${RULES_FILE}.`);

if (broken.length > 0) {
  console.error(`\nFAIL  ${broken.length} citation(s) name a rule that does not exist:`);
  for (const line of broken) console.error(`  - ${line}`);
  console.error("\nEither the rule was renamed or removed and the prose was not updated, or the");
  console.error("citation is a typo. Prose cites; Rules.cs owns. Fix the citation to match.");
  process.exit(1);
}

console.log(`PASS  all ${cited.size} cited rules exist.`);

// Not a failure: a rule nobody points at still runs and still fails. But it is
// harder to find, and a rule an agent never reads about is a rule it learns only
// by breaking. Reported so the gap is visible rather than discovered.
const uncited = [...declared].filter((name) => !cited.has(name));

if (uncited.length > 0) {
  console.log(`\nNote: ${uncited.length} rule(s) are enforced but cited nowhere in prose:`);
  for (const name of uncited) console.log(`  - Rules.${name}`);
}
