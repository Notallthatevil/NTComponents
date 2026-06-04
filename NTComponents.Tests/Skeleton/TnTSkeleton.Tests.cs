using NTComponents.Core;
using NTComponents.Interfaces;

namespace NTComponents.Tests.Skeleton;

public class TnTSkeleton_Tests : BunitContext {

    [Fact]
    public void AdditionalAttributes_Class_Merged() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "class", "custom-skeleton" } };

        // Act
        var cut = Render<TnTSkeleton>(p => p.Add(s => s.AdditionalAttributes, attrs));
        var div = cut.Find("div.tnt-skeleton");
        var cls = div.GetAttribute("class")!;

        // Assert
        cls.Should().Contain("custom-skeleton");
        cls.Should().Contain("tnt-skeleton");
    }

    [Fact]
    public void AdditionalAttributes_Style_Merged() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "style", "width:200px;" } };

        // Act
        var cut = Render<TnTSkeleton>(p => p.Add(s => s.AdditionalAttributes, attrs));
        var div = cut.Find("div.tnt-skeleton");
        var style = div.GetAttribute("style")!;

        // Assert
        style.Should().Contain("width:200px");
        style.Should().Contain("--tnt-skeleton-bg-color");
    }

    [Fact]
    public void All_Properties_Work_Together() {
        // Arrange
        var attrs = new Dictionary<string, object>
        {
            { "class", "custom-loading" },
            { "style", "height:100px;border:1px solid red;" },
            { "data-component", "skeleton" }
        };

        // Act
        var cut = Render<TnTSkeleton>(p => p
            .Add(s => s.Appearance, SkeletonAppearance.Round)
            .Add(s => s.BackgroundColor, TnTColor.Error)
            .Add(s => s.AnimatedColor, TnTColor.OnError)
            .Add(s => s.ElementId, "main-skeleton")
            .Add(s => s.ElementTitle, "Loading main content")
            .Add(s => s.ElementLang, "en-GB")
            .Add(s => s.AutoFocus, true)
            .Add(s => s.AdditionalAttributes, attrs));

        var div = cut.Find("div.tnt-skeleton");
        var cls = div.GetAttribute("class")!;
        var style = div.GetAttribute("style")!;

        // Assert Classes
        cls.Should().Contain("tnt-skeleton");
        cls.Should().Contain("tnt-skeleton-round");
        cls.Should().Contain("tnt-animated");
        cls.Should().Contain("custom-loading");
        // Background color classes are no longer generated
        cls.Should().NotContain("tnt-bg-color-error");

        // Styles
        style.Should().Contain("--tnt-skeleton-bg-color:var(--tnt-color-error)");
        style.Should().Contain("--tnt-skeleton-shimmer-color:var(--tnt-color-on-error)");
        style.Should().Contain("height:100px");
        style.Should().Contain("border:1px solid red");

        // Attributes
        div.GetAttribute("id").Should().Be("main-skeleton");
        div.GetAttribute("title").Should().Be("Loading main content");
        div.GetAttribute("lang").Should().Be("en-GB");
        div.HasAttribute("autofocus").Should().BeTrue();
        div.GetAttribute("data-component").Should().Be("skeleton");
        div.HasAttribute("tntid").Should().BeTrue();
    }

    [Fact]
    public void AnimatedColor_Adds_Animated_Class_And_Style_Variable() {
        // Act
        var cut = Render<TnTSkeleton>(p => p.Add(s => s.AnimatedColor, TnTColor.Primary));
        var div = cut.Find("div.tnt-skeleton");
        var style = div.GetAttribute("style")!;

        // Assert
        div.GetAttribute("class")!.Should().Contain("tnt-animated");
        style.Should().Contain("--tnt-skeleton-shimmer-color:var(--tnt-color-primary)");
    }

    [Fact]
    public void AutoFocus_False_Does_Not_Render_Autofocus_Attribute() {
        // Act
        var cut = Render<TnTSkeleton>(p => p.Add(s => s.AutoFocus, false));
        var div = cut.Find("div.tnt-skeleton");

        // Assert
        div.HasAttribute("autofocus").Should().BeFalse();
    }

    [Fact]
    public void AutoFocus_Null_Does_Not_Render_Autofocus_Attribute() {
        // Act
        var cut = Render<TnTSkeleton>(p => p.Add(s => s.AutoFocus, (bool?)null));
        var div = cut.Find("div.tnt-skeleton");

        // Assert
        div.HasAttribute("autofocus").Should().BeFalse();
    }

    [Fact]
    public void AutoFocus_True_Renders_Autofocus_Attribute() {
        // Act
        var cut = Render<TnTSkeleton>(p => p.Add(s => s.AutoFocus, true));
        var div = cut.Find("div.tnt-skeleton");

        // Assert
        div.HasAttribute("autofocus").Should().BeTrue();
    }

    [Fact]
    public void BackgroundColor_Always_Generates_Style_Variable_But_No_Class() {
        // Act
        var cut = Render<TnTSkeleton>(p => p.Add(s => s.BackgroundColor, TnTColor.Surface));
        var div = cut.Find("div.tnt-skeleton");
        var style = div.GetAttribute("style")!;

        // Assert
        style.Should().Contain("--tnt-skeleton-bg-color:var(--tnt-color-surface)");
        // Background color classes are no longer generated
        div.GetAttribute("class")!.Should().NotContain("tnt-bg-color-surface");
    }

    [Fact]
    public void BackgroundColor_None_Generates_Style_Variable_But_No_Class() {
        // Act
        var cut = Render<TnTSkeleton>(p => p.Add(s => s.BackgroundColor, TnTColor.None));
        var div = cut.Find("div.tnt-skeleton");
        var style = div.GetAttribute("style")!;

        // Assert
        style.Should().Contain("--tnt-skeleton-bg-color:var(--tnt-color-none)");
        // Background color classes are no longer generated
        div.GetAttribute("class")!.Should().NotContain("tnt-bg-color-none");
    }

    [Fact]
    public void Both_Colors_Set_Adds_Both_Style_Variables() {
        // Act
        var cut = Render<TnTSkeleton>(p => p
            .Add(s => s.BackgroundColor, TnTColor.Surface)
            .Add(s => s.AnimatedColor, TnTColor.OnSurface));
        var div = cut.Find("div.tnt-skeleton");
        var style = div.GetAttribute("style")!;

        // Assert
        style.Should().Contain("--tnt-skeleton-bg-color:var(--tnt-color-surface)");
        style.Should().Contain("--tnt-skeleton-shimmer-color:var(--tnt-color-on-surface)");
        div.GetAttribute("class")!.Should().Contain("tnt-animated");
        // Background color classes are no longer generated
        div.GetAttribute("class")!.Should().NotContain("tnt-bg-color-surface");
    }

    [Fact]
    public void Component_Identifier_Is_Set() {
        // Act
        var cut = Render<TnTSkeleton>();

        // Assert
        cut.Instance.ComponentIdentifier.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Component_Implements_INTComponentBase() {
        // Act
        var cut = Render<TnTSkeleton>();

        // Assert
        cut.Instance.Should().BeAssignableTo<INTComponentBase>();
    }

    [Fact]
    public void Component_Inherits_From_NTComponentBase() {
        // Act
        var cut = Render<TnTSkeleton>();

        // Assert
        cut.Instance.Should().BeAssignableTo<NTComponentBase>();
    }

    [Fact]
    public void Custom_BackgroundColor_Sets_Style_Variable_Only() {
        // Act
        var cut = Render<TnTSkeleton>(p => p.Add(s => s.BackgroundColor, TnTColor.Secondary));
        var div = cut.Find("div.tnt-skeleton");
        var style = div.GetAttribute("style")!;

        // Assert
        style.Should().Contain("--tnt-skeleton-bg-color:var(--tnt-color-secondary)");
        // Background color classes are no longer generated
        div.GetAttribute("class")!.Should().NotContain("tnt-bg-color-secondary");
    }

    [Fact]
    public void Default_AnimatedColor_Is_OnPrimaryContainer() {
        // Act
        var cut = Render<TnTSkeleton>();

        // Assert
        cut.Instance.AnimatedColor.Should().Be(TnTColor.OnPrimaryContainer);
    }

    [Fact]
    public void Default_Appearance_Is_Square() {
        // Act
        var cut = Render<TnTSkeleton>();
        var div = cut.Find("div.tnt-skeleton");

        // Assert
        cut.Instance.Appearance.Should().Be(SkeletonAppearance.Square);
        div.GetAttribute("class")!.Should().NotContain("tnt-skeleton-round");
    }

    [Fact]
    public void Default_BackgroundColor_Is_PrimaryContainer() {
        // Act
        var cut = Render<TnTSkeleton>();
        var div = cut.Find("div.tnt-skeleton");
        var style = div.GetAttribute("style")!;

        // Assert
        cut.Instance.BackgroundColor.Should().Be(TnTColor.PrimaryContainer);
        style.Should().Contain("--tnt-skeleton-bg-color:var(--tnt-color-primary-container)");
        // Removed background color class assertion as it's no longer generated
    }

    [Fact]
    public void Default_Values_With_Animation_Enabled() {
        // Act - Using default values which includes AnimatedColor
        var cut = Render<TnTSkeleton>();
        var div = cut.Find("div.tnt-skeleton");
        var cls = div.GetAttribute("class")!;
        var style = div.GetAttribute("style")!;

        // Assert
        cls.Should().Contain("tnt-animated", "because AnimatedColor has a default value");
        style.Should().Contain("--tnt-skeleton-shimmer-color:var(--tnt-color-on-primary-container)");
    }

    [Fact]
    public void Element_Has_No_Content() {
        // Act
        var cut = Render<TnTSkeleton>();
        var div = cut.Find("div.tnt-skeleton");

        // Assert
        div.TextContent.Should().BeEmpty("because skeleton should be a self-contained placeholder");
    }

    [Fact]
    public void Element_Reference_Captured() {
        // Act
        var cut = Render<TnTSkeleton>();

        // Assert
        cut.Find("div.tnt-skeleton").Should().NotBeNull();
    }

    [Fact]
    public void ElementId_Sets_Id_Attribute() {
        // Act
        var cut = Render<TnTSkeleton>(p => p.Add(s => s.ElementId, "skeleton-id"));
        var div = cut.Find("div.tnt-skeleton");

        // Assert
        div.GetAttribute("id").Should().Be("skeleton-id");
    }

    [Fact]
    public void ElementLang_Sets_Lang_Attribute() {
        // Act
        var cut = Render<TnTSkeleton>(p => p.Add(s => s.ElementLang, "en-US"));
        var div = cut.Find("div.tnt-skeleton");

        // Assert
        div.GetAttribute("lang").Should().Be("en-US");
    }

    [Fact]
    public void ElementTitle_Sets_Title_Attribute() {
        // Act
        var cut = Render<TnTSkeleton>(p => p.Add(s => s.ElementTitle, "Loading content"));
        var div = cut.Find("div.tnt-skeleton");

        // Assert
        div.GetAttribute("title").Should().Be("Loading content");
    }

    [Fact]
    public void Has_TnTId_Attribute() {
        // Act
        var cut = Render<TnTSkeleton>();
        var div = cut.Find("div.tnt-skeleton");

        // Assert
        div.HasAttribute("tntid").Should().BeTrue();
        div.GetAttribute("tntid").Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Markup_Structure_Is_Simple_Div() {
        // Act
        var cut = Render<TnTSkeleton>(p => p.Add(s => s.AnimatedColor, (TnTColor?)null));

        // Assert Should contain a single div with basic structure
        cut.FindAll("*").Count.Should().Be(1, "because skeleton should only render one div element");
        cut.Find("div.tnt-skeleton").Should().NotBeNull();
    }

    [Fact]
    public void Multiple_Custom_Attributes_Applied() {
        // Arrange
        var attrs = new Dictionary<string, object>
        {
            { "data-testid", "skeleton" },
            { "role", "progressbar" },
            { "aria-label", "Loading..." }
        };

        // Act
        var cut = Render<TnTSkeleton>(p => p.Add(s => s.AdditionalAttributes, attrs));
        var div = cut.Find("div.tnt-skeleton");

        // Assert
        div.GetAttribute("data-testid").Should().Be("skeleton");
        div.GetAttribute("role").Should().Be("progressbar");
        div.GetAttribute("aria-label").Should().Be("Loading...");
    }

    [Fact]
    public void Null_AnimatedColor_Does_Not_Add_Animated_Class_Or_Style_Variable() {
        // Act
        var cut = Render<TnTSkeleton>(p => p.Add(s => s.AnimatedColor, (TnTColor?)null));
        var div = cut.Find("div.tnt-skeleton");
        var style = div.GetAttribute("style");

        // Assert
        div.GetAttribute("class")!.Should().NotContain("tnt-animated");
        if (style != null) {
            style.Should().NotContain("--tnt-skeleton-shimmer-color");
        }
    }

    [Fact]
    public void Renders_Div_With_Default_Classes() {
        // Act
        var cut = Render<TnTSkeleton>();
        var div = cut.Find("div.tnt-skeleton");

        // Assert
        div.Should().NotBeNull();
        var cls = div.GetAttribute("class")!;
        cls.Should().Contain("tnt-skeleton");
        cls.Should().Contain("tnt-components");
        // Removed background color class assertion as it's no longer generated
    }

    [Fact]
    public void Round_And_Animated_Combines_Classes() {
        // Act
        var cut = Render<TnTSkeleton>(p => p
            .Add(s => s.Appearance, SkeletonAppearance.Round)
            .Add(s => s.AnimatedColor, TnTColor.Tertiary));
        var div = cut.Find("div.tnt-skeleton");
        var cls = div.GetAttribute("class")!;

        // Assert
        cls.Should().Contain("tnt-skeleton-round");
        cls.Should().Contain("tnt-animated");
    }

    [Fact]
    public void Round_Appearance_Adds_Round_Class() {
        // Act
        var cut = Render<TnTSkeleton>(p => p.Add(s => s.Appearance, SkeletonAppearance.Round));
        var div = cut.Find("div.tnt-skeleton");

        // Assert
        div.GetAttribute("class")!.Should().Contain("tnt-skeleton-round");
    }

    [Fact]
    public void Square_Appearance_Does_Not_Add_Round_Class() {
        // Act
        var cut = Render<TnTSkeleton>(p => p.Add(s => s.Appearance, SkeletonAppearance.Square));
        var div = cut.Find("div.tnt-skeleton");

        // Assert
        div.GetAttribute("class")!.Should().NotContain("tnt-skeleton-round");
    }

    [Theory]
    [InlineData(SkeletonAppearance.Square, false)]
    [InlineData(SkeletonAppearance.Round, true)]
    public void Theory_Appearance_Adds_Correct_Classes(SkeletonAppearance appearance, bool shouldHaveRoundClass) {
        // Act
        var cut = Render<TnTSkeleton>(p => p.Add(s => s.Appearance, appearance));
        var div = cut.Find("div.tnt-skeleton");

        // Assert
        var cls = div.GetAttribute("class")!;
        if (shouldHaveRoundClass) {
            cls.Should().Contain("tnt-skeleton-round");
        }
        else {
            cls.Should().NotContain("tnt-skeleton-round");
        }
    }

    [Theory]
    [InlineData(TnTColor.Primary, "primary")]
    [InlineData(TnTColor.Secondary, "secondary")]
    [InlineData(TnTColor.Surface, "surface")]
    [InlineData(TnTColor.Error, "error")]
    [InlineData(TnTColor.None, "none")]
    public void Theory_BackgroundColor_Sets_Style_Variables_But_No_Classes(TnTColor color, string expectedCssClass) {
        // Act
        var cut = Render<TnTSkeleton>(p => p.Add(s => s.BackgroundColor, color));
        var div = cut.Find("div.tnt-skeleton");
        var style = div.GetAttribute("style")!;

        // Assert
        style.Should().Contain($"--tnt-skeleton-bg-color:var(--tnt-color-{expectedCssClass})");

        // Background color classes are no longer generated for any color
        div.GetAttribute("class")!.Should().NotContain($"tnt-bg-color-{expectedCssClass}");
    }
}