using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using NTComponents.Core;
using System.Text;

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
    public TnTColor? DisabledHandleColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for disabled handle icon color.
    /// </summary>
    [Parameter]
    public TnTColor? DisabledIconColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for disabled track color.
    /// </summary>
    [Parameter]
    public TnTColor? DisabledTrackColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for error track, handle, and supporting text color.
    /// </summary>
    [Parameter]
    public TnTColor? ErrorColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for row icons.
    /// </summary>
    [Parameter]
    public TnTColor? IconColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for label text.
    /// </summary>
    [Parameter]
    public TnTColor? LabelColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for selected handle color.
    /// </summary>
    [Parameter]
    public TnTColor? SelectedHandleColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for selected handle icon color.
    /// </summary>
    [Parameter]
    public TnTColor? SelectedIconColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for selected state-layer color.
    /// </summary>
    [Parameter]
    public TnTColor? SelectedStateLayerColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for selected track color.
    /// </summary>
    [Parameter]
    public TnTColor? SelectedTrackColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for unselected handle color.
    /// </summary>
    [Parameter]
    public TnTColor? UnselectedHandleColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for unselected handle icon color.
    /// </summary>
    [Parameter]
    public TnTColor? UnselectedIconColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for unselected track outline color.
    /// </summary>
    [Parameter]
    public TnTColor? UnselectedOutlineColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for unselected track color.
    /// </summary>
    [Parameter]
    public TnTColor? UnselectedTrackColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the unselected state-layer color.
    /// </summary>
    [Parameter]
    public TnTColor? StateLayerColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for supporting text when not invalid.
    /// </summary>
    [Parameter]
    public TnTColor? SupportingTextColor { get; set; }

    /// <inheritdoc />
    protected override IEnumerable<string> ExplicitInputAttributeNames => SwitchExplicitInputAttributeNames;

    /// <inheritdoc />
    protected override string InputIdPrefix => "nt-switch";

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();
        _elementStyle = BuildElementStyle();
    }

    private string? BuildElementStyle() => CssStyleBuilder.Create()
        .AddVariable("nt-switch-disabled-handle-color", DisabledHandleColor.ToCssTnTColorVariable(), DisabledHandleColor.HasValue)
        .AddVariable("nt-switch-disabled-icon-color", DisabledIconColor.ToCssTnTColorVariable(), DisabledIconColor.HasValue)
        .AddVariable("nt-switch-disabled-track-color", DisabledTrackColor.ToCssTnTColorVariable(), DisabledTrackColor.HasValue)
        .AddVariable("nt-switch-error-color", ErrorColor.ToCssTnTColorVariable(), ErrorColor.HasValue)
        .AddVariable("nt-switch-icon-color", IconColor.ToCssTnTColorVariable(), IconColor.HasValue)
        .AddVariable("nt-switch-label-color", LabelColor.ToCssTnTColorVariable(), LabelColor.HasValue)
        .AddVariable("nt-switch-selected-handle-color", SelectedHandleColor.ToCssTnTColorVariable(), SelectedHandleColor.HasValue)
        .AddVariable("nt-switch-selected-icon-color", SelectedIconColor.ToCssTnTColorVariable(), SelectedIconColor.HasValue)
        .AddVariable("nt-switch-selected-state-layer-color", SelectedStateLayerColor.ToCssTnTColorVariable(), SelectedStateLayerColor.HasValue)
        .AddVariable("nt-switch-selected-track-color", SelectedTrackColor.ToCssTnTColorVariable(), SelectedTrackColor.HasValue)
        .AddVariable("nt-switch-state-layer-color", StateLayerColor.ToCssTnTColorVariable(), StateLayerColor.HasValue)
        .AddVariable("nt-switch-supporting-text-color", SupportingTextColor.ToCssTnTColorVariable(), SupportingTextColor.HasValue)
        .AddVariable("nt-switch-unselected-handle-color", UnselectedHandleColor.ToCssTnTColorVariable(), UnselectedHandleColor.HasValue)
        .AddVariable("nt-switch-unselected-icon-color", UnselectedIconColor.ToCssTnTColorVariable(), UnselectedIconColor.HasValue)
        .AddVariable("nt-switch-unselected-outline-color", UnselectedOutlineColor.ToCssTnTColorVariable(), UnselectedOutlineColor.HasValue)
        .AddVariable("nt-switch-unselected-track-color", UnselectedTrackColor.ToCssTnTColorVariable(), UnselectedTrackColor.HasValue)
        .Build();

    private string BuildRootClass() {
        var className = new StringBuilder(RootClassBase);
        className.Append(Variant switch {
            NTSwitchVariant.Leading => " nt-switch-leading-control",
            NTSwitchVariant.Trailing => " nt-switch-trailing-control",
            _ => throw new InvalidOperationException($"{Variant} is not a valid value of {nameof(NTSwitchVariant)}")
        });

        className.Append(EffectiveDensity switch {
            NTFormDensity.Comfortable => " nt-switch-comfortable",
            NTFormDensity.Dense => " nt-switch-dense",
            _ => " nt-switch-standard"
        });

        if (CurrentValue) {
            className.Append(" nt-switch-selected");
        }

        if (FieldDisabled) {
            className.Append(" nt-switch-disabled");
        }

        if (FieldReadOnly) {
            className.Append(" nt-switch-readonly");
        }

        if (ShowHandleIcon) {
            className.Append(" nt-switch-has-handle-icon");
        }

        if (LeadingIcon is not null) {
            className.Append(" nt-switch-has-leading-icon");
        }

        if (TrailingIcon is not null) {
            className.Append(" nt-switch-has-trailing-icon");
        }

        if (string.IsNullOrWhiteSpace(Label)) {
            className.Append(" nt-switch-no-label");
        }

        return AppendFieldCssClass(className.ToString());
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
