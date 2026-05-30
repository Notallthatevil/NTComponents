using Microsoft.AspNetCore.Components;
using NTComponents.Core;
using System.Text;

namespace NTComponents;

/// <summary>
///     Base class for Material 3 aligned FormV2 fields.
/// </summary>
/// <remarks>
///     Derive field components from this type when they share the Material field shell: appearance, density, label,
///     supporting/error text, icons, color overrides, form inheritance, stable ids, and Blazor validation integration.
///     Concrete components remain responsible for rendering the native control and parsing its value.
/// </remarks>
/// <typeparam name="TValue">The field value type.</typeparam>
public abstract class NTFieldBase<TValue> : NTFormControlBase<TValue> {
    private string _appearanceClass = "nt-input-outlined";
    private string _densityClass = "nt-input-standard";
    private string? _rootClassWithInvalid;
    private string? _rootClassWithoutInvalid;

    /// <summary>
    ///     Gets or sets the field appearance. When null, the containing <see cref="NTForm" /> value is used.
    /// </summary>
    [Parameter]
    public NTFormAppearance? Appearance { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the filled field container color.
    /// </summary>
    [Parameter]
    public TnTColor? BackgroundColor { get; set; }

    /// <summary>
    /// Gets or sets the autocomplete attribute value. Defaults to <see cref="NTComponents.AutoComplete.Off"/> to make
    /// this behavior explicit. 
    /// </summary>
    [Parameter]
    public string? AutoComplete { get; set; } = NTComponents.AutoComplete.Off;

    /// <summary>
    ///     Gets or sets an optional override for the resting filled active indicator color.
    /// </summary>
    [Parameter]
    public TnTColor? ActiveIndicatorColor { get; set; }

    /// <summary>
    ///     Gets or sets a callback invoked after binding.
    /// </summary>
    [Parameter]
    public EventCallback<TValue?> BindAfter { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the text cursor color.
    /// </summary>
    [Parameter]
    public TnTColor? CaretColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the disabled filled container color.
    /// </summary>
    [Parameter]
    public TnTColor? DisabledContainerColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for disabled content, label, icon, supporting text, and counter color.
    /// </summary>
    [Parameter]
    public TnTColor? DisabledContentColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the disabled outline color.
    /// </summary>
    [Parameter]
    public TnTColor? DisabledOutlineColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for error text, label, outline, indicator, and icon color.
    /// </summary>
    [Parameter]
    public TnTColor? ErrorColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the text cursor color in the error state.
    /// </summary>
    [Parameter]
    public TnTColor? ErrorCaretColor { get; set; }

    /// <summary>
    ///     Gets or sets the error icon shown when the field is invalid.
    /// </summary>
    [Parameter]
    public TnTIcon? ErrorIcon { get; set; } = MaterialIcon.Error;

    /// <summary>
    ///     Gets or sets an optional override for the focus label, outline, active indicator, and default caret color.
    /// </summary>
    [Parameter]
    public TnTColor? FocusColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the hovered filled active indicator color.
    /// </summary>
    [Parameter]
    public TnTColor? HoverActiveIndicatorColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the hovered outline color.
    /// </summary>
    [Parameter]
    public TnTColor? HoverOutlineColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for leading and trailing icon color.
    /// </summary>
    [Parameter]
    public TnTColor? IconColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the resting label color.
    /// </summary>
    [Parameter]
    public TnTColor? LabelColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the resting outlined border color.
    /// </summary>
    [Parameter]
    public TnTColor? OutlineColor { get; set; }

    /// <summary>
    ///     Gets or sets placeholder text.
    /// </summary>
    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for visible placeholder text color.
    /// </summary>
    [Parameter]
    public TnTColor? PlaceholderColor { get; set; }

    /// <summary>
    ///     Gets or sets prefix text shown before the editable value.
    /// </summary>
    [Parameter]
    public string? PrefixText { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for prefix and suffix text color.
    /// </summary>
    [Parameter]
    public TnTColor? PrefixSuffixColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the hover state-layer color.
    /// </summary>
    [Parameter]
    public TnTColor? StateLayerColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for supporting text and counter color.
    /// </summary>
    [Parameter]
    public TnTColor? SupportingTextColor { get; set; }

    /// <summary>
    ///     Gets or sets suffix text shown after the editable value.
    /// </summary>
    [Parameter]
    public string? SuffixText { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for input text color.
    /// </summary>
    [Parameter]
    public TnTColor? TextColor { get; set; }

    /// <summary>
    ///     Gets additional attributes filtered for the concrete native control.
    /// </summary>
    protected IReadOnlyDictionary<string, object?>? ControlAttributes { get; private set; }

    /// <summary>
    ///     Gets the current effective appearance.
    /// </summary>
    protected NTFormAppearance EffectiveAppearance { get; private set; } = NTFormAppearance.Outlined;

    /// <summary>
    ///     Gets the synthetic placeholder used by native text controls to drive floating label state.
    /// </summary>
    protected string EffectivePlaceholder { get; private set; } = " ";

    /// <summary>
    ///     Gets a value indicating whether the native required attribute is present.
    /// </summary>
    protected bool IsRequired { get; private set; }

    /// <summary>
    ///     Gets the id for prefix text.
    /// </summary>
    protected string PrefixId => $"{InputId}-prefix";

    /// <summary>
    ///     Gets the id for suffix text.
    /// </summary>
    protected string SuffixId => $"{InputId}-suffix";

    /// <summary>
    ///     Gets inline CSS variable overrides for the field.
    /// </summary>
    protected string? ElementStyle => CssStyleBuilder.Create()
        .AddVariable("nt-input-active-indicator-color", ActiveIndicatorColor.ToCssTnTColorVariable(), ActiveIndicatorColor.HasValue)
        .AddVariable("nt-input-caret-color", CaretColor.ToCssTnTColorVariable(), CaretColor.HasValue)
        .AddVariable("nt-input-container-color", BackgroundColor.ToCssTnTColorVariable(), BackgroundColor.HasValue)
        .AddVariable("nt-input-disabled-container-color", DisabledContainerColor.ToCssTnTColorVariable(), DisabledContainerColor.HasValue)
        .AddVariable("nt-input-disabled-content-color", DisabledContentColor.ToCssTnTColorVariable(), DisabledContentColor.HasValue)
        .AddVariable("nt-input-disabled-outline-color", DisabledOutlineColor.ToCssTnTColorVariable(), DisabledOutlineColor.HasValue)
        .AddVariable("nt-input-error-caret-color", ErrorCaretColor.ToCssTnTColorVariable(), ErrorCaretColor.HasValue)
        .AddVariable("nt-input-error-color", ErrorColor.ToCssTnTColorVariable(), ErrorColor.HasValue)
        .AddVariable("nt-input-focus-color", FocusColor.ToCssTnTColorVariable(), FocusColor.HasValue)
        .AddVariable("nt-input-hover-active-indicator-color", HoverActiveIndicatorColor.ToCssTnTColorVariable(), HoverActiveIndicatorColor.HasValue)
        .AddVariable("nt-input-hover-outline-color", HoverOutlineColor.ToCssTnTColorVariable(), HoverOutlineColor.HasValue)
        .AddVariable("nt-input-icon-color", IconColor.ToCssTnTColorVariable(), IconColor.HasValue)
        .AddVariable("nt-input-label-color", LabelColor.ToCssTnTColorVariable(), LabelColor.HasValue)
        .AddVariable("nt-input-outline-color", OutlineColor.ToCssTnTColorVariable(), OutlineColor.HasValue)
        .AddVariable("nt-input-placeholder-color", PlaceholderColor.ToCssTnTColorVariable(), PlaceholderColor.HasValue)
        .AddVariable("nt-input-prefix-suffix-color", PrefixSuffixColor.ToCssTnTColorVariable(), PrefixSuffixColor.HasValue)
        .AddVariable("nt-input-state-layer-color", StateLayerColor.ToCssTnTColorVariable(), StateLayerColor.HasValue)
        .AddVariable("nt-input-supporting-text-color", SupportingTextColor.ToCssTnTColorVariable(), SupportingTextColor.HasValue)
        .AddVariable("nt-input-text-color", TextColor.ToCssTnTColorVariable(), TextColor.HasValue)
        .Build();

    /// <summary>
    ///     Gets concrete control attribute names that should not pass through from additional attributes.
    /// </summary>
    protected abstract IEnumerable<string> ExplicitControlAttributeNames { get; }

    /// <inheritdoc />
    protected override string InputIdPrefix => "nt-input";

    /// <inheritdoc />
    protected override bool HasRequiredSupportingText => IsRequired;

    /// <summary>
    ///     Gets a value indicating whether the current value should float the label.
    /// </summary>
    protected virtual bool HasFloatingValue => !string.IsNullOrEmpty(CurrentValueAsString);

    /// <summary>
    ///     Gets a value indicating whether the field has extra supporting row content.
    /// </summary>
    protected virtual bool HasSupportingRowContent => false;

    /// <summary>
    ///     Builds additional native control attributes supplied by derived field types.
    /// </summary>
    /// <returns>The additional attributes to merge onto the native control.</returns>
    protected virtual IReadOnlyDictionary<string, object?>? BuildAdditionalControlAttributes() => null;

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();
        CacheEffectiveState();
        IsRequired = HasAdditionalAttribute("required");
        ControlAttributes = BuildControlAttributes();
    }

    /// <summary>
    ///     Gets the root CSS class for the current validation state.
    /// </summary>
    /// <param name="hasErrorText">Whether the field has active error text.</param>
    /// <returns>The root CSS class.</returns>
    protected string GetRootClass(bool hasErrorText) => hasErrorText ? _rootClassWithInvalid ?? "nt-input" : _rootClassWithoutInvalid ?? "nt-input";

    /// <summary>
    ///     Builds additional described-by ids for concrete fields.
    /// </summary>
    /// <param name="current">The current described-by value.</param>
    /// <returns>The updated described-by value.</returns>
    protected virtual string? BuildAdditionalDescribedBy(string? current) => current;

    /// <summary>
    ///     Builds additional root classes for concrete fields.
    /// </summary>
    /// <param name="builder">The root class builder.</param>
    protected virtual void BuildAdditionalRootClasses(StringBuilder builder) { }

    /// <summary>
    ///     Creates the trailing adornment state.
    /// </summary>
    /// <param name="hasErrorText">Whether the field has error text.</param>
    /// <returns>The trailing adornment state.</returns>
    protected virtual TrailingAdornmentState CreateTrailingAdornmentState(bool hasErrorText) {
        if (hasErrorText) {
            return new TrailingAdornmentState {
                Icon = ErrorIcon ?? MaterialIcon.Error,
                Class = "nt-input-error-icon",
                AriaLabel = "Error"
            };
        }

        return TrailingIcon is null
            ? default
            : new TrailingAdornmentState {
                Icon = TrailingIcon,
                Class = "nt-input-trailing",
                AriaHidden = "true"
            };
    }

    private void CacheEffectiveState() {
        EffectiveAppearance = Appearance ?? Form?.Appearance ?? NTFormAppearance.Outlined;
        var effectiveDensity = Density ?? Form?.Density ?? NTFormDensity.Standard;
        _appearanceClass = EffectiveAppearance switch {
            NTFormAppearance.Filled => "nt-input-filled",
            NTFormAppearance.Outlined => "nt-input-outlined",
            _ => $"nt-input-{EffectiveAppearance.ToString().ToLowerInvariant()}"
        };
        _densityClass = effectiveDensity switch {
            NTFormDensity.Comfortable => "nt-input-comfortable",
            NTFormDensity.Standard => "nt-input-standard",
            NTFormDensity.Dense => "nt-input-dense",
            _ => $"nt-input-{effectiveDensity.ToString().ToLowerInvariant()}"
        };

        EffectivePlaceholder = string.IsNullOrWhiteSpace(Placeholder) ? " " : Placeholder;
        _rootClassWithoutInvalid = BuildRootClass(isInvalid: false);
        _rootClassWithInvalid = BuildRootClass(isInvalid: true);
    }

    private IReadOnlyDictionary<string, object?>? BuildControlAttributes() => BuildFilteredAttributes(ExplicitControlAttributeNames, BuildAdditionalControlAttributes());

    private string BuildRootClass(bool isInvalid) {
        var className = new StringBuilder("nt-input ");
        className.Append(_appearanceClass);
        className.Append(' ');
        className.Append(_densityClass);

        if (isInvalid) {
            className.Append(" nt-input-invalid");
        }

        if (FieldDisabled) {
            className.Append(" nt-input-disabled");
        }

        if (FieldReadOnly) {
            className.Append(" nt-input-readonly");
        }

        if (!HasLabel) {
            className.Append(" nt-input-no-label");
        }

        if (!string.IsNullOrWhiteSpace(Placeholder)) {
            className.Append(" nt-input-has-placeholder");
        }

        if (HasFloatingValue) {
            className.Append(" nt-input-has-value");
        }

        BuildAdditionalRootClasses(className);
        return className.ToString();
    }

    /// <summary>
    ///     Builds the aria-describedby value for the current validation and supporting-text state.
    /// </summary>
    /// <param name="hasErrorText">Whether the field has active error text.</param>
    /// <param name="hasSupportingText">Whether the field has active supporting text.</param>
    /// <returns>The aria-describedby value.</returns>
    protected override string? BuildDescribedBy(bool hasErrorText, bool hasSupportingText) {
        string? ids = null;
        if (!string.IsNullOrWhiteSpace(PrefixText)) {
            ids = AppendDescribedById(ids, PrefixId);
        }

        if (!string.IsNullOrWhiteSpace(SuffixText)) {
            ids = AppendDescribedById(ids, SuffixId);
        }

        if (hasErrorText) {
            ids = AppendDescribedById(ids, ErrorTextId);
        }
        else if (hasSupportingText) {
            ids = AppendDescribedById(ids, SupportingTextId);
        }

        return BuildAdditionalDescribedBy(ids) ?? string.Empty;
    }

    private bool HasAdditionalAttribute(string name) => TryGetAdditionalAttribute(name, out _);

    /// <summary>
    ///     Render state for the field trailing adornment.
    /// </summary>
    protected readonly record struct TrailingAdornmentState {
        /// <summary>
        ///     Gets the icon.
        /// </summary>
        public TnTIcon? Icon { get; init; }

        /// <summary>
        ///     Gets the CSS class.
        /// </summary>
        public string? Class { get; init; }

        /// <summary>
        ///     Gets the aria-label value.
        /// </summary>
        public string? AriaLabel { get; init; }

        /// <summary>
        ///     Gets the aria-hidden value.
        /// </summary>
        public string? AriaHidden { get; init; }
    }
}
