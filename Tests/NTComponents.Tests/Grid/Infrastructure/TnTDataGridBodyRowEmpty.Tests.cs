using Microsoft.AspNetCore.Components;
using NTComponents.Grid;
using NTComponents.Grid.Columns;
using NTComponents.Grid.Infrastructure;
using RippleTestingUtility = NTComponents.Tests.TestingUtility.TestingUtility;

namespace NTComponents.Tests.Grid.Infrastructure;

/// <summary>
///     Unit tests for <see cref="TnTDataGridBodyRowEmpty{TGridItem}" />.
/// </summary>
public class TnTDataGridBodyRowEmpty_Tests : BunitContext {

    public TnTDataGridBodyRowEmpty_Tests() {
        // Arrange (global) & Act: JS module setup for ripple in constructor
        RippleTestingUtility.SetupRippleEffectModule(this);
    }

    [Fact]
    public void Context_IsRequired() {
        // Arrange & Act
        var act = () => Render<TnTDataGridBodyRowEmpty<TestGridItem>>();

        // Assert
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("Context");
    }

    [Fact]
    public void Context_IsSetCorrectly() {
        // Arrange
        var context = CreateGridContext();

        // Act
        var cut = RenderEmptyRowWithContext(context);

        // Assert
        var component = cut.Instance;
        component.Context.Should().BeSameAs(context);
    }

    [Fact]
    public void HasCascadingTypeParameterAttribute() {
        // Arrange & Act
        var componentType = typeof(TnTDataGridBodyRowEmpty<TestGridItem>);

        // Assert
        var attribute = componentType.GetCustomAttributes(typeof(CascadingTypeParameterAttribute), false);
        attribute.Should().HaveCount(1);
        var cascadingAttr = (CascadingTypeParameterAttribute)attribute[0];
        cascadingAttr.Name.Should().Be("TGridItem");
    }

    [Fact]
    public void InheritsFrom_TnTDataGridRow() {
        // Arrange & Act
        var cut = RenderEmptyRowWithContext();

        // Assert
        var component = cut.Instance;
        component.Should().BeAssignableTo<TnTDataGridRow<TestGridItem>>();
    }

    [Fact]
    public void Renders_CorrectColspan_WithRowClickCallback() {
        // Arrange
        var context = CreateGridContext();
        context.Grid.OnRowClicked = EventCallback.Factory.Create<TestGridItem>(context.Grid, _ => { });
        // Add multiple columns
        var column1 = new TestTemplateColumn<TestGridItem>();
        var column2 = new TestTemplateColumn<TestGridItem>();
        var column3 = new TestTemplateColumn<TestGridItem>();
        context.RegisterColumn(column1);
        context.RegisterColumn(column2);
        context.RegisterColumn(column3);

        // Act
        var cut = RenderEmptyRowWithContext(context);

        // Assert
        cut.FindAll("td").Should().HaveCount(1); // Only the content cell
        var contentCell = cut.Find("td.tnt-empty-content-row");
        contentCell.GetAttribute("colspan").Should().Be("3"); // Should match column count
    }

    [Fact]
    public void Renders_EmptyContentCell_WithCorrectColspan() {
        // Arrange
        var context = CreateGridContext();
        // Add some columns
        var column1 = new TestTemplateColumn<TestGridItem>();
        var column2 = new TestTemplateColumn<TestGridItem>();
        var column3 = new TestTemplateColumn<TestGridItem>();
        context.RegisterColumn(column1);
        context.RegisterColumn(column2);
        context.RegisterColumn(column3);

        // Act
        var cut = RenderEmptyRowWithContext(context);

        // Assert
        var contentCell = cut.Find("td.tnt-empty-content-row");
        contentCell.GetAttribute("colspan").Should().Be("3");
    }

    [Fact]
    public void Renders_EmptyContentCell_WithNoContentMessage() {
        // Arrange & Act
        var cut = RenderEmptyRowWithContext();

        // Assert
        var contentCell = cut.Find("td.tnt-empty-content-row");
        contentCell.InnerHtml.Trim().Should().Be("No content to show.");
    }

    [Fact]
    public void Renders_TrElement_WithEmptyContent() {
        // Arrange & Act
        var cut = RenderEmptyRowWithContext();

        // Assert
        cut.FindAll("tr").Should().HaveCount(1);
        var tr = cut.Find("tr");
        tr.Should().NotBeNull();
        cut.Markup.Should().Contain("No content to show");
    }

