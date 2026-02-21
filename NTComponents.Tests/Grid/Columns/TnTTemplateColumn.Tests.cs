using Microsoft.AspNetCore.Components;
using NTComponents.Grid;
using NTComponents.Grid.Columns;
using NTComponents.Grid.Infrastructure;

namespace NTComponents.Tests.Grid.Columns;

/// <summary>
///     Unit tests for <see cref="TnTTemplateColumn{TGridItem}" />.
/// </summary>
public class TnTTemplateColumn_Tests : BunitContext {

    private readonly TestGridItem _testItem = new() {
        Name = "John Doe",
        Age = 25,
        IsActive = true,
        Salary = 50000.99m
    };

    [Fact]
    public void ChildContent_IsRequired() {
        // Arrange
        var gridContext = CreateMockGridContext();
        var column = CreateColumn(gridContext);

        // Act & Assert The ChildContent property should have EditorRequired attribute
        var property = typeof(TnTTemplateColumn<TestGridItem>).GetProperty(nameof(TnTTemplateColumn<TestGridItem>.ChildContent));
        var editorRequiredAttribute = property?.GetCustomAttributes(typeof(EditorRequiredAttribute), false);
        editorRequiredAttribute.Should().NotBeNull().And.HaveCount(1);
    }


    [Fact]
    public void ElementStyle_IsNull() {
        // Arrange
        var gridContext = CreateMockGridContext();
        var column = CreateColumn(gridContext);

        // Act & Assert
        column.ElementStyle.Should().BeNull();
    }

    [Fact]
    public void ImplementsRenderCellContent() {
        // Arrange
        var gridContext = CreateMockGridContext();
        var column = CreateColumn(gridContext);
        column.ChildContent = item => builder => builder.AddContent(0, "Test");

        // Act
        var fragment = column.RenderCellContent(_testItem);

        // Assert
        fragment.Should().NotBeNull();
        var cut = Render(fragment);
        cut.Markup.Should().Contain("Test");
    }

    [Fact]
    public void InheritsFromTnTColumnBase() {
        // Arrange & Act
        var gridContext = CreateMockGridContext();
        var column = CreateColumn(gridContext);

        // Assert
        column.Should().BeAssignableTo<TnTColumnBase<TestGridItem>>();
    }

    [Fact]
    public void RenderCellContent_WithButtonComponent_RendersButton() {
        // Arrange
        var gridContext = CreateMockGridContext();
        var column = CreateColumn(gridContext);
        column.ChildContent = item => builder => {
            builder.OpenElement(0, "button");
            builder.AddAttribute(1, "type", "button");
            builder.AddAttribute(2, "class", "btn btn-primary");
            builder.AddContent(3, "Edit");
            builder.CloseElement();
        };

        // Act
        var fragment = column.RenderCellContent(_testItem);
        var cut = Render(fragment);

        // Assert
        var button = cut.Find("button[type='button']");
        button.Should().NotBeNull();
        var classes = button.GetAttribute("class")!.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        classes.Should().Contain("btn").And.Contain("btn-primary");
        button.TextContent.Should().Be("Edit");
    }

    [Fact]
    public void RenderCellContent_WithComplexTemplate_RendersCorrectly() {
        // Arrange
        var gridContext = CreateMockGridContext();
        var column = CreateColumn(gridContext);
        column.ChildContent = item => builder => {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "custom-cell");
            builder.OpenElement(2, "span");
            builder.AddAttribute(3, "class", "name");
            builder.AddContent(4, item.Name);
            builder.CloseElement();
            builder.OpenElement(5, "span");
            builder.AddAttribute(6, "class", "age");
            builder.AddContent(7, $" ({item.Age})");
            builder.CloseElement();
            builder.CloseElement();
        };

        // Act
        var fragment = column.RenderCellContent(_testItem);
        var cut = Render(fragment);

