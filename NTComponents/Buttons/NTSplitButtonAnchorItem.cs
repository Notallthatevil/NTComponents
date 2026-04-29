using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace NTComponents;

/// <summary>
///     Represents a navigation item that registers itself with an <see cref="NTSplitButton" /> menu.
/// </summary>
public sealed partial class NTSplitButtonAnchorItem : Microsoft.AspNetCore.Components.IComponent, ISplitButtonItem, IDisposable {
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
        builder.OpenElement(sequence++, "a");
        builder.AddMultipleAttributes(sequence++, NTSplitButton.GetMenuItemAdditionalAttributes(this));
        builder.AddAttribute(sequence++, "class", owner.GetMenuItemClass(this));
        builder.AddAttribute(sequence++, "href", owner.GetMenuAnchorItemHref(this));
        builder.AddAttribute(sequence++, "target", Target);
        builder.AddAttribute(sequence++, "role", "menuitem");
        builder.AddAttribute(sequence++, "aria-disabled", owner.GetMenuItemAriaDisabled(this));
        builder.AddAttribute(sequence++, "aria-label", owner.GetMenuItemAriaLabel(this));
        builder.AddAttribute(sequence++, "tabindex", owner.GetMenuItemTabIndex(this));
        builder.AddContent(sequence++, owner.RenderMenuItemContent(this));
        builder.CloseElement();
    };

    /// <inheritdoc />
    public Task SetParametersAsync(ParameterView parameters) {
        var previousParent = Parent;
        parameters.SetParameterProperties(this);

        if (Parent is null) {
            throw new InvalidOperationException($"{nameof(NTSplitButtonAnchorItem)} must be used inside an {nameof(NTSplitButton)}.");
        }

        if (string.IsNullOrWhiteSpace(Label)) {
            throw new InvalidOperationException($"{nameof(NTSplitButtonAnchorItem)} requires a non-empty {nameof(Label)}.");
        }

        if (string.IsNullOrWhiteSpace(Href)) {
            throw new InvalidOperationException($"{nameof(NTSplitButtonAnchorItem)} requires a non-empty {nameof(Href)}.");
        }

        if (!ReferenceEquals(previousParent, Parent)) {
            _registeredParent?.UnregisterMenuItem(this);
            Parent.RegisterMenuItem(this);
            _registeredParent = Parent;
        }

        return Task.CompletedTask;
    }
}
