# TnTBreadcrumbs Implementation Notes

## Goals
- Add a breadcrumb component to the NTComponents library.
- Follow the repository Blazor and SCSS guidance.
- Align the visual treatment with Material 3 principles.
- Support accessibility best practices and static SSR rendering.
- Add both unit coverage and an SSR-focused browser verification.

## Component Shape
- Add `TnTBreadcrumbs.razor`, `TnTBreadcrumbs.razor.cs`, and `TnTBreadcrumbs.razor.scss` in `NTComponents/Breadcrumbs/`.
- Add a public model type `TnTBreadcrumbItem.cs` in the same folder.
- Inherit `TnTBreadcrumbs` from `TnTComponentBase` so the component follows the library's standard attribute, class, style, and element-reference behavior.

## Public API
- `Items` is the only required parameter.
- `AriaLabel` defaults to `Breadcrumb`.
- `TnTBreadcrumbItem` exposes:
  - `Text`
  - `Href`
  - `Icon`
  - `AriaLabel`
  - `Disabled`
  - `IsCurrent`

## Rendering Rules
- Render semantic markup with `nav > ol > li`.
- Render separators between items with `aria-hidden="true"`.
- Render non-current, enabled items with an `href` as links.
- Render the current item as text with `aria-current="page"`.
- Render disabled items as text with `aria-disabled="true"`.
- If no item is marked current, treat the last item as current.
- If more than one item is marked current, throw during parameter validation.
- If an item has no visible text, require `AriaLabel`.

## Styling Direction
- Use Material 3 surface and on-surface tokens already present in the library.
- Keep the component text-forward and low-emphasis for non-current crumbs.
- Use a stronger emphasis for the current page.
- Preserve a 48px touch target for interactive crumbs.
- Allow wrapping on smaller viewports.
- Keep selectors shallow and scoped under `.tnt-breadcrumbs`.

## Tests
- Add bUnit coverage for:
  - required `Items`
  - empty items
  - current-item inference
  - explicit current item behavior
  - invalid multiple current items
  - disabled item rendering
  - icon-only accessibility validation
  - additional attribute merging
  - custom nav `aria-label`
- Add an SSR E2E check in `NTComponents.Tests/E2E/Breadcrumbs/` that validates:
  - semantic breadcrumb markup
  - one current page item
  - no injected script dependency
  - disabled breadcrumb items are not rendered as links

## Demo Integration
- Add a static breadcrumb page in `LiveTest/LiveTest.Client/Pages/Breadcrumbs.razor`.
- Include representative examples:
  - standard path
  - path with leading home icon
  - disabled intermediate item
- Add a side-nav entry in the LiveTest layout.
