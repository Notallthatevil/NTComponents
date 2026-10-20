using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
///     A Material 3 checkbox for selecting one or more options from a set.
/// </summary>
/// <remarks>
///     <para>
///         Use checkboxes when multiple items may be selected, when users need to opt into a form/list option, or when a
///         parent item summarizes a selected subset of children. Use radio buttons for mutually exclusive choices and
///         switches for settings that take effect immediately.
///     </para>
///     <para>
///         Do keep labels short, concrete, and programmatically associated with the input. When no visible label is
///         rendered, provide <c>aria-label</c> or <c>aria-labelledby</c> through
///         <see cref="InputBase{TValue}.AdditionalAttributes" />. Do use supporting text for brief context and validation
///         messages for actionable errors. Do use <see cref="Indeterminate" /> only for parent/child selection summaries.
///     </para>
///     <para>
///         Do not use a checkbox for a single immediate on/off app setting, do not use it when only one item in a group can
///         be selected, do not rely on color alone to communicate errors, and do not hide the accessible label.
///     </para>
/// </remarks>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.SsrCompatible,
    CompatibilitySummary = "Renders native form markup that works with static SSR and form posts.",
    CompatibilityDetails = "The native control can participate in static SSR and normal form posts. Blazor binding callbacks and live validation updates require interactivity or a subsequent render.")]
public partial class NTInputCheckbox {
    private const string RootClassBase = "nt-checkbox";
    private static readonly string[] CheckboxExplicitInputAttributeNames = [.. CommonExplicitInputAttributeNames, "data-indeterminate"];
    private bool _isIndeterminate;
    private string? _elementStyle;
    private string? _rootClass;
    private (NTInputCheckboxVariant, NTFormDensity, int) _rootClassKey;

    /// <summary>
    ///     Gets or sets whether the checkbox renders the mixed/indeterminate visual state.
    /// </summary>
    /// <remarks>
    ///     Use indeterminate for parent checkboxes when some, but not all, child items are selected. The caller owns the
    ///     parent/child selection model; changing this checkbox clears the visual indeterminate state locally and raises
    ///     <see cref="IndeterminateChanged" /> with <c>false</c>. Do not submit indeterminate as a separate value; the
    ///     bound <see cref="InputBase{TValue}.Value" /> remains the selected/unselected boolean.
    /// </remarks>
    [Parameter]
    public bool Indeterminate { get; set; }

    /// <summary>
    ///     Gets or sets the callback for two-way binding <see cref="Indeterminate" />.
    /// </summary>
    [Parameter]
    public EventCallback<bool> IndeterminateChanged { get; set; }

    /// <summary>
    ///     Gets or sets the checkbox layout variant.
    /// </summary>
    /// <remarks>
    ///     Use <see cref="NTInputCheckboxVariant.Leading" /> for the standard Material checkbox layout. Use
    ///     <see cref="NTInputCheckboxVariant.Trailing" /> for full-width rows where the label should stay near the leading edge
    ///     and the checkbox should sit on the trailing edge, such as settings lists or preference rows.
    /// </remarks>
    [Parameter]
    public NTInputCheckboxVariant Variant { get; set; } = NTInputCheckboxVariant.Leading;

    /// <summary>
    ///     Gets or sets an optional override for the disabled selected container color.
    /// </summary>
    /// <remarks>
    ///     Prefer the default Material color roles. Override colors for themed surfaces only when contrast remains clear in
    ///     selected, unselected, error, focus, hover, pressed, and disabled states.
    /// </remarks>
    [Parameter]
    public NTColor? DisabledContainerColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the disabled selected icon color.
    /// </summary>
    [Parameter]
    public NTColor? DisabledIconColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the disabled unselected outline color.
    /// </summary>
    [Parameter]
    public NTColor? DisabledOutlineColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for error outline and selected container color.
    /// </summary>
    [Parameter]
    public NTColor? ErrorColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the selected error icon color.
    /// </summary>
    [Parameter]
    public NTColor? ErrorIconColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the focused unselected outline color.
    /// </summary>
    [Parameter]
    public NTColor? FocusOutlineColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the hovered unselected outline color.
    /// </summary>
    [Parameter]
    public NTColor? HoverOutlineColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for label text color.
    /// </summary>
    [Parameter]
    public NTColor? LabelColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for leading and trailing icon color.
    /// </summary>
    [Parameter]
    public NTColor? IconColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the resting unselected outline color.
    /// </summary>
    [Parameter]
    public NTColor? OutlineColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the pressed unselected outline color.
    /// </summary>
    [Parameter]
    public NTColor? PressedOutlineColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the selected container color.
    /// </summary>
    [Parameter]
    public NTColor? SelectedContainerColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the selected icon color.
    /// </summary>
    [Parameter]
    public NTColor? SelectedIconColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the selected state-layer color.
    /// </summary>
    [Parameter]
    public NTColor? SelectedStateLayerColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the unselected state-layer color.
    /// </summary>
    [Parameter]
    public NTColor? StateLayerColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for supporting and error text color when not invalid.
    /// </summary>
    [Parameter]
    public NTColor? SupportingTextColor { get; set; }

