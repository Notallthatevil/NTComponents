using System.ComponentModel;
using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace NTComponents;

/// <summary>
///     Represents a button action item that registers itself with an <see cref="NTFabMenu" />.
/// </summary>
public sealed partial class NTFabMenuButtonItem : Microsoft.AspNetCore.Components.IComponent, IFabMenuItem, IDisposable {
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

    /// <inheritdoc />
    [Parameter]
    public TnTIcon? Icon { get; set; }

    /// <inheritdoc />
    [Parameter]
    [EditorRequired]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    ///     Invoked when the enabled menu item is clicked.
    /// </summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnClickCallback { get; set; }

    /// <summary>
    ///     The parent FAB menu that owns this item.
    /// </summary>
    [CascadingParameter]
    public NTFabMenu? Parent { get; set; }

    internal bool HasInteractiveCallback => OnClickCallback.HasDelegate;

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
        builder.OpenElement(sequence++, "button");
        builder.SetKey(this);
        builder.AddMultipleAttributes(sequence++, NTFabMenu.GetMenuItemAdditionalAttributes(this));
        builder.AddAttribute(sequence++, "class", owner.GetMenuItemClass(this));
        builder.AddAttribute(sequence++, "type", "button");
        builder.AddAttribute(sequence++, "role", "menuitem");
        builder.AddAttribute(sequence++, "disabled", owner.IsMenuItemDisabled(this));
        builder.AddAttribute(sequence++, "aria-label", owner.GetMenuItemAriaLabel(this));
        builder.AddAttribute(sequence++, "popovertarget", owner.CloseOnMenuContentClick && !owner.IsMenuItemDisabled(this) ? owner.MenuId : null);
        builder.AddAttribute(sequence++, "popovertargetaction", "hide");

        if (HasInteractiveCallback && !NTFabMenu.TryGetAdditionalAttribute(this, "onclick", out _)) {
            builder.AddAttribute(sequence++, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, async args => {
                if (!owner.IsMenuItemDisabled(this) && HasInteractiveCallback) {
                    await OnClickCallback.InvokeAsync(args);
                }
            }));
        }

        owner.RenderMenuItemContent(builder, this);
        builder.CloseElement();
    };

    /// <inheritdoc />
    public Task SetParametersAsync(ParameterView parameters) {
        var previousParent = Parent;
        parameters.SetParameterProperties(this);

        if (Parent is null) {
            throw new InvalidOperationException($"{nameof(NTFabMenuButtonItem)} must be used inside an {nameof(NTFabMenu)}.");
        }

        if (string.IsNullOrWhiteSpace(Label)) {
            throw new InvalidOperationException($"{nameof(NTFabMenuButtonItem)} requires a non-empty {nameof(Label)}.");
        }

        if (Icon is null) {
            Debug.WriteLine($"{nameof(NTFabMenuButtonItem)} should supply an {nameof(Icon)}. Material 3 FAB menu items should keep icons unless removal is necessary.");
        }

        if (!ReferenceEquals(previousParent, Parent)) {
            _registeredParent?.UnregisterMenuItem(this);
            Parent.RegisterMenuItem(this);
            _registeredParent = Parent;
        }

        return Task.CompletedTask;
    }
}
