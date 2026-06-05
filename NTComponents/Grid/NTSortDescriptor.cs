namespace NTComponents;

/// <summary>
///     Describes a single sort applied by <see cref="NTDataGrid{TItem}" />.
/// </summary>
public sealed record NTSortDescriptor(string PropertyName, SortDirection Direction);

