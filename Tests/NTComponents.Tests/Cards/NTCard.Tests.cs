using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace NTComponents.Tests.Cards;

public class NTCard_Tests : BunitContext {

    [Fact]
    public void AdditionalAttributes_Class_Merged() {
        var attrs = new Dictionary<string, object> { { "class", "extra" } };

        var cut = Render<NTCard>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var cls = cut.Find(".nt-card").GetAttribute("class")!;

        cls.Should().Contain("extra");
        cls.Should().Contain("nt-card");
    }

    [Fact]
    public void AdditionalAttributes_Style_Merged() {
        var attrs = new Dictionary<string, object> { { "style", "margin:3px" } };

        var cut = Render<NTCard>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var style = cut.Find(".nt-card").GetAttribute("style")!;

        style.Should().Contain("margin:3px");
        style.Should().Contain("--nt-card-background-color");
    }

    [Fact]
    public void Default_Render_Is_Filled_Div() {
        var cut = Render<NTCard>(p => p.AddChildContent("Content"));

        var card = cut.Find("div.nt-card");
        var cls = card.GetAttribute("class")!;
        var style = card.GetAttribute("style")!;

        cls.Should().Contain("nt-card-filled");
        cls.Should().Contain("nt-corner-radius-medium");
        cls.Should().NotContain("nt-card-actionable");
        style.Should().Contain("--nt-card-background-color:var(--tnt-color-surface-container-highest)");
        style.Should().Contain("--nt-card-content-color:var(--tnt-color-on-surface)");
        style.Should().Contain("--nt-card-state-layer-color:var(--tnt-color-on-surface)");
        cut.Markup.Should().Contain("Content");
    }

    [Fact]
    public void CornerRadius_Adds_CornerRadius_Class() {
        var cut = Render<NTCard>(p => p.Add(c => c.CornerRadius, NTCornerRadius.Full));

        cut.Find("div.nt-card").GetAttribute("class")!.Should().Contain("nt-corner-radius-full");
    }

    [Fact]
    public void Outlined_Variant_Adds_Outlined_Class_And_Default_Outline_Color() {
        var cut = Render<NTCard>(p => p.Add(c => c.Variant, NTCardVariant.Outlined));

        var card = cut.Find("div.nt-card");
        card.GetAttribute("class")!.Should().Contain("nt-card-outlined");
        card.GetAttribute("style")!.Should().Contain("--nt-card-background-color:var(--tnt-color-transparent)");
        card.GetAttribute("style")!.Should().Contain("--nt-card-outline-color:var(--tnt-color-outline-variant)");
        card.GetAttribute("style")!.Should().NotContain("--nt-card-content-color");
    }

    [Fact]
    public void Elevated_Variant_Adds_Elevation_Class() {
        var cut = Render<NTCard>(p => p
            .Add(c => c.Variant, NTCardVariant.Elevated)
            .Add(c => c.Elevation, NTElevation.Low));

        cut.Find("div.nt-card").GetAttribute("class")!.Should().Contain("nt-elevation-low");
        cut.Find("div.nt-card").GetAttribute("style")!.Should().Contain("--nt-card-background-color:var(--tnt-color-surface-container-low)");
    }

    [Fact]
    public void Filled_Variant_Does_Not_Emit_Elevation_Class() {
        var cut = Render<NTCard>(p => p
            .Add(c => c.Variant, NTCardVariant.Filled)
            .Add(c => c.Elevation, NTElevation.Highest));

        cut.Find("div.nt-card").GetAttribute("class")!.Should().NotContain("nt-elevation-");
    }

    [Fact]
    public void OnClickCallback_Renders_Button() {
        var cut = Render<NTCard>(p => p
            .Add(c => c.OnClickCallback, EventCallback.Factory.Create<MouseEventArgs>(this, _ => Task.CompletedTask))
            .AddChildContent("Action"));

        var button = cut.Find("button.nt-card");
        button.GetAttribute("type")!.Should().Be("button");
        button.GetAttribute("class")!.Should().Contain("nt-card-actionable");
    }

