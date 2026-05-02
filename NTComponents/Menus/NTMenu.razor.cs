using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
///     Material 3 menu panel that can be anchored to a trigger element.
/// </summary>
/// <remarks>
///     <para>
///         Menus are temporary surfaces for related actions. Use the standard surface-based color mapping for routine action menus, and reserve stronger tertiary-based mappings for menus that need
///         prominent selection emphasis.
///     </para>
///     <para>When anchoring, assign the same CSS anchor name to the trigger with <c>anchor-name</c> and to this menu with <see cref="AnchorName" /> .</para>
///     <para>The smallest button-triggered menu uses a native popover target and matching anchor selector:</para>
///     <code>
/// &lt;NTButton ElementId="actions-button"
///           Label="Actions"
///           style="anchor-name: --actions-menu-anchor;"
///           popovertarget="actions-menu"
///           popovertargetaction="toggle" /&gt;
///
/// &lt;NTMenu ElementId="actions-menu"
///         AnchorName="--actions-menu-anchor"
///         AnchorSelector="#actions-button"
///         AriaLabel="Actions"&gt;
///     &lt;NTMenuButtonItem Label="Rename" /&gt;
/// &lt;/NTMenu&gt;
///     </code>
/// </remarks>
public partial class NTMenu {

    /// <summary>
    ///     Gets or sets the CSS anchor name assigned to the trigger element.
    /// </summary>
    [Parameter]
    public string? AnchorName { get; set; }

    /// <summary>
    ///     Gets or sets a CSS selector for the element that opens and anchors the menu.
    /// </summary>
    [Parameter]
    public string? AnchorSelector { get; set; }

    /// <summary>
    ///     Gets or sets the accessible label for the menu container.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public string? AriaLabel { get; set; }

    /// <summary>
    ///     Gets or sets the menu content.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public RenderFragment? ChildContent { get; set; }

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
    ///     Gets or sets whether the menu and registered items are disabled.
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create("nt-menu")
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("nt-menu-anchor-auto")
        .AddClass("nt-menu-anchor-end")
        .AddClass("nt-menu-placement-auto")
        .AddClass("nt-menu-placement-bottom")
        .AddClass("nt-menu-submenu", IsSubMenu)
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddStyle("position-anchor", AnchorName, !string.IsNullOrWhiteSpace(AnchorName))
        .AddVariable("nt-menu-container-color", ContainerColor.ToCssTnTColorVariable(), ContainerColor.HasValue)
        .AddVariable("nt-menu-content-color", TextColor.ToCssTnTColorVariable(), TextColor.HasValue)
        .AddVariable("nt-menu-selected-container-color", SelectedContainerColor.ToCssTnTColorVariable(), SelectedContainerColor.HasValue)
        .AddVariable("nt-menu-selected-content-color", SelectedTextColor.ToCssTnTColorVariable(), SelectedTextColor.HasValue)
        .Build();

    /// <summary>
    ///     Gets or sets whether this menu is a nested submenu.
    /// </summary>
    [Parameter]
    public bool IsSubMenu { get; set; }

    /// <inheritdoc />
    public override string? JsModulePath => "./_content/NTComponents/Menus/NTMenu.razor.js";

    /// <summary>
    ///     Gets or sets the native popover mode.
    /// </summary>
    [Parameter]
    public string Popover { get; set; } = "auto";

    /// <summary>
    ///     Gets or sets the menu role.
    /// </summary>
    [Parameter]
    public string Role { get; set; } = "menu";

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
    ///     Gets or sets an optional override for the menu text and icon color.
    /// </summary>
    [Parameter]
    public TnTColor? TextColor { get; set; }

    /// <summary>
    ///     Gets whether this menu has at least one actionable registered item.
    /// </summary>
    internal bool HasActionableMenuItems => _menuItems.Any(static item => item.IsActionable);

    /// <summary>
    ///     Gets whether this menu has registered menu items.
    /// </summary>
    internal bool HasRegisteredMenuItems => _menuItems.Count > 0;

    private string CloseOnContentClickAttribute => CloseOnContentClick ? "true" : "false";
    private string IsSubMenuAttribute => IsSubMenu ? "true" : "false";

    private static readonly RenderFragment ButtonRipple = NTRipple.RenderHost();
    private readonly List<INTMenuItem> _menuItems = [];

    internal static IEnumerable<KeyValuePair<string, object>>? GetMenuItemAdditionalAttributes(INTMenuItem item) {
        if (item.AdditionalAttributes is null) {
            return null;
        }

        return GetMenuItemAdditionalAttributesCore(item.AdditionalAttributes);
    }

