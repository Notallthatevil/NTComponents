using Microsoft.Extensions.DependencyInjection;
using NTComponents.Core;

namespace NTComponents.Tests.Layout;

public class NTHeader_Tests : BunitContext {
    [Fact]
    public void Renders_Default_Header_Tag_With_Default_Colors() {
        var cut = Render<NTHeader>(p => p.AddChildContent("Header"));

        var root = cut.Find("header.nt-layout-header");

        root.GetAttribute("data-nt-sticky")!.Should().Be("false");
        root.GetAttribute("style")!.Should().Contain("--nt-layout-header-background-color:var(--tnt-color-surface)");
        root.GetAttribute("style")!.Should().Contain("--nt-layout-header-text-color:var(--tnt-color-on-surface)");
    }

    [Fact]
    public void Sticky_Header_Adds_Class_And_Can_Render_Section() {
        var cut = Render<NTHeader>(p => p
            .Add(c => c.TagName, "section")
            .Add(c => c.Sticky, true)
            .AddChildContent("Header"));

        var root = cut.Find("section.nt-layout-header");
        root.GetAttribute("data-nt-sticky")!.Should().Be("true");
        root.GetAttribute("class")!.Should().Contain("nt-layout-header-sticky");
    }

    [Fact]
    public void Registered_Defaults_Are_Used_When_No_Explicit_Values_Are_Set() {
        using var context = new BunitContext();
        context.Services.AddSingleton(new NTComponentsDefaultOptions {
            Layout = new NTLayoutDefaults {
                HeaderTagName = "section",
                HeaderBackgroundColor = TnTColor.PrimaryContainer,
                HeaderTextColor = TnTColor.OnPrimaryContainer,
                HeaderSticky = true
            }
        });

        var cut = context.Render<NTHeader>(p => p.AddChildContent("Header"));
        var root = cut.Find("section.nt-layout-header");

        root.GetAttribute("data-nt-sticky")!.Should().Be("true");
        root.GetAttribute("style")!.Should().Contain("--nt-layout-header-background-color:var(--tnt-color-primary-container)");
        root.GetAttribute("style")!.Should().Contain("--nt-layout-header-text-color:var(--tnt-color-on-primary-container)");
    }
}
