# NTComponents.Site Documentation Redesign Plan

## Summary

Redesign `NTComponents.Site` into a polished NTComponents-powered documentation and marketing site. The first pass will deliver the full documentation architecture, homepage, generated API browsing, theme guidance, a functional theme converter, and curated examples for key components, with room to add more examples incrementally.

## Key Changes

- Add an explicit `NTComponents` project reference to `NTComponents.Site` so the site depends directly on the library it documents instead of only reaching it through charts.
- Replace the default Blazor layout with an NTComponents-based app shell using `TnTLayout`, `TnTHeader`, `TnTSideNav`, `TnTSideNavLink`, `TnTContainer`, `TnTThemeToggle`, buttons, cards, tabs, badges, inputs, and grid-style documentation tables.
- Build a homepage that functions as the advertising page: clear product positioning, install/get-started callouts, component previews, theming preview, docs search entry, and links into API/component docs.
- Add documentation routes:
  - `/docs` for documentation landing and search.
  - `/docs/components` for grouped component index.
  - `/docs/components/{slug}` for component detail pages.
  - `/docs/api` and `/docs/api/{typeSlug}` for generated API browsing.
  - `/docs/theming` for theme generation instructions.
  - `/tools/theme-converter` for the converter.
- Use `NTComponents.GeneratedDocumentation.GeneratedCodeDocumentation.Model` as the API documentation source. Group types by namespace/component family, expose summaries, signatures, inherited members, properties, methods, fields, and XML summaries where available.
- Add a curated docs layer for examples and metadata. Generated API docs provide reference data; curated entries provide human-friendly examples, runtime configuration controls, category labels, and featured status.

## Component Docs

- Implement reusable documentation building blocks:
  - `DocsPageShell` for title, summary, side table of contents, and content layout.
  - `ComponentExample` for live preview, code snippet, and configuration controls.
  - `ApiMemberTable` for properties, methods, fields, inherited flags, and declaring type.
  - `CodeBlock` for Razor/C#/CSS snippets with copy affordance.
  - `DocsSearchBox` for client-side filtering across component names, summaries, and API type names.
- First curated component pages should cover representative, high-value areas:
  - Buttons: `TnTButton`, `TnTImageButton`, `TnTFabButton`.
  - Layout/navigation: `TnTLayout`, `TnTSideNav`, `TnTHeader`, `TnTContainer`.
  - Forms: `TnTInputText`, `TnTInputSelect`, `TnTInputCheckbox`, `TnTInputSwitch`.
  - Feedback/display: `TnTCard`, `TnTBadge`, `TnTTooltip`, `TnTToast`, `TnTSnackbar`.
  - Data: `TnTDataGrid`, pagination, template/property columns.
  - Advanced: `TnTDialog`, `TnTTabView`, `TnTAccordion`, `NTRichTextEditor`.
- Every component detail page should render generated API reference automatically even when curated examples are not yet present.
- Curated examples should include runtime controls where meaningful, such as color, size, appearance, disabled state, loading state, label text, selected value, placement, and density.

## Theme Converter

- Build `/tools/theme-converter` as a client-side Blazor page using NTComponents controls.
- Accept pasted Material Theme Builder JSON.
- Parse the JSON into a local site-side model compatible with the existing theme shape in `NTComponents.Theming.ThemeFile`.
- Output six CSS text blocks matching `NTComponents.Site/wwwroot/Themes`:
  - `light.css`
  - `light-mc.css`
  - `light-hc.css`
  - `dark.css`
  - `dark-mc.css`
  - `dark-hc.css`
- Convert color values to the existing NTComponents CSS variable format, for example `--tnt-color-primary: rgb(144 75 62);`.
- Include validation for invalid JSON, missing `schemes`, missing `light`/`dark`, and unsupported color formats.
- For medium/high contrast outputs, use the corresponding `*-mc` and `*-hc` scheme objects when present. If the pasted JSON only contains `light` and `dark`, generate default CSS only and show a clear warning that contrast files require contrast scheme data.

## Tests

- Build verification:
  - `dotnet build NTComponents.Site/NTComponents.Site.csproj -c Release`
  - `dotnet build NTComponents.slnx -c Release` if the project reference or shared docs code changes affect the broader solution.
- Add bUnit tests where practical for docs services/helpers:
  - Generated docs indexing groups components and API types correctly.
  - Component slug generation is stable and resolves expected types.
  - Search returns matches by name, summary, and namespace.
  - Theme converter maps known JSON tokens to expected `--tnt-color-*` variables.
  - Invalid theme JSON returns user-facing validation errors.
- Add at least one smoke-style render test for the docs shell/homepage if existing test infrastructure supports it without heavy browser setup.
- Run `npm test` only if JS modules or JS-tested behavior are changed.

## Assumptions

- First pass is architecture plus key docs, not exhaustive curated examples for every component.
- Documentation strategy is hybrid: generated API reference plus curated examples/config panels.
- Theme converter input is pasted Material Theme Builder JSON and output is six CSS file contents for the `Themes` folder.
- The site should remain Blazor WebAssembly on `net10.0`.
- The chart submodule stays referenced, but the site should explicitly reference `NTComponents` for documentation and demos.
- No generated documentation should be persisted as checked-in files in this pass; the site reads the source-generator model at compile time.
