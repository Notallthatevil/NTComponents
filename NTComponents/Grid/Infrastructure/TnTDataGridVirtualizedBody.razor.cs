using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;
using NTComponents.Virtualization;

namespace NTComponents.Grid.Infrastructure;

/// <summary>
///     Provides the virtualized body for the data grid, handling item virtualization and debounced data requests.
/// </summary>
/// <typeparam name="TGridItem">The type of the grid item.</typeparam>
[CascadingTypeParameter(nameof(TGridItem))]
[Obsolete("This legacy grid API is obsolete. Use NTDataGrid, NTPropertyColumn, and NTTemplateColumn instead.")]
public partial class TnTDataGridVirtualizedBody<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] TGridItem> {

    /// <summary>
    ///     The default debounce delay in milliseconds for virtualized data requests.
    /// </summary>
    private const int _defaultDelay = 500;

    /// <summary>
    ///     The current debounce delay in milliseconds for virtualized data requests.
    /// </summary>
    private int _delay = _defaultDelay;

    /// <summary>
    ///     The virtualize component instance used to manage virtualization and data loading.
    /// </summary>
    private NTVirtualize<(int, TGridItem)> _virtualize = default!;

    /// <inheritdoc />
    public override async Task RefreshAsync() {
        await base.RefreshAsync();
        await InvokeAsync(async () => await (_virtualize?.RefreshDataAsync() ?? Task.CompletedTask));
    }

    /// <summary>
    ///     Provides items for virtualization, applying a debounce to reduce redundant queries.
    /// </summary>
    /// <param name="request">The request containing information about the items to provide.</param>
    /// <returns>A <see cref="TnTItemsProviderResult{TItem}" /> containing the items and total count for the grid.</returns>
    private async ValueTask<TnTItemsProviderResult<(int, TGridItem)>> ProvideVirtualizedItemsAsync(NTVirtualizeItemsProviderRequest<(int, TGridItem)> request) {
        // Debounce the requests. This eliminates a lot of redundant queries at the cost of slight lag after interactions.
        await Task.Delay(_delay);
        if (_delay < 500) {
            Interlocked.Add(ref _delay, 50);
        }
        var result = default(TnTItemsProviderResult<(int, TGridItem)>);
        if (!request.CancellationToken.IsCancellationRequested) {
            var providerResult = await Context.Grid.ResolveItemsRequestAsync(new() {
                StartIndex = request.StartIndex,
                Count = request.Count,
                CancellationToken = request.CancellationToken,
                SortBy = Context.SortBy
            });

            if (!request.CancellationToken.IsCancellationRequested) {
                Context.TotalRowCount = providerResult.TotalItemCount;

                result = new TnTItemsProviderResult<(int, TGridItem)> {
                    Items = [.. providerResult.Items.Select((x, i) => ValueTuple.Create(i + request.StartIndex + 2, x))],
                    TotalItemCount = Context.TotalRowCount
                };
                Interlocked.Exchange(ref _delay, _defaultDelay); // Reset the debounce delay
            }
        }

        return result;
    }
}