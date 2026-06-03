using Microsoft.AspNetCore.Components;

namespace NTComponents;

/// <summary>
///     Renders a template-only column.
/// </summary>
public sealed class NTTemplateColumn<TItem> : NTDataGridColumn<TItem> where TItem : class {
    /// <summary>
    ///     Gets or sets whether the column can be sorted.
    /// </summary>
    [Parameter]
    public override bool Sortable { get; set; }

    /// <summary>
    ///     Gets or sets the template rendered for each row.
    /// </summary>
    [Parameter]
    public RenderFragment<TItem>? ChildContent { get; set; }

    internal override string DefaultTitle => string.Empty;

    internal override string? SortPropertyName => null;

    /// <inheritdoc />
    protected override string GetStateSignature() => string.Join("|", base.GetStateSignature(), ChildContent?.GetHashCode());

    internal override RenderFragment RenderCell(TItem item) => CellTemplate?.Invoke(item) ?? ChildContent?.Invoke(item) ?? (_ => { });
}
