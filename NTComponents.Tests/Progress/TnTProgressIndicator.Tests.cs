namespace NTComponents.Tests.Progress;

/// <summary>
///     Unit tests for <see cref="TnTProgressIndicator" />.
/// </summary>
public class TnTProgressIndicator_Tests : BunitContext {

    [Fact]
    public void Additional_Attributes_Are_Applied_To_Progress_Element() {
        // Arrange
        var attrs = new Dictionary<string, object>
        {
            { "data-testid", "progress-indicator" },
            { "role", "progressbar" }
        };

        // Act
        var cut = Render<TnTProgressIndicator>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var progressElement = cut.Find("progress");

        // Assert
        progressElement.GetAttribute("data-testid")!.Should().Be("progress-indicator");
        progressElement.GetAttribute("role")!.Should().Be("progressbar");
    }

    [Fact]
    public void Auto_Focus_False_Does_Not_Set_Autofocus_Attribute() {
        // Arrange & Act
        var cut = Render<TnTProgressIndicator>(p => p.Add(c => c.AutoFocus, false));
        var progressElement = cut.Find("progress");

        // Assert
        progressElement.HasAttribute("autofocus").Should().BeFalse();
    }

    [Fact]
    public void Auto_Focus_Null_Does_Not_Set_Autofocus_Attribute() {
        // Arrange & Act
        var cut = Render<TnTProgressIndicator>(p => p.Add(c => c.AutoFocus, null));
        var progressElement = cut.Find("progress");

        // Assert
        progressElement.HasAttribute("autofocus").Should().BeFalse();
    }

    [Fact]
    public void Auto_Focus_True_Sets_Autofocus_Attribute() {
        // Arrange & Act
        var cut = Render<TnTProgressIndicator>(p => p.Add(c => c.AutoFocus, true));
        var progressElement = cut.Find("progress");

        // Assert
        progressElement.HasAttribute("autofocus").Should().BeTrue();
    }

    [Fact]
    public void Child_Content_Renders_Inside_Progress_Element() {
        // Arrange & Act
        var cut = Render<TnTProgressIndicator>(p => p.AddChildContent("Loading..."));
        var progressElement = cut.Find("progress");

        // Assert
        progressElement.InnerHtml.Should().Contain("Loading...");
    }

    [Fact]
    public void Complex_Scenario_With_Multiple_Parameters() {
        // Arrange & Act
        var cut = Render<TnTProgressIndicator>(p => p
            .Add(c => c.Appearance, ProgressAppearance.Linear)
            .Add(c => c.Size, Size.Large)
            .Add(c => c.ProgressColor, TnTColor.Success)
            .Add(c => c.Value, 75.0)
            .Add(c => c.Max, 100.0)
            .Add(c => c.ElementId, "test-progress")
            .Add(c => c.ElementTitle, "Test Progress")
            .AddChildContent("75%"));

        var progressElement = cut.Find("progress");
        var cls = progressElement.GetAttribute("class")!;
        var style = progressElement.GetAttribute("style")!;

        // Assert
        cls.Should().Contain("tnt-progress-linear");
        cls.Should().Contain("tnt-size-l");
        style.Should().Contain("--progress-color:var(--tnt-color-success)");
        progressElement.GetAttribute("value")!.Should().Be("75");
        progressElement.GetAttribute("max")!.Should().Be("100");
        progressElement.GetAttribute("id")!.Should().Be("test-progress");
        progressElement.GetAttribute("title")!.Should().Be("Test Progress");
        progressElement.InnerHtml.Should().Contain("75%");
    }

