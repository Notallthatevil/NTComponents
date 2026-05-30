using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using NTComponents.Ext;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Encodings.Web;

namespace NTComponents;

/// <summary>
///     A Material 3 aligned autocomplete text field.
/// </summary>
/// <remarks>
///     Use <see cref="NTAutocomplete" /> when users enter a text value and can benefit from suggestions. The bound and submitted value is the native input value, and TypeScript owns the suggestion
///     popover interaction. JavaScript enhances the input with a Material-style menu from inert static SSR option metadata and filtering suggestions in the browser by contains matching while
///     prioritizing starts-with matches. Suggestions are supplied from the local <see cref="Items" /> list.
///     <para>Best practices:</para>
///     <list type="bullet">
///         <item>Use this component for text entry with suggestions. Use <see cref="NTSelect{TValue}" /> when users must choose exactly one known value.</item>
///         <item>Use <see cref="NTCombobox{TValue}" /> when users need to select multiple values.</item>
///         <item>Keep <see cref="Items" /> bounded and already loaded on the client.</item>
///         <item>Set <see cref="AllowCustomValue" /> to <see langword="false" /> when the submitted value must match a listed suggestion. The component renders a native pattern constraint for form posts.</item>
///         <item>Keep suggestion labels concise. Use supporting text only when it helps distinguish similar suggestions.</item>
///         <item>Keep suggestion values stable because the selected value is posted directly through the native input.</item>
///         <item>Use this component where the TypeScript enhancement is available. It works with static SSR and does not require interactive server or WebAssembly render modes.</item>
///         <item>Provide a visible label whenever possible. If a label is intentionally hidden, provide an <c>aria-label</c> through additional attributes.</item>
///     </list>
/// </remarks>
public partial class NTAutocomplete : IAsyncDisposable {
    private const string AutocompleteControlBaseClass = "nt-input-control nt-autocomplete-control";
    private const string JsModulePath = "./_content/NTComponents/FormV2/NTAutocomplete.razor.js";
    private const string PatternSpecialCharacters = @"\^$.|?*+()[]{}-/";
    private static readonly HashSet<string> AutocompleteExplicitControlAttributeNames = new(StringComparer.OrdinalIgnoreCase) {
        "id",
        "name",
        "type",
        "title",
        "autofocus",
        "autocomplete",
        "readonly",
        "disabled",
        "required",
        "placeholder",
        "aria-autocomplete",
        "aria-controls",
        "aria-describedby",
        "aria-expanded",
        "aria-haspopup",
        "aria-invalid",
        "aria-errormessage",
        "aria-activedescendant",
        "role",
        "value",
        "onchange",
        "oninput"
    };

    private DotNetObjectReference<NTAutocomplete>? _dotNetObjectRef;
    private IReadOnlyList<NTAutocompleteOption> _items = [];
    private IJSObjectReference? _jsModule;

    /// <summary>
    ///     Gets or sets a value indicating whether users can submit values that are not in <see cref="Items" />.
    /// </summary>
    /// <remarks>
    ///     When this is <see langword="false" />, the component rejects non-empty values that do not match an enabled option value exactly. The enhanced menu also hides the explicit custom-value row.
    /// </remarks>
    [Parameter]
    public bool AllowCustomValue { get; set; } = true;

    /// <summary>
    ///     Gets or sets the display format for the custom-value option. Use <c>{0}</c> for the typed value.
    /// </summary>
    [Parameter]
    public string CustomValueOptionFormat { get; set; } = "Use \"{0}\"";

    /// <summary>
    ///     Gets or sets the text rendered when filtering produces no suggestions.
    /// </summary>
    [Parameter]
    public string EmptyText { get; set; } = "No options";

    /// <summary>
    ///     Gets or sets the available local suggestions.
    /// </summary>
    /// <remarks>
    ///     Provide a bounded list that is already available in memory. Items are rendered into inert JSON metadata during SSR and converted into live menu rows by TypeScript only while the popover is open.
    /// </remarks>
    [Parameter]
    public IReadOnlyList<NTAutocompleteOption> Items { get; set; } = [];

