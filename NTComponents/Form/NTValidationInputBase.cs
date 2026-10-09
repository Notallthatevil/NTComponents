using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace NTComponents;

/// <summary>
///     Limits validation-triggered rendering to changes in this input's field messages or CSS state.
/// </summary>
/// <typeparam name="TValue">The bound value type.</typeparam>
public abstract class NTValidationInputBase<TValue> : InputBase<TValue> {
    private string[] _renderedValidationMessages = [];
    private string? _renderedFieldCssClass;
    private bool _isValidationRender;
    private EditContext? _subscribedEditContext;

    [CascadingParameter]
    private EditContext? ValidationEditContext { get; set; }

    /// <inheritdoc />
    public override Task SetParametersAsync(ParameterView parameters) {
        if (_subscribedEditContext is not null
            || !parameters.TryGetValue<EditContext>(nameof(ValidationEditContext), out var editContext)
            || editContext is null) {
            return base.SetParametersAsync(parameters);
        }

        _subscribedEditContext = editContext;
        // Bracket InputBase's private handler so ordinary ComponentBase render requests remain unconditional.
        editContext.OnValidationStateChanged += BeginValidationRender;
        try {
            var task = base.SetParametersAsync(parameters);
            editContext.OnValidationStateChanged += EndValidationRender;
            return task;
        }
        catch {
            DetachValidationHandlers();
            throw;
        }
    }

    /// <inheritdoc />
    protected override bool ShouldRender() {
        var isValidationRender = _isValidationRender;
        _isValidationRender = false;
        return !isValidationRender
            || EditContext is null
            || !EditContext.GetValidationMessages(FieldIdentifier).SequenceEqual(_renderedValidationMessages, StringComparer.Ordinal)
            || !string.Equals(EditContext.FieldCssClass(FieldIdentifier), _renderedFieldCssClass, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender) {
        _renderedValidationMessages = EditContext?.GetValidationMessages(FieldIdentifier).ToArray() ?? [];
        _renderedFieldCssClass = EditContext?.FieldCssClass(FieldIdentifier);
        _isValidationRender = false;
        base.OnAfterRender(firstRender);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing) {
        if (disposing) {
            DetachValidationHandlers();
        }

        base.Dispose(disposing);
    }

    private void BeginValidationRender(object? sender, ValidationStateChangedEventArgs args) => _isValidationRender = true;

    private void EndValidationRender(object? sender, ValidationStateChangedEventArgs args) => _isValidationRender = false;

    private void DetachValidationHandlers() {
        if (_subscribedEditContext is null) {
            return;
        }

        _subscribedEditContext.OnValidationStateChanged -= BeginValidationRender;
        _subscribedEditContext.OnValidationStateChanged -= EndValidationRender;
        _subscribedEditContext = null;
        _isValidationRender = false;
    }
}
