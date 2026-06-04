using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using NTComponents.Core;
using NTComponents.Ext;
using System.Diagnostics.CodeAnalysis;
using System.Text;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
///     A Material 3 aligned async typeahead field that binds a selected item and participates in <see cref="NTForm" /> validation.
/// </summary>
/// <typeparam name="TItem">The item type selected by the typeahead.</typeparam>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.InteractiveRequired,
    CompatibilitySummary = "Requires an interactive render mode for async lookup and selection.",
    CompatibilityDetails = "The typeahead depends on Blazor events, async lookup callbacks, cancellation, and JavaScript scroll enhancement for its primary suggestion workflow. Static SSR can only render the initial field and submitted value.")]
public partial class NTTypeahead<TItem> : IAsyncDisposable {
    private const string JsModulePath = "./_content/NTComponents/Form/NTTypeahead.razor.js";
    private static readonly HashSet<string> TypeaheadExplicitControlAttributeNames = new(StringComparer.OrdinalIgnoreCase) {
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
        "oninput",
        "onfocus",
        "onkeydown",
        "onblur"
    };

    private CancellationTokenSource? _searchCancellationTokenSource;
    private IReadOnlyList<TItem> _items = [];
    private IJSObjectReference? _jsModule;
    private TItem? _lastSyncedValue;
    private string? _lastSearchTextParameter;
    private int _activeIndex = -1;
    private bool _disposed;
    private bool _isOpen;
    private bool _searching;
    private bool _selectActiveItemOnBlur;
    private bool _resetResultsAfterParametersSet;
    private string? _lastUserInputSearchText;
    private string? _searchText;
    private int _searchVersion;

    /// <summary>
    ///     Gets or sets the delay before invoking <see cref="ItemsLookupFunc" /> after input changes.
    /// </summary>
    [Parameter]
    public int DebounceMilliseconds { get; set; } = 300;

    /// <summary>
    ///     Gets or sets the text shown when the search returns no items.
    /// </summary>
    [Parameter]
    public string EmptyText { get; set; } = "No results found";

    /// <summary>
    ///     Gets or sets the function used to retrieve suggestions for the current search text.
    /// </summary>
    [Parameter, EditorRequired]
    public Func<string?, CancellationToken, Task<IEnumerable<TItem>>> ItemsLookupFunc { get; set; } = default!;

    /// <summary>
    ///     Gets or sets a function that returns optional supporting text for a suggestion.
    /// </summary>
    [Parameter]
    public Func<TItem, string?>? ItemSupportingTextSelector { get; set; }

    /// <summary>
    ///     Gets or sets the callback invoked after an item is selected.
    /// </summary>
    [Parameter]
    public EventCallback<TItem?> ItemSelectedCallback { get; set; }

    /// <summary>
    ///     Gets or sets the function used to render an item as field text.
    /// </summary>
    [Parameter]
    public Func<TItem, string> ItemTextSelector { get; set; } = item => item?.ToString() ?? string.Empty;

    /// <summary>
    ///     Gets or sets the function used to convert a selected item into a stable native form-post value.
    /// </summary>
    /// <remarks>
    ///     Set this with <see cref="ItemValueParser" /> when the typeahead participates in static SSR or native form posts.
    /// </remarks>
    [Parameter]
    public Func<TItem, string?>? ItemValueSelector { get; set; }

    /// <summary>
    ///     Gets or sets the function used to parse a native form-post value back into a selected item.
    /// </summary>
    [Parameter]
    public Func<string?, TItem?>? ItemValueParser { get; set; }

    /// <summary>
    ///     Gets or sets the text shown while async search is running.
    /// </summary>
    [Parameter]
    public string LoadingText { get; set; } = "Searching...";

    /// <summary>
    ///     Gets or sets the maximum number of results rendered in the popup. Set to <see langword="null" /> to render all lookup results.
    /// </summary>
    [Parameter]
    public int? MaxResults { get; set; } = 50;

    /// <summary>
    ///     Gets or sets the popup menu item appearance.
    /// </summary>
    [Parameter]
    public NTMenuItemAppearance MenuItemAppearance { get; set; }

    /// <summary>
    ///     Gets or sets the minimum number of typed characters before search runs.
    /// </summary>
    [Parameter]
    public int MinimumSearchLength { get; set; } = 1;

    /// <summary>
    ///     Gets or sets whether the selected value is cleared when typed text no longer matches the selected item text.
    /// </summary>
    [Parameter]
    public bool ResetSelectionOnInput { get; set; } = true;

