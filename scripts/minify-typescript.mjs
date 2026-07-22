import { readdir, readFile, writeFile } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { minify } from "terser";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../NTComponents");

async function getTypeScriptFiles(directory) {
  const entries = await readdir(directory, { withFileTypes: true });
  const files = await Promise.all(
    entries.map((entry) => {
      const entryPath = path.join(directory, entry.name);
      return entry.isDirectory() ? getTypeScriptFiles(entryPath) : entryPath;
    }),
  );

  return files.flat().filter((file) => file.endsWith(".ts") && !file.endsWith(".d.ts"));
}

for (const typeScriptFile of await getTypeScriptFiles(root)) {
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
