using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using NTComponents.Ext;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
///     A Material 3 aligned multi-select combobox field.
/// </summary>
/// <remarks>
///     Use <see cref="NTCombobox{TValue}" /> when users need to choose multiple values from a finite list while retaining
///     text-field placement and optional filtering. Items toggle on click or keyboard activation; users never need to hold
///     Control or Command. Keep option labels short, use supporting text only when it materially helps recognition, and
///     prefer selected check icons because selected state should not rely on color alone.
///     <para>
///         Best practices:
///     </para>
///     <list type="bullet">
///         <item>Use this component for multi-select choice from a known list. Use <see cref="NTSelect{TValue}" /> for a single native choice.</item>
///         <item>Keep labels concise and scannable. Put clarifying detail in <see cref="NTComboboxOption{TValue}.SupportingText" /> only when it helps users distinguish similar options.</item>
///         <item>Provide a visible label whenever possible. If a label is intentionally hidden, provide an <c>aria-label</c> through additional attributes.</item>
///         <item>Use stable, unique formatted option values. Duplicate values or values that format to the same string cannot be distinguished by the browser module.</item>
///         <item>Prefer leaving <see cref="Searchable" /> enabled for medium or long lists. Disable it only for short lists where filtering adds friction.</item>
///         <item>Do not put arbitrary interactive content inside options. Use the provided label, supporting text, disabled state, and leading icon properties so keyboard and screen-reader behavior stays predictable.</item>
///     </list>
///     <para>
///         The component renders form-post hidden inputs for selected values and uses a small JavaScript module for popup
///         behavior. Static SSR can render the current selection, but opening, filtering, and toggling options require an
///         interactive render mode.
///     </para>
/// </remarks>
/// <typeparam name="TValue">The option value type.</typeparam>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.InteractiveRequired,
    CompatibilitySummary = "Requires browser enhancement for its multi-select combobox workflow.",
    CompatibilityDetails = "Static SSR renders the current selection and hidden form values, but opening, filtering, and toggling options depend on the JavaScript module and Blazor callbacks.")]
public partial class NTCombobox<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TValue> : IAsyncDisposable {
    private const string JsModulePath = "./_content/NTComponents/Form/NTCombobox.razor.js";
    private static readonly HashSet<string> ComboboxExplicitControlAttributeNames = new(StringComparer.OrdinalIgnoreCase) {
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
        "value"
    };

    private const string ComboboxControlBaseClass = "nt-input-control nt-combobox-control";
    private DotNetObjectReference<NTCombobox<TValue>>? _dotNetObjectRef;
    private readonly Dictionary<string, TValue> _optionsByFormattedValue = new(StringComparer.Ordinal);
    private IReadOnlyList<NTComboboxOption<TValue>> _items = [];
    private IJSObjectReference? _jsModule;
    private bool _isOpen;

    /// <summary>
    ///     Gets or sets the comparison used to match selected values to options.
    /// </summary>
    /// <remarks>
    ///     Set this when <typeparamref name="TValue" /> uses custom equality, case-insensitive string semantics, or value
    ///     objects. The comparer is used for display and selected-state rendering; browser callbacks still depend on stable
    ///     formatted option values.
    /// </remarks>
    [Parameter]
    public IEqualityComparer<TValue>? Comparer { get; set; }

    /// <summary>
    ///     Gets or sets the text rendered when filtering produces no options.
    /// </summary>
    /// <remarks>
    ///     Keep this text short and actionable. Avoid error-like wording because an empty filtered result is usually a normal
    ///     search state, not validation failure.
    /// </remarks>
    [Parameter]
    public string EmptyText { get; set; } = "No options";

    /// <summary>
    ///     Gets or sets the available options.
    /// </summary>
    /// <remarks>
    ///     Prefer a stable list instance or stable option ordering so selected summaries and keyboard navigation do not shift
    ///     unexpectedly across renders. Option values should be unique after invariant formatting.
    /// </remarks>
    [Parameter]
    public IReadOnlyList<NTComboboxOption<TValue>> Items { get; set; } = [];

