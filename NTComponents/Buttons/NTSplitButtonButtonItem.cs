using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace NTComponents;

/// <summary>
///     Represents a button action item that registers itself with an <see cref="NTSplitButton" /> menu.
/// </summary>
public sealed partial class NTSplitButtonButtonItem : Microsoft.AspNetCore.Components.IComponent, ISplitButtonItem, IDisposable {
    private NTSplitButton? _registeredParent;

    /// <inheritdoc />
    [Parameter(CaptureUnmatchedValues = true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public Dictionary<string, object?>? AdditionalAttributes { get; set; }

    IReadOnlyDictionary<string, object?>? ISplitButtonItem.AdditionalAttributes => AdditionalAttributes;

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
    ///     The parent split button that owns this item.
    /// </summary>
    [CascadingParameter]
    public NTSplitButton? Parent { get; set; }

    /// <inheritdoc />
    public void Attach(RenderHandle renderHandle) { }

    /// <inheritdoc />
    public void Dispose() {
        _registeredParent?.UnregisterMenuItem(this);
        _registeredParent = null;
    }

    /// <inheritdoc />
    public RenderFragment Render(NTSplitButton owner) => builder => {
        var sequence = 0;
        builder.OpenElement(sequence++, "button");
        builder.AddMultipleAttributes(sequence++, NTSplitButton.GetMenuItemAdditionalAttributes(this));
        builder.AddAttribute(sequence++, "class", owner.GetMenuItemClass(this));
        builder.AddAttribute(sequence++, "type", "button");
        builder.AddAttribute(sequence++, "role", "menuitem");
        builder.AddAttribute(sequence++, "disabled", owner.IsMenuItemDisabled(this));
        builder.AddAttribute(sequence++, "aria-label", owner.GetMenuItemAriaLabel(this));
        builder.AddAttribute(sequence++, "popovertarget", owner.GetMenuItemPopoverTarget(this));
        builder.AddAttribute(sequence++, "popovertargetaction", "hide");

        if (!NTSplitButton.HasNativeOnClick(this)) {
            builder.AddAttribute(sequence++, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, args => owner.HandleMenuButtonItemClickAsync(this, args)));
        }

        builder.AddContent(sequence++, owner.RenderMenuItemContent(this));
        builder.CloseElement();
    };

    /// <inheritdoc />
    public Task SetParametersAsync(ParameterView parameters) {
        var previousParent = Parent;
        parameters.SetParameterProperties(this);

        if (Parent is null) {
            throw new InvalidOperationException($"{nameof(NTSplitButtonButtonItem)} must be used inside an {nameof(NTSplitButton)}.");
        }

        if (string.IsNullOrWhiteSpace(Label)) {
            throw new InvalidOperationException($"{nameof(NTSplitButtonButtonItem)} requires a non-empty {nameof(Label)}.");
        }

        if (!ReferenceEquals(previousParent, Parent)) {
            _registeredParent?.UnregisterMenuItem(this);
            Parent.RegisterMenuItem(this);
            _registeredParent = Parent;
        }

        return Task.CompletedTask;
    }
}
