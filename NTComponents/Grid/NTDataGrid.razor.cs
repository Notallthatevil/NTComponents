using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;
using NTComponents.Core;
using NTComponents.Ext;
using NTComponents.Virtualization;
using System.Globalization;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
/// Density options for <see cref="NTDataGrid{TItem}" />.
/// </summary>
public enum NTDataGridDensity {
    /// <summary>
    /// Default row density.
    /// </summary>
    Standard,

    /// <summary>
    /// Compact row density.
    /// </summary>
    Compact
}

/// <summary>
/// Appearance options for <see cref="NTDataGrid{TItem}" />.
/// </summary>
public enum NTDataGridAppearance {
    /// <summary>
    /// Default grid appearance.
    /// </summary>
    Default,

    /// <summary>
    /// Shows alternating row backgrounds.
    /// </summary>
    Striped
}

/// <summary>
/// A Material 3 aligned data grid for direct, provider, paginated, and virtualized data.
/// </summary>
[CascadingTypeParameter(nameof(TItem))]
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.ProgressivelyEnhanced,
    CompatibilitySummary = "Renders grid markup in static SSR and enhances sorting, paging, and virtualization interactively.",
    CompatibilityDetails = "Static SSR can emit the current table rows and query-link sorting or paging. Interactive sorting, paging, row callbacks, and Virtualize mode require an interactive render mode or browser enhancement.")]
public partial class NTDataGrid<TItem> : IDisposable where TItem : class {
    private const string _rowClass = "nt-data-grid-row";
    private const string _clickableRowClass = "nt-data-grid-row nt-data-grid-row-clickable";
    private const string _stripedOddRowClass = "nt-data-grid-row nt-data-grid-row-striped-odd";
    private const string _stripedEvenRowClass = "nt-data-grid-row nt-data-grid-row-striped-even";
    private const string _clickableStripedOddRowClass = "nt-data-grid-row nt-data-grid-row-clickable nt-data-grid-row-striped-odd";
    private const string _clickableStripedEvenRowClass = "nt-data-grid-row nt-data-grid-row-clickable nt-data-grid-row-striped-even";
    private const string _emptyRowClass = "nt-data-grid-row nt-data-grid-row-empty";
    private readonly List<NTDataGridColumn<TItem>> _columns = [];
    private readonly List<TItem> _loadedItems = [];
    private readonly List<NTSortDescriptor> _sorts = [];
    private readonly Dictionary<object, int> _rowIndices = [];
    private readonly CancellationTokenSource _componentCancellation = new();
    private IReadOnlyDictionary<string, object>? _rootAttributes;
    private IQueryable<TItem>? _previousItems;
    private NTDataGridItemsProvider<TItem>? _previousItemsProvider;
    private Func<TItem, object>? _previousRowKey;
    private string? _class;
    private string? _previousDataState;
    private string? _style;
    private bool _hasDataStateSnapshot;
    private bool _refreshQueued;
    private bool _sortsInitialized;
    private bool _hasVirtualizationItemSizeParameter;
    private int _currentPageIndex;
    private int _currentPageSize;
    private int _totalItemCount;
    private int _virtualizeVersion;

    /// <summary>
    /// Gets or sets the direct item source.
    /// </summary>
    [Parameter]
    public IQueryable<TItem>? Items { get; set; }

    /// <summary>
    /// Gets or sets the provider item source.
    /// </summary>
    [Parameter]
    public NTDataGridItemsProvider<TItem>? ItemsProvider { get; set; }

    /// <summary>
    /// Gets or sets the grid columns.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets the table caption.
    /// </summary>
    [Parameter]
    public string? Caption { get; set; }

    /// <summary>
    /// Gets or sets the message rendered when no items are available.
    /// </summary>
    [Parameter]
    public string EmptyText { get; set; } = "No data";

    /// <summary>
    /// Gets or sets whether pagination controls are shown.
    /// </summary>
    [Parameter]
    public bool ShowPagination { get; set; }

    /// <summary>
    /// Gets or sets the current zero-based page index.
    /// </summary>
    [Parameter]
    public int PageIndex { get; set; }

    /// <summary>
    /// Invoked when <see cref="PageIndex" /> changes.
    /// </summary>
    [Parameter]
    public EventCallback<int> PageIndexChanged { get; set; }

    /// <summary>
    /// Gets or sets the current page size.
    /// </summary>
    [Parameter]
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Invoked when <see cref="PageSize" /> changes.
    /// </summary>
    [Parameter]
    public EventCallback<int> PageSizeChanged { get; set; }

    /// <summary>
    /// Gets or sets the available page sizes.
    /// </summary>
    [Parameter]
    public IReadOnlyList<int> PageSizeOptions { get; set; } = [10, 25, 50];

