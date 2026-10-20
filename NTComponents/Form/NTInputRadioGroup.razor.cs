using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
///     A Material 3 radio-button group for selecting one value from a set of options.
/// </summary>
/// <remarks>
///     Use radio buttons when options are mutually exclusive and visible at the same time. Keep option labels short and
///     scannable, prefer a group label, and use supporting or validation text for brief context. The component renders
///     native radio inputs so static SSR form posts work without hydration; the shared keyboard enhancer adds number-key
///     selection and arrow-key highlighting when JavaScript is available.
/// </remarks>
/// <typeparam name="TValue">The selected option value type.</typeparam>
[CascadingTypeParameter(nameof(TValue))]
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.SsrCompatible,
    CompatibilitySummary = "Renders native form markup that works with static SSR and form posts.",
    CompatibilityDetails = "The native control can participate in static SSR and normal form posts. Blazor binding callbacks and live validation updates require interactivity or a subsequent render.")]
public partial class NTInputRadioGroup<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TValue> {
    private static readonly HashSet<string> GroupExplicitAttributeNames = new(StringComparer.OrdinalIgnoreCase) {
        "id",
        "class",
        "title",
        "lang",
        "disabled",
        "role",
        "aria-labelledby",
        "aria-describedby",
        "aria-invalid",
        "aria-errormessage",
        "aria-required",
        "tabindex",
        "onkeydown"
    };

    private readonly List<NTInputRadio<TValue>> _registeredRadios = [];
    private string? _elementStyle;
    private string? _rootClass;
    private string? _rootClassAdditionalClass;
    private (NTFormAppearance, NTFormDensity, int) _rootClassKey;

    /// <summary>
    ///     Gets or sets the radio options.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    ///     Gets or sets the group appearance. When null, the containing <see cref="NTForm" /> value is used.
    /// </summary>
    [Parameter]
    public NTFormAppearance? Appearance { get; set; }

    /// <summary>
    ///     Gets or sets an optional explicit native name shared by all radios in this group.
    /// </summary>
    [Parameter]
    public string? GroupName { get; set; }

