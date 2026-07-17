using System.Collections.Generic;
using Bunit;
using Xunit;
using NTComponents;
using Microsoft.AspNetCore.Components;

namespace NTComponents.Tests.Divider;

#pragma warning disable CS0618 // TnTDivider is obsolete but remains covered for compatibility.

public class TnTDivider_Tests : BunitContext {

    [Fact]
    public void AdditionalAttributes_Class_Merged() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "class", "custom-divider" } };

        // Act
        var cut = Render<TnTDivider>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var cls = cut.Find("div").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("custom-divider");
        cls.Should().Contain("tnt-divider");
    }

    [Fact]
    public void AdditionalAttributes_Custom_Attributes_Applied() {
        // Arrange
        var attrs = new Dictionary<string, object> {
            { "data-testid", "divider-test" },
            { "role", "separator" }
        };

        // Act
        var cut = Render<TnTDivider>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var div = cut.Find("div");

        // Assert
        div.GetAttribute("data-testid").Should().Be("divider-test");
        div.GetAttribute("role").Should().Be("separator");
    }

    [Fact]
    public void AdditionalAttributes_Multiple_Classes_Merged() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "class", "first-class second-class" } };

        // Act
        var cut = Render<TnTDivider>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var cls = cut.Find("div").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("first-class");
        cls.Should().Contain("second-class");
        cls.Should().Contain("tnt-divider");
    }

    [Fact]
    public void AdditionalAttributes_Multiple_Styles_Merged() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "style", "margin:10px;padding:5px" } };

        // Act
        var cut = Render<TnTDivider>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var style = cut.Find("div").GetAttribute("style")!;

        // Assert
        style.Should().Contain("margin:10px");
        style.Should().Contain("padding:5px");
        style.Should().Contain("--tnt-divider-color");
    }

    [Fact]
    public void AdditionalAttributes_Style_Merged() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "style", "margin:10px" } };

        // Act
        var cut = Render<TnTDivider>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var style = cut.Find("div").GetAttribute("style")!;

        // Assert
        style.Should().Contain("margin:10px");
        style.Should().Contain("--tnt-divider-color");
    }

    [Fact]
    public void All_Base_Component_Properties_Work_Together() {
        // Arrange
        var attrs = new Dictionary<string, object> {
            { "class", "extra-class" },
            { "style", "border-radius:4px" }
        };

        // Act
        var cut = Render<TnTDivider>(p => p
            .Add(c => c.Direction, LayoutDirection.Vertical)
            .Add(c => c.Color, TnTColor.Success)
            .Add(c => c.ElementId, "main-divider")
            .Add(c => c.ElementTitle, "Main separator")
            .Add(c => c.ElementLang, "en")
            .Add(c => c.AutoFocus, true)
            .Add(c => c.AdditionalAttributes, attrs));

        var div = cut.Find("div");
        var cls = div.GetAttribute("class")!;
        var style = div.GetAttribute("style")!;

        // Assert
        cls.Should().Contain("tnt-divider");
        cls.Should().Contain("tnt-components");
        cls.Should().Contain("tnt-divider-vertical");
        cls.Should().Contain("extra-class");

        style.Should().Contain("--tnt-divider-color:var(--tnt-color-success)");
        style.Should().Contain("border-radius:4px");

        div.GetAttribute("id")!.Should().Be("main-divider");
        div.GetAttribute("title")!.Should().Be("Main separator");
        div.GetAttribute("lang")!.Should().Be("en");
        div.HasAttribute("autofocus").Should().BeTrue();
        div.HasAttribute("tntid").Should().BeTrue();
    }

    [Fact]
    public void AutoFocus_False_Does_Not_Render_Autofocus_Attribute() {
        // Arrange & Act
        var cut = Render<TnTDivider>(p => p.Add(c => c.AutoFocus, false));

        // Assert
        cut.Find("div").HasAttribute("autofocus").Should().BeFalse();
    }

    [Fact]
    public void AutoFocus_True_Renders_Autofocus_Attribute() {
        // Arrange & Act
        var cut = Render<TnTDivider>(p => p.Add(c => c.AutoFocus, true));

        // Assert
        cut.Find("div").HasAttribute("autofocus").Should().BeTrue();
    }

    [Fact]
    public void Color_Error_Adds_Error_Color_Variable() {
        // Arrange & Act
        var cut = Render<TnTDivider>(p => p.Add(c => c.Color, TnTColor.Error));
        var style = cut.Find("div").GetAttribute("style")!;

        // Assert
        style.Should().Contain("--tnt-divider-color:var(--tnt-color-error)");
    }

    [Fact]
    public void Color_Null_Does_Not_Add_Color_Variable() {
        // Arrange & Act
        var cut = Render<TnTDivider>(p => p.Add(c => c.Color, null));
        var style = cut.Find("div").GetAttribute("style")!;

        // Assert
        style.Should().NotContain("--tnt-divider-color");
    }

    [Fact]
    public void Color_Primary_Adds_Primary_Color_Variable() {
        // Arrange & Act
        var cut = Render<TnTDivider>(p => p.Add(c => c.Color, TnTColor.Primary));
        var style = cut.Find("div").GetAttribute("style")!;

        // Assert
        style.Should().Contain("--tnt-divider-color:var(--tnt-color-primary)");
    }

    [Fact]
    public void Color_Secondary_Adds_Secondary_Color_Variable() {
        // Arrange & Act
        var cut = Render<TnTDivider>(p => p.Add(c => c.Color, TnTColor.Secondary));
        var style = cut.Find("div").GetAttribute("style")!;

        // Assert
        style.Should().Contain("--tnt-divider-color:var(--tnt-color-secondary)");
    }

    [Fact]
    public void Color_Success_Adds_Success_Color_Variable() {
        // Arrange & Act
        var cut = Render<TnTDivider>(p => p.Add(c => c.Color, TnTColor.Success));
        var style = cut.Find("div").GetAttribute("style")!;

        // Assert
        style.Should().Contain("--tnt-divider-color:var(--tnt-color-success)");
    }

    [Fact]
    public void Color_Warning_Adds_Warning_Color_Variable() {
        // Arrange & Act
        var cut = Render<TnTDivider>(p => p.Add(c => c.Color, TnTColor.Warning));
        var style = cut.Find("div").GetAttribute("style")!;

        // Assert
        style.Should().Contain("--tnt-divider-color:var(--tnt-color-warning)");
    }

    [Fact]
    public void Complex_Multi_Hump_Color_Names_Convert_To_Kebab_Case() {
        // Arrange & Act
        var cut = Render<TnTDivider>(p => p.Add(c => c.Color, TnTColor.SurfaceContainerHighest));
        var style = cut.Find("div").GetAttribute("style")!;

        // Assert
        style.Should().Contain("--tnt-divider-color:var(--tnt-color-surface-container-highest)");
    }

    [Fact]
    public void Default_Color_Is_OutlineVariant() {
        // Arrange & Act
        var cut = Render<TnTDivider>();
        var style = cut.Find("div").GetAttribute("style")!;

        // Assert
        style.Should().Contain("--tnt-divider-color:var(--tnt-color-outline-variant)");
    }

    [Fact]
    public void Default_Direction_Is_Horizontal() {
        // Arrange & Act
        var cut = Render<TnTDivider>();
        var cls = cut.Find("div").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("tnt-divider-horizontal");
        cls.Should().NotContain("tnt-divider-vertical");
    }

    [Fact]
    public void Direction_And_Color_Parameters_Can_Be_Combined() {
        // Arrange & Act
        var cut = Render<TnTDivider>(p => p
            .Add(c => c.Direction, LayoutDirection.Vertical)
            .Add(c => c.Color, TnTColor.Primary));
        var div = cut.Find("div");
        var cls = div.GetAttribute("class")!;
        var style = div.GetAttribute("style")!;

        // Assert
        cls.Should().Contain("tnt-divider-vertical");
        style.Should().Contain("--tnt-divider-color:var(--tnt-color-primary)");
    }

    [Fact]
    public void Direction_Horizontal_Adds_Horizontal_Class() {
        // Arrange & Act
        var cut = Render<TnTDivider>(p => p.Add(c => c.Direction, LayoutDirection.Horizontal));
        var cls = cut.Find("div").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("tnt-divider-horizontal");
        cls.Should().NotContain("tnt-divider-vertical");
    }

    [Fact]
    public void Direction_Vertical_Adds_Vertical_Class() {
        // Arrange & Act
        var cut = Render<TnTDivider>(p => p.Add(c => c.Direction, LayoutDirection.Vertical));
        var cls = cut.Find("div").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("tnt-divider-vertical");
        cls.Should().NotContain("tnt-divider-horizontal");
    }

    [Fact]
    public void ElementId_Renders_Id_Attribute() {
        // Arrange & Act
        var cut = Render<TnTDivider>(p => p.Add(c => c.ElementId, "divider-id"));

        // Assert
        cut.Find("div").GetAttribute("id")!.Should().Be("divider-id");
    }

    [Fact]
    public void ElementLang_Renders_Lang_Attribute() {
        // Arrange & Act
        var cut = Render<TnTDivider>(p => p.Add(c => c.ElementLang, "en"));

        // Assert
        cut.Find("div").GetAttribute("lang")!.Should().Be("en");
    }

    [Fact]
    public void ElementTitle_Renders_Title_Attribute() {
        // Arrange & Act
        var cut = Render<TnTDivider>(p => p.Add(c => c.ElementTitle, "Separator"));

        // Assert
        cut.Find("div").GetAttribute("title")!.Should().Be("Separator");
    }

    [Fact]
    public void Has_TnTId_Attribute() {
        // Arrange & Act
        var cut = Render<TnTDivider>();
        var root = cut.Find("div.tnt-divider");

        // Assert
        root.HasAttribute("tntid").Should().BeTrue();
        root.GetAttribute("tntid").Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Renders_Default_Divider_With_Base_Classes() {
        // Arrange & Act
        var cut = Render<TnTDivider>();
        var div = cut.Find("div.tnt-divider");
        var cls = div.GetAttribute("class")!;

        // Assert
        cls.Should().Contain("tnt-divider");
        cls.Should().Contain("tnt-components");
        cls.Should().Contain("tnt-divider-horizontal");
    }

    [Fact]
    public void Renders_Empty_Div_With_No_Content() {
        // Arrange & Act
        var cut = Render<TnTDivider>();
        var div = cut.Find("div.tnt-divider");

        // Assert
        div.InnerHtml.Should().BeEmpty();
    }

    [Fact]
    public void Single_Hump_Color_Names_Convert_To_Kebab_Case() {
        // Arrange & Act
        var cut = Render<TnTDivider>(p => p.Add(c => c.Color, TnTColor.OnPrimary));
        var style = cut.Find("div").GetAttribute("style")!;

        // Assert
        style.Should().Contain("--tnt-divider-color:var(--tnt-color-on-primary)");
    }
}

#pragma warning restore CS0618
