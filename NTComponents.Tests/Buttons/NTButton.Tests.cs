using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using RippleTestingUtility = NTComponents.Tests.TestingUtility.TestingUtility;

namespace NTComponents.Tests.Buttons;

public class NTButton_Tests : BunitContext {
    public NTButton_Tests() => RippleTestingUtility.SetupRippleEffectModule(this);

    [Fact]
    public void Default_Render_Uses_Filled_Variant_And_Button_Type() {
        var cut = Render<NTButton>(parameters => parameters.Add(x => x.Label, "Save"));
        var button = cut.Find("button");

        button.GetAttribute("class")!.Should().Contain("nt-button-filled");
        button.GetAttribute("type")!.Should().Be("button");
    }

    [Fact]
    public void Toggle_Button_Renders_AriaPressed_State() {
        var cut = Render<NTButton>(parameters => parameters
            .Add(x => x.Label, "Favorite")
            .Add(x => x.IsToggleButton, true)
            .Add(x => x.Selected, true)
            .Add(x => x.Variant, NTButtonVariant.Filled));

        var button = cut.Find("button");

        button.GetAttribute("aria-pressed")!.Should().Be("true");
        button.GetAttribute("class")!.Should().Contain("nt-button-selected");
    }

    [Fact]
    public void NonToggle_Button_Does_Not_Render_AriaPressed() {
        var cut = Render<NTButton>(parameters => parameters.Add(x => x.Label, "Continue"));

        cut.Find("button").HasAttribute("aria-pressed").Should().BeFalse();
    }

    [Fact]
    public void LeadingIcon_Renders_Before_Label() {
        var cut = Render<NTButton>(parameters => parameters
            .Add(x => x.Label, "Edit")
            .Add(x => x.LeadingIcon, MaterialIcon.Edit));

        cut.Find(".nt-button-icon .tnt-icon").TextContent.Should().Be(MaterialIcon.Edit.Icon);
        cut.Find("button").TextContent.Should().Contain("Edit");
    }

    [Fact]
    public void EnableRipple_False_Does_Not_Render_Ripple_Component() {
        var cut = Render<NTButton>(parameters => parameters
            .Add(x => x.Label, "No ripple")
            .Add(x => x.EnableRipple, false));

        cut.Markup.Should().NotContain("tnt-ripple-effect");
        cut.Markup.Should().NotContain("nt-button-ripple-host");
    }

    [Fact]
    public void Click_Invokes_OnClickCallback() {
        var clicked = 0;
        var cut = Render<NTButton>(parameters => parameters
            .Add(x => x.Label, "Click")
            .Add(x => x.OnClickCallback, EventCallback.Factory.Create<MouseEventArgs>(this, () => clicked++)));

        cut.Find("button").Click();

        clicked.Should().Be(1);
    }

    [Fact]
    public void Selected_Toggle_Inverts_Round_Shape_To_Square_Class() {
        var cut = Render<NTButton>(parameters => parameters
            .Add(x => x.Label, "Selected")
            .Add(x => x.IsToggleButton, true)
            .Add(x => x.Selected, true)
            .Add(x => x.Shape, ButtonShape.Round));

        var button = cut.Find("button");

        button.GetAttribute("class")!.Should().Contain("nt-button-shape-square");
        button.GetAttribute("class")!.Should().NotContain("nt-button-shape-round");
    }

    [Fact]
    public void Text_Toggle_Button_Throws() {
        var render = () => Render<NTButton>(parameters => parameters
            .Add(x => x.Label, "Invalid")
            .Add(x => x.IsToggleButton, true)
            .Add(x => x.Variant, NTButtonVariant.Text));

        render.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Text_Button_With_Visible_BackgroundColor_Throws() {
        var render = () => Render<NTButton>(parameters => parameters
            .Add(x => x.Label, "Invalid")
            .Add(x => x.Variant, NTButtonVariant.Text)
            .Add(x => x.BackgroundColor, TnTColor.Primary));

        render.Should().Throw<InvalidOperationException>()
            .WithMessage("*Text buttons must use a transparent BackgroundColor*");
    }

    [Theory]
    [InlineData(NTButtonVariant.Elevated)]
    [InlineData(NTButtonVariant.Filled)]
    [InlineData(NTButtonVariant.Tonal)]
    public void Contained_Button_With_Transparent_BackgroundColor_Throws(NTButtonVariant variant) {
        var render = () => Render<NTButton>(parameters => parameters
            .Add(x => x.Label, "Invalid")
            .Add(x => x.Variant, variant)
            .Add(x => x.BackgroundColor, TnTColor.Transparent));

        render.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{variant} buttons must use a visible container BackgroundColor*");
    }

    [Fact]
    public void Filled_Button_With_Elevation_Throws() {
        var render = () => Render<NTButton>(parameters => parameters
            .Add(x => x.Label, "Invalid")
            .Add(x => x.Variant, NTButtonVariant.Filled)
            .Add(x => x.Elevation, NTElevation.Lowest));

        render.Should().Throw<InvalidOperationException>()
            .WithMessage("*Filled buttons must use None Elevation*");
    }

    [Fact]
    public void Elevated_Button_With_No_Elevation_Throws() {
        var render = () => Render<NTButton>(parameters => parameters
            .Add(x => x.Label, "Invalid")
            .Add(x => x.Variant, NTButtonVariant.Elevated)
            .Add(x => x.Elevation, NTElevation.None));

        render.Should().Throw<InvalidOperationException>()
            .WithMessage("*Elevated buttons must use a non-zero Elevation*");
    }

    [Fact]
    public void Empty_Label_Throws() {
        var render = () => Render<NTButton>(parameters => parameters
            .Add(x => x.Label, string.Empty));

        render.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Smallest_Size_Adds_Size_Class() {
        var cut = Render<NTButton>(parameters => parameters
            .Add(x => x.Label, "XS")
            .Add(x => x.ButtonSize, Size.Smallest));

        cut.Find("button").GetAttribute("class")!.Should().Contain("tnt-size-xs");
    }

    [Fact]
    public void EnableRipple_True_Renders_RippleHost() {
        var cut = Render<NTButton>(parameters => parameters.Add(x => x.Label, "Ripple"));

        cut.Find(".nt-button-ripple-host").Should().NotBeNull();
        cut.Markup.Should().NotContain("tnt-ripple-effect");
    }

    [Fact]
    public void Custom_Colors_Render_As_Css_Variables() {
        var cut = Render<NTButton>(parameters => parameters
            .Add(x => x.Label, "Styled")
            .Add(x => x.BackgroundColor, TnTColor.SecondaryContainer)
            .Add(x => x.TextColor, TnTColor.OnSecondaryContainer));

        var style = cut.Find("button").GetAttribute("style");

        style.Should().Contain("--nt-button-bg:var(--tnt-color-secondary-container)");
        style.Should().Contain("--nt-button-fg:var(--tnt-color-on-secondary-container)");
    }

    [Fact]
    public void Default_Colors_Come_From_Variant_When_No_Overrides_Are_Set() {
        var cut = Render<NTButton>(parameters => parameters
            .Add(x => x.Label, "Outlined")
            .Add(x => x.Variant, NTButtonVariant.Outlined));

        var button = cut.Find("button");
        var style = button.GetAttribute("style");

        button.GetAttribute("class")!.Should().Contain("nt-button-outlined");
        style.Should().Contain("--nt-button-bg:var(--tnt-color-transparent)");
        style.Should().Contain("--nt-button-fg:var(--tnt-color-primary)");
    }
}
