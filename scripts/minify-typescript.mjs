import { readdir, readFile, writeFile } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { minify } from "terser";

const repositoryRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const roots = [path.join(repositoryRoot, "NTComponents"), path.join(repositoryRoot, "NTComponents.Site", "wwwroot")];
const headSources = new Set([
  path.join(repositoryRoot, "NTComponents", "Theming", "NTTheme.runtime.ts"),
  path.join(repositoryRoot, "NTComponents", "Theming", "theme-bootstrap.ts"),
]);

async function getTypeScriptFiles(directory) {
  const entries = await readdir(directory, { withFileTypes: true });
  const files = await Promise.all(
    entries.map((entry) => {
      const entryPath = path.join(directory, entry.name);
      return entry.isDirectory() ? getTypeScriptFiles(entryPath) : entryPath;
    }),
  );

  return files.flat().filter((file) => file.endsWith(".ts") && !file.endsWith(".d.ts") && !headSources.has(file));
}

const typeScriptFiles = (await Promise.all(roots.map(getTypeScriptFiles))).flat();

for (const typeScriptFile of typeScriptFiles) {
  const javaScriptFile = typeScriptFile.slice(0, -3) + ".js";
  const result = await minify(await readFile(javaScriptFile, "utf8"), {
    compress: true,
    format: { comments: false },
    mangle: false,
    module: true,
  });

  if (!result.code) {
    throw new Error(`Terser did not produce output for ${javaScriptFile}.`);
  }

  await writeFile(javaScriptFile, result.code);
}
