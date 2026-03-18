using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;
using NTComponents.Grid.Columns;

namespace NTComponents.Grid.Infrastructure;

/// <summary>
///     Provides internal state and logic for a <see cref="TnTDataGrid{TGridItem}" /> component, including column registration, sorting, pagination, and item management.
/// </summary>
/// <typeparam name="TGridItem">The type of data represented by each row in the grid.</typeparam>
internal sealed class TnTInternalGridContext<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] TGridItem>(TnTDataGrid<TGridItem> _grid) {

    /// <summary>
    ///     Gets the columns registered with the grid, ordered by <see cref="TnTColumnBase{TGridItem}.Order" /> and <see cref="TnTColumnBase{TGridItem}.ColumnId" />.
    /// </summary>
    public IEnumerable<TnTColumnBase<TGridItem>> Columns => _columns.Values.OrderBy(c => c.Order).ThenBy(c => c.ColumnId);

    /// <summary>
    ///     Gets the appearance settings for the data grid.
    /// </summary>
    public DataGridAppearance DataGridAppearance => _grid.DataGridAppearance;

    /// <summary>
    ///     Gets the associated <see cref="TnTDataGrid{TGridItem}" /> instance.
    /// </summary>
    public TnTDataGrid<TGridItem> Grid => _grid;

    /// <summary>
    ///     Gets the function used to extract a unique key from each grid item.
    /// </summary>
    public Func<TGridItem, object> ItemKey => _grid.ItemKey;

    /// <summary>
    ///     Gets or sets the items currently displayed in the grid, after applying sorting and pagination.
    /// </summary>
    public IEnumerable<TGridItem> Items { get; private set; } = [];

    /// <summary>
    ///     Gets the current pagination state, if pagination is enabled.
    /// </summary>
    public TnTPaginationState? PaginationState => _grid.Pagination;

    /// <summary>
    ///     Gets or sets a delegate that returns the CSS class name for a given data grid row.
    /// </summary>
    /// <remarks>
    ///     Use this property to customize the appearance of individual rows based on their data or state. The delegate receives the row data as a parameter and should return a string containing one
    ///     or more CSS class names to apply to that row. If the delegate returns null or an empty string, no additional class is applied.
    /// </remarks>
    public Func<TGridItem, string>? RowClass => _grid.RowClass;

    /// <summary>
    ///     Gets the callback invoked when a row is clicked.
    /// </summary>
    public EventCallback<TGridItem> RowClickCallback => _grid.OnRowClicked;

    /// <summary>
    ///     Gets the current sorting rules applied to the grid.
    /// </summary>
    public TnTGridSort<TGridItem>? SortBy { get; private set; }

    /// <summary>
    ///     Gets or sets the total number of rows available in the grid.
    /// </summary>
    public int TotalRowCount { get; set; }

    private static int _nextColumnId;
    private readonly Dictionary<int, TnTColumnBase<TGridItem>> _columns = [];
    private readonly List<TnTColumnBase<TGridItem>> _sortByColumns = [];
    private readonly Dictionary<int, SortDirection> _sortingDirections = [];

    /// <summary>
    ///     Gets the sort direction for the specified column, if it is currently sorted.
    /// </summary>
    /// <param name="column">The column to check.</param>
    /// <returns>The sort direction if the column is sorted; otherwise, <c>null</c>.</returns>
    public SortDirection? ColumnIsSortedOn(TnTColumnBase<TGridItem> column) =>
        _sortingDirections.TryGetValue(column.ColumnId, out var direction) ? direction : null;

    /// <summary>
    ///     Gets the sort index of the specified column if it is currently sorted.
    /// </summary>
    /// <param name="column">The column to check.</param>
    /// <returns>The 1-based sort index, or <c>null</c> if the column is not sorted.</returns>
    public int? GetSortIndex(TnTColumnBase<TGridItem> column) => _sortByColumns.IndexOf(column) switch {
        -1 => null,
        var index => index + 1 // +1 because the first column is always the default sort column
    };

