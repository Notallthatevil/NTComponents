# Material 3 Interaction States Reference

## Purpose

This document is the durable NTComponents reference for generic Material 3 interaction-state behavior.

Use it when implementing or reviewing interactive components so state handling stays consistent across the library.

Source pages reviewed on April 23, 2026:

- https://m3.material.io/foundations/interaction/states/overview
- https://m3.material.io/foundations/interaction/states/state-layers
- https://m3.material.io/foundations/interaction/states/applying-states

## Core Model

- States communicate the current interaction status of a component or UI element.
- States should be applied consistently across components.
- States can combine with persistent conditions such as `selected`, `activated`, or `checked`.
- A state should not rely on a single visual treatment. Focused elements, for example, use both state styling and a keyboard focus indicator.

## Standard State Set

The generic Material 3 interactive state set is:

- `enabled`
- `disabled`
- `hover`
- `focused`
- `pressed`
- `dragged`

Not every component inherits every state. State support must be chosen by component category.

## State Layers

A state layer is a semi-transparent overlay used to express an interaction state.

Generic rules:

- The state layer sits between the container and the content.
- The state layer color is derived from the content color.
- When a container uses an `on-*` content color, the state layer uses that same `on-*` color.
- When a surface-colored component uses `primary` for content, the state layer uses `primary`.
- Only one state layer should be active at a time.
- The state layer can cover the whole component or only the active part of the component, such as a circular region.

## State Layer Opacity Values

Use these fixed overlay opacities:

| State | Opacity |
| --- | --- |
| Hover | `0.08` |
| Focus | `0.10` |
| Pressed | `0.10` |
| Dragged | `0.16` |
| Disabled content treatment | `0.38` |

Notes:

- Hover is low emphasis.
- Focus is higher emphasis than hover.
- Pressed is high emphasis and usually paired with ripple or composition change.
- Dragged is lower emphasis than pressed to avoid distracting from drag-and-drop work.

## State Behavior Rules

### Enabled

- Enabled is the default interactive styling.
- Enabled communicates that the element can be acted on.

### Disabled

- Disabled communicates that the element is not interactive.
- Disabled components do not hover, focus, press, or drag.
- Disabled components do not show interactive state layers.
- Disabled treatment is commonly communicated through reduced contrast and reduced elevation.
- Disabled states do not need to meet Material contrast requirements.

### Hover

- Hover is initiated by cursor pause.
- Hover should animate in and out with a light fade.
- Hover can combine with selected, focused, activated, or pressed conditions.
- In a layout, only one element is hovered at a time.

### Focused

- Focus is initiated by keyboard, voice, or equivalent non-pointer navigation.
- Focus applies to interactive components.
- Focus should include a keyboard focus indicator ring for keyboard navigation scenarios.
- Focus can combine with hover, selected, or activated conditions.
- In a layout, only one element is focused at a time.

### Pressed

- Pressed communicates an active tap, click, key press, or voice-triggered action.
- Pressed should be high emphasis.
- Ripple is a pressed-state indicator.
- Some components can also change elevation or composition while pressed.
- Pressed can combine with hover, focus, selected, or activated conditions.
- In a layout, only one element is pressed at a time.

### Dragged

- Dragged communicates press-and-move interaction.
- Dragged should stay lower emphasis than pressed.
- Some components can change elevation while dragged.
- In a layout, only one element is dragged at a time.

## Combination Rules

When states combine:

- Keep the persistent condition, such as `selected`, in the base styling.
- Apply only one transient state layer at a time.
- Use other signals, such as ripple, focus ring, elevation, shape, or selected styling, to express the full combined state.
- Do not stack separate hover and focus overlays on top of each other.

## Component Adoption Guidance

Use this when deciding whether a component should inherit a given state.

### Usually inherits disabled, hover, focus, and pressed

- Buttons
- Cards
- Checkboxes and selection controls
- Chips
- List items
- Text fields

### Often does not inherit container-wide states

- App bars
- Badges
- Dialogs
- Menus
- Navigation bar, rail, and drawer
- Sheets
- Tabs

For these surfaces, the actionable child elements inherit the state instead of the whole container.

### Dragged is more selective

Dragged generally applies to movable containment or selection surfaces such as:

- Cards
- Chips
- List items
- Sliders

Dragged generally does not apply to buttons or fixed-position communication/navigation surfaces.

## Implementation Checklist

When building a component:

1. Decide which of the standard states the component inherits.
2. Derive the state layer color from the current content color.
3. Place the state layer between container and content.
4. Use the fixed state opacity values.
5. Suppress all transient states when disabled.
6. Add a visible keyboard focus indicator for keyboard-reachable components.
7. Use ripple and optional elevation/composition changes for pressed behavior where the component type supports it.
8. Avoid stacking multiple simultaneous state layers.
9. Verify that persistent states such as `selected` still combine correctly with hover, focus, and pressed.

## Review Checklist

When reviewing an interactive component:

- Does it implement only the states appropriate for that component category?
- Is the overlay color derived from content color rather than an arbitrary hover/focus token?
- Are hover, focus, pressed, and dragged using the correct opacity values?
- Is the focus ring separate from the focus state layer?
- Does disabled fully suppress transient interactive states?
- Does pressed use ripple or another high-emphasis signal where appropriate?
- Are combined states handled without stacking multiple overlays?

## NTComponents Note

This reference is intentionally generic. Component-specific decisions should point back to this document rather than duplicating the same state rules in multiple component plans or review notes.
