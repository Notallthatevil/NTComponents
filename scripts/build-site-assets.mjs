import { build } from "esbuild";
import path from "node:path";
import { fileURLToPath } from "node:url";

const repositoryRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const minify = process.argv.includes("--minify");

await build({
  entryPoints: [path.join(repositoryRoot, "NTComponents.Site", "wwwroot", "js", "theme-creator.ts")],
  bundle: true,
  format: "esm",
  minify,
  outfile: path.join(repositoryRoot, "NTComponents.Site", "wwwroot", "js", "theme-creator.js"),
  sourcemap: false,
  target: "es2022",
});
