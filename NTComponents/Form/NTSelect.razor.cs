using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
///     A Material 3 aligned native single-select field.
/// </summary>
/// <remarks>
///     Use <see cref="NTSelect{TValue}" /> when users choose one value from a finite set and native browser select
///     behavior is appropriate. The component intentionally renders a real HTML <c>select</c> and accepts normal
///     <c>option</c> and <c>optgroup</c> child content. It does not support multi-select, search, freeform values, or rich
///     option templates; use a future combobox/autocomplete component for those behaviors.
///     <para>
///         Best practices:
///     </para>
///     <list type="bullet">
///         <item>Use this component for a single, compact choice where native browser behavior is acceptable and predictable.</item>
///         <item>Use <see cref="NTCombobox{TValue}" /> when users need to select multiple values, filter a richer list, or toggle options without native multi-select gestures.</item>
///         <item>Keep option text short and place related choices in native <c>optgroup</c> elements when grouping improves scanning.</item>
///         <item>Provide a visible label whenever possible. If a label is intentionally hidden, provide an <c>aria-label</c> through additional attributes.</item>
///         <item>Do not pass a <c>multiple</c> attribute. The component throws because multi-select behavior needs a different interaction model.</item>
///         <item>Do not use rich markup or nested controls inside options. Browser support for option markup is inconsistent and harms accessibility.</item>
///     </list>
///     <para>
///         <typeparamref name="TValue" /> is intentionally constrained at runtime to string and value types. Match each
///         <c>option</c> value to the bound type: strings bind directly, bool values should use <c>true</c> or
///         <c>false</c>, enum values should use the enum member name, and other value types should use a parseable text
///         representation.
///     </para>
///     <para>
///         Because this is a native select, it works well with static SSR and normal form posts. When readonly, the native
///         select is disabled and the component emits a hidden value so form posts keep the current value.
///     </para>
/// </remarks>
/// <typeparam name="TValue">The selected value type.</typeparam>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.SsrCompatible,
    CompatibilitySummary = "Renders a native select that works with static SSR and form posts.",
    CompatibilityDetails = "The component intentionally uses a real HTML select and native option content. Blazor binding callbacks and live validation updates still require interactivity or form post handling.")]
public partial class NTSelect<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TValue> {
    private static readonly Type NullableValueType = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);

    private static readonly HashSet<string> SelectExplicitControlAttributeNames = new(StringComparer.OrdinalIgnoreCase) {
        "id",
        "name",
        "title",
        "autofocus",
        "autocomplete",
        "disabled",
        "required",
        "multiple",
        "aria-describedby",
        "aria-invalid",
        "aria-errormessage",
        "value",
        "onchange"
    };

    /// <summary>
    ///     Gets or sets the native <c>option</c> and <c>optgroup</c> content rendered inside the select.
    /// </summary>
    /// <remarks>
    ///     Render only native <c>option</c> and <c>optgroup</c> elements. Include an empty option for nullable values when the
    ///     user should be able to clear the selection. Keep option values stable across renders so validation and form posts
    ///     remain predictable.
    /// </remarks>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <inheritdoc />
    protected override IEnumerable<string> ExplicitControlAttributeNames => SelectExplicitControlAttributeNames;

    /// <inheritdoc />
    protected override string InputIdPrefix => "nt-select";

    /// <inheritdoc />
    protected override bool HasFloatingValue => !string.IsNullOrEmpty(CurrentValueAsString);

    private static bool AllowsEmptyValue => !typeof(TValue).IsValueType || Nullable.GetUnderlyingType(typeof(TValue)) is not null;

    private static bool IsSupportedValueType => NullableValueType == typeof(string) || NullableValueType.IsValueType;

    private string SelectClass {
        get {
            var cssClass = CssClass;
            return string.IsNullOrEmpty(cssClass) ? "nt-input-control nt-select-control" : $"nt-input-control nt-select-control {cssClass}";
        }
    }

    private bool ShouldRenderReadOnlyFormPostValue => !FieldDisabled && FieldReadOnly && !string.IsNullOrWhiteSpace(ElementName);

    /// <inheritdoc />
    protected override string? FormatValueAsString(TValue? value) => BindConverter.FormatValue(value)?.ToString();

    /// <inheritdoc />
    protected override void OnParametersSet() {
        if (TryGetAdditionalAttribute("multiple", out _)) {
            throw new InvalidOperationException($"{nameof(NTSelect<TValue>)} does not support multi-select. Use {typeof(NTCombobox<TValue>).Name} instead.");
        }

        if (!IsSupportedValueType) {
            throw new InvalidOperationException($"{nameof(NTSelect<TValue>)} supports string and value types. The type '{typeof(TValue)}' is not supported.");
        }

        base.OnParametersSet();
    }

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out TValue result, [NotNullWhen(false)] out string? validationErrorMessage) {
        if (NullableValueType == typeof(bool)) {
            if (TryConvertToBool(value, out result)) {
                validationErrorMessage = null;
                return true;
            }
        }
        else if (NullableValueType.IsEnum) {
            if (TryConvertToEnum(value, out result)) {
                validationErrorMessage = null;
                return true;
            }
        }
        else if (BindConverter.TryConvertTo(value, CultureInfo.CurrentCulture, out result)) {
            validationErrorMessage = null;
            return true;
        }

        result = default;
        validationErrorMessage = $"The {DisplayName ?? FieldIdentifier.FieldName} field is not valid.";
        return false;
    }

    /// <inheritdoc />
    protected override void BuildAdditionalRootClasses(StringBuilder builder) {
        builder.Append(" nt-select");
    }

    /// <inheritdoc />
    protected override TrailingAdornmentState CreateTrailingAdornmentState(bool hasErrorText) {
        if (hasErrorText) {
            return base.CreateTrailingAdornmentState(hasErrorText);
        }

        return new TrailingAdornmentState {
            Icon = TrailingIcon ?? MaterialIcon.ArrowDropDown,
            Class = "nt-input-trailing nt-select-indicator",
            AriaHidden = "true"
        };
    }

    private async Task OnChangeAsync(ChangeEventArgs args) {
        if (FieldReadOnly || FieldDisabled) {
            return;
        }

        CurrentValueAsString = args.Value?.ToString();
        await BindAfter.InvokeAsync(CurrentValue);
    }

    private static bool TryConvertToBool<T>(string? value, out T result) {
        if (string.IsNullOrEmpty(value) && AllowsEmptyValue) {
            result = default!;
            return true;
        }

        if (bool.TryParse(value, out var boolValue)) {
            result = (T)(object)boolValue;
            return true;
        }

        result = default!;
        return false;
    }

    private static bool TryConvertToEnum<T>(string? value, out T result) {
        if (string.IsNullOrEmpty(value) && AllowsEmptyValue) {
            result = default!;
            return true;
        }

        if (!string.IsNullOrEmpty(value) && Enum.TryParse(NullableValueType, value, ignoreCase: true, out var enumValue)) {
            result = (T)enumValue;
            return true;
        }

        result = default!;
        return false;
    }
}
