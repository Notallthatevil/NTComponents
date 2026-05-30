using System.Collections.Generic;
using Bunit;
using Xunit;

#pragma warning disable CS0618

namespace NTComponents.Tests.Anchor;

public class TnTNavLink_Tests : BunitContext {
    private const string RippleJsModule = "./_content/NTComponents/Core/TnTRippleEffect.razor.js";

    public TnTNavLink_Tests() {
        var module = JSInterop.SetupModule(RippleJsModule);
        module.SetupVoid("onLoad", _ => true).SetVoidResult();
        module.SetupVoid("onUpdate", _ => true).SetVoidResult();
        module.SetupVoid("onDispose", _ => true).SetVoidResult();
    }

    [Fact]
    public void ActiveColors_AddsActiveClassesAndVariables() {
        // Arrange/Act
        var cut = Render<TnTNavLink>(p => p.Add(x => x.ActiveBackgroundColor, TnTColor.Primary).Add(x => x.ActiveTextColor, TnTColor.OnPrimary).AddChildContent("Active"));
        // Assert
        cut.Markup.Should().Contain("active-bg-color");
        cut.Markup.Should().Contain("active-fg-color");
        cut.Markup.Should().Contain("--tnt-anchor-active-bg-color:var(--tnt-color-primary);");
        cut.Markup.Should().Contain("--tnt-anchor-active-fg-color:var(--tnt-color-on-primary);");
    }

