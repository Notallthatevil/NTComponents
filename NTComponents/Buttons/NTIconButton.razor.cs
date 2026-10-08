using Microsoft.AspNetCore.Components;
using NTComponents.Core;
using NTComponents.Interfaces;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
///     Material 3 icon-only button component with explicit accessibility, variant, size, width, shape, and toggle semantics.
/// </summary>
/// <remarks>
///     <para>
///         Do use icon buttons for compact, repeated, toolbar, or secondary actions where a visible text label would make the interface harder to scan. Do provide a clear <see cref="AriaLabel" /> for
///         every icon button, and use <see cref="NTButtonBase.Tooltip" /> on web surfaces when the action is not immediately obvious from context. Prefer system icons with clear, familiar meaning.
///     </para>
///     <para>
///         Do use <see cref="IsToggleButton" /> only for persistent selected or on-off state. Consumers can bind <see cref="Selected" /> and change <see cref="Icon" /> at runtime when selected and
///         unselected states need different symbols.
///     </para>
///     <para>
///         Do not use icon buttons when a visible text label is needed for clarity; use <see cref="NTButton" /> instead. Do not rely on ambiguous icons without accessible names, and do not add visible
///         text inside <see cref="NTIconButton" />.
///     </para>
///     <para>
///         <see cref="NTButtonVariant.Text" /> maps to the Material 3 standard icon button. <see cref="NTButtonVariant.Elevated" /> is available as an NTComponents extension for icon actions that need
///         separation from a visually busy surface.
///     </para>
/// </remarks>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.ProgressivelyEnhanced,
    CompatibilitySummary = "Renders useful static HTML and adds Blazor behavior when interactive.",
    CompatibilityDetails = "Static SSR preserves the rendered markup and native browser behavior. EventCallback handlers, bound state updates, and live validation require an interactive render mode.")]
public partial class NTIconButton : NTButtonBase, INTBadgeable {

    /// <summary>
    ///     Gets or sets the accessible name announced for the icon-only button.
    /// </summary>
    /// <remarks>
    ///     This value is required because <see cref="NTIconButton" /> does not render a visible label. Keep it action-oriented, such as "Open menu", "Add item", or "Mark as favorite".
    /// </remarks>
    [Parameter, EditorRequired]
    public string AriaLabel { get; set; } = string.Empty;

    /// <inheritdoc />
    /// <remarks>Announced through <c>aria-describedby</c> after the button's <see cref="AriaLabel" />.</remarks>
    [Parameter]
    public string? BadgeAriaLabel { get; set; }

    /// <inheritdoc />
    [Parameter]
    public string? BadgeContent { get; set; }

    /// <inheritdoc />
    /// <remarks>The badge is anchored to the top-end corner of <see cref="Icon" /> using Material 3 offsets.</remarks>
    [Parameter]
    public bool ShowBadge { get; set; }

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("nt-icon-button")
        .AddClass("nt-icon-button-elevated", Variant == NTButtonVariant.Elevated)
        .AddClass("nt-icon-button-filled", Variant == NTButtonVariant.Filled)
        .AddClass("nt-icon-button-tonal", Variant == NTButtonVariant.Tonal)
        .AddClass("nt-icon-button-outlined", Variant == NTButtonVariant.Outlined)
        .AddClass("nt-icon-button-standard", Variant == NTButtonVariant.Text)
        .AddClass("nt-icon-button-toggle", IsToggleButton)
        .AddClass("nt-icon-button-selected", Selected)
        .AddClass("nt-button-progress-active", ShowProgress)
        .AddClass("nt-icon-button-width-narrow", Width == NTIconButtonAppearance.Narrow)
        .AddClass("nt-icon-button-width-default", Width == NTIconButtonAppearance.Default)
        .AddClass("nt-icon-button-width-wide", Width == NTIconButtonAppearance.Wide)
        .AddClass("nt-icon-button-shape-round", EffectiveShape == ButtonShape.Round)
        .AddClass("nt-icon-button-shape-square", EffectiveShape == ButtonShape.Square)
        .AddElevation(Elevation)
        .AddSize(ButtonSize)
        .AddDisabled(Disabled)
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddVariable("nt-icon-button-bg", BackgroundColor.ToCssNTColorVariable(), BackgroundColor.HasValue)
        .AddVariable("nt-icon-button-fg", TextColor.ToCssNTColorVariable(), TextColor.HasValue)
        .Build();

    /// <summary>
    ///     Gets or sets an optional override for the icon button elevation.
    /// </summary>
    /// <remarks>
    ///     Only <see cref="NTButtonVariant.Elevated" /> icon buttons should use non-zero elevation.
    /// </remarks>
    [Parameter]
    public NTElevation? Elevation { get; set; }

    /// <summary>
    ///     Gets or sets the only visual icon rendered inside the button.
    /// </summary>
    /// <remarks>
    ///     This value is required. For toggle buttons, bind <see cref="Selected" /> and provide a different icon value from the parent when selected and unselected states need different symbols.
    /// </remarks>
    [Parameter, EditorRequired]
    public TnTIcon Icon { get; set; } = default!;

