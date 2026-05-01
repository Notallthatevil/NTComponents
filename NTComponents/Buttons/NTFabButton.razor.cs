using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
///     Material 3 floating action button component with icon-only and extended label modes.
/// </summary>
/// <remarks>
///     Use <see cref="NTFabButton" /> for the single most important constructive action on a screen. The icon is always required. When <see cref="Label" /> is supplied the button renders as an
///     extended FAB; otherwise it renders as an icon-only FAB and requires <see cref="AriaLabel" />.
/// </remarks>
public partial class NTFabButton : TnTComponentBase {

    /// <summary>
    ///     Gets or sets the accessible name announced for icon-only FABs or a more descriptive name for extended FABs.
    /// </summary>
    [Parameter]
    public string? AriaLabel { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the FAB container color.
    /// </summary>
    [Parameter]
    public TnTColor? BackgroundColor { get; set; }

    /// <summary>
    ///     Gets or sets the size of the FAB.
    /// </summary>
    /// <remarks>Supports <see cref="Size.Small" />, <see cref="Size.Medium" />, and <see cref="Size.Large" />. Unsupported enum values map to the nearest supported Material FAB size.</remarks>
    [Parameter]
    public Size ButtonSize { get; set; } = Size.Medium;

    /// <summary>
    ///     Gets or sets whether the FAB is disabled.
    /// </summary>
    /// <remarks>Material guidance recommends not rendering unavailable FAB actions instead of disabling them.</remarks>
    [Parameter]
    public bool Disabled { get; set; }

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("nt-fab-button")
        .AddClass("nt-fab-button-extended", HasLabel)
        .AddClass("nt-fab-button-icon-only", !HasLabel)
        .AddClass(GetPlacementClass())
        .AddElevation(Elevation)
        .AddSize(EffectiveButtonSize)
        .AddDisabled(Disabled)
        .Build();

    /// <summary>
    ///     Gets or sets the optional native name attribute.
    /// </summary>
    [Parameter]
    public string? ElementName { get; set; }

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddVariable("nt-fab-bg", BackgroundColor.ToCssTnTColorVariable(), BackgroundColor.HasValue)
        .AddVariable("nt-fab-fg", TextColor.ToCssTnTColorVariable(), TextColor.HasValue)
        .Build();

    /// <summary>
    ///     Gets or sets the resting elevation of the FAB.
    /// </summary>
    [Parameter]
    public NTElevation Elevation { get; set; } = NTElevation.Medium;

    /// <summary>
    ///     Gets or sets whether a ripple effect should be rendered.
    /// </summary>
    [Parameter]
    public bool EnableRipple { get; set; } = true;

    /// <summary>
    ///     Gets or sets the action icon rendered inside the FAB.
    /// </summary>
    [Parameter, EditorRequired]
    public TnTIcon Icon { get; set; } = default!;

    /// <summary>
    ///     Gets or sets the optional visible label. When supplied, the FAB renders as an extended FAB.
    /// </summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>
    ///     Gets or sets the click callback.
    /// </summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnClickCallback { get; set; }

    /// <summary>
    ///     Gets or sets where the FAB is positioned.
    /// </summary>
    /// <remarks>The default <see cref="NTFabButtonPlacement.Inline" /> keeps the FAB in normal document flow. The corner placements use fixed viewport positioning.</remarks>
    [Parameter]
    public NTFabButtonPlacement Placement { get; set; } = NTFabButtonPlacement.Inline;

    /// <summary>
    ///     Gets or sets whether click events should stop propagating.
    /// </summary>
    [Parameter]
    public bool StopPropagation { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the FAB icon and label color.
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

    internal string? EffectiveAriaLabel => string.IsNullOrWhiteSpace(AriaLabel) ? null : AriaLabel;

    internal Size EffectiveButtonSize => ButtonSize switch {
        Size.Smallest => Size.Small,
        Size.Small => Size.Small,
        Size.Medium => Size.Medium,
        Size.Large => Size.Large,
        Size.Largest => Size.Large,
        _ => Size.Medium
    };

    internal bool HasLabel => !string.IsNullOrWhiteSpace(Label);

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();

        if (Icon is null) {
            throw new ArgumentNullException(nameof(Icon), "NTFabButton requires a non-null Icon parameter.");
        }

        if (!HasLabel && string.IsNullOrWhiteSpace(AriaLabel)) {
            throw new ArgumentException("Icon-only NTFabButton requires a non-empty AriaLabel.", nameof(AriaLabel));
        }

        if (Label?.Contains('\n', StringComparison.Ordinal) == true || Label?.Contains('\r', StringComparison.Ordinal) == true) {
            throw new ArgumentException("NTFabButton Label must not contain line breaks.", nameof(Label));
        }

        if (BackgroundColor is TnTColor.None or TnTColor.Transparent) {
            throw new InvalidOperationException($"{nameof(BackgroundColor)} must be a visible container color.");
        }

        if (TextColor is TnTColor.None or TnTColor.Transparent) {
            throw new InvalidOperationException($"{nameof(TextColor)} must be a visible content color.");
        }

        WarnForUnsupportedSize();
    }

    private string GetPlacementClass() => Placement switch {
        NTFabButtonPlacement.Inline => "nt-fab-button-placement-inline",
        NTFabButtonPlacement.LowerRight => "nt-fab-button-placement-lower-right",
        NTFabButtonPlacement.LowerLeft => "nt-fab-button-placement-lower-left",
        NTFabButtonPlacement.UpperRight => "nt-fab-button-placement-upper-right",
        NTFabButtonPlacement.UpperLeft => "nt-fab-button-placement-upper-left",
        _ => throw new ArgumentOutOfRangeException(nameof(Placement), Placement, null)
    };

    private async Task HandleClickAsync(MouseEventArgs args) {
        if (Disabled) {
            return;
        }

        await OnClickCallback.InvokeAsync(args);
    }

    private void WarnForUnsupportedSize() {
        if (ButtonSize is Size.Smallest) {
            Debug.WriteLine($"{nameof(NTFabButton)} does not support {nameof(Size.Smallest)}. Rendering with {nameof(Size.Small)}.");
            return;
        }

        if (ButtonSize is Size.Largest) {
            Debug.WriteLine($"{nameof(NTFabButton)} does not support {nameof(Size.Largest)}. Rendering with {nameof(Size.Large)}.");
        }
    }
}