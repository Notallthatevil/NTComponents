using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Diagnostics.CodeAnalysis;
using NTComponents.Core;
using NTComponents.Grid.Columns;
using NTComponents.Grid.Infrastructure;

namespace NTComponents.Grid.Infrastructure;

/// <summary>
///     Base class for all data grid row components in NTComponents. Provides access to the grid context and common row logic.
/// </summary>
/// <typeparam name="TGridItem">The type of data represented by each row in the grid.</typeparam>
[CascadingTypeParameter(nameof(TGridItem))]
[Obsolete("This legacy grid API is obsolete. Use NTDataGrid, NTPropertyColumn, and NTTemplateColumn instead.")]
public abstract partial class TnTDataGridRow<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] TGridItem> : ComponentBase {

    /// <summary>
    ///     The internal grid context for this row, cascaded from the parent grid.
    /// </summary>
    [CascadingParameter]
    internal TnTInternalGridContext<TGridItem> Context { get; set; } = default!;

    /// <summary>
    ///     Gets the number of columns in the current context.
    /// </summary>
    protected int ColumnCount => Context?.Columns.Count() ?? 0;

    /// <summary>
    ///     Refreshes the row by invoking a re-render.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous operation.</returns>
    public virtual async Task RefreshAsync() {
        await InvokeAsync(StateHasChanged);
    }

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();
        ArgumentNullException.ThrowIfNull(Context, nameof(Context));
    }
}