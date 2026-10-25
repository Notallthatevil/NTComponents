namespace NTComponents.Analyzers.Tests;

public sealed class NTCarouselRules_Tests {

    [Theory]
    [InlineData("")]
    [InlineData("builder.AddComponentParameter(1, \"AriaLabel\", null);")]
    [InlineData("builder.AddComponentParameter(1, \"AriaLabel\", \"  \");")]
    public async Task Reports_Missing_Or_Blank_Carousel_Label(string label) {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync($$"""
            builder.OpenComponent<NTCarousel>(0);
            {{label}}
            builder.CloseComponent();
            """);

        Assert.Equal(["NTC1069"], diagnostics.Ids());
    }

    [Theory]
    [InlineData("")]
    [InlineData("builder.AddComponentParameter(1, \"AriaLabel\", \"\");")]
    public async Task Reports_Missing_Or_Blank_Item_Label(string label) {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync($$"""
            builder.OpenComponent<NTCarouselItem>(0);
            {{label}}
            builder.CloseComponent();
            """);

        Assert.Equal(["NTC1072"], diagnostics.Ids());
    }

    [Theory]
    [InlineData("AutoPlayInterval", "0d")]
    [InlineData("AutoPlayInterval", "-1d")]
    [InlineData("AutoPlayInterval", "double.NaN")]
    [InlineData("AutoPlayInterval", "double.PositiveInfinity")]
    [InlineData("ItemHeight", "0")]
    [InlineData("MaxLargeItemWidth", "-5")]
    [InlineData("PreferredItemWidth", "0")]
    public async Task Reports_Non_Positive_Numbers(string parameter, string value) {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync($$"""
            builder.OpenComponent<NTCarousel>(0);
            builder.AddComponentParameter(1, "AriaLabel", "Photos");
            builder.AddComponentParameter(2, "{{parameter}}", {{value}});
            builder.CloseComponent();
            """);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("NTC1070", diagnostic.Id);
        Assert.Contains(parameter, diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("0.5")]
    [InlineData("-1d")]
    [InlineData("2d")]
    [InlineData("double.NaN")]
    public async Task Reports_Item_Aspect_Ratio_Outside_The_Material_Range(string ratio) {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync($$"""
            builder.OpenComponent<NTCarouselItem>(0);
            builder.AddComponentParameter(1, "AriaLabel", "Slide");
            builder.AddComponentParameter(2, "AspectRatio", {{ratio}});
            builder.CloseComponent();
            """);

        Assert.Equal(["NTC1073"], diagnostics.Ids());
    }

    [Fact]
    public async Task Reports_Full_Screen_Without_Snapping() {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync("""
            builder.OpenComponent<NTCarousel>(0);
            builder.AddComponentParameter(1, "AriaLabel", "Photos");
            builder.AddComponentParameter(2, "Appearance", CarouselAppearance.FullScreen);
            builder.AddComponentParameter(3, "EnableSnapping", false);
            builder.CloseComponent();
            """);

        Assert.Equal(["NTC1071"], diagnostics.Ids());
    }

    [Fact]
    public async Task Reports_Undefined_Appearance_Only() {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync("""
            builder.OpenComponent<NTCarousel>(0);
            builder.AddComponentParameter(1, "AriaLabel", "Photos");
            builder.AddComponentParameter(2, "Appearance", (CarouselAppearance)99);
            builder.AddComponentParameter(3, "EnableSnapping", false);
            builder.CloseComponent();
            """);

        Assert.Equal(["NTC1074"], diagnostics.Ids());
    }

    [Fact]
    public async Task DoesNotReport_Valid_Configurations() {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync("""
            builder.OpenComponent<NTCarousel>(0);
            builder.AddComponentParameter(1, "AriaLabel", "Photos");
            builder.AddComponentParameter(2, "AutoPlayInterval", 3d);
            builder.AddComponentParameter(3, "MaxLargeItemWidth", 400);
            builder.AddComponentParameter(4, "Appearance", CarouselAppearance.FullScreen);
            builder.CloseComponent();
            builder.OpenComponent<NTCarousel>(5);
            builder.AddComponentParameter(6, "AriaLabel", "Photos");
            builder.AddComponentParameter(7, "Appearance", CarouselAppearance.Hero);
            builder.AddComponentParameter(8, "EnableSnapping", false);
            builder.AddComponentParameter(9, "AutoPlayInterval", (double?)null);
            builder.CloseComponent();
            builder.OpenComponent<NTCarouselItem>(10);
            builder.AddComponentParameter(11, "AriaLabel", "Slide");
            builder.AddComponentParameter(12, "AspectRatio", 1d);
            builder.CloseComponent();
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task DoesNotReport_Values_Known_Only_At_Runtime() {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync("""
            var label = DateTime.Now.ToString();
            var number = DateTime.Now.Year - 5000;
            var appearance = Enum.Parse<CarouselAppearance>("FullScreen");
            builder.OpenComponent<NTCarousel>(0);
            builder.AddComponentParameter(1, "AriaLabel", label);
            builder.AddComponentParameter(2, "ItemHeight", number);
            builder.AddComponentParameter(3, "Appearance", appearance);
            builder.AddComponentParameter(4, "EnableSnapping", false);
            builder.CloseComponent();
            builder.OpenComponent<NTCarouselItem>(5);
            builder.AddComponentParameter(6, "AriaLabel", label);
            builder.AddComponentParameter(7, "AspectRatio", (double)number);
            builder.CloseComponent();
            builder.OpenComponent<NTCarousel>(8);
            builder.AddMultipleAttributes(9, new System.Collections.Generic.Dictionary<string, object>());
            builder.CloseComponent();
            """);

        Assert.Empty(diagnostics);
    }
}
