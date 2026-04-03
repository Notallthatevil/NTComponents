using Microsoft.Extensions.DependencyInjection;
using NTComponents.Core;

namespace NTComponents.Tests.Layout;

public class NTLayout_Tests : BunitContext {
    [Fact]
    public void Renders_Default_Layout_With_Div_Tag_And_Default_Colors() {
        var cut = Render<NTLayout>(p => p.AddChildContent("Layout"));

        var root = cut.Find("div.nt-layout");

        root.GetAttribute("data-nt-mode")!.Should().Be("root");
        root.GetAttribute("data-nt-fill-viewport")!.Should().Be("false");
        root.GetAttribute("class")!.Should().Contain("nt-layout-root");
        root.GetAttribute("style")!.Should().Contain("--nt-layout-background-color:var(--tnt-color-background)");
        root.GetAttribute("style")!.Should().Contain("--nt-layout-text-color:var(--tnt-color-on-background)");
        cut.Markup.Should().Contain("Layout");
    }

    [Fact]
    public void TagName_Can_Render_Section() {
        var cut = Render<NTLayout>(p => p
            .Add(c => c.TagName, "section")
            .AddChildContent("Layout"));

        cut.Find("section.nt-layout").Should().NotBeNull();
    }

    [Fact]
    public void Registered_Defaults_Are_Used_When_Parameters_Are_Not_Set() {
        using var context = new BunitContext();
        context.Services.AddSingleton(new NTComponentsDefaultOptions {
            Layout = new NTLayoutDefaults {
                LayoutTagName = "section",
                Mode = NTLayoutMode.Nested,
                FillViewport = true,
                LayoutBackgroundColor = TnTColor.SurfaceContainer,
                LayoutTextColor = TnTColor.OnSurface
            }
        });

        var cut = context.Render<NTLayout>(p => p.AddChildContent("Layout"));
        var root = cut.Find("section.nt-layout");

        root.GetAttribute("data-nt-mode")!.Should().Be("nested");
        root.GetAttribute("data-nt-fill-viewport")!.Should().Be("true");
        root.GetAttribute("class")!.Should().Contain("nt-layout-nested");
        root.GetAttribute("class")!.Should().Contain("nt-layout-fill-viewport");
        root.GetAttribute("style")!.Should().Contain("--nt-layout-background-color:var(--tnt-color-surface-container)");
        root.GetAttribute("style")!.Should().Contain("--nt-layout-text-color:var(--tnt-color-on-surface)");
    }

    [Fact]
    public void Explicit_Parameters_Override_Defaults() {
        using var context = new BunitContext();
        context.Services.AddSingleton(new NTComponentsDefaultOptions {
            Layout = new NTLayoutDefaults {
                FillViewport = true,
                LayoutBackgroundColor = TnTColor.Secondary
            }
        });

        var cut = context.Render<NTLayout>(p => p
            .Add(c => c.FillViewport, false)
            .Add(c => c.BackgroundColor, TnTColor.Tertiary)
            .AddChildContent("Layout"));

        var root = cut.Find(".nt-layout");
        root.GetAttribute("data-nt-fill-viewport")!.Should().Be("false");
        root.GetAttribute("class")!.Should().NotContain("nt-layout-fill-viewport");
        root.GetAttribute("style")!.Should().Contain("--nt-layout-background-color:var(--tnt-color-tertiary)");
    }
}
