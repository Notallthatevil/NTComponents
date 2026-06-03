using Microsoft.AspNetCore.Http;
using System.Diagnostics.CodeAnalysis;

namespace NTComponents.Virtualization;

/// <summary>
///     Represents an NT item request that can be bound from HTTP query parameters.
/// </summary>
/// <remarks>
///     This endpoint accepts query parameters in the form: <c>?StartIndex=0&amp;Count=10&amp;SortOnProperties=[Name,Ascending],[Age,Descending]</c>.
/// </remarks>
[ExcludeFromCodeCoverage]
public readonly record struct NTItemsProviderRequest() {
    /// <summary>
    ///     Gets the start index of the requested items.
    /// </summary>
    public int StartIndex { get; init; }

    /// <summary>
    ///     Gets the properties to sort on and their sort directions.
    /// </summary>
    public IEnumerable<KeyValuePair<string, SortDirection>> SortOnProperties { get; init; } = [];

    /// <summary>
    ///     Gets the maximum number of items to retrieve.
    /// </summary>
    public int? Count { get; init; }

    /// <summary>
    ///     Binds the HTTP context query parameters to an <see cref="NTItemsProviderRequest" /> instance.
    /// </summary>
    public static ValueTask<NTItemsProviderRequest?> BindAsync(HttpContext context) {
        var query = context.Request.Query;
        if (!query.TryGetValue(nameof(StartIndex), out var startIndexes) || string.IsNullOrWhiteSpace(startIndexes.FirstOrDefault()) || !int.TryParse(startIndexes.FirstOrDefault(), out var startIndex)) {
            return ValueTask.FromResult((NTItemsProviderRequest?)null);
        }

        int? count = null;
        var countValues = query[nameof(Count)];
        if (!string.IsNullOrWhiteSpace(countValues.FirstOrDefault()) && int.TryParse(countValues.FirstOrDefault(), out var countResult)) {
            count = countResult;
        }

        var sortOnProperties = query[nameof(SortOnProperties)]
            .SelectMany(value => value?.Split("],[", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [])
            .Select(value => value.Replace("[", string.Empty, StringComparison.Ordinal).Replace("]", string.Empty, StringComparison.Ordinal))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => {
                var split = value.Split(',', StringSplitOptions.TrimEntries);
                var direction = split.Length > 1 && Enum.TryParse<SortDirection>(split[^1], true, out var parsedDirection)
                    ? parsedDirection
                    : SortDirection.Ascending;
                return new KeyValuePair<string, SortDirection>(split[0], direction);
            })
            .ToList();

        return ValueTask.FromResult<NTItemsProviderRequest?>(new NTItemsProviderRequest {
            StartIndex = startIndex,
            Count = count,
            SortOnProperties = sortOnProperties
        });
    }
}

