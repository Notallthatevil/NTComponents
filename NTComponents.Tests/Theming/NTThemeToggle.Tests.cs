namespace NTComponents.Tests.Theming;

public class NTThemeToggle_Tests : BunitContext {

    public NTThemeToggle_Tests() {
        var themeToggleModule = JSInterop.SetupModule("./_content/NTComponents/Theming/NTThemeToggle.razor.js?v=1");
        themeToggleModule.SetupVoid("onLoad", _ => true);
        themeToggleModule.SetupVoid("onUpdate", _ => true);
        themeToggleModule.SetupVoid("onDispose", _ => true);
    }

    [Fact]
    public void RendersIndependentNtThemeToggleElement() {
        var cut = Render<NTThemeToggle>();

        cut.Find("nt-theme-toggle").Should().NotBeNull();
        cut.FindAll("tnt-theme-toggle").Should().BeEmpty();
        cut.Markup.Should().NotContain("<TnTThemeToggle");
    }

    [Fact]
    public void CustomThemeConfiguration_RendersNtAttributes() {
        var cut = Render<NTThemeToggle>(parameters => parameters
            .Add(p => p.DefaultTheme, NTTheme.Dark)
            .Add(p => p.DefaultContrast, NTThemeContrast.High)
            .Add(p => p.ThemesRoot, "/brand/themes")
            .Add(p => p.LightDefaultCss, "brand-light.css")
            .Add(p => p.LightMediumCss, "brand-light-mc.css")
            .Add(p => p.LightHighCss, "brand-light-hc.css")
            .Add(p => p.DarkDefaultCss, "brand-dark.css")
            .Add(p => p.DarkMediumCss, "brand-dark-mc.css")
            .Add(p => p.DarkHighCss, "brand-dark-hc.css"));

        var element = cut.Find("nt-theme-toggle");
        element.GetAttribute("nt-default-theme").Should().Be("DARK");
        element.GetAttribute("nt-default-contrast").Should().Be("HIGH");
        element.GetAttribute("nt-themes-root").Should().Be("/brand/themes");
        element.GetAttribute("nt-light-default").Should().Be("brand-light.css");
        element.GetAttribute("nt-light-medium").Should().Be("brand-light-mc.css");
        element.GetAttribute("nt-light-high").Should().Be("brand-light-hc.css");
        element.GetAttribute("nt-dark-default").Should().Be("brand-dark.css");
        element.GetAttribute("nt-dark-medium").Should().Be("brand-dark-mc.css");
        element.GetAttribute("nt-dark-high").Should().Be("brand-dark-hc.css");
    }

    [Fact]
    public void UsesNtThemeToggleModulePath() {
        var themeToggle = new NTThemeToggle();

        themeToggle.JsModulePath.Should().Be("./_content/NTComponents/Theming/NTThemeToggle.razor.js?v=1");
    }
}
