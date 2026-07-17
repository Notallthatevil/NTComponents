using Microsoft.AspNetCore.Components;

namespace NTComponents.Tests.Layout;

public class TnTContainer_Tests : BunitContext {

    [Fact]
    public void AdditionalAttributes_Class_Merged() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "class", "content-container" } };

        // Act
        var cut = Render<TnTContainer>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var cls = cut.Find("div").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("content-container");
        cls.Should().Contain("tnt-container");
    }

    [Fact]
    public void AdditionalAttributes_Custom_Attribute_Added() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "data-container", "main" } };

        // Act
        var cut = Render<TnTContainer>(p => p.Add(c => c.AdditionalAttributes, attrs));

        // Assert
        cut.Find("div").GetAttribute("data-container")!.Should().Be("main");
    }

    [Fact]
    public void AdditionalAttributes_Multiple_Classes_Merged() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "class", "wrapper fluid" } };

        // Act
        var cut = Render<TnTContainer>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var cls = cut.Find("div").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("wrapper");
        cls.Should().Contain("fluid");
        cls.Should().Contain("tnt-container");
    }

    [Fact]
    public void AdditionalAttributes_Multiple_Styles_Merged() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "style", "padding:20px;margin:auto" } };

        // Act
        var cut = Render<TnTContainer>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var style = cut.Find("div").GetAttribute("style");

        // Assert
        style.Should().Contain("padding:20px");
        style.Should().Contain("margin:auto");
    }

    [Fact]
    public void AdditionalAttributes_Style_Merged() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "style", "max-width:1200px" } };

        // Act
        var cut = Render<TnTContainer>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var style = cut.Find("div").GetAttribute("style");

        // Assert
        style.Should().Contain("max-width:1200px");
    }

    [Fact]
    public void All_Base_Properties_Work_Together() {
        // Arrange
        var attrs = new Dictionary<string, object> {
            { "class", "custom-container" },
            { "style", "border:1px solid black" },
            { "data-id", "container-1" }
        };

        // Act
        var cut = Render<TnTContainer>(p => p
            .Add(c => c.ElementId, "container-id")
            .Add(c => c.ElementTitle, "Container Title")
            .Add(c => c.ElementLang, "en")
            .Add(c => c.AutoFocus, true)
            .Add(c => c.AdditionalAttributes, attrs)
            .AddChildContent("Test Content"));

        var div = cut.Find("div");

        // Assert
        div.GetAttribute("id")!.Should().Be("container-id");
        div.GetAttribute("title")!.Should().Be("Container Title");
        div.GetAttribute("lang")!.Should().Be("en");
        div.HasAttribute("autofocus").Should().BeTrue();
        div.GetAttribute("class")!.Should().Contain("custom-container");
        div.GetAttribute("class")!.Should().Contain("tnt-container");
        div.GetAttribute("style").Should().Contain("border:1px solid black");
        div.GetAttribute("data-id")!.Should().Be("container-1");
        cut.Markup.Should().Contain("Test Content");
    }

    [Fact]
    public void AutoFocus_False_Does_Not_Render_Autofocus_Attribute() {
        // Arrange & Act
        var cut = Render<TnTContainer>(p => p.Add(c => c.AutoFocus, false));

        // Assert
        cut.Find("div").HasAttribute("autofocus").Should().BeFalse();
    }

    [Fact]
    public void AutoFocus_True_Renders_Autofocus_Attribute() {
        // Arrange & Act
        var cut = Render<TnTContainer>(p => p.Add(c => c.AutoFocus, true));

        // Assert
        cut.Find("div").HasAttribute("autofocus").Should().BeTrue();
    }

    [Fact]
    public void ChildContent_Renders_Correctly() {
        // Arrange & Act
        var cut = Render<TnTContainer>(p => p.AddChildContent("<article>Article Content</article>"));

        // Assert
        cut.Markup.Should().Contain("<article>Article Content</article>");
    }

    [Fact]
    public void Element_Reference_Captured() {
        // Arrange & Act
        var cut = Render<TnTContainer>();

        // Assert
        cut.Find("div").Should().NotBeNull();
    }

    [Fact]
    public void ElementId_Renders_Id_Attribute() {
        // Arrange & Act
        var cut = Render<TnTContainer>(p => p.Add(c => c.ElementId, "main-container"));

        // Assert
        cut.Find("div").GetAttribute("id")!.Should().Be("main-container");
    }

    [Fact]
    public void ElementLang_Renders_Lang_Attribute() {
        // Arrange & Act
        var cut = Render<TnTContainer>(p => p.Add(c => c.ElementLang, "ja"));

        // Assert
        cut.Find("div").GetAttribute("lang")!.Should().Be("ja");
    }

    [Fact]
    public void ElementTitle_Renders_Title_Attribute() {
        // Arrange & Act
        var cut = Render<TnTContainer>(p => p.Add(c => c.ElementTitle, "Content container"));

        // Assert
        cut.Find("div").GetAttribute("title")!.Should().Be("Content container");
    }

    [Fact]
    public void Multiple_AdditionalAttributes_All_Rendered() {
        // Arrange
        var attrs = new Dictionary<string, object> {
            { "data-testid", "container" },
            { "role", "region" },
            { "aria-label", "Content area" }
        };

        // Act
        var cut = Render<TnTContainer>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var div = cut.Find("div");

        // Assert
        div.GetAttribute("data-testid")!.Should().Be("container");
        div.GetAttribute("role")!.Should().Be("region");
        div.GetAttribute("aria-label")!.Should().Be("Content area");
    }

    [Fact]
    public void Null_ChildContent_Does_Not_Throw() {
        // Arrange & Act
        var cut = Render<TnTContainer>(p => p.Add(c => c.ChildContent, (RenderFragment?)null));

        // Assert
        cut.Find("div").Should().NotBeNull();
    }

    [Fact]
    public void Renders_Default_Container_With_Base_Class() {
        // Arrange & Act
        var cut = Render<TnTContainer>(p => p.AddChildContent("Container Content"));
        var div = cut.Find("div.tnt-container");
        var cls = div.GetAttribute("class")!;

        // Assert
        cls.Should().Contain("tnt-container");
        cls.Should().Contain("tnt-components");
        cut.Markup.Should().Contain("Container Content");
    }
}