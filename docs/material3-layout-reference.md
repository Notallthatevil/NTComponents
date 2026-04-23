# Material 3 Layout Reference

## Purpose

This document is the durable NTComponents reference for generic Material 3 layout behavior.

Use it when implementing or reviewing layout systems, adaptive page shells, panes, spacing, density options, and RTL behavior across the library.

Source pages reviewed on April 23, 2026:

- https://m3.material.io/foundations/layout/understanding-layout/overview
- https://m3.material.io/foundations/layout/understanding-layout/spacing
- https://m3.material.io/foundations/layout/understanding-layout/parts-of-layout
- https://m3.material.io/foundations/layout/understanding-layout/density
- https://m3.material.io/foundations/layout/understanding-layout/hardware-considerations
- https://m3.material.io/foundations/layout/understanding-layout/bidirectionality-rtl
- https://m3.material.io/foundations/layout/applying-layout/pane-layouts
- https://m3.material.io/foundations/layout/applying-layout/window-size-classes
- https://m3.material.io/foundations/layout/applying-layout/compact
- https://m3.material.io/foundations/layout/applying-layout/medium
- https://m3.material.io/foundations/layout/applying-layout/expanded
- https://m3.material.io/foundations/layout/applying-layout/large-extra-large
- https://m3.material.io/foundations/layout/canonical-layouts/overview
- https://m3.material.io/foundations/layout/canonical-layouts/list-detail
- https://m3.material.io/foundations/layout/canonical-layouts/supporting-pane
- https://m3.material.io/foundations/layout/canonical-layouts/feed

## Core Model

- Layout is the visual arrangement of elements on the screen.
- Layout should direct attention toward the action users need to take.
- Material 3 layout guidance applies to Android and the web.
- Start from a canonical layout before introducing a custom grid.
- Treat spacing, panes, density, hardware constraints, and bidirectionality as part of one layout system rather than separate afterthoughts.

## Canonical Layouts

Material 3 defines three canonical layouts that should be the first stop before designing a custom application shell:

- `list-detail`
- `supporting pane`
- `feed`

Guidance:

- Use canonical layouts as starting points for organizing common app structures across width classes.
- Each canonical layout has expected behavior across compact, medium, and expanded widths.
- Extend a canonical layout only when product-specific needs require it.

### List-detail

Use list-detail when the product has a browsable collection and an adjacent detail surface for the selected item.

Typical examples:

- inbox + selected email
- settings categories + category detail
- file browser + open folder
- message list + conversation detail

Generic rules:

- Compact uses a single pane with either list or detail visible at a time.
- Medium usually uses a single pane, but can use two panes when content is lighter and still usable in compressed widths.
- Expanded, large, and extra-large use two panes.
- In single-pane mode, a back button appears only in detail view.
- In two-pane mode, selected state remains visible in the list pane.
- When transitioning from one pane to two, show the selected detail if one exists; otherwise show placeholder or empty detail content.
- When transitioning from two panes back to one, preserve the product’s established single-pane behavior consistently.
- Preserve detail-view state such as scroll position when moving between selected items where that continuity matters.

### Supporting pane

Use supporting pane when the secondary content only makes sense in relation to the primary content, but is not a list-detail parent-child pattern.

Typical examples:

- productivity tools
- document editing with comments
- media browsing with supplemental context

Generic rules:

- The primary pane takes most of the body region.
- The supporting pane provides secondary context.
- Compact and medium place the supporting pane below the primary pane.
- A bottom sheet can be a useful compact supporting-pane variant when focus should remain on the primary surface.
- Expanded places the supporting pane to the left or right of the primary pane.
- Expanded supporting panes typically use a fixed width of `360dp`.

### Feed

Use feed when the product is primarily about scanning or discovering a large amount of content in cards or card-like items.

Typical examples:

- news
- photos
- social feeds

Generic rules:

- Feed is a grid-based composition rather than a strict pane pairing.
- Compact usually stacks items vertically like a card list.
- Medium can introduce multiple columns and varied item widths.
- Expanded, large, and extra-large increase the number of columns as space grows.
- Feed items should reflow as width changes.
- Item order is determined by item position in the feed, so reflow rules should preserve understandable ordering.

## Window Size Classes

Material 3 recommends designing for five width classes:

| Width class | Width |
| --- | --- |
| Compact | `< 600dp` |
| Medium | `600dp - 839dp` |
| Expanded | `840dp - 1199dp` |
| Large | `1200dp - 1599dp` |
| Extra-large | `>= 1600dp` |

Guidance:

- Layouts should adapt at these breakpoints.
- Do not treat phone, tablet, foldable, and desktop as separate one-off layout categories when width classes already explain the available space.
- Use canonical layouts as the starting point for how panes and navigation change across these classes.
- Large and extra-large are especially relevant for desktop and monitor-oriented experiences, but not every product needs dedicated designs for them.

