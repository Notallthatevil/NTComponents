namespace NTComponents.Tests.Core;

public class NTHeadDependencies_Tests : BunitContext {

    [Fact]
    public void Render_Includes_MaterialSymbolsSharp_With_Fill_Axis() {
        var cut = Render<NTHeadDependencies>();

        cut.Markup.Should().Contain("https://fonts.googleapis.com/css2?family=Material+Symbols+Sharp:opsz,wght,FILL,GRAD@20..48,100..700,0..1,-50..200");
    }

    [Fact]
    public void Render_EmitsThemeRuntimeConfigBeforeBootstrap() {
        var cut = Render<NTHeadDependencies>();
        var markup = cut.Markup;

        markup.Should().Contain("data-tnt-theme-critical");
        markup.Should().Contain("data-nt-theme-critical");
        markup.Should().Contain("data-permanent");
        markup.Should().Contain("data-nt-theme-default");
        markup.Should().Contain("id=\"nt-theme-config\"");
        markup.Should().Contain("id=\"nt-theme-state\"");
        markup.Should().Contain("_content/NTComponents/NTTheme.runtime.js");
        markup.Should().Contain("_content/NTComponents/theme-bootstrap.js");
        markup.Should().Contain("<style class=\"tnt-measurements\">:root{--tnt-header-height:64px;--tnt-footer-height:64px;--tnt-side-nav-width:256px;}</style>");
        markup.Should().Contain("_content/NTComponents/nt-measurements.css");

        markup.IndexOf("data-nt-theme-default", StringComparison.Ordinal).Should().BeLessThan(markup.IndexOf("id=\"nt-theme-config\"", StringComparison.Ordinal));
        markup.IndexOf("id=\"nt-theme-config\"", StringComparison.Ordinal).Should().BeLessThan(markup.IndexOf("_content/NTComponents/NTTheme.runtime.js", StringComparison.Ordinal));
        markup.IndexOf("id=\"nt-theme-state\"", StringComparison.Ordinal).Should().BeLessThan(markup.IndexOf("_content/NTComponents/NTTheme.runtime.js", StringComparison.Ordinal));
        markup.IndexOf("_content/NTComponents/NTTheme.runtime.js", StringComparison.Ordinal).Should().BeLessThan(markup.IndexOf("_content/NTComponents/theme-bootstrap.js", StringComparison.Ordinal));
        markup.IndexOf("_content/NTComponents/theme-bootstrap.js", StringComparison.Ordinal).Should().BeLessThan(markup.IndexOf("class=\"tnt-measurements\"", StringComparison.Ordinal));
        markup.IndexOf("class=\"tnt-measurements\"", StringComparison.Ordinal).Should().BeLessThan(markup.IndexOf("_content/NTComponents/nt-measurements.css", StringComparison.Ordinal));
        markup.IndexOf("_content/NTComponents/nt-measurements.css", StringComparison.Ordinal).Should().BeLessThan(markup.IndexOf("_content/NTComponents/nt-ripple.css", StringComparison.Ordinal));
    }

    [Fact]
    public void Render_MarksThemeHeadItemsAsPermanent() {
        var cut = Render<NTHeadDependencies>();
        var markup = cut.Markup;

        markup.Should().Contain("data-tnt-theme-critical=\"true\" data-nt-theme-critical=\"true\" data-permanent");
        cut.FindAll("link[data-nt-theme-default]").Should().OnlyContain(link => link.HasAttribute("data-permanent"));
        markup.Should().Contain("id=\"nt-theme-config\" data-permanent");
        markup.Should().Contain("id=\"nt-theme-state\" data-permanent");
        markup.Should().Contain("_content/NTComponents/NTTheme.runtime.js\" data-permanent");
        markup.Should().Contain("_content/NTComponents/theme-bootstrap.js\" data-permanent");
        markup.Should().Contain("_content/NTComponents/nt-measurements.css\" data-permanent");
    }

