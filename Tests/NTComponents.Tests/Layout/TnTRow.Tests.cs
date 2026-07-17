using Microsoft.AspNetCore.Components;

namespace NTComponents.Tests.Layout;

public class TnTRow_Tests : BunitContext {

    [Fact]
    public void AdditionalAttributes_Class_Merged() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "class", "flex-row" } };

        // Act
        var cut = Render<TnTRow>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var cls = cut.Find("div").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("flex-row");
        cls.Should().Contain("tnt-row");
    }

    [Fact]
    public void AdditionalAttributes_Custom_Attribute_Added() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "data-row", "main" } };

        // Act
        var cut = Render<TnTRow>(p => p.Add(c => c.AdditionalAttributes, attrs));

        // Assert
        cut.Find("div").GetAttribute("data-row")!.Should().Be("main");
    }

    [Fact]
    public void AdditionalAttributes_Multiple_Classes_Merged() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "class", "align-center justify-between" } };

        // Act
        var cut = Render<TnTRow>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var cls = cut.Find("div").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("align-center");
        cls.Should().Contain("justify-between");
        cls.Should().Contain("tnt-row");
    }

    [Fact]
    public void AdditionalAttributes_Multiple_Styles_Merged() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "style", "flex-wrap:wrap;align-items:center" } };

        // Act
        var cut = Render<TnTRow>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var style = cut.Find("div").GetAttribute("style");

        // Assert
        style.Should().Contain("flex-wrap:wrap");
        style.Should().Contain("align-items:center");
    }

    [Fact]
    public void AdditionalAttributes_Style_Merged() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "style", "gap:10px" } };

        // Act
        var cut = Render<TnTRow>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var style = cut.Find("div").GetAttribute("style");

        // Assert
        style.Should().Contain("gap:10px");
    }

    [Fact]
    public void All_Base_Properties_Work_Together() {
        // Arrange
        var attrs = new Dictionary<string, object> {
            { "class", "custom-row" },
            { "style", "height:50px" },
            { "data-row-id", "row-123" }
        };

        // Act
        var cut = Render<TnTRow>(p => p
            .Add(c => c.ElementId, "main-row")
            .Add(c => c.ElementTitle, "Main Row")
            .Add(c => c.ElementLang, "en-GB")
            .Add(c => c.AutoFocus, true)
            .Add(c => c.AdditionalAttributes, attrs)
            .AddChildContent("Row Content"));

        var div = cut.Find("div");

        // Assert
        div.GetAttribute("id")!.Should().Be("main-row");
        div.GetAttribute("title")!.Should().Be("Main Row");
        div.GetAttribute("lang")!.Should().Be("en-GB");
        div.HasAttribute("autofocus").Should().BeTrue();
        div.GetAttribute("class")!.Should().Contain("custom-row");
        div.GetAttribute("class")!.Should().Contain("tnt-row");
        div.GetAttribute("style").Should().Contain("height:50px");
        div.GetAttribute("data-row-id")!.Should().Be("row-123");
        cut.Markup.Should().Contain("Row Content");
    }

    [Fact]
    public void AutoFocus_False_Does_Not_Render_Autofocus_Attribute() {
        // Arrange & Act
        var cut = Render<TnTRow>(p => p.Add(c => c.AutoFocus, false));

        // Assert
        cut.Find("div").HasAttribute("autofocus").Should().BeFalse();
    }

    [Fact]
    public void AutoFocus_Null_Does_Not_Render_Autofocus_Attribute() {
        // Arrange & Act
        var cut = Render<TnTRow>(p => p.Add(c => c.AutoFocus, (bool?)null));

        // Assert
        cut.Find("div").HasAttribute("autofocus").Should().BeFalse();
    }

    [Fact]
    public void AutoFocus_True_Renders_Autofocus_Attribute() {
        // Arrange & Act
        var cut = Render<TnTRow>(p => p.Add(c => c.AutoFocus, true));

        // Assert
        cut.Find("div").HasAttribute("autofocus").Should().BeTrue();
    }

    [Fact]
    public void ChildContent_Renders_Correctly() {
        // Arrange & Act
        var cut = Render<TnTRow>(p => p.AddChildContent("<div>Column 1</div><div>Column 2</div>"));

        // Assert
        cut.Markup.Should().Contain("<div>Column 1</div><div>Column 2</div>");
    }

    [Fact]
    public void Element_Reference_Captured() {
        // Arrange & Act
        var cut = Render<TnTRow>();

        // Assert
        cut.Find("div").Should().NotBeNull();
    }

    [Fact]
    public void ElementId_Renders_Id_Attribute() {
        // Arrange & Act
        var cut = Render<TnTRow>(p => p.Add(c => c.ElementId, "row-1"));

        // Assert
        cut.Find("div").GetAttribute("id")!.Should().Be("row-1");
    }

    [Fact]
    public void ElementLang_Renders_Lang_Attribute() {
        // Arrange & Act
        var cut = Render<TnTRow>(p => p.Add(c => c.ElementLang, "ko"));

        // Assert
        cut.Find("div").GetAttribute("lang")!.Should().Be("ko");
    }

    [Fact]
    public void ElementTitle_Renders_Title_Attribute() {
        // Arrange & Act
        var cut = Render<TnTRow>(p => p.Add(c => c.ElementTitle, "Grid row"));

        // Assert
        cut.Find("div").GetAttribute("title")!.Should().Be("Grid row");
    }

    [Fact]
    public void Multiple_AdditionalAttributes_All_Rendered() {
        // Arrange
        var attrs = new Dictionary<string, object> {
            { "data-testid", "row" },
            { "role", "row" },
            { "aria-label", "Grid row" }
        };

        // Act
        var cut = Render<TnTRow>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var div = cut.Find("div");

        // Assert
        div.GetAttribute("data-testid")!.Should().Be("row");
        div.GetAttribute("role")!.Should().Be("row");
        div.GetAttribute("aria-label")!.Should().Be("Grid row");
    }

    [Fact]
    public void Null_ChildContent_Does_Not_Throw() {
        // Arrange & Act
        var cut = Render<TnTRow>(p => p.Add(c => c.ChildContent, (RenderFragment?)null));

        // Assert
        cut.Find("div").Should().NotBeNull();
    }

    [Fact]
    public void Renders_Default_Row_With_Base_Class() {
        // Arrange & Act
        var cut = Render<TnTRow>(p => p.AddChildContent("Row Content"));
        var div = cut.Find("div.tnt-row");
        var cls = div.GetAttribute("class")!;

        // Assert
        cls.Should().Contain("tnt-row");
        cls.Should().Contain("tnt-components");
        cut.Markup.Should().Contain("Row Content");
    }
}