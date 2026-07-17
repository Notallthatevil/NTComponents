namespace NTComponents.Tests.Layout;

public class TnTBody_Tests : BunitContext {

    [Fact]
    public void AdditionalAttributes_Class_Merged() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "class", "custom-class" } };

        // Act
        var cut = Render<TnTBody>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var cls = cut.Find("div").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("custom-class");
        cls.Should().Contain("tnt-body");
    }

    [Fact]
    public void AdditionalAttributes_Style_Merged() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "style", "margin:10px" } };

        // Act
        var cut = Render<TnTBody>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var style = cut.Find("div").GetAttribute("style")!;

        // Assert
        style.Should().Contain("margin:10px");
        style.Should().Contain("--tnt-layout-background-color");
    }

    [Fact]
    public void ChildContent_Renders_Correctly() {
        // Arrange & Act
        var cut = Render<TnTBody>(p => p.AddChildContent("<span>Test Content</span>"));

        // Assert
        cut.Markup.Should().Contain("<span>Test Content</span>");
    }

    [Fact]
    public void Custom_BackgroundColor_Variable_In_Style() {
        // Arrange & Act
        var cut = Render<TnTBody>(p => p.Add(c => c.BackgroundColor, TnTColor.Surface));
        var style = cut.Find("div").GetAttribute("style")!;

        // Assert
        style.Should().Contain("--tnt-layout-background-color:var(--tnt-color-surface)");
    }

    [Fact]
    public void Custom_TextColor_Variable_In_Style() {
        // Arrange & Act
        var cut = Render<TnTBody>(p => p.Add(c => c.TextColor, TnTColor.Primary));
        var style = cut.Find("div").GetAttribute("style")!;

        // Assert
        style.Should().Contain("--tnt-layout-text-color:var(--tnt-color-primary)");
    }

    [Fact]
    public void Default_Background_And_TextColor_Variables_In_Style() {
        // Arrange & Act
        var cut = Render<TnTBody>();
        var style = cut.Find("div").GetAttribute("style")!;

        // Assert
        style.Should().Contain("--tnt-layout-background-color:var(--tnt-color-background)");
        style.Should().Contain("--tnt-layout-text-color:var(--tnt-color-on-background)");
    }

    [Fact]
    public void Element_Reference_Captured() {
        // Arrange & Act
        var cut = Render<TnTBody>();

        // Assert
        cut.Find("div").Should().NotBeNull();
    }

    [Fact]
    public void ElementId_Renders_Id_Attribute() {
        // Arrange & Act
        var cut = Render<TnTBody>(p => p.Add(c => c.ElementId, "body-id"));

        // Assert
        cut.Find("div").GetAttribute("id")!.Should().Be("body-id");
    }

    [Fact]
    public void ElementLang_Renders_Lang_Attribute() {
        // Arrange & Act
        var cut = Render<TnTBody>(p => p.Add(c => c.ElementLang, "en"));

        // Assert
        cut.Find("div").GetAttribute("lang")!.Should().Be("en");
    }

    [Fact]
    public void ElementTitle_Renders_Title_Attribute() {
        // Arrange & Act
        var cut = Render<TnTBody>(p => p.Add(c => c.ElementTitle, "Body title"));

        // Assert
        cut.Find("div").GetAttribute("title")!.Should().Be("Body title");
    }

    [Fact]
    public void Renders_Default_Body_With_Base_Class() {
        // Arrange & Act
        var cut = Render<TnTBody>(p => p.AddChildContent("Body Content"));
        var div = cut.Find("div.tnt-body");
        var cls = div.GetAttribute("class")!;

        // Assert
        cls.Should().Contain("tnt-body");
        cls.Should().Contain("tnt-components");
        cut.Markup.Should().Contain("Body Content");
    }

    [Fact]
    public void TextAlignment_Center_Adds_Class() {
        // Arrange & Act
        var cut = Render<TnTBody>(p => p.Add(c => c.TextAlignment, TextAlign.Center));
        var cls = cut.Find("div").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("tnt-text-align-center");
    }

    [Fact]
    public void TextAlignment_Justify_Adds_Class() {
        // Arrange & Act
        var cut = Render<TnTBody>(p => p.Add(c => c.TextAlignment, TextAlign.Justify));
        var cls = cut.Find("div").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("tnt-text-align-justify");
    }

    [Fact]
    public void TextAlignment_Left_Adds_Class() {
        // Arrange & Act
        var cut = Render<TnTBody>(p => p.Add(c => c.TextAlignment, TextAlign.Left));
        var cls = cut.Find("div").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("tnt-text-align-left");
    }

    [Fact]
    public void TextAlignment_Right_Adds_Class() {
        // Arrange & Act
        var cut = Render<TnTBody>(p => p.Add(c => c.TextAlignment, TextAlign.Right));
        var cls = cut.Find("div").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("tnt-text-align-right");
    }
}