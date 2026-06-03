using System.Diagnostics.CodeAnalysis;

namespace NTComponents.Grid.Infrastructure;

/// <summary>
///     Represents an empty row within the body of a data grid for a specified item type.
/// </summary>
/// <typeparam name="TGridItem">The type of the data item associated with the data grid row. Must have accessible properties or fields as required by the data grid.</typeparam>
[Obsolete("This legacy grid API is obsolete. Use NTDataGrid, NTPropertyColumn, and NTTemplateColumn instead.")]
public partial class TnTDataGridBodyRowEmpty<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] TGridItem>;