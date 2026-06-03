using Microsoft.AspNetCore.Components;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
///     Native SSR-compatible disclosure item for <see cref="NTAccordion" />.
/// </summary>
public partial class NTAccordionItem : TnTComponentBase {

    /// <summary>
    ///     Item body content rendered after the summary row.
    /// </summary>
    [Parameter]
    public RenderFragment ChildContent { get; set; } = default!;

    /// <summary>
    ///     Optional content background override for this item.
    /// </summary>
    [Parameter]
    public TnTColor? ContentColor { get; set; }

    /// <summary>
    ///     Optional content text color override for this item.
    /// </summary>
    [Parameter]
    public TnTColor? ContentTextColor { get; set; }

    /// <summary>
    ///     When set, renders the item as a non-interactive static surface.
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("nt-accordion-item")
        .AddClass("nt-accordion-item-open", Disabled && RenderedOpen)
        .AddClass("nt-accordion-item-compact", IsCompact)
        .AddClass("nt-accordion-item-disabled", Disabled)
        .AddClass("nt-accordion-item-with-leading-icon", LeadingIcon is not null)
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddVariable("nt-accordion-header-color", HeaderColor.ToCssTnTColorVariable(), HeaderColor.HasValue)
        .AddVariable("nt-accordion-header-text-color", HeaderTextColor.ToCssTnTColorVariable(), HeaderTextColor.HasValue)
        .AddVariable("nt-accordion-content-color", ContentColor.ToCssTnTColorVariable(), ContentColor.HasValue)
        .AddVariable("nt-accordion-content-text-color", ContentTextColor.ToCssTnTColorVariable(), ContentTextColor.HasValue)
        .Build();

    /// <summary>
    ///     Optional native <c>details</c> group name. A parent <see cref="NTAccordion" /> with <see cref="NTAccordion.LimitToOneExpanded" /> overrides this value.
    /// </summary>
    [Parameter]
    public string? GroupName { get; set; }

    /// <summary>
    ///     Optional custom non-interactive summary content. When set, <see cref="Label" /> is ignored.
    /// </summary>
    /// <remarks>
    ///     The content is rendered inside native <c>summary</c>, which is already the disclosure control. Avoid nested interactive controls such as buttons and links.
    /// </remarks>
    [Parameter]
    public RenderFragment? HeaderContent { get; set; }

    /// <summary>
    ///     Optional summary row background color override for this item.
    /// </summary>
    [Parameter]
    public TnTColor? HeaderColor { get; set; }

    /// <summary>
    ///     Optional summary row text color override for this item.
    /// </summary>
    [Parameter]
    public TnTColor? HeaderTextColor { get; set; }

    /// <summary>
    ///     Text rendered in the summary row when <see cref="HeaderContent" /> is not provided.
    /// </summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>
    ///     Optional leading Material icon shown before the label.
    /// </summary>
    [Parameter]
    public TnTIcon? LeadingIcon { get; set; }

    /// <summary>
    ///     Initial native open state for the item.
    /// </summary>
    [Parameter]
    public bool Open { get; set; }

    /// <summary>
    ///     When set, emits <c>role="region"</c> on the content panel.
    /// </summary>
    [Parameter]
    public bool UseRegionRole { get; set; }

    /// <summary>
    ///     Optional secondary text rendered below the label in the summary row.
    /// </summary>
    [Parameter]
    public string? SupportingText { get; set; }

    private string? EffectiveName => ParentAccordion?.DetailsGroupName ?? GroupName;
    private bool IsCompact => ParentAccordion?.Appearance == NTAccordionAppearance.Compact;
    private string? PanelRole => UseRegionRole ? "region" : null;
    private bool RenderedOpen => _renderedOpen;
    private string SummaryElementId => $"{ComponentIdentifier}-summary";
    private bool _renderedOpen;

    [CascadingParameter]
    private NTAccordion? ParentAccordion { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();

        if (HeaderContent is null && string.IsNullOrWhiteSpace(Label)) {
            throw new InvalidOperationException($"{nameof(NTAccordionItem)} requires either {nameof(Label)} or {nameof(HeaderContent)} to be set.");
        }

        _renderedOpen = ParentAccordion?.ReserveRenderedOpen(this, Open) ?? Open;
    }
}
