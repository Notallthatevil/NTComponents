using Microsoft.AspNetCore.Components;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
/// Centered page-width container view for readable content regions.
/// </summary>
/// <remarks>
/// <para> Use this view when page content should stay centered and bounded instead of stretching across the full
/// viewport. It replaces the legacy <see cref="TnTContainer" /> width behavior with Material 3 window-size breakpoints.
/// </para> <para> Best practice: use this as a layout wrapper around page sections, forms, articles, or view
/// compositions that need comfortable reading width. Keep shape, color, elevation, and surface styling on the content
/// inside the container. </para> <para> Do not use this view to create cards, panels, or visual surfaces. It controls
/// only horizontal placement and width.</para>
/// </remarks>
public partial class NTContainerView {

    /// <inheritdoc />
    public override string? JsModulePath => ContainerViewJsModulePath;

    /// <summary>
    ///     Content rendered inside the centered container.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    ///     Whether the container should render and hydrate the generated "On this page" navigation.
    /// </summary>
    [Parameter]
    public bool EnableOnThisPageNavigation { get; set; } = true;

    /// <summary>
    ///     Label rendered above the generated on-this-page links.
    /// </summary>
    [Parameter]
    public string OnThisPageLabel { get; set; } = "On this page";

    /// <summary>
    ///     Accessible label for the generated on-this-page navigation landmark. Defaults to <see cref="OnThisPageLabel" />.
    /// </summary>
    [Parameter]
    public string? OnThisPageAriaLabel { get; set; }

    /// <summary>
    ///     Optional title rendered below the "On this page" label.
    /// </summary>
    [Parameter]
    public string? OnThisPageTitle { get; set; }

    /// <summary>
    ///     Optional color override for the "On this page" label.
    /// </summary>
    [Parameter]
    public TnTColor? OnThisPageLabelColor { get; set; } = TnTColor.OnSurfaceVariant;

    /// <summary>
    ///     Optional color override for the quick navigation selector outline.
    /// </summary>
    [Parameter]
    public TnTColor? OnThisPageSelectorColor { get; set; } = TnTColor.Outline;

    /// <summary>
    ///     Optional color override for the selected quick navigation link text.
    /// </summary>
    [Parameter]
    public TnTColor? OnThisPageSelectedTextColor { get; set; } = TnTColor.OnSurface;

    /// <summary>
    ///     Optional color override for quick navigation link text.
    /// </summary>
    [Parameter]
    public TnTColor? OnThisPageTextColor { get; set; } = TnTColor.OnSurface;

    /// <inheritdoc />
    protected override string ComponentClass => "nt-container-view";

    /// <inheritdoc />
    protected override bool ShouldLoadJsModule => EnableOnThisPageNavigation;

    private bool HasOnThisPageTitle => !string.IsNullOrWhiteSpace(OnThisPageTitle);

    private string OnThisPageNavigationAriaLabel => string.IsNullOrWhiteSpace(OnThisPageAriaLabel) ? OnThisPageLabel : OnThisPageAriaLabel;

    private string? OnThisPageNavigationEnabledAttribute => EnableOnThisPageNavigation ? "true" : null;

    private const string ContainerViewJsModulePath = "./_content/NTComponents/Layout/Views/NTContainerView.razor.js";

    /// <inheritdoc />
    protected override void AddComponentStyles(CssStyleBuilder builder) {
        builder
            .AddVariable("nt-container-view-on-this-page-label-color", OnThisPageLabelColor.ToCssTnTColorVariable(), OnThisPageLabelColor.HasValue)
            .AddVariable("nt-container-view-on-this-page-selector-color", OnThisPageSelectorColor.ToCssTnTColorVariable(), OnThisPageSelectorColor.HasValue)
            .AddVariable("nt-container-view-on-this-page-selected-text-color", OnThisPageSelectedTextColor.ToCssTnTColorVariable(), OnThisPageSelectedTextColor.HasValue)
            .AddVariable("nt-container-view-on-this-page-text-color", OnThisPageTextColor.ToCssTnTColorVariable(), OnThisPageTextColor.HasValue);
    }
}