    /// <summary>
    ///     Gets or sets the popup menu item appearance.
    /// </summary>
    /// <remarks>
    ///     Use <see cref="NTMenuItemAppearance.Condensed" /> only for compact, highly scannable lists. Condensed menu items
    ///     are capped at 38 CSS pixels and are best suited to short labels without supporting text.
    /// </remarks>
    [Parameter]
    public NTMenuItemAppearance MenuItemAppearance { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether users can type to filter the list.
    /// </summary>
    /// <remarks>
    ///     Leave filtering enabled for medium and long lists. For very short lists, disabling search keeps the field acting
    ///     more like a compact multi-select menu while preserving click-to-toggle behavior.
    /// </remarks>
    [Parameter]
    public bool Searchable { get; set; } = true;

    /// <summary>
    ///     Gets or sets the separator used to summarize selected labels in the collapsed field.
    /// </summary>
    /// <remarks>
    ///     Use a short separator such as <c>", "</c>. Long separators make selected summaries harder to scan and can crowd
    ///     compact field densities.
    /// </remarks>
    [Parameter]
    public string SelectedTextSeparator { get; set; } = ", ";

    /// <inheritdoc />
    protected override IEnumerable<string> ExplicitControlAttributeNames => ComboboxExplicitControlAttributeNames;

    /// <inheritdoc />
    protected override string InputIdPrefix => "nt-combobox";

    /// <inheritdoc />
    protected override bool HasFloatingValue => SelectedValues.Count > 0;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    private string ComboboxControlClass {
        get {
            var cssClass = CssClass;
            return string.IsNullOrEmpty(cssClass) ? ComboboxControlBaseClass : $"{ComboboxControlBaseClass} {cssClass}";
        }
    }

    private IEqualityComparer<TValue> EffectiveComparer => Comparer ?? EqualityComparer<TValue>.Default;

    private IReadOnlyList<NTComboboxOption<TValue>> EffectiveItems => _items;

    private string ListboxId => $"{InputId}-listbox";

    private string ListboxLabel => string.IsNullOrWhiteSpace(Label) ? FieldIdentifier.FieldName : Label;

    private IReadOnlyList<TValue> SelectedValues => CurrentValue ?? [];

    private static string GetOptionClass(bool selected, bool disabled) => (selected, disabled) switch {
        (true, true) => "nt-combobox-option nt-combobox-option-selected nt-combobox-option-disabled",
        (true, false) => "nt-combobox-option nt-combobox-option-selected",
        (false, true) => "nt-combobox-option nt-combobox-option-disabled",
        _ => "nt-combobox-option"
    };

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

    /// <inheritdoc />
    protected override void BuildAdditionalRootClasses(StringBuilder builder) {
        builder.Append(" nt-combobox");
        if (MenuItemAppearance == NTMenuItemAppearance.Condensed) {
            builder.Append(" nt-combobox-menu-items-condensed");
        }

        if (_isOpen) {
            builder.Append(" nt-combobox-open");
        }
    }

    /// <inheritdoc />
    protected override TrailingAdornmentState CreateTrailingAdornmentState(bool hasErrorText) {
        if (hasErrorText) {
            return base.CreateTrailingAdornmentState(hasErrorText);
        }

        return new TrailingAdornmentState {
            Icon = TrailingIcon ?? MaterialIcon.ArrowDropDown,
            Class = "nt-input-trailing nt-combobox-indicator",
            AriaHidden = "true"
        };
    }

    /// <inheritdoc />
    protected override string? FormatValueAsString(IReadOnlyList<TValue>? value) => value is null || value.Count == 0 ? null : BuildSelectionDisplayText(value, EffectiveItems);

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
    }

