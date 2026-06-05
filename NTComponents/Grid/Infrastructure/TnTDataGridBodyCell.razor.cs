using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;
using NTComponents.Core;

namespace NTComponents.Grid.Infrastructure;

/// <summary>
///     Represents a cell in the body of a data grid for a specific grid item type.
/// </summary>
/// <typeparam name="TGridItem">The type of the data item displayed in the cell.</typeparam>
[CascadingTypeParameter(nameof(TGridItem))]
[Obsolete("This legacy grid API is obsolete. Use NTDataGrid, NTPropertyColumn, and NTTemplateColumn instead.")]
public partial class TnTDataGridBodyCell<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] TGridItem> {

    /// <summary>
    ///     The data item associated with this cell.
    /// </summary>
    [Parameter, EditorRequired]
    public TGridItem Item { get; set; }

    /// <inheritdoc />
    protected override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(Column.AdditionalAttributes)
        .Add(Column.MaxWidth.HasValue ? $"max-width:{Column.MaxWidth.Value}px;word-wrap:break-word" : null!)
        .Build();

}