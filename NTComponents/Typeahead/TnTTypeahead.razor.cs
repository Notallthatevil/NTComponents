using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Linq.Expressions;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
///     Represents a typeahead component that provides suggestions as the user types.
/// </summary>
/// <typeparam name="TItem">The type of the items to be suggested.</typeparam>
public partial class TnTTypeahead<TItem> {

    /// <summary>
    ///     The background color of the typeahead component.
    /// </summary>
    [Parameter]
    public FormAppearance Appearance { get; set; }

    /// <summary>
    ///     The background color of the typeahead component.
    /// </summary>
    [Parameter]
    public TnTColor BackgroundColor { get; set; } = TnTColor.SurfaceContainerHighest;

    /// <summary>
    ///     The delay in milliseconds before performing a search after the user types.
    /// </summary>
    [Parameter]
    public int DebounceMilliseconds { get; set; } = 300;

    /// <summary>
    ///     Indicates whether the typeahead input is disabled.
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("tnt-typeahead")
        .Build();

    /// <summary>
    ///     The name attribute for the input element.
    /// </summary>
    [Parameter]
    public string? ElementName { get; set; }

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .Build();

    /// <summary>
    ///     Enables ripple effect on the input element if true.
    /// </summary>
    [Parameter]
    public bool EnableRipple { get; set; }

    /// <summary>
    ///     Gets or sets the error message to display when an error occurs.
    /// </summary>
    [Parameter]
    public string? ErrorMessage { get; set; }

    /// <summary>
    ///     Callback invoked when an item is selected from the suggestions.
    /// </summary>
    [Parameter]
    public EventCallback<TItem> ItemSelectedCallback { get; set; }

    /// <inheritdoc />
    public override string? JsModulePath => "./_content/NTComponents/Typeahead/TnTTypeahead.razor.js";

    /// <summary>
    ///     Function used to retrieve items based on the current search text.
    /// </summary>
    [Parameter, EditorRequired]
    public Func<string?, CancellationToken, Task<IEnumerable<TItem>>> ItemsLookupFunc { get; set; } = default!;

    /// <summary>
    ///     The label displayed for the input element.
    /// </summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>
    ///     The color used for content that appears on the tint color.
    /// </summary>
    [Parameter]
    public TnTColor? OnTintColor { get; set; }

    /// <summary>
    ///     The placeholder text shown in the input when empty.
    /// </summary>
    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>
    ///     The color of the progress indicator shown during search.
    /// </summary>
    [Parameter]
    public TnTColor ProgressColor { get; set; } = TnTColor.Primary;

    /// <summary>
    ///     If true, the input box will be refocused after an item is selected.
    /// </summary>
    [Parameter]
    public bool RefocusAfterSelect { get; set; } = true;

    /// <summary>
    ///     If true, the input value will be reset when the Escape key is pressed.
    /// </summary>
    [Parameter]
    public bool ResetValueOnEscape { get; set; } = true;

    /// <summary>
    ///     If true, the input value will be reset after an item is selected.
    /// </summary>
    [Parameter]
    public bool ResetValueOnSelect { get; set; } = true;

    /// <summary>
    ///     Template used to render each result item in the suggestion list.
    /// </summary>
    [Parameter]
    public RenderFragment<TItem>? ResultTemplate { get; set; }

    /// <summary>
    ///     The supporting text that provides additional context or information related to the associated component.
    /// </summary>
    /// <remarks>This property can be used to display supplementary information to users, enhancing the understanding of the component's purpose or functionality.</remarks>
    [Parameter]
    public string? SupportingText { get; set; }

    /// <summary>
    ///     The tint color applied to the component.
    /// </summary>
    [Parameter]
    public TnTColor? TintColor { get; set; }

    /// <summary>
    ///     Gets or sets the content to be displayed as a tooltip when hovering over the associated element.
    /// </summary>
    [Parameter]
    public RenderFragment? Tooltip { get; set; }

    /// <summary>
    ///     Gets or sets the icon displayed in the tooltip.
    /// </summary>
    [Parameter]
    public TnTIcon TooltipIcon { get; set; } = MaterialIcon.Help;

    /// <summary>
    ///     The current value of the input box.
    /// </summary>
    [Parameter]
    public string? Value { get; set; }

    /// <summary>
    ///     Event callback invoked when the input value changes.
    /// </summary>
    [Parameter]
    public EventCallback<string> ValueChanged { get; set; }

    /// <summary>
    ///     An expression that identifies the value to be validated.
    /// </summary>
    [Parameter]
    public Expression<Func<TItem?>>? ValueExpression { get; set; }

    /// <summary>
    ///     Function used to convert an item to its string representation for display.
    /// </summary>
    [Parameter]
    public Func<TItem, string> ValueToStringFunc { get; set; } = item => item?.ToString() ?? string.Empty;

    /// <summary>
    ///     The cascading edit context used for form validation.
    /// </summary>
    [CascadingParameter]
    private EditContext? EditContext { get; set; }

    /// <summary>
    ///     The currently focused item in the suggestion list.
    /// </summary>
    private TItem? _focusedItem { get; set; }

    private TnTDebouncer _debouncer = default!;
    private TnTInputText _inputBox = default!;
    private IEnumerable<TItem> _items = [];
    private bool _itemSelected;
    private int _lastDebounceMilliseconds = -1;
    private bool _searching;
    private string? _displayErrorMessage;
    private FieldIdentifier? _fieldIdentifier;
    private bool _managedResourcesDisposed;

