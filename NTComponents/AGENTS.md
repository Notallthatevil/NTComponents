# NTComponents Library Notes

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
