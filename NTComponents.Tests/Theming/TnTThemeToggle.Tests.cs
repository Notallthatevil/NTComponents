namespace NTComponents.Tests.Theming;

/// <summary>
///     Unit tests for <see cref="TnTThemeToggle" />.
/// </summary>
public class TnTThemeToggle_Tests : BunitContext {

    public TnTThemeToggle_Tests() {
        // Set up JavaScript module for theme toggle functionality
        var themeToggleModule = JSInterop.SetupModule("./_content/NTComponents/Theming/TnTThemeToggle.razor.js?v=2");
        themeToggleModule.SetupVoid("onLoad", _ => true);
        themeToggleModule.SetupVoid("onUpdate", _ => true);
        themeToggleModule.SetupVoid("onDispose", _ => true);
    }

    [Fact]
    public void AdditionalAttributes_AreAppliedToThemeToggle() {
        // Arrange
        var cut = RenderThemeToggle(parameters => parameters
            .AddUnmatched("data-testid", "test-theme-toggle")
            .AddUnmatched("aria-label", "Theme Toggle"));

        // Act & Assert
        var themeToggle = cut.Find("tnt-theme-toggle");
        themeToggle.GetAttribute("data-testid").Should().Be("test-theme-toggle");
        themeToggle.GetAttribute("aria-label").Should().Be("Theme Toggle");
    }

    [Fact]
    public void AllCssAttributesArePresent() {
        // Arrange & Act
        var cut = RenderThemeToggle();

        // Assert
        var element = cut.Find("tnt-theme-toggle");
        element.GetAttribute("tnt-light-default").Should().Be("light.css");
        element.GetAttribute("tnt-light-medium").Should().Be("light-mc.css");
        element.GetAttribute("tnt-light-high").Should().Be("light-hc.css");
        element.GetAttribute("tnt-dark-default").Should().Be("dark.css");
        element.GetAttribute("tnt-dark-medium").Should().Be("dark-mc.css");
        element.GetAttribute("tnt-dark-high").Should().Be("dark-hc.css");
    }

    [Fact]
    public void AllowContrastSelection_DefaultValue_IsTrue() {
        // Arrange & Act
        var cut = RenderThemeToggle();

        // Assert
        cut.Instance.AllowContrastSelection.Should().BeTrue();
    }

    [Fact]
    public void AllowThemeSelection_DefaultValue_IsTrue() {
        // Arrange & Act
        var cut = RenderThemeToggle();

        // Assert
        cut.Instance.AllowThemeSelection.Should().BeTrue();
    }

    [Fact]
    public void BothAllowed_RendersAllOptions() {
        // Arrange & Act
        var cut = RenderThemeToggle(parameters => parameters
            .Add(p => p.AllowThemeSelection, true)
            .Add(p => p.AllowContrastSelection, true));

        // Assert
        var options = cut.FindAll("select.tnt-theme-select option");
        options.Should().HaveCount(9);

        options.Should().Contain(o => o.GetAttribute("value") == "LIGHT-DEFAULT" && o.TextContent == "Light - Default");
        options.Should().Contain(o => o.GetAttribute("value") == "LIGHT-MEDIUM" && o.TextContent == "Light - Medium");
        options.Should().Contain(o => o.GetAttribute("value") == "LIGHT-HIGH" && o.TextContent == "Light - High");
        options.Should().Contain(o => o.GetAttribute("value") == "DARK-DEFAULT" && o.TextContent == "Dark - Default");
        options.Should().Contain(o => o.GetAttribute("value") == "DARK-MEDIUM" && o.TextContent == "Dark - Medium");
        options.Should().Contain(o => o.GetAttribute("value") == "DARK-HIGH" && o.TextContent == "Dark - High");
        options.Should().Contain(o => o.GetAttribute("value") == "SYSTEM-DEFAULT" && o.TextContent == "System - Default");
        options.Should().Contain(o => o.GetAttribute("value") == "SYSTEM-MEDIUM" && o.TextContent == "System - Medium");
        options.Should().Contain(o => o.GetAttribute("value") == "SYSTEM-HIGH" && o.TextContent == "System - High");
    }

