using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using NTComponents.Core;
using NTComponents.Ext;
using NTComponents.Grid;
using NTComponents.Grid.Columns;
using NTComponents.Grid.Infrastructure;
using NTComponents.Virtualization;

namespace NTComponents;

/// <summary>
///     Specifies appearance options for the <see cref="TnTDataGrid{TGridItem}" />.
/// </summary>
[Flags]
public enum DataGridAppearance {

    /// <summary>
    ///     The default appearance.
    /// </summary>
    Default = 0,

    /// <summary>
    ///     Applies a stripped row style.
    /// </summary>
    Stripped = 1,

    /// <summary>
    ///     Applies a compact row style. Must be used in conjunction with <see cref="TnTColumnBase{TGridItem}.MaxWidth"/> to be effective.
    /// </summary>
    Compact = 2,

    /// <summary>
    ///     Specifies that the table layout uses fixed column widths, regardless of the content within each cell.
    /// </summary>
    TableLayoutFixed = 4
}

/// <summary>
///     Specifies the direction of sorting for a column.
/// </summary>
public enum SortDirection {

    /// <summary>
    ///     Sorts in ascending order.
    /// </summary>
    Ascending,

    /// <summary>
    ///     Sorts in descending order.
    /// </summary>
    Descending
}

/// <summary>
///     A component that displays a grid of data items.
/// </summary>
/// <typeparam name="TGridItem">The type of data represented by each row in the grid.</typeparam>
[CascadingTypeParameter(nameof(TGridItem))]
public partial class TnTDataGrid<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] TGridItem> {

    /// <inheritdoc />
    [Parameter]
    public TnTColor BackgroundColor { get; set; } = TnTColor.Background;

    /// <inheritdoc />
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    ///     The appearance options for the data grid.
    /// </summary>
    [Parameter]
    public DataGridAppearance DataGridAppearance { get; set; }

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("tnt-datagrid")
        .AddClass("tnt-stripped", DataGridAppearance.HasFlag(DataGridAppearance.Stripped))
        .AddClass("tnt-compact", DataGridAppearance.HasFlag(DataGridAppearance.Compact))
        .AddClass("tnt-table-layout-fixed", DataGridAppearance.HasFlag(DataGridAppearance.TableLayoutFixed))
        .AddClass("tnt-resizable", Resizable)
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddVariable("tnt-data-grid-bg-color", BackgroundColor)
        .AddVariable("tnt-data-grid-fg-color", TextColor)
        .AddVariable("tnt-data-grid-tint-color", TintColor)
        .AddVariable("tnt-data-grid-on-tint-color", OnTintColor)
        .AddVariable("tnt-data-grid-row-height", $"{ItemSize}px")
        .Build();

    /// <summary>
    ///     A function that returns a unique key for each grid item.
    /// </summary>
    [Parameter]
    public Func<TGridItem, object> ItemKey { get; set; } = x => x!;

    /// <summary>
    ///     The queryable source of data for the grid.
    /// </summary>
    [Parameter]
    public IQueryable<TGridItem>? Items { get; set; }

    /// <summary>
    ///     The expected height in pixels for each row.
    /// </summary>
    [Parameter]
    public int ItemSize { get; set; } = 32;

    /// <summary>
    ///     A callback that supplies data for the grid.
    /// </summary>
    [Parameter]
    public TnTGridItemsProvider<TGridItem>? ItemsProvider { get; set; }

    /// <inheritdoc />
    public override string? JsModulePath => "./_content/NTComponents/Grid/TnTDataGrid.razor.js";

    /// <summary>
    ///     Callback invoked when a row is clicked.
    /// </summary>
    [Parameter]
    public EventCallback<TGridItem> OnRowClicked { get; set; }

    /// <summary>
    ///     The color to use on the tint color.
    /// </summary>
    [Parameter]
    public TnTColor OnTintColor { get; set; } = TnTColor.OnPrimary;

    /// <summary>
    ///     The number of extra rows to load beyond the visible area when virtualizing.
    /// </summary>
    [Parameter]
    public int OverscanCount { get; set; } = 5;