    /// <summary>
    ///     Gets or sets the popup menu item appearance.
    /// </summary>
    /// <remarks>
    ///     Use <see cref="NTMenuItemAppearance.Condensed" /> only for compact, highly scannable suggestion lists. Condensed
    ///     menu items are capped at 38 CSS pixels and are best suited to short labels without supporting text.
    /// </remarks>
    [Parameter]
    public NTMenuItemAppearance MenuItemAppearance { get; set; }

    /// <inheritdoc />
    protected override IEnumerable<string> ExplicitControlAttributeNames => AutocompleteExplicitControlAttributeNames;

    /// <inheritdoc />
    protected override bool HasFloatingValue => !string.IsNullOrEmpty(CurrentValueAsString);

    /// <inheritdoc />
    protected override string InputIdPrefix => "nt-autocomplete";

    private string AutocompleteControlClass {
        get {
            var cssClass = CssClass;
            return string.IsNullOrEmpty(cssClass) ? AutocompleteControlBaseClass : $"{AutocompleteControlBaseClass} {cssClass}";
        }
    }

    private string EffectiveCustomValueOptionFormat => CustomValueOptionFormat ?? "Use \"{0}\"";
    private IReadOnlyList<NTAutocompleteOption> EffectiveItems => _items;
    private string AutocompleteOptionsJson => BuildAutocompleteOptionsJson(ListboxId, _items, CurrentValue, AllowCustomValue, EffectiveCustomValueOptionFormat, FieldDisabled || FieldReadOnly);

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    private string ListboxId => $"{InputId}-listbox";
    private string ListboxLabel => string.IsNullOrWhiteSpace(Label) ? FieldIdentifier.FieldName : Label;

