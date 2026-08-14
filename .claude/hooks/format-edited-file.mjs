#!/usr/bin/env node
// PostToolUse formatter.
//
// Runs `dotnet format` over exactly ONE file -- the one that was just edited --
// so that diffs carry no style noise. Deliberately NOT solution-wide: a
// whole-solution format on every edit would cost seconds per tool call.
//
// Uses the `whitespace` subcommand in `--folder` mode: it applies .editorconfig
// without loading MSBuild, restoring, or compiling, so it stays sub-second and
// still works while the code is mid-edit and does not build.

import { spawn } from "node:child_process";
import { fileURLToPath } from "node:url";
import path from "node:path";

const REPO_ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..", "..");
const TIMEOUT_MS = 20_000;

const readStdin = async () => {
  const chunks = [];
  for await (const chunk of process.stdin) chunks.push(chunk);
  return Buffer.concat(chunks).toString("utf8");
};

const payload = await readStdin()
  .then((raw) => JSON.parse(raw))
  .catch(() => null);

const filePath = payload?.tool_input?.file_path;
if (!filePath || path.extname(filePath).toLowerCase() !== ".cs") process.exit(0);

const rel = path.relative(REPO_ROOT, path.resolve(REPO_ROOT, filePath));
if (!rel || rel.startsWith("..") || path.isAbsolute(rel)) process.exit(0);
if (/(^|[\\/])(bin|obj)([\\/]|$)/i.test(rel)) process.exit(0);

const child = spawn(
  "dotnet",
  [
    "format",
    "whitespace",
    REPO_ROOT,
    "--folder",
    "--include",
    rel.split(path.sep).join("/"),
    "--verbosity",
    "quiet",
  ],
  { cwd: REPO_ROOT, stdio: ["ignore", "ignore", "pipe"], shell: process.platform === "win32" }
);

let stderr = "";
child.stderr.on("data", (d) => (stderr += d));

const kill = setTimeout(() => child.kill(), TIMEOUT_MS);
child.on("error", () => process.exit(0)); // no dotnet on PATH: formatting is a nicety, never a blocker
child.on("close", (code) => {
  clearTimeout(kill);
  if (code !== 0 && stderr.trim()) console.error(`dotnet format: ${stderr.trim()}`);
  process.exit(0);
});
