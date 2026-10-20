using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
///     A Material 3 switch for immediately toggling a setting on or off.
/// </summary>
/// <remarks>
///     <para>
///         Use switches for settings that can be independently controlled and whose selected/unselected state should be
///         visible at a glance. Use checkboxes when users are making selections in a form or choosing one or more items
///         from a set, and use radio buttons when only one option can be selected.
///     </para>
///     <para>
///         Keep labels short and programmatically associated with the switch. When the visible label is ambiguous, provide
///         a more descriptive <c>aria-label</c> or <c>aria-labelledby</c> through
///         <see cref="InputBase{TValue}.AdditionalAttributes" />. Do not rely on color alone to communicate state.
///     </para>
///     <para>
///         Material 3 recommends keeping switch targets at least 48 by 48 CSS pixels. Dense is available for compact
///         application surfaces, but should be an opt-in layout choice rather than the default.
///     </para>
/// </remarks>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.SsrCompatible,
    CompatibilitySummary = "Renders native form markup that works with static SSR and form posts.",
    CompatibilityDetails = "The native control can participate in static SSR and normal form posts. Blazor binding callbacks and live validation updates require interactivity or a subsequent render.")]
public partial class NTInputSwitch {
    private const string RootClassBase = "nt-switch";
    private static readonly string[] SwitchExplicitInputAttributeNames = [.. CommonExplicitInputAttributeNames, "role"];
    private string? _elementStyle;
    private string? _rootClass;
    private (NTSwitchVariant, NTFormDensity, int) _rootClassKey;

    /// <summary>
    ///     Gets or sets whether Material handle icons are rendered inside the switch handle.
    /// </summary>
    /// <remarks>
    ///     Material 3 allows optional icons inside the handle. Icons are off by default to keep the switch visually quiet
    ///     and reduce DOM size. Enable them when the additional state cue helps recognition.
    /// </remarks>
    [Parameter]
    public bool ShowHandleIcon { get; set; }

    /// <summary>
    ///     Gets or sets the switch layout variant.
    /// </summary>
    /// <remarks>
    ///     Use <see cref="NTSwitchVariant.Leading" /> for the standard Material control-before-label layout. Use
    ///     <see cref="NTSwitchVariant.Trailing" /> for full-width settings rows where labels stay near the leading edge and
    ///     the switch sits on the trailing edge.
    ///     Keep variant usage consistent inside the same settings group so users can scan state and labels predictably.
    /// </remarks>
    [Parameter]
    public NTSwitchVariant Variant { get; set; } = NTSwitchVariant.Leading;

    /// <summary>
    ///     Gets or sets an optional override for disabled handle color.
    /// </summary>
    /// <remarks>
    ///     Prefer the default Material color roles. Override only when the surrounding theme surface requires it and the
    ///     disabled state remains visually distinct from enabled selected and unselected states.
    /// </remarks>
    [Parameter]
    public NTColor? DisabledHandleColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for disabled handle icon color.
    /// </summary>
    [Parameter]
    public NTColor? DisabledIconColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for disabled track color.
    /// </summary>
    [Parameter]
    public NTColor? DisabledTrackColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for error track, handle, and supporting text color.
    /// </summary>
    [Parameter]
    public NTColor? ErrorColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for row icons.
    /// </summary>
    [Parameter]
    public NTColor? IconColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for label text.
    /// </summary>
    [Parameter]
    public NTColor? LabelColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for selected handle color.
    /// </summary>
    [Parameter]
    public NTColor? SelectedHandleColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for selected handle icon color.
    /// </summary>
    [Parameter]
    public NTColor? SelectedIconColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for selected state-layer color.
    /// </summary>
    [Parameter]
    public NTColor? SelectedStateLayerColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for selected track color.
    /// </summary>
    [Parameter]
    public NTColor? SelectedTrackColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for unselected handle color.
    /// </summary>
    [Parameter]
    public NTColor? UnselectedHandleColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for unselected handle icon color.
    /// </summary>
    [Parameter]
    public NTColor? UnselectedIconColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for unselected track outline color.
    /// </summary>
    [Parameter]
    public NTColor? UnselectedOutlineColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for unselected track color.
    /// </summary>
    [Parameter]
    public NTColor? UnselectedTrackColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the unselected state-layer color.
    /// </summary>
    [Parameter]
    public NTColor? StateLayerColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for supporting text when not invalid.
    /// </summary>
    [Parameter]
    public NTColor? SupportingTextColor { get; set; }