    /// <inheritdoc />
    public async ValueTask DisposeAsync() {
        try {
            if (_jsModule is not null) {
                try {
                    await _jsModule.InvokeVoidAsync("onDispose", Element, _dotNetObjectRef).ConfigureAwait(false);
                    await _jsModule.DisposeAsync().ConfigureAwait(false);
                }
                catch (JSDisconnectedException) {
                    // JS runtime was disconnected, safe to ignore during disposal.
                }
            }
        }
        finally {
            _jsModule = null;
            _dotNetObjectRef?.Dispose();
            _dotNetObjectRef = null;
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    ///     Receives blur/touched notifications from the browser module.
    /// </summary>
    /// <returns>A task representing the update.</returns>
    [JSInvokable]
    public async Task NotifyAutocompleteTouched() {
        await OnBlurAsync(new Microsoft.AspNetCore.Components.Web.FocusEventArgs());
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    ///     Receives the selected or typed value from the browser module.
    /// </summary>
    /// <param name="value">The current input value.</param>
    /// <param name="closeMenu">Whether the menu should be closed after the update.</param>
    /// <returns>A task representing the update.</returns>
    [JSInvokable]
    public async Task NotifyAutocompleteValueChanged(string? value, bool closeMenu) {
        if (!CanUpdateCurrentValue()) {
            return;
        }

        await UpdateCurrentValueAsync(value);
        await InvokeAsync(StateHasChanged);
    }

    /// <inheritdoc />
    protected override void BuildAdditionalRootClasses(StringBuilder builder) {
        builder.Append(" nt-autocomplete");
        if (MenuItemAppearance == NTMenuItemAppearance.Condensed) {
            builder.Append(" nt-autocomplete-menu-items-condensed");
        }
    }

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object?>? BuildAdditionalControlAttributes() {
        if (AllowCustomValue) {
            return null;
        }

        return new Dictionary<string, object?> {
            ["pattern"] = BuildAllowedValuesPattern(_items)
        };
    }

    /// <inheritdoc />
    protected override TrailingAdornmentState CreateTrailingAdornmentState(bool hasErrorText) {
        if (hasErrorText) {
            return base.CreateTrailingAdornmentState(hasErrorText);
        }

        return new TrailingAdornmentState {
            Icon = TrailingIcon ?? MaterialIcon.ArrowDropDown,
            Class = "nt-input-trailing nt-autocomplete-indicator",
            AriaHidden = "true"
        };
    }

    /// <inheritdoc />
    protected override string? FormatValueAsString(string? value) => value;

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender) {
        await base.OnAfterRenderAsync(firstRender);

        try {
            _dotNetObjectRef ??= DotNetObjectReference.Create(this);
            if (firstRender) {
                _jsModule = await JSRuntime.ImportIsolatedJs(this, JsModulePath);
                await _jsModule.InvokeVoidAsync("onLoad", Element, _dotNetObjectRef);
            }
            else if (_jsModule is not null) {
                await _jsModule.InvokeVoidAsync("onUpdate", Element, _dotNetObjectRef);
            }
        }
        catch (JSDisconnectedException) {
            // JS runtime was disconnected, safe to ignore during render.
        }
        catch (JSException) {
            // Enhancement failed. Keep the native input usable instead of failing the interactive circuit.
            _jsModule = null;
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet() {
        _items = Items ?? [];
        base.OnParametersSet();
    }

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out string? result, [NotNullWhen(false)] out string? validationErrorMessage) {
        if (!AllowCustomValue && !string.IsNullOrEmpty(value) && !_items.Any(option => !option.Disabled && string.Equals(option.Value, value, StringComparison.Ordinal))) {
            result = null;
            validationErrorMessage = $"The {DisplayName ?? FieldIdentifier.FieldName} field must match one of the available options.";
            return false;
        }

        result = value;
        validationErrorMessage = null;
        return true;
    }

    private static string FormatCustomValueOptionText(string format, string value) {
        var placeholderIndex = format.IndexOf("{0}", StringComparison.Ordinal);
        if (placeholderIndex < 0) {
            return format;
        }

        return string.Concat(format.AsSpan(0, placeholderIndex), value, format.AsSpan(placeholderIndex + 3));
    }

    private static string BuildAllowedValuesPattern(IReadOnlyList<NTAutocompleteOption> items) {
        var enabledValues = items
            .Where(option => !option.Disabled)
            .Select(option => option.Value)
            .ToArray();

        if (enabledValues.Length == 0) {
            return "a^";
        }

        var builder = new StringBuilder();
        builder.Append("(?:");
        for (var index = 0; index < enabledValues.Length; index++) {
            if (index > 0) {
                builder.Append('|');
            }

            AppendEscapedPatternValue(builder, enabledValues[index]);
        }

        builder.Append(')');
        return builder.ToString();
    }

    private static void AppendEscapedPatternValue(StringBuilder builder, string value) {
        foreach (var character in value) {
            if (PatternSpecialCharacters.Contains(character)) {
                builder.Append('\\');
            }

            builder.Append(character);
        }
    }

    private static string BuildAutocompleteOptionsJson(string listboxId, IReadOnlyList<NTAutocompleteOption> items, string? currentValue, bool allowCustomValue, string customValueOptionFormat, bool fieldDisabled) {
        var builder = new StringBuilder();
        builder.Append('[');
        for (var index = 0; index < items.Count; index++) {
            if (index > 0) {
                builder.Append(',');
            }

            var option = items[index];
            var selected = string.Equals(option.Value, currentValue, StringComparison.Ordinal);
            var disabled = fieldDisabled || option.Disabled;
            AppendOptionJson(builder, GetOptionId(listboxId, index), GetOptionClass(selected, disabled), option.Value, option.Label, option.SupportingText, disabled, selected, isCustom: false, customFormat: null, option.LeadingIcon);
        }

        if (allowCustomValue) {
            if (items.Count > 0) {
                builder.Append(',');
            }

            AppendOptionJson(builder, GetOptionId(listboxId, items.Count), "nt-combobox-option", string.Empty, string.Empty, null, fieldDisabled, selected: false, isCustom: true, customValueOptionFormat, leadingIcon: null);
        }

        builder.Append(']');
        return builder.ToString();
    }

    private static void AppendOptionJson(StringBuilder builder, string id, string cssClass, string value, string label, string? supportingText, bool disabled, bool selected, bool isCustom, string? customFormat, TnTIcon? leadingIcon) {
        builder.Append('{');
        AppendJsonProperty(builder, "id", id);
        builder.Append(',');
        AppendJsonProperty(builder, "cssClass", cssClass);
        builder.Append(',');
        AppendJsonProperty(builder, "value", value);
        builder.Append(',');
        AppendJsonProperty(builder, "label", label);
        if (!string.IsNullOrWhiteSpace(supportingText)) {
            builder.Append(',');
            AppendJsonProperty(builder, "supportingText", supportingText);
        }

        builder.Append(',');
        AppendJsonProperty(builder, "disabled", disabled);
        builder.Append(',');
        AppendJsonProperty(builder, "selected", selected);
        builder.Append(',');
        AppendJsonProperty(builder, "isCustom", isCustom);
        if (customFormat is not null) {
            builder.Append(',');
            AppendJsonProperty(builder, "customFormat", customFormat);
        }

        if (leadingIcon is not null) {
            builder.Append(",\"leadingIcon\":{");
            AppendJsonProperty(builder, "icon", leadingIcon.Icon);
            builder.Append(',');
            AppendJsonProperty(builder, "cssClass", leadingIcon.ElementClass ?? "tnt-components tnt-icon material-symbols-outlined mi-medium");
            if (!string.IsNullOrWhiteSpace(leadingIcon.ElementStyle)) {
                builder.Append(',');
                AppendJsonProperty(builder, "style", leadingIcon.ElementStyle);
            }

            builder.Append(',');
            AppendJsonProperty(builder, "title", leadingIcon.ElementTitle ?? leadingIcon.Icon);
            builder.Append('}');
        }

        builder.Append('}');
    }

    private static void AppendJsonProperty(StringBuilder builder, string name, string value) {
        builder.Append('"');
        builder.Append(name);
        builder.Append("\":\"");
        builder.Append(JavaScriptEncoder.Default.Encode(value));
        builder.Append('"');
    }

    private static void AppendJsonProperty(StringBuilder builder, string name, bool value) {
        builder.Append('"');
        builder.Append(name);
        builder.Append("\":");
        builder.Append(value ? "true" : "false");
    }

    private static string GetOptionClass(bool selected, bool disabled) => (selected, disabled) switch {
        (true, true) => "nt-combobox-option nt-combobox-option-selected nt-combobox-option-disabled",
        (true, false) => "nt-combobox-option nt-combobox-option-selected",
        (false, true) => "nt-combobox-option nt-combobox-option-disabled",
        _ => "nt-combobox-option"
    };

    private static string GetOptionId(string listboxId, int index) => $"{listboxId}-option-{index}";

    private bool CanUpdateCurrentValue() => !FieldReadOnly && !FieldDisabled;

    private async Task OnChangeAsync(ChangeEventArgs args) {
        if (!CanUpdateCurrentValue()) {
            return;
        }

        await UpdateCurrentValueAsync(args.Value?.ToString());
    }

    private async Task UpdateCurrentValueAsync(string? value) {
        CurrentValueAsString = value;
        await BindAfter.InvokeAsync(CurrentValue);
    }

}

/// <summary>
///     Describes one suggestion in an <see cref="NTAutocomplete" />.
/// </summary>
public sealed class NTAutocompleteOption {

    /// <summary>
    ///     Gets or sets whether the suggestion is disabled in the enhanced menu.
    /// </summary>
    public bool Disabled { get; set; }

    /// <summary>
    ///     Gets or sets the visible suggestion label.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the optional leading icon.
    /// </summary>
    public TnTIcon? LeadingIcon { get; set; }

    /// <summary>
    ///     Gets or sets optional supporting text shown below the label.
    /// </summary>
    public string? SupportingText { get; set; }

    /// <summary>
    ///     Gets or sets the value inserted into the text field and submitted with forms.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    ///     Initializes a new instance of the <see cref="NTAutocompleteOption" /> class.
    /// </summary>
    public NTAutocompleteOption() { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="NTAutocompleteOption" /> class.
    /// </summary>
    /// <param name="value">The inserted and submitted value.</param>
    /// <param name="label">The visible suggestion label.</param>
    public NTAutocompleteOption(string value, string label) {
        Value = value;
        Label = label;
    }
}
