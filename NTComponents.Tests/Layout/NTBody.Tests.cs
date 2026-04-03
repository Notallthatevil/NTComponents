using Microsoft.Extensions.DependencyInjection;
using NTComponents.Core;

namespace NTComponents.Tests.Layout;

public class NTBody_Tests : BunitContext {
    [Fact]
    public void Renders_Default_Main_Tag_With_Default_Colors_And_Scrollable_Class() {
        var cut = Render<NTBody>(p => p.AddChildContent("Body"));

        var root = cut.Find("main.nt-layout-body");

        root.GetAttribute("data-nt-scrollable")!.Should().Be("true");
        root.GetAttribute("class")!.Should().Contain("nt-layout-body-scrollable");
        root.GetAttribute("style")!.Should().Contain("--nt-layout-body-background-color:var(--tnt-color-background)");
        root.GetAttribute("style")!.Should().Contain("--nt-layout-body-text-color:var(--tnt-color-on-background)");
    }

    [Fact]
    public void TagName_Can_Render_Article() {
        var cut = Render<NTBody>(p => p
            .Add(c => c.TagName, "article")
            .AddChildContent("Article"));

        cut.Find("article.nt-layout-body").Should().NotBeNull();
    }

    [Fact]
    public void Explicit_Parameters_Are_Used() {
        var cut = Render<NTBody>(p => p
            .Add(c => c.BackgroundColor, TnTColor.Surface)
            .Add(c => c.TextColor, TnTColor.OnSurface)
            .Add(c => c.Scrollable, false)
            .AddChildContent("Body"));

        var root = cut.Find(".nt-layout-body");
        root.GetAttribute("data-nt-scrollable")!.Should().Be("false");
        root.GetAttribute("class")!.Should().NotContain("nt-layout-body-scrollable");
        root.GetAttribute("style")!.Should().Contain("--nt-layout-body-background-color:var(--tnt-color-surface)");
        root.GetAttribute("style")!.Should().Contain("--nt-layout-body-text-color:var(--tnt-color-on-surface)");
    }

    [Fact]
    public void Explicit_Parameters_Override_Defaults() {
        using var context = new BunitContext();
        context.Services.AddSingleton(new NTComponentsDefaultOptions {
            Layout = new NTLayoutDefaults {
                BodyBackgroundColor = TnTColor.PrimaryContainer,
                BodyTextColor = TnTColor.OnPrimaryContainer,
                BodyScrollable = true
            }
        });

        var cut = context.Render<NTBody>(p => p
            .Add(c => c.BackgroundColor, TnTColor.WarningContainer)
            .Add(c => c.Scrollable, false)
            .AddChildContent("Body"));

        var root = cut.Find(".nt-layout-body");
        root.GetAttribute("data-nt-scrollable")!.Should().Be("false");
        root.GetAttribute("style")!.Should().Contain("--nt-layout-body-background-color:var(--tnt-color-warning-container)");
        root.GetAttribute("style")!.Should().Contain("--nt-layout-body-text-color:var(--tnt-color-on-primary-container)");
    }
}
