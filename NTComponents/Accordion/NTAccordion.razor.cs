using Microsoft.AspNetCore.Components;
using NTComponents.Core;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
///     SSR-compatible Material 3-aligned disclosure container for grouping native disclosure items.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="NTAccordion" /> is intentionally built on native <c>details</c> and <c>summary</c> elements through <see cref="NTAccordionItem" />. Expand and collapse behavior therefore works
///         in static server-side rendering without JavaScript, JS interop, hydration, or an interactive Blazor circuit.
///     </para>
///     <para>
///         Use <see cref="LimitToOneExpanded" /> when the accordion should keep only one item open (expanded). The component emits a shared native <c>name</c> value for its child
///         <see cref="NTAccordionItem" /> elements so browsers can enforce the group without script.
///     </para>
///     <para>
///         Native <c>details name</c> grouping is a modern browser feature. Browsers without support still render all items and allow independent expand/collapse behavior.
///     </para>
/// </remarks>
/// <example>
/// <code><![CDATA[
/// <NTAccordion LimitToOneExpanded="true">
///     <NTAccordionItem Label="Account details">
///         Account content
///     </NTAccordionItem>
///     <NTAccordionItem Label="Preferences">
///         Preference content
///     </NTAccordionItem>
/// </NTAccordion>
/// ]]></code>
/// </example>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.SsrCompatible,
    CompatibilitySummary = "Renders native disclosure markup that works in static SSR.",
    CompatibilityDetails = "The component uses native details and summary behavior through NTAccordionItem, so expand and collapse works without Blazor interactivity or JavaScript.")]
public partial class NTAccordion : NTComponentBase {

    /// <summary>
    ///     Optional background color override for item content regions.
    /// </summary>
    [Parameter]
    public TnTColor? ContentColor { get; set; }

    /// <summary>
    ///     Text color for item content regions.
    /// </summary>
    [Parameter]
    public TnTColor ContentTextColor { get; set; } = TnTColor.OnSurface;

    /// <summary>
    ///     The accordion items to render.
    /// </summary>
    [Parameter]
    public RenderFragment ChildContent { get; set; } = default!;

    /// <summary>
    ///     Density and layout treatment for accordion items.
    /// </summary>
    [Parameter]
    public NTAccordionAppearance Appearance { get; set; } = NTAccordionAppearance.Standard;

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("nt-accordion")
        .AddClass("nt-accordion-compact", Appearance == NTAccordionAppearance.Compact)
        .AddClass("nt-accordion-filled", Variant == NTAccordionVariant.Filled)
        .AddClass("nt-accordion-outlined", Variant == NTAccordionVariant.Outlined)
        .AddClass("nt-accordion-elevated", Variant == NTAccordionVariant.Elevated)
        .AddClass("nt-accordion-limit-one-expanded", LimitToOneExpanded)
        .AddClass("nt-accordion-separated", Separated)
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddVariable("nt-accordion-header-color", HeaderColor.ToCssTnTColorVariable(), HeaderColor.HasValue)
        .AddVariable("nt-accordion-header-text-color", HeaderTextColor)
        .AddVariable("nt-accordion-content-color", ContentColor.ToCssTnTColorVariable(), ContentColor.HasValue)
        .AddVariable("nt-accordion-content-text-color", ContentTextColor)
        .AddVariable("nt-accordion-outline-color", OutlineColor)
        .AddVariable("nt-accordion-state-layer-color", StateLayerColor.ToCssTnTColorVariable(), StateLayerColor.HasValue)
        .Build();

    /// <summary>
    ///     Shared native <c>details</c> group name used when <see cref="LimitToOneExpanded" /> is enabled.
    /// </summary>
    [Parameter]
    public string? GroupName { get; set; }

    /// <summary>
    ///     Optional background color override for item summary rows.
    /// </summary>
    [Parameter]
    public TnTColor? HeaderColor { get; set; }

    /// <summary>
    ///     Text color for item summary rows.
    /// </summary>
    [Parameter]
    public TnTColor HeaderTextColor { get; set; } = TnTColor.OnSurface;

    /// <summary>
    ///     When set, native browser behavior limits the group to one expanded item at a time.
    /// </summary>
    [Parameter]
    public bool LimitToOneExpanded { get; set; }

    /// <summary>
    ///     Outline and focus-ring color.
    /// </summary>
    [Parameter]
    public TnTColor OutlineColor { get; set; } = TnTColor.Outline;

    /// <summary>
    ///     When set, each item is rendered as an individually rounded surface with spacing between items.
    /// </summary>
    [Parameter]
    public bool Separated { get; set; }

    /// <summary>
    ///     Color used for summary row hover, focus, and pressed state layers.
    /// </summary>
    [Parameter]
    public TnTColor? StateLayerColor { get; set; }

    /// <summary>
    ///     Visual treatment for the accordion surfaces.
    /// </summary>
    [Parameter]
    public NTAccordionVariant Variant { get; set; } = NTAccordionVariant.Filled;

    internal string? DetailsGroupName => LimitToOneExpanded ? GroupName ?? ComponentIdentifier : null;

    private NTAccordionItem? _reservedOpenItem;

    internal bool ReserveRenderedOpen(NTAccordionItem item, bool open) {
        if (!open) {
            return false;
        }

        if (!LimitToOneExpanded) {
            return true;
        }

        if (_reservedOpenItem is null || ReferenceEquals(_reservedOpenItem, item)) {
            _reservedOpenItem = item;
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    protected override void OnParametersSet() {
        _reservedOpenItem = null;
        base.OnParametersSet();
    }
}

/// <summary>
///     Density and label-layout treatments for <see cref="NTAccordion" />.
/// </summary>
public enum NTAccordionAppearance {

    /// <summary>
    ///     Standard Material-style spacing with supporting text below the item label.
    /// </summary>
    Standard,

    /// <summary>
    ///     Denser spacing with supporting text inlined beside the item label.
    /// </summary>
    Compact
}

/// <summary>
///     Visual variants for <see cref="NTAccordion" />.
/// </summary>
public enum NTAccordionVariant {

    /// <summary>
    ///     Filled neutral surfaces with subtle separation.
    /// </summary>
    Filled,

    /// <summary>
    ///     Transparent surfaces with explicit outlines.
    /// </summary>
    Outlined,

    /// <summary>
    ///     Raised surfaces with low resting elevation.
    /// </summary>
    Elevated
}