    [Fact]
    public void Href_Attribute_Renders_Anchor() {
        var cut = Render<NTCard>(p => p
            .Add(c => c.AdditionalAttributes, new Dictionary<string, object> { { "href", "/details" } })
            .AddChildContent("Go"));

        var anchor = cut.Find("a.nt-card");
        anchor.GetAttribute("href")!.Should().Be("/details");
        anchor.GetAttribute("class")!.Should().Contain("nt-card-link");
    }

    [Fact]
    public void OnClickCallback_Takes_Precedence_Over_Href_Attribute() {
        var cut = Render<NTCard>(p => p
            .Add(c => c.AdditionalAttributes, new Dictionary<string, object> { { "href", "/details" } })
            .Add(c => c.OnClickCallback, EventCallback.Factory.Create<MouseEventArgs>(this, _ => Task.CompletedTask)));

        var button = cut.Find("button.nt-card");
        button.HasAttribute("href").Should().BeFalse();
        button.GetAttribute("class")!.Should().Contain("nt-card-button");
    }

    [Fact]
    public void Anchor_Retains_Link_Attributes() {
        var cut = Render<NTCard>(p => p
            .Add(c => c.AdditionalAttributes, new Dictionary<string, object> {
                { "href", "/details" },
                { "target", "_blank" },
                { "rel", "noopener" }
            }));

        var anchor = cut.Find("a.nt-card");
        anchor.GetAttribute("target")!.Should().Be("_blank");
        anchor.GetAttribute("rel")!.Should().Be("noopener");
    }

    [Fact]
    public void Disabled_Anchor_Removes_Href_And_Becomes_AriaDisabled() {
        var cut = Render<NTCard>(p => p
            .Add(c => c.AdditionalAttributes, new Dictionary<string, object> {
                { "href", "/details" },
                { "disabled", true }
            })
            .AddChildContent("Disabled"));

        var anchor = cut.Find("a.nt-card");
        anchor.HasAttribute("href").Should().BeFalse();
        anchor.GetAttribute("aria-disabled")!.Should().Be("true");
        anchor.GetAttribute("tabindex")!.Should().Be("-1");
        anchor.GetAttribute("class")!.Should().Contain("nt-card-disabled");
    }

    [Fact]
    public void Custom_Colors_Appear_In_Style() {
        var cut = Render<NTCard>(p => p
            .Add(c => c.BackgroundColor, TnTColor.SuccessContainer)
            .Add(c => c.TextColor, TnTColor.OnSuccessContainer)
            .Add(c => c.OutlineColor, TnTColor.Outline));

        var style = cut.Find(".nt-card").GetAttribute("style")!;
        style.Should().Contain("--nt-card-background-color:var(--tnt-color-success-container)");
        style.Should().Contain("--nt-card-content-color:var(--tnt-color-on-success-container)");
        style.Should().Contain("--nt-card-outline-color:var(--tnt-color-outline)");
        style.Should().Contain("--nt-card-state-layer-color:var(--tnt-color-on-success-container)");
    }

    [Fact]
    public void Filled_Transparent_Background_Falls_Back_To_Default_Surface() {
        var cut = Render<NTCard>(p => p.Add(c => c.BackgroundColor, TnTColor.Transparent));

        cut.Find(".nt-card").GetAttribute("style")!.Should().Contain("--nt-card-background-color:var(--tnt-color-surface-container-highest)");
    }

    [Fact]
    public void Elevated_Transparent_Background_Falls_Back_To_Elevated_Default() {
        var cut = Render<NTCard>(p => p
            .Add(c => c.Variant, NTCardVariant.Elevated)
            .Add(c => c.BackgroundColor, TnTColor.Transparent));

        cut.Find(".nt-card").GetAttribute("style")!.Should().Contain("--nt-card-background-color:var(--tnt-color-surface-container-low)");
    }

    [Fact]
    public void Outlined_Allows_Transparent_Background_Override() {
        var cut = Render<NTCard>(p => p
            .Add(c => c.Variant, NTCardVariant.Outlined)
            .Add(c => c.BackgroundColor, TnTColor.Transparent));

        cut.Find(".nt-card").GetAttribute("style")!.Should().Contain("--nt-card-background-color:var(--tnt-color-transparent)");
    }
}
