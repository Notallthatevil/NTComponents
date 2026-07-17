using static NTComponents.Tests.TestingUtility.TestingUtility;

namespace NTComponents.Tests.Layout;

public class TnTSideNavMenuGroup_Tests : BunitContext {

    public TnTSideNavMenuGroup_Tests() {
        SetupRippleEffectModule(this);
    }

    [Fact]
    public void AdditionalAttributes_Applied_To_Element() {
        // Arrange
        var attrs = new Dictionary<string, object> {
            { "data-test", "menu-group" },
            { "aria-label", "Navigation group" }
        };

        // Act
        var cut = Render<TnTSideNavMenuGroup>(p => p
            .Add(c => c.Label, "Test")
            .Add(c => c.AdditionalAttributes, attrs));

        var outerDiv = cut.Find("div");  // First div element

        // Assert - Check that custom attributes are applied
        outerDiv.GetAttribute("data-test")!.Should().Be("menu-group");
        outerDiv.GetAttribute("aria-label")!.Should().Be("Navigation group");

        // Base classes should still be present
        outerDiv.GetAttribute("class")!.Should().Contain("tnt-side-nav-menu-group");
        outerDiv.GetAttribute("class")!.Should().Contain("tnt-components");
    }

    [Fact]
    public void Arrow_Icon_Is_Present() {
        // Arrange & Act
        var cut = Render<TnTSideNavMenuGroup>(p => p.Add(c => c.Label, "Test"));

        // Assert
        cut.Find("span.tnt-close-icon").Should().NotBeNull();
        cut.Markup.Should().Contain("arrow_drop_up");
    }

    [Fact]
    public void AutoFocus_True_Renders_Autofocus_Attribute() {
        // Arrange & Act
        var cut = Render<TnTSideNavMenuGroup>(p => p
            .Add(c => c.Label, "Test")
            .Add(c => c.AutoFocus, true));

        // Assert
        cut.Find("div.tnt-side-nav-menu-group").HasAttribute("autofocus").Should().BeTrue();
    }

    [Fact]
    public void ChildContent_Renders_In_Content_Section() {
        // Arrange & Act
        var cut = Render<TnTSideNavMenuGroup>(p => p
            .Add(c => c.Label, "Test")
            .AddChildContent("<div>Menu Item 1</div><div>Menu Item 2</div>"));

        // Assert
        cut.Find("div.tnt-side-nav-menu-group-content").InnerHtml.Should().Contain("<div>Menu Item 1</div>");
        cut.Find("div.tnt-side-nav-menu-group-content").InnerHtml.Should().Contain("<div>Menu Item 2</div>");
    }

    [Fact]
    public void Click_Handler_Is_Attached() {
        // Arrange & Act
        var cut = Render<TnTSideNavMenuGroup>(p => p.Add(c => c.Label, "Test"));

        // Assert
        cut.Find("div.tnt-side-nav-menu-group-label").GetAttribute("onclick")!
            .Should().Be("NTComponents.toggleSideNavGroup(event)");
    }

    [Fact]
    public void Custom_BackgroundColor_Variable_In_Style() {
        // Arrange & Act
        var cut = Render<TnTSideNavMenuGroup>(p => p
            .Add(c => c.Label, "Test")
            .Add(c => c.BackgroundColor, TnTColor.Primary));
        var style = cut.Find("div.tnt-side-nav-menu-group").GetAttribute("style")!;

        // Assert
        style.Should().Contain("--tnt-side-nav-menu-group-bg-color:var(--tnt-color-primary)");
    }

    [Fact]
    public void Custom_TextColor_Variable_In_Style() {
        // Arrange & Act
        var cut = Render<TnTSideNavMenuGroup>(p => p
            .Add(c => c.Label, "Test")
            .Add(c => c.TextColor, TnTColor.OnPrimary));
        var style = cut.Find("div.tnt-side-nav-menu-group").GetAttribute("style")!;

        // Assert
        style.Should().Contain("--tnt-side-nav-menu-group-fg-color:var(--tnt-color-on-primary)");
    }

