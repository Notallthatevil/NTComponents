using Microsoft.AspNetCore.Components;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
///     Specifies the appearance of a tab view.
/// </summary>
public enum TabViewAppearance {

    /// <summary>
    ///     The primary appearance of the tab view.
    /// </summary>
    Primary,

    /// <summary>
    ///     The secondary appearance of the tab view.
    /// </summary>
    Secondary
}

/// <summary>
///     Represents a tab view component.
/// </summary>
public partial class TnTTabView {

    /// <summary>
    ///     Gets or sets the color of the active indicator.
    /// </summary>
    [Parameter]
    public TnTColor ActiveIndicatorColor { get; set; } = TnTColor.Primary;

    /// <summary>
    ///     Gets or sets the alignment of the tab headers within the tab view.
    /// </summary>
    [Parameter]
    public NTTabViewAlignment Alignment { get; set; } = NTTabViewAlignment.Center;

    /// <summary>
    ///     Gets or sets the appearance of the tab view.
    /// </summary>
    [Parameter]
    public TabViewAppearance Appearance { get; set; } = TabViewAppearance.Primary;

    /// <summary>
    ///     Gets or sets the child content to be rendered inside the tab view.
    /// </summary>
    [Parameter]
    public RenderFragment ChildContent { get; set; } = default!;

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("tnt-tab-view")
        .AddClass("tnt-tab-view-secondary", Appearance == TabViewAppearance.Secondary)
        .AddClass($"tnt-tab-view-alignment-{Alignment.ToString().ToLowerInvariant()}")
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddVariable("tnt-tab-view-active-indicator-color", ActiveIndicatorColor)
        .AddVariable("tnt-tab-view-header-button-gap-size", $"{HeaderButtonGapSize}px")
        .AddVariable("tnt-tab-view-header-border-size", $"{HeaderBorderHeight}px")
        .Build();

    /// <summary>
    ///     Gets or sets the background color of the tab header.
    /// </summary>
    [Parameter]
    public TnTColor HeaderBackgroundColor { get; set; } = TnTColor.Surface;

    /// <summary>
    ///     Gets or sets the text color of the tab header.
    /// </summary>
    [Parameter]
    public TnTColor HeaderTextColor { get; set; } = TnTColor.OnSurface;

    /// <summary>
    ///     The tint color for the tab header, used for ripple effects and other visual enhancements.
    /// </summary>
    [Parameter]
    public TnTColor HeaderTintColor { get; set; } = TnTColor.SurfaceTint;

    /// <summary>
    /// Gets or sets the gap size, in pixels, between header buttons.
    /// </summary>
    [Parameter]
    public int HeaderButtonGapSize { get; set; } = 0;

    /// <summary>
    /// Gets or sets the height of the border, in pixels, for the component.
    /// </summary>
    [Parameter]
    public int HeaderBorderHeight { get; set; } = 1;

    /// <inheritdoc />
    public override string? JsModulePath => "./_content/NTComponents/TabView/TnTTabView.razor.js";

    private readonly List<TnTTabChild> _tabChildren = [];

    /// <summary>
    ///     Adds a child tab to the tab view.
    /// </summary>
    /// <param name="tabChild">The child tab to add.</param>
    public void AddTabChild(TnTTabChild tabChild) => _tabChildren.Add(tabChild);

    /// <summary>
    ///     Removes a child tab from the tab view.
    /// </summary>
    /// <param name="tabChild">The child to remove</param>
    public void RemoveTabChild(TnTTabChild tabChild) => _tabChildren.Remove(tabChild);
}

/// <summary>
///     Specifies the alignment options for tab view content.
/// </summary>
/// <remarks>Use this enumeration to control how tabs are aligned within a tab view component. The alignment affects the positioning of tab headers or content, depending on the implementation.</remarks>
public enum NTTabViewAlignment {

    /// <summary>
    ///     Specifies that content is aligned to the center.
    /// </summary>
    Center,

    /// <summary>
    ///     Represents the starting point or initial state.
    /// </summary>
    Start,

    /// <summary>
    ///     Gets or sets the end value or position.
    /// </summary>
    End
}