    /// <inheritdoc />
    protected override IEnumerable<string> ExplicitInputAttributeNames => CheckboxExplicitInputAttributeNames;

    /// <inheritdoc />
    protected override string InputIdPrefix => "nt-checkbox";

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();
        _isIndeterminate = Indeterminate;
        _elementStyle = BuildElementStyle();
    }

    /// <inheritdoc />
    protected override async Task OnChangeAsync(ChangeEventArgs args) {
        if (FieldReadOnly || FieldDisabled) {
            return;
        }

        CurrentValue = args.Value is bool boolValue && boolValue;
        if (_isIndeterminate) {
            _isIndeterminate = false;
            await IndeterminateChanged.InvokeAsync(false);
        }

        await BindAfter.InvokeAsync(CurrentValue);
    }

    private string? BuildElementStyle() {
        var style = AppendColorVariable(null, "nt-checkbox-disabled-container-color", DisabledContainerColor);
        style = AppendColorVariable(style, "nt-checkbox-disabled-icon-color", DisabledIconColor);
        style = AppendColorVariable(style, "nt-checkbox-disabled-outline-color", DisabledOutlineColor);
        style = AppendColorVariable(style, "nt-checkbox-error-color", ErrorColor);
        style = AppendColorVariable(style, "nt-checkbox-error-icon-color", ErrorIconColor);
        style = AppendColorVariable(style, "nt-checkbox-focus-outline-color", FocusOutlineColor);
        style = AppendColorVariable(style, "nt-checkbox-hover-outline-color", HoverOutlineColor);
        style = AppendColorVariable(style, "nt-checkbox-icon-color", IconColor);
        style = AppendColorVariable(style, "nt-checkbox-label-color", LabelColor);
        style = AppendColorVariable(style, "nt-checkbox-outline-color", OutlineColor);
        style = AppendColorVariable(style, "nt-checkbox-pressed-outline-color", PressedOutlineColor);
        style = AppendColorVariable(style, "nt-checkbox-selected-container-color", SelectedContainerColor);
        style = AppendColorVariable(style, "nt-checkbox-selected-icon-color", SelectedIconColor);
        style = AppendColorVariable(style, "nt-checkbox-selected-state-layer-color", SelectedStateLayerColor);
        style = AppendColorVariable(style, "nt-checkbox-state-layer-color", StateLayerColor);
        return AppendColorVariable(style, "nt-checkbox-supporting-text-color", SupportingTextColor);
    }

    private string BuildRootClass() {
        var flags = (CurrentValue ? 1 : 0) | (_isIndeterminate ? 2 : 0) | (FieldDisabled ? 4 : 0) | (FieldReadOnly ? 8 : 0)
            | (LeadingIcon is not null ? 16 : 0) | (TrailingIcon is not null ? 32 : 0) | (string.IsNullOrWhiteSpace(Label) ? 64 : 0);
        // The class string only changes with these inputs, so reuse it across renders.
        if (_rootClass is null || _rootClassKey != (Variant, EffectiveDensity, flags)) {
            _rootClass = string.Concat(
                RootClassBase,
                Variant switch {
                    NTInputCheckboxVariant.Leading => " nt-checkbox-leading-control",
                    NTInputCheckboxVariant.Trailing => " nt-checkbox-trailing-control",
                    _ => throw new InvalidOperationException($"{Variant} is not a valid value of {nameof(NTInputCheckboxVariant)}")
                },
                EffectiveDensity switch {
                    NTFormDensity.Comfortable => " nt-checkbox-comfortable",
                    NTFormDensity.Dense => " nt-checkbox-dense",
                    _ => " nt-checkbox-standard"
                },
                CurrentValue ? " nt-checkbox-selected" : null,
                _isIndeterminate ? " nt-checkbox-indeterminate" : null,
                FieldDisabled ? " nt-checkbox-disabled" : null,
                FieldReadOnly ? " nt-checkbox-readonly" : null,
                LeadingIcon is not null ? " nt-checkbox-has-leading-icon" : null,
                TrailingIcon is not null ? " nt-checkbox-has-trailing-icon" : null,
                string.IsNullOrWhiteSpace(Label) ? " nt-checkbox-no-label" : null);
            _rootClassKey = (Variant, EffectiveDensity, flags);
        }

        return AppendFieldCssClass(_rootClass);
    }
}

/// <summary>
///     Defines the checkbox control placement within an <see cref="NTInputCheckbox" /> row.
/// </summary>
public enum NTInputCheckboxVariant {

    /// <summary>
    ///     Places the checkbox before the label, matching the standard Material checkbox layout.
    /// </summary>
    Leading,

    /// <summary>
    ///     Places the checkbox on the trailing edge and stretches the row across its parent container.
    /// </summary>
    Trailing
}
