using Microsoft.AspNetCore.Components;
using NTComponents.Grid;
using NTComponents.Grid.Columns;
using NTComponents.Grid.Infrastructure;
using RippleTestingUtility = NTComponents.Tests.TestingUtility.TestingUtility;

namespace NTComponents.Tests.Grid.Infrastructure;

/// <summary>
///     Unit tests for <see cref="TnTDataGridHeaderCell{TGridItem}" />.
/// </summary>
public class TnTDataGridHeaderCell_Tests : BunitContext {

    public TnTDataGridHeaderCell_Tests() {
        // Arrange (global) & Act: JS module setup for ripple in constructor
        RippleTestingUtility.SetupRippleEffectModule(this);
    }

    [Fact]
    public void Column_IsRequired() {
        // Arrange & Act
        var act = () => Render<TnTDataGridHeaderCell<TestGridItem>>(parameters => parameters
            .Add(p => p.Column, (TnTColumnBase<TestGridItem>)null!));

        // Assert
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("Column");
    }

    [Fact]
    public void Column_IsSetCorrectly() {
        // Arrange
        var column = CreateTestColumn("Header");

        // Act
        var cut = RenderHeaderCellWithColumn(column);

        // Assert
        var component = cut.Instance;
        component.Column.Should().BeSameAs(column);
    }

    [Fact]
    public void ElementClass_BuildsCorrectly() {
        // Arrange
        var column = CreateTestColumn("Header");
        column.AdditionalAttributes = new Dictionary<string, object> {
            { "class", "extra-class" }
        };

        // Act
        var cut = RenderHeaderCellWithColumn(column);

        // Assert We can't directly access ElementClass due to it being protected, but we can verify the rendered class attribute
        var th = cut.Find("th");
        var classAttribute = th.GetAttribute("class");
        classAttribute.Should().Contain("tnt-column-header-cell");
        classAttribute.Should().Contain("extra-class");
    }

    [Fact]
    public void ElementStyle_BuildsCorrectly() {
        // Arrange
        var column = CreateTestColumn("Header");
        column.Width = 100;
        column.AdditionalAttributes = new Dictionary<string, object> {
            { "style", "color: blue;" }
        };

        // Act
        var cut = RenderHeaderCellWithColumn(column);

        // Assert
        var th = cut.Find("th");
        var style = th.GetAttribute("style");
        style.Should().Contain("width:100px");
        style.Should().Contain("color: blue");
    }

    [Fact]
    public void HasCascadingTypeParameter() {
        // Arrange & Act
        var componentType = typeof(TnTDataGridHeaderCell<TestGridItem>);

        // Assert
        var attribute = componentType.GetCustomAttributes(typeof(CascadingTypeParameterAttribute), false);
        attribute.Should().HaveCount(1);
        var cascadingAttr = (CascadingTypeParameterAttribute)attribute[0];
        cascadingAttr.Name.Should().Be("TGridItem");
    }

    [Fact]
    public void InheritsFrom_TnTDataGridCell() {
        // Arrange
        var column = CreateTestColumn("Header");

        // Act
        var cut = RenderHeaderCellWithColumn(column);

        // Assert
        var component = cut.Instance;
        component.Should().BeAssignableTo<TnTDataGridCell<TestGridItem>>();
    }

    [Fact]
    public void Renders_ColumnHeaderContent() {
        // Arrange
        var column = new TestTemplateColumn<TestGridItem> {
            HeaderTemplate = builder => {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "header-content");
                builder.AddContent(2, "Custom Header");
                builder.CloseElement();
            }
        };

        // Act
        var cut = RenderHeaderCellWithColumn(column);

