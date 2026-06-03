using NTComponents.Grid.Infrastructure;

namespace NTComponents.Grid;

/// <summary>
///     Represents an asynchronous callback for when the total item count changes.
/// </summary>
/// <param name="paginationState">The pagination state.</param>
/// <returns>A task representing the asynchronous operation.</returns>
[Obsolete("This legacy grid API is obsolete. Use NTDataGrid, NTPropertyColumn, and NTTemplateColumn instead.")]
internal delegate Task TotalItemCountChangedAsync(TnTPaginationState paginationState);

/// <summary>
///     Represents the pagination state for a grid, including current page, items per page, and total item count.
/// </summary>
[Obsolete("This legacy grid API is obsolete. Use NTDataGrid, NTPropertyColumn, and NTTemplateColumn instead.")]
public class TnTPaginationState {

    /// <summary>
    ///     Occurs when the current page index changes asynchronously.
    /// </summary>
    internal event CurrentPageChangedAsync? CurrentPageChangedCallback;

    /// <summary>
    ///     Occurs when the total item count changes asynchronously.
    /// </summary>
    internal event TotalItemCountChangedAsync? TotalItemCountChangedCallback;

    /// <summary>
    ///     The zero-based index of the current page.
    /// </summary>
    public int CurrentPageIndex { get; private set; }

    /// <summary>
    ///     The number of items displayed per page.
    /// </summary>
    public int ItemsPerPage { get; set; } = 10;

    /// <summary>
    ///     The zero-based index of the last page, calculated from the total item count and items per page.
    /// </summary>
    public int? LastPageIndex => (TotalItemCount - 1) / ItemsPerPage;

    /// <summary>
    ///     The total number of items available for pagination.
    /// </summary>
    public int? TotalItemCount { get; private set; }

    /// <inheritdoc />
    public override int GetHashCode()
        => HashCode.Combine(ItemsPerPage, CurrentPageIndex, TotalItemCount);

    /// <summary>
    ///     Sets the current page index and triggers the <see cref="CurrentPageChangedCallback" /> event.
    /// </summary>
    /// <param name="pageIndex">The new page index to set.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task SetCurrentPageIndexAsync(int pageIndex) {
        CurrentPageIndex = pageIndex;
        return CurrentPageChangedCallback?.Invoke(this) ?? Task.CompletedTask;
    }

    /// <summary>
    ///     Sets the total item count and triggers the <see cref="TotalItemCountChangedCallback" /> event. If the current page index is no longer valid, moves to the last valid page index.
    /// </summary>
    /// <param name="totalItemCount">The new total item count.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task SetTotalItemCountAsync(int totalItemCount) {
        if (totalItemCount == TotalItemCount) {
            return Task.CompletedTask;
        }

        TotalItemCount = totalItemCount;

        if (CurrentPageIndex > 0 && CurrentPageIndex > LastPageIndex) {
            // If the number of items has reduced such that the current page index is no longer valid, move automatically to the final valid page index and trigger a further data load.
            SetCurrentPageIndexAsync(LastPageIndex.Value);
        }

        // Under normal circumstances, we just want any associated pagination UI to update
        return TotalItemCountChangedCallback?.Invoke(this) ?? Task.CompletedTask;
    }

    /// <summary>
    ///     Applies pagination to the provided queryable source.
    /// </summary>
    /// <typeparam name="TGridItem">The type of items in the grid.</typeparam>
    /// <param name="src">The source queryable to paginate.</param>
    /// <returns>A queryable containing only the items for the current page.</returns>
    internal IQueryable<TGridItem> Apply<TGridItem>(IQueryable<TGridItem> src) {
        return src.Skip(CurrentPageIndex * ItemsPerPage)
           .Take(ItemsPerPage);
    }
}

/// <summary>
///     Represents an asynchronous callback for when the current page index changes.
/// </summary>
/// <param name="paginationState">The pagination state.</param>
/// <returns>A task representing the asynchronous operation.</returns>
[Obsolete("This legacy grid API is obsolete. Use NTDataGrid, NTPropertyColumn, and NTTemplateColumn instead.")]
internal delegate Task CurrentPageChangedAsync(TnTPaginationState paginationState);