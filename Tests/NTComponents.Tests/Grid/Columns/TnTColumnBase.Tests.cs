using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System.Reflection;
using System.Threading.Tasks;
using NTComponents.Grid;
using NTComponents.Grid.Columns;
using NTComponents.Grid.Infrastructure;

namespace NTComponents.Tests.Grid.Columns;

public class TnTColumnBase_Tests : BunitContext {

    [Fact]
    public void Dispose_RegistersColumn() {
        // Arrange
        var grid = CreateDataGrid();
        var context = new TnTInternalGridContext<TestGridItem>(grid);
        var column = new TestTemplateColumn<TestGridItem>();
        column.Context = context;
        column.ColumnId = -1;

        // Act
        column.Dispose();

        // Assert
        column.ColumnId.Should().BeGreaterThan(0);
        context.Columns.Should().Contain(column);
    }

    [Fact]
    public void OnInitialized_RegistersColumnWithContext() {
        // Arrange
        var grid = CreateDataGrid();
        var context = new TnTInternalGridContext<TestGridItem>(grid);
        var column = new TestTemplateColumn<TestGridItem>();
        column.Context = context;

        // Act
        column.InvokeOnInitialized();

        // Assert
        column.ColumnId.Should().BeGreaterThan(0);
        context.Columns.Should().Contain(column);
    }

    [Fact]
    public void OnInitialized_WithNullContext_ThrowsArgumentNullException() {
        // Arrange
        var column = new TestTemplateColumn<TestGridItem>();
        column.Context = null!;

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => column.InvokeOnInitialized());
        ex.ParamName.Should().Be("Context");
    }

    [Fact]
    public void OnParametersSet_WhenNewColumnAndDefaultSort_CallsSortAndClearsNewFlag() {
        // Arrange
        var grid = CreateDataGrid();
        grid.Items = new[] {
            new TestGridItem { Id = 1, Name = "A" }
        }.AsQueryable();
        var context = new TnTInternalGridContext<TestGridItem>(grid);
        var column = new TestTemplateColumn<TestGridItem> {
            SortBy = TnTGridSort<TestGridItem>.ByAscending(x => x.Name),
            InitialSortDirection = SortDirection.Ascending,
            Sortable = true,
            IsDefaultSortColumn = true
        };
        column.Context = context;
        column.NewColumn = true;

        // Act
        column.InvokeOnParametersSet();

        // Assert
        column.NewColumn.Should().BeFalse();
        context.ColumnIsSortedOn(column).Should().Be(SortDirection.Ascending);
    }

    private TnTDataGrid<TestGridItem> CreateDataGrid() {
        var grid = new TnTDataGrid<TestGridItem>();
        grid.ItemKey = item => item.Id;
        grid.ItemSize = 40;
        return grid;
    }

    private class TestGridItem {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class TestTemplateColumn<TItem> : TnTColumnBase<TItem> {
        public override string? ElementClass => null;
        public override string? ElementStyle => null;
        public override TnTGridSort<TItem>? SortBy { get; set; }

        // Expose protected OnInitialized for tests
        public void InvokeOnInitialized() => base.OnInitialized();

        // Use reflection to invoke the protected OnParametersSet
        public void InvokeOnParametersSet() {
            var method = typeof(TnTColumnBase<TItem>).GetMethod("OnParametersSet", BindingFlags.Instance | BindingFlags.NonPublic);
            method!.Invoke(this, null);
        }

        public override RenderFragment RenderCellContent(TItem gridItem) => builder => { };

        // Override RenderHeaderContent to match base virtual
        public override void RenderHeaderContent(RenderTreeBuilder builder) => builder.AddContent(0, "Header");
    }
}