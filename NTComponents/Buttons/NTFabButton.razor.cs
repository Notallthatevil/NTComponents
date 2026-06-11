using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using NTComponents.Core;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
///     Material 3 floating action button component with icon-only and extended label modes.
/// </summary>
/// <remarks>
///     Use <see cref="NTFabButton" /> for the single most important constructive action on a screen. The icon is always required. When <see cref="Label" /> is supplied the button renders as an
///     extended FAB; otherwise it renders as an icon-only FAB and requires <see cref="AriaLabel" />.
/// </remarks>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.ProgressivelyEnhanced,
    CompatibilitySummary = "Renders useful static HTML and adds Blazor behavior when interactive.",
    CompatibilityDetails = "Static SSR preserves the rendered markup and native browser behavior. EventCallback handlers, bound state updates, and live validation require an interactive render mode.")]
public partial class NTFabButton : NTButtonBase {

    /// <summary>
    ///     Gets or sets the accessible name announced for icon-only FABs or a more descriptive name for extended FABs.
    /// </summary>
    [Parameter]
    public string? AriaLabel { get; set; }

    /// <summary>
    ///     Gets or sets the size of the FAB.
    /// </summary>
    /// <remarks>Supports <see cref="Size.Small" />, <see cref="Size.Medium" />, and <see cref="Size.Large" />. Unsupported enum values map to the nearest supported Material FAB size.</remarks>
    [Parameter]
    public override Size ButtonSize { get; set; } = Size.Small;

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("nt-fab-button")
        .AddClass("nt-fab-button-extended", HasLabel)
        .AddClass("nt-fab-button-icon-only", !HasLabel)
        .AddClass(GetPlacementClass())
        .AddClass("nt-button-progress-active", ShowProgress)
        .AddElevation(Elevation)
        .AddSize(EffectiveButtonSize)
        .AddDisabled(Disabled)
        .Build();

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
    ///     Gets or sets where the FAB is positioned.
    /// </summary>
    /// <remarks>The default <see cref="NTFabButtonPlacement.Inline" /> keeps the FAB in normal document flow. The corner placements use fixed viewport positioning.</remarks>
    [Parameter]
    public NTFabButtonPlacement Placement { get; set; } = NTFabButtonPlacement.Inline;

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
    protected override TnTColor EffectiveProgressColor => TextColor ?? TnTColor.OnPrimaryContainer;

    /// <inheritdoc />
    protected override Size EffectiveProgressSize => EffectiveButtonSize;

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
