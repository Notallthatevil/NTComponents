namespace NTComponents.Tests.Layout;

public class TnTHeader_Tests : BunitContext {

    [Fact]
    public void AdditionalAttributes_Class_Merged() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "class", "custom-header" } };

        // Act
        var cut = Render<TnTHeader>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var cls = cut.Find("div").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("custom-header");
        cls.Should().Contain("tnt-header");
    }

    [Fact]
    public void AdditionalAttributes_Style_Merged() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "style", "padding:10px" } };

        // Act
        var cut = Render<TnTHeader>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var style = cut.Find("div").GetAttribute("style")!;

        // Assert
        style.Should().Contain("padding:10px");
        style.Should().Contain("--tnt-header-bg-color");
    }

    [Fact]
    public void ChildContent_Renders_Correctly() {
        // Arrange & Act
        var cut = Render<TnTHeader>(p => p.AddChildContent("<h1>Page Title</h1>"));

        // Assert
        cut.Markup.Should().Contain("<h1>Page Title</h1>");
    }

    [Fact]
    public void Custom_BackgroundColor_Added_To_Class() {
        // Arrange & Act
        var cut = Render<TnTHeader>(p => p.Add(c => c.BackgroundColor, TnTColor.Secondary));
        var cls = cut.Find("div").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("tnt-bg-color-secondary");
    }

    [Fact]
    public void Custom_BackgroundColor_Variable_In_Style() {
        // Arrange & Act
        var cut = Render<TnTHeader>(p => p.Add(c => c.BackgroundColor, TnTColor.Primary));
        var style = cut.Find("div").GetAttribute("style")!;

        // Assert
        style.Should().Contain("--tnt-header-bg-color:var(--tnt-color-primary)");
    }

    [Fact]
    public void Custom_TextColor_Added_To_Class() {
        // Arrange & Act
        var cut = Render<TnTHeader>(p => p.Add(c => c.TextColor, TnTColor.OnSecondary));
        var cls = cut.Find("div").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("tnt-fg-color-on-secondary");
    }

    [Fact]
    public void Custom_TextColor_Variable_In_Style() {
        // Arrange & Act
        var cut = Render<TnTHeader>(p => p.Add(c => c.TextColor, TnTColor.OnPrimary));
        var style = cut.Find("div").GetAttribute("style")!;

        // Assert
        style.Should().Contain("--tnt-header-fg-color:var(--tnt-color-on-primary)");
    }

    [Fact]
    public void Default_BackgroundColor_Added_To_Class() {
        // Arrange & Act
        var cut = Render<TnTHeader>();
        var cls = cut.Find("div").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("tnt-bg-color-surface");
    }

    [Fact]
    public void Default_BackgroundColor_Is_Surface() {
        // Arrange & Act
        var cut = Render<TnTHeader>();
        var style = cut.Find("div").GetAttribute("style")!;

        // Assert
        style.Should().Contain("--tnt-header-bg-color:var(--tnt-color-surface)");
    }

    [Fact]
    public void Default_TextColor_Added_To_Class() {
        // Arrange & Act
        var cut = Render<TnTHeader>();
        var cls = cut.Find("div").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("tnt-fg-color-on-surface");
    }

    [Fact]
    public void Default_TextColor_Is_OnSurface() {
        // Arrange & Act
        var cut = Render<TnTHeader>();
        var style = cut.Find("div").GetAttribute("style")!;

        // Assert
        style.Should().Contain("--tnt-header-fg-color:var(--tnt-color-on-surface)");
    }

    [Fact]
    public void Element_Reference_Captured() {
        // Arrange & Act
        var cut = Render<TnTHeader>();

        // Assert
        cut.Find("div").Should().NotBeNull();
    }

    [Fact]
    public void ElementId_Renders_Id_Attribute() {
        // Arrange & Act
        var cut = Render<TnTHeader>(p => p.Add(c => c.ElementId, "header-id"));

        // Assert
        cut.Find("div").GetAttribute("id")!.Should().Be("header-id");
    }

    [Fact]
    public void ElementLang_Renders_Lang_Attribute() {
        // Arrange & Act
        var cut = Render<TnTHeader>(p => p.Add(c => c.ElementLang, "de"));

        // Assert
        cut.Find("div").GetAttribute("lang")!.Should().Be("de");
    }

    [Fact]
    public void ElementTitle_Renders_Title_Attribute() {
        // Arrange & Act
        var cut = Render<TnTHeader>(p => p.Add(c => c.ElementTitle, "Header title"));

        // Assert
        cut.Find("div").GetAttribute("title")!.Should().Be("Header title");
    }

    [Fact]
    public void Multiple_Properties_Combined() {
        // Arrange & Act
        var cut = Render<TnTHeader>(p => p
            .Add(c => c.BackgroundColor, TnTColor.Tertiary)
            .Add(c => c.TextColor, TnTColor.OnTertiary)
            .Add(c => c.TextAlignment, TextAlign.Center));

        var div = cut.Find("div");
        var cls = div.GetAttribute("class")!;
        var style = div.GetAttribute("style")!;

        // Assert
        cls.Should().Contain("tnt-bg-color-tertiary");
        cls.Should().Contain("tnt-fg-color-on-tertiary");
        cls.Should().Contain("tnt-text-align-center");
        style.Should().Contain("--tnt-header-bg-color:var(--tnt-color-tertiary)");
        style.Should().Contain("--tnt-header-fg-color:var(--tnt-color-on-tertiary)");
    }

    [Fact]
    public void Renders_Default_Header_With_Base_Class() {
        // Arrange & Act
        var cut = Render<TnTHeader>(p => p.AddChildContent("Header Content"));
        var div = cut.Find("div.tnt-header");
        var cls = div.GetAttribute("class")!;

        // Assert
        cls.Should().Contain("tnt-header");
        cls.Should().Contain("tnt-components");
        cut.Markup.Should().Contain("Header Content");
    }

    [Fact]
    public void TextAlignment_Center_Adds_Class() {
        // Arrange & Act
        var cut = Render<TnTHeader>(p => p.Add(c => c.TextAlignment, TextAlign.Center));
        var cls = cut.Find("div").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("tnt-text-align-center");
    }

    [Fact]
    public void TextAlignment_Justify_Adds_Class() {
        // Arrange & Act
        var cut = Render<TnTHeader>(p => p.Add(c => c.TextAlignment, TextAlign.Justify));
        var cls = cut.Find("div").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("tnt-text-align-justify");
    }

    [Fact]
    public void TextAlignment_Left_Adds_Class() {
        // Arrange & Act
        var cut = Render<TnTHeader>(p => p.Add(c => c.TextAlignment, TextAlign.Left));
        var cls = cut.Find("div").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("tnt-text-align-left");
    }

    [Fact]
    public void TextAlignment_Right_Adds_Class() {
        // Arrange & Act
        var cut = Render<TnTHeader>(p => p.Add(c => c.TextAlignment, TextAlign.Right));
        var cls = cut.Find("div").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("tnt-text-align-right");
    }
}