        // Assert
        cut.Find("div.custom-cell").Should().NotBeNull();
        cut.Find("span.name").TextContent.Should().Be("John Doe");
        cut.Find("span.age").TextContent.Should().Be(" (25)");
    }

    [Fact]
    public void RenderCellContent_WithConditionalRendering_RendersCorrectly() {
        // Arrange
        var gridContext = CreateMockGridContext();
        var column = CreateColumn(gridContext);
        column.ChildContent = item => builder => {
            if (item.IsActive) {
                builder.AddContent(0, "Active User");
            }
            else {
                builder.AddContent(0, "Inactive User");
            }
        };

        // Act
        var fragment = column.RenderCellContent(_testItem);
        var cut = Render(fragment);

        // Assert
        cut.Markup.Should().Contain("Active User");
        cut.Markup.Should().NotContain("Inactive User");
    }

    [Fact]
    public void RenderCellContent_WithDataAttributes_RendersCorrectly() {
        // Arrange
        var gridContext = CreateMockGridContext();
        var column = CreateColumn(gridContext);
        column.ChildContent = item => builder => {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "data-id", item.Age.ToString());
            builder.AddAttribute(2, "data-name", item.Name);
            builder.AddContent(3, item.Name);
            builder.CloseElement();
        };

        // Act
        var fragment = column.RenderCellContent(_testItem);
        var cut = Render(fragment);

        // Assert
        cut.Find("div").GetAttribute("data-id").Should().Be("25");
        cut.Find("div").GetAttribute("data-name").Should().Be("John Doe");
    }

    [Fact]
    public void RenderCellContent_WithEmptyTemplate_RendersEmpty() {
        // Arrange
        var gridContext = CreateMockGridContext();
        var column = CreateColumn(gridContext);
        column.ChildContent = item => builder => {
            // Empty template
        };

        // Act
        var fragment = column.RenderCellContent(_testItem);
        var cut = Render(fragment);

        // Assert
        cut.Markup.Should().BeEmpty();
    }

    [Fact]
    public void RenderCellContent_WithFormattedValue_RendersCorrectly() {
        // Arrange
        var gridContext = CreateMockGridContext();
        var column = CreateColumn(gridContext);
        column.ChildContent = item => builder => {
            builder.AddContent(0, $"${item.Salary:N2}");
        };

        // Act
        var fragment = column.RenderCellContent(_testItem);
        var cut = Render(fragment);

        // Assert
        cut.Markup.Should().Contain("$50,000.99");
    }

    [Fact]
    public void RenderCellContent_WithInactiveUser_RendersInactive() {
        // Arrange
        var gridContext = CreateMockGridContext();
        var column = CreateColumn(gridContext);
        column.ChildContent = item => builder => {
            if (item.IsActive) {
                builder.AddContent(0, "Active User");
            }
            else {
                builder.AddContent(0, "Inactive User");
            }
        };
        var inactiveItem = new TestGridItem { Name = "Jane Doe", Age = 30, IsActive = false };

        // Act
        var fragment = column.RenderCellContent(inactiveItem);
        var cut = Render(fragment);

        // Assert
        cut.Markup.Should().Contain("Inactive User");
        cut.Markup.Should().NotContain("Active User");
    }

    [Fact]
    public void RenderCellContent_WithMultipleElements_RendersAll() {
        // Arrange
        var gridContext = CreateMockGridContext();
        var column = CreateColumn(gridContext);
        column.ChildContent = item => builder => {
            builder.OpenElement(0, "div");
            builder.AddContent(1, item.Name);
            builder.CloseElement();
            builder.OpenElement(2, "small");
            builder.AddContent(3, $"Age: {item.Age}");
            builder.CloseElement();
        };

        // Act
        var fragment = column.RenderCellContent(_testItem);
        var cut = Render(fragment);

        // Assert
        cut.Find("div").TextContent.Should().Be("John Doe");
        cut.Find("small").TextContent.Should().Be("Age: 25");
    }

    [Fact]
    public void RenderCellContent_WithNullItem_HandlesGracefully() {
        // Arrange
        var gridContext = CreateMockGridContext();
        var column = CreateColumn(gridContext);
        column.ChildContent = item => builder => {
            if (item != null) {
                builder.AddContent(0, item.Name);
            }
            else {
                builder.AddContent(0, "No item");
            }
        };

        // Act
        var fragment = column.RenderCellContent(null!);
        var cut = Render(fragment);

        // Assert
        cut.Markup.Should().Contain("No item");
    }

    [Fact]
    public void RenderCellContent_WithSimpleTemplate_RendersCorrectly() {
        // Arrange
        var gridContext = CreateMockGridContext();
        var column = CreateColumn(gridContext);
        column.ChildContent = item => builder => {
            builder.AddContent(0, $"Name: {item.Name}");
        };

        // Act
        var fragment = column.RenderCellContent(_testItem);
        var cut = Render(fragment);

        // Assert
        cut.Markup.Should().Contain("Name: John Doe");
    }

    [Fact]
    public void RenderCellContent_WithSpecialCharacters_RendersCorrectly() {
        // Arrange
        var gridContext = CreateMockGridContext();
        var column = CreateColumn(gridContext);
        column.ChildContent = item => builder => {
            builder.AddContent(0, $"Name: {item.Name} & Age: {item.Age} < > \" '");
        };

        // Act
        var fragment = column.RenderCellContent(_testItem);
        var cut = Render(fragment);

        // Assert
        cut.Markup.Should().Contain("Name: John Doe &amp; Age: 25 &lt; &gt;");
    }

    [Fact]
    public void SortBy_CanBeNull() {
        // Arrange
        var gridContext = CreateMockGridContext();
        var column = CreateColumn(gridContext);

        // Act
        column.SortBy = null;

        // Assert
        column.SortBy.Should().BeNull();
    }

    [Fact]
    public void SortBy_CanBeSet() {
        // Arrange
        var gridContext = CreateMockGridContext();
        var column = CreateColumn(gridContext);
        var sortBy = TnTGridSort<TestGridItem>.ByAscending(x => x.Name);

        // Act
        column.SortBy = sortBy;

        // Assert
        column.SortBy.Should().Be(sortBy);
    }

    private TnTTemplateColumn<TestGridItem> CreateColumn(TnTInternalGridContext<TestGridItem> context) {
        var column = new TnTTemplateColumn<TestGridItem>();

        // Set the cascading parameter using reflection
        var contextProperty = typeof(TnTTemplateColumn<TestGridItem>)
            .GetProperty("Context", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        contextProperty?.SetValue(column, context);

        return column;
    }

    private TnTInternalGridContext<TestGridItem> CreateMockGridContext() {
        var grid = new TnTDataGrid<TestGridItem>();
        return new TnTInternalGridContext<TestGridItem>(grid);
    }

    /// <summary>
    ///     Test model for template column tests.
    /// </summary>
    private class TestGridItem {
        public int Age { get; set; }
        public bool IsActive { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Salary { get; set; }
    }
}