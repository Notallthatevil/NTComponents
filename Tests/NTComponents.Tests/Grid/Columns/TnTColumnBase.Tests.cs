using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System.Threading.Tasks;
using NTComponents.Core;
using NTComponents.Grid;
using NTComponents.Grid.Columns;
using NTComponents.Grid.Infrastructure;
using RippleTestingUtility = NTComponents.Tests.TestingUtility.TestingUtility;

namespace NTComponents.Tests.Grid.Columns;

public class TnTColumnBase_Tests : BunitContext {
    public TnTColumnBase_Tests() {
        RippleTestingUtility.SetupRippleEffectModule(this);
        JSInterop.SetupModule("./_content/NTComponents/Tooltip/TnTTooltip.razor.js").SetupVoid().SetVoidResult();
        var dataGridModule = JSInterop.SetupModule("./_content/NTComponents/Grid/TnTDataGrid.razor.js");
        dataGridModule.SetupVoid().SetVoidResult();
        dataGridModule.Setup<int>("getBodyHeight", _ => true).SetResult(400);
    }

    [Fact]
    public void ElementClass_Merges_Consumer_Class_And_Text_Alignment() {
        var column = new TestTemplateColumn<TestGridItem> {
            AdditionalAttributes = new Dictionary<string, object> { ["class"] = "consumer-column" },
            TextAlign = TextAlign.Right
        };

        column.ElementClass.Should().Contain("nt-grid-cell").And.Contain("consumer-column").And.Contain("tnt-text-align-right");
    }

    [Fact]
    public void RenderHeaderContent_Unsorted_Sortable_Header_Shows_Title_Ripple_And_Interaction_State() {
        var (column, _) = CreateRegisteredColumn();
        column.Title = "Customer name";
        column.Sortable = true;
        column.HeaderAlignment = TextAlign.Center;

        var cut = Render((RenderFragment)column.RenderBaseHeader);

        cut.Find(".tnt-column-header-title").TextContent.Should().Be("Customer name");
        cut.Find(".tnt-header-content").ClassList.Should().Contain("tnt-interactable").And.Contain("tnt-text-align-center");
        cut.FindAll(".tnt-column-header-sort-icon").Should().BeEmpty();
        cut.FindComponent<TnTRippleEffect>().Should().NotBeNull();
    }

    [Fact]
    public void RenderHeaderContent_NonSortable_Header_Has_No_Sort_Or_Ripple_Affordance() {
        var (column, _) = CreateRegisteredColumn();
        column.Title = "Customer name";
        column.Sortable = false;

        var cut = Render((RenderFragment)column.RenderBaseHeader);

        cut.Find(".tnt-header-content").ClassList.Should().NotContain("tnt-interactable");
        cut.FindAll(".tnt-column-header-sort-icon").Should().BeEmpty();
        cut.FindComponents<TnTRippleEffect>().Should().BeEmpty();
    }

    [Fact]
    public void RenderHeaderContent_Custom_Template_And_Tooltip_Replace_The_Default_Header() {
        var (column, _) = CreateRegisteredColumn();
        column.Title = "Default title";
        column.Sortable = true;
        column.HeaderToolip = builder => builder.AddContent(0, "Sort customer names");
        column.HeaderCellItemTemplate = currentColumn => builder => {
            builder.OpenElement(0, "strong");
            builder.AddAttribute(1, "class", "custom-header");
            builder.AddContent(2, $"Custom {currentColumn.ColumnId}");
            builder.CloseElement();
        };

        var cut = Render((RenderFragment)column.RenderBaseHeader);

        cut.Find(".tnt-tooltip-content").TextContent.Should().Contain("Sort customer names");
        cut.Find(".custom-header").TextContent.Should().Be($"Custom {column.ColumnId}");
        cut.Markup.Should().NotContain("Default title");
        cut.FindComponents<TnTRippleEffect>().Should().BeEmpty();
    }

