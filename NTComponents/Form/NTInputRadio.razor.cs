using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using NTComponents.Core;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace NTComponents;

/// <summary>
///     A radio option for an <see cref="NTInputRadioGroup{TValue}" />.
/// </summary>
/// <typeparam name="TValue">The option value type.</typeparam>
public partial class NTInputRadio<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TValue> : ComponentBase, IDisposable {
    private static readonly HashSet<string> InputExplicitAttributeNames = new(StringComparer.OrdinalIgnoreCase) {
        "id",
        "name",
        "type",
        "value",
        "checked",
        "disabled",
        "required",
        "aria-describedby",
        "onchange"
    };

    /// <summary>
    ///     Gets or sets additional native input attributes.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object?>? AdditionalAttributes { get; set; }

    /// <summary>
    ///     Gets or sets whether this option is disabled.
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    ///     Gets the native radio element.
    /// </summary>
    public ElementReference Element { get; private set; }

    /// <summary>
    ///     Gets or sets an explicit radio input id.
    /// </summary>
    [Parameter]
    public string? ElementId { get; set; }

    /// <summary>
    ///     Gets or sets the option language.
    /// </summary>
    [Parameter]
    public string? ElementLang { get; set; }

    /// <summary>
    ///     Gets or sets the option title.
    /// </summary>
    [Parameter]
    public string? ElementTitle { get; set; }

    /// <summary>
    ///     Gets or sets the visible option label.
    /// </summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>
    ///     Gets or sets an icon before the radio control.
    /// </summary>
    [Parameter]
    public TnTIcon? LeadingIcon { get; set; }

    /// <summary>
    ///     Gets or sets whether this option is read-only.
    /// </summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>
    ///     Gets or sets supporting text for this option.
    /// </summary>
    [Parameter]
    public string? SupportingText { get; set; }

    /// <summary>
    ///     Gets or sets an icon after the option label.
    /// </summary>
    [Parameter]
    public TnTIcon? TrailingIcon { get; set; }

    /// <summary>
    ///     Gets or sets the option value.
    /// </summary>
    [Parameter, EditorRequired]
    public TValue Value { get; set; } = default!;

    [CascadingParameter]
    internal NTInputRadioGroup<TValue> Group { get; set; } = default!;

    [CascadingParameter(Name = "SelectedRadioValue")]
    internal TValue? SelectedValue { get; set; }

    private string InputId => Group.GetRadioInputId(this);

    private IReadOnlyDictionary<string, object?>? InputAttributes => BuildInputAttributes();

    private bool IsSelected => EqualityComparer<TValue>.Default.Equals(SelectedValue, Value);

    private string LabelId => $"{InputId}-label";

    private string? SupportingTextId => string.IsNullOrWhiteSpace(SupportingText) ? null : $"{InputId}-supporting";

    private string? ValueAsString => BindConverter.FormatValue(Value?.ToString(), CultureInfo.CurrentCulture);

    /// <inheritdoc />
    public void Dispose() {
        Group?.UnregisterRadio(this);
    }

    /// <summary>
    ///     Sets focus to this radio option.
    /// </summary>
    public ValueTask SetFocusAsync() => Element.FocusAsync();

    /// <inheritdoc />
    protected override void OnInitialized() {
        if (Group is null) {
            throw new InvalidOperationException($"{nameof(NTInputRadio<TValue>)} must be rendered inside {nameof(NTInputRadioGroup<TValue>)}.");
        }

        Group.RegisterRadio(this);
    }

    private string BuildRootClass() => CssClassBuilder.Create("nt-radio-option")
        .AddClass("nt-radio-option-disabled", Group.IsGroupDisabled || Disabled)
        .AddClass("nt-radio-option-readonly", Group.IsGroupReadOnly || ReadOnly)
        .AddClass("nt-radio-option-standard", Group.IsStandard)
        .AddClass("nt-radio-option-dense", Group.IsDense)
        .AddClass("nt-radio-option-no-label", string.IsNullOrWhiteSpace(Label))
        .AddClass("nt-radio-option-has-leading-icon", LeadingIcon is not null)
        .AddClass("nt-radio-option-has-trailing-icon", TrailingIcon is not null)
        .AddClass(GetAdditionalClass())
        .Build();

    private IReadOnlyDictionary<string, object?>? BuildInputAttributes() {
        if (AdditionalAttributes is null) {
            return null;
        }

        var attributes = new Dictionary<string, object?>(AdditionalAttributes, StringComparer.OrdinalIgnoreCase);
        attributes.Remove("class");
        foreach (var attributeName in InputExplicitAttributeNames) {
            attributes.Remove(attributeName);
        }

        return attributes.Count == 0 ? null : attributes;
    }

    private string? GetAdditionalClass() => AdditionalAttributes?.TryGetValue("class", out var @class) == true ? @class?.ToString() : null;

    private Task OnChangeAsync(ChangeEventArgs args) => Group.SelectValueFromStringAsync(args.Value?.ToString());
}
