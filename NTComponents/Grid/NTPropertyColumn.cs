using Microsoft.AspNetCore.Components;
using NTComponents.CodeDocumentation;
using NTComponents.Ext;
using System.Globalization;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
namespace NTComponents;

/// <summary>
///     Renders a column bound to a property expression.
/// </summary>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.ProgressivelyEnhanced,
    CompatibilitySummary = "Participates in parent component rendering and inherits the parent interaction model.",
    CompatibilityDetails = "The column can contribute header and cell content to the parent grid during static SSR. Sorting and grid state changes depend on the parent grid render mode.")]
public sealed class NTPropertyColumn<TItem, TValue> : NTDataGridColumn<TItem> where TItem : class {
    private Func<TItem, TValue>? _accessor;
    private string? _accessorExpressionText;

    /// <summary>
    ///     Gets or sets whether the column can be sorted.
    /// </summary>
    public override bool Sortable => true;

    /// <summary>
    ///     Gets or sets the property expression rendered by the column.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public Expression<Func<TItem, TValue>>? Property { get; set; }

    /// <summary>
    ///     Gets or sets the format string used for values implementing <see cref="IFormattable" />.
    /// </summary>
    [Parameter]
    public string? Format { get; set; }

    internal override string DefaultTitle => NTDataGridColumn<TItem>.GetMemberName(Property).SplitPascalCase();

    internal override string? SortPropertyName => NTDataGridColumn<TItem>.GetMemberName(Property);

    /// <inheritdoc />
    protected override void OnParametersSet() {
        var expressionText = Property?.ToString();
        if (_accessor is null || !string.Equals(_accessorExpressionText, expressionText, StringComparison.Ordinal)) {
            _accessor = Property?.Compile();
            _accessorExpressionText = expressionText;
        }

        base.OnParametersSet();
    }

    internal override object? GetSortValue(TItem item) => _accessor is null ? null : _accessor(item);

    internal override IOrderedQueryable<TItem>? ApplyQueryableSort(IQueryable<TItem> source, SortDirection direction, bool thenBy) {
        if (Property is null) {
            return null;
        }

        if (thenBy && source is IOrderedQueryable<TItem> orderedSource) {
            return direction == SortDirection.Descending
                ? orderedSource.ThenByDescending(Property)
                : orderedSource.ThenBy(Property);
        }

        return direction == SortDirection.Descending
            ? source.OrderByDescending(Property)
            : source.OrderBy(Property);
    }

    /// <inheritdoc />
    protected override string GetStateSignature() => string.Join("|", base.GetStateSignature(), Property?.ToString(), Format);

    /// <inheritdoc />
    protected override string GetSortStateSignature() => string.Join("|", base.GetSortStateSignature(), Property?.ToString());

    internal override RenderFragment RenderCell(TItem item) {
        if (CellTemplate is not null) {
            return CellTemplate(item);
        }

        return builder => builder.AddContent(0, FormatValue(GetSortValue(item)));
    }

    private string? FormatValue(object? value) {
        if (value is null) {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(Format) && value is IFormattable formattable) {
            return formattable.ToString(Format, CultureInfo.CurrentCulture);
        }

        return value.ToString();
    }
}
