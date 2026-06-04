using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
///     Base class for Material 3 aligned NT text-entry components.
/// </summary>
/// <remarks>
///     Derive text-field components from <see cref="NTInputBase{TValue}" /> when the rendered control should follow the
///     Material 3 text-field model and use a native text-entry control such as <c>input</c> or <c>textarea</c>.
///     Components that render other controls, such as <c>select</c>, should derive from <see cref="NTFieldBase{TValue}" />
///     directly.
/// </remarks>
/// <typeparam name="TValue">The input value type.</typeparam>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.ProgressivelyEnhanced,
    CompatibilitySummary = "Renders native text-field input structure for static SSR.",
    CompatibilityDetails = "Derived inputs emit named native controls, labels, and supporting text. Live binding, validation, and browser-specific enhancements require interactivity or form post handling.")]
public abstract partial class NTInputBase<TValue> : NTFieldBase<TValue> {
    private static readonly HashSet<string> InputExplicitControlAttributeNames = new(StringComparer.OrdinalIgnoreCase) {
        "id",
        "name",
        "type",
        "title",
        "autofocus",
        "autocomplete",
        "readonly",
        "disabled",
        "required",
        "minlength",
        "maxlength",
        "min",
        "max",
        "placeholder",
        "aria-describedby",
        "aria-invalid",
        "aria-errormessage",
        "oninput",
        "value"
    };

    private const string InputControlClass = "nt-input-control";

    private int? _maxLength;
    private string? _maxValue;
    private int? _minLength;
    private string? _minValue;

    /// <summary>
    ///     Gets or sets a value indicating whether binding should happen on input instead of change.
    /// </summary>
    /// <remarks>
    ///     Enable this for live previews, live filtering, or real-time validation. Leave it disabled for ordinary forms so
    ///     each keystroke does not force model updates and validation work.
    /// </remarks>
    [Parameter]
    public bool BindOnInput { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the character counter is shown when a maximum length exists.
    /// </summary>
    /// <remarks>
    ///     Keep this enabled when the field has a native <c>maxlength</c> attribute so users can see how much input remains.
    /// </remarks>
    [Parameter]
    public bool ShowCharacterCounter { get; set; } = true;

    /// <summary>
    ///     Gets the native input type.
    /// </summary>
    protected virtual InputType InputTypeAttribute => InputType.Text;

    /// <inheritdoc />
    protected override IEnumerable<string> ExplicitControlAttributeNames => InputExplicitControlAttributeNames;

    /// <summary>
    ///     Gets the accessible label for the character counter.
    /// </summary>
    private protected string CounterAriaLabel => $"Character count {CounterText}";

    /// <summary>
    ///     Gets the character counter id.
    /// </summary>
    private protected string CounterId => $"{InputId}-counter";

    /// <summary>
    ///     Gets the character counter display text.
    /// </summary>
    private protected string CounterText => $"{CurrentValueAsString?.Length ?? 0}/{_maxLength}";

    /// <summary>
    ///     Gets the hidden form-post input id.
    /// </summary>
    private protected string FormPostInputId => $"{InputId}-form-value";

    /// <summary>
    ///     Gets the hidden form-post input value.
    /// </summary>
    private protected string? FormPostValue => FormatFormPostValue();

    /// <summary>
    ///     Gets the native control CSS class.
    /// </summary>
    private protected string InputClass {
        get {
            var cssClass = CssClass;
            return string.IsNullOrEmpty(cssClass) ? InputControlClass : $"{InputControlClass} {cssClass}";
        }
    }

    /// <summary>
    ///     Gets the native visible control name.
    /// </summary>
    private protected string? InputElementName => UsesSeparateFormPostValue ? null : ElementName;

    /// <summary>
    ///     Gets the composed native input handler.
    /// </summary>
    private protected string? InputNativeOnInputHandler => BuildInputNativeOnInputHandler();

    /// <summary>
    ///     Gets a value indicating whether binding should happen on input for this field.
    /// </summary>
    private protected bool EffectiveBindOnInput => BindOnInput || Form?.BindOnInput == true;

    /// <summary>
    ///     Gets a value indicating whether the character counter should render.
    /// </summary>
    private protected bool ShowCounter => ShowCharacterCounter && _maxLength is > 0;

    /// <summary>
    ///     Gets a value indicating whether a hidden form-post input should render.
    /// </summary>
    private protected bool ShouldRenderFormPostValue => UsesSeparateFormPostValue
        && !FieldDisabled
        && !string.IsNullOrWhiteSpace(ElementName);

    /// <inheritdoc />
    protected override bool HasSupportingRowContent => ShowCounter;

    /// <summary>
    ///     Builds additional native input attributes supplied by derived input types.
    /// </summary>
    /// <returns>The additional attributes to merge onto the native input element.</returns>
    protected virtual IReadOnlyDictionary<string, object?>? BuildAdditionalInputAttributes() => null;

    /// <summary>
    ///     Gets a native JavaScript input handler for behavior that must run before Blazor receives the input event.
    /// </summary>
    protected virtual string? NativeOnInputHandler => null;

    /// <summary>
    ///     Gets a value indicating whether the visible input should post through a separate hidden value input.
    /// </summary>
    protected virtual bool UsesSeparateFormPostValue => false;

    /// <summary>
    ///     Formats the value submitted by the hidden form-post input when <see cref="UsesSeparateFormPostValue" /> is true.
    /// </summary>
    /// <returns>The form-post value.</returns>
    protected virtual string? FormatFormPostValue() => CurrentValueAsString;

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object?>? BuildAdditionalControlAttributes() => BuildAdditionalInputAttributes();

    /// <inheritdoc />
    protected override string? BuildAdditionalDescribedBy(string? current) => ShowCounter ? AppendDescribedById(current, CounterId) : current;

    /// <summary>
    ///     Handles the native blur event for the input element.
    /// </summary>
    /// <param name="args">The focus event arguments.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected override async Task OnBlurAsync(FocusEventArgs args) {
        await base.OnBlurAsync(args);
        if (!EffectiveBindOnInput) {
            await BindAfter.InvokeAsync(CurrentValue);
        }
    }

