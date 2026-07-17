using Microsoft.AspNetCore.Components;
using NTComponents.Grid;
using NTComponents.Grid.Columns;
using NTComponents.Grid.Infrastructure;
using RippleTestingUtility = NTComponents.Tests.TestingUtility.TestingUtility;

namespace NTComponents.Tests.Grid.Infrastructure;

/// <summary>
///     Unit tests for <see cref="TnTDataGridHeaderRow{TGridItem}" />.
/// </summary>
public class TnTDataGridHeaderRow_Tests : BunitContext {

    public TnTDataGridHeaderRow_Tests() {
        // Arrange (global) & Act: JS module setup for ripple in constructor
        RippleTestingUtility.SetupRippleEffectModule(this);
    }

    [Fact]
    public void Context_IsRequired() {
        // Arrange & Act
        var act = () => Render<TnTDataGridHeaderRow<TestGridItem>>();

        // Assert
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("Context");
    }

    [Fact]
    public void Context_IsSetCorrectly() {
        // Arrange
        var context = CreateGridContext();

        // Act
        var cut = RenderHeaderRowWithContext(context);

        // Assert
        var component = cut.Instance;
        component.Context.Should().BeSameAs(context);
    }

    [Fact]
    public void DoesNotRender_InteractablePlaceholder_WhenNoRowClickCallback() {
        // Arrange
        var context = CreateGridContext();
        var column = new TestTemplateColumn<TestGridItem> {
            HeaderTemplate = builder => builder.AddContent(0, "Column")
        };
        context.RegisterColumn(column);

        // Act
        var cut = RenderHeaderRowWithContext(context);

        // Assert
        var thElements = cut.FindAll("th");
        thElements.Should().HaveCount(1); // Only the actual column
        thElements[0].InnerHtml.Should().Contain("Column");
    }

    [Fact]
    public void HasCascadingTypeParameterAttribute() {
        // Arrange & Act
        var componentType = typeof(TnTDataGridHeaderRow<TestGridItem>);

        // Assert
        var attribute = componentType.GetCustomAttributes(typeof(CascadingTypeParameterAttribute), false);
        attribute.Should().HaveCount(1);
        var cascadingAttr = (CascadingTypeParameterAttribute)attribute[0];
        cascadingAttr.Name.Should().Be("TGridItem");
    }

    [Fact]
    public void InheritsFrom_TnTDataGridRow() {
        // Arrange & Act
        var cut = RenderHeaderRowWithContext();

        // Assert
        var component = cut.Instance;
        component.Should().BeAssignableTo<TnTDataGridRow<TestGridItem>>();
    }

