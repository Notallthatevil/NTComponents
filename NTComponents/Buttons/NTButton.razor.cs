using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
///     Material 3 button component with explicit variant, size, shape, and toggle semantics.
/// </summary>
/// <remarks>
///     Use the lowest-emphasis variant that still communicates the action clearly: <see cref="NTButtonVariant.Filled" /> for the primary action on a screen, <see cref="NTButtonVariant.Tonal" /> for
///     important secondary actions, <see cref="NTButtonVariant.Outlined" /> for medium-emphasis actions, and <see cref="NTButtonVariant.Text" /> for low-emphasis actions in compact or text-heavy
///     contexts. Reserve <see cref="NTButtonVariant.Elevated" /> for actions that need separation from a surface; do not add elevation to other variants. Text and outlined buttons should keep a
///     transparent container, while filled, tonal, and elevated buttons need a visible container color. Text color must remain visible against the chosen container. Toggle behavior is supported for
///     contained and outlined buttons, but not for text buttons. Keep every button's activation target at least 48 by 48 CSS pixels, even when the visible container is smaller.
/// </remarks>
public partial class NTButton : TnTComponentBase {

    /// <summary>
    ///     Gets or sets an optional override for the button container color.
    /// </summary>
    [Parameter]
    public TnTColor? BackgroundColor { get; set; }

    /// <summary>
    ///     Gets or sets the size of the button.
    /// </summary>
    [Parameter]
    public Size ButtonSize { get; set; } = Size.Small;

