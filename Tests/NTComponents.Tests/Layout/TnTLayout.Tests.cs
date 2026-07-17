using Microsoft.AspNetCore.Components;

namespace NTComponents.Tests.Layout;

public class TnTLayout_Tests : BunitContext {

    [Fact]
    public void AdditionalAttributes_Class_Merged() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "class", "layout-wrapper" } };

        // Act
        var cut = Render<TnTLayout>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var cls = cut.Find("div").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("layout-wrapper");
        cls.Should().Contain("tnt-layout");
    }

    [Fact]
    public void AdditionalAttributes_Custom_Attribute_Added() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "data-testid", "layout-container" } };

        // Act
        var cut = Render<TnTLayout>(p => p.Add(c => c.AdditionalAttributes, attrs));

        // Assert
        cut.Find("div").GetAttribute("data-testid")!.Should().Be("layout-container");
    }

    [Fact]
    public void AdditionalAttributes_Multiple_Classes_Merged() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "class", "primary secondary" } };

        // Act
        var cut = Render<TnTLayout>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var cls = cut.Find("div").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("primary");
        cls.Should().Contain("secondary");
        cls.Should().Contain("tnt-layout");
    }

    [Fact]
    public void AdditionalAttributes_Multiple_Styles_Merged() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "style", "width:100%;min-height:100vh" } };

        // Act
        var cut = Render<TnTLayout>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var style = cut.Find("div").GetAttribute("style");

        // Assert
        style.Should().Contain("width:100%");
        style.Should().Contain("min-height:100vh");
    }

    [Fact]
    public void AdditionalAttributes_Style_Merged() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "style", "height:100vh" } };

        // Act
        var cut = Render<TnTLayout>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var style = cut.Find("div").GetAttribute("style");

        // Assert
        style.Should().Contain("height:100vh");
    }

    [Fact]
    public void All_Base_Properties_Work_Together() {
        // Arrange
        var attrs = new Dictionary<string, object> {
            { "class", "custom-layout" },
            { "style", "background:red" }
        };

        // Act
        var cut = Render<TnTLayout>(p => p
            .Add(c => c.ElementId, "layout-1")
            .Add(c => c.ElementTitle, "Layout Title")
            .Add(c => c.ElementLang, "en-US")
            .Add(c => c.AdditionalAttributes, attrs)
            .AddChildContent("Content"));

        var div = cut.Find("div");

        // Assert
        div.GetAttribute("id")!.Should().Be("layout-1");
        div.GetAttribute("title")!.Should().Be("Layout Title");
        div.GetAttribute("lang")!.Should().Be("en-US");
        div.GetAttribute("class")!.Should().Contain("custom-layout");
        div.GetAttribute("class")!.Should().Contain("tnt-layout");
        div.GetAttribute("style").Should().Contain("background:red");
        cut.Markup.Should().Contain("Content");
    }

    [Fact]
    public void ChildContent_Renders_Correctly() {
        // Arrange & Act
        var cut = Render<TnTLayout>(p => p.AddChildContent("<section>Main Section</section>"));

        // Assert
        cut.Markup.Should().Contain("<section>Main Section</section>");
    }

    [Fact]
    public void Element_Reference_Captured() {
        // Arrange & Act
        var cut = Render<TnTLayout>();

        // Assert
        cut.Find("div").Should().NotBeNull();
    }

    [Fact]
    public void ElementId_Renders_Id_Attribute() {
        // Arrange & Act
        var cut = Render<TnTLayout>(p => p.Add(c => c.ElementId, "main-layout"));

        // Assert
        cut.Find("div").GetAttribute("id")!.Should().Be("main-layout");
    }

    [Fact]
    public void ElementLang_Renders_Lang_Attribute() {
        // Arrange & Act
        var cut = Render<TnTLayout>(p => p.Add(c => c.ElementLang, "it"));

        // Assert
        cut.Find("div").GetAttribute("lang")!.Should().Be("it");
    }

    [Fact]
    public void ElementTitle_Renders_Title_Attribute() {
        // Arrange & Act
        var cut = Render<TnTLayout>(p => p.Add(c => c.ElementTitle, "Main layout container"));

        // Assert
        cut.Find("div").GetAttribute("title")!.Should().Be("Main layout container");
    }

    [Fact]
    public void Multiple_AdditionalAttributes_All_Rendered() {
        // Arrange
        var attrs = new Dictionary<string, object> {
            { "data-component", "layout" },
            { "role", "main" },
            { "aria-label", "Main layout" }
        };

        // Act
        var cut = Render<TnTLayout>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var div = cut.Find("div");

        // Assert
        div.GetAttribute("data-component")!.Should().Be("layout");
        div.GetAttribute("role")!.Should().Be("main");
        div.GetAttribute("aria-label")!.Should().Be("Main layout");
    }

    [Fact]
    public void Null_ChildContent_Does_Not_Throw() {
        // Arrange & Act
        var cut = Render<TnTLayout>(p => p.Add(c => c.ChildContent, (RenderFragment?)null));

        // Assert
        cut.Find("div").Should().NotBeNull();
    }

    [Fact]
    public void Renders_Default_Layout_With_Base_Class() {
        // Arrange & Act
        var cut = Render<TnTLayout>(p => p.AddChildContent("Layout Content"));
        var div = cut.Find("div.tnt-layout");
        var cls = div.GetAttribute("class")!;

        // Assert
        cls.Should().Contain("tnt-layout");
        cls.Should().Contain("tnt-components");
        cut.Markup.Should().Contain("Layout Content");
    }
}