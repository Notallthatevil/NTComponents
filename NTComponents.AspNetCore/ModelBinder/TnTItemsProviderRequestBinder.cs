using Microsoft.AspNetCore.Mvc.ModelBinding;
using NTComponents.Virtualization;

namespace NTComponents.AspNetCore.ModelBinder;

/// <summary>
///     Binder for <see cref="TnTItemsProviderRequest" />. This class is used to bind the HTTP context query parameters to a <see cref="TnTItemsProviderRequest" /> instance.
/// </summary>
[Obsolete("Use NTItemsProviderRequest with ASP.NET Core model binding instead.")]
public class TnTItemsProviderRequestBinder : IModelBinder {

    /// <inheritdoc />
    public Task BindModelAsync(ModelBindingContext bindingContext) {
        ArgumentNullException.ThrowIfNull(bindingContext, nameof(bindingContext));

        if (bindingContext.BindingSource == BindingSource.Query) {
            var query = bindingContext.HttpContext?.Request?.Query;
            if (query is not null && query.TryGetValue(nameof(TnTItemsProviderRequest.StartIndex), out var startIndexes) && !string.IsNullOrWhiteSpace(startIndexes.FirstOrDefault()) && int.TryParse(startIndexes.FirstOrDefault(), out var startIndex)) {
                int? count = null;
                var countStr = query[nameof(TnTItemsProviderRequest.Count)];
                if (!string.IsNullOrWhiteSpace(countStr.FirstOrDefault()) && int.TryParse(countStr.FirstOrDefault(), out var countResult)) {
                    count = countResult;
                }
                var sortOnProperties = query[nameof(TnTItemsProviderRequest.SortOnProperties)].SelectMany(s => s!.Split("],[").Select(t => t.Replace("[", string.Empty).Replace("]", string.Empty)))
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => {
                        var split = s.Split(',');
                        return new KeyValuePair<string, SortDirection>(split.First(), Enum.Parse<SortDirection>(split.LastOrDefault() ?? nameof(SortDirection.Ascending)));
                    })
                    .ToList();

                var result = new TnTItemsProviderRequest {
                    StartIndex = startIndex,
                    Count = count,
                    SortOnProperties = sortOnProperties
                };

                bindingContext.Result = ModelBindingResult.Success(result);
            }
            else {
                bindingContext.Result = ModelBindingResult.Failed();
            }
        }
        return Task.CompletedTask;
    }
}
