using Microsoft.AspNetCore.Components;
using NTComponents.Core;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
///     Material 3 canonical list-detail view for collection and selected-item workflows.
/// </summary>
/// <remarks>
///     <para>Use this view when a browsable list, table, inbox, file browser, settings category list, or similar collection drives one selected detail surface.</para>
///     <para>
///         Best practice: keep the list and detail semantically connected, preserve the selected item when width changes, and let <see cref="NTListDetailViewMode.Auto" /> handle the default
///         compact-to-expanded adaptation unless the product workflow needs a stricter presentation.
///     </para>
///     <para>
///         Do not use this view for unrelated two-column content, peer dashboards, or supplemental tools that are not selected from the list. Use <see cref="NTSupportingPaneView" />, <see
///         cref="NTMultiPaneView" />, or <see cref="NTFeedView" /> for those patterns instead.
///     </para>
/// </remarks>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.ProgressivelyEnhanced,
    CompatibilitySummary = "Renders useful static markup and enhances behavior with browser JavaScript.",
    CompatibilityDetails = "Static SSR emits the component shell and accessible markup. The browser module adds richer behavior after the page reaches the browser.")]
public partial class NTListDetailView {

    /// <summary>
    ///     Detail pane content for the selected item.
    /// </summary>
    /// <remarks>Render the selected item's primary content here. Keep it useful in both single-pane and two-pane presentations, and avoid depending on the list being visible on compact widths.</remarks>
    [Parameter]
    public RenderFragment? Detail { get; set; }

    /// <summary>
    ///     Optional content rendered before detail content, typically for caller-owned back or close controls in single-pane detail views.
    /// </summary>
    /// <remarks>
    ///     Use this for a back, close, or contextual header that is needed when the detail pane replaces the list. Do not duplicate persistent two-pane navigation here; the list remains visible when
    ///     the layout has enough width.
    /// </remarks>
    [Parameter]
    public RenderFragment? DetailHeader { get; set; }

    /// <summary>
    ///     Accessible label for the detail pane.
    /// </summary>
    [Parameter]
    public string DetailPaneLabel { get; set; } = "Detail";

    /// <summary>
    ///     Whether the detail pane is the active compact pane.
    /// </summary>
    /// <remarks>
    ///     Use this to choose the initial single-pane state for SSR and compact widths. Keep it in sync with the selected item in caller-owned state when the page needs deterministic back/forward or
    ///     deep-link behavior.
    /// </remarks>
    [Parameter]
    public bool DetailVisible { get; set; }

    /// <summary>
    ///     Content rendered when no detail content is available.
    /// </summary>
    /// <remarks>Prefer a concise empty state or instructions for selecting an item. Do not use this slot for unrelated page content; the empty state should still describe the list-detail relationship.</remarks>
    [Parameter]
    public RenderFragment? EmptyDetail { get; set; }

    /// <inheritdoc />
    public override string? JsModulePath => ListDetailJsModulePath;

    /// <summary>
    ///     List pane content.
    /// </summary>
    /// <remarks>
    ///     Render the selectable collection here. Use stable selection affordances, accessible labels, and item state so users can understand which detail is active when the view expands to two panes.
    /// </remarks>
    [Parameter]
    public RenderFragment? List { get; set; }

    /// <summary>
    ///     Accessible label for the list pane.
    /// </summary>
    [Parameter]
    public string ListPaneLabel { get; set; } = "List";

    /// <summary>
    ///     Presentation mode for the list-detail panes.
    /// </summary>
    /// <remarks>
    ///     Prefer <see cref="NTListDetailViewMode.Auto" /> for standard Material 3 behavior. Use <see cref="NTListDetailViewMode.SinglePane" /> for focused workflows that should never show list and
    ///     detail together. Use <see cref="NTListDetailViewMode.TwoPane" /> when keeping the list visible materially improves comparison, scanning, or repeated selection.
    /// </remarks>
    [Parameter]
    public NTListDetailViewMode Mode { get; set; }

    /// <inheritdoc />
    protected override string ComponentClass => "nt-list-detail-view";

    private const string ListDetailJsModulePath = "./_content/NTComponents/Layout/Views/NTListDetailView.razor.js";

    /// <inheritdoc />
    protected override void AddComponentClasses(CssClassBuilder builder) {
        builder
            .AddClass("nt-list-detail-view-detail-visible", DetailVisible)
            .AddClass("nt-list-detail-view-single-pane", Mode == NTListDetailViewMode.SinglePane)
            .AddClass("nt-list-detail-view-two-pane", Mode == NTListDetailViewMode.TwoPane);
    }
}