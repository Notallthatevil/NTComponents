using Microsoft.AspNetCore.Components;
using NTComponents.Core;
using NTComponents.Grid;

namespace NTComponents;

/// <summary>
///     Renders pagination buttons for navigating between pages in a grid.
/// </summary>
[Obsolete("This legacy grid API is obsolete. Use NTDataGrid, NTPropertyColumn, and NTTemplateColumn instead.")]
public partial class TnTPaginationButtons {

    /// <summary>
    ///     The background color for the active (selected) pagination button.
    /// </summary>
    [Parameter]
    public TnTColor ActiveBackgroundColor { get; set; } = TnTColor.Primary;

    /// <summary>
    ///     The text color for the active (selected) pagination button.
    /// </summary>
    [Parameter]
    public TnTColor ActiveTextColor { get; set; } = TnTColor.OnPrimary;

    /// <summary>
    ///     The background color for non-active pagination buttons.
    /// </summary>
    [Parameter]
    public TnTColor BackgroundColor { get; set; } = TnTColor.PrimaryContainer;

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("tnt-pagination-buttons")
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .Build();

    /// <summary>
    ///     The pagination state that controls the current page and total pages.
    /// </summary>
    [Parameter, EditorRequired]
    public TnTPaginationState PaginationState { get; set; } = default!;

    /// <summary>
    ///     The text color for non-active pagination buttons.
    /// </summary>
    [Parameter]
    public TnTColor TextColor { get; set; } = TnTColor.OnPrimaryContainer;

    /// <summary>
    ///     The total number of pages, calculated from <see cref="PaginationState" />.
    /// </summary>
    private int _numberOfPages;
    private TnTPaginationState? _lastUsedState;

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();
        ArgumentNullException.ThrowIfNull(PaginationState, nameof(PaginationState));

        if (_lastUsedState != PaginationState) {
            PaginationState.TotalItemCountChangedCallback -= PageCountChangedAsync;

            _lastUsedState = PaginationState;
            _lastUsedState.TotalItemCountChangedCallback += PageCountChangedAsync;
        }

        _numberOfPages = (PaginationState?.LastPageIndex ?? 0) + 1;
    }

    private async Task PageCountChangedAsync(TnTPaginationState paginationState) {
        _numberOfPages = (paginationState.LastPageIndex ?? 0) + 1;
        await InvokeAsync(StateHasChanged);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing) {
        _lastUsedState?.TotalItemCountChangedCallback -= PageCountChangedAsync;
        _lastUsedState = null;
        base.Dispose(disposing);
    }
}