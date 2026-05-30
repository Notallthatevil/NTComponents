using Microsoft.AspNetCore.Components;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
///     Material 3 context menu wrapper that opens an <see cref="NTMenu" /> from pointer, keyboard, or long-press invocation.
/// </summary>
/// <remarks>
///     Use <see cref="TargetContent" /> for the interactive region and <see cref="MenuContent" /> for standard <see cref="NTMenuButtonItem" />, <see cref="NTMenuAnchorItem" />,
///     <see cref="NTMenuLabelItem" />, <see cref="NTMenuDividerItem" />, and <see cref="NTMenuSubMenuItem" /> content.
/// </remarks>
public partial class NTContextMenu {

    /// <summary>
    ///     Gets or sets the visual density of the opened menu.
    /// </summary>
    [Parameter]
    public NTMenuAppearance Appearance { get; set; } = NTMenuAppearance.Standard;

    /// <summary>
    ///     Gets or sets the accessible label for the context menu.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public string? AriaLabel { get; set; }

    /// <summary>
    ///     Gets or sets whether item clicks should close the owning popover menu.
    /// </summary>
    [Parameter]
    public bool CloseOnContentClick { get; set; } = true;

    /// <summary>
    ///     Gets or sets an optional override for the menu container color.
    /// </summary>
    [Parameter]
    public TnTColor? ContainerColor { get; set; }

    /// <summary>
    ///     Gets or sets whether context menu invocation is disabled.
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create("nt-context-menu")
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddDisabled(Disabled)
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .Build();

    /// <summary>
    ///     Gets or sets the elevation level used by the floating menu surface.
    /// </summary>
    [Parameter]
    public NTElevation Elevation { get; set; } = NTElevation.Medium;

    /// <summary>
    ///     Gets or sets whether this context menu should append the menu content from ancestor <see cref="NTContextMenu" /> components.
    /// </summary>
    [Parameter]
    public bool InheritParentMenus { get; set; }

    /// <inheritdoc />
    public override string? JsModulePath => "./_content/NTComponents/Menus/NTContextMenu.razor.js";

    /// <summary>
    ///     Gets or sets the long-press delay in milliseconds for touch and pen context menu invocation.
    /// </summary>
    [Parameter]
    public int LongPressDelay { get; set; } = 500;

    /// <summary>
    ///     Gets or sets the context menu content.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public RenderFragment? MenuContent { get; set; }

    /// <summary>
    ///     Gets or sets the nearest ancestor context menu, when this context menu is nested inside another context menu target.
    /// </summary>
    [CascadingParameter]
    public NTContextMenu? ParentContextMenu { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the selected menu item container color.
    /// </summary>
    [Parameter]
    public TnTColor? SelectedContainerColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for selected menu item text and icon color.
    /// </summary>
    [Parameter]
    public TnTColor? SelectedTextColor { get; set; }

    /// <summary>
    ///     Gets or sets the target content that receives context menu gestures.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public RenderFragment? TargetContent { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the menu text and icon color.
    /// </summary>
    [Parameter]
    public TnTColor? TextColor { get; set; }

    private IReadOnlyList<RenderFragment> InheritedParentMenuContent =>
        InheritParentMenus && ParentContextMenu is not null
            ? ParentContextMenu.EnumerateMenuContentWithAncestors().ToArray()
            : [];

    private string DisabledAttribute => Disabled ? "true" : "false";
    private string MenuId => $"{StableElementId}-menu";
    private string StableElementId => string.IsNullOrWhiteSpace(ElementId) ? ComponentIdentifier : ElementId;

    private IEnumerable<RenderFragment> EnumerateMenuContentWithAncestors() {
        if (MenuContent is not null) {
            yield return MenuContent;
        }

        if (ParentContextMenu is null) {
            yield break;
        }

        foreach (var menuContent in ParentContextMenu.EnumerateMenuContentWithAncestors()) {
            yield return menuContent;
        }
    }
}
