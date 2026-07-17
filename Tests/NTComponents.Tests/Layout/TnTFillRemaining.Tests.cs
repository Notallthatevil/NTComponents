using Microsoft.AspNetCore.Components;

namespace NTComponents.Tests.Layout;

public class TnTFillRemaining_Tests : BunitContext {

    [Fact]
    public void AdditionalAttributes_Class_Merged() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "class", "extra-class" } };

        // Act
        var cut = Render<TnTFillRemaining>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var cls = cut.Find("div").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("extra-class");
        cls.Should().Contain("tnt-fill-remaining");
    }

    [Fact]
    public void AdditionalAttributes_Custom_Attribute_Added() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "data-test", "fill-test" } };

        // Act
        var cut = Render<TnTFillRemaining>(p => p.Add(c => c.AdditionalAttributes, attrs));

        // Assert
        cut.Find("div").GetAttribute("data-test")!.Should().Be("fill-test");
    }

    [Fact]
    public void AdditionalAttributes_Multiple_Classes_Merged() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "class", "class1 class2" } };

        // Act
        var cut = Render<TnTFillRemaining>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var cls = cut.Find("div").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("class1");
        cls.Should().Contain("class2");
        cls.Should().Contain("tnt-fill-remaining");
    }

    [Fact]
    public void AdditionalAttributes_Multiple_Styles_Merged() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "style", "margin:2px;border:1px solid red" } };

        // Act
        var cut = Render<TnTFillRemaining>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var style = cut.Find("div").GetAttribute("style");

        // Assert
        style.Should().Contain("margin:2px");
        style.Should().Contain("border:1px solid red");
    }

    [Fact]
    public void AdditionalAttributes_Style_Merged() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "style", "padding:5px" } };

        // Act
        var cut = Render<TnTFillRemaining>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var style = cut.Find("div").GetAttribute("style");

        // Assert
        style.Should().Contain("padding:5px");
    }

    [Fact]
    public void ChildContent_Renders_Correctly() {
        // Arrange & Act
        var cut = Render<TnTFillRemaining>(p => p.AddChildContent("<p>Test Content</p>"));

        // Assert
        cut.Markup.Should().Contain("<p>Test Content</p>");
    }

    [Fact]
    public void Element_Reference_Captured() {
        // Arrange & Act
        var cut = Render<TnTFillRemaining>();

        // Assert
        cut.Find("div").Should().NotBeNull();
    }

    [Fact]
    public void ElementId_Renders_Id_Attribute() {
        // Arrange & Act
        var cut = Render<TnTFillRemaining>(p => p.Add(c => c.ElementId, "fill-id"));

        // Assert
        cut.Find("div").GetAttribute("id")!.Should().Be("fill-id");
    }

    [Fact]
    public void ElementLang_Renders_Lang_Attribute() {
        // Arrange & Act
        var cut = Render<TnTFillRemaining>(p => p.Add(c => c.ElementLang, "es"));

        // Assert
        cut.Find("div").GetAttribute("lang")!.Should().Be("es");
    }

    [Fact]
    public void ElementTitle_Renders_Title_Attribute() {
        // Arrange & Act
        var cut = Render<TnTFillRemaining>(p => p.Add(c => c.ElementTitle, "Fill remaining title"));

        // Assert
        cut.Find("div").GetAttribute("title")!.Should().Be("Fill remaining title");
    }

    [Fact]
    public void Multiple_AdditionalAttributes_All_Rendered() {
        // Arrange
        var attrs = new Dictionary<string, object> {
            { "data-test", "value" },
            { "role", "main" },
            { "aria-label", "Fill remaining content" }
        };

        // Act
        var cut = Render<TnTFillRemaining>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var div = cut.Find("div");

        // Assert
        div.GetAttribute("data-test")!.Should().Be("value");
        div.GetAttribute("role")!.Should().Be("main");
        div.GetAttribute("aria-label")!.Should().Be("Fill remaining content");
    }

    [Fact]
    public void Null_ChildContent_Does_Not_Throw() {
        // Arrange & Act
        var cut = Render<TnTFillRemaining>(p => p.Add(c => c.ChildContent, (RenderFragment?)null));

        // Assert
        cut.Find("div").Should().NotBeNull();
    }

    [Fact]
    public void Renders_Default_FillRemaining_With_Base_Class() {
        // Arrange & Act
        var cut = Render<TnTFillRemaining>(p => p.AddChildContent("Fill Content"));
        var div = cut.Find("div.tnt-fill-remaining");
        var cls = div.GetAttribute("class")!;

        // Assert
        cls.Should().Contain("tnt-fill-remaining");
        cls.Should().Contain("tnt-components");
        cut.Markup.Should().Contain("Fill Content");
    }
}