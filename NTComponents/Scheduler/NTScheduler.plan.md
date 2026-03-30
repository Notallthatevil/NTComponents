# NTScheduler Implementation Plan

## Goals
- Replace the legacy `TnTScheduler` experience with a new `NTScheduler` component designed around month, week, and day views.
- Support event creation, editing, deletion, and drag/drop rescheduling.
- Use Blazor-first rendering with an isolated JavaScript module for pointer-driven drag/drop so the component works in interactive SSR scenarios.
- Follow repository Blazor, style, testing, accessibility, and Material 3 guidance.

## Architecture
- Keep `TnTScheduler` intact as the legacy component.
- Introduce a new `NTScheduler<TEventType>` shell with:
  - bound `Date` and `View`
  - a Material 3 toolbar
  - built-in event editor
  - callback-driven event notifications
  - internal working collection based on `ICollection<TEventType>`
- Split rendering into focused child components:
  - `NTSchedulerMonthView`
  - `NTSchedulerWeekView`
  - `NTSchedulerDayView`
- Use small scheduler-specific models for:
  - month cells
  - time slots
  - timed event layout
  - event change notifications

## Interaction Model
- Click empty month cells or time-grid slots to create an event draft.
- Click events to edit them in a built-in dialog surface.
- Use JavaScript-backed drag/drop to move events and marshal the result back to .NET through `JSInvokable`.
- Keep keyboard access available through standard buttons and dialog controls even when drag/drop is unavailable.

## SSR Strategy
- Render the scheduler markup server-side.
- Enhance it with `NTScheduler.razor.js` for drag/drop behavior.
- Treat full interaction as available when `RendererInfo.IsInteractive` is true.
- In non-interactive static SSR, keep the component readable and disable interactive affordances that require a live .NET circuit.

## Testing Strategy
- Add bUnit coverage for:
  - toolbar rendering and view switching
  - create/edit/delete flows
  - JS callback-based event moves
  - static SSR behavior
- Keep tests focused on business behavior rather than implementation details.

## Follow-up Opportunities
- Add resize handles for duration edits.
- Add disabled dates/times and business-hour constraints.
- Add richer templates for event content and editor fields.
- Add Playwright coverage for full pointer drag/drop flows after the base component stabilizes.