    [Fact]
    public void Data_Permanent_Section_Is_Present() {
        // Arrange & Act
        var cut = Render<TnTSideNavMenuGroup>(p => p.Add(c => c.Label, "Test"));

        // Assert
        cut.Find("span.tnt-side-nav-data-permanent").Should().NotBeNull();
        cut.Find("span.tnt-side-nav-data-permanent").HasAttribute("data-permanent").Should().BeTrue();
    }

    [Fact]
    public void Default_BackgroundColor_Is_SurfaceVariant() {
        // Arrange & Act
        var cut = Render<TnTSideNavMenuGroup>(p => p.Add(c => c.Label, "Test"));
        var style = cut.Find("div.tnt-side-nav-menu-group").GetAttribute("style")!;

        // Assert
        style.Should().Contain("--tnt-side-nav-menu-group-bg-color:var(--tnt-color-surface-variant)");
    }

    [Fact]
    public void Default_EnableRipple_Is_True() {
        // Arrange & Act
        var cut = Render<TnTSideNavMenuGroup>(p => p.Add(c => c.Label, "Test"));

        // Assert
        cut.Find("div.tnt-side-nav-menu-group-label").GetAttribute("class")!.Should().Contain("tnt-ripple");
        cut.Markup.Should().Contain("TnTRippleEffect");
    }

    [Fact]
    public void Default_ExpandByDefault_Is_True() {
        // Arrange & Act
        var cut = Render<TnTSideNavMenuGroup>(p => p.Add(c => c.Label, "Test"));

        // Assert
        cut.Find("span.tnt-side-nav-menu-group-toggler").GetAttribute("class")!.Should().Contain("tnt-toggle");
    }

    [Fact]
    public void Default_TextColor_Is_OnSurfaceVariant() {
        // Arrange & Act
        var cut = Render<TnTSideNavMenuGroup>(p => p.Add(c => c.Label, "Test"));
        var style = cut.Find("div.tnt-side-nav-menu-group").GetAttribute("style")!;

        // Assert
        style.Should().Contain("--tnt-side-nav-menu-group-fg-color:var(--tnt-color-on-surface-variant)");
    }

    [Fact]
    public void Default_TintColor_Is_SurfaceTint() {
        // Arrange & Act
        var cut = Render<TnTSideNavMenuGroup>(p => p.Add(c => c.Label, "Test"));
        var style = cut.Find("div.tnt-side-nav-menu-group").GetAttribute("style")!;

        // Assert
        style.Should().Contain("--tnt-side-nav-menu-group-tint-color:var(--tnt-color-surface-tint)");
    }

    [Fact]
    public void Disabled_False_Does_Not_Add_Disabled_Class() {
        // Arrange & Act
        var cut = Render<TnTSideNavMenuGroup>(p => p
            .Add(c => c.Label, "Test")
            .Add(c => c.Disabled, false));

        // Assert
        cut.Find("div.tnt-side-nav-menu-group").GetAttribute("class")!.Should().NotContain("tnt-disabled");
    }

    [Fact]
    public void Disabled_True_Adds_Disabled_Class() {
        // Arrange & Act
        var cut = Render<TnTSideNavMenuGroup>(p => p
            .Add(c => c.Label, "Test")
            .Add(c => c.Disabled, true));

        // Assert
        cut.Find("div.tnt-side-nav-menu-group").GetAttribute("class")!.Should().Contain("tnt-disabled");
    }

    [Fact]
    public void Element_Reference_Captured() {
        // Arrange & Act
        var cut = Render<TnTSideNavMenuGroup>(p => p.Add(c => c.Label, "Test"));

        // Assert
        cut.Find("div.tnt-side-nav-menu-group").Should().NotBeNull();
    }