    [Fact]
    public void RenderHeaderContent_Sorted_Header_Shows_Direction_And_MultiSort_Position() {
        var (ascendingColumn, ascendingContext) = CreateRegisteredColumn();
        ascendingColumn.Title = "Ascending";
        ascendingColumn.Sortable = true;
        ascendingContext.SortByColumn(ascendingColumn);
        var ascending = Render((RenderFragment)ascendingColumn.RenderBaseHeader);

        var (descendingColumn, descendingContext) = CreateRegisteredColumn();
        descendingColumn.Title = "Descending";
        descendingColumn.Sortable = true;
        descendingContext.SortByColumn(descendingColumn);
        descendingContext.SortByColumn(descendingColumn);
        var descending = Render((RenderFragment)descendingColumn.RenderBaseHeader);

        ascending.Find(".tnt-header-content").ClassList.Should().Contain("tnt-column-header-sorted-on");
        ascending.Find(".tnt-column-header-sort-icon").TextContent.Should().Contain(MaterialIcon.ArrowDropUp.Icon);
        ascending.Find(".tnt-column-header-sort-index").TextContent.Trim().Should().Be("1");
        descending.Find(".tnt-column-header-sort-icon").TextContent.Should().Contain(MaterialIcon.ArrowDropDown.Icon);
        descending.Find(".tnt-column-header-sort-index").TextContent.Trim().Should().Be("1");
    }

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
    public void Dispose_Without_A_Context_Is_Idempotent() {
        var column = new TestTemplateColumn<TestGridItem> { Context = null! };

        var firstDispose = Record.Exception(column.Dispose);
        var secondDispose = Record.Exception(column.Dispose);

        firstDispose.Should().BeNull();
        secondDispose.Should().BeNull();
        column.IsSortedOn.Should().BeNull();
    }

    [Fact]
    public async Task SortAsync_Updates_The_Context_And_Completes_The_Grid_Refresh() {
        var grid = Render<TnTDataGrid<TestGridItem>>(parameters => parameters
            .Add(component => component.Items, new[] { new TestGridItem { Id = 1, Name = "Alpha" } }.AsQueryable()));
        var context = new TnTInternalGridContext<TestGridItem>(grid.Instance);
        var column = new TestTemplateColumn<TestGridItem> {
            Context = context,
            SortBy = TnTGridSort<TestGridItem>.ByAscending(item => item.Name),
            InitialSortDirection = SortDirection.Ascending,
            Sortable = true
        };
        context.RegisterColumn(column);

        await column.SortAsync();

        context.ColumnIsSortedOn(column).Should().Be(SortDirection.Ascending);
        context.SortBy.Should().BeSameAs(column.SortBy);
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

    private (TestTemplateColumn<TestGridItem> Column, TnTInternalGridContext<TestGridItem> Context) CreateRegisteredColumn() {
        var context = new TnTInternalGridContext<TestGridItem>(CreateDataGrid());
        var column = new TestTemplateColumn<TestGridItem> {
            Context = context,
            SortBy = TnTGridSort<TestGridItem>.ByAscending(item => item.Name),
            InitialSortDirection = SortDirection.Ascending
        };
        context.RegisterColumn(column);
        return (column, context);
    }

    private class TestGridItem {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class TestTemplateColumn<TItem> : TnTColumnBase<TItem> {
        public override string? ElementStyle => null;
        public override TnTGridSort<TItem>? SortBy { get; set; }

        // Expose protected OnInitialized for tests
        public void InvokeOnInitialized() => base.OnInitialized();

        public void InvokeOnParametersSet() => base.OnParametersSet();

        public void RenderBaseHeader(RenderTreeBuilder builder) => base.RenderHeaderContent(builder);

        public override RenderFragment RenderCellContent(TItem gridItem) => builder => { };

        // Override RenderHeaderContent to match base virtual
        public override void RenderHeaderContent(RenderTreeBuilder builder) => builder.AddContent(0, "Header");
    }
}