    /// <inheritdoc />
    protected override void OnInitialized() {
        base.OnInitialized();

        EditContext?.OnValidationStateChanged += HandleValidationStateChanged;

        _displayErrorMessage = ErrorMessage;
    }

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();

        if (EditContext is not null && ValueExpression is not null) {
            _fieldIdentifier = FieldIdentifier.Create(ValueExpression);
        }

        // Only recreate debouncer if the delay actually changed
        if (_lastDebounceMilliseconds != DebounceMilliseconds) {
            _debouncer?.Dispose();
            _debouncer = new TnTDebouncer(DebounceMilliseconds);
            _lastDebounceMilliseconds = DebounceMilliseconds;
        }

        UpdateValidationState();
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing) {
        if (disposing) {
            DisposeManagedResources();
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore() {
        DisposeManagedResources();
        await base.DisposeAsyncCore();
    }

    /// <summary>
    ///     Handles the selection of an item asynchronously and updates the input value and state.
    /// </summary>
    /// <param name="item">The selected item.</param>
    private async Task ItemSelectedAsync(TItem item) {
        Value = ResetValueOnSelect ? null : ValueToStringFunc(item);
        await ValueChanged.InvokeAsync(Value);
        _items = [];
        _searching = false;
        _focusedItem = default;
        _itemSelected = true;
        await ItemSelectedCallback.InvokeAsync(item);

        EditContext?.Validate();

        if (RefocusAfterSelect) {
            await _inputBox.SetFocusAsync();
        }
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    ///     Performs a search asynchronously based on the provided search value and updates the suggestion list.
    /// </summary>
    /// <param name="searchValue">The search value.</param>
    private async Task SearchAsync(string? searchValue) {
        _searching = true;
        _itemSelected = false;
        await InvokeAsync(StateHasChanged);

        await _debouncer.DebounceAsync(async token => {
            await ValueChanged.InvokeAsync(searchValue);
            if (string.IsNullOrWhiteSpace(searchValue)) {
                _searching = false;
                _items = [];

                EditContext?.Validate();
            }
            else {
                _items = [];
                _focusedItem = default;
                var result = await ItemsLookupFunc.Invoke(searchValue, token);
                if (!token.IsCancellationRequested) {
                    _items = result;
                    _focusedItem = _items.FirstOrDefault();
                }
                _searching = false;
            }
            await InvokeAsync(StateHasChanged);
        });
    }

    /// <summary>
    ///     Handles keyboard events to select an item or shift focus within the suggestion list.
    /// </summary>
    /// <param name="args">The keyboard event arguments.</param>
    private async Task SelectOrShiftFocusAsync(KeyboardEventArgs args) {
        if (_focusedItem is not null && !_focusedItem.Equals(default)) {
            switch (args.Key) {
                case "Enter":
                    await ItemSelectedAsync(_focusedItem);
                    break;

                case "ArrowDown": {
                        var index = _items.ToList().IndexOf(_focusedItem);
                        _focusedItem = index < _items.Count() - 1 ? _items.ElementAt(index + 1) : _items.FirstOrDefault();
                        break;
                    }
                case "ArrowUp": {
                        var index = _items.ToList().IndexOf(_focusedItem);
                        _focusedItem = index > 0 ? _items.ElementAt(index - 1) : _items.LastOrDefault();
                        break;
                    }
                case "Escape": {
                        _items = [];
                        _searching = false;
                        if (ResetValueOnEscape) {
                            Value = null;
                            await ValueChanged.InvokeAsync(Value);
                        }
                    }
                    break;

                default:
                    break;
            }
        }
    }

    private void HandleValidationStateChanged(object? sender, ValidationStateChangedEventArgs e) {
        UpdateValidationState();
    }

    /// <summary>
    /// Invoked from JavaScript to close the dropdown and reset its state asynchronously.
    /// </summary>
    /// <remarks>This method is intended to be called via JavaScript interop. It resets the dropdown's items
    /// and state, ensuring the UI reflects the closed state. If the dropdown is already empty or certain conditions are
    /// met, the method completes without making changes.</remarks>
    /// <returns>A task that represents the asynchronous operation of closing the dropdown.</returns>
    [JSInvokable]
    public async Task CloseDropdownFromJs() {
        if (!_items.Any() && (string.IsNullOrWhiteSpace(Value) || _searching || _itemSelected)) {
            return;
        }

        _items = [];
        _focusedItem = default;
        _searching = false;
        _itemSelected = true;
        await InvokeAsync(StateHasChanged);
    }

    private void UpdateValidationState() {
        var messages = _fieldIdentifier.HasValue
            ? EditContext?.GetValidationMessages(_fieldIdentifier.Value)
            : EditContext?.GetValidationMessages();

        _displayErrorMessage = messages?.Any() == true ? ErrorMessage : null;
        InvokeAsync(StateHasChanged);
    }

    private void DisposeManagedResources() {
        if (_managedResourcesDisposed) {
            return;
        }

        EditContext?.OnValidationStateChanged -= HandleValidationStateChanged;

        _debouncer?.Dispose();
        _debouncer = null!;

        _managedResourcesDisposed = true;
    }
}