    /// <summary>
    ///     Gets or sets whether the button is disabled.
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; }

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

    /// <summary>
    ///     Gets or sets the optional name attribute.
    /// </summary>
    [Parameter]
    public string? ElementName { get; set; }

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddVariable("nt-button-bg", BackgroundColor.ToCssTnTColorVariable(), BackgroundColor.HasValue)
        .AddVariable("nt-button-fg", TextColor.ToCssTnTColorVariable(), TextColor.HasValue)
        .Build();

    /// <summary>
    ///     Gets or sets an optional override for the button elevation.
    /// </summary>
    [Parameter]
    public NTElevation? Elevation { get; set; }

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
    ///     Gets or sets the click callback.
    /// </summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnClickCallback { get; set; }

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
    ///     Gets or sets the content displayed as a tooltip.
    /// </summary>
    [Parameter]
    public RenderFragment? Tooltip { get; set; }

    /// <summary>
    ///     Gets or sets the button type.
    /// </summary>
    [Parameter]
    public ButtonType Type { get; set; }

    /// <summary>
    ///     Gets or sets the visual variant of the button.
    /// </summary>
    [Parameter]
    public NTButtonVariant Variant { get; set; } = NTButtonVariant.Filled;

    internal string? AriaPressed => IsToggleButton ? Selected.ToString().ToLowerInvariant() : null;

    private ButtonShape EffectiveShape => IsToggleButton
        ? Selected ? ButtonShape.Square : ButtonShape.Round
        : Shape;

    private bool _backgroundColorWasProvided;
    private bool _elevationWasProvided;
    private bool _textColorWasProvided;

    /// <inheritdoc />
    public override Task SetParametersAsync(ParameterView parameters) {
        _backgroundColorWasProvided = false;
        _elevationWasProvided = false;
        _textColorWasProvided = false;

        foreach (var parameter in parameters) {
            switch (parameter.Name) {
                case nameof(BackgroundColor):
                    _backgroundColorWasProvided = true;
                    break;

                case nameof(Elevation):
                    _elevationWasProvided = true;
                    break;

                case nameof(TextColor):
                    _textColorWasProvided = true;
                    break;
            }
        }

        return base.SetParametersAsync(parameters);
    }

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();
        if (string.IsNullOrWhiteSpace(Label)) {
            throw new ArgumentException("NTButton requires a non-empty Label.", nameof(Label));
        }

        if (IsToggleButton && Variant == NTButtonVariant.Text) {
            throw new InvalidOperationException("Text buttons do not support toggle behavior.");
        }

        if (!_backgroundColorWasProvided || !BackgroundColor.HasValue) {
            BackgroundColor = GetDefaultBackgroundColor();
        }

        if (!_elevationWasProvided || !Elevation.HasValue) {
            Elevation = GetDefaultElevation();
        }

        if (!_textColorWasProvided || !TextColor.HasValue) {
            TextColor = GetDefaultTextColor();
        }

        ValidateVariantColorCombination();
        ValidateVariantElevationCombination();
    }

    private TnTColor GetDefaultBackgroundColor() {
        if (IsToggleButton) {
            return GetDefaultToggleBackgroundColor();
        }

        return Variant switch {
            NTButtonVariant.Elevated => TnTColor.SurfaceContainerLow,
            NTButtonVariant.Filled => TnTColor.Primary,
            NTButtonVariant.Tonal => TnTColor.SecondaryContainer,
            NTButtonVariant.Outlined => TnTColor.Transparent,
            NTButtonVariant.Text => TnTColor.Transparent,
            _ => throw new ArgumentOutOfRangeException(nameof(Variant), Variant, null)
        };
    }

    private TnTColor GetDefaultToggleBackgroundColor() {
        return Variant switch {
            NTButtonVariant.Elevated => Selected ? TnTColor.Primary : TnTColor.SurfaceContainerLow,
            NTButtonVariant.Filled => Selected ? TnTColor.Primary : TnTColor.SurfaceContainer,
            NTButtonVariant.Tonal => Selected ? TnTColor.Secondary : TnTColor.SecondaryContainer,
            NTButtonVariant.Outlined => Selected ? TnTColor.InverseSurface : TnTColor.Transparent,
            NTButtonVariant.Text => TnTColor.Transparent,
            _ => throw new ArgumentOutOfRangeException(nameof(Variant), Variant, null)
        };
    }

    private NTElevation GetDefaultElevation() {
        return Variant == NTButtonVariant.Elevated ? NTElevation.Lowest : NTElevation.None;
    }

    private TnTColor GetDefaultTextColor() {
        if (IsToggleButton) {
            return GetDefaultToggleTextColor();
        }

        return Variant switch {
            NTButtonVariant.Elevated => TnTColor.Primary,
            NTButtonVariant.Filled => TnTColor.OnPrimary,
            NTButtonVariant.Tonal => TnTColor.OnSecondaryContainer,
            NTButtonVariant.Outlined => TnTColor.Primary,
            NTButtonVariant.Text => TnTColor.Primary,
            _ => throw new ArgumentOutOfRangeException(nameof(Variant), Variant, null)
        };
    }

    private TnTColor GetDefaultToggleTextColor() {
        return Variant switch {
            NTButtonVariant.Elevated => Selected ? TnTColor.OnPrimary : TnTColor.Primary,
            NTButtonVariant.Filled => Selected ? TnTColor.OnPrimary : TnTColor.OnSurfaceVariant,
            NTButtonVariant.Tonal => Selected ? TnTColor.OnSecondary : TnTColor.OnSecondaryContainer,
            NTButtonVariant.Outlined => Selected ? TnTColor.InverseOnSurface : TnTColor.OnSurfaceVariant,
            NTButtonVariant.Text => TnTColor.Primary,
            _ => throw new ArgumentOutOfRangeException(nameof(Variant), Variant, null)
        };
    }

    private void ValidateBackgroundColorForVariant() {
        if (Variant is NTButtonVariant.Text or NTButtonVariant.Outlined) {
            if (Variant == NTButtonVariant.Outlined && IsToggleButton && Selected) {
                if (BackgroundColor is TnTColor.None or TnTColor.Transparent) {
                    throw new InvalidOperationException($"{Variant} selected toggle buttons must use a visible container {nameof(BackgroundColor)}.");
                }

                return;
            }

            if (BackgroundColor != TnTColor.Transparent) {
                throw new InvalidOperationException($"{Variant} buttons must use a transparent {nameof(BackgroundColor)}.");
            }

            return;
        }

        if (BackgroundColor is TnTColor.None or TnTColor.Transparent) {
            throw new InvalidOperationException($"{Variant} buttons must use a visible container {nameof(BackgroundColor)}.");
        }
    }

    private void ValidateVariantColorCombination() {
        if (BackgroundColor.HasValue) {
            ValidateBackgroundColorForVariant();
        }

        if (TextColor is TnTColor.None or TnTColor.Transparent) {
            throw new InvalidOperationException($"{nameof(TextColor)} must be a visible content color.");
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
}
