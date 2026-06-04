using Microsoft.AspNetCore.Components;
using NTComponents.Core;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
///     Peer-pane view that evenly distributes one to five panes when space allows.
/// </summary>
/// <remarks>
///     <para>
///         Use this view when panes have equal importance, such as dashboard columns, claim summary groups, operational panels,
///         or comparison surfaces that should share width as peers.
///     </para>
///     <para>
///         Best practice: keep panes independent and scannable, cap the peer count to the amount of content users can compare,
///         and let rows re-balance at narrower widths instead of forcing horizontal overflow.
///     </para>
///     <para>
///         Material 3 canonical layouts recommend one to three panes, with three panes primarily reserved for extra-large widths.
///         This component allows up to five panes for NTComponents operational dashboards; use that expanded capacity sparingly.
///     </para>
///     <para>
///         Do not use this view when one pane is dominant and another is contextual. Use <see cref="NTSupportingPaneView" /> for
///         that relationship, <see cref="NTListDetailView" /> for collection-to-detail flows, and <see cref="NTFeedView" /> for
///         card discovery surfaces.
///     </para>
/// </remarks>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.SsrCompatible,
    CompatibilitySummary = "Renders useful static HTML without requiring Blazor interactivity.",
    CompatibilityDetails = "Static SSR preserves the component structure, styling, and accessibility semantics. Dynamic parameter changes require a new render.")]
public partial class NTMultiPaneView {
    private int _effectivePaneCount = 2;
    private NTMultiPaneViewMinPaneWidth _effectiveMinPaneWidth = NTMultiPaneViewMinPaneWidth.Medium;

    /// <summary>
    ///     Uses one shared grid track set across all rows. When false, wrapped rows size independently.
    /// </summary>
    /// <remarks>
    ///     Leave this as <see langword="false" /> for the browser-first behavior that lets each wrapped row fill the available
    ///     width evenly. Set it to <see langword="true" /> only when every row must preserve the same track sizing for strict
    ///     visual alignment.
    /// </remarks>
    [Parameter]
    public bool EnforceEvenSizing { get; set; }

    /// <summary>
    ///     Peer panes rendered as direct children. Each direct child is treated as one pane.
    /// </summary>
    /// <remarks>
    ///     Render complete pane surfaces as direct children. Do not wrap all panes in an extra container, because the view sizes
    ///     only direct children as panes.
    /// </remarks>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    ///     Minimum inline size token each pane should use before equal columns are allowed.
    /// </summary>
    /// <remarks>
    ///     Choose the smallest token at which a pane remains readable and actionable. Fixed tokens keep all wrapping rules in
    ///     static CSS instead of rendering per-instance style tags.
    /// </remarks>
    [Parameter]
    public NTMultiPaneViewMinPaneWidth MinPaneWidth { get; set; } = NTMultiPaneViewMinPaneWidth.Medium;

    /// <summary>
    ///     Maximum number of equal panes per row when width allows.
    /// </summary>
    /// <remarks>
    ///     Values are clamped to one through five. Prefer one to three panes for Material 3-aligned layouts, and reserve four or
    ///     five panes for dense internal tools where users benefit from comparing several peer panels at once.
    /// </remarks>
    [Parameter]
    public int PaneCount { get; set; } = 2;

    /// <inheritdoc />
    protected override string ComponentClass => "nt-multi-pane-view";

    /// <inheritdoc />
    protected override void OnParametersSet() {
        _effectivePaneCount = Math.Clamp(PaneCount, 1, 5);
        _effectiveMinPaneWidth = NormalizeMinPaneWidth(MinPaneWidth);

        base.OnParametersSet();
    }

    /// <inheritdoc />
    protected override void AddComponentClasses(CssClassBuilder builder) {
        builder
            .AddClass($"nt-multi-pane-view-{_effectivePaneCount}-panes")
            .AddClass(GetMinPaneWidthClass(_effectiveMinPaneWidth))
            .AddClass("nt-multi-pane-view-staged-sizing", !EnforceEvenSizing)
            .AddClass("nt-multi-pane-view-enforce-even-sizing", EnforceEvenSizing);
    }

    private static string GetMinPaneWidthClass(NTMultiPaneViewMinPaneWidth minPaneWidth) =>
        minPaneWidth switch {
            NTMultiPaneViewMinPaneWidth.Compact => "nt-multi-pane-view-min-pane-width-compact",
            NTMultiPaneViewMinPaneWidth.Large => "nt-multi-pane-view-min-pane-width-large",
            NTMultiPaneViewMinPaneWidth.ExtraLarge => "nt-multi-pane-view-min-pane-width-extra-large",
            _ => "nt-multi-pane-view-min-pane-width-medium"
        };

    private static NTMultiPaneViewMinPaneWidth NormalizeMinPaneWidth(NTMultiPaneViewMinPaneWidth minPaneWidth) =>
        minPaneWidth is NTMultiPaneViewMinPaneWidth.Compact
            or NTMultiPaneViewMinPaneWidth.Medium
            or NTMultiPaneViewMinPaneWidth.Large
            or NTMultiPaneViewMinPaneWidth.ExtraLarge
                ? minPaneWidth
                : NTMultiPaneViewMinPaneWidth.Medium;
}
