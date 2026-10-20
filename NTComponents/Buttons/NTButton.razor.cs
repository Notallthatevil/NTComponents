using Microsoft.AspNetCore.Components;
using NTComponents.CodeDocumentation;
using NTComponents.Core;
using NTComponents.Interfaces;

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
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.ProgressivelyEnhanced,
    CompatibilitySummary = "Renders a native button in static SSR and adds Blazor callbacks when interactive.",
    CompatibilityDetails = "Use AdditionalAttributes for native HTML attributes in static SSR. Blazor EventCallback parameters require an interactive render mode.")]
public partial class NTButton : NTButtonBase, INTBadgeable {
    /// <inheritdoc />
    [Parameter]
    public string? BadgeAriaLabel { get; set; }

    /// <inheritdoc />
    [Parameter]
    public string? BadgeContent { get; set; }

    /// <inheritdoc />
    /// <remarks>The badge straddles the container's top-end corner, centered on its rounded edge for every shape.</remarks>
    [Parameter]
    public bool ShowBadge { get; set; }


    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("nt-button")
        .AddClass(Variant switch {
            NTButtonVariant.Elevated => "nt-button-elevated",
            NTButtonVariant.Filled => "nt-button-filled",
            NTButtonVariant.Tonal => "nt-button-tonal",
            NTButtonVariant.Outlined => "nt-button-outlined",
            NTButtonVariant.Text => "nt-button-text",
            _ => null
        })
        .AddClass("nt-button-toggle", IsToggleButton)
        .AddClass("nt-button-selected", Selected)
        .AddClass("nt-button-progress-active", ShowProgress)
        .AddClass(EffectiveShape switch {
            ButtonShape.Round => "nt-button-shape-round",
            ButtonShape.Square => "nt-button-shape-square",
            _ => null
        })
        .AddElevation(Elevation)
        .AddSize(ButtonSize)
        .AddDisabled(Disabled)
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddVariable("nt-button-bg", BackgroundColor.ToCssNTColorVariable(), BackgroundColor.HasValue)
        .AddVariable("nt-button-fg", TextColor.ToCssNTColorVariable(), TextColor.HasValue)
        .Build();

    /// <summary>
    ///     Gets or sets an optional override for the button elevation.
    /// </summary>
    [Parameter]
    public NTElevation? Elevation { get; set; }

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
    public NTIcon? LeadingIcon { get; set; }

    /// <summary>
    ///     Gets or sets whether the toggle button is currently selected.
    /// </summary>
    [Parameter]
    public bool Selected { get; set; }

    /// <summary>
    ///     Gets or sets the callback invoked when the toggle selected state changes.
    /// </summary>
    [Parameter]
    public EventCallback<bool> SelectedChanged { get; set; }

    /// <summary>
    ///     Gets or sets the base resting shape for the button.
    /// </summary>
    [Parameter]
    public ButtonShape Shape { get; set; } = ButtonShape.Round;

    /// <summary>
    ///     Gets or sets the visual variant of the button.
    /// </summary>
    [Parameter]
    public NTButtonVariant Variant { get; set; } = NTButtonVariant.Filled;

    internal string? AriaPressed => ToggleAriaPressed;

    private string? AriaDescribedBy => DescribedBy(ShowBadge && AdditionalAttributes?.ContainsKey("aria-label") == true);

    private ButtonShape EffectiveShape => GetEffectiveToggleShape(Shape);

    /// <inheritdoc />
    protected override bool IsToggleEnabled => IsToggleButton;

    /// <inheritdoc />
    protected override bool ToggleSelected { get => Selected; set => Selected = value; }

    /// <inheritdoc />
    protected override EventCallback<bool> ToggleSelectedChanged => SelectedChanged;

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();
        if (string.IsNullOrWhiteSpace(Label)) {
            throw new ArgumentException("NTButton requires a non-empty Label.", nameof(Label));
        }

        if (IsToggleButton && Variant == NTButtonVariant.Text) {
            throw new InvalidOperationException("Text buttons do not support toggle behavior.");
        }

        if (!WasParameterProvided(nameof(BackgroundColor)) || !BackgroundColor.HasValue) {
            BackgroundColor = GetDefaultBackgroundColor();
        }

