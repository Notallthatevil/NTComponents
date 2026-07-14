using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
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
    private string? _previousFormat;
    private string? _previousPropertyExpressionText;
    private Expression<Func<TItem, TValue>>? _propertyMetadataSource;
    private string _defaultTitle = string.Empty;
    private string _memberName = string.Empty;

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

    internal override string DefaultTitle => _defaultTitle;

    internal override string? SortPropertyName => _memberName;

    /// <inheritdoc />
    protected override void OnParametersSet() {
        if (!ReferenceEquals(_propertyMetadataSource, Property)) {
            _propertyMetadataSource = Property;
            var memberName = NTDataGridColumn<TItem>.GetMemberName(Property);
            if (!string.Equals(_memberName, memberName, StringComparison.Ordinal)) {
                _memberName = memberName;
                _defaultTitle = memberName.SplitPascalCase();
            }
        }

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
    private protected override bool HasAdditionalStateChanged() =>
        !string.Equals(_previousPropertyExpressionText, _accessorExpressionText, StringComparison.Ordinal)
        || !string.Equals(_previousFormat, Format, StringComparison.Ordinal);

    /// <inheritdoc />
    private protected override bool HasAdditionalSortStateChanged() => !string.Equals(_previousPropertyExpressionText, _accessorExpressionText, StringComparison.Ordinal);

    /// <inheritdoc />
    private protected override void CaptureAdditionalState() {
        _previousPropertyExpressionText = _accessorExpressionText;
        _previousFormat = Format;
    }

    internal override void RenderCell(RenderTreeBuilder builder, TItem item) {
        if (CellTemplate is not null) {
            builder.AddContent(0, CellTemplate, item);
            return;
        }

        if (_accessor is not null) {
            builder.AddContent(0, FormatValue(_accessor(item)));
        }
    }

    private string? FormatValue(TValue value) {
        if (value is null) {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(Format) && value is IFormattable formattable) {
            return formattable.ToString(Format, CultureInfo.CurrentCulture);
        }

        return value.ToString();
    }
}
