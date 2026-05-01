using System.ComponentModel;
using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace NTComponents;

/// <summary>
///     Represents a navigation item that registers itself with an <see cref="NTFabMenu" />.
/// </summary>
public sealed partial class NTFabMenuAnchorItem : Microsoft.AspNetCore.Components.IComponent, IFabMenuItem, IDisposable {
    private NTFabMenu? _registeredParent;

    /// <inheritdoc />
    [Parameter(CaptureUnmatchedValues = true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public Dictionary<string, object?>? AdditionalAttributes { get; set; }

    IReadOnlyDictionary<string, object?>? IFabMenuItem.AdditionalAttributes => AdditionalAttributes;

    /// <inheritdoc />
    [Parameter]
    public string? AriaLabel { get; set; }

    /// <inheritdoc />
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    ///     The destination URL for this menu item.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public string Href { get; set; } = string.Empty;

    /// <inheritdoc />
    [Parameter]
    public TnTIcon? Icon { get; set; }

    /// <inheritdoc />
    [Parameter]
    [EditorRequired]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    ///     Optional anchor target.
    /// </summary>
    [Parameter]
    public string? Target { get; set; }

    /// <summary>
    ///     The parent FAB menu that owns this item.
    /// </summary>
    [CascadingParameter]
    public NTFabMenu? Parent { get; set; }

    /// <inheritdoc />
    public void Attach(RenderHandle renderHandle) { }

    /// <inheritdoc />
    public void Dispose() {
        _registeredParent?.UnregisterMenuItem(this);
        _registeredParent = null;
    }

    /// <inheritdoc />
    public RenderFragment Render(NTFabMenu owner) => builder => {
        var sequence = 0;
        builder.OpenElement(sequence++, "a");
        builder.SetKey(this);
        builder.AddMultipleAttributes(sequence++, NTFabMenu.GetMenuItemAdditionalAttributes(this));
        builder.AddAttribute(sequence++, "class", owner.GetMenuItemClass(this));
        builder.AddAttribute(sequence++, "href", owner.IsMenuItemDisabled(this) ? null : Href);
        builder.AddAttribute(sequence++, "target", Target);
        builder.AddAttribute(sequence++, "role", "menuitem");
        builder.AddAttribute(sequence++, "aria-disabled", owner.IsMenuItemDisabled(this) ? "true" : null);
        builder.AddAttribute(sequence++, "aria-label", owner.GetMenuItemAriaLabel(this));
        builder.AddAttribute(sequence++, "tabindex", owner.IsMenuItemDisabled(this) ? "-1" : null);
        owner.RenderMenuItemContent(builder, this);
        builder.CloseElement();
    };

    /// <inheritdoc />
    public Task SetParametersAsync(ParameterView parameters) {
        var previousParent = Parent;
        parameters.SetParameterProperties(this);

        if (Parent is null) {
            throw new InvalidOperationException($"{nameof(NTFabMenuAnchorItem)} must be used inside an {nameof(NTFabMenu)}.");
        }

        if (string.IsNullOrWhiteSpace(Label)) {
            throw new InvalidOperationException($"{nameof(NTFabMenuAnchorItem)} requires a non-empty {nameof(Label)}.");
        }

        if (string.IsNullOrWhiteSpace(Href)) {
            throw new InvalidOperationException($"{nameof(NTFabMenuAnchorItem)} requires a non-empty {nameof(Href)}.");
        }

        if (Icon is null) {
            Debug.WriteLine($"{nameof(NTFabMenuAnchorItem)} should supply an {nameof(Icon)}. Material 3 FAB menu items should keep icons unless removal is necessary.");
        }

        if (!ReferenceEquals(previousParent, Parent)) {
            _registeredParent?.UnregisterMenuItem(this);
            Parent.RegisterMenuItem(this);
            _registeredParent = Parent;
        }

        return Task.CompletedTask;
    }
}