    [Fact]
    public void ElementId_Renders_Id_Attribute() {
        // Arrange & Act
        var cut = Render<TnTSideNavMenuGroup>(p => p
            .Add(c => c.Label, "Test")
            .Add(c => c.ElementId, "group-id"));

        // Assert
        cut.Find("div.tnt-side-nav-menu-group").GetAttribute("id")!.Should().Be("group-id");
    }

    [Fact]
    public void ElementLang_Renders_Lang_Attribute() {
        // Arrange & Act
        var cut = Render<TnTSideNavMenuGroup>(p => p
            .Add(c => c.Label, "Test")
            .Add(c => c.ElementLang, "es"));

        // Assert
        cut.Find("div.tnt-side-nav-menu-group").GetAttribute("lang")!.Should().Be("es");
    }

    [Fact]
    public void ElementTitle_Renders_Title_Attribute() {
        // Arrange & Act
        var cut = Render<TnTSideNavMenuGroup>(p => p
            .Add(c => c.Label, "Test")
            .Add(c => c.ElementTitle, "Menu group"));

        // Assert
        cut.Find("div.tnt-side-nav-menu-group").GetAttribute("title")!.Should().Be("Menu group");
    }

    [Fact]
    public void EnableRipple_False_Does_Not_Add_Ripple_Class_Or_Effect() {
        // Arrange & Act
        var cut = Render<TnTSideNavMenuGroup>(p => p
            .Add(c => c.Label, "Test")
            .Add(c => c.EnableRipple, false));

        // Assert
        cut.Find("div.tnt-side-nav-menu-group-label").GetAttribute("class")!.Should().NotContain("tnt-ripple");
        cut.Markup.Should().NotContain("TnTRippleEffect");
    }

    [Fact]
    public void ExpandByDefault_False_Does_Not_Add_Toggle_Class() {
        // Arrange & Act
        var cut = Render<TnTSideNavMenuGroup>(p => p
            .Add(c => c.Label, "Test")
            .Add(c => c.ExpandByDefault, false));

        // Assert
        cut.Find("span.tnt-side-nav-menu-group-toggler").GetAttribute("class")!.Should().NotContain("tnt-toggle");
    }

    [Fact]
    public void Icon_Does_Not_Render_When_Null() {
        // Arrange & Act
        var cut = Render<TnTSideNavMenuGroup>(p => p
            .Add(c => c.Label, "Test")
            .Add(c => c.Icon, (TnTIcon?)null));

        // Assert
        cut.Find("span.tnt-side-nav-menu-group-label-content").InnerHtml.Should().NotContain("<span");
    }

    [Fact]
    public void Icon_Renders_When_Provided() {
        // Arrange & Act
        var cut = Render<TnTSideNavMenuGroup>(p => p
            .Add(c => c.Label, "Test")
            .Add(c => c.Icon, MaterialIcon.Folder));

        // Assert
        cut.Markup.Should().Contain("folder");
    }

    [Fact]
    public void Interactable_Class_Is_Present() {
        // Arrange & Act
        var cut = Render<TnTSideNavMenuGroup>(p => p.Add(c => c.Label, "Test"));

        // Assert
        cut.Find("div.tnt-side-nav-menu-group-label").GetAttribute("class")!.Should().Contain("tnt-interactable");
    }

    [Fact]
    public void Label_Is_Required_And_Renders() {
        // Arrange & Act
        var cut = Render<TnTSideNavMenuGroup>(p => p.Add(c => c.Label, "Test Label"));

        // Assert
        cut.Find("span.tnt-side-nav-menu-group-label-content").InnerHtml.Should().Contain("Test Label");
    }

