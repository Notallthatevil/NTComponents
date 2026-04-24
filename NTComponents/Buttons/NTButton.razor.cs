using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
///     Material 3 button component with explicit variant, size, shape, and toggle semantics.
/// </summary>
public partial class NTButton : TnTComponentBase {

    /// <summary>
    ///     Gets or sets the visual variant of the button.
    /// </summary>
    [Parameter]
    public NTButtonVariant Variant { get; set; } = NTButtonVariant.Filled;

    /// <summary>
    ///     Gets or sets the size of the button.
    /// </summary>
    [Parameter]
    public Size ButtonSize { get; set; } = Size.Small;

    /// <summary>
    ///     Gets or sets an optional override for the button container color.
    /// </summary>
    [Parameter]
    public TnTColor? BackgroundColor { get; set; }

    /// <summary>
    ///     Gets or sets the visible text label rendered by the button.
    /// </summary>
    [Parameter, EditorRequired]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets an optional leading icon rendered before the label.
    /// </summary>
    [Parameter]
    public TnTIcon? LeadingIcon { get; set; }

    /// <summary>
    ///     Gets or sets whether the button is disabled.
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    ///     Gets or sets whether a ripple effect should be rendered.
    /// </summary>
    [Parameter]
    public bool EnableRipple { get; set; } = true;

    /// <summary>
    ///     Gets or sets whether this button behaves as a toggle button.
    /// </summary>
    [Parameter]
    public bool IsToggleButton { get; set; }

    /// <summary>
    ///     Gets or sets whether the toggle button is currently selected.
    /// </summary>
    [Parameter]
    public bool Selected { get; set; }

    /// <summary>
    ///     Gets or sets the base resting shape for the button.
    /// </summary>
    [Parameter]
    public ButtonShape Shape { get; set; } = ButtonShape.Round;

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
    ///     Gets or sets the click callback.
    /// </summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnClickCallback { get; set; }

    /// <summary>
    ///     Gets or sets the optional name attribute.
    /// </summary>
    [Parameter]
    public string? ElementName { get; set; }

    /// <summary>
    ///     Gets or sets the button type.
    /// </summary>
    [Parameter]
    public ButtonType Type { get; set; }

    /// <summary>
    ///     Gets or sets the content displayed as a tooltip.
    /// </summary>
    [Parameter]
    public RenderFragment? Tooltip { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the button elevation.
    /// </summary>
    [Parameter]
    public NTElevation? Elevation { get; set; }

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("nt-button")
        .AddClass("nt-button-elevated", Variant == NTButtonVariant.Elevated)
        .AddClass("nt-button-filled", Variant == NTButtonVariant.Filled)
        .AddClass("nt-button-tonal", Variant == NTButtonVariant.Tonal)
        .AddClass("nt-button-outlined", Variant == NTButtonVariant.Outlined)
        .AddClass("nt-button-text", Variant == NTButtonVariant.Text)
        .AddClass("nt-button-toggle", IsToggleButton)
        .AddClass("nt-button-selected", Selected)
        .AddClass("nt-button-shape-round", EffectiveShape == ButtonShape.Round)
        .AddClass("nt-button-shape-square", EffectiveShape == ButtonShape.Square)
        .AddElevation(Elevation)
        .AddSize(ButtonSize)
        .AddDisabled(Disabled)
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddVariable("nt-button-bg", BackgroundColor.ToCssTnTColorVariable(), BackgroundColor.HasValue)
        .AddVariable("nt-button-fg", TextColor.ToCssTnTColorVariable(), TextColor.HasValue)
        .Build();

    internal string? AriaPressed => IsToggleButton ? Selected.ToString().ToLowerInvariant() : null;

    private ButtonShape EffectiveShape => IsToggleButton && Selected
        ? Shape == ButtonShape.Round ? ButtonShape.Square : ButtonShape.Round
        : Shape;

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();
        if (string.IsNullOrWhiteSpace(Label)) {
            throw new ArgumentException("NTButton requires a non-empty Label.", nameof(Label));
        }

        if (IsToggleButton && Variant == NTButtonVariant.Text) {
            throw new InvalidOperationException("Text buttons do not support toggle behavior.");
        }

        ValidateVariantColorCombination();
        ValidateVariantElevationCombination();

        BackgroundColor ??= GetDefaultBackgroundColor();
        Elevation ??= GetDefaultElevation();
        TextColor ??= GetDefaultTextColor();
    }

    private TnTColor GetDefaultBackgroundColor() {
        return Variant switch {
            NTButtonVariant.Elevated => TnTColor.SurfaceContainerLow,
            NTButtonVariant.Filled => TnTColor.Primary,
            NTButtonVariant.Tonal => TnTColor.SecondaryContainer,
            NTButtonVariant.Outlined => TnTColor.Transparent,
            NTButtonVariant.Text => TnTColor.Transparent,
            _ => throw new ArgumentOutOfRangeException(nameof(Variant), Variant, null)
        };
    }

    private void ValidateVariantColorCombination() {
        if (BackgroundColor.HasValue) {
            ValidateBackgroundColorForVariant();
        }

        if (TextColor is TnTColor.None or TnTColor.Transparent) {
            throw new InvalidOperationException($"{nameof(TextColor)} must be a visible content color.");
        }
    }

    private void ValidateBackgroundColorForVariant() {
        if (Variant is NTButtonVariant.Text or NTButtonVariant.Outlined) {
            if (BackgroundColor != TnTColor.Transparent) {
                throw new InvalidOperationException($"{Variant} buttons must use a transparent {nameof(BackgroundColor)}.");
            }

            return;
        }

        if (BackgroundColor is TnTColor.None or TnTColor.Transparent) {
            throw new InvalidOperationException($"{Variant} buttons must use a visible container {nameof(BackgroundColor)}.");
        }
    }

    private void ValidateVariantElevationCombination() {
        if (!Elevation.HasValue) {
            return;
        }

        if (Variant == NTButtonVariant.Elevated) {
            if (Elevation == NTElevation.None) {
                throw new InvalidOperationException($"{nameof(NTButtonVariant.Elevated)} buttons must use a non-zero {nameof(Elevation)}.");
            }

            return;
        }

        if (Elevation != NTElevation.None) {
            throw new InvalidOperationException($"{Variant} buttons must use {nameof(NTElevation.None)} {nameof(Elevation)}.");
        }
    }

    private NTElevation GetDefaultElevation() {
        return Variant == NTButtonVariant.Elevated ? NTElevation.Lowest : NTElevation.None;
    }

    private TnTColor GetDefaultTextColor() {
        return Variant switch {
            NTButtonVariant.Elevated => TnTColor.Primary,
            NTButtonVariant.Filled => TnTColor.OnPrimary,
            NTButtonVariant.Tonal => TnTColor.OnSecondaryContainer,
            NTButtonVariant.Outlined => TnTColor.Primary,
            NTButtonVariant.Text => TnTColor.Primary,
            _ => throw new ArgumentOutOfRangeException(nameof(Variant), Variant, null)
        };
    }
}
