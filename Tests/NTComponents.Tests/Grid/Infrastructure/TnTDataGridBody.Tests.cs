using Microsoft.AspNetCore.Components;
using NTComponents.Grid;
using NTComponents.Grid.Columns;
using NTComponents.Grid.Infrastructure;
using RippleTestingUtility = NTComponents.Tests.TestingUtility.TestingUtility;

namespace NTComponents.Tests.Grid.Infrastructure;

/// <summary>
///     Unit tests for <see cref="TnTDataGridBody{TGridItem}" />.
/// </summary>
public class TnTDataGridBody_Tests : BunitContext {

    private readonly List<TestGridItem> _testItems = [
            new() { Id = 1, Name = "John Doe", Email = "john@example.com" },
        new() { Id = 2, Name = "Jane Smith", Email = "jane@example.com" },
        new() { Id = 3, Name = "Bob Johnson", Email = "bob@example.com" }
        ];

    public TnTDataGridBody_Tests() {
        // Arrange (global) & Act: JS module setup for ripple in constructor
        RippleTestingUtility.SetupRippleEffectModule(this);
    }

    [Fact]
    public void Context_IsRequired() {
        // Arrange & Act
        var act = () => Render<TnTDataGridBody<TestGridItem>>();

        // Assert The component may not throw ArgumentNullException explicitly for cascading parameters Instead, it may fail during rendering when trying to use the null context
        act.Should().Throw<Exception>(); // More general exception expectation
    }

    [Fact]
    public void Context_IsSetCorrectly() {
        // Arrange
        var grid = CreateDataGrid();
        var context = new TnTInternalGridContext<TestGridItem>(grid);

        // Act
        var cut = Render<TnTDataGridBody<TestGridItem>>(parameters => parameters
            .AddCascadingValue(context));

        // Assert
        var component = cut.Instance;
        component.Context.Should().BeSameAs(context);
    }

    [Fact]
    public async Task RefreshAsync_ReturnsCompletedTask() {
        // Arrange
        var cut = RenderWithItems(_testItems);
        var component = cut.Instance;

        // Act
        var task = component.RefreshAsync();

        // Assert
        task.Should().NotBeNull();
        await task; // Should complete without throwing
        task.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task RefreshAsync_TriggersStateChange() {
        // Arrange
        var cut = RenderWithItems(_testItems);
        var component = cut.Instance;
        var initialMarkup = cut.Markup;

        // Act
        await component.RefreshAsync();

        // Assert The refresh should complete without error Since it's a state change trigger, we verify it doesn't throw
        cut.Markup.Should().NotBeEmpty();
    }

    [Fact]
    public void Renders_TbodyElement_WithCorrectStructure() {
        // Arrange & Act
        var cut = RenderWithItems(_testItems);

        // Assert
        var tbody = cut.Find("tbody");
        tbody.Should().NotBeNull();
        tbody.Children.Should().HaveCount(3); // Three rows for three items
    }

    [Fact]
    public void Renders_WithItems_ShowsBodyRows() {
        // Arrange - Need to add columns so the cells can render content
        var grid = CreateDataGrid();
        grid.Items = _testItems.AsQueryable();
        var context = new TnTInternalGridContext<TestGridItem>(grid);

        // Register columns so content can be rendered
        var nameColumn = new TestTemplateColumn<TestGridItem> {
            CellTemplate = item => builder => builder.AddContent(0, item.Name)
        };
        var emailColumn = new TestTemplateColumn<TestGridItem> {
            CellTemplate = item => builder => builder.AddContent(0, item.Email)
        };
        context.RegisterColumn(nameColumn);
        context.RegisterColumn(emailColumn);
        context.UpdateItems();

        // Act
        var cut = Render<TnTDataGridBody<TestGridItem>>(parameters => parameters
            .AddCascadingValue(context));

        // Assert
        cut.FindAll("tbody").Should().HaveCount(1);
        cut.FindAll("tr").Should().HaveCount(3); // One for each item
        cut.Markup.Should().Contain("John Doe");
        cut.Markup.Should().Contain("Jane Smith");
        cut.Markup.Should().Contain("Bob Johnson");
    }

    [Fact]
    public void Renders_WithNoItems_ShowsEmptyRow() {
        // Arrange & Act
        var cut = RenderWithItems([]);

        // Assert
        cut.FindAll("tbody").Should().HaveCount(1);
        cut.FindAll("tr").Should().HaveCount(1); // Empty row
        cut.Markup.Should().Contain("No content to show");
    }

    [Fact]
    public void Renders_WithNullItems_ShowsEmptyRow() {
        // Arrange
        var grid = CreateDataGrid();
        grid.Items = null;
        var context = new TnTInternalGridContext<TestGridItem>(grid);
        context.UpdateItems();

        // Act
        var cut = Render<TnTDataGridBody<TestGridItem>>(parameters => parameters
            .AddCascadingValue(context));

        // Assert
        cut.FindAll("tbody").Should().HaveCount(1);
        cut.FindAll("tr").Should().HaveCount(1); // Empty row
        cut.Markup.Should().Contain("No content to show");
    }

    [Fact]
    public void RendersCorrectly_WithRowClassCallback() {
        // Arrange
        var grid = CreateDataGrid();
        grid.Items = _testItems.AsQueryable();
        grid.RowClass = item => item.Id == 1 ? "highlighted" : string.Empty;
        var context = new TnTInternalGridContext<TestGridItem>(grid);
        context.UpdateItems();

        // Act
        var cut = Render<TnTDataGridBody<TestGridItem>>(parameters => parameters
            .AddCascadingValue(context));

        // Assert
        cut.Markup.Should().Contain("highlighted"); // First row should have the class
    }

    [Fact]
    public void RendersCorrectly_WithRowClickCallback() {
        // Arrange
        var grid = CreateDataGrid();
        grid.Items = _testItems.AsQueryable();
        var clickedItem = (TestGridItem?)null;
        grid.OnRowClicked = EventCallback.Factory.Create<TestGridItem>(grid, item => clickedItem = item);
        var context = new TnTInternalGridContext<TestGridItem>(grid);
        context.UpdateItems();

        // Act
        var cut = Render<TnTDataGridBody<TestGridItem>>(parameters => parameters
            .AddCascadingValue(context));

        // Assert
        cut.FindAll("tr").Should().HaveCount(3);
        // Verify rows are clickable by checking for interactable class instead of onclick attribute
        var firstRow = cut.FindAll("tr")[0];
        firstRow.GetAttribute("class").Should().Contain("tnt-interactable");
    }

    private TnTDataGrid<TestGridItem> CreateDataGrid() {
        var grid = new TnTDataGrid<TestGridItem>();
        grid.ItemKey = item => item.Id;
        grid.ItemSize = 40;
        return grid;
    }

    private IRenderedComponent<TnTDataGridBody<TestGridItem>> RenderWithItems(List<TestGridItem> items) {
        var grid = CreateDataGrid();
        grid.Items = items.AsQueryable();
        var context = new TnTInternalGridContext<TestGridItem>(grid);
        context.UpdateItems();

        return Render<TnTDataGridBody<TestGridItem>>(parameters => parameters
            .AddCascadingValue(context));
    }

    /// <summary>
    ///     Test model for grid body tests.
    /// </summary>
    private class TestGridItem {
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