using Microsoft.AspNetCore.Components;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
///     Material 3 canonical supporting-pane view for primary content with related secondary context.
/// </summary>
/// <remarks>
///     <para>Use this view when one primary task surface owns the user's work and a secondary pane provides related context, tools, metadata, comments, properties, or supplemental actions.</para>
///     <para>Best practice: keep the primary pane complete and understandable on its own, then use the supporting pane to reduce mode switching or reveal helpful context when width allows.</para>
///     <para>
///         Do not use the supporting pane for peer content columns, required navigation, or controls that are essential on compact screens when using <see
///         cref="NTSupportingPaneViewMode.HideOnSmallScreens" />. Use <see cref="NTMultiPaneView" /> for peer panes and <see cref="NTListDetailView" /> when a list selects a detail.
///     </para>
/// </remarks>
public partial class NTSupportingPaneView {

    /// <inheritdoc />
    public override string? JsModulePath => SupportingPaneJsModulePath;

    /// <summary>
    ///     Presentation mode for the supporting pane.
    /// </summary>
    /// <remarks>
    ///     Prefer <see cref="NTSupportingPaneViewMode.Auto" /> when supporting content is useful at every width. Use <see cref="NTSupportingPaneViewMode.Stacked" /> when the secondary content should
    ///     read as continuation content. Use <see cref="NTSupportingPaneViewMode.HideOnSmallScreens" /> only when the primary pane remains fully usable before the supporting pane appears at 840px and wider.
    /// </remarks>
    [Parameter]
    public NTSupportingPaneViewMode Mode { get; set; }

    /// <summary>
    ///     Primary pane content.
    /// </summary>
    /// <remarks>
    ///     Render the main task, document, editor, media, or working surface here. Do not make this pane depend on the supporting pane to complete critical actions at compact or medium widths.
    /// </remarks>
    [Parameter]
    public RenderFragment? Primary { get; set; }

    /// <summary>
    ///     Accessible label for the primary pane.
    /// </summary>
    [Parameter]
    public string PrimaryPaneLabel { get; set; } = "Primary content";

    /// <summary>
    ///     Supporting pane content.
    /// </summary>
    /// <remarks>
    ///     Render contextual information, comments, related tools, properties, or metadata here. Keep this pane
    ///     supplemental: if the content is equally important to the primary pane, use <see cref="NTMultiPaneView" /> instead.
    /// </remarks>
    [Parameter]
    public RenderFragment? Supporting { get; set; }

    /// <summary>
    ///     Accessible label for the supporting pane.
    /// </summary>
    [Parameter]
    public string SupportingPaneLabel { get; set; } = "Supporting content";

    /// <inheritdoc />
    protected override string ComponentClass => "nt-supporting-pane-view";

    private const string SupportingPaneJsModulePath = "./_content/NTComponents/Layout/Views/NTSupportingPaneView.razor.js";

    /// <inheritdoc />
    protected override void AddComponentClasses(CssClassBuilder builder) {
        builder
            .AddClass("nt-supporting-pane-view-stacked", Mode == NTSupportingPaneViewMode.Stacked)
            .AddClass("nt-supporting-pane-view-hide-on-small-screens", Mode == NTSupportingPaneViewMode.HideOnSmallScreens);
    }
}