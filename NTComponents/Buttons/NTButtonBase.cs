using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
///     Base class for concrete NTComponents button controls that render a native button.
/// </summary>
public abstract class NTButtonBase : NTComponentBase {
    private readonly HashSet<string> _providedParameterNames = [];

    /// <summary>
    ///     Gets or sets an optional override for the button container color.
    /// </summary>
    [Parameter]
    public TnTColor? BackgroundColor { get; set; }

    /// <summary>
    ///     Gets or sets the size of the button.
    /// </summary>
    [Parameter]
    public virtual Size ButtonSize { get; set; } = Size.Small;

    /// <summary>
    ///     Gets or sets whether the button is disabled.
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    ///     Gets or sets the optional native name attribute.
    /// </summary>
    [Parameter]
    public string? ElementName { get; set; }

    /// <summary>
    ///     Gets or sets whether a ripple effect should be rendered.
    /// </summary>
    [Parameter]
    public bool EnableRipple { get; set; } = true;

    /// <summary>
    ///     Gets or sets the click callback.
    /// </summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnClickCallback { get; set; }

    /// <summary>
    ///     Gets or sets whether click events should stop propagating.
    /// </summary>
    [Parameter]
    public bool StopPropagation { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the button content color.
    /// </summary>
    [Parameter]
    public TnTColor? TextColor { get; set; }

    /// <summary>
    ///     Gets or sets the content displayed as a tooltip.
    /// </summary>
    [Parameter]
    public RenderFragment? Tooltip { get; set; }

    /// <summary>
    ///     Gets or sets the native button type.
    /// </summary>
    [Parameter]
    public ButtonType Type { get; set; }

    /// <summary>
    ///     Gets whether this button currently exposes toggle behavior.
    /// </summary>
    protected virtual bool IsToggleEnabled => false;

    /// <summary>
    ///     Gets or sets whether this button is currently selected when toggle behavior is enabled.
    /// </summary>
    protected virtual bool ToggleSelected { get => false; set { } }

    /// <summary>
    ///     Gets the callback invoked when toggle selection changes.
    /// </summary>
    protected virtual EventCallback<bool> ToggleSelectedChanged => default;

    /// <summary>
    ///     Gets the rendered aria-pressed value for toggle buttons.
    /// </summary>
    protected string? ToggleAriaPressed => IsToggleEnabled ? ToggleSelected.ToString().ToLowerInvariant() : null;

    /// <inheritdoc />
    public override Task SetParametersAsync(ParameterView parameters) {
        _providedParameterNames.Clear();

        foreach (var parameter in parameters) {
            _providedParameterNames.Add(parameter.Name);
        }

        return base.SetParametersAsync(parameters);
    }

    /// <summary>
    ///     Returns whether the current parameter set included the supplied parameter name.
    /// </summary>
    protected bool WasParameterProvided(string parameterName) => _providedParameterNames.Contains(parameterName);

    /// <summary>
    ///     Gets the resting shape after applying shared toggle shape behavior.
    /// </summary>
    protected ButtonShape GetEffectiveToggleShape(ButtonShape shape) => IsToggleEnabled
        ? ToggleSelected ? ButtonShape.Square : ButtonShape.Round
        : shape;

    /// <summary>
    ///     Handles the native button click event.
    /// </summary>
    protected async Task HandleClickAsync(MouseEventArgs args) {
        if (Disabled) {
            return;
        }

        await OnButtonClickAsync();
        await OnClickCallback.InvokeAsync(args);
    }

    /// <summary>
    ///     Runs shared button behavior before the public click callback.
    /// </summary>
    protected virtual async Task OnButtonClickAsync() {
        if (!IsToggleEnabled) {
            return;
        }

        ToggleSelected = !ToggleSelected;
        await ToggleSelectedChanged.InvokeAsync(ToggleSelected);
    }
}