    /// <inheritdoc />
    protected override IEnumerable<string> ExplicitInputAttributeNames => SwitchExplicitInputAttributeNames;

    /// <inheritdoc />
    protected override string InputIdPrefix => "nt-switch";

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();
        _elementStyle = BuildElementStyle();
    }

    private string? BuildElementStyle() {
        var style = AppendColorVariable(null, "nt-switch-disabled-handle-color", DisabledHandleColor);
        style = AppendColorVariable(style, "nt-switch-disabled-icon-color", DisabledIconColor);
        style = AppendColorVariable(style, "nt-switch-disabled-track-color", DisabledTrackColor);
        style = AppendColorVariable(style, "nt-switch-error-color", ErrorColor);
        style = AppendColorVariable(style, "nt-switch-icon-color", IconColor);
        style = AppendColorVariable(style, "nt-switch-label-color", LabelColor);
        style = AppendColorVariable(style, "nt-switch-selected-handle-color", SelectedHandleColor);
        style = AppendColorVariable(style, "nt-switch-selected-icon-color", SelectedIconColor);
        style = AppendColorVariable(style, "nt-switch-selected-state-layer-color", SelectedStateLayerColor);
        style = AppendColorVariable(style, "nt-switch-selected-track-color", SelectedTrackColor);
        style = AppendColorVariable(style, "nt-switch-state-layer-color", StateLayerColor);
        style = AppendColorVariable(style, "nt-switch-supporting-text-color", SupportingTextColor);
        style = AppendColorVariable(style, "nt-switch-unselected-handle-color", UnselectedHandleColor);
        style = AppendColorVariable(style, "nt-switch-unselected-icon-color", UnselectedIconColor);
        style = AppendColorVariable(style, "nt-switch-unselected-outline-color", UnselectedOutlineColor);
        return AppendColorVariable(style, "nt-switch-unselected-track-color", UnselectedTrackColor);
    }

    private string BuildRootClass() {
        var flags = (CurrentValue ? 1 : 0) | (FieldDisabled ? 2 : 0) | (FieldReadOnly ? 4 : 0) | (ShowHandleIcon ? 8 : 0)
            | (LeadingIcon is not null ? 16 : 0) | (TrailingIcon is not null ? 32 : 0) | (string.IsNullOrWhiteSpace(Label) ? 64 : 0);
        // The class string only changes with these inputs, so reuse it across renders.
        if (_rootClass is null || _rootClassKey != (Variant, EffectiveDensity, flags)) {
            _rootClass = string.Concat(
                RootClassBase,
                Variant switch {
                    NTSwitchVariant.Leading => " nt-switch-leading-control",
                    NTSwitchVariant.Trailing => " nt-switch-trailing-control",
                    _ => throw new InvalidOperationException($"{Variant} is not a valid value of {nameof(NTSwitchVariant)}")
                },
                EffectiveDensity switch {
                    NTFormDensity.Comfortable => " nt-switch-comfortable",
                    NTFormDensity.Dense => " nt-switch-dense",
                    _ => " nt-switch-standard"
                },
                CurrentValue ? " nt-switch-selected" : null,
                FieldDisabled ? " nt-switch-disabled" : null,
                FieldReadOnly ? " nt-switch-readonly" : null,
                ShowHandleIcon ? " nt-switch-has-handle-icon" : null,
                LeadingIcon is not null ? " nt-switch-has-leading-icon" : null,
                TrailingIcon is not null ? " nt-switch-has-trailing-icon" : null,
                string.IsNullOrWhiteSpace(Label) ? " nt-switch-no-label" : null);
            _rootClassKey = (Variant, EffectiveDensity, flags);
        }

        return AppendFieldCssClass(_rootClass);
    }
}

/// <summary>
///     Defines the switch control placement within an <see cref="NTInputSwitch" /> row.
/// </summary>
/// <remarks>
///     Choose one variant per settings group. Mixing leading and trailing placement in the same group makes it harder to
///     scan labels and selected state.
/// </remarks>
public enum NTSwitchVariant {

    /// <summary>
    ///     Places the switch before the label, matching the standard Material switch layout.
    /// </summary>
    /// <remarks>
    ///     Use for compact inline controls and short labels where the switch itself is the first visual cue.
    /// </remarks>
    Leading,

    /// <summary>
    ///     Places the switch on the trailing edge and stretches the row across its parent container.
    /// </summary>
    /// <remarks>
    ///     Use for settings-list rows where users scan labels first and compare the switch state on the trailing edge.
    /// </remarks>
    Trailing
}