    [Fact]
    public void CompleteThemeToggle_WithAllParameters_RendersCorrectly() {
        // Arrange & Act
        var cut = RenderThemeToggle(parameters => parameters
            .Add(p => p.AllowThemeSelection, true)
            .Add(p => p.AllowContrastSelection, true)
            .Add(p => p.DefaultTheme, Theme.Dark)
            .Add(p => p.Hide, false)
            .Add(p => p.ThemesRoot, "/custom/themes")
            .Add(p => p.LightDefaultCss, "custom-light.css")
            .Add(p => p.DarkDefaultCss, "custom-dark.css")
            .AddUnmatched("data-test", "complete-theme-toggle"));

        // Assert
        var themeToggle = cut.Find("tnt-theme-toggle");
        themeToggle.Should().NotBeNull();
        themeToggle.GetAttribute("tnt-themes-root").Should().Be("/custom/themes");
        themeToggle.GetAttribute("tnt-light-default").Should().Be("custom-light.css");
        themeToggle.GetAttribute("tnt-dark-default").Should().Be("custom-dark.css");
        themeToggle.GetAttribute("data-test").Should().Be("complete-theme-toggle");

        var options = cut.FindAll("select option");
        options.Should().HaveCount(9);

        var icon = cut.Find("span.tnt-theme-toggle-icon");
        icon.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_InitializesCorrectly() {
        // Arrange & Act
        var themeToggle = new TnTThemeToggle();

        // Assert
        themeToggle.Should().NotBeNull();
        themeToggle.AllowThemeSelection.Should().BeTrue();
        themeToggle.AllowContrastSelection.Should().BeTrue();
        themeToggle.DefaultTheme.Should().Be(Theme.System);
        themeToggle.Hide.Should().BeFalse();
        themeToggle.ThemesRoot.Should().Be("/Themes");
        themeToggle.LightDefaultCss.Should().Be("light.css");
        themeToggle.DarkDefaultCss.Should().Be("dark.css");
    }

    [Fact]
    public void ContrastOnlyAllowed_RendersContrastOptions() {
        // Arrange & Act
        var cut = RenderThemeToggle(parameters => parameters
            .Add(p => p.AllowThemeSelection, false)
            .Add(p => p.AllowContrastSelection, true));

        // Assert
        var options = cut.FindAll("select.tnt-theme-select option");
        options.Should().HaveCount(3);

        options.Should().Contain(o => o.GetAttribute("value") == "LIGHT-DEFAULT" && o.TextContent == "Default");
        options.Should().Contain(o => o.GetAttribute("value") == "LIGHT-MEDIUM" && o.TextContent == "Medium");
        options.Should().Contain(o => o.GetAttribute("value") == "LIGHT-HIGH" && o.TextContent == "High");
    }

    [Fact]
    public void CssFileNames_CanBeCustomized() {
        // Arrange & Act
        var cut = RenderThemeToggle(parameters => parameters
            .Add(p => p.LightDefaultCss, "custom-light.css")
            .Add(p => p.DarkDefaultCss, "custom-dark.css")
            .Add(p => p.LightHighCss, "custom-light-hc.css"));

        // Assert
        cut.Instance.LightDefaultCss.Should().Be("custom-light.css");
        cut.Instance.DarkDefaultCss.Should().Be("custom-dark.css");
        cut.Instance.LightHighCss.Should().Be("custom-light-hc.css");

        var element = cut.Find("tnt-theme-toggle");
        element.GetAttribute("tnt-light-default").Should().Be("custom-light.css");
        element.GetAttribute("tnt-dark-default").Should().Be("custom-dark.css");
        element.GetAttribute("tnt-light-high").Should().Be("custom-light-hc.css");
    }

    [Fact]
    public void DefaultTheme_DefaultValue_IsSystem() {
        // Arrange & Act
        var cut = RenderThemeToggle();

        // Assert
        cut.Instance.DefaultTheme.Should().Be(Theme.System);
    }

    [Fact]
    public void ElementClass_ReturnsEmptyString() {
        // Arrange
        var themeToggle = new TnTThemeToggle();

        // Act
        var elementClass = themeToggle.ElementClass;

        // Assert
        elementClass.Should().Be(string.Empty);
    }

    [Fact]
    public void ElementHasCorrectAttributes() {
        // Arrange & Act
        var cut = RenderThemeToggle();

        // Assert
        var element = cut.Find("tnt-theme-toggle");
        element.HasAttribute("data-permanent").Should().BeTrue();
        element.GetAttribute("tnt-themes-root").Should().Be("/Themes");
        element.GetAttribute("tnt-light-default").Should().Be("light.css");
        element.GetAttribute("tnt-dark-default").Should().Be("dark.css");
    }

    [Fact]
    public void ElementStyle_ReturnsEmptyString() {
        // Arrange
        var themeToggle = new TnTThemeToggle();

        // Act
        var elementStyle = themeToggle.ElementStyle;

        // Assert
        elementStyle.Should().Be(string.Empty);
    }

    [Fact]
    public void EmptyThemesRoot_RendersCorrectly() {
        // Arrange & Act
        var cut = RenderThemeToggle(parameters => parameters
            .Add(p => p.ThemesRoot, ""));

        // Assert
        cut.Find("tnt-theme-toggle").GetAttribute("tnt-themes-root").Should().Be("");
    }

    [Fact]
    public void Hide_DefaultValue_IsFalse() {
        // Arrange & Act
        var cut = RenderThemeToggle();

        // Assert
        cut.Instance.Hide.Should().BeFalse();
        cut.Find("tnt-theme-toggle").GetAttribute("style").Should().BeNullOrEmpty();
    }

    [Fact]
    public void Hide_WhenTrue_AddsDisplayNoneStyle() {
        // Arrange & Act
        var cut = RenderThemeToggle(parameters => parameters
            .Add(p => p.Hide, true));

        // Assert
        cut.Instance.Hide.Should().BeTrue();
        cut.Find("tnt-theme-toggle").GetAttribute("style").Should().Contain("display:none");
    }

    [Fact]
    public void Icon_HasCorrectClasses() {
        // Arrange & Act
        var cut = RenderThemeToggle();

        // Assert
        var icon = cut.Find("span.tnt-theme-toggle-icon");
        var classes = icon.GetAttribute("class");
        classes.Should().Contain("tnt-icon");
        classes.Should().Contain("material-symbols-outlined");
        classes.Should().Contain("tnt-theme-toggle-icon");
    }

    [Fact]
    public void JsModulePath_ReturnsCorrectPath() {
        // Arrange
        var themeToggle = new TnTThemeToggle();

        // Act
        var path = themeToggle.JsModulePath;

        // Assert
        path.Should().Be("./_content/NTComponents/Theming/TnTThemeToggle.razor.js?v=2");
    }

    [Fact]
    public void NeitherAllowed_DoesNotRender() {
        // Arrange & Act
        var cut = RenderThemeToggle(parameters => parameters
            .Add(p => p.AllowThemeSelection, false)
            .Add(p => p.AllowContrastSelection, false));

        // Assert
        cut.Markup.Should().BeEmpty();
    }

    [Fact]
    public void NullCssFileNames_RenderAsEmpty() {
        // Arrange & Act
        var cut = RenderThemeToggle(parameters => parameters
            .Add(p => p.LightDefaultCss, null!)
            .Add(p => p.DarkDefaultCss, null!));

        // Assert
        var element = cut.Find("tnt-theme-toggle");
        element.GetAttribute("tnt-light-default").Should().BeNullOrEmpty();
        element.GetAttribute("tnt-dark-default").Should().BeNullOrEmpty();
    }

    [Fact]
    public void OnlyThemeSelection_WithHide_RendersCorrectly() {
        // Arrange & Act
        var cut = RenderThemeToggle(parameters => parameters
            .Add(p => p.AllowThemeSelection, true)
            .Add(p => p.AllowContrastSelection, false)
            .Add(p => p.Hide, true));

        // Assert
        var themeToggle = cut.Find("tnt-theme-toggle");
        themeToggle.GetAttribute("style").Should().Contain("display:none");

        var options = cut.FindAll("select option");
        options.Should().HaveCount(3);
        options.Should().Contain(o => o.TextContent == "Light");
        options.Should().Contain(o => o.TextContent == "Dark");
        options.Should().Contain(o => o.TextContent == "System");
    }

    [Fact]
    public void RendersCorrectElementStructure() {
        // Arrange & Act
        var cut = RenderThemeToggle();

        // Assert
        cut.Find("tnt-theme-toggle").Should().NotBeNull();
        cut.Find("span.tnt-icon.material-symbols-outlined.tnt-theme-toggle-icon").Should().NotBeNull();
        cut.Find("select.tnt-theme-select").Should().NotBeNull();
    }

    [Fact]
    public void Select_HasCorrectClass() {
        // Arrange & Act
        var cut = RenderThemeToggle();

        // Assert
        var select = cut.Find("select.tnt-theme-select");
        select.GetAttribute("class").Should().Be("tnt-theme-select");
    }

    [Fact]
    public void Select_HasCorrectTitle() {
        // Arrange & Act
        var cut = RenderThemeToggle();

        // Assert
        var select = cut.Find("select.tnt-theme-select");
        select.GetAttribute("title").Should().Be("Select Theme and Contrast");
    }

    [Fact]
    public void Select_Has_Accessible_Label() {
        // Arrange & Act
        var cut = RenderThemeToggle();

        // Assert
        var select = cut.Find("select.tnt-theme-select");
        select.GetAttribute("aria-label").Should().Be("Select theme and contrast");
    }

    [Fact]
    public void Theme_Dark_HasCorrectValue() {
        // Assert
        ((int)Theme.Dark).Should().Be(2);
    }

    [Fact]
    public void Theme_Light_HasCorrectValue() {
        // Assert
        ((int)Theme.Light).Should().Be(1);
    }

    [Fact]
    public void Theme_System_HasCorrectValue() {
        // Assert
        ((int)Theme.System).Should().Be(0);
    }

    [Fact]
    public void ThemeOnlyAllowed_RendersThemeOptions() {
        // Arrange & Act
        var cut = RenderThemeToggle(parameters => parameters
            .Add(p => p.AllowThemeSelection, true)
            .Add(p => p.AllowContrastSelection, false));

        // Assert
        var options = cut.FindAll("select.tnt-theme-select option");
        options.Should().HaveCount(3);

        options.Should().Contain(o => o.GetAttribute("value") == "LIGHT-DEFAULT" && o.TextContent == "Light");
        options.Should().Contain(o => o.GetAttribute("value") == "DARK-DEFAULT" && o.TextContent == "Dark");
        options.Should().Contain(o => o.GetAttribute("value") == "SYSTEM-DEFAULT" && o.TextContent == "System");
    }

    [Fact]
    public void ThemesRoot_CanBeCustomized() {
        // Arrange & Act
        var cut = RenderThemeToggle(parameters => parameters
            .Add(p => p.ThemesRoot, "/custom/themes"));

        // Assert
        cut.Instance.ThemesRoot.Should().Be("/custom/themes");
        cut.Find("tnt-theme-toggle").GetAttribute("tnt-themes-root").Should().Be("/custom/themes");
    }

    private IRenderedComponent<TnTThemeToggle> RenderThemeToggle(
        Action<ComponentParameterCollectionBuilder<TnTThemeToggle>>? parameterBuilder = null) {
        return Render<TnTThemeToggle>(parameters => {
            parameterBuilder?.Invoke(parameters);
        });
    }
}