    /// <summary>
    /// Invoked when the resolved total item count changes.
    /// </summary>
    [Parameter]
    public EventCallback<int> TotalItemCountChanged { get; set; }

    /// <summary>
    /// Gets or sets whether the body is rendered through Blazor virtualization.
    /// </summary>
    [Parameter]
    public bool Virtualize { get; set; }

    /// <summary>
    /// Gets or sets the approximate virtualized row height in pixels.
    /// </summary>
    [Parameter]
    public float VirtualizationItemSize { get; set; } = 36;

    /// <summary>
    /// Gets or sets the number of extra virtualized rows requested around the visible range.
    /// </summary>
    [Parameter]
    public int VirtualizationOverscanCount { get; set; } = 3;

    /// <summary>
    /// Gets or sets how many additional visible windows of placeholder rows are rendered after loaded virtualized rows.
    /// </summary>
    [Parameter]
    public int VirtualizationPlaceholderPreloadWindowCount { get; set; } = 1;

    /// <summary>
    /// Gets or sets how many additional visible windows of virtualized rows are fetched in the background.
    /// </summary>
    [Parameter]
    public int VirtualizationBackgroundPreloadWindowCount { get; set; } = 1;

    /// <summary>
    /// Gets or sets the maximum number of virtualized rows cached by the grid.
    /// </summary>
    [Parameter]
    public int VirtualizationMaxCachedItemCount { get; set; } = 1_000;

    /// <summary>
    /// Gets or sets whether cached virtualized rows are re-fetched in the background when they become visible again.
    /// </summary>
    [Parameter]
    public bool VirtualizationRevalidateCachedItems { get; set; }

    /// <summary>
    /// Gets or sets the first server-rendered request size for provider-backed virtual grids.
    /// </summary>
    [Parameter]
    public int VirtualizationInitialItemCount { get; set; } = 20;

    /// <summary>
    /// Gets or sets the query parameter prefix used for sort and page state.
    /// </summary>
    [Parameter]
    public string QueryParameterPrefix { get; set; } = "ntdg";

    /// <summary>
    /// Gets or sets whether sorting can be applied to more than one column at a time.
    /// </summary>
    [Parameter]
    public bool AllowMultiSort { get; set; } = true;

    /// <summary>
    /// Gets or sets the grid density.
    /// </summary>
    [Parameter]
    public NTDataGridDensity Density { get; set; }

    /// <summary>
    /// Gets or sets the grid appearance.
    /// </summary>
    [Parameter]
    public NTDataGridAppearance Appearance { get; set; }

    /// <summary>
    /// Gets or sets a row key selector.
    /// </summary>
    [Parameter]
    public Func<TItem, object>? RowKey { get; set; } = item => (object?)item ?? DBNull.Value;

    /// <summary>
    /// Invoked when a data row is clicked.
    /// </summary>
    [Parameter]
    public EventCallback<TItem> OnRowClicked { get; set; }

    /// <summary>
    /// Gets or sets the pagination navigation label.
    /// </summary>
    [Parameter]
    public string PaginationAriaLabel { get; set; } = "Data grid pagination";

    /// <summary>
    /// Gets or sets unmatched attributes for the grid root.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    [Inject]
    private NavigationManager _navManager { get; set; } = default!;

    [Inject]
    private IJSRuntime _jsRuntime { get; set; } = default!;

    private int VisibleColumnCount => Math.Max(_columns.Count, 1);

    private int CurrentPageIndex => Math.Max(0, _currentPageIndex);

    private int CurrentPageSize => Math.Max(1, _currentPageSize);

    private int PageCount => Math.Max(1, (int)Math.Ceiling(_totalItemCount / (double)CurrentPageSize));

    private bool CanGoToPreviousPage => ShowPagination && CurrentPageIndex > 0;

    private bool CanGoToNextPage => ShowPagination && CurrentPageIndex < PageCount - 1;

    private bool HasRowClickCallback => OnRowClicked.HasDelegate;

    private bool HasStripedRows => Appearance == NTDataGridAppearance.Striped;

    private float DefaultVirtualizationItemSize => Density == NTDataGridDensity.Compact ? 28 : 36;

    private float ResolvedVirtualizationItemSize => Math.Max(1, _hasVirtualizationItemSizeParameter ? VirtualizationItemSize : DefaultVirtualizationItemSize);

    private string RootClass => CssClassBuilder.Create("nt-data-grid")
        .AddClass("nt-data-grid-compact", Density == NTDataGridDensity.Compact)
        .AddClass("nt-data-grid-striped", HasStripedRows)
        .AddClass("nt-data-grid-virtualized", Virtualize)
        .AddClass(_class, !string.IsNullOrWhiteSpace(_class))
        .Build();

    private string? ScrollStyle => Virtualize ? $"max-height: {Math.Max(1, VirtualizationInitialItemCount) * ResolvedVirtualizationItemSize}px;" : null;

