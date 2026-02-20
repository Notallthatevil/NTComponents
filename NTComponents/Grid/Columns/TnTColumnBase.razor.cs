using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography.X509Certificates;
using NTComponents.Core;
using NTComponents.Grid.Infrastructure;
using NTComponents.Interfaces;

namespace NTComponents.Grid.Columns;

/// <summary>
///     Base class for all grid column components in NTComponents. Provides common parameters and logic for sorting, ordering, and rendering header/cell content.
/// </summary>
/// <typeparam name="TGridItem">The type of data represented by each row in the grid.</typeparam>
[CascadingTypeParameter(nameof(TGridItem))]
[DebuggerDisplay("Title = {Title}, Order = {Order}, Sortable = {Sortable}, IsSortedOn = {IsSortedOn}")]
public abstract partial class TnTColumnBase<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] TGridItem> : TnTComponentBase, ITnTComponentBase, IDisposable {
    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("nt-grid-cell")
        .AddTextAlign(TextAlign)
        .Build();

    /// <summary>
    ///     Unique column ID assigned by the grid context.
    /// </summary>
    public int ColumnId { get; internal set; } = -1;

    /// <summary>
    ///     Enables ripple effect on the column header.
    /// </summary>
    [Parameter]
    public bool EnableRipple { get; set; } = true;

    /// <summary>
    ///     Alignment of the header cell content.
    /// </summary>
    [Parameter]
    public TextAlign HeaderAlignment { get; set; } = NTComponents.TextAlign.Left;

    /// <summary>
    ///     Optional template for rendering the header cell content.
    /// </summary>
    [Parameter]
    public RenderFragment<TnTColumnBase<TGridItem>>? HeaderCellItemTemplate { get; set; }

    /// <summary>
    ///     The content to display as a tooltip for the header.
    /// </summary>
    [Parameter]
    public RenderFragment? HeaderToolip { get; set; }

    /// <summary>
    ///     The initial sort direction for the column.
    /// </summary>
    [Parameter]
    public SortDirection InitialSortDirection { get; set; }

    /// <summary>
    ///     Indicates if this column is the default sort column.
    /// </summary>
    [Parameter]
    public bool IsDefaultSortColumn { get; set; }

    /// <summary>
    ///     The sort direction if the column is currently sorted.
    /// </summary>
    public SortDirection? IsSortedOn => Context?.ColumnIsSortedOn(this);

    /// <summary>
    ///     The maximum width, in pixels, that the column can occupy.
    /// </summary>
    [Parameter]
    public int? MaxWidth { get; set; }

    /// <summary>
    ///     The order of the column in the grid.
    /// </summary>
    [Parameter]
    public int Order { get; set; }

    /// <summary>
    ///     Indicates whether the component's content is displayed as a tooltip when hovered over.
    /// </summary>
    [Parameter]
    public bool ShowContentAsTooltip { get; set; } = false;

    /// <summary>
    ///     Indicates whether the column is sortable.
    /// </summary>
    [Parameter]
    public bool Sortable { get; set; }

    /// <summary>
    ///     The sorting logic for the column.
    /// </summary>
    public abstract TnTGridSort<TGridItem>? SortBy { get; set; }

    /// <summary>
    ///     Alignment of the cell content.
    /// </summary>
    [Parameter]
    public TextAlign? TextAlign { get; set; }

    /// <summary>
    ///     The title displayed in the column header.
    /// </summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>
    ///     The width of the column in pixels. If not provided, the column will be default width.
    /// </summary>
    [Parameter]
    public int? Width { get; set; }

    /// <summary>
    ///     The grid context for this column.
    /// </summary>
    [CascadingParameter]
    internal TnTInternalGridContext<TGridItem> Context { get; set; } = default!;

    /// <summary>
    ///     Indicates if this is a newly added column.
    /// </summary>
    internal bool NewColumn { get; set; }

    /// <summary>
    ///     CSS class for the header cell.
    /// </summary>
    private string _headerClass => CssClassBuilder.Create()
        .AddClass("tnt-header-content")
        .AddTextAlign(HeaderAlignment)
        .AddClass("tnt-interactable", Sortable)
        .AddClass("tnt-column-header-sorted-on", IsSortedOn.HasValue)
        .Build();

    /// <summary>
    ///     The sort index of the column if it is sorted.
    /// </summary>
    private int? _sortIndex => Context.GetSortIndex(this);

    /// <summary>
    ///     Disposes the column and unregisters it from the grid context.
    /// </summary>
    public void Dispose() {
        Context?.RegisterColumn(this);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Renders the cell content for a given grid item.
    /// </summary>
    /// <param name="gridItem">The grid item to render content for.</param>
    /// <returns>A <see cref="RenderFragment" /> representing the cell content.</returns>
    public abstract RenderFragment RenderCellContent(TGridItem gridItem);

    /// <summary>
    ///     Sorts the grid by this column and refreshes the grid.
    /// </summary>
    public async Task SortAsync() {
        Context.SortByColumn(this);
        await Context.RefreshAsync();
    }

    /// <summary>
    ///     Called when the component is initialized. Registers the column with the grid context.
    /// </summary>
    protected override void OnInitialized() {
        base.OnInitialized();
        ArgumentNullException.ThrowIfNull(Context, nameof(Context));
        Context.RegisterColumn(this);
    }

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();
        if (NewColumn) {
            if (Sortable && IsDefaultSortColumn) {
                Context.SortByColumn(this);
            }
            NewColumn = false;
        }
    }
}