using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace NTComponents.Virtualization;

/// <summary>
///     Represents an NT item request that can be bound from HTTP query parameters.
/// </summary>
/// <remarks>
///     This endpoint accepts query parameters in the form: <c>?StartIndex=0&amp;Count=10&amp;Sorts=Name,Ascending&amp;Sorts=Age,Descending</c>.
/// </remarks>
[ExcludeFromCodeCoverage]
public readonly record struct NTItemsProviderRequest() {
    /// <summary>
    ///     Gets the start index of the requested items.
    /// </summary>
    public int StartIndex { get; init; }

    /// <summary>
    ///     Gets the maximum number of items to retrieve.
    /// </summary>
    public int? Count { get; init; }

    /// <summary>
    ///     Gets the sort descriptors formatted as <c>PropertyName,Direction</c>.
    /// </summary>
    public string[] Sorts { get; init; } = [];

    /// <summary>
    ///     Gets the parsed properties to sort on and their sort directions.
    /// </summary>
    [IgnoreDataMember]
    [JsonIgnore]
    public IReadOnlyList<KeyValuePair<string, SortDirection>> SortOnProperties => ParseSorts(Sorts);

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

        var sorts = GetQueryValues(query, nameof(Sorts));
        if (sorts.Length == 0) {
            sorts = GetQueryValues(query, nameof(SortOnProperties));
        }

        return ValueTask.FromResult<NTItemsProviderRequest?>(new NTItemsProviderRequest {
            StartIndex = startIndex,
            Count = count,
            Sorts = sorts
        });
    }

    internal static string[] FormatSorts(IEnumerable<KeyValuePair<string, SortDirection>>? sortOnProperties) {
        if (sortOnProperties is null) {
            return [];
        }

        var sorts = new List<string>();
        foreach (var sort in sortOnProperties) {
            if (!string.IsNullOrWhiteSpace(sort.Key)) {
                sorts.Add($"{sort.Key},{sort.Value}");
            }
        }

        return [.. sorts];
    }

    private static IReadOnlyList<KeyValuePair<string, SortDirection>> ParseSorts(string[]? sorts) {
        if (sorts is not { Length: > 0 }) {
            return [];
        }

        var sortOnProperties = new List<KeyValuePair<string, SortDirection>>();
        foreach (var sort in sorts) {
            if (string.IsNullOrWhiteSpace(sort)) {
                continue;
            }

            AddSorts(sortOnProperties, sort.Replace("[", string.Empty, StringComparison.Ordinal).Replace("]", string.Empty, StringComparison.Ordinal));
        }

        return sortOnProperties;
    }

    private static void AddSorts(List<KeyValuePair<string, SortDirection>> sortOnProperties, string sort) {
        var sortParts = sort.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (var i = 0; i < sortParts.Length; i += 2) {
            var direction = i + 1 < sortParts.Length && Enum.TryParse<SortDirection>(sortParts[i + 1], true, out var parsedDirection)
                ? parsedDirection
                : SortDirection.Ascending;
            sortOnProperties.Add(new KeyValuePair<string, SortDirection>(sortParts[i], direction));
        }
    }

    private static string[] GetQueryValues(IQueryCollection query, string key) {
        if (!query.TryGetValue(key, out var values) || values.Count == 0) {
            return [];
        }

        var count = CountNonEmptyValues(values);
        if (count == 0) {
            return [];
        }

        var queryValues = new string[count];
        var index = 0;
        foreach (var value in values) {
            if (!string.IsNullOrWhiteSpace(value)) {
                queryValues[index++] = value;
            }
        }

        return queryValues;
    }

    private static int CountNonEmptyValues(StringValues values) {
        var count = 0;
        foreach (var value in values) {
            if (!string.IsNullOrWhiteSpace(value)) {
                count++;
            }
        }

        return count;
    }
}
