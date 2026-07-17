using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using RippleTestingUtility = NTComponents.Tests.TestingUtility.TestingUtility;

namespace NTComponents.Tests.Buttons;

public class TnTButton_Tests : BunitContext {
    private const string RippleJsModulePath = "./_content/NTComponents/Core/TnTRippleEffect.razor.js";

    public TnTButton_Tests() {
        // Arrange (global) & Act: JS module setup for ripple in constructor
        RippleTestingUtility.SetupRippleEffectModule(this);
    }

    [Fact]
    public void Appearance_Elevated_Adds_Filled_And_Elevated_Class() {
        // Arrange & Act
        var cut = Render<TnTButton>(p => p.Add(c => c.Appearance, ButtonAppearance.Elevated));
        var cls = cut.Find("button").GetAttribute("class")!;
        // Assert
        cls.Should().Contain("tnt-filled");
        cls.Should().Contain("tnt-elevated");
    }

    [Fact]
    public void Appearance_Filled_Adds_Filled_Class() {
        // Arrange & Act
        var cut = Render<TnTButton>(p => p.Add(c => c.Appearance, ButtonAppearance.Filled));
        // Assert
        cut.Find("button").GetAttribute("class")!.Should().Contain("tnt-filled");
    }

    [Fact]
    public void Appearance_Outlined_Adds_Outlined_Class_Not_Filled() {
        // Arrange & Act
        var cut = Render<TnTButton>(p => p.Add(c => c.Appearance, ButtonAppearance.Outlined));
        var cls = cut.Find("button").GetAttribute("class")!;
        // Assert
        cls.Should().Contain("tnt-outlined");
        cls.Should().NotContain("tnt-filled tnt-elevated");
    }

    [Fact]
    public void Appearance_Text_Adds_Text_Class() {
        // Arrange & Act
        var cut = Render<TnTButton>(p => p.Add(c => c.Appearance, ButtonAppearance.Text));
        // Assert
        cut.Find("button").GetAttribute("class")!.Should().Contain("tnt-text");
    }

    [Fact]
    public void AutoFocus_True_Renders_Autofocus_Attribute() {
        // Arrange & Act
        var cut = Render<TnTButton>(p => p.Add(c => c.AutoFocus, true));
        // Assert
        cut.Find("button").HasAttribute("autofocus").Should().BeTrue();
    }

    [Fact]
    public void Background_And_TextColor_Variables_In_Style() {
        // Arrange & Act
        var cut = Render<TnTButton>(p => p.Add(c => c.BackgroundColor, TnTColor.Success).Add(c => c.TextColor, TnTColor.OnSuccess));
        var style = cut.Find("button").GetAttribute("style")!;
        // Assert
        style.Should().Contain("--tnt-button-bg-color:var(--tnt-color-success)");
        style.Should().Contain("--tnt-button-fg-color:var(--tnt-color-on-success)");
    }

    [Fact]
    public void Click_Invokes_OnClickCallback() {
        // Arrange
        var clicked = 0;
        var cut = Render<TnTButton>(p => p.Add(c => c.OnClickCallback, EventCallback.Factory.Create<MouseEventArgs>(this, () => clicked++)));
        // Act
        cut.Find("button").Click();
        // Assert
        clicked.Should().Be(1);
    }

    [Fact]
    public void Default_Type_Is_Button() {
        // Arrange & Act
        var cut = Render<TnTButton>();
        // Assert
        cut.Find("button").GetAttribute("type")!.Should().Be("button");
    }

