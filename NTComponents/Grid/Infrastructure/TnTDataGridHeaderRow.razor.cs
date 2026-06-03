using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;
using NTComponents.Core;

namespace NTComponents.Grid.Infrastructure;

/// <summary>
///     Represents the header row for a data grid component, providing column headers and supporting generic grid item types.
/// </summary>
/// <typeparam name="TGridItem">The type of the grid item displayed in the data grid.</typeparam>
[CascadingTypeParameter(nameof(TGridItem))]
[Obsolete("This legacy grid API is obsolete. Use NTDataGrid, NTPropertyColumn, and NTTemplateColumn instead.")]
public partial class TnTDataGridHeaderRow<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] TGridItem>;