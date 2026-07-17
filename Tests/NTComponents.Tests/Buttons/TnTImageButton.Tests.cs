using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using RippleTestingUtility = NTComponents.Tests.TestingUtility.TestingUtility;

namespace NTComponents.Tests.Buttons;

public class TnTImageButton_Tests : BunitContext {
    private static TnTIcon SampleIcon => MaterialIcon.Menu;

    public TnTImageButton_Tests() {
        // Arrange (global) & Act: setup ripple interop
        RippleTestingUtility.SetupRippleEffectModule(this);
    }

    [Fact]
    public void Additional_Attributes_Class_Merged() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "class", "extra" } };
        // Act
        var cut = Render<TnTImageButton>(p => p.Add(c => c.Icon, SampleIcon).Add(c => c.AdditionalAttributes, attrs));
        var cls = cut.Find("button").GetAttribute("class")!;
        // Assert
        cls.Should().Contain("extra");
        cls.Should().Contain("tnt-image-button");
    }

    [Fact]
    public void Additional_Attributes_Style_Merged() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "style", "margin:5px" } };
        // Act
        var cut = Render<TnTImageButton>(p => p.Add(c => c.Icon, SampleIcon).Add(c => c.AdditionalAttributes, attrs));
        var style = cut.Find("button").GetAttribute("style")!;
        // Assert
        style.Should().Contain("margin:5px");
        style.Should().Contain("--tnt-button-bg-color");
    }

    [Fact]
    public void Appearance_Elevated_Adds_Filled_And_Elevated() {
        // Arrange & Act
        var cut = Render<TnTImageButton>(p => p.Add(c => c.Icon, SampleIcon).Add(c => c.Appearance, ButtonAppearance.Elevated));
        var cls = cut.Find("button").GetAttribute("class")!;
        // Assert
        cls.Should().Contain("tnt-filled");
        cls.Should().Contain("tnt-elevated");
    }

    [Fact]
    public void Appearance_Filled_Adds_Filled() {
        // Arrange & Act
        var cut = Render<TnTImageButton>(p => p.Add(c => c.Icon, SampleIcon).Add(c => c.Appearance, ButtonAppearance.Filled));
        // Assert
        cut.Find("button").GetAttribute("class")!.Should().Contain("tnt-filled");
    }

    [Fact]
    public void Appearance_Outlined_Adds_Outlined() {
        // Arrange & Act
        var cut = Render<TnTImageButton>(p => p.Add(c => c.Icon, SampleIcon).Add(c => c.Appearance, ButtonAppearance.Outlined));
        // Assert
        cut.Find("button").GetAttribute("class")!.Should().Contain("tnt-outlined");
    }

    [Fact]
    public void Appearance_Text_Adds_Text() {
        // Arrange & Act
        var cut = Render<TnTImageButton>(p => p.Add(c => c.Icon, SampleIcon).Add(c => c.Appearance, ButtonAppearance.Text));
        // Assert
        cut.Find("button").GetAttribute("class")!.Should().Contain("tnt-text");
    }

    [Fact]
    public void Background_And_TextColor_Variables_In_Style() {
        // Arrange & Act
        var cut = Render<TnTImageButton>(p => p.Add(c => c.Icon, SampleIcon).Add(c => c.BackgroundColor, TnTColor.Success).Add(c => c.TextColor, TnTColor.OnSuccess));
        var style = cut.Find("button").GetAttribute("style")!;
        // Assert
        style.Should().Contain("--tnt-button-bg-color:var(--tnt-color-success)");
        style.Should().Contain("--tnt-button-fg-color:var(--tnt-color-on-success)");
    }

    [Fact]
    public void Badge_Renders_When_Provided() {
        // Arrange
        var badge = TnTBadge.CreateBadge("1");
        // Act
        var cut = Render<TnTImageButton>(p => p.Add(c => c.Icon, SampleIcon).Add(c => c.Badge, badge));
        // Assert
        cut.Markup.Should().Contain("1");
        cut.Markup.Should().Contain("tnt-badge");
    }

    [Fact]
    public void Click_Invokes_OnClickCallback() {
        // Arrange
        var clicked = 0;
        var cut = Render<TnTImageButton>(p => p.Add(c => c.Icon, SampleIcon).Add(c => c.OnClickCallback, EventCallback.Factory.Create<MouseEventArgs>(this, () => clicked++)));
        // Act
        cut.Find("button").Click();
        // Assert
        clicked.Should().Be(1);
    }

    [Fact]
    public void Disabled_Adds_Disabled_Class() {
        // Arrange & Act
        var cut = Render<TnTImageButton>(p => p.Add(c => c.Icon, SampleIcon).Add(c => c.Disabled, true));
        // Assert
        cut.Find("button").GetAttribute("class")!.Should().Contain("tnt-disabled");
    }

    [Fact]
    public void ImageButtonAppearance_Narrow_Adds_Narrow_Class() {
        // Arrange & Act
        var cut = Render<TnTImageButton>(p => p.Add(c => c.Icon, SampleIcon).Add(c => c.ImageButtonAppearance, ImageButtonAppearance.Narrow));
        // Assert
        cut.Find("button").GetAttribute("class")!.Should().Contain("tnt-image-button-narrow");
    }

    [Fact]
    public void ImageButtonAppearance_Wide_Adds_Wide_Class() {
        // Arrange & Act
        var cut = Render<TnTImageButton>(p => p.Add(c => c.Icon, SampleIcon).Add(c => c.ImageButtonAppearance, ImageButtonAppearance.Wide));
        // Assert
        cut.Find("button").GetAttribute("class")!.Should().Contain("tnt-image-button-wide");
    }

    [Fact]
    public void OnTintColor_Adds_Class_And_Style() {
        // Arrange & Act
        var cut = Render<TnTImageButton>(p => p.Add(c => c.Icon, SampleIcon).Add(c => c.OnTintColor, TnTColor.OnPrimary));
        var btn = cut.Find("button");
        // Assert
        btn.GetAttribute("class")!.Should().Contain("tnt-button-on-tint-color");
        btn.GetAttribute("style")!.Should().Contain("--tnt-button-on-tint-color:var(--tnt-color-on-primary)");
    }

    [Fact]
    public void Renders_Default_ImageButton_With_Base_Classes() {
        // Arrange & Act
        var cut = Render<TnTImageButton>(p => p.Add(c => c.Icon, SampleIcon));
        var btn = cut.Find("button.tnt-image-button");
        var cls = btn.GetAttribute("class")!;
        // Assert
        cls.Should().Contain("tnt-image-button");
        cls.Should().Contain("tnt-components");
        cls.Should().Contain("tnt-size-s");
        cls.Should().Contain("tnt-interactable");
        cls.Should().Contain("tnt-ripple");
        cls.Should().Contain("tnt-image-button-round"); // default ImageButtonAppearance=Round
        cls.Should().Contain("tnt-button-tint-color");
    }

    [Fact]
    public void Requires_Icon_Parameter() {
        // Arrange & Act
        var ex = Record.Exception(() => Render<TnTImageButton>()); // Icon is EditorRequired and null triggers validation
        // Assert
        ex.Should().BeOfType<ArgumentNullException>();
    }

    [Fact]
    public void Ripple_Disabled_Removes_Ripple_Class_And_Component() {
        // Arrange & Act
        var cut = Render<TnTImageButton>(p => p.Add(c => c.Icon, SampleIcon).Add(c => c.EnableRipple, false));
        var cls = cut.Find("button").GetAttribute("class")!;
        // Assert
        cls.Should().NotContain("tnt-ripple");
        cut.Markup.Should().NotContain("tnt-ripple-effect");
    }

    [Fact]
    public void Ripple_Enabled_Renders_Component() {
        // Arrange & Act
        var cut = Render<TnTImageButton>(p => p.Add(c => c.Icon, SampleIcon));
        // Assert
        cut.Markup.Should().Contain("tnt-ripple-effect");
    }

    [Fact]
    public void Shape_Square_Adds_Button_Square_Class() {
        // Arrange & Act
        var cut = Render<TnTImageButton>(p => p.Add(c => c.Icon, SampleIcon).Add(c => c.Shape, ButtonShape.Square));
        // Assert
        cut.Find("button").GetAttribute("class")!.Should().Contain("tnt-button-square");
    }

    [Fact]
    public void Size_M_Removes_Small_Layer() {
        // Arrange & Act
        var cut = Render<TnTImageButton>(p => p.Add(c => c.Icon, SampleIcon).Add(c => c.ButtonSize, Size.Medium));
        // Assert
        cut.Markup.Should().NotContain("tnt-small-button-layer");
    }

    [Fact]
    public void Size_XS_Renders_Small_Layer() {
        // Arrange & Act
        var cut = Render<TnTImageButton>(p => p.Add(c => c.Icon, SampleIcon).Add(c => c.ButtonSize, Size.Smallest));
        // Assert
        cut.Markup.Should().Contain("tnt-small-button-layer");
    }

    [Fact]
    public void StopPropagation_False_Allows_Bubbling() {
        // Arrange
        int parentClicks = 0, childClicks = 0;
        RenderFragment frag = b => {
            b.OpenElement(0, "div");
            b.AddAttribute(1, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => parentClicks++));
            b.OpenComponent<TnTImageButton>(10);
            b.AddComponentParameter(20, nameof(TnTImageButton.Icon), SampleIcon);
            b.AddComponentParameter(30, nameof(TnTImageButton.OnClickCallback), EventCallback.Factory.Create<MouseEventArgs>(this, () => childClicks++));
            b.AddComponentParameter(40, nameof(TnTImageButton.StopPropagation), false);
            b.CloseComponent();
            b.CloseElement();
        };
        var cut = Render(frag);
        // Act
        cut.Find("button").Click();
        // Assert
        childClicks.Should().Be(1);
        parentClicks.Should().Be(1);
    }

    [Fact]
    public void StopPropagation_True_Prevents_Bubbling() {
        // Arrange
        int parentClicks = 0, childClicks = 0;
        RenderFragment frag = b => {
            b.OpenElement(0, "div");
            b.AddAttribute(1, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => parentClicks++));
            b.OpenComponent<TnTImageButton>(10);
            b.AddComponentParameter(20, nameof(TnTImageButton.Icon), SampleIcon);
            b.AddComponentParameter(30, nameof(TnTImageButton.OnClickCallback), EventCallback.Factory.Create<MouseEventArgs>(this, () => childClicks++));
            b.AddComponentParameter(40, nameof(TnTImageButton.StopPropagation), true);
            b.CloseComponent();
            b.CloseElement();
        };
        var cut = Render(frag);
        // Act
        cut.Find("button").Click();
        // Assert
        childClicks.Should().Be(1);
        parentClicks.Should().Be(0);
    }

    [Fact]
    public void TintColor_Default_Adds_Class_And_Variable() {
        // Arrange & Act
        var cut = Render<TnTImageButton>(p => p.Add(c => c.Icon, SampleIcon));
        var btn = cut.Find("button");
        // Assert
        btn.GetAttribute("class")!.Should().Contain("tnt-button-tint-color");
        btn.GetAttribute("style")!.Should().Contain("--tnt-button-tint-color:var(--tnt-color-surface-tint)");
    }

    [Fact]
    public void TintColor_Null_Removes_Class_And_Variable() {
        // Arrange & Act
        var cut = Render<TnTImageButton>(p => p.Add(c => c.Icon, SampleIcon).Add(c => c.TintColor, null));
        var btn = cut.Find("button");
        // Assert
        btn.GetAttribute("class")!.Should().NotContain("tnt-button-tint-color");
        btn.GetAttribute("style")!.Should().NotContain("--tnt-button-tint-color");
    }
}