    /// <summary>
    ///     The pagination state for the grid.
    /// </summary>
    [Parameter]
    public TnTPaginationState? Pagination { get; set; }

    /// <summary>
    ///     Allows the grid to be resized by the user.
    /// </summary>
    [Parameter]
    public bool Resizable { get; set; }

    /// <summary>
    ///     Gets or sets a delegate that returns the CSS class name for a given data grid row.
    /// </summary>
    /// <remarks>
    ///     Use this property to customize the appearance of individual rows based on their data or state. The delegate receives the row data as a parameter and should return a string containing one
    ///     or more CSS class names to apply to that row. If the delegate returns null or an empty string, no additional class is applied.
    /// </remarks>
    [Parameter]
    public Func<TGridItem, string>? RowClass { get; set; }

    /// <summary>
    ///     The text color of the data grid.
    /// </summary>
    [Parameter]
    public TnTColor TextColor { get; set; } = TnTColor.OnBackground;

    /// <summary>
    ///     The highlight, tint color, of the data grid.
    /// </summary>
    [Parameter]
    public TnTColor TintColor { get; set; } = TnTColor.SurfaceTint;

    /// <summary>
    ///     Whether virtualization is enabled for the grid.
    /// </summary>
    [Parameter]
    public bool Virtualize { get; set; }

    /// <summary>
    ///     Indicates whether users can sort by multiple columns simultaneously.
    /// </summary>
    [Parameter]
    public bool AllowMultiSort { get; set; } = true;

    /// <summary>
    ///     The items currently provided to the grid.
    /// </summary>
    internal IEnumerable<TGridItem>? ProvidedItems;

    [Inject]
    private IServiceProvider? _serviceProvider { get; set; }

    /// <summary>
    ///     The internal grid context containing state and configuration for the grid.
    /// </summary>
    private readonly TnTInternalGridContext<TGridItem> _internalGridContext;

    /// <summary>
    ///     Adapter for asynchronous query execution.
    /// </summary>
    private IAsyncQueryExecutor? _asyncQueryExecutor;

    /// <summary>
    ///     The body section of the data grid.
    /// </summary>
    private TnTDataGridBody<TGridItem> _body = default!;

    /// <summary>
    ///     The cancellation token source for async operations.
    /// </summary>
    private CancellationTokenSource? _cancellationTokenSource;

    /// <summary>
    ///     The header row of the data grid.
    /// </summary>
    private TnTDataGridHeaderRow<TGridItem> _headerRow = default!;

    /// <summary>
    ///     The last used pagination state.
    /// </summary>
    private TnTPaginationState? _lastUsedPaginationState;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TnTDataGrid{TGridItem}" /> class.
    /// </summary>
    public TnTDataGrid() {
        _internalGridContext = new(this);
    }

    /// <summary>
    ///     Refreshes the data grid by resolving pagination results and updating the UI.
    /// </summary>
    public async Task RefreshDataGridAsync(CancellationToken cancellationToken = default) {
        await ResolvePaginationResultsAsync(cancellationToken);
        if (!cancellationToken.IsCancellationRequested) {
            await InvokeAsync(StateHasChanged);
            await (_body?.RefreshAsync() ?? Task.CompletedTask);
            await (_headerRow?.RefreshAsync() ?? Task.CompletedTask);
        }
    }