    /// <summary>
    /// Refreshes the data grid by re-resolving the current data source and updating the rendered rows.
    /// </summary>
    public async Task RefreshDataGridAsync(CancellationToken cancellationToken = default) {
        if (Virtualize) {
            ResetVirtualizedData();
        }
        else {
            await RefreshDataAsync(cancellationToken.CanBeCanceled ? cancellationToken : _componentCancellation.Token);
        }
        await InvokeAsync(StateHasChanged);
    }

    /// <inheritdoc />
    public override Task SetParametersAsync(ParameterView parameters) {
        _hasVirtualizationItemSizeParameter = parameters.TryGetValue<float>(nameof(VirtualizationItemSize), out _);
        return base.SetParametersAsync(parameters);
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync() {
        ValidateConfiguration();
        CaptureRootAttributes();
        _currentPageSize = PageSize > 0 ? PageSize : 10;
        _currentPageIndex = Math.Max(0, PageIndex);
        ApplyQueryState();
        var dataStateChanged = CaptureDataStateChanged();

        if (Virtualize) {
            if (dataStateChanged) {
                ResetVirtualizedData();
            }
        }
        else if (_columns.Count > 0 && dataStateChanged && !_refreshQueued) {
            await RefreshDataAsync(_componentCancellation.Token);
        }
    }

    internal void RegisterColumn(NTDataGridColumn<TItem> column) {
        if (_columns.Contains(column)) {
            return;
        }

        column.Sequence = _columns.Count;
        _columns.Add(column);
        TryAddInitialSort(column);
        QueueRefresh();
    }

    internal void NotifyColumnChanged(NTDataGridColumn<TItem> column, bool sortStateChanged) {
        if (!_columns.Contains(column)) {
            return;
        }

        if (sortStateChanged) {
            ResetSorts();
        }

        QueueRefresh();
    }

    internal void UnregisterColumn(NTDataGridColumn<TItem> column) {
        if (_columns.Remove(column)) {
            ReindexColumns();
            ResetSorts();
            QueueRefresh();
        }
    }

    private void QueueRefresh() {
        if (_refreshQueued) {
            return;
        }

        _refreshQueued = true;
        _ = InvokeAsync(async () => {
            _refreshQueued = false;
            InitializeSorts();
            if (!Virtualize) {
                await RefreshDataAsync(_componentCancellation.Token);
            }
            else {
                ResetVirtualizedData();
            }
            StateHasChanged();
        });
    }

    private void ReindexColumns() {
        for (var i = 0; i < _columns.Count; i++) {
            _columns[i].Sequence = i;
        }
    }

    private void ValidateConfiguration() {
        if (Items is not null && ItemsProvider is not null) {
            throw new InvalidOperationException($"{nameof(NTDataGrid<TItem>)} requires either {nameof(Items)} or {nameof(ItemsProvider)}, not both.");
        }

        if (Items is null && ItemsProvider is null) {
            throw new InvalidOperationException($"{nameof(NTDataGrid<TItem>)} requires {nameof(Items)} or {nameof(ItemsProvider)}.");
        }

        if (Virtualize && ShowPagination) {
            throw new InvalidOperationException($"{nameof(NTDataGrid<TItem>)} does not support using {nameof(Virtualize)} and {nameof(ShowPagination)} together.");
        }
    }

    private async Task RefreshDataAsync(CancellationToken cancellationToken) {
        InitializeSorts();
        var startIndex = ShowPagination ? CurrentPageIndex * CurrentPageSize : 0;
        int? count = ShowPagination || ItemsProvider is not null ? CurrentPageSize : null;
        var result = await ResolveItemsAsync(startIndex, count, cancellationToken);
        _loadedItems.Clear();
        _loadedItems.AddRange(result.Items);
        _rowIndices.Clear();
        RegisterRowIndices(startIndex, _loadedItems);
        await SetTotalItemCountAsync(result.TotalItemCount);
    }

    private async ValueTask<TnTItemsProviderResult<TItem>> ProvideVirtualizedItemsAsync(NTVirtualizeItemsProviderRequest<TItem> request) {
        InitializeSorts();
        var requestedCount = request.Count > 0 ? request.Count : VirtualizationInitialItemCount;
        var result = await ResolveItemsAsync(request.StartIndex, requestedCount, request.CancellationToken);
        RegisterRowIndices(request.StartIndex, result.Items);
        TrimVirtualizedRowIndices(request.StartIndex, result.Items.Count);
        await SetTotalItemCountAsync(result.TotalItemCount);
        return new TnTItemsProviderResult<TItem>(result.Items, result.TotalItemCount);
    }

    private async ValueTask<NTItemsProviderResult<TItem>> ResolveItemsAsync(int startIndex, int? count, CancellationToken cancellationToken) {
        if (ItemsProvider is not null) {
            return await ItemsProvider(new NTDataGridItemsProviderRequest<TItem> {
                StartIndex = Math.Max(0, startIndex),
                Count = count,
                CancellationToken = cancellationToken,
                Sorts = GetProviderSortDescriptors()
            });
        }

        if (Items is null) {
            return new NTItemsProviderResult<TItem>([], 0);
        }

        if (TryApplyQueryableSorts(Items, out var query)) {
            var totalCount = query.Count();
            var page = query.Skip(Math.Max(0, startIndex));
            if (count is > 0) {
                page = page.Take(count.Value);
            }

            return new NTItemsProviderResult<TItem>(page.ToArray(), totalCount);
        }

        var localItems = ApplyLocalSorts(Items).ToArray();
        var localStartIndex = Math.Min(Math.Max(0, startIndex), localItems.Length);
        var localCount = count is > 0 ? Math.Min(count.Value, localItems.Length - localStartIndex) : localItems.Length - localStartIndex;
        var localPage = localStartIndex == 0 && localCount == localItems.Length
            ? localItems
            : localItems.AsSpan(localStartIndex, localCount).ToArray();
        return new NTItemsProviderResult<TItem>(localPage, localItems.Length);
    }

    private bool TryApplyQueryableSorts(IQueryable<TItem> source, out IQueryable<TItem> query) {
        query = source;
        IOrderedQueryable<TItem>? ordered = null;
        foreach (var sort in _sorts) {
            var column = _columns.FirstOrDefault(candidate => string.Equals(candidate.SortPropertyName, sort.PropertyName, StringComparison.Ordinal));
            if (column is null) {
                continue;
            }

            ordered = column.ApplyQueryableSort(ordered ?? source, sort.Direction, ordered is not null);
            if (ordered is null) {
                return false;
            }
        }

        query = ordered ?? source;
        return true;
    }

    private IEnumerable<TItem> ApplyLocalSorts(IEnumerable<TItem> source) {
        IOrderedEnumerable<TItem>? ordered = null;
        foreach (var sort in _sorts) {
            var column = _columns.FirstOrDefault(candidate => string.Equals(candidate.SortPropertyName, sort.PropertyName, StringComparison.Ordinal));
            if (column is null) {
                continue;
            }

            var customSort = column.ApplyLocalSort(ordered ?? source, sort.Direction, ordered is not null);
            ordered = customSort ?? (ordered is null
                ? sort.Direction == SortDirection.Descending
                    ? source.OrderByDescending(column.GetSortValue, NTDataGridObjectComparer.Instance)
                    : source.OrderBy(column.GetSortValue, NTDataGridObjectComparer.Instance)
                : sort.Direction == SortDirection.Descending
                    ? ordered.ThenByDescending(column.GetSortValue, NTDataGridObjectComparer.Instance)
                    : ordered.ThenBy(column.GetSortValue, NTDataGridObjectComparer.Instance));
        }

        return ordered ?? source;
    }

    private IReadOnlyList<NTSortDescriptor> GetProviderSortDescriptors() => [.. _sorts.SelectMany(sort => {
        var column = _columns.FirstOrDefault(candidate => string.Equals(candidate.SortPropertyName, sort.PropertyName, StringComparison.Ordinal));
        return column?.GetSortDescriptors(sort.Direction) ?? [sort];
    })];

    private async Task SetTotalItemCountAsync(int totalItemCount) {
        totalItemCount = Math.Max(0, totalItemCount);
        if (_totalItemCount == totalItemCount) {
            return;
        }

        _totalItemCount = totalItemCount;
        if (TotalItemCountChanged.HasDelegate) {
            await TotalItemCountChanged.InvokeAsync(totalItemCount);
        }
    }

    private void InitializeSorts() {
        if (_sortsInitialized || _columns.Count == 0) {
            return;
        }

        _sortsInitialized = true;
        foreach (var column in _columns) {
            TryAddInitialSort(column);
        }
    }

    private void ResetSorts() {
        _sortsInitialized = false;
        _sorts.Clear();
        ApplyQueryState();
        InitializeSorts();
    }

    private void TryAddInitialSort(NTDataGridColumn<TItem> column) {
        if (!_sortsInitialized || column.InitialSortDirection is null || !CanSort(column)) {
            return;
        }

        if (!AllowMultiSort && _sorts.Count > 0) {
            return;
        }

        if (_sorts.Any(sort => string.Equals(sort.PropertyName, column.SortPropertyName, StringComparison.Ordinal))) {
            return;
        }

        _sorts.Add(new NTSortDescriptor(column.SortPropertyName!, column.InitialSortDirection.Value));
    }

    private bool CanSort(NTDataGridColumn<TItem> column) => column.CanSort;

    private async Task SortByColumnAsync(NTDataGridColumn<TItem> column) {
        if (!CanSort(column)) {
            return;
        }

        var nextSorts = GetNextSorts(column);
        _sorts.Clear();
        _sorts.AddRange(nextSorts);

        _currentPageIndex = 0;
        await PageIndexChanged.InvokeAsync(_currentPageIndex);
        if (Virtualize) {
            _virtualizeVersion++;
            StateHasChanged();
        }
        else {
            await RefreshDataAsync(_componentCancellation.Token);
        }

        await UpdateBrowserUriAsync();
    }

    private async Task GoToPageAsync(int pageIndex) {
        if (!ShowPagination) {
            return;
        }

        var nextPageIndex = Math.Clamp(pageIndex, 0, PageCount - 1);
        if (nextPageIndex == CurrentPageIndex) {
            return;
        }

        _currentPageIndex = nextPageIndex;
        await PageIndexChanged.InvokeAsync(nextPageIndex);
        await RefreshDataAsync(_componentCancellation.Token);
        await UpdateBrowserUriAsync();
    }

    private async Task ChangePageSizeAsync(ChangeEventArgs args) {
        if (!int.TryParse(Convert.ToString(args.Value, CultureInfo.InvariantCulture), out var pageSize) || pageSize <= 0) {
            return;
        }

        _currentPageSize = pageSize;
        _currentPageIndex = 0;
        await PageSizeChanged.InvokeAsync(pageSize);
        await PageIndexChanged.InvokeAsync(0);
        await RefreshDataAsync(_componentCancellation.Token);
        await UpdateBrowserUriAsync();
    }

    private ValueTask UpdateBrowserUriAsync() => RendererInfo.IsInteractive ? _jsRuntime.UpdateUriAsync(BuildPageHref(CurrentPageIndex)) : ValueTask.CompletedTask;

    private string BuildSortHref(NTDataGridColumn<TItem> column) {
        var nextSorts = GetNextSorts(column);
        return BuildHref(CurrentPageIndex, nextSorts);
    }

    private string BuildPageHref(int pageIndex) => BuildHref(Math.Clamp(pageIndex, 0, PageCount - 1), _sorts);

    private string BuildHref(int pageIndex, IReadOnlyList<NTSortDescriptor> sorts) {
        var uri = _navManager.ToAbsoluteUri(_navManager.Uri);
        var query = ParseQuery(uri.Query);
        query[GetQueryName("page")] = (Math.Max(0, pageIndex) + 1).ToString(CultureInfo.InvariantCulture);
        query.Remove(GetQueryName("pageSize"));

        var sortValue = SerializeSorts(sorts);
        if (string.IsNullOrWhiteSpace(sortValue)) {
            query.Remove(GetQueryName("sort"));
        }
        else {
            query[GetQueryName("sort")] = sortValue;
        }

        var queryString = string.Join("&", query.Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));
        return uri.GetLeftPart(UriPartial.Path) + (string.IsNullOrEmpty(queryString) ? string.Empty : "?" + queryString);
    }

