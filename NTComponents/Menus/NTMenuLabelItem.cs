using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace NTComponents;

/// <summary>
///     Represents non-interactive label text that identifies a group or section inside an <see cref="NTMenu" />.
/// </summary>
public class NTMenuLabelItem : Microsoft.AspNetCore.Components.IComponent, INTMenuItem, IDisposable {
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

    /// <inheritdoc />
    public bool IsActionable => false;

    /// <inheritdoc />
    [Parameter]
    [EditorRequired]
    public string Label { get; set; } = string.Empty;

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
        builder.AddAttribute(sequence++, "class", owner.GetMenuLabelClass(this));
        builder.AddAttribute(sequence++, "role", "presentation");
        builder.AddContent(sequence++, Label);
        builder.CloseElement();
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
