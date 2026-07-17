namespace NTComponents.Tests.Layout;

public class TnTFooter_Tests : BunitContext {

    [Fact]
    public void AdditionalAttributes_Class_Merged() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "class", "custom-footer" } };

        // Act
        var cut = Render<TnTFooter>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var cls = cut.Find("div").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("custom-footer");
        cls.Should().Contain("tnt-footer");
    }

    [Fact]
    public void AdditionalAttributes_Style_Merged() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "style", "margin-top:20px" } };

        // Act
        var cut = Render<TnTFooter>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var style = cut.Find("div").GetAttribute("style")!;

        // Assert
        style.Should().Contain("margin-top:20px");
        style.Should().Contain("--tnt-footer-bg-color");
    }

    [Fact]
    public void ChildContent_Renders_Correctly() {
        // Arrange & Act
        var cut = Render<TnTFooter>(p => p.AddChildContent("<nav>Footer Navigation</nav>"));

        // Assert
        cut.Markup.Should().Contain("<nav>Footer Navigation</nav>");
    }

    [Fact]
    public void Custom_BackgroundColor_Variable_In_Style() {
        // Arrange & Act
        var cut = Render<TnTFooter>(p => p.Add(c => c.BackgroundColor, TnTColor.Primary));
        var style = cut.Find("div").GetAttribute("style")!;

        // Assert
        style.Should().Contain("--tnt-footer-bg-color:var(--tnt-color-primary)");
    }

    [Fact]
    public void Custom_Elevation_Adds_Elevation_Class() {
        // Arrange & Act
        var cut = Render<TnTFooter>(p => p.Add(c => c.Elevation, 5));
        var cls = cut.Find("div").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("tnt-elevation-5");
        cls.Should().NotContain("tnt-elevation-2");
    }

    [Fact]
    public void Custom_TextColor_Variable_In_Style() {
        // Arrange & Act
        var cut = Render<TnTFooter>(p => p.Add(c => c.TextColor, TnTColor.OnPrimary));
        var style = cut.Find("div").GetAttribute("style")!;

        // Assert
        style.Should().Contain("--tnt-footer-fg-color:var(--tnt-color-on-primary)");
    }

    [Fact]
    public void Default_BackgroundColor_Is_SurfaceVariant() {
        // Arrange & Act
        var cut = Render<TnTFooter>();
        var style = cut.Find("div").GetAttribute("style")!;

        // Assert
        style.Should().Contain("--tnt-footer-bg-color:var(--tnt-color-surface-variant)");
    }

    [Fact]
    public void Default_Elevation_Is_2() {
        // Arrange & Act
        var cut = Render<TnTFooter>();
        var cls = cut.Find("div").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("tnt-elevation-2");
    }

    [Fact]
    public void Default_TextColor_Is_OnSurfaceVariant() {
        // Arrange & Act
        var cut = Render<TnTFooter>();
        var style = cut.Find("div").GetAttribute("style")!;

        // Assert
        style.Should().Contain("--tnt-footer-fg-color:var(--tnt-color-on-surface-variant)");
    }

    [Fact]
    public void Element_Reference_Captured() {
        // Arrange & Act
        var cut = Render<TnTFooter>();

        // Assert
        cut.Find("div").Should().NotBeNull();
    }

    [Fact]
    public void ElementId_Renders_Id_Attribute() {
        // Arrange & Act
        var cut = Render<TnTFooter>(p => p.Add(c => c.ElementId, "footer-id"));

        // Assert
        cut.Find("div").GetAttribute("id")!.Should().Be("footer-id");
    }

    [Fact]
    public void ElementLang_Renders_Lang_Attribute() {
        // Arrange & Act
        var cut = Render<TnTFooter>(p => p.Add(c => c.ElementLang, "fr"));

        // Assert
        cut.Find("div").GetAttribute("lang")!.Should().Be("fr");
    }

    [Fact]
    public void ElementTitle_Renders_Title_Attribute() {
        // Arrange & Act
        var cut = Render<TnTFooter>(p => p.Add(c => c.ElementTitle, "Footer title"));

        // Assert
        cut.Find("div").GetAttribute("title")!.Should().Be("Footer title");
    }

    [Fact]
    public void Multiple_Properties_Combined() {
        // Arrange & Act
        var cut = Render<TnTFooter>(p => p
            .Add(c => c.BackgroundColor, TnTColor.Secondary)
            .Add(c => c.TextColor, TnTColor.OnSecondary)
            .Add(c => c.Elevation, 3)
            .Add(c => c.TextAlignment, TextAlign.Center));

        var div = cut.Find("div");
        var cls = div.GetAttribute("class")!;
        var style = div.GetAttribute("style")!;

        // Assert
        cls.Should().Contain("tnt-elevation-3");
        cls.Should().Contain("tnt-text-align-center");
        style.Should().Contain("--tnt-footer-bg-color:var(--tnt-color-secondary)");
        style.Should().Contain("--tnt-footer-fg-color:var(--tnt-color-on-secondary)");
    }

    [Fact]
    public void Renders_Default_Footer_With_Base_Class() {
        // Arrange & Act
        var cut = Render<TnTFooter>(p => p.AddChildContent("Footer Content"));
        var div = cut.Find("div.tnt-footer");
        var cls = div.GetAttribute("class")!;

        // Assert
        cls.Should().Contain("tnt-footer");
        cls.Should().Contain("tnt-components");
        cut.Markup.Should().Contain("Footer Content");
    }

    [Fact]
    public void TextAlignment_Center_Adds_Class() {
        // Arrange & Act
        var cut = Render<TnTFooter>(p => p.Add(c => c.TextAlignment, TextAlign.Center));
        var cls = cut.Find("div").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("tnt-text-align-center");
    }

    [Fact]
    public void TextAlignment_Justify_Adds_Class() {
        // Arrange & Act
        var cut = Render<TnTFooter>(p => p.Add(c => c.TextAlignment, TextAlign.Justify));
        var cls = cut.Find("div").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("tnt-text-align-justify");
    }

    [Fact]
    public void TextAlignment_Left_Adds_Class() {
        // Arrange & Act
        var cut = Render<TnTFooter>(p => p.Add(c => c.TextAlignment, TextAlign.Left));
        var cls = cut.Find("div").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("tnt-text-align-left");
    }

    [Fact]
    public void TextAlignment_Right_Adds_Class() {
        // Arrange & Act
        var cut = Render<TnTFooter>(p => p.Add(c => c.TextAlignment, TextAlign.Right));
        var cls = cut.Find("div").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("tnt-text-align-right");
    }

    [Fact]
    public void Zero_Elevation_Adds_Elevation_Class() {
        // Arrange & Act
        var cut = Render<TnTFooter>(p => p.Add(c => c.Elevation, 0));
        var cls = cut.Find("div").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("tnt-elevation-0");
        cls.Should().NotContain("tnt-elevation-2");
    }
}