    private IReadOnlyList<NTSortDescriptor> GetNextSorts(NTDataGridColumn<TItem> column) {
        if (!CanSort(column)) {
            return _sorts;
        }

        return ApplySortTransition(_sorts, column.SortPropertyName!, column.DefaultSortDirection, AllowMultiSort);
    }

    private static IReadOnlyList<NTSortDescriptor> ApplySortTransition(IEnumerable<NTSortDescriptor> currentSorts, string propertyName, SortDirection defaultDirection, bool allowMultiSort) {
        var nextSorts = currentSorts.ToList();
        var currentIndex = nextSorts.FindIndex(sort => string.Equals(sort.PropertyName, propertyName, StringComparison.Ordinal));
        if (!allowMultiSort) {
            SortDirection? currentDirection = currentIndex < 0 ? null : nextSorts[currentIndex].Direction;
            var nextDirection = GetNextSortDirection(currentDirection, defaultDirection);
            return nextDirection is null ? [] : [new NTSortDescriptor(propertyName, nextDirection.Value)];
        }

        if (currentIndex < 0) {
            nextSorts.Add(new NTSortDescriptor(propertyName, defaultDirection));
        }
        else {
            var nextDirection = GetNextSortDirection(nextSorts[currentIndex].Direction, defaultDirection);
            if (nextDirection is null) {
                nextSorts.RemoveAt(currentIndex);
            }
            else {
                nextSorts[currentIndex] = nextSorts[currentIndex] with { Direction = nextDirection.Value };
            }
        }

        return nextSorts;
    }

