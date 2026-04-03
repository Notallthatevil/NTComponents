using Microsoft.Extensions.DependencyInjection;
using NTComponents.Core;

namespace NTComponents.Tests.Layout;

public class NTFooter_Tests : BunitContext {
    [Fact]
    public void Renders_Default_Footer_Tag_With_Default_Colors_And_Elevation() {
        var cut = Render<NTFooter>(p => p.AddChildContent("Footer"));

        var root = cut.Find("footer.nt-layout-footer");

        root.GetAttribute("data-nt-sticky")!.Should().Be("false");
        root.GetAttribute("class")!.Should().Contain("tnt-elevation-2");
        root.GetAttribute("style")!.Should().Contain("--nt-layout-footer-background-color:var(--tnt-color-surface-container-low)");
        root.GetAttribute("style")!.Should().Contain("--nt-layout-footer-text-color:var(--tnt-color-on-surface-variant)");
    }

    [Fact]
    public void Explicit_Parameters_Are_Used() {
        var cut = Render<NTFooter>(p => p
            .Add(c => c.Sticky, true)
            .Add(c => c.Elevation, 4)
            .Add(c => c.BackgroundColor, TnTColor.SecondaryContainer)
            .AddChildContent("Footer"));

        var root = cut.Find(".nt-layout-footer");
        root.GetAttribute("data-nt-sticky")!.Should().Be("true");
        root.GetAttribute("class")!.Should().Contain("tnt-elevation-4");
        root.GetAttribute("class")!.Should().Contain("nt-layout-footer-sticky");
        root.GetAttribute("style")!.Should().Contain("--nt-layout-footer-background-color:var(--tnt-color-secondary-container)");
    }

    [Fact]
    public void Registered_Defaults_Are_Used_When_No_Explicit_Values_Are_Set() {
        using var context = new BunitContext();
        context.Services.AddSingleton(new NTComponentsDefaultOptions {
            Layout = new NTLayoutDefaults {
                FooterTagName = "section",
                FooterBackgroundColor = TnTColor.SecondaryContainer,
                FooterTextColor = TnTColor.OnSecondaryContainer,
                FooterElevation = 5,
                FooterSticky = true
            }
        });

        var cut = context.Render<NTFooter>(p => p.AddChildContent("Footer"));
        var root = cut.Find("section.nt-layout-footer");

        root.GetAttribute("data-nt-sticky")!.Should().Be("true");
        root.GetAttribute("class")!.Should().Contain("tnt-elevation-5");
        root.GetAttribute("style")!.Should().Contain("--nt-layout-footer-background-color:var(--tnt-color-secondary-container)");
        root.GetAttribute("style")!.Should().Contain("--nt-layout-footer-text-color:var(--tnt-color-on-secondary-container)");
    }
}