### Recommended pane totals by width class

| Width class | Recommended pane total | Other allowed pane totals |
| --- | --- | --- |
| Compact | `1` | none |
| Medium | `1` | `2` |
| Expanded | `2` | `1` |
| Large | `2` | `1` |
| Extra-large | `2` | `1`, `3` |

### Per-width-class defaults

| Width class | Navigation defaults | Body defaults | Margins |
| --- | --- | --- | --- |
| Compact | Navigation bar or modal drawer | Single pane | `16dp` |
| Medium | Navigation rail or modal drawer for single pane; navigation bar for two-pane | Single pane recommended, two-pane possible | `24dp` |
| Expanded | Navigation rail or persistent drawer | One or two panes, two-pane often best | `24dp` |
| Large | Navigation rail or persistent drawer | One or two panes, two-pane often best | `24dp` |
| Extra-large | Navigation rail or persistent drawer | One to three panes, two-pane often best | `24dp` |

## Major Layout Parts

### Window

- The window frames the product.
- Material treats the window as two primary regions: `navigation` and `body`.

### Navigation region

- Holds primary navigation structures such as navigation drawers, rails, and bars.
- Navigation should sit near the window edge that is easiest to reach.
- Use the leading edge: left in LTR, right in RTL.

### Body region

- Holds the app’s primary content and actions.
- Content is organized into one or more panes.

### Panes

- All content should live inside a pane.
- A layout may contain `1-3` panes.
- Pane types are `fixed` and `flexible`.
- Every responsive layout needs at least one flexible pane.
- Multiple panes can be shown together when space allows.
- Pane changes should preserve context and meaning as the window resizes.
- Panes can be permanent or temporary.
- Temporary panes may appear and dismiss while affecting the size of other panes.

### Single-pane layouts

- Single-pane layouts use one flexible pane.
- They work at any width class.
- They are recommended by default for compact and medium widths.
- Single-pane can also be the right choice for dense media or immersive tasks.

### Two-pane layouts

Two-pane layouts come in two common forms:

- `split-pane`: both panes are flexible
- `fixed and flexible`: one pane uses a fixed width and the other absorbs remaining space

Guidance:

- Split-pane is a good default for foldables and more dynamic resizable layouts.
- In split-pane layouts, the spacer should remain visually centered.
- If a rail or drawer is present in split-pane, the navigation component should reduce one pane so the spacer still appears centered.
- In medium widths, if two panes are used, each pane should default to `50%` of the width and avoid custom widths.
- In expanded fixed-and-flexible layouts, fixed panes should default to `360dp`.
- In large and extra-large fixed-and-flexible layouts, fixed panes should default to `412dp`.

### Three-pane layouts

- Three panes are primarily an extra-large pattern.
- A standard side sheet can serve as the third pane.
- Do not exceed three panes.
- In extra-large layouts, fixed panes are recommended at `412dp`, while a side sheet may still cap around `400dp`.

### Pane adaptation strategies

Use these generic strategies when a layout has to adapt:

- `show and hide`
- `levitate`
- `reflow`

Avoid moving core elements to unrelated UI objects just because the window class changes.

### Display modes for multiple panes

Multiple panes can appear in three display modes:

- `co-planar`: panes sit side by side
- `floating`: a pane appears above other content
- `docked`: a pane appears attached to an edge while overlapping other content

Guidance:

- Choose the pane display mode based on width class and task needs.
- Floating panes may be modal or non-modal.
- On large screens, a scrim behind a floating pane may be optional.
- Docked panes in medium and expanded widths may adapt into floating or co-planar panes.

### Pane resizing behavior

Pane resizing can be either:

- `persistent`: user width preference is remembered
- `temporary`: user width preference resets when the pane or app closes

Use persistent resizing for most resizable layouts. Use temporary resizing for layouts where resizing is uncommon, such as many supporting-pane cases.

### Columns

- Columns exist inside a pane, not at the window level.
- Use columns to segment and align pane content.

### App bars

- App bars sit inside panes.
- As width changes, hide or reveal nested actions based on available width.

### Drag handles

- Drag handles resize panes.
- They belong in the spacer between panes, or on the edge of a single expanded pane when relevant.
- They can be horizontal or vertical.
- In two-pane layouts, recommended custom snap widths include `360dp`, `412dp`, or a visually centered split-pane spacer.
- Do not heavily customize the drag handle.
- Drag handles should also support direct toggle actions such as tap, double tap, or long press where appropriate.

## Spacing System

Material layout spacing is built from grouping, margins, spacers, and padding.

### Grouping

Use grouping to show relationships and boundaries.

