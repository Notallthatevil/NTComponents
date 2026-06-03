using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.Diagnostics.CodeAnalysis;

namespace NTComponents;

/// <summary>
///     Provides shared behavior for Material 3 boolean inputs such as checkboxes and switches.
/// </summary>
/// <remarks>
///     This base intentionally owns behavior only: form inheritance, stable ids and names, static SSR fallback inputs,
///     required validation, and common labeling/supporting text parameters. Visual structure, color parameters, variants,
///     and Material measurements remain owned by each concrete component.
/// </remarks>
public abstract class NTBooleanInputBase : NTFormControlBase<bool>, IDisposable {
    /// <summary>
    ///     Gets the native input attributes owned by all boolean inputs.
    /// </summary>
    protected static readonly string[] CommonExplicitInputAttributeNames = [
        "id",
        "name",
        "type",
        "value",
        "checked",
        "required",
        "disabled",
        "autofocus",
        "aria-checked",
        "aria-describedby",
        "aria-invalid",
        "aria-errormessage",
        "aria-required",
        "onchange"
    ];

    private bool _hasValidatedRequired;
    private bool _requiredValidationMessageVisible;
    private EditContext? _validationEditContext;
    private ValidationMessageStore? _requiredValidationMessages;
    /// <summary>
    ///     Gets or sets a callback invoked after the value changes.
    /// </summary>
    [Parameter]
    public EventCallback<bool> BindAfter { get; set; }

    /// <summary>
    ///     Gets or sets whether this boolean input must be selected/on for Blazor validation.
    /// </summary>
    [Parameter]
    public bool Required { get; set; }

    /// <summary>
    ///     Gets or sets the validation message shown when <see cref="Required" /> is true and the input is not selected/on.
    /// </summary>
    [Parameter]
    public string RequiredErrorText { get; set; } = "This field is required.";

    /// <summary>
    ///     Gets filtered additional attributes for the native input element.
    /// </summary>
    protected IReadOnlyDictionary<string, object?>? InputAttributes { get; private set; }

    /// <summary>
    ///     Gets the value posted by hidden static SSR fallback inputs.
    /// </summary>
    protected string FormPostValue => CurrentValue ? "true" : "false";

    /// <summary>
    ///     Gets a value indicating whether an editable static SSR false fallback should be rendered.
    /// </summary>
    protected bool ShouldRenderFormPostFallback => !FieldDisabled && !string.IsNullOrWhiteSpace(ElementName);

    /// <summary>
    ///     Gets a value indicating whether a read-only static SSR current-value fallback should be rendered.
    /// </summary>
    protected bool ShouldRenderReadOnlyFormPostValue => !FieldDisabled && FieldReadOnly && !string.IsNullOrWhiteSpace(ElementName);

    /// <summary>
    ///     Gets explicit input attributes that this component owns and should not be overridden by additional attributes.
    /// </summary>
    protected virtual IEnumerable<string> ExplicitInputAttributeNames => CommonExplicitInputAttributeNames;

    /// <summary>
    ///     Gets the generated id prefix for the concrete input.
    /// </summary>
    protected abstract override string InputIdPrefix { get; }

    /// <inheritdoc />
    protected override bool HasRequiredSupportingText => Required;

    /// <inheritdoc />
    public void Dispose() => DetachValidationEditContext();

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();
        AttachValidationEditContext();
        InputAttributes = BuildInputAttributes();

        if (!Required && _requiredValidationMessageVisible) {
            ClearRequiredValidation();
            _hasValidatedRequired = false;
        }
        else if (_hasValidatedRequired) {
            ValidateRequired(notifyValidationStateChanged: false);
        }
    }

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out bool result, [NotNullWhen(false)] out string? validationErrorMessage) {
        throw new NotSupportedException($"{GetType().Name} does not parse string values. Use the native change event.");
    }

    /// <summary>
    ///     Handles the native change event.
    /// </summary>
    protected virtual async Task OnChangeAsync(ChangeEventArgs args) {
        if (FieldReadOnly || FieldDisabled) {
            return;
        }

        CurrentValue = args.Value is bool boolValue && boolValue;
        await BindAfter.InvokeAsync(CurrentValue);
    }

    private void AttachValidationEditContext() {
        if (ReferenceEquals(_validationEditContext, EditContext)) {
            return;
        }

        DetachValidationEditContext();
        if (EditContext is null) {
            return;
        }

        _validationEditContext = EditContext;
        _requiredValidationMessages = new ValidationMessageStore(EditContext);
        EditContext.OnValidationRequested += OnValidationRequested;
        EditContext.OnFieldChanged += OnFieldChanged;
    }

    private void DetachValidationEditContext() {
        if (_validationEditContext is null) {
            return;
        }

        var shouldNotify = _requiredValidationMessageVisible;
        _requiredValidationMessages?.Clear(FieldIdentifier);
        _requiredValidationMessageVisible = false;
        _hasValidatedRequired = false;
        _validationEditContext.OnValidationRequested -= OnValidationRequested;
        _validationEditContext.OnFieldChanged -= OnFieldChanged;
        if (shouldNotify) {
            _validationEditContext.NotifyValidationStateChanged();
        }

        _validationEditContext = null;
        _requiredValidationMessages = null;
    }

    private void OnValidationRequested(object? sender, ValidationRequestedEventArgs args) {
        _hasValidatedRequired = true;
        ValidateRequired();
    }

    private void OnFieldChanged(object? sender, FieldChangedEventArgs args) {
        if (!args.FieldIdentifier.Equals(FieldIdentifier)) {
            return;
        }

        _hasValidatedRequired = true;
        ValidateRequired();
    }

    private void ClearRequiredValidation() {
        _requiredValidationMessages?.Clear(FieldIdentifier);
        _requiredValidationMessageVisible = false;
        EditContext?.NotifyValidationStateChanged();
    }

    private void ValidateRequired(bool notifyValidationStateChanged = true) {
        _requiredValidationMessages?.Clear(FieldIdentifier);
        _requiredValidationMessageVisible = false;

        if (Required && !CurrentValue) {
            _requiredValidationMessages?.Add(FieldIdentifier, RequiredErrorText);
            _requiredValidationMessageVisible = true;
        }

        if (notifyValidationStateChanged) {
            EditContext?.NotifyValidationStateChanged();
        }
    }

    private IReadOnlyDictionary<string, object?>? BuildInputAttributes() => BuildFilteredAttributes(ExplicitInputAttributeNames);
}
