using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;
using NTComponents.Core;

namespace NTComponents.Grid.Infrastructure;

/// <summary>
///     Represents the body section of a <see cref="TnTDataGrid{TGridItem}" />. Responsible for rendering grid rows and handling refresh logic.
/// </summary>
/// <typeparam name="TGridItem">The type of data represented by each row in the grid.</typeparam>
[CascadingTypeParameter(nameof(TGridItem))]
[Obsolete("This legacy grid API is obsolete. Use NTDataGrid, NTPropertyColumn, and NTTemplateColumn instead.")]
public partial class TnTDataGridBody<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] TGridItem> {

    /// <summary>
    ///     The internal grid context containing state and configuration for the grid.
    /// </summary>
    [CascadingParameter]
    internal TnTInternalGridContext<TGridItem> Context { get; set; } = default!;

    /// <summary>
    ///     Refreshes the grid body by triggering a re-render.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous operation.</returns>
    public virtual Task RefreshAsync() => InvokeAsync(StateHasChanged);
}