    [Fact]
    public void Render_DefaultSystemTheme_EmitsMediaQualifiedFirstPaintThemeLinks() {
        var cut = Render<NTHeadDependencies>();
        var defaultLinks = cut.FindAll("link[data-nt-theme-default]");

        defaultLinks.Should().HaveCount(2);
        defaultLinks[0].GetAttribute("rel").Should().Be("stylesheet");
        defaultLinks[0].GetAttribute("href").Should().Be("/Themes/light.css");
        defaultLinks[0].GetAttribute("media").Should().Be("(prefers-color-scheme: light)");
        defaultLinks[1].GetAttribute("rel").Should().Be("stylesheet");
        defaultLinks[1].GetAttribute("href").Should().Be("/Themes/dark.css");
        defaultLinks[1].GetAttribute("media").Should().Be("(prefers-color-scheme: dark)");
    }

    [Fact]
    public void Render_CustomThemeConfiguration_IsSerialized() {
        var cut = Render<NTHeadDependencies>(parameters => parameters
            .Add(p => p.DefaultTheme, NTTheme.Dark)
            .Add(p => p.DefaultContrast, NTThemeContrast.High)
            .Add(p => p.ThemesRoot, "/brand/themes")
            .Add(p => p.LightDefaultCss, "brand-light.css")
            .Add(p => p.LightMediumCss, "brand-light-mc.css")
            .Add(p => p.LightHighCss, "brand-light-hc.css")
            .Add(p => p.DarkDefaultCss, "brand-dark.css")
            .Add(p => p.DarkMediumCss, "brand-dark-mc.css")
            .Add(p => p.DarkHighCss, "brand-dark-hc.css"));

        cut.Markup.Should().Contain("\"defaultTheme\":\"DARK\"");
        cut.Markup.Should().Contain("\"defaultContrast\":\"HIGH\"");
        cut.Markup.Should().Contain("\"themesRoot\":\"/brand/themes\"");
        cut.Markup.Should().Contain("\"lightDefaultCss\":\"brand-light.css\"");
        cut.Markup.Should().Contain("\"darkHighCss\":\"brand-dark-hc.css\"");
    }

    [Fact]
    public void Render_ExplicitDefaultTheme_EmitsSingleFirstPaintThemeLink() {
        var cut = Render<NTHeadDependencies>(parameters => parameters
            .Add(p => p.DefaultTheme, NTTheme.Dark)
            .Add(p => p.DefaultContrast, NTThemeContrast.High)
            .Add(p => p.ThemesRoot, "/brand/themes")
            .Add(p => p.DarkHighCss, "brand-dark-hc.css"));
        var defaultLinks = cut.FindAll("link[data-nt-theme-default]");

        defaultLinks.Should().HaveCount(1);
        defaultLinks[0].GetAttribute("href").Should().Be("/brand/themes/brand-dark-hc.css");
        defaultLinks[0].HasAttribute("media").Should().BeFalse();
    }

    [Fact]
    public void Render_CustomMeasurementConfiguration_IsEmittedInline() {
        var cut = Render<NTHeadDependencies>(parameters => parameters
            .Add(p => p.TokenScopeSelector, ".app-shell")
            .Add(p => p.HeaderHeight, 72.5)
            .Add(p => p.FooterHeight, 48)
            .Add(p => p.SideNavWidth, 300));

        cut.Markup.Should().Contain("<style class=\"tnt-measurements\">.app-shell{--tnt-header-height:72.5px;--tnt-footer-height:48px;--tnt-side-nav-width:300px;}</style>");
    }

    [Fact]
    public void TnTHeadDependencies_RendersThrough_NTHeadDependencies() {
#pragma warning disable CS0618
        var cut = Render<TnTHeadDependencies>();
#pragma warning restore CS0618

        cut.Markup.Should().Contain("id=\"nt-theme-config\"");
        cut.Markup.Should().Contain("<style class=\"tnt-measurements\">:root{--tnt-header-height:64px;--tnt-footer-height:64px;--tnt-side-nav-width:256px;}</style>");
    }
}
