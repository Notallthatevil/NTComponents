using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using NTComponents.Core;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
///     A radio option for an <see cref="NTInputRadioGroup{TValue}" />.
/// </summary>
/// <typeparam name="TValue">The option value type.</typeparam>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.SsrCompatible,
    CompatibilitySummary = "Renders native form markup that works with static SSR and form posts.",
    CompatibilityDetails = "The native control can participate in static SSR and normal form posts. Blazor binding callbacks and live validation updates require interactivity or a subsequent render.")]
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

    private string? _generatedInputId;
    private string? _generatedInputIdBase;
    private int _generatedInputIdIndex;
    private string? _inputId;
    private string? _labelId;
    private string? _rootClass;
    private string? _rootClassAdditionalClass;
    private int _rootClassFlags;
    private string? _supportingTextId;

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
    public NTIcon? LeadingIcon { get; set; }

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
    public NTIcon? TrailingIcon { get; set; }

    /// <summary>
    ///     Gets or sets the option value.
    /// </summary>
    [Parameter, EditorRequired]
    public TValue Value { get; set; } = default!;

    [CascadingParameter]
    internal NTInputRadioGroup<TValue> Group { get; set; } = default!;

    [CascadingParameter(Name = "SelectedRadioValue")]
    internal TValue? SelectedValue { get; set; }

    // Rebuilt every render: AdditionalAttributes can be passed explicitly and mutated in place, so a reference-keyed cache could go stale.
    private IReadOnlyDictionary<string, object?>? InputAttributes => BuildInputAttributes();

    private bool IsSelected => EqualityComparer<TValue>.Default.Equals(SelectedValue, Value);

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

    private string BuildRootClass() {
        var additionalClass = GetAdditionalClass();
        var flags = (Group.IsGroupDisabled || Disabled ? 1 : 0) | (Group.IsGroupReadOnly || ReadOnly ? 2 : 0) | (Group.IsStandard ? 4 : 0) | (Group.IsDense ? 8 : 0)
            | (string.IsNullOrWhiteSpace(Label) ? 16 : 0) | (LeadingIcon is not null ? 32 : 0) | (TrailingIcon is not null ? 64 : 0);
        // The class string only changes with these inputs, so reuse it across renders.
        if (_rootClass is null || flags != _rootClassFlags || !ReferenceEquals(additionalClass, _rootClassAdditionalClass)) {
            _rootClass = CssClassBuilder.Create("nt-radio-option")
                .AddClass("nt-radio-option-disabled", (flags & 1) != 0)
                .AddClass("nt-radio-option-readonly", (flags & 2) != 0)
                .AddClass("nt-radio-option-standard", (flags & 4) != 0)
                .AddClass("nt-radio-option-dense", (flags & 8) != 0)
                .AddClass("nt-radio-option-no-label", (flags & 16) != 0)
                .AddClass("nt-radio-option-has-leading-icon", (flags & 32) != 0)
                .AddClass("nt-radio-option-has-trailing-icon", (flags & 64) != 0)
                .AddClass(additionalClass)
                .Build();
            _rootClassFlags = flags;
            _rootClassAdditionalClass = additionalClass;
        }

        return _rootClass;
    }

    /// <summary>
    ///     Resolves the input, label, and supporting-text ids, rebuilding them only when the group id or option index changes.
    /// </summary>
    private string ResolveIds() {
        string inputId;
        if (!string.IsNullOrWhiteSpace(ElementId)) {
            inputId = ElementId;
        }
        else {
            var groupId = Group.RadioIdBase;
            var index = Group.GetRadioIndex(this);
            if (_generatedInputId is null || index != _generatedInputIdIndex || !ReferenceEquals(groupId, _generatedInputIdBase)) {
                _generatedInputId = $"{groupId}-option-{index + 1}";
                _generatedInputIdBase = groupId;
                _generatedInputIdIndex = index;
            }

            inputId = _generatedInputId;
        }

        if (!ReferenceEquals(inputId, _inputId)) {
            _inputId = inputId;
            _labelId = null;
            _supportingTextId = null;
        }

        return inputId;
    }

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