    [Fact]
    public void Disabled_Adds_Disabled_Class_And_Disabled_Attribute() {
        // Arrange & Act
        var cut = Render<TnTButton>(p => p.Add(c => c.Disabled, true));
        var root = cut.Find("button");
        // Assert
        root.GetAttribute("class")!.Should().Contain("tnt-disabled");
        root.HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void Disabled_DoesNot_Remove_Ripple_Component() {
        // Arrange & Act
        var cut = Render<TnTButton>(p => p.Add(c => c.Disabled, true));
        // Assert
        cut.Markup.Should().Contain("tnt-ripple-effect");
    }

    [Fact]
    public void Disabled_Small_Size_Disabled_Class_Also_On_Small_Layer() {
        // Arrange & Act
        var cut = Render<TnTButton>(p => p.Add(c => c.ButtonSize, Size.Small).Add(c => c.Disabled, true));
        // Assert
        cut.Find("div.tnt-small-button-layer").GetAttribute("class")!.Should().Contain("tnt-disabled");
    }

    [Fact]
    public void ElementId_Renders_Id_Attribute() {
        // Arrange & Act
        var cut = Render<TnTButton>(p => p.Add(c => c.ElementId, "btn-id"));
        // Assert
        cut.Find("button").GetAttribute("id")!.Should().Be("btn-id");
    }

    [Fact]
    public void ElementName_Sets_Name_Attribute() {
        // Arrange & Act
        var cut = Render<TnTButton>(p => p.Add(c => c.ElementName, "myBtn"));
        // Assert
        cut.Find("button").GetAttribute("name")!.Should().Be("myBtn");
    }

    [Fact]
    public void ElementTitle_Renders_Title_Attribute() {
        // Arrange & Act
        var cut = Render<TnTButton>(p => p.Add(c => c.ElementTitle, "Tooltip"));
        // Assert
        cut.Find("button").GetAttribute("title")!.Should().Be("Tooltip");
    }

    [Fact]
    public void Has_TnTId_Attribute() {
        // Arrange & Act
        var cut = Render<TnTButton>();
        var root = cut.Find("button.tnt-button");
        // Assert
        root.HasAttribute("tntid").Should().BeTrue();
        root.GetAttribute("tntid").Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Medium_Size_Does_Not_Render_Small_Button_Layer() {
        // Arrange & Act
        var cut = Render<TnTButton>(p => p.Add(c => c.ButtonSize, Size.Medium));
        // Assert
        cut.Markup.Should().NotContain("tnt-small-button-layer");
    }

    [Fact]
    public void Merges_Custom_Class_From_AdditionalAttributes() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "class", "extra" } };
        // Act
        var cut = Render<TnTButton>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var cls = cut.Find("button").GetAttribute("class")!;
        // Assert
        cls.Should().Contain("extra");
        cls.Should().Contain("tnt-button");
    }

    [Fact]
    public void Merges_Custom_Style_From_AdditionalAttributes() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "style", "margin:2px" } };
        // Act
        var cut = Render<TnTButton>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var style = cut.Find("button").GetAttribute("style")!;
        // Assert
        style.Should().Contain("margin:2px");
        style.Should().Contain("--tnt-button-bg-color");
    }

    [Fact]
    public void Merges_Multiple_Custom_Classes() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "class", "foo bar" } };
        // Act
        var cut = Render<TnTButton>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var cls = cut.Find("button").GetAttribute("class")!;
        // Assert
        cls.Should().Contain("foo");
        cls.Should().Contain("bar");
        cls.Should().Contain("tnt-button");
    }

    [Fact]
    public void No_Tint_And_No_OnTint_Removes_Both_Classes() {
        // Arrange & Act
        var cut = Render<TnTButton>(p => p.Add(c => c.TintColor, null).Add(c => c.OnTintColor, null));
        var cls = cut.Find("button").GetAttribute("class")!;
        // Assert
        cls.Should().NotContain("tnt-button-tint-color");
        cls.Should().NotContain("tnt-button-on-tint-color");
    }

    [Fact]
    public void Null_ChildContent_Renders_Empty_Button_Content() {
        // Arrange & Act
        var cut = Render<TnTButton>();
        // Assert For small default size there will be a small layer; ensure no unintended text.
        cut.Markup.Should().NotContain("Inner");
    }

    [Fact]
    public void OnTintColor_Adds_Class_And_Style_Variable() {
        // Arrange & Act
        var cut = Render<TnTButton>(p => p.Add(c => c.OnTintColor, TnTColor.OnPrimary));
        var cls = cut.Find("button").GetAttribute("class")!;
        var style = cut.Find("button").GetAttribute("style")!;
        // Assert
        cls.Should().Contain("tnt-button-on-tint-color");
        style.Should().Contain("--tnt-button-on-tint-color:var(--tnt-color-on-primary)");
    }

    [Fact]
    public void OnTintColor_Null_Removes_Class_And_Style() {
        // Arrange & Act
        var cut = Render<TnTButton>(p => p.Add(c => c.OnTintColor, null));
        var cls = cut.Find("button").GetAttribute("class")!;
        // Assert
        cls.Should().NotContain("tnt-button-on-tint-color");
        cut.Find("button").GetAttribute("style")!.Should().NotContain("--tnt-button-on-tint-color");
    }

    [Fact]
    public void Renders_Button_With_Default_Classes_And_Tint_Class() {
        // Arrange & Act
        var cut = Render<TnTButton>(p => p.AddChildContent("Click"));
        var root = cut.Find("button.tnt-button");
        // Assert
        var cls = root.GetAttribute("class")!;
        cls.Should().Contain("tnt-button");
        cls.Should().Contain("tnt-components");
        cls.Should().Contain("tnt-size-s");
        cls.Should().Contain("tnt-interactable");
        cls.Should().Contain("tnt-ripple");
        cls.Should().Contain("tnt-button-tint-color"); // default TintColor provided
        cut.Markup.Should().Contain("Click");
    }

    [Fact]
    public void Ripple_Disabled_Removes_Ripple_Class_And_Component() {
        // Arrange & Act
        var cut = Render<TnTButton>(p => p.Add(c => c.EnableRipple, false));
        var cls = cut.Find("button").GetAttribute("class")!;
        // Assert
        cls.Should().NotContain("tnt-ripple");
        cut.Markup.Should().NotContain("tnt-ripple-effect");
    }

    [Fact]
    public void Ripple_Enabled_Renders_Ripple_Component() {
        // Arrange & Act
        var cut = Render<TnTButton>();
        // Assert
        cut.Markup.Should().Contain("tnt-ripple-effect");
    }

    [Fact]
    public void Shape_Square_Adds_Button_Square_Class() {
        // Arrange & Act
        var cut = Render<TnTButton>(p => p.Add(c => c.Shape, ButtonShape.Square));
        // Assert
        cut.Find("button").GetAttribute("class")!.Should().Contain("tnt-button-square");
    }

    [Fact]
    public void Size_Large_Adds_Size_L_Class() {
        // Arrange & Act
        var cut = Render<TnTButton>(p => p.Add(c => c.ButtonSize, Size.Large));
        // Assert
        cut.Find("button").GetAttribute("class")!.Should().Contain("tnt-size-l");
    }

    [Fact]
    public void Size_XL_Class_No_Small_Layer() {
        // Arrange & Act
        var cut = Render<TnTButton>(p => p.Add(c => c.ButtonSize, Size.Largest));
        var cls = cut.Find("button").GetAttribute("class")!;
        // Assert
        cls.Should().Contain("tnt-size-xl");
        cut.Markup.Should().NotContain("tnt-small-button-layer");
    }

    [Fact]
    public void Size_XS_Class_And_Layer_Render() {
        // Arrange & Act
        var cut = Render<TnTButton>(p => p.Add(c => c.ButtonSize, Size.Smallest));
        var cls = cut.Find("button").GetAttribute("class")!;
        // Assert
        cls.Should().Contain("tnt-size-xs");
        cut.Markup.Should().Contain("tnt-small-button-layer");
    }

    [Fact]
    public void Small_Size_Renders_Small_Button_Layer_With_Repeated_Classes() {
        // Arrange & Act
        var cut = Render<TnTButton>(p => p.Add(c => c.ButtonSize, Size.Small).AddChildContent("Inner"));
        // Assert
        cut.Find("div.tnt-small-button-layer").MarkupMatches(cut.Find("div.tnt-small-button-layer").OuterHtml);
        cut.Markup.Should().Contain("Inner");
    }

    [Fact]
    public void Small_Size_Ripple_Disabled_Removes_Ripple_Component() {
        // Arrange & Act
        var cut = Render<TnTButton>(p => p.Add(c => c.ButtonSize, Size.Small).Add(c => c.EnableRipple, false));
        // Assert
        cut.Markup.Should().NotContain("tnt-ripple-effect");
        cut.Find("button").GetAttribute("class")!.Should().NotContain("tnt-ripple");
    }

    [Fact]
    public void StopPropagation_False_Allows_Bubbling() {
        // Arrange
        int parentClicks = 0, childClicks = 0;
        RenderFragment frag = b => {
            b.OpenElement(0, "div");
            b.AddAttribute(1, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => parentClicks++));
            b.OpenComponent<TnTButton>(10);
            b.AddComponentParameter(20, nameof(TnTButton.ChildContent), (RenderFragment)(c => c.AddContent(0, "X")));
            b.AddComponentParameter(30, nameof(TnTButton.OnClickCallback), EventCallback.Factory.Create<MouseEventArgs>(this, () => childClicks++));
            b.AddComponentParameter(40, nameof(TnTButton.StopPropagation), false);
            b.CloseComponent();
            b.CloseElement();
        };
        var cut = Render(frag);
        // Act
        cut.Find("button").Click();
        // Assert
        childClicks.Should().Be(1);
        parentClicks.Should().Be(1); // bubbled
    }

    [Fact]
    public void StopPropagation_True_Prevents_Bubbling() {
        // Arrange
        int parentClicks = 0, childClicks = 0;
        RenderFragment frag = b => {
            b.OpenElement(0, "div");
            b.AddAttribute(1, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => parentClicks++));
            b.OpenComponent<TnTButton>(10);
            b.AddComponentParameter(20, nameof(TnTButton.ChildContent), (RenderFragment)(c => c.AddContent(0, "Y")));
            b.AddComponentParameter(30, nameof(TnTButton.OnClickCallback), EventCallback.Factory.Create<MouseEventArgs>(this, () => childClicks++));
            b.AddComponentParameter(40, nameof(TnTButton.StopPropagation), true);
            b.CloseComponent();
            b.CloseElement();
        };
        var cut = Render(frag);
        // Act
        cut.Find("button").Click();
        // Assert
        childClicks.Should().Be(1);
        parentClicks.Should().Be(0); // stopped
    }

    [Fact]
    public void TextAlignment_Adds_TextAlign_Class() {
        // Arrange & Act
        var cut = Render<TnTButton>(p => p.Add(c => c.TextAlignment, TextAlign.Center));
        // Assert
        cut.Find("button").GetAttribute("class")!.Should().Contain("tnt-text-align-center");
    }

    [Fact]
    public void Tint_And_OnTint_Both_Add_Classes() {
        // Arrange & Act
        var cut = Render<TnTButton>(p => p.Add(c => c.TintColor, TnTColor.Primary).Add(c => c.OnTintColor, TnTColor.OnPrimary));
        var cls = cut.Find("button").GetAttribute("class")!;
        // Assert
        cls.Should().Contain("tnt-button-tint-color");
        cls.Should().Contain("tnt-button-on-tint-color");
    }

    [Fact]
    public void TintColor_Default_Style_Includes_Tint_Variable() {
        // Arrange & Act
        var cut = Render<TnTButton>();
        // Assert
        cut.Find("button").GetAttribute("style")!.Should().Contain("--tnt-button-tint-color:var(--tnt-color-surface-tint)");
    }

    [Fact]
    public void TintColor_Null_Removes_Tint_Class_And_Style() {
        // Arrange & Act
        var cut = Render<TnTButton>(p => p.Add(c => c.TintColor, null));
        var cls = cut.Find("button").GetAttribute("class")!;
        // Assert
        cls.Should().NotContain("tnt-button-tint-color");
        cut.Find("button").GetAttribute("style")!.Should().NotContain("--tnt-button-tint-color");
    }

    [Fact]
    public void Type_Reset_Adds_Reset_Type_Attribute() {
        // Arrange & Act
        var cut = Render<TnTButton>(p => p.Add(c => c.Type, ButtonType.Reset));
        // Assert
        cut.Find("button").GetAttribute("type")!.Should().Be("reset");
    }

    [Fact]
    public void Type_Submit_Adds_Submit_Type_Attribute() {
        // Arrange & Act
        var cut = Render<TnTButton>(p => p.Add(c => c.Type, ButtonType.Submit));
        // Assert
        cut.Find("button").GetAttribute("type")!.Should().Be("submit");
    }
}