    /// <summary>
    ///     Gets or sets a callback invoked after the selected value changes.
    /// </summary>
    [Parameter]
    public EventCallback<TValue?> BindAfter { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the filled group container color.
    /// </summary>
    [Parameter]
    public NTColor? BackgroundColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the resting filled active indicator color.
    /// </summary>
    [Parameter]
    public NTColor? ActiveIndicatorColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for disabled content and radio color.
    /// </summary>
    [Parameter]
    public NTColor? DisabledContentColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for disabled filled container color.
    /// </summary>
    [Parameter]
    public NTColor? DisabledContainerColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for disabled outline color.
    /// </summary>
    [Parameter]
    public NTColor? DisabledOutlineColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for error text, outline, indicator, and radio color.
    /// </summary>
    [Parameter]
    public NTColor? ErrorColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for focused outline, active indicator, and selected radio color.
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
    ///     Gets or sets an optional override for label text color.
    /// </summary>
    [Parameter]
    public NTColor? LabelColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the resting outlined border color.
    /// </summary>
    [Parameter]
    public NTColor? OutlineColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for selected radio color.
    /// </summary>
    [Parameter]
    public NTColor? SelectedColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the selected radio state-layer color.
    /// </summary>
    [Parameter]
    public NTColor? SelectedStateLayerColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for the unselected radio state-layer color.
    /// </summary>
    [Parameter]
    public NTColor? StateLayerColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for supporting text color.
    /// </summary>
    [Parameter]
    public NTColor? SupportingTextColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for unselected radio color.
    /// </summary>
    [Parameter]
    public NTColor? UnselectedColor { get; set; }

    /// <summary>
    ///     Gets the current effective appearance.
    /// </summary>
    internal NTFormAppearance EffectiveAppearance { get; private set; } = NTFormAppearance.Outlined;

    /// <summary>
    ///     Gets filtered additional attributes for the group fieldset.
    /// </summary>
    protected IReadOnlyDictionary<string, object?>? GroupAttributes { get; private set; }

    /// <summary>
    ///     Gets a value indicating whether native radios should render the required attribute.
    /// </summary>
    internal bool IsRequired { get; private set; }

    /// <summary>
    ///     Gets a value indicating whether the group is disabled.
    /// </summary>
    internal bool IsGroupDisabled => FieldDisabled;

    /// <summary>
    ///     Gets a value indicating whether the group is read-only.
    /// </summary>
    internal bool IsGroupReadOnly => FieldReadOnly;

    /// <summary>
    ///     Gets a value indicating whether the group is using standard density.
    /// </summary>
    internal bool IsStandard => EffectiveDensity == NTFormDensity.Standard;

    /// <summary>
    ///     Gets a value indicating whether the group is using dense density.
    /// </summary>
    internal bool IsDense => EffectiveDensity == NTFormDensity.Dense;

    /// <summary>
    ///     Gets the id prefix for generated radio option ids.
    /// </summary>
    internal string RadioIdBase => InputId;

    /// <summary>
    ///     Gets the HTML name shared by the radios in this group.
    /// </summary>
    internal string? RadioName => string.IsNullOrWhiteSpace(GroupName) ? ElementName : GroupName;

    /// <inheritdoc />
    protected override string InputIdPrefix => "nt-radio";

    /// <inheritdoc />
    protected override bool HasRequiredSupportingText => IsRequired;

    /// <inheritdoc />
    public void Dispose() => ((IDisposable)this).Dispose();

    /// <inheritdoc />
    protected override void Dispose(bool disposing) {
        if (disposing) {
            _registeredRadios.Clear();
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc />
    public override async ValueTask SetFocusAsync() {
        var radio = _registeredRadios.FirstOrDefault(IsSelected) ?? _registeredRadios.FirstOrDefault();
        if (radio is null) {
            await base.SetFocusAsync();
            return;
        }

        await radio.SetFocusAsync();
    }

    /// <summary>
    ///     Gets the zero-based registration index used to build a radio option's deterministic input id.
    /// </summary>
    internal int GetRadioIndex(NTInputRadio<TValue> radio) => Math.Max(0, _registeredRadios.IndexOf(radio));

    /// <summary>
    ///     Gets whether a radio option is selected.
    /// </summary>
    internal bool IsSelected(NTInputRadio<TValue> radio) => EqualityComparer<TValue>.Default.Equals(CurrentValue, radio.Value);

    /// <summary>
    ///     Registers a child radio.
    /// </summary>
    internal void RegisterRadio(NTInputRadio<TValue> radio) {
        if (!_registeredRadios.Contains(radio)) {
            _registeredRadios.Add(radio);
        }
    }

    /// <summary>
    ///     Selects a child radio value from its native string value.
    /// </summary>
    internal async Task SelectValueFromStringAsync(string? value) {
        CurrentValueAsString = value;
        await InvokeAsync(StateHasChanged);
        await BindAfter.InvokeAsync(CurrentValue);
    }

    /// <summary>
    ///     Unregisters a child radio.
    /// </summary>
    internal void UnregisterRadio(NTInputRadio<TValue> radio) {
        _registeredRadios.Remove(radio);
    }

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();
        EffectiveAppearance = Appearance ?? Form?.Appearance ?? NTFormAppearance.Outlined;
        IsRequired = TryGetAdditionalAttribute("required", out _);
        GroupAttributes = BuildFilteredAttributes(GroupExplicitAttributeNames);
        _elementStyle = BuildElementStyle();
    }

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out TValue result, [NotNullWhen(false)] out string? validationErrorMessage) {
        try {
            if (typeof(TValue) == typeof(bool)) {
                if (TryConvertToBool(value, out result)) {
                    validationErrorMessage = null;
                    return true;
                }
            }
            else if (typeof(TValue) == typeof(bool?)) {
                if (TryConvertToNullableBool(value, out result)) {
                    validationErrorMessage = null;
                    return true;
                }
            }
            else if (BindConverter.TryConvertTo<TValue>(value, CultureInfo.CurrentCulture, out var parsedValue)) {
                result = parsedValue;
                validationErrorMessage = null;
                return true;
            }

            result = default;
            validationErrorMessage = $"The {DisplayName ?? FieldIdentifier.FieldName} field is not valid.";
            return false;
        }
        catch (InvalidOperationException ex) {
            throw new InvalidOperationException($"{GetType()} does not support the type '{typeof(TValue)}'.", ex);
        }
    }

    private string BuildRootClass() {
        var additionalClass = GetAdditionalAttributeString("class");
        var flags = (FieldDisabled ? 1 : 0) | (FieldReadOnly ? 2 : 0) | (HasLabel ? 0 : 4);
        // The class string only changes with these inputs, so reuse it across renders.
        if (_rootClass is null || _rootClassKey != (EffectiveAppearance, EffectiveDensity, flags) || !ReferenceEquals(additionalClass, _rootClassAdditionalClass)) {
            _rootClass = string.Concat(
                "nt-radio ",
                EffectiveAppearance switch {
                    NTFormAppearance.Filled => "nt-radio-filled",
                    NTFormAppearance.Outlined => "nt-radio-outlined",
                    _ => $"nt-radio-{EffectiveAppearance.ToString().ToLowerInvariant()}"
                },
                EffectiveDensity switch {
                    NTFormDensity.Comfortable => " nt-radio-comfortable",
                    NTFormDensity.Dense => " nt-radio-dense",
                    _ => " nt-radio-standard"
                },
                FieldDisabled ? " nt-radio-disabled" : null,
                FieldReadOnly ? " nt-radio-readonly" : null,
                HasLabel ? null : " nt-radio-no-label",
                additionalClass is null ? null : " " + additionalClass);
            _rootClassKey = (EffectiveAppearance, EffectiveDensity, flags);
            _rootClassAdditionalClass = additionalClass;
        }

        return AppendFieldCssClass(_rootClass);
    }

    private string? BuildElementStyle() {
        var style = AppendColorVariable(null, "nt-radio-active-indicator-color", ActiveIndicatorColor);
        style = AppendColorVariable(style, "nt-radio-container-color", BackgroundColor);
        style = AppendColorVariable(style, "nt-radio-disabled-container-color", DisabledContainerColor);
        style = AppendColorVariable(style, "nt-radio-disabled-content-color", DisabledContentColor);
        style = AppendColorVariable(style, "nt-radio-disabled-outline-color", DisabledOutlineColor);
        style = AppendColorVariable(style, "nt-radio-error-color", ErrorColor);
        style = AppendColorVariable(style, "nt-radio-focus-color", FocusColor);
        style = AppendColorVariable(style, "nt-radio-hover-active-indicator-color", HoverActiveIndicatorColor);
        style = AppendColorVariable(style, "nt-radio-hover-outline-color", HoverOutlineColor);
        style = AppendColorVariable(style, "nt-radio-label-color", LabelColor);
        style = AppendColorVariable(style, "nt-radio-outline-color", OutlineColor);
        style = AppendColorVariable(style, "nt-radio-selected-color", SelectedColor);
        style = AppendColorVariable(style, "nt-radio-selected-state-layer-color", SelectedStateLayerColor);
        style = AppendColorVariable(style, "nt-radio-state-layer-color", StateLayerColor);
        style = AppendColorVariable(style, "nt-radio-supporting-text-color", SupportingTextColor);
        return AppendColorVariable(style, "nt-radio-unselected-color", UnselectedColor);
    }

    private static bool TryConvertToBool<T>(string? value, out T result) {
        if (bool.TryParse(value, out var boolValue)) {
            result = (T)(object)boolValue;
            return true;
        }

        result = default!;
        return false;
    }

    private static bool TryConvertToNullableBool<T>(string? value, out T result) {
        if (string.IsNullOrEmpty(value)) {
            result = default!;
            return true;
        }

        return TryConvertToBool(value, out result);
    }
}