    private static SortDirection? GetNextSortDirection(SortDirection? currentDirection, SortDirection defaultDirection) {
        if (currentDirection is null) {
            return defaultDirection;
        }

        return currentDirection == defaultDirection
            ? defaultDirection == SortDirection.Ascending ? SortDirection.Descending : SortDirection.Ascending
            : null;
    }

    private void ApplyQueryState() {
        var uri = _navManager.ToAbsoluteUri(_navManager.Uri);
        var query = ParseQuery(uri.Query);
        if (query.TryGetValue(GetQueryName("page"), out var pageValue) && int.TryParse(pageValue, out var pageNumber)) {
            _currentPageIndex = Math.Max(1, pageNumber) - 1;
        }

        if (query.TryGetValue(GetQueryName("sort"), out var sortValue)) {
            _sorts.Clear();
            _sorts.AddRange(NormalizeSorts(ParseSorts(sortValue)));
            _sortsInitialized = true;
        }
    }

    private string GetQueryName(string name) => string.IsNullOrWhiteSpace(QueryParameterPrefix) ? name : $"{QueryParameterPrefix}-{name}";

    private static Dictionary<string, string> ParseQuery(string query) {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var trimmed = query.TrimStart('?');
        if (string.IsNullOrWhiteSpace(trimmed)) {
            return result;
        }

        foreach (var part in trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries)) {
            var split = part.Split('=', 2);
            var key = Uri.UnescapeDataString(split[0].Replace("+", " ", StringComparison.Ordinal));
            var value = split.Length > 1 ? Uri.UnescapeDataString(split[1].Replace("+", " ", StringComparison.Ordinal)) : string.Empty;
            result[key] = value;
        }