    /// <summary>
    ///     Gets or sets whether Escape clears the typed search text and selected item.
    /// </summary>
    [Parameter]
    public bool ResetValueOnEscape { get; set; } = true;

    /// <summary>
    ///     Gets or sets the template used to render a suggestion row.
    /// </summary>
    [Parameter]
    public RenderFragment<TItem>? ResultTemplate { get; set; }

    /// <summary>
    ///     Gets or sets the typed query text shown in the field. Use <c>@bind-SearchText</c> to observe or control the search text separately from the selected value.
    /// </summary>
    [Parameter]
    public string? SearchText { get; set; }

    /// <summary>
    ///     Gets or sets the callback invoked when <see cref="SearchText" /> changes.
    /// </summary>
    [Parameter]
    public EventCallback<string?> SearchTextChanged { get; set; }

    /// <inheritdoc />
    protected override IEnumerable<string> ExplicitControlAttributeNames => TypeaheadExplicitControlAttributeNames;

    /// <inheritdoc />
    protected override bool HasFloatingValue => !string.IsNullOrEmpty(_searchText);

    /// <inheritdoc />
    protected override string InputIdPrefix => "nt-typeahead";

    private string? ActiveDescendantId => _isOpen && _activeIndex >= 0 && _activeIndex < _items.Count ? GetOptionId(_activeIndex) : null;
    private string FormPostValue => CurrentValue is null ? string.Empty : ItemValueSelector?.Invoke(CurrentValue) ?? FormatValueAsString(CurrentValue) ?? string.Empty;
    private bool HasSearchTextParameter => SearchTextChanged.HasDelegate || SearchText is not null;
    private string ListboxId => $"{InputId}-listbox";
    private string ListboxLabel => string.IsNullOrWhiteSpace(Label) ? FieldIdentifier.FieldName : Label;
    private string TypeaheadControlClass {
        get {
            var cssClass = CssClass;
            return string.IsNullOrEmpty(cssClass) ? "nt-input-control nt-typeahead-control" : $"nt-input-control nt-typeahead-control {cssClass}";
        }
    }

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    /// <inheritdoc />
    public async ValueTask DisposeAsync() {
        _disposed = true;
        await CancelSearchAsync(dispose: true);

        if (_jsModule is not null) {
            try {
                await _jsModule.InvokeVoidAsync("onDispose", Element);
                await _jsModule.DisposeAsync();
            }
            catch (JSDisconnectedException) {
                // JS runtime was disconnected, safe to ignore during disposal.
            }
        }

        _jsModule = null;

        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    protected override void BuildAdditionalRootClasses(StringBuilder builder) {
        builder.Append(" nt-typeahead");
        if (MenuItemAppearance == NTMenuItemAppearance.Condensed) {
            builder.Append(" nt-typeahead-menu-items-condensed");
        }
    }

    /// <inheritdoc />
    protected override TrailingAdornmentState CreateTrailingAdornmentState(bool hasErrorText) {
        if (hasErrorText) {
            return base.CreateTrailingAdornmentState(hasErrorText);
        }

        return new TrailingAdornmentState {
            Icon = TrailingIcon ?? MaterialIcon.Search,
            Class = "nt-input-trailing nt-typeahead-indicator",
            AriaHidden = "true"
        };
    }

    /// <inheritdoc />
    protected override string? FormatValueAsString(TItem? value) => value is null ? null : ItemTextSelector(value);

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender) {
        await base.OnAfterRenderAsync(firstRender);

        if (!firstRender) {
            return;
        }

        try {
            _jsModule = await JSRuntime.ImportIsolatedJs(this, JsModulePath);
            await _jsModule.InvokeVoidAsync("onLoad", Element);
        }
        catch (JSDisconnectedException) {
            // JS runtime was disconnected, safe to ignore during render.
        }
        catch (JSException) {
            // Scrolling enhancement failed. Keep the typeahead usable instead of failing the circuit.
            _jsModule = null;
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();

        if (HasSearchTextParameter && !string.Equals(SearchText, _lastSearchTextParameter, StringComparison.Ordinal)) {
            var isExternalSearchTextChange = !string.Equals(SearchText, _lastUserInputSearchText, StringComparison.Ordinal);
            _searchText = SearchText;
            _lastSearchTextParameter = SearchText;
            if (isExternalSearchTextChange) {
                ResetResults();
            }
        }

        if (!EqualityComparer<TItem?>.Default.Equals(CurrentValue, _lastSyncedValue)) {
            if (!HasSearchTextParameter) {
                _searchText = FormatValueAsString(CurrentValue);
            }

            _lastSyncedValue = CurrentValue;
            ResetResults();
        }
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync() {
        await base.OnParametersSetAsync();

        if (!_resetResultsAfterParametersSet) {
            return;
        }

        _resetResultsAfterParametersSet = false;
        await CancelSearchAsync();
    }

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out TItem? result, [NotNullWhen(false)] out string? validationErrorMessage) {
        if (ItemValueParser is not null) {
            result = ItemValueParser(value);
            validationErrorMessage = null;
            return true;
        }

        if (string.IsNullOrEmpty(value)) {
            result = default;
            validationErrorMessage = null;
            return true;
        }

        result = CurrentValue;
        validationErrorMessage = $"The {DisplayName ?? FieldIdentifier.FieldName} field is not valid.";
        return false;
    }

    private async Task ClearSearchAsync(bool clearValue) {
        await CancelSearchAsync();
        _items = [];
        _activeIndex = -1;
        _isOpen = false;
        _searching = false;

        if (clearValue && !EqualityComparer<TItem?>.Default.Equals(CurrentValue, default)) {
            CurrentValue = default;
            _lastSyncedValue = CurrentValue;
            await BindAfter.InvokeAsync(CurrentValue);
        }
    }

    private async Task CancelSearchAsync(bool dispose = false) {
        var cancellationTokenSource = _searchCancellationTokenSource;
        _searchCancellationTokenSource = null;
        await (cancellationTokenSource?.CancelAsync() ?? Task.CompletedTask);

        if (dispose) {
            cancellationTokenSource?.Dispose();
        }
    }

    private string GetItemSupportingText(TItem item) => ItemSupportingTextSelector?.Invoke(item) ?? string.Empty;
    private string GetOptionId(int index) => $"{InputId}-option-{index}";
    private static string GetOptionClass(bool isActive, bool isSelected) => CssClassBuilder.Create()
        .AddClass("nt-combobox-option")
        .AddClass("nt-combobox-option-active", isActive)
        .AddClass("nt-combobox-option-selected", isSelected)
        .Build()!;

    private async Task OnFocusAsync(FocusEventArgs args) {
        if (!FieldDisabled && !FieldReadOnly && _items.Count > 0) {
            _isOpen = true;
        }

        await Task.CompletedTask;
    }

    private void ResetResults() {
        _items = [];
        _activeIndex = -1;
        _isOpen = false;
        _searching = false;
        _selectActiveItemOnBlur = false;
        _resetResultsAfterParametersSet = true;
    }

    private async Task OnInputAsync(ChangeEventArgs args) {
        if (FieldDisabled || FieldReadOnly) {
            return;
        }

        var searchText = args.Value?.ToString();
        var searchVersion = Interlocked.Increment(ref _searchVersion);
        var searchTextChangedTask = SetSearchTextAsync(searchText);
        if (ResetSelectionOnInput && !string.Equals(_searchText, FormatValueAsString(CurrentValue), StringComparison.Ordinal)) {
            if (!EqualityComparer<TItem?>.Default.Equals(CurrentValue, default)) {
                CurrentValue = default;
                _lastSyncedValue = CurrentValue;
                await BindAfter.InvokeAsync(CurrentValue);
            }
        }

        _ = RunSearchAsync(searchText, searchVersion);
        await searchTextChangedTask;
    }

    private async Task RunSearchAsync(string? searchText, int searchVersion) {
        try {
            await InvokeAsync(() => SearchAsync(searchText, searchVersion));
        }
        catch (Exception exception) {
            await DispatchExceptionAsync(exception);
        }
    }

    private async Task OnKeyDownAsync(KeyboardEventArgs args) {
        if (FieldDisabled || FieldReadOnly) {
            return;
        }

        switch (args.Key) {
            case "ArrowDown":
                await MoveActiveItemAsync(1);
                break;
            case "ArrowUp":
                await MoveActiveItemAsync(-1);
                break;
            case "Enter":
                if (_isOpen && _activeIndex >= 0 && _activeIndex < _items.Count) {
                    await SelectItemAsync(_items[_activeIndex]);
                }
                break;
            case "Tab":
                if (_isOpen && _activeIndex >= 0 && _activeIndex < _items.Count) {
                    _selectActiveItemOnBlur = true;
                }
                break;
            case "Escape":
                if (ResetValueOnEscape) {
                    await SetSearchTextAsync(null);
                    await ClearSearchAsync(clearValue: true);
                }
                else {
                    await ClearSearchAsync(clearValue: false);
                }
                break;
        }
    }

    /// <inheritdoc />
    protected override async Task OnBlurAsync(FocusEventArgs args) {
        if (_selectActiveItemOnBlur && _activeIndex >= 0 && _activeIndex < _items.Count) {
            var activeItem = _items[_activeIndex];
            _selectActiveItemOnBlur = false;
            await SelectItemAsync(activeItem);
        }
        else {
            _selectActiveItemOnBlur = false;
        }

        await base.OnBlurAsync(args);
        if (!_isOpen) {
            return;
        }

        _isOpen = false;
        await InvokeAsync(StateHasChanged);
    }

    private async Task MoveActiveItemAsync(int delta) {
        if (_items.Count == 0) {
            return;
        }

        _isOpen = true;
        _activeIndex = _activeIndex < 0
            ? delta > 0 ? 0 : _items.Count - 1
            : (_activeIndex + delta + _items.Count) % _items.Count;
        await ScrollActiveOptionIntoViewAsync();
    }

    private async Task ScrollActiveOptionIntoViewAsync() {
        if (_jsModule is null || ActiveDescendantId is null) {
            return;
        }

        try {
            await _jsModule.InvokeVoidAsync("scrollActiveOptionIntoView", Element, ActiveDescendantId);
        }
        catch (JSDisconnectedException) {
            // JS runtime was disconnected, safe to ignore.
        }
        catch (JSException) {
            // Scrolling is progressive enhancement; keep keyboard navigation usable.
        }
    }

    private async Task SetSearchTextAsync(string? searchText) {
        _searchText = searchText;
        _lastUserInputSearchText = searchText;

        if (SearchTextChanged.HasDelegate) {
            await SearchTextChanged.InvokeAsync(searchText);
        }
    }

    private async Task SearchAsync(string? searchText, int searchVersion) {
        await CancelSearchAsync();

        if (string.IsNullOrWhiteSpace(searchText) || searchText.Length < MinimumSearchLength) {
            _items = [];
            _activeIndex = -1;
            _isOpen = false;
            _searching = false;
            return;
        }

        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        _searchCancellationTokenSource = cancellationTokenSource;

        try {
            if (DebounceMilliseconds > 0) {
                await Task.Delay(DebounceMilliseconds, cancellationToken);
            }

            if (cancellationToken.IsCancellationRequested || searchVersion != Volatile.Read(ref _searchVersion)) {
                return;
            }

            _searching = true;
            _isOpen = true;
            _items = [];
            _activeIndex = -1;
            await InvokeAsync(StateHasChanged);

            var results = await ItemsLookupFunc(searchText, cancellationToken);
            if (cancellationToken.IsCancellationRequested || searchVersion != Volatile.Read(ref _searchVersion)) {
                return;
            }

            _items = ApplyMaxResults(results);
            _activeIndex = _items.Count > 0 ? 0 : -1;
            _isOpen = true;
        }
        catch (OperationCanceledException) { }
        finally {
            var isCurrentSearch = ReferenceEquals(_searchCancellationTokenSource, cancellationTokenSource);
            if (isCurrentSearch) {
                _searchCancellationTokenSource = null;
            }

            cancellationTokenSource.Dispose();

            if (isCurrentSearch && !_disposed) {
                _searching = false;
                await InvokeAsync(StateHasChanged);
            }
        }
    }

    private async Task SelectItemAsync(TItem item) {
        await CancelSearchAsync();
        CurrentValue = item;
        _lastSyncedValue = CurrentValue;
        await SetSearchTextAsync(ItemTextSelector(item));
        _items = [];
        _activeIndex = -1;
        _isOpen = false;
        _searching = false;

        await ItemSelectedCallback.InvokeAsync(item);
        await BindAfter.InvokeAsync(CurrentValue);
        await InvokeAsync(StateHasChanged);
    }

    private IReadOnlyList<TItem> ApplyMaxResults(IEnumerable<TItem>? results) {
        if (results is null) {
            return [];
        }

        if (MaxResults is null) {
            return results.ToArray();
        }

        return MaxResults > 0 ? results.Take(MaxResults.Value).ToArray() : [];
    }
}
