using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
///     Represents a navigation item that registers itself with an <see cref="NTMenu" />.
/// </summary>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.ProgressivelyEnhanced,
    CompatibilitySummary = "Participates in parent component rendering and inherits the parent interaction model.",
    CompatibilityDetails = "The parent menu can emit a usable anchor in static SSR. Menu closing, submenu coordination, and focus behavior are enhanced by the parent menu script.")]
public class NTMenuAnchorItem : Microsoft.AspNetCore.Components.IComponent, INTMenuItem, IDisposable {
    private NTMenu? _registeredParent;

    /// <inheritdoc />
    [Parameter(CaptureUnmatchedValues = true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public Dictionary<string, object?>? AdditionalAttributes { get; set; }

    IReadOnlyDictionary<string, object?>? INTMenuItem.AdditionalAttributes => AdditionalAttributes;

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
    public bool IsActionable => true;

    /// <inheritdoc />
    [Parameter]
    [EditorRequired]
    public string Label { get; set; } = string.Empty;

    /// <inheritdoc />
    [Parameter]
    public bool Selected { get; set; }

    /// <summary>
    ///     Optional anchor target.
    /// </summary>
    [Parameter]
    public string? Target { get; set; }

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
        builder.OpenElement(sequence++, "a");
        builder.SetKey(this);
        builder.AddMultipleAttributes(sequence++, NTMenu.GetMenuItemAdditionalAttributes(this));
        builder.AddAttribute(sequence++, "class", owner.GetMenuItemClass(this));
        builder.AddAttribute(sequence++, "href", owner.GetMenuAnchorItemHref(this));
        builder.AddAttribute(sequence++, "target", Target);
        builder.AddAttribute(sequence++, "role", "menuitem");
        builder.AddAttribute(sequence++, "aria-disabled", owner.GetMenuItemAriaDisabled(this));
        builder.AddAttribute(sequence++, "aria-label", owner.GetMenuItemAriaLabel(this));
        builder.AddAttribute(sequence++, "aria-selected", owner.GetMenuItemSelectedAttribute(this));
        builder.AddAttribute(sequence++, "tabindex", owner.GetMenuItemTabIndex(this));
        builder.AddContent(sequence++, owner.RenderMenuItemContent(this));
        builder.CloseElement();
    };

    /// <inheritdoc />
    public Task SetParametersAsync(ParameterView parameters) {
        var previousParent = Parent;
        var previousAriaLabel = AriaLabel;
        var previousDisabled = Disabled;
        var previousHref = Href;
        var previousIcon = Icon;
        var previousLabel = Label;
        var previousSelected = Selected;
        var previousTarget = Target;
        parameters.SetParameterProperties(this);

        if (Parent is null) {
            throw new InvalidOperationException($"{GetType().Name} must be used inside an {nameof(NTMenu)}.");
        }

        if (string.IsNullOrWhiteSpace(Label)) {
            throw new InvalidOperationException($"{GetType().Name} requires a non-empty {nameof(Label)}.");
        }

        if (string.IsNullOrWhiteSpace(Href)) {
            throw new InvalidOperationException($"{GetType().Name} requires a non-empty {nameof(Href)}.");
        }

        if (!ReferenceEquals(previousParent, Parent)) {
            _registeredParent?.UnregisterMenuItem(this);
            Parent.RegisterMenuItem(this);
            _registeredParent = Parent;
        }
        else if (_registeredParent is not null && RenderedStateChanged(previousAriaLabel, previousDisabled, previousHref, previousIcon, previousLabel, previousSelected, previousTarget)) {
            Parent.NotifyMenuItemChanged(this);
        }

        return Task.CompletedTask;
    }

    private bool RenderedStateChanged(string? previousAriaLabel, bool previousDisabled, string previousHref, TnTIcon? previousIcon, string previousLabel, bool previousSelected, string? previousTarget) =>
        previousAriaLabel != AriaLabel || previousDisabled != Disabled || previousHref != Href || !NTMenu.IconStateEquals(previousIcon, Icon) || previousLabel != Label || previousSelected != Selected || previousTarget != Target;
}