    [Fact]
    public void Multiple_Properties_Combined() {
        // Arrange & Act
        var cut = Render<TnTSideNavMenuGroup>(p => p
            .Add(c => c.Label, "Multi Property Group")
            .Add(c => c.BackgroundColor, TnTColor.Tertiary)
            .Add(c => c.TextColor, TnTColor.OnTertiary)
            .Add(c => c.TintColor, TnTColor.Warning)
            .Add(c => c.OnTintColor, TnTColor.OnWarning)
            .Add(c => c.Icon, MaterialIcon.Settings)
            .Add(c => c.EnableRipple, false)
            .Add(c => c.ExpandByDefault, false)
            .Add(c => c.Disabled, true)
            .AddChildContent("<div>Child Item</div>"));

        var mainDiv = cut.Find("div.tnt-side-nav-menu-group");
        var labelDiv = cut.Find("div.tnt-side-nav-menu-group-label");
        var contentDiv = cut.Find("div.tnt-side-nav-menu-group-content");
        var toggleSpan = cut.Find("span.tnt-side-nav-menu-group-toggler");

        var mainCls = mainDiv.GetAttribute("class")!;
        var labelCls = labelDiv.GetAttribute("class")!;
        var style = mainDiv.GetAttribute("style")!;

        // Assert
        mainCls.Should().Contain("tnt-disabled");
        labelCls.Should().Contain("tnt-side-nav-menu-group-tint-color");
        labelCls.Should().Contain("tnt-side-nav-menu-group-on-tint-color");
        labelCls.Should().NotContain("tnt-ripple");
        toggleSpan.GetAttribute("class")!.Should().NotContain("tnt-toggle");
        style.Should().Contain("--tnt-side-nav-menu-group-bg-color:var(--tnt-color-tertiary)");
        style.Should().Contain("--tnt-side-nav-menu-group-fg-color:var(--tnt-color-on-tertiary)");
        style.Should().Contain("--tnt-side-nav-menu-group-tint-color:var(--tnt-color-warning)");
        style.Should().Contain("--tnt-side-nav-menu-group-on-tint-color:var(--tnt-color-on-warning)");
        cut.Markup.Should().Contain("settings");
        cut.Markup.Should().Contain("Multi Property Group");
        contentDiv.InnerHtml.Should().Contain("<div>Child Item</div>");
        cut.Markup.Should().NotContain("TnTRippleEffect");
    }

    [Fact]
    public void OnTintColor_Adds_On_Tint_Color_Class_And_Variable() {
        // Arrange & Act
        var cut = Render<TnTSideNavMenuGroup>(p => p
            .Add(c => c.Label, "Test")
            .Add(c => c.OnTintColor, TnTColor.OnPrimary));

        var cls = cut.Find("div.tnt-side-nav-menu-group-label").GetAttribute("class")!;
        var style = cut.Find("div.tnt-side-nav-menu-group").GetAttribute("style")!;

        // Assert
        cls.Should().Contain("tnt-side-nav-menu-group-on-tint-color");
        style.Should().Contain("--tnt-side-nav-menu-group-on-tint-color:var(--tnt-color-on-primary)");
    }

    [Fact]
    public void Renders_Default_MenuGroup_With_Base_Class() {
        // Arrange & Act
        var cut = Render<TnTSideNavMenuGroup>(p => p
            .Add(c => c.Label, "Menu Group")
            .AddChildContent("Group Content"));
        var div = cut.Find("div.tnt-side-nav-menu-group");

        // Assert
        div.Should().NotBeNull();
        cut.Markup.Should().Contain("Menu Group");
        cut.Markup.Should().Contain("Group Content");
    }

    [Fact]
    public void TintColor_Adds_Tint_Color_Class_And_Variable() {
        // Arrange & Act
        var cut = Render<TnTSideNavMenuGroup>(p => p
            .Add(c => c.Label, "Test")
            .Add(c => c.TintColor, TnTColor.Primary));

        var cls = cut.Find("div.tnt-side-nav-menu-group-label").GetAttribute("class")!;
        var style = cut.Find("div.tnt-side-nav-menu-group").GetAttribute("style")!;

        // Assert
        cls.Should().Contain("tnt-side-nav-menu-group-tint-color");
        style.Should().Contain("--tnt-side-nav-menu-group-tint-color:var(--tnt-color-primary)");
    }
}