    /// <inheritdoc />
    protected override void OnParametersSet() {
        _items = Items ?? [];
        _optionsByFormattedValue.Clear();
        foreach (var option in _items) {
            _optionsByFormattedValue.TryAdd(FormatOptionValue(option.Value), option.Value);
        }

        base.OnParametersSet();
    }

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out IReadOnlyList<TValue> result, [NotNullWhen(false)] out string? validationErrorMessage) {
        result = CurrentValue ?? [];
        validationErrorMessage = null;
        return true;
    }

    /// <summary>
    ///     Receives selected option values from the browser module.
    /// </summary>
    /// <param name="selectedValues">The formatted selected values.</param>
    /// <returns>A task representing the update.</returns>
    [JSInvokable]
    public async Task NotifyComboboxSelectionChanged(string[] selectedValues) {
        if (FieldDisabled || FieldReadOnly) {
            return;
        }

        CurrentValue = ResolveSelectedValues(selectedValues);
        _isOpen = true;
        await BindAfter.InvokeAsync(CurrentValue);
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    ///     Receives blur/touched notifications from the browser module.
    /// </summary>
    /// <returns>A task representing the update.</returns>
    [JSInvokable]
    public async Task NotifyComboboxTouched() {
        _isOpen = false;
        await OnBlurAsync(new Microsoft.AspNetCore.Components.Web.FocusEventArgs());
        await InvokeAsync(StateHasChanged);
    }

    private string BuildSelectionDisplayText(IReadOnlyList<TValue> selectedValues, IReadOnlyList<NTComboboxOption<TValue>> items) {
        if (selectedValues.Count == 0) {
            return string.Empty;
        }

        var selectedLabels = new List<string>(selectedValues.Count);
        foreach (var selectedValue in selectedValues) {
            var option = items.FirstOrDefault(item => ValuesEqual(item.Value, selectedValue));
            selectedLabels.Add(option is null ? FormatOptionValue(selectedValue) : option.Label);
        }

        return string.Join(SelectedTextSeparator, selectedLabels);
    }

    private HashSet<TValue>? BuildSelectedValueSet(IReadOnlyList<TValue> selectedValues) => selectedValues.Count == 0 ? null : new HashSet<TValue>(selectedValues, EffectiveComparer);

    private string FormatOptionValue(TValue value) => value switch {
        null => string.Empty,
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty
    };

    private static string GetOptionId(string listboxId, int index) => $"{listboxId}-option-{index}";

    private IReadOnlyList<TValue> ResolveSelectedValues(IEnumerable<string> selectedValues) {
        var resolvedValues = new List<TValue>();
        foreach (var selectedValue in selectedValues) {
            if (selectedValue is not null && _optionsByFormattedValue.TryGetValue(selectedValue, out var resolvedValue)) {
                resolvedValues.Add(resolvedValue);
            }
        }

        return resolvedValues;
    }

    private bool ShouldRenderFormPostValues(IReadOnlyList<TValue> selectedValues) => !FieldDisabled && !string.IsNullOrWhiteSpace(ElementName) && selectedValues.Count > 0;

    private bool ValuesEqual(TValue left, TValue right) => EffectiveComparer.Equals(left, right);
}

/// <summary>
///     Describes one option in an <see cref="NTCombobox{TValue}" />.
/// </summary>
/// <remarks>
///     Keep each option focused on recognition: a value, a short label, optional supporting text, optional leading icon, and
///     disabled state. Avoid encoding presentation-only text into <see cref="Value" />; the value is emitted to forms and
///     binding callbacks.
/// </remarks>
/// <typeparam name="TValue">The option value type.</typeparam>
public sealed class NTComboboxOption<TValue> {
    /// <summary>
    ///     Initializes a new instance of the <see cref="NTComboboxOption{TValue}" /> class.
    /// </summary>
    public NTComboboxOption() { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="NTComboboxOption{TValue}" /> class.
    /// </summary>
    /// <param name="value">The emitted value.</param>
    /// <param name="label">The visible option label.</param>
    public NTComboboxOption(TValue value, string label) {
        Value = value;
        Label = label;
    }

    /// <summary>
    ///     Gets or sets whether the option is disabled.
    /// </summary>
    /// <remarks>
    ///     Disabled options remain visible for context but cannot be toggled. Use disabled options sparingly; when an option is
    ///     unavailable, supporting text should explain why if the reason is not obvious.
    /// </remarks>
    public bool Disabled { get; set; }

    /// <summary>
    ///     Gets or sets the visible option label.
    /// </summary>
    /// <remarks>
    ///     Use concise human-readable text. Labels are used for filtering and selected-value summaries.
    /// </remarks>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the optional leading icon.
    /// </summary>
    /// <remarks>
    ///     Icons should reinforce recognition and should not be the only way to distinguish options.
    /// </remarks>
    public TnTIcon? LeadingIcon { get; set; }

    /// <summary>
    ///     Gets or sets optional supporting text shown below the label.
    /// </summary>
    /// <remarks>
    ///     Use supporting text for disambiguation, not secondary paragraphs. Long supporting text is truncated in the popup.
    /// </remarks>
    public string? SupportingText { get; set; }

    /// <summary>
    ///     Gets or sets the emitted value.
    /// </summary>
    /// <remarks>
    ///     Values are formatted invariantly for DOM attributes, hidden form inputs, and JavaScript callbacks. Use values that
    ///     produce unique formatted strings.
    /// </remarks>
    public TValue Value { get; set; } = default!;
}
