# Popover Implementation Plan

## Goals

- Add a draggable, modeless popover/window feature to the component library.
- Support close, hide, and restore flows similar to floating tool windows.
- Keep the implementation SSR-safe by treating drag behavior as progressive enhancement.
- Follow the repository's Blazor, styling, accessibility, and testing guidance.
- Align the visual treatment with Material 3 surface, elevation, and type scale patterns.

## Architecture

1. Add a service-driven popover system that mirrors the existing dialog/snackbar host pattern.
2. Introduce a layout-level `TnTPopoverHost` component that renders active windows and hidden-window launchers.
3. Represent each popover through an `ITnTPopoverHandle` so content can close, hide, or re-activate itself through a cascading value.
4. Keep popover state in C#:
   - visibility
   - z-order
   - position
   - content type / parameters
   - options
5. Render each floating window through a dedicated `TnTPopoverWindow` component.

## SSR And Interactivity

1. Render fully valid markup during SSR without requiring JS.
2. Only import and run drag JS when `RendererInfo.IsInteractive` is true.
3. Preserve the last known C# position so the server-rendered layout and hydrated layout stay aligned.
4. Disable hide/close controls while the component is statically rendered, then allow normal interaction after hydration.

## Accessibility

1. Use `role="dialog"` with `aria-modal="false"` because these are modeless floating windows.
2. Connect title and description through `aria-labelledby` and `aria-describedby`.
3. Ensure hide/close actions are keyboard reachable and clearly labelled.
4. Support `Escape` to close when enabled.
5. Provide a keyboard drag alternative through arrow-key movement when the title bar is focused.
6. Render hidden windows as explicit launcher buttons instead of leaving hidden interactive content in the DOM.

## Material 3 Styling

1. Use surface container colors and on-surface foreground colors from the existing theme tokens.
2. Use rounded corners, outlined borders, and elevated/focused states to match Material 3 floating surfaces.
3. Keep selectors shallow and scoped under the component root classes.
4. Style the hidden-window launcher strip as a compact toolbar of resurfaced actions.

## Testing

1. Add service tests for:
   - open
   - hide/show
   - bring-to-front
   - close
   - position updates
2. Add bUnit host/component tests for:
   - rendering
   - launcher strip behavior
   - accessibility attributes
   - SSR-safe rendering without JS
3. Add Jest tests for the drag module covering:
   - initialization
   - pointer drag updates
   - activation callbacks
   - cleanup
4. Add a browser E2E test against a LiveTest page for:
   - opening multiple windows
   - dragging
   - hiding
   - restoring
   - z-order activation

## Delivery Scope

### Phase 1

- Floating windows
- Dragging
- Hide/show/close
- Z-order management
- Host/service integration
- Tests

### Deferred

- Snap zones
- Edge docking
- Saved workspace layouts
- Resizing
