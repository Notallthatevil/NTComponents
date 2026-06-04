using Microsoft.AspNetCore.Components;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
///     Renders a template-only column.
/// </summary>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.ProgressivelyEnhanced,
    CompatibilitySummary = "Participates in parent component rendering and inherits the parent interaction model.",
    CompatibilityDetails = "The templates are rendered by the parent grid into static markup. Interactive row, sort, paging, or virtualized behavior depends on the parent grid render mode.")]
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