        if (!WasParameterProvided(nameof(Elevation)) || !Elevation.HasValue) {
            Elevation = GetDefaultElevation();
        }

        if (!WasParameterProvided(nameof(TextColor)) || !TextColor.HasValue) {
            TextColor = GetDefaultTextColor();
        }

        ValidateVariantColorCombination();
        ValidateVariantElevationCombination();
    }

    private NTColor GetDefaultBackgroundColor() {
        if (IsToggleButton) {
            return GetDefaultToggleBackgroundColor();
        }

        return Variant switch {
            NTButtonVariant.Elevated => NTColor.SurfaceContainerLow,
            NTButtonVariant.Filled => NTColor.Primary,
            NTButtonVariant.Tonal => NTColor.SecondaryContainer,
            NTButtonVariant.Outlined => NTColor.Transparent,
            NTButtonVariant.Text => NTColor.Transparent,
            _ => throw new ArgumentOutOfRangeException(nameof(Variant), Variant, null)
        };
    }

    private NTColor GetDefaultToggleBackgroundColor() {
        return Variant switch {
            NTButtonVariant.Elevated => Selected ? NTColor.Primary : NTColor.SurfaceContainerLow,
            NTButtonVariant.Filled => Selected ? NTColor.Primary : NTColor.SurfaceContainer,
            NTButtonVariant.Tonal => Selected ? NTColor.Secondary : NTColor.SecondaryContainer,
            NTButtonVariant.Outlined => Selected ? NTColor.InverseSurface : NTColor.Transparent,
            NTButtonVariant.Text => NTColor.Transparent,
            _ => throw new ArgumentOutOfRangeException(nameof(Variant), Variant, null)
        };
    }

    private NTElevation GetDefaultElevation() {
        return Variant == NTButtonVariant.Elevated ? NTElevation.Lowest : NTElevation.None;
    }

    private NTColor GetDefaultTextColor() {
        if (IsToggleButton) {
            return GetDefaultToggleTextColor();
        }

        return Variant switch {
            NTButtonVariant.Elevated => NTColor.Primary,
            NTButtonVariant.Filled => NTColor.OnPrimary,
            NTButtonVariant.Tonal => NTColor.OnSecondaryContainer,
            NTButtonVariant.Outlined => NTColor.Primary,
            NTButtonVariant.Text => NTColor.Primary,
            _ => throw new ArgumentOutOfRangeException(nameof(Variant), Variant, null)
        };
    }

    private NTColor GetDefaultToggleTextColor() {
        return Variant switch {
            NTButtonVariant.Elevated => Selected ? NTColor.OnPrimary : NTColor.Primary,
            NTButtonVariant.Filled => Selected ? NTColor.OnPrimary : NTColor.OnSurfaceVariant,
            NTButtonVariant.Tonal => Selected ? NTColor.OnSecondary : NTColor.OnSecondaryContainer,
            NTButtonVariant.Outlined => Selected ? NTColor.InverseOnSurface : NTColor.OnSurfaceVariant,
            NTButtonVariant.Text => NTColor.Primary,
            _ => throw new ArgumentOutOfRangeException(nameof(Variant), Variant, null)
        };
    }

    private void ValidateBackgroundColorForVariant() {
        if (Variant is NTButtonVariant.Text or NTButtonVariant.Outlined) {
            if (Variant == NTButtonVariant.Outlined && IsToggleButton && Selected) {
                if (BackgroundColor is NTColor.Transparent) {
                    throw new InvalidOperationException($"{Variant} selected toggle buttons must use a visible container {nameof(BackgroundColor)}.");
                }

                return;
            }

            if (BackgroundColor != NTColor.Transparent) {
                throw new InvalidOperationException($"{Variant} buttons must use a transparent {nameof(BackgroundColor)}.");
            }

            return;
        }

        if (BackgroundColor is NTColor.Transparent) {
            throw new InvalidOperationException($"{Variant} buttons must use a visible container {nameof(BackgroundColor)}.");
        }
    }

    private void ValidateVariantColorCombination() {
        if (BackgroundColor.HasValue) {
            ValidateBackgroundColorForVariant();
        }

        if (TextColor is NTColor.Transparent) {
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
