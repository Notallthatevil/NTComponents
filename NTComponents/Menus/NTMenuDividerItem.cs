using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace NTComponents;

/// <summary>
///     Represents a visual divider that registers itself with an <see cref="NTMenu" />.
/// </summary>
public class NTMenuDividerItem : Microsoft.AspNetCore.Components.IComponent, INTMenuItem, IDisposable {
    private NTMenu? _registeredParent;

    /// <inheritdoc />
    [Parameter(CaptureUnmatchedValues = true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public Dictionary<string, object?>? AdditionalAttributes { get; set; }

    IReadOnlyDictionary<string, object?>? INTMenuItem.AdditionalAttributes => AdditionalAttributes;

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
    public bool IsActionable => false;

    /// <inheritdoc />
    public string Label => string.Empty;

    /// <inheritdoc />
    public bool Selected => false;

    /// <summary>
    ///     The parent menu that owns this item.
    /// </summary>
    [CascadingParameter]
    public NTMenu? Parent { get; set; }

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
        builder.OpenElement(sequence++, "div");
        builder.SetKey(this);
        builder.AddMultipleAttributes(sequence++, NTMenu.GetMenuItemAdditionalAttributes(this));
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
            throw new InvalidOperationException($"{GetType().Name} must be used inside an {nameof(NTMenu)}.");
        }

        if (!ReferenceEquals(previousParent, Parent)) {
            _registeredParent?.UnregisterMenuItem(this);
            Parent.RegisterMenuItem(this);
            _registeredParent = Parent;
        }

        return Task.CompletedTask;
    }
}