    internal static bool TryGetAdditionalAttribute(INTMenuItem item, string name, out object? value) {
        value = null;

        if (item.AdditionalAttributes is null) {
            return false;
        }

        foreach (var attribute in item.AdditionalAttributes) {
            if (string.Equals(attribute.Key, name, StringComparison.OrdinalIgnoreCase)) {
                value = attribute.Value;
                return true;
            }
        }

        return false;
    }

    internal string? GetMenuAnchorItemHref(NTMenuAnchorItem item) => IsMenuItemDisabled(item) ? null : item.Href;

    internal string GetMenuDividerClass(NTMenuDividerItem item) => CssClassBuilder.Create("nt-menu-divider")
            .AddClass(GetMenuItemAdditionalClass(item))
            .AddClass("nt-menu-divider-inset", item.Inset)
            .Build();

    internal string? GetMenuItemAriaDisabled(INTMenuItem item) => IsMenuItemDisabled(item) ? "true" : null;

    internal string GetMenuItemAriaLabel(INTMenuItem item) => string.IsNullOrWhiteSpace(item.AriaLabel) ? item.Label : item.AriaLabel;

    internal string GetMenuItemClass(INTMenuItem item) => CssClassBuilder.Create("nt-menu-item")
            .AddClass(GetMenuItemAdditionalClass(item))
            .AddClass("nt-menu-item-selected", item.Selected)
            .AddClass("nt-menu-item-disabled", IsMenuItemDisabled(item))
            .Build();

    internal string? GetMenuItemPopoverTarget(INTMenuItem item) => CloseOnContentClick && !IsMenuItemDisabled(item) ? ElementId : null;

    internal string? GetMenuItemSelectedAttribute(INTMenuItem item) => item.Selected ? "true" : null;

    internal string? GetMenuItemTabIndex(INTMenuItem item) => IsMenuItemDisabled(item) ? "-1" : null;

    internal async Task HandleMenuButtonItemClickAsync(NTMenuButtonItem item, MouseEventArgs args) {
        if (IsMenuItemDisabled(item) || !item.HasInteractiveCallback) {
            return;
        }

        await item.OnClickCallback.InvokeAsync(args);
    }

    internal bool IsMenuItemDisabled(INTMenuItem item) => Disabled || item.Disabled;

    /// <summary>
    ///     Tracks a registered menu item so the menu can render constrained Material 3 menu content.
    /// </summary>
    internal void RegisterMenuItem(INTMenuItem item) {
        if (item is not null && !_menuItems.Contains(item)) {
            _menuItems.Add(item);
            StateHasChanged();
        }
    }

    internal RenderFragment RenderMenuItemContent(INTMenuItem item, TnTIcon? trailingIcon = null) => builder => {
        builder.AddContent(0, ButtonRipple);

        if (item.Icon is not null) {
            builder.OpenElement(1, "span");
            builder.AddAttribute(2, "class", "nt-menu-item-icon");
            builder.AddAttribute(3, "aria-hidden", "true");
            builder.AddContent(4, item.Icon.Render());
            builder.CloseElement();
        }

        builder.OpenElement(5, "span");
        builder.AddAttribute(6, "class", "nt-menu-item-label");
        builder.AddContent(7, item.Label);
        builder.CloseElement();

        if (trailingIcon is not null) {
            builder.OpenElement(8, "span");
            builder.AddAttribute(9, "class", "nt-menu-item-trailing-icon");
            builder.AddAttribute(10, "aria-hidden", "true");
            builder.AddContent(11, trailingIcon.Render());
            builder.CloseElement();
        }
    };

    /// <summary>
    ///     Unregisters a menu item when it is removed.
    /// </summary>
    internal void UnregisterMenuItem(INTMenuItem item) {
        if (item is not null && _menuItems.Remove(item)) {
            StateHasChanged();
        }
    }

    private static IEnumerable<KeyValuePair<string, object>> GetMenuItemAdditionalAttributesCore(IReadOnlyDictionary<string, object?> additionalAttributes) {
        foreach (var attribute in additionalAttributes) {
            if (attribute.Value is not null && !string.Equals(attribute.Key, "class", StringComparison.OrdinalIgnoreCase)) {
                yield return new KeyValuePair<string, object>(attribute.Key, attribute.Value);
            }
        }
    }

    private static string? GetMenuItemAdditionalClass(INTMenuItem item) =>
            TryGetAdditionalAttribute(item, "class", out var @class) ? @class?.ToString() : null;
}
