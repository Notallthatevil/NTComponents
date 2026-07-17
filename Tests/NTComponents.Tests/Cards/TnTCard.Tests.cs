#pragma warning disable CS0618
namespace NTComponents.Tests.Cards;

public class TnTCard_Tests : BunitContext {

    [Fact]
    public void AdditionalAttributes_Class_Merged() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "class", "extra" } };
        // Act
        var cut = Render<TnTCard>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var cls = cut.Find("div").GetAttribute("class")!;
        // Assert
        cls.Should().Contain("extra");
        cls.Should().Contain("tnt-card");
    }

    [Fact]
    public void AdditionalAttributes_Multiple_Classes_Merged() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "class", "alpha beta" } };
        // Act
        var cut = Render<TnTCard>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var cls = cut.Find("div").GetAttribute("class")!;
        // Assert
        cls.Should().Contain("alpha");
        cls.Should().Contain("beta");
        cls.Should().Contain("tnt-card");
    }

    [Fact]
    public void AdditionalAttributes_Multiple_Styles_Merged() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "style", "margin:3px;padding:4px" } };
        // Act
        var cut = Render<TnTCard>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var style = cut.Find("div").GetAttribute("style")!;
        // Assert
        style.Should().Contain("margin:3px");
        style.Should().Contain("padding:4px");
        style.Should().Contain("--tnt-card-background-color");
    }

    [Fact]
    public void AdditionalAttributes_Style_Merged() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "style", "margin:3px" } };
        // Act
        var cut = Render<TnTCard>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var style = cut.Find("div").GetAttribute("style")!;
        // Assert
        style.Should().Contain("margin:3px");
        style.Should().Contain("--tnt-card-background-color");
    }

    [Fact]
    public void Appearance_Elevated_Adds_Elevated_Class_Only() {
        // Arrange & Act
        var cut = Render<TnTCard>(p => p.Add(c => c.Appearance, CardAppearance.Elevated));
        var cls = cut.Find("div").GetAttribute("class")!;
        // Assert
        cls.Should().Contain("tnt-card-elevated");
        cls.Should().NotContain("tnt-card-filled");
        cls.Should().NotContain("tnt-card-outlined");
    }

    [Fact]
    public void Appearance_Filled_Adds_Filled_Class_Only() {
        // Arrange & Act
        var cut = Render<TnTCard>(p => p.Add(c => c.Appearance, CardAppearance.Filled));
        var cls = cut.Find("div").GetAttribute("class")!;
        // Assert
        cls.Should().Contain("tnt-card-filled");
        cls.Should().NotContain("tnt-card-outlined");
        cls.Should().NotContain("tnt-card-elevated");
    }

    [Fact]
    public void Appearance_Outlined_Adds_Outlined_Class_Only() {
        // Arrange & Act
        var cut = Render<TnTCard>(p => p.Add(c => c.Appearance, CardAppearance.Outlined));
        var cls = cut.Find("div").GetAttribute("class")!;
        // Assert
        cls.Should().Contain("tnt-card-outlined");
        cls.Should().NotContain("tnt-card-filled");
        cls.Should().NotContain("tnt-card-elevated");
    }

    [Fact]
    public void AutoFocus_True_Renders_Autofocus_Attribute() {
        // Arrange & Act
        var cut = Render<TnTCard>(p => p.Add(c => c.AutoFocus, true));
        // Assert
        cut.Find("div").HasAttribute("autofocus").Should().BeTrue();
    }

    [Fact]
    public void ChildContent_Renders() {
        // Arrange & Act
        var cut = Render<TnTCard>(p => p.AddChildContent("Content"));
        // Assert
        cut.Markup.Should().Contain("Content");
    }

    [Fact]
    public void Custom_Background_And_TextColor_Variables_In_Style() {
        // Arrange & Act
        var cut = Render<TnTCard>(p => p.Add(c => c.BackgroundColor, TnTColor.Success).Add(c => c.TextColor, TnTColor.OnSuccess));
        var style = cut.Find("div").GetAttribute("style")!;
        // Assert
        style.Should().Contain("--tnt-card-background-color:var(--tnt-color-success)");
        style.Should().Contain("--tnt-card-text-color:var(--tnt-color-on-success)");
    }

    [Fact]
    public void Default_Background_And_TextColor_Variables_In_Style() {
        // Arrange & Act
        var cut = Render<TnTCard>();
        var style = cut.Find("div").GetAttribute("style")!;
        // Assert
        style.Should().Contain("--tnt-card-background-color:var(--tnt-color-surface-container-low)");
        style.Should().Contain("--tnt-card-text-color:var(--tnt-color-on-surface)");
    }

    [Fact]
    public void ElementId_Renders_Id_Attribute() {
        // Arrange & Act
        var cut = Render<TnTCard>(p => p.Add(c => c.ElementId, "card-id"));
        // Assert
        cut.Find("div").GetAttribute("id")!.Should().Be("card-id");
    }

    [Fact]
    public void ElementLang_Renders_Lang_Attribute() {
        // Arrange & Act
        var cut = Render<TnTCard>(p => p.Add(c => c.ElementLang, "fr"));
        // Assert
        cut.Find("div").GetAttribute("lang")!.Should().Be("fr");
    }

    [Fact]
    public void ElementTitle_Renders_Title_Attribute() {
        // Arrange & Act
        var cut = Render<TnTCard>(p => p.Add(c => c.ElementTitle, "Tooltip"));
        // Assert
        cut.Find("div").GetAttribute("title")!.Should().Be("Tooltip");
    }

    [Fact]
    public void Renders_Default_Card_With_Base_And_Elevated_Class() {
        // Arrange & Act
        var cut = Render<TnTCard>(p => p.AddChildContent("Inner"));
        var div = cut.Find("div.tnt-card");
        var cls = div.GetAttribute("class")!;
        // Assert
        cls.Should().Contain("tnt-card");
        cls.Should().Contain("tnt-components");
        cls.Should().Contain("tnt-card-elevated");
        cut.Markup.Should().Contain("Inner");
    }
}
#pragma warning restore CS0618
