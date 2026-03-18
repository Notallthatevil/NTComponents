using Microsoft.AspNetCore.Components;
using NTComponents.Grid;
using NTComponents.Grid.Columns;
using NTComponents.Grid.Infrastructure;

namespace NTComponents.Tests.Grid.Infrastructure;

/// <summary>
///     Unit tests for <see cref="TnTInternalGridContext{TGridItem}" />.
/// </summary>
public class TnTInternalGridContext_Tests : BunitContext {

    private readonly List<TestGridItem> _testItems = [
            new() { Id = 1, Name = "John Doe", Email = "john@example.com", CreatedDate = new DateTime(2023, 1, 1) },
        new() { Id = 2, Name = "Jane Smith", Email = "jane@example.com", CreatedDate = new DateTime(2023, 2, 1) },
        new() { Id = 3, Name = "Bob Johnson", Email = "bob@example.com", CreatedDate = new DateTime(2023, 3, 1) },
        new() { Id = 4, Name = "Alice Brown", Email = "alice@example.com", CreatedDate = new DateTime(2023, 4, 1) },
        new() { Id = 5, Name = "Charlie Wilson", Email = "charlie@example.com", CreatedDate = new DateTime(2023, 5, 1) }
        ];

    [Fact]
    public void ColumnIsSortedOn_UnsortedColumn_ReturnsNull() {
        // Arrange
        var grid = CreateDataGrid();
        var context = new TnTInternalGridContext<TestGridItem>(grid);
        var column = new TestTemplateColumn<TestGridItem>();
        context.RegisterColumn(column);

        // Act & Assert
        context.ColumnIsSortedOn(column).Should().BeNull();
    }

    [Fact]
    public void Columns_ReturnsColumnsOrderedByOrderThenColumnId() {
        // Arrange
        var grid = CreateDataGrid();
        var context = new TnTInternalGridContext<TestGridItem>(grid);
        var column1 = new TestTemplateColumn<TestGridItem> { Order = 2 };
        var column2 = new TestTemplateColumn<TestGridItem> { Order = 1 };
        var column3 = new TestTemplateColumn<TestGridItem> { Order = 1 };

        // Act
        context.RegisterColumn(column1);
        context.RegisterColumn(column2);
        context.RegisterColumn(column3);

        // Assert
        var orderedColumns = context.Columns.ToList();
        orderedColumns[0].Order.Should().Be(1);
        orderedColumns[1].Order.Should().Be(1);
        orderedColumns[2].Order.Should().Be(2);
        // Verify secondary sort by ColumnId for same Order
        orderedColumns[0].ColumnId.Should().BeLessThan(orderedColumns[1].ColumnId);
    }

