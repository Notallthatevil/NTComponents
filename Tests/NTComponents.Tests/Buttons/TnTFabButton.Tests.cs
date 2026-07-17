using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using RippleTestingUtility = NTComponents.Tests.TestingUtility.TestingUtility;

namespace NTComponents.Tests.Buttons;

public class TnTFabButton_Tests : BunitContext {

    public TnTFabButton_Tests() {
        // Arrange (global) & Act: setup ripple interop
        RippleTestingUtility.SetupRippleEffectModule(this);
    }

    [Fact]
    public void Additional_Class_Merged() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "class", "extra" } };
        // Act
        var cut = Render<TnTFabButton>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var cls = cut.Find("button").GetAttribute("class")!;
        // Assert
        cls.Should().Contain("extra");
        cls.Should().Contain("tnt-fab-button");
    }

    [Fact]
    public void Additional_Style_Merged() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "style", "margin:3px" } };
        // Act
        var cut = Render<TnTFabButton>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var style = cut.Find("button").GetAttribute("style")!;
        // Assert
        style.Should().Contain("margin:3px");
        style.Should().Contain("--tnt-button-bg-color");
    }

    [Fact]
    public void Background_And_TextColor_Variables_In_Style() {
        // Arrange & Act
        var cut = Render<TnTFabButton>(p => p.Add(c => c.BackgroundColor, TnTColor.Success).Add(c => c.TextColor, TnTColor.OnSuccess));
        var style = cut.Find("button").GetAttribute("style")!;
        // Assert
        style.Should().Contain("--tnt-button-bg-color:var(--tnt-color-success)");
        style.Should().Contain("--tnt-button-fg-color:var(--tnt-color-on-success)");
    }

    [Fact]
    public void Click_Invokes_OnClickCallback() {
        // Arrange
        var clicked = 0;
        var cut = Render<TnTFabButton>(p => p.Add(c => c.OnClickCallback, EventCallback.Factory.Create<MouseEventArgs>(this, () => clicked++)));
        // Act
        cut.Find("button").Click();
        // Assert
        clicked.Should().Be(1);
    }

    [Fact]
    public void Default_Type_Is_Button() {
        // Arrange & Act
        var cut = Render<TnTFabButton>();
        // Assert
        cut.Find("button").GetAttribute("type")!.Should().Be("button");
    }

    [Fact]
    public void Disabled_Sets_Class_And_Attribute() {
        // Arrange & Act
        var cut = Render<TnTFabButton>(p => p.Add(c => c.Disabled, true));
        var btn = cut.Find("button");
        // Assert
        btn.GetAttribute("class")!.Should().Contain("tnt-disabled");
        btn.HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void Has_TnTId_Attribute() {
        // Arrange & Act
        var cut = Render<TnTFabButton>();
        var btn = cut.Find("button.tnt-fab-button");
        // Assert
        btn.HasAttribute("tntid").Should().BeTrue();
        btn.GetAttribute("tntid").Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void OnTintColor_Sets_Class_And_Style_Variable() {
        // Arrange & Act
        var cut = Render<TnTFabButton>(p => p.Add(c => c.OnTintColor, TnTColor.OnPrimary));
        var btn = cut.Find("button");
        // Assert
        btn.GetAttribute("class")!.Should().Contain("tnt-button-on-tint-color");
        btn.GetAttribute("style")!.Should().Contain("--tnt-button-on-tint-color:var(--tnt-color-on-primary)");
    }

    [Fact]
    public void Renders_Default_FabButton_With_Base_Classes() {
        // Arrange & Act
        var cut = Render<TnTFabButton>(p => p.AddChildContent("Fab"));
        var btn = cut.Find("button.tnt-fab-button");
        var cls = btn.GetAttribute("class")!;
        // Assert
        cls.Should().Contain("tnt-fab-button");
        cls.Should().Contain("tnt-components");
        cls.Should().Contain("tnt-size-s");
        cls.Should().Contain("tnt-interactable");
        cls.Should().Contain("tnt-ripple");
        cls.Should().Contain("tnt-button-tint-color");
        cut.Markup.Should().Contain("Fab");
    }

    [Fact]
    public void Ripple_Disabled_Removes_Ripple_Class_And_Component() {
        // Arrange & Act
        var cut = Render<TnTFabButton>(p => p.Add(c => c.EnableRipple, false));
        var cls = cut.Find("button").GetAttribute("class")!;
        // Assert
        cls.Should().NotContain("tnt-ripple");
        cut.Markup.Should().NotContain("tnt-ripple-effect");
    }

    [Fact]
    public void Ripple_Enabled_Renders_Ripple_Component() {
        // Arrange & Act
        var cut = Render<TnTFabButton>();
        // Assert
        cut.Markup.Should().Contain("tnt-ripple-effect");
    }

    [Fact]
    public void Size_L_Adds_Size_L_Class() {
        // Arrange & Act
        var cut = Render<TnTFabButton>(p => p.Add(c => c.ButtonSize, Size.Large));
        var cls = cut.Find("button").GetAttribute("class")!;
        // Assert
        cls.Should().Contain("tnt-size-l");
    }

    [Fact]
    public void Size_XS_Adds_Size_XS_Class() {
        // Arrange & Act
        var cut = Render<TnTFabButton>(p => p.Add(c => c.ButtonSize, Size.Smallest));
        var cls = cut.Find("button").GetAttribute("class")!;
        // Assert
        cls.Should().Contain("tnt-size-xs");
    }

    [Fact]
    public void StopPropagation_False_Allows_Bubbling() {
        // Arrange
        int parentClicks = 0, childClicks = 0;
        RenderFragment frag = b => {
            b.OpenElement(0, "div");
            b.AddAttribute(1, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => parentClicks++));
            b.OpenComponent<TnTFabButton>(10);
            b.AddComponentParameter(20, nameof(TnTFabButton.ChildContent), (RenderFragment)(c => c.AddContent(0, "Fab")));
            b.AddComponentParameter(30, nameof(TnTFabButton.OnClickCallback), EventCallback.Factory.Create<MouseEventArgs>(this, () => childClicks++));
            b.AddComponentParameter(40, nameof(TnTFabButton.StopPropagation), false);
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
            b.OpenComponent<TnTFabButton>(10);
            b.AddComponentParameter(20, nameof(TnTFabButton.ChildContent), (RenderFragment)(c => c.AddContent(0, "Fab")));
            b.AddComponentParameter(30, nameof(TnTFabButton.OnClickCallback), EventCallback.Factory.Create<MouseEventArgs>(this, () => childClicks++));
            b.AddComponentParameter(40, nameof(TnTFabButton.StopPropagation), true);
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
    public void TintColor_Default_Adds_Tint_Class_And_Variable() {
        // Arrange & Act
        var cut = Render<TnTFabButton>();
        var btn = cut.Find("button");
        // Assert
        btn.GetAttribute("class")!.Should().Contain("tnt-button-tint-color");
        btn.GetAttribute("style")!.Should().Contain("--tnt-button-tint-color:var(--tnt-color-surface-tint)");
    }

    [Fact]
    public void TintColor_Null_Removes_Tint_Class_And_Style() {
        // Arrange & Act
        var cut = Render<TnTFabButton>(p => p.Add(c => c.TintColor, null));
        var btn = cut.Find("button");
        // Assert
        btn.GetAttribute("class")!.Should().NotContain("tnt-button-tint-color");
        btn.GetAttribute("style")!.Should().NotContain("--tnt-button-tint-color");
    }
}