    [Fact]
    public void AdditionalAttributes_AreRendered() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "data-test", "foo" } };
        // Act
        var cut = Render<TnTNavLink>(p => p.Add(x => x.AdditionalAttributes, attrs).AddChildContent("Attrs"));
        // Assert
        cut.Markup.Should().Contain("data-test=\"foo\"");
    }

    [Fact]
    public void AnchorSize_Medium_DoesNotRenderSmallLayerSpan() {
        // Act
        var cut = Render<TnTNavLink>(p => p.Add(x => x.AnchorSize, Size.Medium).AddChildContent("Medium"));
        // Assert
        cut.Markup.Should().NotContain("tnt-small-anchor-layer");
    }

    [Fact]
    public void AnchorSize_SmallAndXS_RenderSmallLayerSpan() {
        // Act
        var cutSmall = Render<TnTNavLink>(p => p.AddChildContent("Small"));
        var cutXS = Render<TnTNavLink>(p => p.Add(x => x.AnchorSize, Size.XS).AddChildContent("XS"));
        // Assert
        cutSmall.Markup.Should().Contain("tnt-small-anchor-layer");
        cutXS.Markup.Should().Contain("tnt-small-anchor-layer");
    }

    [Fact]
    public void Appearance_Filled_HasExpectedClasses() {
        // Act
        var cut = Render<TnTNavLink>(p => p.Add(x => x.Appearance, AnchorAppearance.Filled).AddChildContent("Filled"));
        // Assert
        cut.Markup.Should().Contain("tnt-filled");
        cut.Markup.Should().Contain("tnt-interactable");
        cut.Markup.Should().Contain("tnt-anchor-tint-color");
        cut.Markup.Should().NotContain("tnt-underlined");
    }

    [Fact]
    public void Appearance_None_DoesNotHaveSpecialAppearanceClasses() {
        // Act
        var cut = Render<TnTNavLink>(p => p.Add(x => x.Appearance, AnchorAppearance.None).AddChildContent("None"));
        // Assert
        cut.Markup.Should().NotContain("tnt-filled");
        cut.Markup.Should().NotContain("tnt-outlined");
        cut.Markup.Should().NotContain("tnt-underlined");
    }

    [Fact]
    public void Appearance_Outlined_HasExpectedClasses() {
        // Act
        var cut = Render<TnTNavLink>(p => p.Add(x => x.Appearance, AnchorAppearance.Outlined).AddChildContent("Outlined"));
        // Assert
        cut.Markup.Should().Contain("tnt-outlined");
        cut.Markup.Should().Contain("tnt-interactable");
        cut.Markup.Should().NotContain("tnt-underlined");
    }

    [Fact]
    public void Appearance_Underlined_WithLargeSize_HasCorrectClasses() {
        // Act
        var cut = Render<TnTNavLink>(p => p.Add(x => x.Appearance, AnchorAppearance.Underlined).Add(x => x.AnchorSize, Size.Large).AddChildContent("UnderlinedLarge"));
        // Assert
        cut.Markup.Should().Contain("tnt-underlined");
        cut.Markup.Should().Contain("tnt-size-l");
    }

    [Fact]
    public void CustomAttributes_AreRendered() {
        // Act
        var cutId = Render<TnTNavLink>(p => p.Add(x => x.ElementId, "myid").AddChildContent("Meta"));
        var cutLang = Render<TnTNavLink>(p => p.Add(x => x.ElementLang, "fr").AddChildContent("Meta"));
        var cutTitle = Render<TnTNavLink>(p => p.Add(x => x.ElementTitle, "MyTitle").AddChildContent("Meta"));
        var cutAutoFocus = Render<TnTNavLink>(p => p.Add(x => x.AutoFocus, true).AddChildContent("Meta"));
        // Assert
        cutId.Markup.Should().Contain("id=\"myid\"");
        cutLang.Markup.Should().Contain("lang=\"fr\"");
        cutTitle.Markup.Should().Contain("title=\"MyTitle\"");
        cutAutoFocus.Markup.Should().Contain("autofocus");
    }

    [Fact]
    public void CustomColors_AreRendered() {
        // Act
        var cutText = Render<TnTNavLink>(p => p.Add(x => x.TextColor, TnTColor.OnPrimary).AddChildContent("Colors"));
        var cutBg = Render<TnTNavLink>(p => p.Add(x => x.BackgroundColor, TnTColor.Secondary).AddChildContent("Colors"));
        var cutOnTint = Render<TnTNavLink>(p => p.Add(x => x.OnTintColor, TnTColor.OnSecondary).AddChildContent("Colors"));
        // Assert
        cutText.Markup.Should().Contain("--tnt-anchor-fg-color:var(--tnt-color-on-primary);");
        cutBg.Markup.Should().Contain("--tnt-anchor-bg-color:var(--tnt-color-secondary);");
        cutOnTint.Markup.Should().Contain("--tnt-anchor-on-tint-color:var(--tnt-color-on-secondary);");
        cutOnTint.Markup.Should().Contain("tnt-anchor-on-tint-color");
    }

    [Fact]
    public void Default_Render_HasExpectedClasses() {
        // Act
        var cut = Render<TnTNavLink>(p => p.AddChildContent("Link"));
        // Assert
        cut.Markup.Should().Contain("tnt-nav-link");
        cut.Markup.Should().Contain("tnt-underlined");
        cut.Markup.Should().Contain("tnt-size-s");
        cut.Markup.Should().Contain("tnt-ripple");
    }

    [Fact]
    public void Disabled_RemovesHrefAndAddsDisabledClass() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "href", "/test" } };
        // Act
        var cut = Render<TnTNavLink>(p => p.Add(x => x.Disabled, true).Add(x => x.AdditionalAttributes, attrs).AddChildContent("Disabled"));
        // Assert
        cut.Instance.AdditionalAttributes!.ContainsKey("href").Should().BeFalse();
        cut.Markup.Should().Contain("tnt-disabled");
    }

    [Fact]
    public void Enabled_PreservesHrefAndNoDisabledClass() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "href", "/test" } };
        // Act
        var cut = Render<TnTNavLink>(p => p.Add(x => x.Disabled, false).Add(x => x.AdditionalAttributes, attrs).AddChildContent("Enabled"));
        // Assert
        cut.Instance.AdditionalAttributes!.ContainsKey("href").Should().BeTrue();
        cut.Markup.Should().NotContain("tnt-disabled");
    }

    [Fact]
    public void EnableRipple_True_RendersRippleEffect() {
        // Act
        var cut = Render<TnTNavLink>(p => p.Add(x => x.EnableRipple, true).AddChildContent("Ripple"));
        // Assert
        cut.Markup.Should().Contain("tnt-ripple-effect");
    }

    [Fact]
    public void Ripple_Disabled_DoesNotRenderRipple() {
        // Act
        var cut = Render<TnTNavLink>(p => p.Add(x => x.EnableRipple, false).AddChildContent("NoRipple"));
        // Assert
        cut.Markup.Should().NotContain("tnt-ripple-effect");
        cut.Markup.Should().NotContain("tnt-ripple");
    }

    [Fact]
    public void Shape_Square_HasSquareClass() {
        // Act
        var cut = Render<TnTNavLink>(p => p.Add(x => x.Shape, AnchorShape.Square).AddChildContent("Square"));
        // Assert
        cut.Markup.Should().Contain("tnt-anchor-square");
    }

    [Fact]
    public void TintColor_WithFilledAppearance_AddsTintVariable() {
        // Act
        var cut = Render<TnTNavLink>(p => p.Add(x => x.Appearance, AnchorAppearance.Filled).AddChildContent("Tint"));
        // Assert
        cut.Markup.Should().Contain("--tnt-anchor-tint-color:var(--tnt-color-surface-tint);");
    }
}

#pragma warning restore CS0618