    [Fact]
    public void Component_Identifier_Is_Unique() {
        // Arrange & Act
        var cut1 = Render<TnTProgressIndicator>();
        var cut2 = Render<TnTProgressIndicator>();

        // Assert
        cut1.Instance.ComponentIdentifier.Should().NotBe(cut2.Instance.ComponentIdentifier);
        cut1.Instance.ComponentIdentifier.Should().NotBeNullOrEmpty();
        cut2.Instance.ComponentIdentifier.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Component_Inherits_From_NTComponentBase() {
        // Arrange & Act
        var cut = Render<TnTProgressIndicator>();

        // Assert
        cut.Instance.Should().BeAssignableTo<NTComponents.Core.NTComponentBase>();
    }

    [Fact]
    public void Conic_Gradient_Calculates_Correct_Degrees() {
        // Arrange & Act
        var cut = Render<TnTProgressIndicator>(p => p.Add(c => c.Value, 25.0).Add(c => c.Max, 100.0));
        var progressElement = cut.Find("progress");
        var style = progressElement.GetAttribute("style")!;

        // Assert 25/100 * 360 = 90 degrees
        style.Should().Contain("background:conic-gradient(from 0deg, currentColor 90deg, transparent 90deg)");
    }

    [Theory]
    [InlineData(0.0, 100.0, "0deg")]
    [InlineData(25.0, 100.0, "90deg")]
    [InlineData(50.0, 100.0, "180deg")]
    [InlineData(75.0, 100.0, "270deg")]
    [InlineData(100.0, 100.0, "360deg")]
    [InlineData(50.0, 200.0, "90deg")]
    public void Conic_Gradient_Calculates_Correct_Degrees_For_Various_Values(double value, double max, string expectedDegrees) {
        // Arrange & Act
        var cut = Render<TnTProgressIndicator>(p => p.Add(c => c.Value, value).Add(c => c.Max, max));
        var progressElement = cut.Find("progress");
        var style = progressElement.GetAttribute("style")!;

        // Assert
        style.Should().Contain($"background:conic-gradient(from 0deg, currentColor {expectedDegrees}, transparent {expectedDegrees})");
    }

    [Fact]
    public void Custom_Max_Value_Sets_Max_Attribute() {
        // Arrange & Act
        var cut = Render<TnTProgressIndicator>(p => p.Add(c => c.Max, 50.0));
        var progressElement = cut.Find("progress");

        // Assert
        progressElement.GetAttribute("max")!.Should().Be("50");
    }

    [Fact]
    public void Default_Appearance_Is_Ring() {
        // Arrange & Act
        var cut = Render<TnTProgressIndicator>();
        var progressElement = cut.Find("progress");

        // Assert
        progressElement.GetAttribute("class")!.Should().NotContain("tnt-progress-linear");
    }

    [Fact]
    public void Default_Max_Value_Is_100() {
        // Arrange & Act
        var cut = Render<TnTProgressIndicator>();
        var progressElement = cut.Find("progress");

        // Assert
        progressElement.GetAttribute("max")!.Should().Be("100");
    }

    [Fact]
    public void Default_Progress_Color_Is_Primary() {
        // Arrange & Act
        var cut = Render<TnTProgressIndicator>();
        var progressElement = cut.Find("progress");
        var style = progressElement.GetAttribute("style")!;

        // Assert
        style.Should().Contain("--progress-color:var(--tnt-color-primary)");
    }

    [Fact]
    public void Element_Id_Sets_Id_Attribute() {
        // Arrange & Act
        var cut = Render<TnTProgressIndicator>(p => p.Add(c => c.ElementId, "progress-1"));
        var progressElement = cut.Find("progress");

        // Assert
        progressElement.GetAttribute("id")!.Should().Be("progress-1");
    }

    [Fact]
    public void Element_Lang_Sets_Lang_Attribute() {
        // Arrange & Act
        var cut = Render<TnTProgressIndicator>(p => p.Add(c => c.ElementLang, "en"));
        var progressElement = cut.Find("progress");

        // Assert
        progressElement.GetAttribute("lang")!.Should().Be("en");
    }

    [Fact]
    public void Element_Reference_Is_Set_After_Render() {
        // Arrange & Act
        var cut = Render<TnTProgressIndicator>();

        // Assert
        cut.Instance.Element.Should().NotBeNull();
    }

    [Fact]
    public void Element_Title_Sets_Title_Attribute() {
        // Arrange & Act
        var cut = Render<TnTProgressIndicator>(p => p.Add(c => c.ElementTitle, "Progress tooltip"));
        var progressElement = cut.Find("progress");

        // Assert
        progressElement.GetAttribute("title")!.Should().Be("Progress tooltip");
    }

    [Fact]
    public void Has_Default_Classes() {
        // Arrange & Act
        var cut = Render<TnTProgressIndicator>();
        var progressElement = cut.Find("progress");
        var cls = progressElement.GetAttribute("class")!;

        // Assert
        cls.Should().Contain("tnt-components");
        cls.Should().Contain("tnt-size-m"); // Default medium size
    }

    [Fact]
    public void Has_TnTId_Attribute() {
        // Arrange & Act
        var cut = Render<TnTProgressIndicator>();
        var progressElement = cut.Find("progress");

        // Assert
        progressElement.HasAttribute("tntid").Should().BeTrue();
        progressElement.GetAttribute("tntid").Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Linear_Appearance_Adds_Linear_Class() {
        // Arrange & Act
        var cut = Render<TnTProgressIndicator>(p => p.Add(c => c.Appearance, ProgressAppearance.Linear));
        var progressElement = cut.Find("progress");

        // Assert
        progressElement.GetAttribute("class")!.Should().Contain("tnt-progress-linear");
    }

    [Fact]
    public void Merges_Custom_Class_From_Additional_Attributes() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "class", "custom-progress" } };

        // Act
        var cut = Render<TnTProgressIndicator>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var progressElement = cut.Find("progress");
        var cls = progressElement.GetAttribute("class")!;