    [Fact]
    public void Renders_WithAriaRowIndex() {
        // Arrange & Act
        var cut = RenderEmptyRowWithContext();

        // Assert
        var tr = cut.Find("tr");
        tr.GetAttribute("aria-rowindex").Should().Be("2");
    }

    [Fact]
    public void Renders_WithCorrectHeight() {
        // Arrange
        var context = CreateGridContext();
        context.Grid.ItemSize = 60;

        // Act
        var cut = RenderEmptyRowWithContext(context);

        // Assert
        var tr = cut.Find("tr");
        tr.GetAttribute("style").Should().Contain("height: 60px");
    }

    [Fact]
    public void Renders_NoExtraCell_WhenRowClickCallbackExists() {
        // Arrange
        var context = CreateGridContext();
        context.Grid.OnRowClicked = EventCallback.Factory.Create<TestGridItem>(context.Grid, _ => { });
        // Add one column
        var column = new TestTemplateColumn<TestGridItem>();
        context.RegisterColumn(column);

        // Act
        var cut = RenderEmptyRowWithContext(context);

        // Assert
        cut.FindAll("td").Should().HaveCount(1); // Only the content cell
        var contentCell = cut.Find("td.tnt-empty-content-row");
        contentCell.GetAttribute("colspan").Should().Be("1");
    }

    [Fact]
    public void Renders_WithoutExtraCell_WhenNoRowClickCallback() {
        // Arrange
        var context = CreateGridContext();
        // Add one column
        var column = new TestTemplateColumn<TestGridItem>();
        context.RegisterColumn(column);

        // Act
        var cut = RenderEmptyRowWithContext(context);

        // Assert
        cut.FindAll("td").Should().HaveCount(1); // Only the content cell
        var contentCell = cut.Find("td.tnt-empty-content-row");
        contentCell.GetAttribute("colspan").Should().Be("1");
    }

    [Fact]
    public void Renders_WithZeroColumns() {
        // Arrange
        var context = CreateGridContext();
        // Don't register any columns

        // Act
        var cut = RenderEmptyRowWithContext(context);

        // Assert
        var contentCell = cut.Find("td.tnt-empty-content-row");
        contentCell.GetAttribute("colspan").Should().Be("0");
        cut.Markup.Should().Contain("No content to show");
    }

    [Fact]
    public void Renders_WithZeroColumns_AndRowClickCallback() {
        // Arrange
        var context = CreateGridContext();
        context.Grid.OnRowClicked = EventCallback.Factory.Create<TestGridItem>(context.Grid, _ => { });
        // Don't register any columns

        // Act
        var cut = RenderEmptyRowWithContext(context);

        // Assert
        cut.FindAll("td").Should().HaveCount(1); // content
        var contentCell = cut.Find("td.tnt-empty-content-row");
        contentCell.GetAttribute("colspan").Should().Be("0");
    }

    private TnTInternalGridContext<TestGridItem> CreateGridContext() {
        var grid = new TnTDataGrid<TestGridItem>();
        grid.ItemKey = item => item.Id;
        grid.ItemSize = 40;
        return new TnTInternalGridContext<TestGridItem>(grid);
    }

    private IRenderedComponent<TnTDataGridBodyRowEmpty<TestGridItem>> RenderEmptyRowWithContext(TnTInternalGridContext<TestGridItem>? context = null) {
        context ??= CreateGridContext();

        return Render<TnTDataGridBodyRowEmpty<TestGridItem>>(parameters => parameters
            .AddCascadingValue(context));
    }

    /// <summary>
    ///     Test model for grid empty row tests.
    /// </summary>
    private class TestGridItem {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    ///     Test implementation of TnTColumnBase for testing purposes.
    /// </summary>
    private class TestTemplateColumn<TItem> : TnTColumnBase<TItem> {
        public override string? ElementClass => null;
        public override string? ElementStyle => null;
        public override TnTGridSort<TItem>? SortBy { get; set; }

        public override RenderFragment RenderCellContent(TItem item) => builder => builder.AddContent(0, "Cell");

        public override void RenderHeaderContent(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder) {
            builder.AddContent(0, "Header");
        }
    }
}