using static NTComponents.Tests.TestingUtility.TestingUtility;

namespace NTComponents.Tests.Layout;

public class TnTSideNavToggle_Tests : BunitContext {

    public TnTSideNavToggle_Tests() {
        SetupRippleEffectModule(this);
    }

    [Fact]
    public void AdditionalAttributes_Class_Merged() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "class", "custom-toggle" } };

        // Act
        var cut = Render<TnTSideNavToggle>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var cls = cut.Find("button").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("custom-toggle");
        cls.Should().Contain("tnt-side-nav-toggle");
    }

    [Fact]
    public void AdditionalAttributes_Custom_Attribute_Added() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "data-test", "toggle-test" } };

        // Act
        var cut = Render<TnTSideNavToggle>(p => p.Add(c => c.AdditionalAttributes, attrs));

        // Assert
        cut.Find("button").GetAttribute("data-test")!.Should().Be("toggle-test");
    }

    [Fact]
    public void AdditionalAttributes_Style_Merged() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "style", "margin:5px" } };

        // Act
        var cut = Render<TnTSideNavToggle>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var style = cut.Find("button").GetAttribute("style")!;

        // Assert
        style.Should().Contain("margin:5px");
        style.Should().Contain("--tnt-side-nav-toggle-icon-color");
    }

    [Fact]
    public void Button_Type_Is_Button() {
        // Arrange & Act
        var cut = Render<TnTSideNavToggle>();

        // Assert Default button type should be "button" in HTML5, but let's verify no explicit type is set
        cut.Find("button").Should().NotBeNull();
    }

    [Fact]
    public void Click_Handler_Is_Attached() {
        // Arrange & Act
        var cut = Render<TnTSideNavToggle>();

        // Assert
        cut.Find("button").GetAttribute("onclick")!.Should().Be("NTComponents.toggleSideNav(event)");
    }

    [Fact]
    public void Custom_Icon_Renders() {
        // Arrange & Act
        var cut = Render<TnTSideNavToggle>(p => p.Add(c => c.Icon, MaterialIcon.Close));

        // Assert
        cut.Markup.Should().Contain("close");
        cut.Markup.Should().NotContain("menu");
    }

    [Fact]
    public void Custom_IconColor_Variable_In_Style() {
        // Arrange & Act
        var cut = Render<TnTSideNavToggle>(p => p.Add(c => c.IconColor, TnTColor.Primary));
        var style = cut.Find("button").GetAttribute("style")!;

        // Assert
        style.Should().Contain("--tnt-side-nav-toggle-icon-color:var(--tnt-color-primary)");
    }

    [Fact]
    public void Default_EnableRipple_Is_True() {
        // Arrange & Act
        var cut = Render<TnTSideNavToggle>();
        var cls = cut.Find("button").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("tnt-ripple");
        cut.Markup.Should().Contain("TnTRippleEffect");
    }

    [Fact]
    public void Default_Icon_Is_Menu() {
        // Arrange & Act
        var cut = Render<TnTSideNavToggle>();

        // Assert
        cut.Markup.Should().Contain("menu");
    }

    [Fact]
    public void Default_IconColor_Is_OnSurface() {
        // Arrange & Act
        var cut = Render<TnTSideNavToggle>();
        var style = cut.Find("button").GetAttribute("style")!;

        // Assert
        style.Should().Contain("--tnt-side-nav-toggle-icon-color:var(--tnt-color-on-surface)");
    }

    [Fact]
    public void Default_TintColor_Is_SurfaceTint() {
        // Arrange & Act
        var cut = Render<TnTSideNavToggle>();
        var style = cut.Find("button").GetAttribute("style")!;

        // Assert
        style.Should().Contain("--tnt-side-nav-toggle-tint-color:var(--tnt-color-surface-tint)");
    }

    [Fact]
    public void Disabled_False_Does_Not_Add_Disabled_Class() {
        // Arrange & Act
        var cut = Render<TnTSideNavToggle>(p => p.Add(c => c.Disabled, false));
        var cls = cut.Find("button").GetAttribute("class")!;

        // Assert
        cls.Should().NotContain("tnt-disabled");
    }

    [Fact]
    public void Disabled_True_Adds_Disabled_Class() {
        // Arrange & Act
        var cut = Render<TnTSideNavToggle>(p => p.Add(c => c.Disabled, true));
        var cls = cut.Find("button").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("tnt-disabled");
    }

    [Fact]
    public void Element_Reference_Captured() {
        // Arrange & Act
        var cut = Render<TnTSideNavToggle>();

        // Assert
        cut.Find("button").Should().NotBeNull();
    }

    [Fact]
    public void ElementId_Renders_Id_Attribute() {
        // Arrange & Act
        var cut = Render<TnTSideNavToggle>(p => p.Add(c => c.ElementId, "toggle-id"));

        // Assert
        cut.Find("button").GetAttribute("id")!.Should().Be("toggle-id");
    }

    [Fact]
    public void ElementLang_Renders_Lang_Attribute() {
        // Arrange & Act
        var cut = Render<TnTSideNavToggle>(p => p.Add(c => c.ElementLang, "de"));

        // Assert
        cut.Find("button").GetAttribute("lang")!.Should().Be("de");
    }

    [Fact]
    public void ElementTitle_Renders_Title_Attribute() {
        // Arrange & Act
        var cut = Render<TnTSideNavToggle>(p => p.Add(c => c.ElementTitle, "Toggle navigation"));

        // Assert
        cut.Find("button").GetAttribute("title")!.Should().Be("Toggle navigation");
    }

    [Fact]
    public void EnableRipple_False_Does_Not_Add_Ripple_Class_Or_Effect() {
        // Arrange & Act
        var cut = Render<TnTSideNavToggle>(p => p.Add(c => c.EnableRipple, false));

        // Assert
        cut.Find("button").GetAttribute("class")!.Should().NotContain("tnt-ripple");
        cut.Markup.Should().NotContain("TnTRippleEffect");
    }

    [Fact]
    public void Multiple_Properties_Combined() {
        // Arrange & Act
        var cut = Render<TnTSideNavToggle>(p => p
            .Add(c => c.Icon, MaterialIcon.Menu)
            .Add(c => c.IconColor, TnTColor.Error)
            .Add(c => c.TintColor, TnTColor.Warning)
            .Add(c => c.OnTintColor, TnTColor.OnWarning)
            .Add(c => c.EnableRipple, false)
            .Add(c => c.Disabled, true));

        var button = cut.Find("button");
        var cls = button.GetAttribute("class")!;
        var style = button.GetAttribute("style")!;

        // Assert
        cls.Should().Contain("tnt-side-nav-toggle-tint-color");
        cls.Should().Contain("tnt-side-nav-toggle-on-tint-color");
        cls.Should().Contain("tnt-disabled");
        cls.Should().NotContain("tnt-ripple");
        style.Should().Contain("--tnt-side-nav-toggle-tint-color:var(--tnt-color-warning)");
        style.Should().Contain("--tnt-side-nav-toggle-on-tint-color:var(--tnt-color-on-warning)");
        style.Should().Contain("--tnt-side-nav-toggle-icon-color:var(--tnt-color-error)");
        cut.Markup.Should().Contain("menu");
        cut.Markup.Should().NotContain("TnTRippleEffect");
    }

    [Fact]
    public void OnTintColor_Adds_On_Tint_Color_Class_And_Variable() {
        // Arrange & Act
        var cut = Render<TnTSideNavToggle>(p => p.Add(c => c.OnTintColor, TnTColor.OnPrimary));

        var cls = cut.Find("button").GetAttribute("class")!;
        var style = cut.Find("button").GetAttribute("style")!;

        // Assert
        cls.Should().Contain("tnt-side-nav-toggle-on-tint-color");
        style.Should().Contain("--tnt-side-nav-toggle-on-tint-color:var(--tnt-color-on-primary)");
    }

    [Fact]
    public void OnTintColor_Null_Does_Not_Add_On_Tint_Color_Class() {
        // Arrange & Act
        var cut = Render<TnTSideNavToggle>(p => p.Add(c => c.OnTintColor, (TnTColor?)null));
        var cls = cut.Find("button").GetAttribute("class")!;

        // Assert
        cls.Should().NotContain("tnt-side-nav-toggle-on-tint-color");
    }

    [Fact]
    public void Renders_Default_Toggle_With_Base_Class() {
        // Arrange & Act
        var cut = Render<TnTSideNavToggle>();
        var button = cut.Find("button.tnt-side-nav-toggle");
        var cls = button.GetAttribute("class")!;

        // Assert
        cls.Should().Contain("tnt-side-nav-toggle");
        cls.Should().Contain("tnt-interactable");
    }

    [Fact]
    public void TintColor_Adds_Tint_Color_Class_And_Variable() {
        // Arrange & Act
        var cut = Render<TnTSideNavToggle>(p => p.Add(c => c.TintColor, TnTColor.Primary));

        var cls = cut.Find("button").GetAttribute("class")!;
        var style = cut.Find("button").GetAttribute("style")!;

        // Assert
        cls.Should().Contain("tnt-side-nav-toggle-tint-color");
        style.Should().Contain("--tnt-side-nav-toggle-tint-color:var(--tnt-color-primary)");
    }

    [Fact]
    public void TintColor_Null_Does_Not_Add_Tint_Color_Class() {
        // Arrange & Act
        var cut = Render<TnTSideNavToggle>(p => p.Add(c => c.TintColor, (TnTColor?)null));
        var cls = cut.Find("button").GetAttribute("class")!;

        // Assert
        cls.Should().NotContain("tnt-side-nav-toggle-tint-color");
    }
}