    /// <summary>
    ///     Gets or sets whether this icon button behaves as a toggle button.
    /// </summary>
    /// <remarks>
    ///     Toggle mode renders <c>aria-pressed</c>, flips <see cref="Selected" /> on click, invokes <see cref="SelectedChanged" />, and then invokes <see cref="NTButtonBase.OnClickCallback" />.
    /// </remarks>
    [Parameter]
    public bool IsToggleButton { get; set; }

    /// <summary>
    ///     Gets or sets whether the toggle icon button is currently selected.
    /// </summary>
    [Parameter]
    public bool Selected { get; set; }

    /// <summary>
    ///     Gets or sets the callback invoked when the toggle selected state changes.
    /// </summary>
    [Parameter]
    public EventCallback<bool> SelectedChanged { get; set; }

    /// <summary>
    ///     Gets or sets the base resting shape for the icon button.
    /// </summary>
    /// <remarks>
    ///     Toggle icon buttons morph between round and square selected states. Non-toggle icon buttons use this value as their resting shape and still use the pressed shape during interaction.
    /// </remarks>
    [Parameter]
    public ButtonShape Shape { get; set; } = ButtonShape.Round;

    /// <summary>
    ///     Gets or sets the visual variant of the icon button.
    /// </summary>
    /// <remarks>
    ///     <see cref="NTButtonVariant.Text" /> is rendered as the Material 3 standard icon button.
    /// </remarks>
    [Parameter]
    public NTButtonVariant Variant { get; set; } = NTButtonVariant.Text;

    /// <summary>
    ///     Gets or sets the width treatment for the icon button.
    /// </summary>
    [Parameter]
    public NTIconButtonAppearance Width { get; set; } = NTIconButtonAppearance.Default;

    internal string? AriaPressed => ToggleAriaPressed;

    private string? AriaDescribedBy => NTBadge.GetDescribedBy(AdditionalAttributes?.GetValueOrDefault("aria-describedby"), ShowBadge, BadgeId);

    private string BadgeId => $"{ComponentIdentifier}-badge";

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

        if (Icon is null) {
            throw new ArgumentNullException(nameof(Icon), "NTIconButton requires a non-null Icon parameter.");
        }

        if (string.IsNullOrWhiteSpace(AriaLabel)) {
            throw new ArgumentException("NTIconButton requires a non-empty AriaLabel.", nameof(AriaLabel));
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
            NTButtonVariant.Filled => Selected ? NTColor.Primary : NTColor.SurfaceContainerHighest,
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
            NTButtonVariant.Outlined => NTColor.OnSurfaceVariant,
            NTButtonVariant.Text => NTColor.OnSurfaceVariant,
            _ => throw new ArgumentOutOfRangeException(nameof(Variant), Variant, null)
        };
    }

    private NTColor GetDefaultToggleTextColor() {
        return Variant switch {
            NTButtonVariant.Elevated => Selected ? NTColor.OnPrimary : NTColor.Primary,
            NTButtonVariant.Filled => Selected ? NTColor.OnPrimary : NTColor.Primary,
            NTButtonVariant.Tonal => Selected ? NTColor.OnSecondary : NTColor.OnSecondaryContainer,
            NTButtonVariant.Outlined => Selected ? NTColor.InverseOnSurface : NTColor.OnSurfaceVariant,
            NTButtonVariant.Text => Selected ? NTColor.Primary : NTColor.OnSurfaceVariant,
            _ => throw new ArgumentOutOfRangeException(nameof(Variant), Variant, null)
        };
    }

    private void ValidateBackgroundColorForVariant() {
        if (Variant is NTButtonVariant.Text or NTButtonVariant.Outlined) {
            if (Variant == NTButtonVariant.Outlined && IsToggleButton && Selected) {
                if (BackgroundColor is NTColor.Transparent) {
                    throw new InvalidOperationException($"{Variant} selected toggle icon buttons must use a visible container {nameof(BackgroundColor)}.");
                }

                return;
            }

            if (BackgroundColor != NTColor.Transparent) {
                throw new InvalidOperationException($"{Variant} icon buttons must use a transparent {nameof(BackgroundColor)}.");
            }

            return;
        }

        if (BackgroundColor is NTColor.Transparent) {
            throw new InvalidOperationException($"{Variant} icon buttons must use a visible container {nameof(BackgroundColor)}.");
        }
    }

    private void ValidateVariantColorCombination() {
        if (BackgroundColor.HasValue) {
            ValidateBackgroundColorForVariant();
        }

        if (TextColor is NTColor.Transparent) {
            throw new InvalidOperationException($"{nameof(TextColor)} must be a visible icon color.");
        }
    }

    private void ValidateVariantElevationCombination() {
        if (!Elevation.HasValue) {
            return;
        }

        if (Variant == NTButtonVariant.Elevated) {
            if (Elevation == NTElevation.None) {
                throw new InvalidOperationException($"{nameof(NTButtonVariant.Elevated)} icon buttons must use a non-zero {nameof(Elevation)}.");
            }

            return;
        }

        if (Elevation != NTElevation.None) {
            throw new InvalidOperationException($"{Variant} icon buttons must use {nameof(NTElevation.None)} {nameof(Elevation)}.");
        }
    }
}