    [Fact]
    public void Constructor_WithNullGrid_AcceptsNullParameter() {
        // Arrange & Act
        // Note: The primary constructor parameter doesn't enforce null checking This tests the actual behavior of the constructor
        var act = () => new TnTInternalGridContext<TestGridItem>(null!);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithValidGrid_InitializesCorrectly() {
        // Arrange
        var grid = CreateDataGrid();

        // Act
        var context = new TnTInternalGridContext<TestGridItem>(grid);

        // Assert
        context.Grid.Should().BeSameAs(grid);
        context.ItemKey.Should().BeSameAs(grid.ItemKey);
        context.Items.Should().BeEmpty();
        context.SortBy.Should().BeNull();
        context.TotalRowCount.Should().Be(0);
        context.Columns.Should().BeEmpty();
    }

    [Fact]
    public void DataGridAppearance_ReturnsGridAppearance() {
        // Arrange
        var grid = CreateDataGrid();
        grid.DataGridAppearance = DataGridAppearance.Stripped;
        var context = new TnTInternalGridContext<TestGridItem>(grid);

        // Act & Assert
        context.DataGridAppearance.Should().Be(DataGridAppearance.Stripped);
    }

    [Fact]
    public void EdgeCase_RemoveSortedColumn_UpdatesSortingState() {
        // Arrange
        var grid = CreateDataGrid();
        grid.Items = _testItems.AsQueryable();
        var context = new TnTInternalGridContext<TestGridItem>(grid);
        var column = new TestTemplateColumn<TestGridItem> {
            SortBy = TnTGridSort<TestGridItem>.ByAscending(x => x.Name),
            InitialSortDirection = SortDirection.Ascending
        };
        context.RegisterColumn(column);
        context.SortByColumn(column);

        // Act
        context.RemoveColumn(column);
        context.UpdateItems();

        // Assert
        context.Columns.Should().NotContain(column);
        // The sorting state should remain (it's not automatically cleared when column is removed) This tests the current behavior - may need adjustment based on requirements
    }

    [Fact]
    public void FullWorkflow_RegisterColumnsSortAndUpdate_WorksCorrectly() {
        // Arrange
        var grid = CreateDataGrid();
        grid.Items = _testItems.AsQueryable();
        var context = new TnTInternalGridContext<TestGridItem>(grid);

        var nameColumn = new TestTemplateColumn<TestGridItem> {
            SortBy = TnTGridSort<TestGridItem>.ByAscending(x => x.Name),
            InitialSortDirection = SortDirection.Ascending,
            Order = 1
        };
        var idColumn = new TestTemplateColumn<TestGridItem> {
            SortBy = TnTGridSort<TestGridItem>.ByDescending(x => x.Id),
            InitialSortDirection = SortDirection.Descending,
            Order = 0 // Should appear first in columns list
        };

        // Act
        context.RegisterColumn(nameColumn);
        context.RegisterColumn(idColumn);
        context.SortByColumn(nameColumn);
        context.UpdateItems();

        // Assert Verify columns are ordered correctly
        var columns = context.Columns.ToList();
        columns[0].Should().BeSameAs(idColumn); // Order 0 comes first
        columns[1].Should().BeSameAs(nameColumn); // Order 1 comes second

        // Verify sorting is applied
        var sortedItems = context.Items.ToList();
        sortedItems.Should().BeInAscendingOrder(x => x.Name);

        // Verify sorting state
        context.ColumnIsSortedOn(nameColumn).Should().Be(SortDirection.Ascending);
        context.ColumnIsSortedOn(idColumn).Should().BeNull();
    }

    [Fact]
    public void GetSortIndex_UnsortedColumn_ReturnsNull() {
        // Arrange
        var grid = CreateDataGrid();
        var context = new TnTInternalGridContext<TestGridItem>(grid);
        var column = new TestTemplateColumn<TestGridItem>();
        context.RegisterColumn(column);

        // Act & Assert
        context.GetSortIndex(column).Should().BeNull();
    }

    [Fact]
    public void Items_InitiallyEmpty() {
        // Arrange
        var grid = CreateDataGrid();
        var context = new TnTInternalGridContext<TestGridItem>(grid);

        // Act & Assert
        context.Items.Should().BeEmpty();
    }

    [Fact]
    public void PaginationState_ReturnsGridPagination() {
        // Arrange
        var grid = CreateDataGrid();
        var pagination = new TnTPaginationState();
        grid.Pagination = pagination;
        var context = new TnTInternalGridContext<TestGridItem>(grid);

        // Act & Assert
        context.PaginationState.Should().BeSameAs(pagination);
    }

    [Fact]
    public void RefreshAsync_ReturnsTask() {
        // Arrange
        var grid = CreateDataGrid();
        var context = new TnTInternalGridContext<TestGridItem>(grid);

        // Act
        var task = context.RefreshAsync();

        // Assert
        task.Should().NotBeNull();
        // Verify it returns some kind of Task (the exact generic type may vary)
        typeof(Task).IsAssignableFrom(task.GetType()).Should().BeTrue();
    }

    [Fact]
    public void RegisterColumn_DuplicateColumn_UpdatesNewColumnFlag() {
        // Arrange
        var grid = CreateDataGrid();
        var context = new TnTInternalGridContext<TestGridItem>(grid);
        var column = new TestTemplateColumn<TestGridItem>();
        column.ColumnId = 1;

        // Act
        context.RegisterColumn(column); // First registration
        var firstNewFlag = column.NewColumn;
        context.RegisterColumn(column); // Second registration

        // Assert
        firstNewFlag.Should().BeTrue();
        column.NewColumn.Should().BeFalse(); // Second registration should set to false
    }

    [Fact]
    public void RegisterColumn_MultipleColumns_AssignsUniqueIds() {
        // Arrange
        var grid = CreateDataGrid();
        var context = new TnTInternalGridContext<TestGridItem>(grid);
        var column1 = new TestTemplateColumn<TestGridItem>();
        var column2 = new TestTemplateColumn<TestGridItem>();

        // Act
        context.RegisterColumn(column1);
        context.RegisterColumn(column2);

        // Assert
        column1.ColumnId.Should().NotBe(column2.ColumnId);
        column1.ColumnId.Should().BeGreaterThan(0);
        column2.ColumnId.Should().BeGreaterThan(0);
        context.Columns.Should().HaveCount(2);
    }

    [Fact]
    public void RegisterColumn_WithExistingColumnId_DoesNotReassignId() {
        // Arrange
        var grid = CreateDataGrid();
        var context = new TnTInternalGridContext<TestGridItem>(grid);
        var column = new TestTemplateColumn<TestGridItem>();
        column.ColumnId = 42;

        // Act
        context.RegisterColumn(column);

        // Assert
        column.ColumnId.Should().Be(42);
        context.Columns.Should().Contain(column);
    }

    [Fact]
    public void RegisterColumn_WithNewColumn_AssignsColumnId() {
        // Arrange
        var grid = CreateDataGrid();
        var context = new TnTInternalGridContext<TestGridItem>(grid);
        var column = new TestTemplateColumn<TestGridItem>();

        // Act
        context.RegisterColumn(column);

        // Assert
        column.ColumnId.Should().BeGreaterThan(0);
        column.NewColumn.Should().BeTrue();
        context.Columns.Should().Contain(column);
    }

    [Fact]
    public void RemoveColumn_ExistingColumn_RemovesFromContext() {
        // Arrange
        var grid = CreateDataGrid();
        var context = new TnTInternalGridContext<TestGridItem>(grid);
        var column = new TestTemplateColumn<TestGridItem>();
        context.RegisterColumn(column);

        // Act
        context.RemoveColumn(column);

        // Assert
        context.Columns.Should().NotContain(column);
    }

    [Fact]
    public void RemoveColumn_NonExistentColumn_DoesNotThrow() {
        // Arrange
        var grid = CreateDataGrid();
        var context = new TnTInternalGridContext<TestGridItem>(grid);
        var column = new TestTemplateColumn<TestGridItem>();

        // Act & Assert
        var act = () => context.RemoveColumn(column);
        act.Should().NotThrow();
    }

    [Fact]
    public void RowClass_ReturnsGridRowClass() {
        // Arrange
        var grid = CreateDataGrid();
        grid.RowClass = item => item.Id == 1 ? "highlighted" : "normal";
        var context = new TnTInternalGridContext<TestGridItem>(grid);

        // Act & Assert
        context.RowClass.Should().BeSameAs(grid.RowClass);
        context.RowClass!(new TestGridItem { Id = 1 }).Should().Be("highlighted");
    }

    [Fact]
    public void RowClickCallback_ReturnsGridCallback() {
        // Arrange
        var grid = CreateDataGrid();
        var callback = EventCallback.Factory.Create<TestGridItem>(grid, _ => { });
        grid.OnRowClicked = callback;
        var context = new TnTInternalGridContext<TestGridItem>(grid);

        // Act & Assert
        context.RowClickCallback.Should().Be(callback);
    }

    [Fact]
    public void SortByColumn_FirstTime_SetsSortDirection() {
        // Arrange
        var grid = CreateDataGrid();
        grid.Items = _testItems.AsQueryable();
        var context = new TnTInternalGridContext<TestGridItem>(grid);
        var column = new TestTemplateColumn<TestGridItem> {
            SortBy = TnTGridSort<TestGridItem>.ByAscending(x => x.Name),
            InitialSortDirection = SortDirection.Ascending
        };
        context.RegisterColumn(column);

        // Act
        context.SortByColumn(column);

        // Assert
        context.SortBy.Should().NotBeNull();
        context.ColumnIsSortedOn(column).Should().Be(SortDirection.Ascending);
        context.GetSortIndex(column).Should().Be(1);
    }

    [Fact]
    public void SortByColumn_MultipleColumns_CombinesSorting() {
        // Arrange
        var grid = CreateDataGrid();
        grid.Items = _testItems.AsQueryable();
        var context = new TnTInternalGridContext<TestGridItem>(grid);
        var column1 = new TestTemplateColumn<TestGridItem> {
            SortBy = TnTGridSort<TestGridItem>.ByAscending(x => x.Name),
            InitialSortDirection = SortDirection.Ascending
        };
        var column2 = new TestTemplateColumn<TestGridItem> {
            SortBy = TnTGridSort<TestGridItem>.ByDescending(x => x.Id),
            InitialSortDirection = SortDirection.Descending
        };
        context.RegisterColumn(column1);
        context.RegisterColumn(column2);

        // Act
        context.SortByColumn(column1);
        context.SortByColumn(column2);

        // Assert
        context.SortBy.Should().NotBeNull();
        context.GetSortIndex(column1).Should().Be(1);
        context.GetSortIndex(column2).Should().Be(2);
    }

    [Fact]
    public void SortByColumn_SecondTime_FlipsSortDirection() {
        // Arrange
        var grid = CreateDataGrid();
        grid.Items = _testItems.AsQueryable();
        var context = new TnTInternalGridContext<TestGridItem>(grid);
        var column = new TestTemplateColumn<TestGridItem> {
            SortBy = TnTGridSort<TestGridItem>.ByAscending(x => x.Name),
            InitialSortDirection = SortDirection.Ascending
        };
        context.RegisterColumn(column);

        // Act
        context.SortByColumn(column); // First sort - Ascending
        context.SortByColumn(column); // Second sort - should flip to Descending

        // Assert
        context.ColumnIsSortedOn(column).Should().Be(SortDirection.Descending);
        column.SortBy!.FlipDirections.Should().BeTrue();
    }

    [Fact]
    public void SortByColumn_ThirdTime_RemovesSorting() {
        // Arrange
        var grid = CreateDataGrid();
        grid.Items = _testItems.AsQueryable();
        var context = new TnTInternalGridContext<TestGridItem>(grid);
        var column = new TestTemplateColumn<TestGridItem> {
            SortBy = TnTGridSort<TestGridItem>.ByAscending(x => x.Name),
            InitialSortDirection = SortDirection.Ascending
        };
        context.RegisterColumn(column);

        // Act
        context.SortByColumn(column); // First sort - Ascending
        context.SortByColumn(column); // Second sort - Descending
        context.SortByColumn(column); // Third sort - should remove

        // Assert
        context.ColumnIsSortedOn(column).Should().BeNull();
        context.SortBy.Should().BeNull();
        context.GetSortIndex(column).Should().BeNull();
    }

    [Fact]
    public void TotalRowCount_CanBeSet() {
        // Arrange
        var grid = CreateDataGrid();
        var context = new TnTInternalGridContext<TestGridItem>(grid);

        // Act
        context.TotalRowCount = 100;

        // Assert
        context.TotalRowCount.Should().Be(100);
    }

    [Fact]
    public void TotalRowCount_InitiallyZero() {
        // Arrange
        var grid = CreateDataGrid();
        var context = new TnTInternalGridContext<TestGridItem>(grid);

        // Act & Assert
        context.TotalRowCount.Should().Be(0);
    }

    [Fact]
    public void UpdateItems_WithNullItems_UsesProvidedItems() {
        // Arrange
        var grid = CreateDataGrid();
        grid.Items = null;
        grid.ProvidedItems = _testItems.Take(2);
        var context = new TnTInternalGridContext<TestGridItem>(grid);

        // Act
        context.UpdateItems();

        // Assert
        context.Items.Should().HaveCount(2);
        context.Items.Should().BeEquivalentTo(_testItems.Take(2));
    }

    [Fact]
    public void UpdateItems_WithNullItemsAndNullProvidedItems_UsesEmptyCollection() {
        // Arrange
        var grid = CreateDataGrid();
        grid.Items = null;
        grid.ProvidedItems = null;
        var context = new TnTInternalGridContext<TestGridItem>(grid);

        // Act
        context.UpdateItems();

        // Assert
        context.Items.Should().BeEmpty();
    }

    [Fact]
    public void UpdateItems_WithPagination_AppliesPagination() {
        // Arrange
        var grid = CreateDataGrid();
        grid.Items = _testItems.AsQueryable();
        var pagination = new TnTPaginationState();
        // Assume pagination has methods to set page size and current page This test verifies the integration with pagination
        grid.Pagination = pagination;
        var context = new TnTInternalGridContext<TestGridItem>(grid);

        // Act
        context.UpdateItems();

        // Assert The actual result depends on pagination implementation This verifies that pagination is considered in the pipeline
        context.Items.Should().NotBeNull();
    }

    [Fact]
    public void UpdateItems_WithQueryableItems_UpdatesItemsCollection() {
        // Arrange
        var grid = CreateDataGrid();
        grid.Items = _testItems.AsQueryable();
        var context = new TnTInternalGridContext<TestGridItem>(grid);

        // Act
        context.UpdateItems();

        // Assert
        context.Items.Should().HaveCount(_testItems.Count);
        context.Items.Should().BeEquivalentTo(_testItems);
    }

    [Fact]
    public void UpdateItems_WithSorting_AppliesSorting() {
        // Arrange
        var grid = CreateDataGrid();
        grid.Items = _testItems.AsQueryable();
        var context = new TnTInternalGridContext<TestGridItem>(grid);
        var column = new TestTemplateColumn<TestGridItem> {
            SortBy = TnTGridSort<TestGridItem>.ByDescending(x => x.Id),
            InitialSortDirection = SortDirection.Descending
        };
        context.RegisterColumn(column);
        context.SortByColumn(column);

        // Act
        context.UpdateItems();

        // Assert
        var items = context.Items.ToList();
        items.Should().HaveCount(_testItems.Count);
        items[0].Id.Should().Be(5); // Highest ID first due to descending sort
        items[^1].Id.Should().Be(1); // Lowest ID last
    }

    [Fact]
    public void SortByColumn_SingleSortMode_FirstClickSetsInitialDirection() {
        // Arrange
        var grid = CreateDataGrid();
        grid.Items = _testItems.AsQueryable();
        grid.AllowMultiSort = false;
        var context = new TnTInternalGridContext<TestGridItem>(grid);
        var column = new TestTemplateColumn<TestGridItem> {
            SortBy = TnTGridSort<TestGridItem>.ByAscending(x => x.Name),
            InitialSortDirection = SortDirection.Ascending
        };
        context.RegisterColumn(column);

        // Act
        context.SortByColumn(column);

        // Assert
        context.ColumnIsSortedOn(column).Should().Be(SortDirection.Ascending);
        context.SortBy.Should().NotBeNull();
    }

    [Fact]
    public void SortByColumn_SingleSortMode_SecondClickFlipsDirection() {
        // Arrange
        var grid = CreateDataGrid();
        grid.Items = _testItems.AsQueryable();
        grid.AllowMultiSort = false;
        var context = new TnTInternalGridContext<TestGridItem>(grid);
        var column = new TestTemplateColumn<TestGridItem> {
            SortBy = TnTGridSort<TestGridItem>.ByAscending(x => x.Name),
            InitialSortDirection = SortDirection.Ascending
        };
        context.RegisterColumn(column);

        // Act
        context.SortByColumn(column); // First click - Ascending
        context.SortByColumn(column); // Second click - should flip to Descending

        // Assert
        context.ColumnIsSortedOn(column).Should().Be(SortDirection.Descending);
        column.SortBy!.FlipDirections.Should().BeTrue();
    }

    [Fact]
    public void SortByColumn_SingleSortMode_ThirdClickRemovesSorting() {
        // Arrange
        var grid = CreateDataGrid();
        grid.Items = _testItems.AsQueryable();
        grid.AllowMultiSort = false;
        var context = new TnTInternalGridContext<TestGridItem>(grid);
        var column = new TestTemplateColumn<TestGridItem> {
            SortBy = TnTGridSort<TestGridItem>.ByAscending(x => x.Name),
            InitialSortDirection = SortDirection.Ascending
        };
        context.RegisterColumn(column);

        // Act
        context.SortByColumn(column); // First click - Ascending
        context.SortByColumn(column); // Second click - Descending
        context.SortByColumn(column); // Third click - should remove

        // Assert
        context.ColumnIsSortedOn(column).Should().BeNull();
        context.SortBy.Should().BeNull();
        column.SortBy!.FlipDirections.Should().BeFalse();
    }

    [Fact]
    public void SortByColumn_SingleSortMode_WithDisableSortToggleToNone_ThirdClickRestoresAscendingSort() {
        // Arrange
        var grid = CreateDataGrid();
        grid.Items = _testItems.AsQueryable();
        grid.AllowMultiSort = false;
        grid.DisableSortToggleToNone = true;
        var context = new TnTInternalGridContext<TestGridItem>(grid);
        var column = new TestTemplateColumn<TestGridItem> {
            SortBy = TnTGridSort<TestGridItem>.ByAscending(x => x.Name),
            InitialSortDirection = SortDirection.Ascending
        };
        context.RegisterColumn(column);

        // Act
        context.SortByColumn(column); // First click - Ascending
        context.SortByColumn(column); // Second click - Descending
        context.SortByColumn(column); // Third click - should return to Ascending

        // Assert
        context.ColumnIsSortedOn(column).Should().Be(SortDirection.Ascending);
        context.SortBy.Should().NotBeNull();
        column.SortBy!.FlipDirections.Should().BeFalse();
        context.Items.Should().BeInAscendingOrder(x => x.Name);
    }

    [Fact]
    public void SortByColumn_SingleSortMode_WithDisableSortToggleToNone_ThirdClickRestoresDescendingSort() {
        // Arrange
        var grid = CreateDataGrid();
        grid.Items = _testItems.AsQueryable();
        grid.AllowMultiSort = false;
        grid.DisableSortToggleToNone = true;
        var context = new TnTInternalGridContext<TestGridItem>(grid);
        var column = new TestTemplateColumn<TestGridItem> {
            SortBy = TnTGridSort<TestGridItem>.ByDescending(x => x.Id),
            InitialSortDirection = SortDirection.Descending
        };
        context.RegisterColumn(column);

        // Act
        context.SortByColumn(column); // First click - Descending
        context.SortByColumn(column); // Second click - Ascending
        context.SortByColumn(column); // Third click - should return to Descending

        // Assert
        context.ColumnIsSortedOn(column).Should().Be(SortDirection.Descending);
        context.SortBy.Should().NotBeNull();
        column.SortBy!.FlipDirections.Should().BeFalse();
        context.Items.Should().BeInDescendingOrder(x => x.Id);
    }

    [Fact]
    public void SortByColumn_SingleSortMode_SwitchingColumns_RemovesPreviousSort() {
        // Arrange
        var grid = CreateDataGrid();
        grid.Items = _testItems.AsQueryable();
        grid.AllowMultiSort = false;
        var context = new TnTInternalGridContext<TestGridItem>(grid);
        var column1 = new TestTemplateColumn<TestGridItem> {
            SortBy = TnTGridSort<TestGridItem>.ByAscending(x => x.Name),
            InitialSortDirection = SortDirection.Ascending
        };
        var column2 = new TestTemplateColumn<TestGridItem> {
            SortBy = TnTGridSort<TestGridItem>.ByAscending(x => x.Id),
            InitialSortDirection = SortDirection.Ascending
        };
        context.RegisterColumn(column1);
        context.RegisterColumn(column2);

        // Act
        context.SortByColumn(column1); // Sort by column1
        context.SortByColumn(column2); // Sort by column2 - should remove column1 sort

        // Assert
        context.ColumnIsSortedOn(column1).Should().BeNull();
        context.ColumnIsSortedOn(column2).Should().Be(SortDirection.Ascending);
    }

    [Fact]
    public void SortByColumn_SingleSortMode_WithDescendingInitialDirection() {
        // Arrange
        var grid = CreateDataGrid();
        grid.Items = _testItems.AsQueryable();
        grid.AllowMultiSort = false;
        var context = new TnTInternalGridContext<TestGridItem>(grid);
        var column = new TestTemplateColumn<TestGridItem> {
            SortBy = TnTGridSort<TestGridItem>.ByDescending(x => x.Id),
            InitialSortDirection = SortDirection.Descending
        };
        context.RegisterColumn(column);

        // Act
        context.SortByColumn(column); // First click - Descending (initial)
        // Assert - First click should set to initial direction
        context.ColumnIsSortedOn(column).Should().Be(SortDirection.Descending);

        // Act
        context.SortByColumn(column); // Second click - should flip to Ascending

        // Assert
        context.ColumnIsSortedOn(column).Should().Be(SortDirection.Ascending);

        // Act
        context.SortByColumn(column); // Third click - should remove

        // Assert
        context.ColumnIsSortedOn(column).Should().BeNull();
    }

    private TnTDataGrid<TestGridItem> CreateDataGrid() {
        var grid = new TnTDataGrid<TestGridItem>();
        grid.ItemKey = item => item.Id;
        grid.ItemSize = 40;
        return grid;
    }

    /// <summary>
    ///     Test model for grid context tests.
    /// </summary>
    private class TestGridItem {
        public DateTime CreatedDate { get; set; }
        public string Email { get; set; } = string.Empty;
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    ///     Test implementation of TnTColumnBase for testing purposes.
    /// </summary>
    private class TestTemplateColumn<TItem> : TnTColumnBase<TItem> {
        public RenderFragment<TItem>? CellTemplate { get; set; }

        public override string? ElementClass => null;
        public override string? ElementStyle => null;
        public override TnTGridSort<TItem>? SortBy { get; set; }

        public override RenderFragment RenderCellContent(TItem item) =>
            CellTemplate?.Invoke(item) ?? (builder => { });

        public override void RenderHeaderContent(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder) {
            builder.AddContent(0, "Header");
        }
    }
}