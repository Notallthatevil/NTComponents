using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
///     Material 3 button component with explicit variant, size, shape, and toggle semantics.
/// </summary>
public partial class NTButton : TnTComponentBase {
    private static readonly NTMotionEasing ShapeMorphEasing = NTMotionEasing.Standard;

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

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("nt-button")
        .AddClass("nt-button--elevated", Variant == NTButtonVariant.Elevated)
        .AddClass("nt-button--filled", Variant == NTButtonVariant.Filled)
        .AddClass("nt-button--tonal", Variant == NTButtonVariant.Tonal)
        .AddClass("nt-button--outlined", Variant == NTButtonVariant.Outlined)
        .AddClass("nt-button--text", Variant == NTButtonVariant.Text)
        .AddClass("nt-button--toggle", IsToggleButton)
        .AddClass("nt-button--selected", Selected)
        .AddClass("nt-button--shape-round", EffectiveShape == ButtonShape.Round)
        .AddClass("nt-button--shape-square", EffectiveShape == ButtonShape.Square)
        .AddClass("nt-button--with-icon", LeadingIcon is not null)
        .AddClass("nt-button--size-xs", ButtonSize is Size.Smallest or Size.XS)
        .AddClass("nt-button--size-s", ButtonSize == Size.Small)
        .AddClass("nt-button--size-m", ButtonSize == Size.Medium)
        .AddClass("nt-button--size-l", ButtonSize == Size.Large)
        .AddClass("nt-button--size-xl", ButtonSize is Size.Largest or Size.XL)
        .AddClass(ShapeMorphEasing.ToCssClass())
        .AddDisabled(Disabled)
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddVariable("nt-button-bg", BackgroundColor.ToCssTnTColorVariable(), BackgroundColor.HasValue)
        .AddVariable("nt-button-fg", TextColor.ToCssTnTColorVariable(), TextColor.HasValue)
        .AddVariable("nt-button-state-layer-color", TextColor.ToCssTnTColorVariable(), TextColor.HasValue)
        .AddVariable("nt-button-focus-ring-color", TextColor.ToCssTnTColorVariable(), TextColor.HasValue)
        .Build();

    internal string? AriaPressed => IsToggleButton ? Selected.ToString().ToLowerInvariant() : null;

    private ButtonShape EffectiveShape => IsToggleButton && Selected
        ? Shape == ButtonShape.Round ? ButtonShape.Square : ButtonShape.Round
        : Shape;

    /// <inheritdoc />
    protected override void OnParametersSet() {
        if (string.IsNullOrWhiteSpace(Label)) {
            throw new ArgumentException("NTButton requires a non-empty Label.", nameof(Label));
        }

        if (IsToggleButton && Variant == NTButtonVariant.Text) {
            throw new InvalidOperationException("Text buttons do not support toggle behavior.");
        }

        base.OnParametersSet();
    }
}
