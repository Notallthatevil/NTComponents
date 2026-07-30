import { readFile, rm, writeFile } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { minify } from "terser";

const repositoryRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const sourceRoot = path.join(repositoryRoot, "NTComponents", "Theming");
const outputRoot = path.join(repositoryRoot, "NTComponents", "wwwroot");
const testOutputRoot = path.join(sourceRoot, ".generated");
const runtimePath = path.join(testOutputRoot, "NTTheme.runtime.js");
const bootstrapPath = path.join(testOutputRoot, "theme-bootstrap.js");

const runtime = await readFile(runtimePath, "utf8");
const bootstrap = await readFile(bootstrapPath, "utf8");
const anchorPositioning = await readFile(path.join(repositoryRoot, "node_modules", "@oddbird", "css-anchor-positioning", "dist", "css-anchor-positioning.js"), "utf8");

async function prepare(source, module) {
  const result = await minify(source, {
    compress: true,
    format: { comments: false },
    mangle: false,
    module,
  });

  if (!result.code) {
    throw new Error("Terser did not produce head asset output.");
  }

  return `${result.code}\n`;
}

await Promise.all([
  writeFile(path.join(testOutputRoot, "NTTheme.runtime.js"), await prepare(runtime, false)),
  writeFile(path.join(testOutputRoot, "theme-bootstrap.js"), await prepare(bootstrap, false)),
  writeFile(path.join(outputRoot, "NTTheme.js"), await prepare(`${runtime}\n${bootstrap}`, false)),
  writeFile(path.join(outputRoot, "css-anchor-positioning.js"), await prepare(anchorPositioning, true)),
  rm(path.join(outputRoot, "NTTheme.runtime.js"), { force: true }),
  rm(path.join(outputRoot, "theme-bootstrap.js"), { force: true }),
]);