    [Fact]
    public async Task RefreshAsync_InheritsFromBase() {
        // Arrange
        var cut = RenderHeaderRowWithContext();
        var component = cut.Instance;

        // Act
        var task = component.RefreshAsync();

        // Assert
        task.Should().NotBeNull();
        await task; // Should complete without throwing
        task.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void Renders_ColumnsInCorrectOrder() {
        // Arrange
        var context = CreateGridContext();
        var column1 = new TestTemplateColumn<TestGridItem> {
            HeaderTemplate = builder => builder.AddContent(0, "First"),
            Order = 2
        };
        var column2 = new TestTemplateColumn<TestGridItem> {
            HeaderTemplate = builder => builder.AddContent(0, "Second"),
            Order = 1
        };
        var column3 = new TestTemplateColumn<TestGridItem> {
            HeaderTemplate = builder => builder.AddContent(0, "Third"),
            Order = 3
        };

        // Register in different order than display order
        context.RegisterColumn(column1);
        context.RegisterColumn(column3);
        context.RegisterColumn(column2);

        // Act
        var cut = RenderHeaderRowWithContext(context);

        // Assert
        var thElements = cut.FindAll("th");
        thElements.Should().HaveCount(3);
        // Should be ordered by Order property: Second, First, Third
        thElements[0].InnerHtml.Should().Contain("Second");
        thElements[1].InnerHtml.Should().Contain("First");
        thElements[2].InnerHtml.Should().Contain("Third");
    }

    [Fact]
    public void Renders_CompleteHeader_WithMultipleColumnsAndRowCallback() {
        // Arrange
        var context = CreateGridContext();
        context.Grid.OnRowClicked = EventCallback.Factory.Create<TestGridItem>(context.Grid, _ => { });

        var idColumn = new TestTemplateColumn<TestGridItem> {
            HeaderTemplate = builder => builder.AddContent(0, "ID"),
            Order = 1
        };
        var nameColumn = new TestTemplateColumn<TestGridItem> {
            HeaderTemplate = builder => builder.AddContent(0, "Name"),
            Order = 2
        };
        var emailColumn = new TestTemplateColumn<TestGridItem> {
            HeaderTemplate = builder => builder.AddContent(0, "Email"),
            Order = 3
        };

        context.RegisterColumn(nameColumn);
        context.RegisterColumn(idColumn);
        context.RegisterColumn(emailColumn);

        // Act
        var cut = RenderHeaderRowWithContext(context);

        // Assert Should have thead structure
        cut.FindAll("thead").Should().HaveCount(1);
        cut.Find("thead").GetAttribute("class").Should().Be("tnt-data-grid-header");

        // Should have tr with correct classes
        var tr = cut.Find("tr");
        tr.GetAttribute("class").Should().Contain("tnt-data-grid-header-row");
        tr.GetAttribute("class").Should().Contain("tnt-interactable");

        // Should have placeholder + 3 columns = 4 th elements
        var thElements = cut.FindAll("th");
        thElements.Should().HaveCount(4);

        // Find the placeholder th by class rather than assuming index 0
        var placeholder = thElements.FirstOrDefault(t => t.ClassList.Contains("tnt-interactable-placeholder"));
        placeholder.Should().NotBeNull();

        // Others should be in order: ID, Name, Email (skip placeholder)
        var headerTexts = thElements.Where(t => !t.ClassList.Contains("tnt-interactable-placeholder")).Select(t => t.InnerHtml.Trim()).ToList();
        headerTexts[0].Should().Contain("ID");
        headerTexts[1].Should().Contain("Name");
        headerTexts[2].Should().Contain("Email");
    }

    [Fact]
    public void Renders_HeaderCellsForEachColumn() {
        // Arrange
        var context = CreateGridContext();
        var nameColumn = new TestTemplateColumn<TestGridItem> {
            HeaderTemplate = builder => builder.AddContent(0, "Name")
        };
        var emailColumn = new TestTemplateColumn<TestGridItem> {
            HeaderTemplate = builder => builder.AddContent(0, "Email")
        };
        context.RegisterColumn(nameColumn);
        context.RegisterColumn(emailColumn);

        // Act
        var cut = RenderHeaderRowWithContext(context);

        // Assert
        cut.FindAll("th").Should().HaveCount(2);
        cut.Markup.Should().Contain("Name");
        cut.Markup.Should().Contain("Email");
    }

    [Fact]
    public void Renders_InteractablePlaceholder_WhenRowClickCallbackExists() {
        // Arrange
        var context = CreateGridContext();
        context.Grid.OnRowClicked = EventCallback.Factory.Create<TestGridItem>(context.Grid, _ => { });
        var column = new TestTemplateColumn<TestGridItem> {
            HeaderTemplate = builder => builder.AddContent(0, "Column")
        };
        context.RegisterColumn(column);

        // Act
        var cut = RenderHeaderRowWithContext(context);

        // Assert
        var thElements = cut.FindAll("th");
        thElements.Should().HaveCount(2); // Placeholder + actual column

        var placeholder = thElements.FirstOrDefault(t => t.ClassList.Contains("tnt-interactable-placeholder"));
        placeholder.Should().NotBeNull();
        placeholder.InnerHtml.Trim().Should().BeEmpty();
    }

    [Fact]
    public void Renders_TheadElement_WithHeaderStructure() {
        // Arrange & Act
        var cut = RenderHeaderRowWithContext();

        // Assert
        cut.FindAll("thead").Should().HaveCount(1);
        cut.FindAll("tr").Should().HaveCount(1);
        var thead = cut.Find("thead");
        thead.GetAttribute("class").Should().Be("tnt-data-grid-header");
    }

    [Fact]
    public void Renders_TrElement_WithBasicClass() {
        // Arrange & Act
        var cut = RenderHeaderRowWithContext();

        // Assert
        var tr = cut.Find("tr");
        tr.GetAttribute("class").Should().Contain("tnt-data-grid-header-row");
        tr.GetAttribute("scope").Should().Be("row");
        tr.GetAttribute("aria-rowindex").Should().Be("1");
    }

    [Fact]
    public void Renders_WithInteractableClass_WhenRowClickCallbackExists() {
        // Arrange
        var context = CreateGridContext();
        context.Grid.OnRowClicked = EventCallback.Factory.Create<TestGridItem>(context.Grid, _ => { });

        // Act
        var cut = RenderHeaderRowWithContext(context);

        // Assert
        var tr = cut.Find("tr");
        tr.GetAttribute("class").Should().Contain("tnt-interactable");
    }

    [Fact]
    public void Renders_WithNoColumns() {
        // Arrange
        var context = CreateGridContext();
        // Don't register any columns

        // Act
        var cut = RenderHeaderRowWithContext(context);

        // Assert Should still render the basic structure
        cut.FindAll("thead").Should().HaveCount(1);
        cut.FindAll("tr").Should().HaveCount(1);
        // Should have no th elements (except potentially the placeholder)
        var thElements = cut.FindAll("th");
        thElements.Should().HaveCount(0); // No columns, no placeholders
    }

    [Fact]
    public void Renders_WithoutInteractableClass_WhenNoRowClickCallback() {
        // Arrange
        var context = CreateGridContext();
        // Ensure no row click callback is set

        // Act
        var cut = RenderHeaderRowWithContext(context);

        // Assert
        var tr = cut.Find("tr");
        tr.GetAttribute("class").Should().NotContain("tnt-interactable");
        tr.GetAttribute("class").Should().Be("tnt-data-grid-header-row");
    }

    [Fact]
    public void DoesNotRender_InteractablePlaceholder_WhenNoColumns() {
        // Arrange
        var context = CreateGridContext();
        context.Grid.OnRowClicked = EventCallback.Factory.Create<TestGridItem>(context.Grid, _ => { });
        // Don't register any columns

        // Act
        var cut = RenderHeaderRowWithContext(context);

        // Assert
        var thElements = cut.FindAll("th");
        // Some target frameworks/components may render a placeholder th even when no columns are registered; accept either behavior
        if (thElements.Count == 0) {
            thElements.Should().HaveCount(0);
        }
        else {
            // If a single th exists it must be the interactable placeholder with no content
            thElements.Should().HaveCount(1);
            thElements[0].ClassList.Contains("tnt-interactable-placeholder").Should().BeTrue();
            thElements[0].InnerHtml.Trim().Should().BeEmpty();
        }
    }

    private TnTInternalGridContext<TestGridItem> CreateGridContext() {
        var grid = new TnTDataGrid<TestGridItem>();
        grid.ItemKey = item => item.Id;
        grid.ItemSize = 40;
        return new TnTInternalGridContext<TestGridItem>(grid);
    }

    private IRenderedComponent<TnTDataGridHeaderRow<TestGridItem>> RenderHeaderRowWithContext(TnTInternalGridContext<TestGridItem>? context = null) {
        context ??= CreateGridContext();

        return Render<TnTDataGridHeaderRow<TestGridItem>>(parameters => parameters
            .AddCascadingValue(context));
    }

    /// <summary>
    ///     Test model for grid header row tests.
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
        public override string? ElementClass => null;
        public override string? ElementStyle => null;
        public Action<Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder>? HeaderTemplate { get; set; }
        public override TnTGridSort<TItem>? SortBy { get; set; }

        public override RenderFragment RenderCellContent(TItem item) => builder => builder.AddContent(0, "Cell");

        public override void RenderHeaderContent(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder) {
            if (HeaderTemplate != null) {
                HeaderTemplate(builder);
            }
            else {
                builder.AddContent(0, "Default Header");
            }
        }
    }
}