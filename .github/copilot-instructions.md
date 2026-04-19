## TnTComponents – Copilot Quick Guide

### 0. Prime Directive

Never assume. Mirror existing patterns. If something is ambiguous, pause and surface the question instead of guessing.

### 1. Core Principles

* Small, cohesive, testable components (Material 3 look & feel).
* Each component lives in its own folder under `TnTComponents/`.
* Favor consistency over novelty; reuse existing abstractions.
* No new external packages without explicit approval.

### 2. Instruction Files – Your Source of Truth

This repository has detailed instruction files for specific concerns. **Always consult the relevant file for your task:**

* **`./instructions/blazor.instructions.md`** — Apply when creating or modifying `*.razor` files. Covers component markup, dependency injection, no inline `@code`, accessibility, and Blazor conventions.
* **`./instructions/styles.instructions.md`** — Apply when creating or modifying `*.razor.scss` files. Covers SCSS scoping, shared variables, BEM-like patterns, responsiveness, and asset organization.
* **`./instructions/code-generation.instructions.md`** — Apply when generating or writing new C# code (`*.cs` files). Covers documentation, testability, abstractions, design, and PR guidance.
* **`./instructions/tests.instructions.md`** — Apply when creating or modifying `*.Tests.cs` files. Covers test frameworks (xUnit, NSubstitute, AutoFixture, AwesomeAssertions), AAA pattern, naming conventions, and coverage targets.

**Before starting work, identify which instruction file(s) apply and refer to them for deterministic rules and checklists.**

### 3. Creating / Extending a Component

Required files (per folder):

* `Name.razor` (markup only – logic goes in code-behind) — see `./instructions/blazor.instructions.md`
* `Name.razor.cs` (partial class inheriting `TnTComponentBase` unless clearly excluded) — see `./instructions/code-generation.instructions.md`
* `Name.razor.scss` (author styles here) → compiled to `.razor.css` / `.razor.min.css` — see `./instructions/styles.instructions.md`
* Optional `Name.razor.js` if JS interop is needed; must export: `onLoad`, `onUpdate`, `onDispose` (see `TabView` example)
* Corresponding test file `Name.Tests.cs` in `TnTComponents.Tests/` — see `./instructions/tests.instructions.md`

### 4. Dev Workflows

* Build: `dotnet restore` then `dotnet build` (solution root).
* Test: `dotnet test` (or narrow scope by project / filter).
* Manual verification: run `LiveTest` app (client for UI smoke checks).
* Keep the build green before adding more features—incremental, verified changes.
* Code coverage: `dotnet test --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml` (output in test project bin folder).

### 5. Quick Reference Checklist

Before submitting changes:

* [ ] Consulted relevant instruction file(s) from section 2

* [ ] Followed file naming and structure conventions
* [ ] Added/updated tests with new functionality
* [ ] Component is keyboard accessible and semantic
* [ ] No hardcoded theme colors (use variables)
* [ ] No new dependencies without approval
* [ ] Build passes: `dotnet build`
* [ ] Tests pass: `dotnet test`

### 6. Reference Maps

* **Blazor component markup rules:** `./instructions/blazor.instructions.md`
* **SCSS and styling rules:** `./instructions/styles.instructions.md`
* **C# code and documentation rules:** `./instructions/code-generation.instructions.md`
* **Testing framework and conventions:** `./instructions/tests.instructions.md`
* **Component examples to consult:** `Buttons/`, `Accordion/`, `TabView/` (especially for JS interop pattern).

### 7. Escalation Triggers

Surface clarification instead of proceeding when:

* API shape conflicts with an existing analogous component.
* A feature implies cross-cutting concerns (theming, globalization, virtualization).
* Requires altering shared base classes.
* Any instruction file rule is unclear or conflicts with reviewer feedback.
* You are uncertain about which instruction file(s) apply to your task.

---

### Quick Summary

1. **Identify your task:** Am I creating/modifying `.razor` files? Styles (`.razor.scss`)? Tests (`.Tests.cs`)? C# code (`.cs`)?
2. **Consult the right file:** Use the reference maps in section 6 to find the correct instruction file.
3. **Follow the checklist:** Each instruction file contains a deterministic checklist — use it.
4. **Mirror existing patterns:** Consistency and cohesion over novelty.
5. **Ask before guessing:** If a rule is unclear or conflicts, surface it rather than proceeding.

**TL;DR:** Mirror existing component structure; keep logic in code-behind; SCSS only for styling; tests mirror structure; document everything; prefer consistency and accessibility; consult the instruction files for deterministic rules; ask when uncertain.