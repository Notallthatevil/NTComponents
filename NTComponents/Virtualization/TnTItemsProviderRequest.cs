using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace NTComponents.Virtualization;

/// <summary>
///     Represents an item request. This struct is http query parameter binding friendly.
/// </summary>
/// <remarks>
///     To pass this object as a query parameter, use the following format:
///     <code>
///app.MapGet("/endpoint", (TnTItemsProviderRequest request) =&gt; ...);
///     </code>
///     This endpoint accepts the following query parameters: https://example.com?StartIndex=0&amp;SortOnProperties=%5BPropertyName%2CAscending%5D%2C%5Bb%2C%20Descending%5D&amp;Count=10 Decoded = https://example.com?StartIndex=0&amp;SortOnProperties=[PropertyName,Ascending],[PropertyName2,Descending]&amp;Count=10
/// </remarks>
[ExcludeFromCodeCoverage]
public readonly record struct TnTItemsProviderRequest() : IValidatableObject {
    private readonly int? _startIndex;

    /// <summary>
    ///     Gets or sets the start index of the requested items.
    /// </summary>
    public int StartIndex {
        get => _startIndex.GetValueOrDefault();
        init => _startIndex = value;
    }

    /// <summary>
    ///     Gets or sets the properties to sort on and their sort directions.
    /// </summary>
    public readonly IEnumerable<KeyValuePair<string, SortDirection>> SortOnProperties { get; init; } = [];
    /// <summary>
    ///     Gets or sets the maximum number of items to retrieve.
    /// </summary>
    public readonly int? Count { get; init; }

    IEnumerable<ValidationResult> IValidatableObject.Validate(ValidationContext validationContext) {
        if (!_startIndex.HasValue) {
            yield return new ValidationResult("StartIndex is required.", [nameof(StartIndex)]);
        }
    }

    /// <summary>
    ///     Binds the HTTP context query parameters to a <see cref="TnTItemsProviderRequest" /> instance.
    /// </summary>
    /// <param name="context">The HTTP context containing the query parameters.</param>
    /// <returns>A task that represents the asynchronous bind operation. The task result contains the bound <see cref="TnTItemsProviderRequest" /> instance or null if binding failed.</returns>
    public static ValueTask<TnTItemsProviderRequest?> BindAsync(HttpContext context) {
        var query = context.Request.Query;
        if (query.TryGetValue(nameof(StartIndex), out var startIndexes) && !string.IsNullOrWhiteSpace(startIndexes.FirstOrDefault()) && int.TryParse(startIndexes.FirstOrDefault(), out var startIndex)) {
            int? count = null;
            var countStr = query[nameof(Count)];
            if (!string.IsNullOrWhiteSpace(countStr.FirstOrDefault()) && int.TryParse(countStr.FirstOrDefault(), out var countResult)) {
                count = countResult;
            }
            var sortOnProperties = query[nameof(SortOnProperties)].SelectMany(s => s!.Split("],[").Select(t => t.Replace("[", string.Empty).Replace("]", string.Empty)))
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => {
                    var split = s.Split(',');
                    return new KeyValuePair<string, SortDirection>(split[0], Enum.Parse<SortDirection>(split.LastOrDefault() ?? nameof(SortDirection.Ascending)));
                })
                .ToList();

            return ValueTask.FromResult<TnTItemsProviderRequest?>(new TnTItemsProviderRequest {
                StartIndex = startIndex,
                Count = count,
                SortOnProperties = sortOnProperties
            });
        }

        return ValueTask.FromResult((TnTItemsProviderRequest?)null);
    }
}