    /// <summary>
    ///     Handles input value changes before optional binding callbacks run.
    /// </summary>
    /// <param name="newValue">The new text value.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected virtual async Task OnInputAsync(string? newValue) {
        CurrentValueAsString = newValue;
        if (EffectiveBindOnInput) {
            await BindAfter.InvokeAsync(CurrentValue);
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet() {
        CacheNativeMetadata();
        base.OnParametersSet();
    }

    private string? BuildInputNativeOnInputHandler() {
        var nativeHandler = NativeOnInputHandler;
        if (!ShowCounter) {
            return nativeHandler;
        }

        const string CounterHandler = "window.NTComponents?.updateInputCounter?.(this)";
        if (string.IsNullOrWhiteSpace(nativeHandler)) {
            return CounterHandler;
        }

        return $"{nativeHandler}; {CounterHandler}";
    }

    private void CacheNativeMetadata() {
        _maxLength = TryParseAdditionalAttributeInteger("maxlength");
        _minLength = TryParseAdditionalAttributeInteger("minlength");
        _maxValue = TryGetAdditionalAttribute("max", out var maxAttributeValue) ? maxAttributeValue?.ToString() : null;
        _minValue = TryGetAdditionalAttribute("min", out var minAttributeValue) ? minAttributeValue?.ToString() : null;
    }

    private int? TryParseAdditionalAttributeInteger(string name) => TryGetAdditionalAttribute(name, out var value) ? TryParseInteger(value) : null;

    private static int? TryParseInteger(object? value) => int.TryParse(value?.ToString(), out var result) ? result : null;
}