- `explicit grouping` uses outlines, dividers, shadows, or containers
- `implicit grouping` uses proximity and open space

Use explicit grouping when enclosure or affordance matters. Use implicit grouping when proximity is enough.

### Margins

- Margins are the space between the window edge and interior content.
- Margin widths can be fixed or scale by window size class.
- Wider windows should generally use wider margins to create more perimeter space.
- Compact layouts use `16dp` side margins.
- Medium, expanded, large, and extra-large layouts use `24dp` side margins.

### Spacers

- A spacer is the space between panes.
- Standard pane spacers are `24dp`.
- A spacer may contain a drag handle.
- The drag handle touch target can overlap adjacent panes slightly.

### Padding

- Padding is the space between UI elements.
- Padding is measured in `4dp` increments.
- Padding does not need to span the full width or height of a layout.

## Density

Density is about how much information is visible on screen and how tightly components are packed.

### Information density

- Information density can increase by changing layout structure and spacing, not only by shrinking components.
- Higher density can help in data-heavy contexts such as tables, dashboards, and long forms.
- Lower density can be better for focused tasks, easier navigation, or more editorial/marketing surfaces.

### Component scaling

- Component density is measured from a default of `0` and moves denser through `-1`, `-2`, and `-3`.
- Increased density usually reduces vertical padding or overall height in `4dp` steps.
- Text size should not change just because the component container becomes denser.
- Grouped content should remain visually centered inside the component container.

### Density rules

- Do not apply denser component scaling by default if it pushes targets below `48x48`.
- Density should be an opt-in user choice when possible.
- Density should not automatically change just because the window class or orientation changed.
- Do not increase density for focused selection tasks such as menus.
- Do not increase density for change-alerting components such as dialogs or snackbars.

### Targets

- Interactive targets should stay at least `48x48` CSS pixels.
- The visible visual can be smaller than the target, but the hit region should still meet the minimum.

## Hardware Considerations

Window size classes are the baseline, but layout also needs hardware-aware behavior.

### Display cutouts

- Content can extend edge-to-edge, but important UI should remain in safe visible areas.
- Avoid placing critical content where cutouts can obscure it.

### Foldables

- Treat the fold or hinge as a first-class layout constraint.
- Flexible folds can usually be crossed by content more easily.
- Physical hinges are better treated as a separation between distinct panes or window areas.

### Foldable device states

Common postures:

- `folded`
- `open flat`
- `tabletop`

Guidance:

- Folded front screens often behave like compact layouts.
- Open flat postures often move into medium or expanded layouts.
- Tabletop postures may make controls near the fold harder to reach and text near the fold harder to read.

### App continuity

- When hardware posture changes, the app should preserve task state and current location.
- Layout should adapt without resetting the user’s work.

### Multi-window mode

- Multi-window is separate from multi-pane.
- Multiple app windows may reduce the available area enough to force a more compact layout.
- Support straightforward window creation, resizing, and simple mental models.
- Common split ratios include `50/50`, `1:3`, and `2:3`.

## Applying Layout Across Width Classes

When adapting a product from one width class to the next, evaluate five questions:

1. What should be revealed?
2. How should the screen be divided?
3. What should be resized?
4. What should be repositioned?
5. What should be swapped?

### Reveal

- Reveal navigation, panes, or supporting content only when the extra space materially helps the task.
- Additional space should not be used only to make the same single view larger.

### Divide

- Single-pane is best by default in compact and usually medium widths.
- Two-pane is usually best in expanded, large, and extra-large widths.
- Two-pane medium layouts are only a good fit for lower-density content that still remains usable in compressed panes.

### Resize

- Cards, feeds, lists, and panes can resize as width increases.
- When resizing text-containing surfaces, preserve readable line lengths.
- Aim for `40-60` characters per line.

### Reposition

- Reflow elements to use wider layouts better and to improve reachability.
- More width can justify added columns, more negative space, or a different action placement.
- Internal elements can stay anchored to one another as containers scale.

### Swap

- Components with equivalent function may swap across width classes.
- Swaps must be ergonomic and functionally justified, not cosmetic.
- Safe examples include:
  - navigation bar -> navigation rail
  - collapsed rail -> expanded rail
- Do not swap unrelated component types just because there is more space.

## Width-Class-Specific Notes

### Compact

- Use a navigation bar or modal navigation drawer.
- Use a single-pane layout.
- Side margins are `16dp`.
- Compact layouts should dynamically transition when devices unfold, rotate, or resize into larger widths.

### Medium

- Use a navigation rail or modal navigation drawer for single-pane layouts.
- Use a navigation bar for two-pane layouts.
- Single-pane is recommended by default.
- Two-pane is acceptable for lower-density content such as settings.
- In medium two-pane layouts, each pane should default to `50%` width and avoid custom widths.
- Side margins and pane spacers are `24dp`.
- Be careful with reachability on horizontal tablets and unfolded foldables.
- The upper `25%` of the screen is often harder to reach and should not hold too many critical interactions.

