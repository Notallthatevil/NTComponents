using NTComponents;
using NTComponents.Interfaces;

namespace NTComponents.Tests.Progress;

/// <summary>
///     Unit tests for <see cref="NTLoader" />.
/// </summary>
public class NTLoader_Tests : BunitContext {
    public NTLoader_Tests() {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Additional_Attributes_Are_Applied_To_Root_Element() {
        var attrs = new Dictionary<string, object> {
            { "data-testid", "loader" },
            { "role", "status" }
        };

        var cut = Render<NTLoader>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var loader = cut.Find(".nt-loader");

        loader.GetAttribute("data-testid")!.Should().Be("loader");
        loader.GetAttribute("role")!.Should().Be("status");
    }

    [Fact]
    public void Aria_LabelledBy_Suppresses_Default_Aria_Label() {
        var cut = Render<NTLoader>(p => p.Add(c => c.AriaLabelledBy, "loading-label"));
        var loader = cut.Find(".nt-loader");

        loader.GetAttribute("aria-labelledby")!.Should().Be("loading-label");
        loader.HasAttribute("aria-label").Should().BeFalse();
    }

    [Fact]
    public void Contained_Variant_Adds_Contained_Class_And_Color_Variable() {
        var cut = Render<NTLoader>(p => p
            .Add(c => c.Variant, NTLoaderVariant.Contained)
            .Add(c => c.ContainerColor, TnTColor.SecondaryContainer));
        var loader = cut.Find(".nt-loader");

        loader.GetAttribute("class")!.Should().Contain("nt-loader-contained");
        loader.GetAttribute("style")!.Should().Contain("--nt-loader-container-color:var(--tnt-color-secondary-container)");
    }

    [Fact]
    public void Custom_Shapes_Are_Rendered_From_Provided_Sequence() {
        var cut = Render<NTLoader>(p => p.Add(c => c.Shapes, new[] {
            NTShapeType.Hexagon,
            NTShapeType.Puffy,
            NTShapeType.Gem
        }));
        var loader = cut.Find(".nt-loader");
        var shape = cut.Find("nt-shape.nt-loader-shape");

        loader.GetAttribute("data-shape-sequence")!.Should().Be("2 28 22");
        loader.GetAttribute("data-shape-interval-ms")!.Should().Be("1250");
        loader.GetAttribute("data-transition-duration-ms")!.Should().Be("700");
        shape.GetAttribute("data-shape")!.Should().Be(((int)NTShapeType.Hexagon).ToString());
    }

    [Fact]
    public void Default_Render_Is_Accessible_Indeterminate_Progressbar() {
        var cut = Render<NTLoader>();
        var loader = cut.Find(".nt-loader");

        loader.GetAttribute("role")!.Should().Be("progressbar");
        loader.GetAttribute("aria-label")!.Should().Be("Loading");
        loader.HasAttribute("aria-valuenow").Should().BeFalse();
    }

    [Fact]
    public void Default_Render_Uses_One_Morphing_Shape_Mark() {
        var cut = Render<NTLoader>();

        var shape = cut.Find("nt-shape.nt-loader-shape");
        shape.Should().NotBeNull();
        shape.GetAttribute("data-animate-shape-changes")!.Should().Be("true");
        cut.FindAll(".nt-progress-fill").Should().BeEmpty();
    }

    [Fact]
    public void Emits_Page_Script_For_Static_And_Interactive_Rendering() {
        var cut = Render<NTLoader>();

        cut.Instance.Should().BeAssignableTo<INTPageScriptComponent<NTLoader>>();
        cut.FindAll("tnt-page-script")
            .Select(script => script.GetAttribute("src"))
            .Should()
            .ContainSingle("./_content/NTComponents/Progress/NTLoader.razor.js");
    }

    [Fact]
    public void Indicator_Color_Sets_Css_Variable() {
        var cut = Render<NTLoader>(p => p.Add(c => c.Color, TnTColor.Tertiary));
        var loader = cut.Find(".nt-loader");

        loader.GetAttribute("style")!.Should().Contain("--nt-loader-indicator-color:var(--tnt-color-tertiary)");
        loader.GetAttribute("style")!.Should().NotContain("--nt-shape-content-background");
    }

    [Fact]
    public void Merges_Custom_Class_And_Style() {
        var attrs = new Dictionary<string, object> {
            { "class", "custom-loader" },
            { "style", "margin: 10px;" }
        };

        var cut = Render<NTLoader>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var loader = cut.Find(".nt-loader");

        loader.GetAttribute("class")!.Should().Contain("custom-loader");
        loader.GetAttribute("style")!.Should().Contain("margin: 10px;");
    }

    [Fact]
    public void Paused_Render_Removes_Animated_Class() {
        var cut = Render<NTLoader>(p => p.Add(c => c.Animate, false));
        var loader = cut.Find(".nt-loader");

        loader.GetAttribute("class")!.Should().NotContain("nt-loader-animated");
        loader.GetAttribute("class")!.Should().NotContain("nt-loader-paused");
        cut.Find("nt-shape.nt-loader-shape").GetAttribute("data-animate-shape-changes")!.Should().Be("false");
    }

    [Fact]
    public void Show_False_Renders_Nothing() {
        var cut = Render<NTLoader>(p => p.Add(c => c.Show, false));

        cut.Markup.Should().BeEmpty();
    }

    [Theory]
    [InlineData(Size.Smallest, "tnt-size-xs")]
    [InlineData(Size.Small, "tnt-size-s")]
    [InlineData(Size.Medium, "tnt-size-m")]
    [InlineData(Size.Large, "tnt-size-l")]
    [InlineData(Size.Largest, "tnt-size-xl")]
    public void Size_Adds_Correct_Size_Class(Size size, string expectedClass) {
        var cut = Render<NTLoader>(p => p.Add(c => c.Size, size));

        cut.Find(".nt-loader").GetAttribute("class")!.Should().Contain(expectedClass);
    }

    [Fact]
    public void Wrapper_Reserves_Rotated_Shape_Box() {
        var cut = Render<NTLoader>();
        var loader = cut.Find(".nt-loader");

        loader.GetAttribute("style")!.Should().NotContain("inline-size");
        loader.GetAttribute("style")!.Should().NotContain("block-size");
        var shape = cut.Find("nt-shape.nt-loader-shape");
        shape.HasAttribute("style").Should().BeFalse();
    }

    [Fact]
    public void Size_Value_Sets_Size_Css_Variable() {
        var cut = Render<NTLoader>(p => p.Add(c => c.SizeValue, "72px"));

        cut.Find(".nt-loader").GetAttribute("style")!.Should().Contain("--nt-loader-size:72px");
    }

    [Fact]
    public void Animation_Duration_Sets_Shape_Interval() {
        var cut = Render<NTLoader>(p => p.Add(c => c.AnimationDuration, TimeSpan.FromMilliseconds(900)));

        cut.Find(".nt-loader").GetAttribute("data-shape-interval-ms")!.Should().Be("900");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public void Animation_Duration_Clamps_Tiny_Intervals(int milliseconds) {
        var cut = Render<NTLoader>(p => p.Add(c => c.AnimationDuration, TimeSpan.FromMilliseconds(milliseconds)));

        cut.Find(".nt-loader").GetAttribute("data-shape-interval-ms")!.Should().Be("400");
    }
}
