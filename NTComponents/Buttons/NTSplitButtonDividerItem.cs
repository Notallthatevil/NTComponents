using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace NTComponents;

/// <summary>
///     Represents a visual divider that registers itself with an <see cref="NTSplitButton" /> menu.
/// </summary>
public sealed partial class NTSplitButtonDividerItem : Microsoft.AspNetCore.Components.IComponent, ISplitButtonItem, IDisposable {
    private NTSplitButton? _registeredParent;

    /// <inheritdoc />
    [Parameter(CaptureUnmatchedValues = true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public Dictionary<string, object?>? AdditionalAttributes { get; set; }

    IReadOnlyDictionary<string, object?>? ISplitButtonItem.AdditionalAttributes => AdditionalAttributes;

    /// <inheritdoc />
    public string? AriaLabel => null;

    /// <inheritdoc />
    public bool Disabled => true;

    /// <inheritdoc />
    public TnTIcon? Icon => null;

    /// <summary>
    ///     Gets or sets whether the divider should align with list item content instead of spanning the full menu width.
    /// </summary>
    [Parameter]
    public bool Inset { get; set; }

    /// <inheritdoc />
    public string Label => string.Empty;

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
        builder.OpenElement(sequence++, "div");
        builder.SetKey(this);
        builder.AddMultipleAttributes(sequence++, NTSplitButton.GetMenuItemAdditionalAttributes(this));
        builder.AddAttribute(sequence++, "class", owner.GetMenuDividerClass(this));
        builder.AddAttribute(sequence++, "role", "separator");
        builder.AddAttribute(sequence++, "aria-orientation", "horizontal");
        builder.CloseElement();
    };

    /// <inheritdoc />
    public Task SetParametersAsync(ParameterView parameters) {
        var previousParent = Parent;
        parameters.SetParameterProperties(this);

        if (Parent is null) {
            throw new InvalidOperationException($"{nameof(NTSplitButtonDividerItem)} must be used inside an {nameof(NTSplitButton)}.");
        }

        if (!ReferenceEquals(previousParent, Parent)) {
            _registeredParent?.UnregisterMenuItem(this);
            Parent.RegisterMenuItem(this);
            _registeredParent = Parent;
        }

        return Task.CompletedTask;
    }
}
