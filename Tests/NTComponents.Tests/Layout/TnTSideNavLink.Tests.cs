using static NTComponents.Tests.TestingUtility.TestingUtility;

namespace NTComponents.Tests.Layout;

public class TnTSideNavLink_Tests : BunitContext {

    public TnTSideNavLink_Tests() {
        SetupRippleEffectModule(this);
    }

    [Fact]
    public void ActiveBackgroundColor_Adds_Active_Bg_Color_Class() {
        // Arrange & Act
        var cut = Render<TnTSideNavLink>(p => p.Add(c => c.ActiveBackgroundColor, TnTColor.Success));
        var cls = cut.Find("a").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("active-bg-color");
    }

    [Fact]
    public void ActiveTextColor_Adds_Active_Fg_Color_Class() {
        // Arrange & Act
        var cut = Render<TnTSideNavLink>(p => p.Add(c => c.ActiveTextColor, TnTColor.Success));
        var cls = cut.Find("a").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("active-fg-color");
    }

    [Fact]
    public void AdditionalAttributes_Class_Merged() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "class", "custom-link" } };

        // Act
        var cut = Render<TnTSideNavLink>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var cls = cut.Find("a").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("custom-link");
        cls.Should().Contain("tnt-side-nav-link");
    }

    [Fact]
    public void AdditionalAttributes_Href_Added() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "href", "/home" } };

        // Act
        var cut = Render<TnTSideNavLink>(p => p.Add(c => c.AdditionalAttributes, attrs));

        // Assert
        cut.Find("a").GetAttribute("href")!.Should().Be("/home");
    }

    [Fact]
    public void AutoFocus_False_Does_Not_Render_Autofocus_Attribute() {
        // Arrange & Act
        var cut = Render<TnTSideNavLink>(p => p.Add(c => c.AutoFocus, false));

        // Assert
        cut.Find("a").HasAttribute("autofocus").Should().BeFalse();
    }

    [Fact]
    public void AutoFocus_True_Renders_Autofocus_Attribute() {
        // Arrange & Act
        var cut = Render<TnTSideNavLink>(p => p.Add(c => c.AutoFocus, true));

        // Assert
        cut.Find("a").HasAttribute("autofocus").Should().BeTrue();
    }

    [Fact]
    public void ChildContent_Renders_Correctly() {
        // Arrange & Act
        var cut = Render<TnTSideNavLink>(p => p.AddChildContent("Navigation Item"));

        // Assert
        cut.Markup.Should().Contain("Navigation Item");
    }

    [Fact]
    public void Custom_BackgroundColor_Variable_In_Style() {
        // Arrange & Act
        var cut = Render<TnTSideNavLink>(p => p.Add(c => c.BackgroundColor, TnTColor.Primary));
        var style = cut.Find("a").GetAttribute("style")!;

        // Assert
        style.Should().Contain("--tnt-side-nav-bg-color:var(--tnt-color-primary)");
    }

    [Fact]
    public void Custom_OnTintColor_Variable_In_Style() {
        // Arrange & Act
        var cut = Render<TnTSideNavLink>(p => p.Add(c => c.OnTintColor, TnTColor.OnSuccess));
        var style = cut.Find("a").GetAttribute("style")!;

        // Assert
        style.Should().Contain("--tnt-side-nav-on-tint-color:var(--tnt-color-on-success)");
    }

    [Fact]
    public void Custom_TextColor_Variable_In_Style() {
        // Arrange & Act
        var cut = Render<TnTSideNavLink>(p => p.Add(c => c.TextColor, TnTColor.OnPrimary));
        var style = cut.Find("a").GetAttribute("style")!;

        // Assert
        style.Should().Contain("--tnt-side-nav-fg-color:var(--tnt-color-on-primary)");
    }

    [Fact]
    public void Custom_TintColor_Variable_In_Style() {
        // Arrange & Act
        var cut = Render<TnTSideNavLink>(p => p.Add(c => c.TintColor, TnTColor.Success));
        var style = cut.Find("a").GetAttribute("style")!;

        // Assert
        style.Should().Contain("--tnt-side-nav-tint-color:var(--tnt-color-success)");
    }

    [Fact]
    public void Data_Permanent_Attribute_Is_Present() {
        // Arrange & Act
        var cut = Render<TnTSideNavLink>();

        // Assert
        cut.Find("a").HasAttribute("data-permanent").Should().BeTrue();
    }

    [Fact]
    public void Default_ActiveBackgroundColor_Is_PrimaryContainer() {
        // Arrange & Act
        var cut = Render<TnTSideNavLink>();
        var style = cut.Find("a").GetAttribute("style")!;

        // Assert
        style.Should().Contain("--tnt-side-nav-active-bg-color:var(--tnt-color-primary-container)");
    }

    [Fact]
    public void Default_ActiveTextColor_Is_OnPrimaryContainer() {
        // Arrange & Act
        var cut = Render<TnTSideNavLink>();
        var style = cut.Find("a").GetAttribute("style")!;

        // Assert
        style.Should().Contain("--tnt-side-nav-active-fg-color:var(--tnt-color-on-primary-container)");
    }

    [Fact]
    public void Default_BackgroundColor_Is_SecondaryContainer() {
        // Arrange & Act
        var cut = Render<TnTSideNavLink>();
        var style = cut.Find("a").GetAttribute("style")!;

        // Assert
        style.Should().Contain("--tnt-side-nav-bg-color:var(--tnt-color-secondary-container)");
    }

    [Fact]
    public void Default_EnableRipple_Is_True() {
        // Arrange & Act
        var cut = Render<TnTSideNavLink>();
        var cls = cut.Find("a").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("tnt-ripple");
        cut.Markup.Should().Contain("TnTRippleEffect");
    }

    [Fact]
    public void Default_TextColor_Is_Secondary() {
        // Arrange & Act
        var cut = Render<TnTSideNavLink>();
        var style = cut.Find("a").GetAttribute("style")!;

        // Assert
        style.Should().Contain("--tnt-side-nav-fg-color:var(--tnt-color-secondary)");
    }

    [Fact]
    public void Default_TintColor_Is_SurfaceTint() {
        // Arrange & Act
        var cut = Render<TnTSideNavLink>();
        var style = cut.Find("a").GetAttribute("style")!;

        // Assert
        style.Should().Contain("--tnt-side-nav-tint-color:var(--tnt-color-surface-tint)");
    }

    [Fact]
    public void Disabled_False_Does_Not_Add_Disabled_Class() {
        // Arrange & Act
        var cut = Render<TnTSideNavLink>(p => p.Add(c => c.Disabled, false));
        var cls = cut.Find("a").GetAttribute("class")!;

        // Assert
        cls.Should().NotContain("tnt-disabled");
    }

    [Fact]
    public void Disabled_Removes_Href_From_AdditionalAttributes() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "href", "/home" } };

        // Act
        var cut = Render<TnTSideNavLink>(p => p
            .Add(c => c.Disabled, true)
            .Add(c => c.AdditionalAttributes, attrs));

        // Assert
        cut.Find("a").HasAttribute("href").Should().BeFalse();
    }

    [Fact]
    public void Disabled_True_Adds_Disabled_Class() {
        // Arrange & Act
        var cut = Render<TnTSideNavLink>(p => p.Add(c => c.Disabled, true));
        var cls = cut.Find("a").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("tnt-disabled");
    }

    [Fact]
    public void Element_Reference_Captured() {
        // Arrange & Act
        var cut = Render<TnTSideNavLink>();

        // Assert
        cut.Find("a").Should().NotBeNull();
    }

    [Fact]
    public void ElementId_Renders_Id_Attribute() {
        // Arrange & Act
        var cut = Render<TnTSideNavLink>(p => p.Add(c => c.ElementId, "nav-link-id"));

        // Assert
        cut.Find("a").GetAttribute("id")!.Should().Be("nav-link-id");
    }

    [Fact]
    public void ElementLang_Renders_Lang_Attribute() {
        // Arrange & Act
        var cut = Render<TnTSideNavLink>(p => p.Add(c => c.ElementLang, "fr"));

        // Assert
        cut.Find("a").GetAttribute("lang")!.Should().Be("fr");
    }

    [Fact]
    public void ElementTitle_Renders_Title_Attribute() {
        // Arrange & Act
        var cut = Render<TnTSideNavLink>(p => p.Add(c => c.ElementTitle, "Navigation link"));

        // Assert
        cut.Find("a").GetAttribute("title")!.Should().Be("Navigation link");
    }

    [Fact]
    public void EnableRipple_False_Does_Not_Add_Ripple_Class_Or_Effect() {
        // Arrange & Act
        var cut = Render<TnTSideNavLink>(p => p.Add(c => c.EnableRipple, false));
        var cls = cut.Find("a").GetAttribute("class")!;

        // Assert
        cls.Should().NotContain("tnt-ripple");
        cut.Markup.Should().NotContain("TnTRippleEffect");
    }

    [Fact]
    public void Icon_Does_Not_Render_When_Null() {
        // Arrange & Act
        var cut = Render<TnTSideNavLink>(p => p.Add(c => c.Icon, (TnTIcon?)null));

        // Assert Should not contain any icon content when null
        cut.Find("a").InnerHtml.Should().NotContain("<span");
    }

    [Fact]
    public void Icon_Renders_When_Provided() {
        // Arrange & Act
        var cut = Render<TnTSideNavLink>(p => p.Add(c => c.Icon, MaterialIcon.Home));

        // Assert
        cut.Markup.Should().Contain("home");
    }

    [Fact]
    public void Multiple_Properties_Combined() {
        // Arrange & Act
        var cut = Render<TnTSideNavLink>(p => p
            .Add(c => c.BackgroundColor, TnTColor.Tertiary)
            .Add(c => c.TextColor, TnTColor.OnTertiary)
            .Add(c => c.TintColor, TnTColor.Warning)
            .Add(c => c.OnTintColor, TnTColor.OnWarning)
            .Add(c => c.ActiveBackgroundColor, TnTColor.Error)
            .Add(c => c.ActiveTextColor, TnTColor.OnError)
            .Add(c => c.Icon, MaterialIcon.Star)
            .Add(c => c.EnableRipple, false)
            .AddChildContent("Multi Property Link"));

        var anchor = cut.Find("a");
        var cls = anchor.GetAttribute("class")!;
        var style = anchor.GetAttribute("style")!;

        // Assert
        cls.Should().Contain("tnt-side-nav-tint-color");
        cls.Should().Contain("tnt-side-nav-on-tint-color");
        cls.Should().Contain("active-fg-color");
        cls.Should().Contain("active-bg-color");
        cls.Should().NotContain("tnt-ripple");
        style.Should().Contain("--tnt-side-nav-bg-color:var(--tnt-color-tertiary)");
        style.Should().Contain("--tnt-side-nav-fg-color:var(--tnt-color-on-tertiary)");
        style.Should().Contain("--tnt-side-nav-tint-color:var(--tnt-color-warning)");
        style.Should().Contain("--tnt-side-nav-on-tint-color:var(--tnt-color-on-warning)");
        style.Should().Contain("--tnt-side-nav-active-bg-color:var(--tnt-color-error)");
        style.Should().Contain("--tnt-side-nav-active-fg-color:var(--tnt-color-on-error)");
        cut.Markup.Should().Contain("star");
        cut.Markup.Should().Contain("Multi Property Link");
        cut.Markup.Should().NotContain("TnTRippleEffect");
    }

    [Fact]
    public void OnTintColor_Adds_On_Tint_Color_Class() {
        // Arrange & Act
        var cut = Render<TnTSideNavLink>(p => p.Add(c => c.OnTintColor, TnTColor.OnPrimary));
        var cls = cut.Find("a").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("tnt-side-nav-on-tint-color");
    }

    [Fact]
    public void Renders_Default_SideNavLink_With_Base_Class() {
        // Arrange & Act
        var cut = Render<TnTSideNavLink>(p => p.AddChildContent("Link Content"));
        var anchor = cut.Find("a.tnt-side-nav-link");
        var cls = anchor.GetAttribute("class")!;

        // Assert
        cls.Should().Contain("tnt-side-nav-link");
        cls.Should().Contain("tnt-interactable");
        cut.Markup.Should().Contain("Link Content");
    }

    [Fact]
    public void TintColor_Adds_Tint_Color_Class() {
        // Arrange & Act
        var cut = Render<TnTSideNavLink>(p => p.Add(c => c.TintColor, TnTColor.Primary));
        var cls = cut.Find("a").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("tnt-side-nav-tint-color");
    }
}