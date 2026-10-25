namespace NTComponents.Analyzers.Tests;

public sealed class UndefinedEnumRules_Tests {

    [Fact]
    public async Task Reports_Undefined_Enum_Casts_On_NT_Components() {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync("""
            builder.OpenComponent<NTButton>(0);
            builder.AddComponentParameter(1, "Label", "Save");
            builder.AddComponentParameter(2, "ButtonSize", (NTSize)99);
            builder.CloseComponent();
            """);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("NTC1089", diagnostic.Id);
        Assert.Equal("NTButton ButtonSize must be a defined NTSize value", diagnostic.GetMessage());
    }

    [Fact]
    public async Task DoesNotReport_Defined_Null_Or_Runtime_Values() {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync("""
            var size = (NTSize)DateTime.Now.Second;
            builder.OpenComponent<NTButton>(0);
            builder.AddComponentParameter(1, "Label", "Save");
            builder.AddComponentParameter(2, "ButtonSize", NTSize.Large);
            builder.AddComponentParameter(3, "Elevation", (NTElevation?)null);
            builder.AddComponentParameter(4, "Shape", size);
            builder.CloseComponent();
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Leaves_Parameters_With_Dedicated_Rules_To_Them() {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync("""
            builder.OpenComponent<NTCarousel>(0);
            builder.AddComponentParameter(1, "AriaLabel", "Photos");
            builder.AddComponentParameter(2, "Appearance", (CarouselAppearance)99);
            builder.CloseComponent();
            """);

        Assert.DoesNotContain("NTC1089", diagnostics.Ids());
    }
}