    /// <summary>
    ///     Resolves a data request for grid items, applying sorting, pagination, and virtualization as needed.
    /// </summary>
    /// <param name="request">The request parameters.</param>
    /// <returns>The result containing items and total count.</returns>
    internal async ValueTask<TnTItemsProviderResult<TGridItem>> ResolveItemsRequestAsync(TnTGridItemsProviderRequest<TGridItem> request) {
        TnTItemsProviderResult<TGridItem> result = new();
        if (ItemsProvider is not null) {
            result = await ItemsProvider(request);
            ProvidedItems = result.Items;
        }
        else if (Items is not null) {
            var totalItemCount = _asyncQueryExecutor is null ? Items.Count() : await _asyncQueryExecutor.CountAsync(Items, request.CancellationToken);
            _internalGridContext.TotalRowCount = totalItemCount;
            var filtered = request.ApplySorting(Items).Skip(request.StartIndex);
            if (request.Count.HasValue) {
                filtered = filtered.Take(request.Count.Value);
            }
            var resultArray = _asyncQueryExecutor is null ? [.. filtered] : await _asyncQueryExecutor.ToArrayAsync(filtered, request.CancellationToken);
            result = new() { Items = resultArray, TotalItemCount = totalItemCount };
        }
        _internalGridContext.UpdateItems();
        return result;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing) {
        base.Dispose(disposing);
        if (disposing) {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            _lastUsedPaginationState?.CurrentPageChangedCallback -= PaginationPageUpdatedAsync;
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();
        if (Virtualize && Pagination is not null) {
            throw new InvalidOperationException($"Virtualization and pagination cannot be used together in {nameof(TnTDataGrid<TGridItem>)}. Please use either {nameof(Virtualize)} or {nameof(Pagination)}.");
        }

        if (Items is not null && ItemsProvider is not null) {
            throw new InvalidOperationException($"{nameof(TnTDataGrid<TGridItem>)} requires one of {nameof(Items)} or {nameof(ItemsProvider)}, but both were specified.");
        }

        if (Items is not null && ItemsProvider is null && !Virtualize && Pagination is null) { // No virtualization or pagination, so we can load all items at once
            _internalGridContext.UpdateItems();
        }

        if (Items is not null && ItemsProvider is null && _serviceProvider is not null) {
            _asyncQueryExecutor = AsyncQueryExecutorSupplier.GetAsyncQueryExecutor(_serviceProvider, Items);
        }
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync() {
        await base.OnParametersSetAsync();
        if (Pagination is not null && _lastUsedPaginationState != Pagination) {
            _lastUsedPaginationState = Pagination;
            _lastUsedPaginationState.CurrentPageChangedCallback += PaginationPageUpdatedAsync;
            await RefreshDataGridAsync();
        }
        else if (Pagination is null && _lastUsedPaginationState is not null) {
            _lastUsedPaginationState.CurrentPageChangedCallback -= PaginationPageUpdatedAsync;
            _lastUsedPaginationState = null;
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender) {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender) {
            if(Pagination is not null && ItemsProvider is not null) {
                await RefreshDataGridAsync();
            }
        }
    }

    /// <summary>
    ///     Callback invoked when the pagination page is updated.
    /// </summary>
    /// <param name="paginationState">The updated pagination state.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private Task PaginationPageUpdatedAsync(TnTPaginationState paginationState) => RefreshDataGridAsync();

    /// <summary>
    ///     Resolves the results for the current pagination state.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ResolvePaginationResultsAsync(CancellationToken cancellationToken) {
        if (_lastUsedPaginationState is not null) {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            var newCts = new CancellationTokenSource();
            var token = cancellationToken != default ? cancellationToken : newCts.Token;
            _cancellationTokenSource = newCts;

            var result = await ResolveItemsRequestAsync(new() {
                CancellationToken = token,
                Count = _lastUsedPaginationState.ItemsPerPage,
                SortBy = _internalGridContext.SortBy,
                StartIndex = _lastUsedPaginationState.CurrentPageIndex * _lastUsedPaginationState.ItemsPerPage
            });
            if (!token.IsCancellationRequested) {
                await _lastUsedPaginationState.SetTotalItemCountAsync(result.TotalItemCount);
                _internalGridContext.TotalRowCount = result.TotalItemCount;
            }
        }
    }
}

/// <summary>
///     Delegate for providing items to a <see cref="TnTDataGrid{TGridItem}" />.
/// </summary>
/// <typeparam name="TGridItem">The type of data represented by each row in the grid.</typeparam>
/// <param name="request">The request parameters.</param>
/// <returns>A value task containing the result.</returns>
public delegate ValueTask<TnTItemsProviderResult<TGridItem>> TnTGridItemsProvider<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] TGridItem>(TnTGridItemsProviderRequest<TGridItem> request);