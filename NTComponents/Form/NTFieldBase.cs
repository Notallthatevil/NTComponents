using Microsoft.AspNetCore.Components;
using System.Text;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
///     Base class for Material 3 aligned Form fields.
/// </summary>
/// <remarks>
///     Derive field components from this type when they share the Material field shell: appearance, density, label,
///     supporting/error text, icons, color overrides, form inheritance, stable ids, and Blazor validation integration.
///     Concrete components remain responsible for rendering the native control and parsing its value.
/// </remarks>
/// <typeparam name="TValue">The field value type.</typeparam>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.ProgressivelyEnhanced,
    CompatibilitySummary = "Renders useful static HTML and adds Blazor behavior when interactive.",
    CompatibilityDetails = "Static SSR preserves the rendered markup and native browser behavior. EventCallback handlers, bound state updates, and live validation require an interactive render mode.")]
public abstract class NTFieldBase<TValue> : NTFormControlBase<TValue> {
    [ThreadStatic]
    private static StringBuilder? _cachedRootClassBuilder;

    private string? _elementStyle;
    private string? _prefixId;
    private string _rootClass = "nt-input";
    private string? _suffixId;

    /// <summary>
    ///     Gets or sets the field appearance. When null, the containing <see cref="NTForm" /> value is used.
    /// </summary>
    [Parameter]
    public NTFormAppearance? Appearance { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the filled field container color.
    /// </summary>
    [Parameter]
    public NTColor? BackgroundColor { get; set; }

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
    public NTColor? ActiveIndicatorColor { get; set; }

    /// <summary>
    ///     Gets or sets a callback invoked after binding.
    /// </summary>
    [Parameter]
    public EventCallback<TValue?> BindAfter { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the text cursor color.
    /// </summary>
    [Parameter]
    public NTColor? CaretColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the disabled filled container color.
    /// </summary>
    [Parameter]
    public NTColor? DisabledContainerColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for disabled content, label, icon, supporting text, and counter color.
    /// </summary>
    [Parameter]
    public NTColor? DisabledContentColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the disabled outline color.
    /// </summary>
    [Parameter]
    public NTColor? DisabledOutlineColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for error text, label, outline, indicator, and icon color.
    /// </summary>
    [Parameter]
    public NTColor? ErrorColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the text cursor color in the error state.
    /// </summary>
    [Parameter]
    public NTColor? ErrorCaretColor { get; set; }

    /// <summary>
    ///     Gets or sets the error icon shown when the field is invalid.
    /// </summary>
    [Parameter]
    public NTIcon? ErrorIcon { get; set; } = MaterialIcon.Error;

    /// <summary>
    ///     Gets or sets an optional override for the focus label, outline, active indicator, and default caret color.
    /// </summary>
    [Parameter]
    public NTColor? FocusColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the hovered filled active indicator color.
    /// </summary>
    [Parameter]
    public NTColor? HoverActiveIndicatorColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the hovered outline color.
    /// </summary>
    [Parameter]
    public NTColor? HoverOutlineColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for leading and trailing icon color.
    /// </summary>
    [Parameter]
    public NTColor? IconColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the resting label color.
    /// </summary>
    [Parameter]
    public NTColor? LabelColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the resting outlined border color.
    /// </summary>
    [Parameter]
    public NTColor? OutlineColor { get; set; }

    /// <summary>
    ///     Gets or sets placeholder text.
    /// </summary>
    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for visible placeholder text color.
    /// </summary>
    [Parameter]
    public NTColor? PlaceholderColor { get; set; }

    /// <summary>
    ///     Gets or sets prefix text shown before the editable value.
    /// </summary>
    [Parameter]
    public string? PrefixText { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for prefix and suffix text color.
    /// </summary>
    [Parameter]
    public NTColor? PrefixSuffixColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the hover state-layer color.
    /// </summary>
    [Parameter]
    public NTColor? StateLayerColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for supporting text and counter color.
    /// </summary>
    [Parameter]
    public NTColor? SupportingTextColor { get; set; }

    /// <summary>
    ///     Gets or sets suffix text shown after the editable value.
    /// </summary>
    [Parameter]
    public string? SuffixText { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for input text color.
    /// </summary>
    [Parameter]
    public NTColor? TextColor { get; set; }

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
    ///     Gets fallback placeholder text used when <see cref="Placeholder" /> is not supplied.
    /// </summary>
    protected virtual string? FallbackPlaceholder => null;

    /// <summary>
    ///     Gets a value indicating whether the native required attribute is present.
    /// </summary>
    protected bool IsRequired { get; private set; }

    /// <summary>
    ///     Gets the id for prefix text.
    /// </summary>
    protected string PrefixId => GetDerivedId(ref _prefixId, "-prefix");

    /// <summary>
    ///     Gets the id for suffix text.
    /// </summary>
    protected string SuffixId => GetDerivedId(ref _suffixId, "-suffix");

    /// <summary>
    ///     Gets inline CSS variable overrides for the field.
    /// </summary>
    protected string? ElementStyle => _elementStyle;

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
        IsRequired = TryGetAdditionalAttribute("required", out _);
        ControlAttributes = BuildFilteredAttributes(ExplicitControlAttributeNames, BuildAdditionalControlAttributes());
        _elementStyle = BuildElementStyle();
    }

    /// <inheritdoc />
    private protected override void ClearDerivedIds() {
        base.ClearDerivedIds();
        _prefixId = null;
        _suffixId = null;
    }

    /// <summary>
    ///     Gets the root CSS class with the current EditContext field state appended.
    /// </summary>
    /// <returns>The root CSS class.</returns>
    protected string GetRootClass() => AppendFieldCssClass(_rootClass);

    /// <summary>
    ///     Gets the root CSS class with an already resolved EditContext field class appended.
    /// </summary>
    private protected string GetRootClass(string? fieldCssClass) => AppendFieldCssClass(_rootClass, fieldCssClass);

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
        var placeholder = string.IsNullOrWhiteSpace(Placeholder) ? FallbackPlaceholder : Placeholder;
        EffectivePlaceholder = string.IsNullOrWhiteSpace(placeholder) ? " " : placeholder;
        _rootClass = BuildRootClass();
    }

    private string? BuildElementStyle() {
        var style = AppendColorVariable(null, "nt-input-active-indicator-color", ActiveIndicatorColor);
        style = AppendColorVariable(style, "nt-input-caret-color", CaretColor);
        style = AppendColorVariable(style, "nt-input-container-color", BackgroundColor);
        style = AppendColorVariable(style, "nt-input-disabled-container-color", DisabledContainerColor);
        style = AppendColorVariable(style, "nt-input-disabled-content-color", DisabledContentColor);
        style = AppendColorVariable(style, "nt-input-disabled-outline-color", DisabledOutlineColor);
        style = AppendColorVariable(style, "nt-input-error-caret-color", ErrorCaretColor);
        style = AppendColorVariable(style, "nt-input-error-color", ErrorColor);
        style = AppendColorVariable(style, "nt-input-focus-color", FocusColor);
        style = AppendColorVariable(style, "nt-input-hover-active-indicator-color", HoverActiveIndicatorColor);
        style = AppendColorVariable(style, "nt-input-hover-outline-color", HoverOutlineColor);
        style = AppendColorVariable(style, "nt-input-icon-color", IconColor);
        style = AppendColorVariable(style, "nt-input-label-color", LabelColor);
        style = AppendColorVariable(style, "nt-input-outline-color", OutlineColor);
        style = AppendColorVariable(style, "nt-input-placeholder-color", PlaceholderColor);
        style = AppendColorVariable(style, "nt-input-prefix-suffix-color", PrefixSuffixColor);
        style = AppendColorVariable(style, "nt-input-state-layer-color", StateLayerColor);
        style = AppendColorVariable(style, "nt-input-supporting-text-color", SupportingTextColor);
        return AppendColorVariable(style, "nt-input-text-color", TextColor);
    }

    private string BuildRootClass() {
        // Reuse one builder per thread, and keep the previous string when the classes are unchanged so field-class
        // composition can reuse its cached result.
        var className = _cachedRootClassBuilder ?? new StringBuilder(96);
        _cachedRootClassBuilder = null;
        className.Clear().Append("nt-input ");
        className.Append(EffectiveAppearance switch {
            NTFormAppearance.Filled => "nt-input-filled",
            NTFormAppearance.Outlined => "nt-input-outlined",
            _ => $"nt-input-{EffectiveAppearance.ToString().ToLowerInvariant()}"
        });
        className.Append(EffectiveDensity switch {
            NTFormDensity.Comfortable => " nt-input-comfortable",
            NTFormDensity.Standard => " nt-input-standard",
            NTFormDensity.Dense => " nt-input-dense",
            _ => $" nt-input-{EffectiveDensity.ToString().ToLowerInvariant()}"
        });

        if (FieldDisabled) {
            className.Append(" nt-input-disabled");
        }

        if (FieldReadOnly) {
            className.Append(" nt-input-readonly");
        }

        if (!HasLabel) {
            className.Append(" nt-input-no-label");
        }

        if (!string.IsNullOrWhiteSpace(EffectivePlaceholder)) {
            className.Append(" nt-input-has-placeholder");
        }

        if (HasFloatingValue) {
            className.Append(" nt-input-has-value");
        }

        BuildAdditionalRootClasses(className);
        var rootClass = className.Equals(_rootClass.AsSpan()) ? _rootClass : className.ToString();
        _cachedRootClassBuilder = className;
        return rootClass;
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

    /// <summary>
    ///     Render state for the field trailing adornment.
    /// </summary>
    protected readonly record struct TrailingAdornmentState {
        /// <summary>
        ///     Gets the icon.
        /// </summary>
        public NTIcon? Icon { get; init; }

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
