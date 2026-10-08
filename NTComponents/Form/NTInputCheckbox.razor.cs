using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using NTComponents.Core;
using System.Text;

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

    private string? BuildElementStyle() => CssStyleBuilder.Create()
        .AddVariable("nt-checkbox-disabled-container-color", DisabledContainerColor.ToCssNTColorVariable(), DisabledContainerColor.HasValue)
        .AddVariable("nt-checkbox-disabled-icon-color", DisabledIconColor.ToCssNTColorVariable(), DisabledIconColor.HasValue)
        .AddVariable("nt-checkbox-disabled-outline-color", DisabledOutlineColor.ToCssNTColorVariable(), DisabledOutlineColor.HasValue)
        .AddVariable("nt-checkbox-error-color", ErrorColor.ToCssNTColorVariable(), ErrorColor.HasValue)
        .AddVariable("nt-checkbox-error-icon-color", ErrorIconColor.ToCssNTColorVariable(), ErrorIconColor.HasValue)
        .AddVariable("nt-checkbox-focus-outline-color", FocusOutlineColor.ToCssNTColorVariable(), FocusOutlineColor.HasValue)
        .AddVariable("nt-checkbox-hover-outline-color", HoverOutlineColor.ToCssNTColorVariable(), HoverOutlineColor.HasValue)
        .AddVariable("nt-checkbox-icon-color", IconColor.ToCssNTColorVariable(), IconColor.HasValue)
        .AddVariable("nt-checkbox-label-color", LabelColor.ToCssNTColorVariable(), LabelColor.HasValue)
        .AddVariable("nt-checkbox-outline-color", OutlineColor.ToCssNTColorVariable(), OutlineColor.HasValue)
        .AddVariable("nt-checkbox-pressed-outline-color", PressedOutlineColor.ToCssNTColorVariable(), PressedOutlineColor.HasValue)
        .AddVariable("nt-checkbox-selected-container-color", SelectedContainerColor.ToCssNTColorVariable(), SelectedContainerColor.HasValue)
        .AddVariable("nt-checkbox-selected-icon-color", SelectedIconColor.ToCssNTColorVariable(), SelectedIconColor.HasValue)
        .AddVariable("nt-checkbox-selected-state-layer-color", SelectedStateLayerColor.ToCssNTColorVariable(), SelectedStateLayerColor.HasValue)
        .AddVariable("nt-checkbox-state-layer-color", StateLayerColor.ToCssNTColorVariable(), StateLayerColor.HasValue)
        .AddVariable("nt-checkbox-supporting-text-color", SupportingTextColor.ToCssNTColorVariable(), SupportingTextColor.HasValue)
        .Build();

    private string BuildRootClass() {
        var className = new StringBuilder(RootClassBase);
        className.Append(Variant switch {
            NTInputCheckboxVariant.Leading => " nt-checkbox-leading-control",
            NTInputCheckboxVariant.Trailing => " nt-checkbox-trailing-control",
            _ => throw new InvalidOperationException($"{Variant} is not a valid value of {nameof(NTInputCheckboxVariant)}")
        });

        className.Append(EffectiveDensity switch {
            NTFormDensity.Comfortable => " nt-checkbox-comfortable",
            NTFormDensity.Dense => " nt-checkbox-dense",
            _ => " nt-checkbox-standard"
        });

        if (CurrentValue) {
            className.Append(" nt-checkbox-selected");
        }

        if (_isIndeterminate) {
            className.Append(" nt-checkbox-indeterminate");
        }

        if (FieldDisabled) {
            className.Append(" nt-checkbox-disabled");
        }

        if (FieldReadOnly) {
            className.Append(" nt-checkbox-readonly");
        }

        if (LeadingIcon is not null) {
            className.Append(" nt-checkbox-has-leading-icon");
        }

        if (TrailingIcon is not null) {
            className.Append(" nt-checkbox-has-trailing-icon");
        }

        if (string.IsNullOrWhiteSpace(Label)) {
            className.Append(" nt-checkbox-no-label");
        }

        return AppendFieldCssClass(className.ToString());
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