        // Assert
        cls.Should().Contain("custom-progress");
        cls.Should().Contain("tnt-components");
    }

    [Fact]
    public void Merges_Custom_Style_From_Additional_Attributes() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "style", "margin: 10px;" } };

        // Act
        var cut = Render<TnTProgressIndicator>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var progressElement = cut.Find("progress");
        var style = progressElement.GetAttribute("style")!;

        // Assert
        style.Should().Contain("margin: 10px;");
        style.Should().Contain("--progress-color:var(--tnt-color-primary)");
    }

    [Fact]
    public void Progress_Appearance_Enum_Has_Expected_Values() {
        // Arrange & Act & Assert
        var ringValue = ProgressAppearance.Ring;
        var linearValue = ProgressAppearance.Linear;

        ringValue.Should().Be(ProgressAppearance.Ring);
        linearValue.Should().Be(ProgressAppearance.Linear);
    }

    [Theory]
    [InlineData(TnTColor.Primary, "--progress-color:var(--tnt-color-primary)")]
    [InlineData(TnTColor.Secondary, "--progress-color:var(--tnt-color-secondary)")]
    [InlineData(TnTColor.Tertiary, "--progress-color:var(--tnt-color-tertiary)")]
    [InlineData(TnTColor.Success, "--progress-color:var(--tnt-color-success)")]
    [InlineData(TnTColor.Warning, "--progress-color:var(--tnt-color-warning)")]
    [InlineData(TnTColor.Error, "--progress-color:var(--tnt-color-error)")]
    public void Progress_Color_Sets_Correct_CSS_Variable(TnTColor color, string expectedStyleVariable) {
        // Arrange & Act
        var cut = Render<TnTProgressIndicator>(p => p.Add(c => c.ProgressColor, color));
        var progressElement = cut.Find("progress");
        var style = progressElement.GetAttribute("style")!;

        // Assert
        style.Should().Contain(expectedStyleVariable);
    }

    [Fact]
    public void Renders_Progress_Element_By_Default() {
        // Arrange & Act
        var cut = Render<TnTProgressIndicator>();

        // Assert
        var progressElement = cut.Find("progress");
        progressElement.Should().NotBeNull();
        progressElement.TagName.Should().Be("PROGRESS");
    }

    [Fact]
    public void Ring_Appearance_Does_Not_Add_Linear_Class() {
        // Arrange & Act
        var cut = Render<TnTProgressIndicator>(p => p.Add(c => c.Appearance, ProgressAppearance.Ring));
        var progressElement = cut.Find("progress");

        // Assert
        progressElement.GetAttribute("class")!.Should().NotContain("tnt-progress-linear");
    }

    [Fact]
    public void Show_False_Does_Not_Render_Progress_Element() {
        // Arrange & Act
        var cut = Render<TnTProgressIndicator>(p => p.Add(c => c.Show, false));

        // Assert
        cut.FindAll("progress").Should().BeEmpty();
        cut.Markup.Should().BeEmpty();
    }

    [Fact]
    public void Show_True_Renders_Progress_Element() {
        // Arrange & Act
        var cut = Render<TnTProgressIndicator>(p => p.Add(c => c.Show, true));

        // Assert
        cut.FindAll("progress").Should().HaveCount(1);
    }

    [Theory]
    [InlineData(Size.Smallest, "tnt-size-xs")]
    [InlineData(Size.Small, "tnt-size-s")]
    [InlineData(Size.Medium, "tnt-size-m")]
    [InlineData(Size.Large, "tnt-size-l")]
    [InlineData(Size.Largest, "tnt-size-xl")]
    public void Size_Adds_Correct_Size_Class(Size size, string expectedClass) {
        // Arrange & Act
        var cut = Render<TnTProgressIndicator>(p => p.Add(c => c.Size, size));
        var progressElement = cut.Find("progress");

        // Assert
        progressElement.GetAttribute("class")!.Should().Contain(expectedClass);
    }

    [Fact]
    public void Value_Adds_Conic_Gradient_Background_Style() {
        // Arrange & Act
        var cut = Render<TnTProgressIndicator>(p => p.Add(c => c.Value, 50.0).Add(c => c.Max, 100.0));
        var progressElement = cut.Find("progress");
        var style = progressElement.GetAttribute("style")!;

        // Assert
        style.Should().Contain("background:conic-gradient(from 0deg, currentColor 180deg, transparent 180deg)");
    }

    [Fact]
    public void Value_Null_Does_Not_Add_Conic_Gradient_Background_Style() {
        // Arrange & Act
        var cut = Render<TnTProgressIndicator>(p => p.Add(c => c.Value, null));
        var progressElement = cut.Find("progress");
        var style = progressElement.GetAttribute("style")!;

        // Assert
        style.Should().NotContain("background:conic-gradient");
    }

    [Fact]
    public void Value_Null_Does_Not_Set_Value_Attribute() {
        // Arrange & Act
        var cut = Render<TnTProgressIndicator>(p => p.Add(c => c.Value, null));
        var progressElement = cut.Find("progress");

        // Assert
        progressElement.HasAttribute("value").Should().BeFalse();
    }

    [Fact]
    public void Value_Set_Adds_Value_Attribute() {
        // Arrange & Act
        var cut = Render<TnTProgressIndicator>(p => p.Add(c => c.Value, 25.0));
        var progressElement = cut.Find("progress");

        // Assert
        progressElement.GetAttribute("value")!.Should().Be("25");
    }
}