    /// <summary>
    ///     Refreshes the grid by invoking the <see cref="TnTDataGrid{TGridItem}.RefreshDataGridAsync" /> method.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous operation.</returns>
    public Task RefreshAsync() => _grid.RefreshDataGridAsync();

    /// <summary>
    ///     Registers a column with the grid context, assigning it a unique <see cref="TnTColumnBase{TGridItem}.ColumnId" /> if necessary.
    /// </summary>
    /// <param name="column">The column to register.</param>
    public void RegisterColumn(TnTColumnBase<TGridItem> column) {
        if (column.ColumnId < 0) {
            column.ColumnId = Interlocked.Increment(ref _nextColumnId);
        }
        column.NewColumn = _columns.TryAdd(column.ColumnId, column);
    }

    /// <summary>
    ///     Removes a column from the grid context.
    /// </summary>
    /// <param name="column">The column to remove.</param>
    public void RemoveColumn(TnTColumnBase<TGridItem> column) => _columns.Remove(column.ColumnId);

    /// <summary>
    ///     Updates the sorting state for the specified column, toggling its sort direction or removing it from sorting as appropriate, and refreshes the grid items.
    /// </summary>
    /// <param name="column">The column to sort by.</param>
    public void SortByColumn(TnTColumnBase<TGridItem> column) {
        if (_grid.AllowMultiSort) {
            if (_sortingDirections.TryGetValue(column.ColumnId, out var direction)) {
                if (direction == column.InitialSortDirection) {
                    var newDirection = direction == SortDirection.Ascending ? SortDirection.Descending : SortDirection.Ascending;
                    _sortingDirections[column.ColumnId] = newDirection;
                    column.SortBy?.FlipDirections = true;
                }
                else {
                    _sortingDirections.Remove(column.ColumnId);
                    _sortByColumns.Remove(column);
                    column.SortBy?.FlipDirections = false;
                }
            }
            else {
                _sortingDirections[column.ColumnId] = column.InitialSortDirection;
                _sortByColumns.Add(column);
            }

            if (_sortByColumns.Count == 0) {
                SortBy = null;
            }
            else {
                SortBy = _sortByColumns[0].SortBy;
                foreach (var c in _sortByColumns.Skip(1)) {
                    SortBy = SortBy?.Append(c.SortBy);
                }
            }
        }
        else {
            if (_sortingDirections.TryGetValue(column.ColumnId, out var direction)) {
                if (direction == column.InitialSortDirection || Grid.DisableSortToggleToNone) {
                    var newDirection = direction == SortDirection.Ascending ? SortDirection.Descending : SortDirection.Ascending;
                    _sortingDirections.Clear();
                    _sortingDirections[column.ColumnId] = newDirection;
                    column.SortBy?.FlipDirections = newDirection != column.InitialSortDirection;
                    SortBy = column.SortBy;
                }
                else {
                    _sortingDirections.Clear();
                    _sortByColumns.Clear();
                    column.SortBy?.FlipDirections = false;
                    SortBy = null;
                }
            }
            else {
                _sortingDirections.Clear();
                _sortingDirections[column.ColumnId] = column.InitialSortDirection;
                column.SortBy?.FlipDirections = false;
                SortBy = column.SortBy;
            }
        }
        UpdateItems();
    }

    /// <summary>
    ///     Updates the <see cref="Items" /> collection by applying sorting and pagination to the grid's data source.
    /// </summary>
    public void UpdateItems() {
        if (_grid.Items is not null) {
            var items = _grid.Items;
            if (SortBy is not null) {
                items = SortBy.Apply(items);
            }
            if (PaginationState is not null) {
                items = PaginationState.Apply(items);
            }
            Items = items;
        }
        else {
            Items = _grid.ProvidedItems ?? [];
        }
        //_grid.Refresh();
    }
}