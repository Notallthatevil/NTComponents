using Microsoft.AspNetCore.Components;
using System.Linq.Expressions;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
///     Base type for columns rendered by <see cref="NTDataGrid{TItem}" />.
/// </summary>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.ProgressivelyEnhanced,
    CompatibilitySummary = "Participates in parent component rendering and inherits the parent interaction model.",
    CompatibilityDetails = "Column definitions participate in parent grid rendering without their own browser APIs. User-driven sort, row, paging, or virtualization behavior depends on the parent grid render mode.")]
public abstract class NTDataGridColumn<TItem> : ComponentBase, IDisposable where TItem : class {
    private string? _sortStateSignature;
    private string? _stateSignature;
    private bool _registered;

    /// <summary>
    ///     Gets or sets the header title.
    /// </summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>
    ///     Gets or sets whether the column can be sorted.
    /// </summary>
    public virtual bool Sortable { get; set; }

    /// <summary>
    ///     Gets or sets the initial sort direction.
    /// </summary>
    [Parameter]
    public SortDirection? InitialSortDirection { get; set; }

    /// <summary>
    ///     Gets or sets the body cell text alignment.
    /// </summary>
    [Parameter]
    public TextAlign? TextAlign { get; set; }

    /// <summary>
    ///     Gets or sets the header cell text alignment.
    /// </summary>
    [Parameter]
    public TextAlign? HeaderTextAlign { get; set; }

    /// <summary>
    ///     Gets or sets the preferred column width.
    /// </summary>
    [Parameter]
    public string? Width { get; set; }

    /// <summary>
    ///     Gets or sets the minimum column width.
    /// </summary>
    [Parameter]
    public string? MinWidth { get; set; }

    /// <summary>
    ///     Gets or sets the maximum column width.
    /// </summary>
    [Parameter]
    public string? MaxWidth { get; set; }

    /// <summary>
    ///     Gets or sets a custom cell template.
    /// </summary>
    [Parameter]
    public RenderFragment<TItem>? CellTemplate { get; set; }

    [CascadingParameter]
    internal NTDataGrid<TItem>? Owner { get; set; }

    internal int Sequence { get; set; }

    internal string HeaderTitle => !string.IsNullOrWhiteSpace(Title) ? Title! : DefaultTitle;

    internal abstract string DefaultTitle { get; }

    internal abstract string? SortPropertyName { get; }

    internal virtual object? GetSortValue(TItem item) => null;

    internal virtual IOrderedQueryable<TItem>? ApplyQueryableSort(IQueryable<TItem> source, SortDirection direction, bool thenBy) => null;

    internal virtual IOrderedEnumerable<TItem>? ApplyLocalSort(IEnumerable<TItem> source, SortDirection direction, bool thenBy) => null;

    internal virtual IReadOnlyList<NTSortDescriptor> GetSortDescriptors(SortDirection direction) => [new(SortPropertyName!, direction)];

    internal abstract RenderFragment RenderCell(TItem item);

    /// <inheritdoc />
    protected override void OnInitialized() {
        if (Owner is null) {
            throw new InvalidOperationException($"{GetType().Name} must be nested inside {nameof(NTDataGrid<TItem>)}.");
        }

        Owner.RegisterColumn(this);
        _registered = true;
    }

    /// <inheritdoc />
    protected override void OnParametersSet() {
        var sortStateSignature = GetSortStateSignature();
        var stateSignature = GetStateSignature();
        if (_registered && !string.Equals(_stateSignature, stateSignature, StringComparison.Ordinal)) {
            Owner?.NotifyColumnChanged(this, !string.Equals(_sortStateSignature, sortStateSignature, StringComparison.Ordinal));
        }

        _sortStateSignature = sortStateSignature;
        _stateSignature = stateSignature;
    }

    /// <inheritdoc />
    public void Dispose() {
        if (_registered) {
            Owner?.UnregisterColumn(this);
        }
    }

    /// <summary>
    ///     Gets the column state that should notify the owning grid when it changes.
    /// </summary>
    /// <returns>A stable signature for data-affecting column state.</returns>
    protected virtual string GetStateSignature() =>
        string.Join("|", Title, Sortable, InitialSortDirection, TextAlign, HeaderTextAlign, Width, MinWidth, MaxWidth, CellTemplate?.GetHashCode(), HeaderTitle, SortPropertyName);

    /// <summary>
    ///     Gets the column state that should reset the owning grid sort state when it changes.
    /// </summary>
    /// <returns>A stable signature for sort-affecting column state.</returns>
    protected virtual string GetSortStateSignature() => string.Join("|", Sortable, InitialSortDirection, SortPropertyName);

    internal static string GetMemberName<TValue>(Expression<Func<TItem, TValue>>? expression) {
        if (expression?.Body is MemberExpression memberExpression) {
            return memberExpression.Member.Name;
        }

        if (expression?.Body is UnaryExpression { Operand: MemberExpression unaryMemberExpression }) {
            return unaryMemberExpression.Member.Name;
        }

        return string.Empty;
    }
}
