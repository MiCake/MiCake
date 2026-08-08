#!/usr/bin/env node
"use strict";

// sources/scripts/lib/workspace-artifacts.js
var import_node_fs = require("node:fs");
var import_node_path = require("node:path");
function findProjectRoot(startPath = process.cwd()) {
  let current = (0, import_node_path.resolve)(startPath);
  while (true) {
    if ((0, import_node_fs.existsSync)((0, import_node_path.join)(current, ".ai-agents"))) return current;
    const parent = (0, import_node_path.dirname)(current);
    if (parent === current) return null;
    current = parent;
  }
}
function toWorkspaceRelative(projectRoot, filePath) {
  return (0, import_node_path.relative)(projectRoot, filePath).split(import_node_path.sep).join("/");
}
function listLiveChangeDirs(projectRoot) {
  const artifactsDir = (0, import_node_path.join)(projectRoot, ".ai-agents", "workspace", "artifacts");
  if (!(0, import_node_fs.existsSync)(artifactsDir)) return [];
  return (0, import_node_fs.readdirSync)(artifactsDir, { withFileTypes: true }).filter((entry) => entry.isDirectory() && entry.name !== "_archived").map((entry) => (0, import_node_path.join)(artifactsDir, entry.name)).sort((left, right) => left.localeCompare(right));
}
function listFilesRecursively(directory) {
  const files = [];
  for (const entry of (0, import_node_fs.readdirSync)(directory, { withFileTypes: true })) {
    const entryPath = (0, import_node_path.join)(directory, entry.name);
    if (entry.isDirectory()) files.push(...listFilesRecursively(entryPath));
    else if (entry.isFile()) files.push(entryPath);
  }
  return files;
}
function scanLiveArtifacts(projectRoot, mode) {
  const changeDirs = listLiveChangeDirs(projectRoot);
  if (mode === "change-dirs") {
    return changeDirs.map((directory) => toWorkspaceRelative(projectRoot, directory));
  }
  if (mode === "plans") {
    return changeDirs.map((directory) => (0, import_node_path.join)(directory, "plan.yaml")).filter((planPath) => (0, import_node_fs.existsSync)(planPath) && (0, import_node_fs.statSync)(planPath).isFile()).map((planPath) => toWorkspaceRelative(projectRoot, planPath));
  }
  if (mode === "files") {
    return changeDirs.flatMap((directory) => listFilesRecursively(directory)).map((filePath) => toWorkspaceRelative(projectRoot, filePath)).sort((left, right) => left.localeCompare(right));
  }
  throw new Error(`Invalid --mode "${mode}". Must be one of: plans, change-dirs, files.`);
}

// sources/scripts/artifact-scan.js
var MODES = ["plans", "change-dirs", "files"];
function parseArgs(argv) {
  const modeIndex = argv.indexOf("--mode");
  return modeIndex >= 0 ? argv[modeIndex + 1] : "";
}
function main() {
  const mode = parseArgs(process.argv);
  if (!MODES.includes(mode)) {
    process.stderr.write("--mode requires one of: plans, change-dirs, files.\n");
    process.exit(1);
  }
  const projectRoot = findProjectRoot();
  if (!projectRoot) {
    process.stderr.write("Could not find project root containing .ai-agents.\n");
    process.exit(1);
  }
  try {
    const entries = scanLiveArtifacts(projectRoot, mode);
    process.stdout.write(JSON.stringify({ ok: true, mode, entries }) + "\n");
  } catch (error) {
    process.stderr.write(`${error.message}
`);
    process.exit(1);
  }
}
main();
