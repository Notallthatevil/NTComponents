using System.Diagnostics.CodeAnalysis;
using NTComponents.Core;
using NTComponents.Grid.Columns;
using NTComponents.Virtualization;

namespace NTComponents.Grid;

/// <summary>
///     Represents a request for grid items, including sorting, pagination, and cancellation support.
/// </summary>
/// <typeparam name="TGridItem">The type of items in the grid.</typeparam>
[ExcludeFromCodeCoverage]
[Obsolete("This legacy grid API is obsolete. Use NTDataGrid, NTPropertyColumn, and NTTemplateColumn instead.")]
public readonly struct TnTGridItemsProviderRequest<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] TGridItem> {

    /// <summary>
    ///     A token that can be used to cancel the request.
    /// </summary>
    public CancellationToken CancellationToken { get; init; }

    /// <summary>
    ///     The maximum number of items to retrieve, or <c>null</c> to retrieve all available items.
    /// </summary>
    public int? Count { get; init; }

    /// <summary>
    ///     The sorting rules to apply to the grid items, or <c>null</c> for no sorting.
    /// </summary>
    public TnTGridSort<TGridItem>? SortBy { get; init; }

    /// <summary>
    ///     The zero-based index of the first item to retrieve.
    /// </summary>
    public int StartIndex { get; init; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="TnTGridItemsProviderRequest{TGridItem}" /> struct.
    /// </summary>
    /// <param name="startIndex">       The zero-based index of the first item to retrieve.</param>
    /// <param name="count">            The maximum number of items to retrieve, or <c>null</c> to retrieve all available items.</param>
    /// <param name="sort">             The sorting rules to apply, or <c>null</c> for no sorting.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the request.</param>
    internal TnTGridItemsProviderRequest(int startIndex, int? count, TnTGridSort<TGridItem>? sort, CancellationToken cancellationToken) {
        StartIndex = startIndex;
        Count = count;
        SortBy = sort;
        CancellationToken = cancellationToken;
    }

    /// <summary>
    ///     Implicitly converts a <see cref="TnTItemsProviderRequest" /> to a <see cref="TnTGridItemsProviderRequest{TGridItem}" />. Sorting information is not transferred.
    /// </summary>
    /// <param name="request">The items provider request to convert.</param>
    public static implicit operator TnTGridItemsProviderRequest<TGridItem>(TnTItemsProviderRequest request) {
        return new TnTGridItemsProviderRequest<TGridItem> {
            Count = request.Count,
            SortBy = null,
            StartIndex = request.StartIndex,
            CancellationToken = default
        };
    }

    /// <summary>
    ///     Implicitly converts a <see cref="TnTGridItemsProviderRequest{TGridItem}" /> to a <see cref="TnTItemsProviderRequest" />. Sorting information is transferred as property names and directions.
    /// </summary>
    /// <param name="request">The grid items provider request to convert.</param>
    public static implicit operator TnTItemsProviderRequest(TnTGridItemsProviderRequest<TGridItem> request) {
        return new TnTItemsProviderRequest {
            StartIndex = request.StartIndex,
            SortOnProperties = [.. request.GetSortByProperties().Select(sp => new KeyValuePair<string, SortDirection>(sp.PropertyName, sp.Direction))],
            Count = request.Count
        };
    }

    /// <summary>
    ///     Applies the sorting rules to the supplied <see cref="IQueryable{TGridItem}" />.
    /// </summary>
    /// <param name="source">The queryable source to sort.</param>
    /// <returns>An <see cref="IQueryable{TGridItem}" /> with sorting applied, or the original source if no sorting is specified.</returns>
    public IQueryable<TGridItem> ApplySorting(IQueryable<TGridItem> source) => SortBy?.Apply(source) ?? source;

    /// <summary>
    ///     Produces a collection of <see cref="SortedProperty" /> representing the sorting rules.
    /// </summary>
    /// <returns>A collection of <see cref="SortedProperty" /> with property names and directions, or an empty collection if no sorting is specified.</returns>
    public IEnumerable<SortedProperty> GetSortByProperties() => SortBy?.ToPropertyList() ?? [];
}