        // Assert
        cut.Markup.Should().Contain("header-content");
        cut.Markup.Should().Contain("Custom Header");
    }

    [Fact]
    public void Renders_ThElement_WithColumnHeaderContent() {
        // Arrange
        var column = CreateTestColumn("Test Header");

        // Act
        var cut = RenderHeaderCellWithColumn(column);

        // Assert
        cut.FindAll("th").Should().HaveCount(1);
        cut.Markup.Should().Contain("Test Header");
    }

    [Fact]
    public void Renders_WithAdditionalAttributeClasses() {
        // Arrange
        var column = CreateTestColumn("Header");
        column.AdditionalAttributes = new Dictionary<string, object> {
            { "class", "custom-header-class" }
        };

        // Act
        var cut = RenderHeaderCellWithColumn(column);

        // Assert
        var th = cut.Find("th");
        var classAttribute = th.GetAttribute("class");
        classAttribute.Should().Contain("tnt-column-header-cell");
        classAttribute.Should().Contain("custom-header-class");
    }

    [Fact]
    public void Renders_WithAdditionalAttributeStyles() {
        // Arrange
        var column = CreateTestColumn("Header");
        column.AdditionalAttributes = new Dictionary<string, object> {
            { "style", "background-color: red; color: white;" }
        };

        // Act
        var cut = RenderHeaderCellWithColumn(column);

        // Assert
        var th = cut.Find("th");
        var style = th.GetAttribute("style");
        style.Should().Contain("background-color: red");
        style.Should().Contain("color: white");
    }

    [Fact]
    public void Renders_WithBaseCssClass() {
        // Arrange
        var column = CreateTestColumn("Header");

        // Act
        var cut = RenderHeaderCellWithColumn(column);

        // Assert
        var th = cut.Find("th");
        th.GetAttribute("class").Should().Contain("tnt-column-header-cell");
    }

    [Fact]
    public void Renders_WithColumnAdditionalAttributes() {
        // Arrange
        var column = CreateTestColumn("Header");
        column.AdditionalAttributes = new Dictionary<string, object> {
            { "data-test", "header-value" },
            { "title", "Column Title" }
        };

        // Act
        var cut = RenderHeaderCellWithColumn(column);

        // Assert
        var th = cut.Find("th");
        th.GetAttribute("data-test").Should().Be("header-value");
        th.GetAttribute("title").Should().Be("Column Title");
    }

    [Fact]
    public void Renders_WithColumnWidth_InStyle() {
        // Arrange
        var column = CreateTestColumn("Header");
        column.Width = 150;

        // Act
        var cut = Render<TnTDataGridHeaderCell<TestGridItem>>(parameters => parameters
            .Add(p => p.Column, column));

        // Assert
        var th = cut.Find("th");
        var style = th.GetAttribute("style");
        style.Should().Contain("width:150px");
        style.Should().Contain("min-width:150px");
    }

    [Fact]
    public void Renders_WithCombinedStyles_WidthAndAdditionalAttributes() {
        // Arrange
        var column = CreateTestColumn("Header");
        column.Width = 200;
        column.AdditionalAttributes = new Dictionary<string, object> {
            { "style", "text-align: center;" }
        };

        // Act
        var cut = RenderHeaderCellWithColumn(column);

        // Assert
        var th = cut.Find("th");
        var style = th.GetAttribute("style");
        style.Should().Contain("width:200px");
        style.Should().Contain("min-width:200px");
        style.Should().Contain("text-align: center");
    }

    [Fact]
    public void Renders_WithComplexHeaderContent() {
        // Arrange
        var column = new TestTemplateColumn<TestGridItem> {
            HeaderTemplate = builder => {
                builder.OpenElement(0, "div");
                builder.OpenElement(1, "i");
                builder.AddAttribute(2, "class", "icon");
                builder.CloseElement();
                builder.OpenElement(3, "strong");
                builder.AddContent(4, "Header Title");
                builder.CloseElement();
                builder.OpenElement(5, "button");
                builder.AddAttribute(6, "type", "button");
                builder.AddContent(7, "Sort");
                builder.CloseElement();
                builder.CloseElement();
            }
        };

        // Act
        var cut = RenderHeaderCellWithColumn(column);

        // Assert
        cut.Markup.Should().Contain("icon");
        cut.Markup.Should().Contain("Header Title");
        cut.Markup.Should().Contain("<button");
    }

    [Fact]
    public void Renders_WithEmptyAdditionalAttributes() {
        // Arrange
        var column = CreateTestColumn("Header");
        column.AdditionalAttributes = new Dictionary<string, object>();

        // Act
        var cut = RenderHeaderCellWithColumn(column);

        // Assert
        cut.FindAll("th").Should().HaveCount(1);
        cut.Markup.Should().Contain("Header");
    }

    [Fact]
    public void Renders_WithMultipleAdditionalAttributeClasses() {
        // Arrange
        var column = CreateTestColumn("Header");
        column.AdditionalAttributes = new Dictionary<string, object> {
            { "class", "class1 class2 class3" }
        };

        // Act
        var cut = RenderHeaderCellWithColumn(column);

        // Assert
        var th = cut.Find("th");
        var classAttribute = th.GetAttribute("class");
        classAttribute.Should().Contain("tnt-column-header-cell");
        classAttribute.Should().Contain("class1");
        classAttribute.Should().Contain("class2");
        classAttribute.Should().Contain("class3");
    }

    [Fact]
    public void Renders_WithNullAdditionalAttributes() {
        // Arrange
        var column = CreateTestColumn("Header");
        column.AdditionalAttributes = null;

        // Act
        var cut = RenderHeaderCellWithColumn(column);

        // Assert
        cut.FindAll("th").Should().HaveCount(1);
        cut.Markup.Should().Contain("Header");
    }

    [Fact]
    public void Renders_WithoutWidthStyle_WhenNoWidthSpecified() {
        // Arrange
        var column = CreateTestColumn("Header");
        column.Width = null;

        // Act
        var cut = RenderHeaderCellWithColumn(column);

        // Assert
     var th = cut.Find("th");
        var style = th.GetAttribute("style");
        if (!string.IsNullOrEmpty(style)) {
            style.Should().NotContain("width:");
          style.Should().NotContain("min-width:");
        }
    }

    [Fact]
    public void Renders_WithMaxWidth_InStyle() {
        // Arrange
        var column = CreateTestColumn("Header");
        column.MaxWidth = 100;

        // Act
    var cut = RenderHeaderCellWithColumn(column);

        // Assert
        var th = cut.Find("th");
        var style = th.GetAttribute("style");
        style.Should().Contain("max-width:100px");
        style.Should().Contain("word-wrap:break-word");
    }

    [Fact]
    public void Renders_WithoutMaxWidthStyle_WhenNoMaxWidthSpecified() {
        // Arrange
        var column = CreateTestColumn("Header");
        column.MaxWidth = null;

        // Act
    var cut = RenderHeaderCellWithColumn(column);

// Assert
        var th = cut.Find("th");
  var style = th.GetAttribute("style");
 if (!string.IsNullOrEmpty(style)) {
 style.Should().NotContain("max-width:");
   style.Should().NotContain("word-wrap:break-word");
        }
    }

    [Fact]
    public void Renders_WithBothWidthAndMaxWidth_InStyle() {
     // Arrange
      var column = CreateTestColumn("Header");
      column.Width = 200;
        column.MaxWidth = 150;

        // Act
        var cut = RenderHeaderCellWithColumn(column);

        // Assert
        var th = cut.Find("th");
        var style = th.GetAttribute("style");
        style.Should().Contain("width:200px");
  style.Should().Contain("min-width:200px");
 style.Should().Contain("max-width:150px");
        style.Should().Contain("word-wrap:break-word");
    }

    [Fact]
    public void Renders_WithMaxWidth_AndAdditionalAttributes() {
  // Arrange
        var column = CreateTestColumn("Header");
        column.MaxWidth = 120;
        column.AdditionalAttributes = new Dictionary<string, object> {
            { "style", "background-color: blue;" }
        };

        // Act
  var cut = RenderHeaderCellWithColumn(column);

        // Assert
        var th = cut.Find("th");
        var style = th.GetAttribute("style");
     style.Should().Contain("max-width:120px");
        style.Should().Contain("word-wrap:break-word");
    style.Should().Contain("background-color: blue");
    }

    [Fact]
    public void Renders_WithSmallMaxWidth_AppliesCorrectly() {
     // Arrange
        var column = CreateTestColumn("Header");
  column.MaxWidth = 25;

   // Act
     var cut = RenderHeaderCellWithColumn(column);

        // Assert
     var th = cut.Find("th");
var style = th.GetAttribute("style");
     style.Should().Contain("max-width:25px;word-wrap:break-word");
    }

    [Fact]
    public void Renders_WithLargeMaxWidth_AppliesCorrectly() {
        // Arrange
        var column = CreateTestColumn("Header");
column.MaxWidth = 500;

        // Act
    var cut = RenderHeaderCellWithColumn(column);

        // Assert
        var th = cut.Find("th");
   var style = th.GetAttribute("style");
      style.Should().Contain("max-width:500px;word-wrap:break-word");
    }

    private TestTemplateColumn<TestGridItem> CreateTestColumn(string headerContent) {
        return new TestTemplateColumn<TestGridItem> {
            HeaderTemplate = builder => builder.AddContent(0, headerContent)
        };
    }

    private IRenderedComponent<TnTDataGridHeaderCell<TestGridItem>> RenderHeaderCellWithColumn(TnTColumnBase<TestGridItem> column) {
        return Render<TnTDataGridHeaderCell<TestGridItem>>(parameters => parameters
            .Add(p => p.Column, column));
    }

    /// <summary>
    ///     Test model for grid header cell tests.
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