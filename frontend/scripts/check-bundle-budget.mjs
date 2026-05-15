#!/usr/bin/env node
import fs from "node:fs";

const BUILD_MANIFEST = ".next/build-manifest.json";
const BUDGET_BYTES = 380 * 1024;

if (!fs.existsSync(BUILD_MANIFEST)) {
  console.error("Bundle budget check failed: build manifest not found.");
  process.exit(1);
}

const manifest = JSON.parse(fs.readFileSync(BUILD_MANIFEST, "utf8"));
const pages = manifest.pages ?? {};
const firstLoadJs = new Set();

for (const key of Object.keys(pages)) {
  const assets = Array.isArray(pages[key]) ? pages[key] : [];
  for (const asset of assets) {
    if (asset.endsWith(".js")) {
      firstLoadJs.add(`.next/${asset}`);
    }
  }
}

let total = 0;
for (const filePath of firstLoadJs) {
  if (fs.existsSync(filePath)) {
    total += fs.statSync(filePath).size;
  }
}

if (total > BUDGET_BYTES) {
  console.error(
    `Bundle budget exceeded: ${total} bytes (limit ${BUDGET_BYTES} bytes).`
  );
  process.exit(1);
}

console.log(`Bundle budget passed: ${total} bytes / ${BUDGET_BYTES} bytes.`);