        return result;
    }

    private static IReadOnlyList<NTSortDescriptor> ParseSorts(string value) {
        return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => part.Split(':', 2, StringSplitOptions.TrimEntries))
            .Where(split => split.Length > 0 && !string.IsNullOrWhiteSpace(split[0]))
            .Select(split => new NTSortDescriptor(split[0], split.Length > 1 && split[1].Equals("desc", StringComparison.OrdinalIgnoreCase) ? SortDirection.Descending : SortDirection.Ascending))
            .ToArray();
    }

    private IReadOnlyList<NTSortDescriptor> NormalizeSorts(IEnumerable<NTSortDescriptor> sorts) => AllowMultiSort ? sorts.ToArray() : sorts.Take(1).ToArray();

    private static string SerializeSorts(IEnumerable<NTSortDescriptor> sorts) =>
        string.Join(",", sorts.Select(sort => $"{sort.PropertyName}:{(sort.Direction == SortDirection.Descending ? "desc" : "asc")}"));

    private string GetSortLinkClass(NTDataGridColumn<TItem> column) => CssClassBuilder.Create("nt-data-grid-sort-link")
        .AddClass("nt-data-grid-sort-link-sorted", GetSortDirection(column) is not null)
        .AddClass("nt-data-grid-sort-link-multi-sorted", ShouldShowSortOrder(GetSortOrder(column)))
        .Build();

    private string? GetColumnStyle(NTDataGridColumn<TItem> column) {
        var declarations = new List<string>();
        AddDeclaration(declarations, "width", column.Width);
        AddDeclaration(declarations, "min-width", column.MinWidth);
        AddDeclaration(declarations, "max-width", column.MaxWidth);
        return declarations.Count == 0 ? null : string.Join(" ", declarations);
    }

    private static void AddDeclaration(List<string> declarations, string propertyName, string? value) {
        if (!string.IsNullOrWhiteSpace(value)) {
            declarations.Add($"{propertyName}: {value};");
        }
    }

    private string? GetAriaSort(NTDataGridColumn<TItem> column) {
        var direction = GetSortDirection(column);
        return direction switch {
            SortDirection.Ascending => "ascending",
            SortDirection.Descending => "descending",
            _ => null
        };
    }

    private SortDirection? GetSortDirection(NTDataGridColumn<TItem> column) =>
        _sorts.FirstOrDefault(sort => string.Equals(sort.PropertyName, column.SortPropertyName, StringComparison.Ordinal))?.Direction;

    private int? GetSortOrder(NTDataGridColumn<TItem> column) {
        var sortIndex = _sorts.FindIndex(sort => string.Equals(sort.PropertyName, column.SortPropertyName, StringComparison.Ordinal));
        return sortIndex < 0 ? null : sortIndex + 1;
    }

    private bool ShouldShowSortOrder(int? sortOrder) => _sorts.Count > 1 && sortOrder is not null;

    private static string GetSortIndicatorClass(SortDirection sortDirection) => CssClassBuilder.Create("nt-data-grid-sort-indicator")
        .AddClass("nt-data-grid-sort-indicator-ascending", sortDirection == SortDirection.Ascending)
        .AddClass("nt-data-grid-sort-indicator-descending", sortDirection == SortDirection.Descending)
        .Build();

    private static string GetSortIndicator() => "arrow_drop_down";

    private IEnumerable<int> GetVisiblePageIndices() {
        const int maxPagesToShow = 5;
        var totalPages = PageCount;
        var pagesToShow = Math.Min(maxPagesToShow, totalPages);
        var startIndex = Math.Max(0, CurrentPageIndex - pagesToShow / 2);
        var endIndex = Math.Min(totalPages - 1, startIndex + pagesToShow - 1);
        if (endIndex - startIndex + 1 < pagesToShow) {
            startIndex = Math.Max(0, endIndex - pagesToShow + 1);
        }

        for (var pageIndex = startIndex; pageIndex <= endIndex; pageIndex++) {
            yield return pageIndex;
        }
    }

    private string GetPaginationButtonClass(bool disabled, bool currentPage, string? roleClass = null) => CssClassBuilder.Create("pagination-btn")
        .AddClass(roleClass, !string.IsNullOrWhiteSpace(roleClass))
        .AddClass("current-page", currentPage)
        .AddClass("tnt-disabled", disabled)
        .Build();

    private IReadOnlyList<int> GetResolvedPageSizeOptions() {
        var options = PageSizeOptions
            .Where(option => option > 0)
            .Append(CurrentPageSize)
            .Distinct()
            .OrderBy(option => option)
            .ToArray();

        return options.Length == 0 ? [CurrentPageSize] : options;
    }

    private string GetRowClass(TItem item) {
        if (!HasStripedRows || !_rowIndices.TryGetValue(GetRowIdentity(item), out var rowIndex)) {
            return HasRowClickCallback ? _clickableRowClass : _rowClass;
        }

        return (HasRowClickCallback, rowIndex % 2 == 0) switch {
            (true, true) => _clickableStripedOddRowClass,
            (true, false) => _clickableStripedEvenRowClass,
            (false, true) => _stripedOddRowClass,
            _ => _stripedEvenRowClass
        };
    }

    private object GetRowIdentity(TItem item) => RowKey?.Invoke(item) ?? (object?)item ?? DBNull.Value;

    private void RegisterRowIndices(int startIndex, IReadOnlyCollection<TItem> items) {
        var index = Math.Max(0, startIndex);
        foreach (var item in items) {
            _rowIndices[GetRowIdentity(item)] = index++;
        }
    }

    private void TrimVirtualizedRowIndices(int activeStartIndex, int activeItemCount) {
        var maxRowIndexCount = Math.Max(Math.Max(1, activeItemCount), VirtualizationMaxCachedItemCount);
        if (_rowIndices.Count <= maxRowIndexCount) {
            return;
        }

        var activeEndIndex = Math.Max(0, activeStartIndex) + Math.Max(0, activeItemCount);
        var activeCenterIndex = activeStartIndex + Math.Max(0, activeItemCount) / 2;
        var keysToRemove = _rowIndices
            .Where(pair => pair.Value < activeStartIndex || pair.Value >= activeEndIndex)
            .OrderByDescending(pair => Math.Abs(pair.Value - activeCenterIndex))
            .Select(pair => pair.Key)
            .Take(_rowIndices.Count - maxRowIndexCount)
            .ToArray();

        foreach (var key in keysToRemove) {
            _rowIndices.Remove(key);
        }
    }

    private void ResetVirtualizedData() {
        _rowIndices.Clear();
        _loadedItems.Clear();
        _totalItemCount = 0;
        _virtualizeVersion++;
    }

    private void CaptureRootAttributes() {
        _class = AdditionalAttributes != null && AdditionalAttributes.TryGetValue("class", out var classValue) ? Convert.ToString(classValue, CultureInfo.InvariantCulture) : null;
        _style = AdditionalAttributes != null && AdditionalAttributes.TryGetValue("style", out var styleValue) ? Convert.ToString(styleValue, CultureInfo.InvariantCulture) : null;
        _rootAttributes = AdditionalAttributes?
            .Where(attribute => !attribute.Key.Equals("class", StringComparison.OrdinalIgnoreCase) && !attribute.Key.Equals("style", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(attribute => attribute.Key, attribute => attribute.Value, StringComparer.OrdinalIgnoreCase);
    }

    private bool CaptureDataStateChanged() {
        var state = string.Join("|", CurrentPageIndex, CurrentPageSize, ShowPagination, Virtualize, AllowMultiSort, _navManager.Uri, SerializeSorts(_sorts));
        var changed = !_hasDataStateSnapshot
            || !ReferenceEquals(Items, _previousItems)
            || ItemsProvider != _previousItemsProvider
            || RowKey != _previousRowKey
            || !string.Equals(_previousDataState, state, StringComparison.Ordinal);

        _hasDataStateSnapshot = true;
        _previousItems = Items;
        _previousItemsProvider = ItemsProvider;
        _previousRowKey = RowKey;
        _previousDataState = state;
        return changed;
    }

    private Task InvokeRowClickedAsync(TItem item) => HasRowClickCallback ? OnRowClicked.InvokeAsync(item) : Task.CompletedTask;

    private Task InvokeRowClickedFromKeyboardAsync(TItem item, KeyboardEventArgs args) =>
        args.Key is "Enter" or " " or "Spacebar" ? InvokeRowClickedAsync(item) : Task.CompletedTask;

    private RenderFragment RenderRow(TItem item) => builder => {
        builder.OpenElement(0, "tr");
        builder.SetKey(RowKey?.Invoke(item) ?? item);
        builder.AddAttribute(1, "class", GetRowClass(item));
        if (HasRowClickCallback) {
            builder.AddAttribute(2, "tabindex", "0");
            builder.AddAttribute(3, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => InvokeRowClickedAsync(item)));
            builder.AddAttribute(4, "onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(this, args => InvokeRowClickedFromKeyboardAsync(item, args)));
        }

        for (var i = 0; i < _columns.Count; i++) {
            var column = _columns[i];
            builder.OpenElement(10, "td");
            builder.AddAttribute(11, "class", column.BodyCellClass);
            builder.OpenElement(12, "div");
            builder.AddAttribute(13, "class", "nt-data-grid-cell-content");
            builder.OpenRegion(14);
            column.RenderCell(builder, item);
            builder.CloseRegion();
            builder.CloseElement();
            builder.CloseElement();
        }
        builder.CloseElement();
    };

    private RenderFragment RenderEmptyRow() => builder => {
        builder.OpenElement(0, "tr");
        builder.AddAttribute(1, "class", _emptyRowClass);
        builder.OpenElement(2, "td");
        builder.AddAttribute(3, "colspan", VisibleColumnCount);
        builder.AddContent(4, EmptyText);
        builder.CloseElement();
        builder.CloseElement();
    };

    private RenderFragment RenderLoadingRow(PlaceholderContext context) => builder => {
        builder.OpenElement(0, "tr");
        builder.AddAttribute(1, "class", "nt-data-grid-row nt-data-grid-row-placeholder");
        var resolvedRowSize = Math.Max(1, context.Size);
        var rowSize = resolvedRowSize.ToString(CultureInfo.InvariantCulture);
        builder.AddAttribute(2, "style", $"height: {rowSize}px; --nt-data-grid-placeholder-row-height: {rowSize}px;");
        builder.AddAttribute(3, "aria-rowindex", context.Index);
        builder.AddAttribute(4, "aria-hidden", "true");
        builder.OpenElement(5, "td");
        builder.AddAttribute(6, "class", "nt-data-grid-cell nt-data-grid-cell-placeholder");
        builder.AddAttribute(7, "colspan", VisibleColumnCount);
        builder.AddAttribute(8, "style", $"height: {rowSize}px; --nt-data-grid-placeholder-row-height: {rowSize}px;");
        builder.OpenElement(9, "div");
        builder.AddAttribute(10, "class", "nt-data-grid-row-skeleton");
        builder.AddAttribute(11, "style", $"height: {rowSize}px; min-height: {rowSize}px;");
        builder.OpenComponent<NTSkeleton>(12);
        builder.AddAttribute(13, nameof(NTSkeleton.Shape), NTSkeletonShape.Rectangle);
        builder.AddAttribute(14, nameof(NTSkeleton.Width), "100%");
        builder.AddAttribute(15, nameof(NTSkeleton.Height), $"{rowSize}px");
        builder.CloseComponent();
        builder.CloseElement();
        builder.CloseElement();
        builder.CloseElement();
    };

    private RenderFragment RenderSpacerCell(PlaceholderContext context) => builder => {
        var resolvedSize = Math.Max(0, context.Size);
        var size = resolvedSize.ToString(CultureInfo.InvariantCulture);
        builder.OpenElement(0, "td");
        builder.AddAttribute(1, "class", "nt-data-grid-spacer-cell");
        builder.AddAttribute(2, "colspan", VisibleColumnCount);
        builder.AddAttribute(3, "style", $"height: {size}px;");
        builder.CloseElement();
    };

    private sealed class NTDataGridObjectComparer : IComparer<object?> {
        public static readonly NTDataGridObjectComparer Instance = new();

        public int Compare(object? x, object? y) {
            if (ReferenceEquals(x, y)) {
                return 0;
            }

            if (x is null) {
                return -1;
            }

            if (y is null) {
                return 1;
            }

            if (x is IComparable comparable) {
                return comparable.CompareTo(y);
            }

            return StringComparer.CurrentCulture.Compare(Convert.ToString(x, CultureInfo.CurrentCulture), Convert.ToString(y, CultureInfo.CurrentCulture));
        }
    }

    /// <inheritdoc />
    public void Dispose() {
        _componentCancellation.Cancel();
        _componentCancellation.Dispose();
    }
}