### Expanded

- Use a navigation rail or persistent navigation drawer.
- One or two panes are valid, with two-pane often best.
- Dense or media-heavy experiences may still prefer a single pane.
- Fixed panes default to `360dp`.
- Split-pane should keep the spacer visually centered.
- Side margins and pane spacers are `24dp`.

### Large and Extra-large

- These widths are especially useful for desktop and monitor-oriented experiences.
- Use a navigation rail or persistent navigation drawer, choosing the drawer when the amount of body content still leaves enough room.
- A drawer may collapse to a rail when space is needed or on deeper destinations.
- Two-pane is often best, though single-pane still fits dense media content.
- Fixed panes default to `412dp`.
- Split-pane should keep the spacer visually centered, even with visible navigation.
- Extra-large supports a third pane through a standard side sheet.
- Side margins and pane spacers are `24dp`.
- Large-screen typography and line length need explicit review for readability.

## Bidirectionality And RTL

Layouts should support both LTR and RTL languages.

### Mirroring

- Mirror layouts horizontally for RTL when the layout is directional.
- Use `leading` and `trailing` semantics instead of hardcoded `left` and `right`.
- Navigation components belong on the leading edge.

### Text

- Text rendering involves both alignment and directionality.
- RTL content should usually be right-aligned and flow right-to-left.
- Do not force LTR directionality onto RTL content.
- Preserve the logical direction of content like email addresses, domains, URLs, and similar mixed-direction strings.

### Icons and symbols

- Mirror directional UI icons such as back and forward in RTL.
- Do not blindly mirror everything. Some content keeps LTR behavior depending on meaning and locale.
- Media controls are always LTR.
- Some timeline or chart directions remain LTR for certain RTL languages.

### Time and progress

- Linear progress generally mirrors in RTL.
- Circular time-based representations remain clockwise.
- Clock movement remains clockwise in RTL.
- In 12-hour clocks, the AM/PM token moves to the left in RTL.

### Components and gestures

- Mirror badges, toolbars, app bars, drawers, rails, text-field icon positions, chips, and swipe-revealed actions where the interaction is directional.
- Predictive back and other directional gestures should mirror in RTL.

## Accessibility Rules

- Minimum interactive targets stay `48x48`.
- Drag handles should expose hover and keyboard behavior.
- Keyboard users should be able to reach a drag handle with `Tab` and activate it with `Space` or `Enter`.
- Screen readers should get a clear accessible label for drag handles such as “Resize layout.”
- Drag handles should expose meaningful state such as equal split or left-pane-expanded.
- For co-planar panes, focus order should match the visual pane order.
- Modal floating panes should move focus into the pane when opened and restore focus logically when closed.
- Non-modal floating panes should remain part of the normal reading and interaction order.
- Docked panes should follow the same modal or non-modal focus rules as floating panes.

## Implementation Checklist

When building a layout system:

1. Start from a canonical layout, not only a grid.
2. Define behavior across all five width classes.
3. Separate navigation and body regions clearly.
4. Ensure all body content belongs to panes.
5. Keep at least one flexible pane in adaptive layouts.
6. Use margins, spacers, and padding deliberately instead of ad hoc gaps.
7. Keep pane spacers at `24dp` unless there is a strong reason not to.
8. Keep layout padding on a `4dp` rhythm.
9. Preserve state and context when posture or window shape changes.
10. Treat density as opt-in, not a silent default shrink.
11. Keep interaction targets at or above `48x48`.
12. Use leading/trailing and mirrored behavior for RTL-aware layouts.
13. Handle RTL exceptions explicitly instead of mirroring everything.
14. Document what reveals, divides, resizes, repositions, and swaps at each width class.
15. Keep focus order aligned with pane order for co-planar layouts and explicitly define modal-pane focus behavior.

## Review Checklist

When reviewing a layout:

- Does the layout adapt by width class instead of by device stereotype?
- Does it preserve a clear navigation region and body region?
- Is all content organized into panes, with at least one flexible pane?
- Are grouping, margins, spacers, and padding consistent?
- Is padding using a clear spacing rhythm?
- Is density only used where it improves scanning or comparison tasks?
- Are interactive targets still accessible after density changes?
- Does the layout account for cutouts, folds, hinges, and multi-window reductions?
- Does the layout preserve context across window and posture transitions?
- Does RTL behavior use leading/trailing logic and correct mirroring exceptions?

## NTComponents Note

This reference is intentionally generic. Component-specific or layout-specific docs should point back to this document rather than restating the same baseline Material 3 layout rules in multiple places.
