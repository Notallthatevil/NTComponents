using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace NTComponents;

/// <summary>
///     Represents a menu item that opens a nested submenu.
/// </summary>
public class NTMenuSubMenuItem : Microsoft.AspNetCore.Components.IComponent, INTMenuItem, IDisposable {
    private readonly string _componentId = $"nt-menu-submenu-{Guid.NewGuid():N}";
    private NTMenu? _registeredParent;

    /// <inheritdoc />
    [Parameter(CaptureUnmatchedValues = true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public Dictionary<string, object?>? AdditionalAttributes { get; set; }

    IReadOnlyDictionary<string, object?>? INTMenuItem.AdditionalAttributes => AdditionalAttributes;

    /// <inheritdoc />
    [Parameter]
    public string? AriaLabel { get; set; }

    /// <summary>
    ///     Gets or sets the nested menu content.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <inheritdoc />
    [Parameter]
    public bool Disabled { get; set; }

    /// <inheritdoc />
    [Parameter]
    public TnTIcon? Icon { get; set; }

    /// <inheritdoc />
    public bool IsActionable => true;

    /// <inheritdoc />
    [Parameter]
    [EditorRequired]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    ///     The parent menu that owns this item.
    /// </summary>
    [CascadingParameter]
    public NTMenu? Parent { get; set; }

    /// <inheritdoc />
    [Parameter]
    public bool Selected { get; set; }

    private string AnchorName => $"--{_componentId}-anchor";
    private string AnchorSelector => $"#{ButtonId}";
    private string ButtonId => $"{_componentId}-button";
    private string ButtonStyle => $"anchor-name: {AnchorName};";
    private string SubMenuId => $"{_componentId}-menu";
    private static TnTIcon TrailingIcon => MaterialIcon.ChevronRight;

    /// <inheritdoc />
    public void Attach(RenderHandle renderHandle) { }

    /// <inheritdoc />
    public void Dispose() {
        _registeredParent?.UnregisterMenuItem(this);
        _registeredParent = null;
    }

    /// <inheritdoc />
    public RenderFragment Render(NTMenu owner) => builder => {
        var sequence = 0;
        builder.OpenElement(sequence++, "button");
        builder.SetKey(this);
        builder.AddMultipleAttributes(sequence++, NTMenu.GetMenuItemAdditionalAttributes(this));
        builder.AddAttribute(sequence++, "class", owner.GetMenuItemClass(this));
        builder.AddAttribute(sequence++, "id", ButtonId);
        builder.AddAttribute(sequence++, "style", ButtonStyle);
        builder.AddAttribute(sequence++, "type", "button");
        builder.AddAttribute(sequence++, "role", "menuitem");
        builder.AddAttribute(sequence++, "aria-disabled", owner.GetMenuItemAriaDisabled(this));
        builder.AddAttribute(sequence++, "aria-haspopup", "menu");
        builder.AddAttribute(sequence++, "aria-controls", SubMenuId);
        builder.AddAttribute(sequence++, "aria-expanded", "false");
        builder.AddAttribute(sequence++, "aria-label", owner.GetMenuItemAriaLabel(this));
        builder.AddAttribute(sequence++, "aria-selected", owner.GetMenuItemSelectedAttribute(this));
        builder.AddAttribute(sequence++, "data-nt-menu-disabled", owner.GetMenuItemDisabledAttribute(this));
        builder.AddAttribute(sequence++, "data-nt-menu-submenu-trigger", "true");
        builder.AddAttribute(sequence++, "popovertarget", owner.IsMenuItemDisabled(this) ? null : SubMenuId);
        builder.AddAttribute(sequence++, "popovertargetaction", "toggle");
        builder.AddContent(sequence++, owner.RenderMenuItemContent(this, TrailingIcon));
        builder.CloseElement();

        builder.OpenComponent<NTMenu>(sequence++);
        builder.SetKey(SubMenuId);
        builder.AddAttribute(sequence++, nameof(NTMenu.ElementId), SubMenuId);
        builder.AddAttribute(sequence++, "class", "nt-menu-submenu");
        builder.AddAttribute(sequence++, nameof(NTMenu.AnchorName), AnchorName);
        builder.AddAttribute(sequence++, nameof(NTMenu.AnchorSelector), AnchorSelector);
        builder.AddAttribute(sequence++, nameof(NTMenu.Appearance), owner.Appearance);
        builder.AddAttribute(sequence++, nameof(NTMenu.AriaLabel), owner.GetMenuItemAriaLabel(this));
        builder.AddAttribute(sequence++, nameof(NTMenu.CloseOnContentClick), owner.CloseOnContentClick);
        builder.AddAttribute(sequence++, nameof(NTMenu.ContainerColor), owner.ContainerColor);
        builder.AddAttribute(sequence++, nameof(NTMenu.Disabled), owner.IsMenuItemDisabled(this));
        builder.AddAttribute(sequence++, nameof(NTMenu.Elevation), owner.Elevation);
        builder.AddAttribute(sequence++, nameof(NTMenu.IsSubMenu), true);
        builder.AddAttribute(sequence++, nameof(NTMenu.SelectedContainerColor), owner.SelectedContainerColor);
        builder.AddAttribute(sequence++, nameof(NTMenu.SelectedTextColor), owner.SelectedTextColor);
        builder.AddAttribute(sequence++, nameof(NTMenu.TextColor), owner.TextColor);
        builder.AddAttribute(sequence++, nameof(NTMenu.ChildContent), ChildContent);
        builder.CloseComponent();
    };

    /// <inheritdoc />
    public Task SetParametersAsync(ParameterView parameters) {
        var previousParent = Parent;
        parameters.SetParameterProperties(this);

        if (Parent is null) {
            throw new InvalidOperationException($"{GetType().Name} must be used inside an {nameof(NTMenu)}.");
        }

        if (string.IsNullOrWhiteSpace(Label)) {
            throw new InvalidOperationException($"{GetType().Name} requires a non-empty {nameof(Label)}.");
        }

        if (!ReferenceEquals(previousParent, Parent)) {
            _registeredParent?.UnregisterMenuItem(this);
            Parent.RegisterMenuItem(this);
            _registeredParent = Parent;
        }

        return Task.CompletedTask;
    }
}
