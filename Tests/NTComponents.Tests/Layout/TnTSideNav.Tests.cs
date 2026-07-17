namespace NTComponents.Tests.Layout;

public class TnTSideNav_Tests : BunitContext {

    [Fact]
    public void AdditionalAttributes_Class_Merged() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "class", "custom-sidenav" } };

        // Act
        var cut = Render<TnTSideNav>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var cls = cut.Find("div.tnt-side-nav").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("custom-sidenav");
        cls.Should().Contain("tnt-side-nav");
    }

    [Fact]
    public void AdditionalAttributes_Style_Merged() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "style", "width:300px" } };

        // Act
        var cut = Render<TnTSideNav>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var style = cut.Find("div.tnt-side-nav").GetAttribute("style")!;

        // Assert
        style.Should().Contain("width:300px");
        style.Should().Contain("--tnt-side-nav-bg-color");
    }

    [Fact]
    public void Cascading_Value_Is_Provided() {
        // Arrange & Act
        var cut = Render<TnTSideNav>(p => p.AddChildContent("<div>Content</div>"));

        // Assert
        cut.Find("div.tnt-side-nav").Should().NotBeNull();
        // The CascadingValue should be providing the component instance
        cut.Markup.Should().Contain("Content");
    }

    [Fact]
    public void ChildContent_Renders_Correctly() {
        // Arrange & Act
        var cut = Render<TnTSideNav>(p => p.AddChildContent("<nav>Navigation Content</nav>"));

        // Assert
        cut.Markup.Should().Contain("<nav>Navigation Content</nav>");
    }

    [Fact]
    public void Custom_BackgroundColor_Added_To_Class() {
        // Arrange & Act
        var cut = Render<TnTSideNav>(p => p.Add(c => c.BackgroundColor, TnTColor.Secondary));
        var cls = cut.Find("div.tnt-side-nav").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("tnt-bg-color-secondary");
    }

    [Fact]
    public void Custom_BackgroundColor_Variable_In_Style() {
        // Arrange & Act
        var cut = Render<TnTSideNav>(p => p.Add(c => c.BackgroundColor, TnTColor.Primary));
        var style = cut.Find("div.tnt-side-nav").GetAttribute("style")!;

        // Assert
        style.Should().Contain("--tnt-side-nav-bg-color:var(--tnt-color-primary)");
    }

    [Fact]
    public void Custom_TextColor_Added_To_Class() {
        // Arrange & Act
        var cut = Render<TnTSideNav>(p => p.Add(c => c.TextColor, TnTColor.OnSecondary));
        var cls = cut.Find("div.tnt-side-nav").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("tnt-fg-color-on-secondary");
    }

    [Fact]
    public void Custom_TextColor_Variable_In_Style() {
        // Arrange & Act
        var cut = Render<TnTSideNav>(p => p.Add(c => c.TextColor, TnTColor.OnPrimary));
        var style = cut.Find("div.tnt-side-nav").GetAttribute("style")!;

        // Assert
        style.Should().Contain("--tnt-side-nav-fg-color:var(--tnt-color-on-primary)");
    }

    [Fact]
    public void Default_BackgroundColor_Added_To_Class() {
        // Arrange & Act
        var cut = Render<TnTSideNav>();
        var cls = cut.Find("div.tnt-side-nav").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("tnt-bg-color-surface-container");
    }

    [Fact]
    public void Default_BackgroundColor_Is_SurfaceContainer() {
        // Arrange & Act
        var cut = Render<TnTSideNav>();
        var style = cut.Find("div.tnt-side-nav").GetAttribute("style")!;

        // Assert
        style.Should().Contain("--tnt-side-nav-bg-color:var(--tnt-color-surface-container)");
    }

    [Fact]
    public void Default_TextColor_Added_To_Class() {
        // Arrange & Act
        var cut = Render<TnTSideNav>();
        var cls = cut.Find("div.tnt-side-nav").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("tnt-fg-color-on-surface-variant");
    }

    [Fact]
    public void Default_TextColor_Is_OnSurfaceVariant() {
        // Arrange & Act
        var cut = Render<TnTSideNav>();
        var style = cut.Find("div.tnt-side-nav").GetAttribute("style")!;

        // Assert
        style.Should().Contain("--tnt-side-nav-fg-color:var(--tnt-color-on-surface-variant)");
    }

    [Fact]
    public void Element_Reference_Captured() {
        // Arrange & Act
        var cut = Render<TnTSideNav>();

        // Assert
        cut.Find("div.tnt-side-nav").Should().NotBeNull();
    }

    [Fact]
    public void ElementId_Renders_Id_Attribute() {
        // Arrange & Act
        var cut = Render<TnTSideNav>(p => p.Add(c => c.ElementId, "side-nav-id"));

        // Assert
        cut.Find("div.tnt-side-nav").GetAttribute("id")!.Should().Be("side-nav-id");
    }

    [Fact]
    public void ElementLang_Renders_Lang_Attribute() {
        // Arrange & Act
        var cut = Render<TnTSideNav>(p => p.Add(c => c.ElementLang, "en"));

        // Assert
        cut.Find("div.tnt-side-nav").GetAttribute("lang")!.Should().Be("en");
    }

    [Fact]
    public void ElementTitle_Renders_Title_Attribute() {
        // Arrange & Act
        var cut = Render<TnTSideNav>(p => p.Add(c => c.ElementTitle, "Side navigation"));

        // Assert
        cut.Find("div.tnt-side-nav").GetAttribute("title")!.Should().Be("Side navigation");
    }

    [Fact]
    public void HideOnLargeScreens_False_Does_Not_Add_Class() {
        // Arrange & Act
        var cut = Render<TnTSideNav>(p => p.Add(c => c.HideOnLargeScreens, false));
        var cls = cut.Find("div.tnt-side-nav").GetAttribute("class")!;

        // Assert
        cls.Should().NotContain("tnt-side-nav-hide-on-large");
    }

    [Fact]
    public void HideOnLargeScreens_True_Adds_Class() {
        // Arrange & Act
        var cut = Render<TnTSideNav>(p => p.Add(c => c.HideOnLargeScreens, true));
        var cls = cut.Find("div.tnt-side-nav").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("tnt-side-nav-hide-on-large");
    }

    [Fact]
    public void Multiple_Properties_Combined() {
        // Arrange & Act
        var cut = Render<TnTSideNav>(p => p
            .Add(c => c.BackgroundColor, TnTColor.Tertiary)
            .Add(c => c.TextColor, TnTColor.OnTertiary)
            .Add(c => c.TextAlignment, TextAlign.Right)
            .Add(c => c.HideOnLargeScreens, true));

        var div = cut.Find("div.tnt-side-nav");
        var cls = div.GetAttribute("class")!;
        var style = div.GetAttribute("style")!;

        // Assert
        cls.Should().Contain("tnt-bg-color-tertiary");
        cls.Should().Contain("tnt-fg-color-on-tertiary");
        cls.Should().Contain("tnt-text-align-right");
        cls.Should().Contain("tnt-side-nav-hide-on-large");
        style.Should().Contain("--tnt-side-nav-bg-color:var(--tnt-color-tertiary)");
        style.Should().Contain("--tnt-side-nav-fg-color:var(--tnt-color-on-tertiary)");
    }

    [Fact]
    public void Renders_Default_SideNav_With_Base_Class() {
        // Arrange & Act
        var cut = Render<TnTSideNav>(p => p.AddChildContent("SideNav Content"));
        var div = cut.Find("div.tnt-side-nav");
        var cls = div.GetAttribute("class")!;

        // Assert
        cls.Should().Contain("tnt-side-nav");
        cls.Should().Contain("tnt-components");
        cut.Markup.Should().Contain("SideNav Content");
    }

    [Fact]
    public void TextAlignment_Center_Adds_Class() {
        // Arrange & Act
        var cut = Render<TnTSideNav>(p => p.Add(c => c.TextAlignment, TextAlign.Center));
        var cls = cut.Find("div.tnt-side-nav").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("tnt-text-align-center");
    }

    [Fact]
    public void Toggle_Indicator_Has_Data_Permanent_Attribute() {
        // Arrange & Act
        var cut = Render<TnTSideNav>();

        // Assert
        cut.Find("div.tnt-side-nav-toggle-indicator").HasAttribute("data-permanent").Should().BeTrue();
    }

    [Fact]
    public void Toggle_Indicator_Is_Rendered() {
        // Arrange & Act
        var cut = Render<TnTSideNav>();

        // Assert
        cut.Find("div.tnt-side-nav-toggle-indicator").Should().NotBeNull();
        cut.Find("div.tnt-toggle-indicator").Should().NotBeNull();
    }
}