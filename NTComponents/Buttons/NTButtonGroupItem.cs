using System;
using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
///     Represents a button description that registers itself with <see cref="NTButtonGroup{TObjectType}" />.
/// </summary>
public sealed partial class NTButtonGroupItem<TObjectType> : Microsoft.AspNetCore.Components.IComponent, IDisposable {
    private NTButtonGroup<TObjectType>? _registeredParent;

    /// <summary>
    ///     Captures unmatched attributes passed to the item.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public Dictionary<string, object?>? AdditionalAttributes { get; set; }

    /// <summary>
    ///     Provides an accessible label for icon-only items.
    /// </summary>
    [Parameter]
    public string? AriaLabel { get; set; }

    /// <summary>
    ///     The parent button group that owns this item.
    /// </summary>
    [CascadingParameter]
    public NTButtonGroup<TObjectType>? Parent { get; set; }

    /// <summary>
    ///     A unique key used to identify the item inside the group.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public TObjectType Key { get; set; } = default!;

    /// <summary>
    ///     Optional label text; when unset, the button may render as an icon-only control.
    /// </summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>
    ///     Optional icon rendered before the label.
    /// </summary>
    [Obsolete("Start icons are not supported by Material 3 button groups use Icon instead. Note: This just wraps Icon.")]
    [Parameter]
    public TnTIcon? StartIcon { get => Icon; set => Icon = value; }

    /// <summary>
    ///     Optional icon rendered before the label.
    /// </summary>
    [Parameter]
    public TnTIcon? Icon { get; set; }

    /// <summary>
    ///     Marks the item as disabled.
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    ///     Deprecated. End icons are not supported by Material 3 button groups and are no longer rendered.
    /// </summary>
    [Obsolete("End icons are not supported by Material 3 button groups and are no longer rendered.")]
    [Parameter]
    public TnTIcon? EndIcon { get; set; }

    /// <summary>
    ///     Indicates whether this item should be selected by default when no other selection exists.
    /// </summary>
    [Parameter]
    public bool IsDefaultSelected { get; set; }

    /// <inheritdoc />
    public void Attach(RenderHandle renderHandle) { }

    /// <inheritdoc />
    public void Dispose() {
        _registeredParent?.UnregisterItem(this);
        _registeredParent = null;
    }

    /// <inheritdoc />
    public Task SetParametersAsync(ParameterView parameters) {
        var previousParent = Parent;
        parameters.SetParameterProperties(this);

        if (Parent is null) {
            throw new InvalidOperationException($"{nameof(NTButtonGroupItem<TObjectType>)} must be used inside an {nameof(NTButtonGroup<TObjectType>)}.");
        }

        if (Icon is not null && string.IsNullOrWhiteSpace(Label) && string.IsNullOrWhiteSpace(AriaLabel)) {
            throw new InvalidOperationException($"{nameof(NTButtonGroupItem<TObjectType>)} requires {nameof(AriaLabel)} when rendering an icon-only item.");
        }

        if (!ReferenceEquals(previousParent, Parent)) {
            _registeredParent?.UnregisterItem(this);
            Parent.RegisterItem(this);
            _registeredParent = Parent;
        }

        return Task.CompletedTask;
    }
}
