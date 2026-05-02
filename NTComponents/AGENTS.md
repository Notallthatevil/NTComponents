# NTComponents Library Notes

## Component Implementation Rules

- Keep component implementations grounded in the relevant Material 3 spec. Use the Figma M3 source and any repo-local spec documents in `D:\NTComponents\docs\` as the baseline for structure, states, spacing, typography, motion, elevation, accessibility, and interaction behavior.
- When a task asks to pull or reanalyze Material 3 docs, create or update a text-only Markdown spec in `docs/` and include image/video-derived details in prose. Then compare the implementation against that spec and close the gaps.
- Keep related component files co-located and consistently named: `.razor`, `.razor.cs`, `.razor.scss`, and `.razor.ts` when client behavior is needed. Treat `.razor.ts` as the source; it must compile to the served `.razor.js`.
- Components that must work in SSR should avoid requiring interactive Blazor callbacks for basic native behavior. Preserve pass-through attributes such as plain `onclick` where applicable, and only attach interactive callbacks when one is registered.
- For reusable browser behavior, prefer a reusable `.razor.ts` module or shared JS utility instead of copying placement, popover, ripple, or keyboard logic across components.
- Add or update analyzer rules when a new component has required parameters, invalid configuration combinations, accessibility requirements, or child-content constraints that should be caught at compile time.
- Add or update LiveTest demos when a component gains user-visible behavior, but do not start the LiveTest process unless the user explicitly asks. The user runs it manually.

## Styling And SCSS Rules

- Keep generated CSS compact. Remove unused selectors, variables, and helper abstractions; inline one-off SCSS helpers when they have a single reference.
- Prefer SCSS variables for compile-time values. Do not use CSS custom properties just to avoid repeating a value in SCSS.
- Use CSS custom properties only for runtime theming or public component overrides, such as colors driven by component parameters or consumer CSS.
- Reference existing tokens from `NTComponents/Styles/_Variables` before adding local values. Do not copy token values locally when a shared variable or map exists.
- Avoid component-local elevation shadows when a component exposes `NTElevation`; emit the appropriate elevation class and rely on the shared elevation system.
- Do not use `__`/BEM-style class names. Prefer a component root class with normal nested classes below it.
- Keep clipping, overflow, and elevation responsibilities separate. Do not put `clip-path` or restrictive overflow on the same element that must render an outer elevation shadow.
- Hide scrollbars only on the scroll container, not on an elevated or clipping wrapper, and preserve scrolling behavior.
- Use ripple hosts and the shared button interaction registration path for clickable menu, button, and action items instead of creating component-specific ripple logic.

## Menu And Popover Rules

- Menus and popover-like components should auto-place using JS when viewport edges matter. Keep the public API simple unless the spec or user explicitly asks for manual placement.
- Anchor alignment should match the trigger edge. Right-side triggers align right edges; left-side triggers align left edges; upper/lower placement determines whether the menu opens below or above.
- Nested menus must keep direct item ownership clear so keyboard navigation, ripple registration, and close-on-click logic only target items in the owning menu.
- Label text is non-interactive menu content. It should not receive ripple, button roles, or item click handling.

## Material 3 Reference Files

The repo now includes two durable Material 3 reference documents under the repo-level `docs/` folder:

- `D:\NTComponents\docs\material3-interaction-states-reference.md`
  - Generic Material 3 interaction-state guidance
  - Summarizes live M3 guidance for enabled, disabled, hover, focus, pressed, dragged, state layers, opacity values, focus indicators, combination rules, and component adoption guidance
  - Use this as the baseline for button, chip, card, input, toggle, drag-handle, and other interactive-state work

- `D:\NTComponents\docs\material3-layout-reference.md`
  - Generic Material 3 layout guidance
  - Summarizes live M3 guidance for window size classes, panes, spacing, density, hardware constraints, RTL, applying-layout rules, and canonical layouts such as list-detail, supporting pane, and feed
  - Use this as the baseline for layout systems, adaptive shells, pane behavior, responsive docs pages, navigation placement, spacing, and RTL work

These files are internal distilled references. They exist to avoid re-scraping the Material site every time and to keep repeated layout/state rules out of ad hoc implementation notes.

## Context Loading Rules

Do not load these files by default.

Load `material3-interaction-states-reference.md` only when the task involves:

- hover, focus, pressed, disabled, dragged, or selected-state behavior
- ripple, state-layer, opacity, or focus-ring behavior
- reviewing whether an interactive component matches Material 3 state guidance
- designing or refactoring interaction behavior for buttons, chips, cards, inputs, toggles, drag handles, or similar controls

Load `material3-layout-reference.md` only when the task involves:

- adaptive layout behavior across width classes
- panes, split views, supporting panes, feed layouts, or canonical layouts
- spacing, margins, spacers, padding, density, target sizing, or reachability
- navigation placement by window size
- foldables, cutouts, multi-window behavior, or RTL layout rules
- reviewing whether a page, shell, or layout component matches Material 3 layout guidance

## When Not To Load Them

Do not load either file for:

- routine bug fixes inside a component that are unrelated to Material behavior
- pure C# logic, data flow, or parameter wiring work
- test-only changes that do not change layout or interactive behavior
- styling work that is strictly local and already specified by the user
- simple markup cleanup, naming cleanup, or refactors with no Material 3 design question

If the task is obviously local and implementation-only, keep these files out of context.

## Preferred Usage

- Start with direct code inspection first.
- Load one of these references only if the task touches the behavior categories above.
- Prefer loading only the one relevant file, not both.
- Load both only when a task genuinely spans interaction states and adaptive layout behavior.
- Treat these files as summarized repo guidance, not as a substitute for user instructions.
- If the user gives an explicit design direction that conflicts with the reference, follow the user and note the deviation.
