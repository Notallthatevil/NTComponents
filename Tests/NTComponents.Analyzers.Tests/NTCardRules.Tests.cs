namespace NTComponents.Analyzers.Tests;

public sealed class NTCardRules_Tests {

    [Theory]
    [InlineData("", "Filled")]
    [InlineData("builder.AddComponentParameter(1, \"Variant\", NTCardVariant.Filled);", "Filled")]
    [InlineData("builder.AddComponentParameter(1, \"Variant\", NTCardVariant.Elevated);", "Elevated")]
    public async Task Reports_Transparent_Background_On_Filled_And_Elevated(string variantParameter, string expectedVariant) {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync($$"""
            builder.OpenComponent<NTCard>(0);
            {{variantParameter}}
            builder.AddComponentParameter(2, "BackgroundColor", NTColor.Transparent);
            builder.CloseComponent();
            """);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("NTC1001", diagnostic.Id);
        Assert.Contains($"'{expectedVariant}'", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task DoesNotReport_Transparent_Background_On_Outlined() {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync("""
            builder.OpenComponent<NTCard>(0);
            builder.AddComponentParameter(1, "Variant", NTCardVariant.Outlined);
            builder.AddComponentParameter(2, "BackgroundColor", NTColor.Transparent);
            builder.CloseComponent();
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Null_Background_Means_The_Variant_Default_Not_Transparent() {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync("""
            builder.OpenComponent<NTCard>(0);
            builder.AddComponentParameter(1, "BackgroundColor", (NTColor?)null);
            builder.CloseComponent();
            builder.OpenComponent<NTCard>(2);
            builder.AddComponentParameter(3, "BackgroundColor", Microsoft.AspNetCore.Components.CompilerServices.RuntimeHelpers.TypeCheck<NTColor?>(null));
            builder.CloseComponent();
            builder.OpenComponent<NTCard>(4);
            builder.AddComponentParameter(5, "BackgroundColor", NTColor.Primary);
            builder.CloseComponent();
            """);

        Assert.Empty(diagnostics);
    }

    [Theory]
    [InlineData("Filled")]
    [InlineData("Outlined")]
    public async Task Reports_Elevation_Unless_The_Variant_Is_Elevated(string variant) {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync($$"""
            builder.OpenComponent<NTCard>(0);
            builder.AddComponentParameter(1, "Variant", NTCardVariant.{{variant}});
            builder.AddComponentParameter(2, "Elevation", NTElevation.None);
            builder.CloseComponent();
            """);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("NTC1002", diagnostic.Id);
        Assert.Contains($"'{variant}'", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Reports_Elevation_On_The_Default_Variant() {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync("""
            builder.OpenComponent<NTCard>(0);
            builder.AddComponentParameter(1, "Elevation", NTElevation.None);
            builder.CloseComponent();
            """);

        Assert.Equal(["NTC1002"], diagnostics.Ids());
    }

    [Fact]
    public async Task DoesNotReport_Valid_Cards() {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync("""
            builder.OpenComponent<NTCard>(0);
            builder.CloseComponent();
            builder.OpenComponent<NTCard>(1);
            builder.AddComponentParameter(2, "Variant", NTCardVariant.Elevated);
            builder.AddComponentParameter(3, "Elevation", NTElevation.None);
            builder.AddComponentParameter(4, "BackgroundColor", NTColor.Primary);
            builder.CloseComponent();
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task DoesNotReport_Values_Known_Only_At_Runtime() {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync("""
            var variant = Enum.Parse<NTCardVariant>("Elevated");
            var color = Enum.Parse<NTColor>("Transparent");
            builder.OpenComponent<NTCard>(0);
            builder.AddComponentParameter(1, "Variant", variant);
            builder.AddComponentParameter(2, "BackgroundColor", NTColor.Transparent);
            builder.AddComponentParameter(3, "Elevation", NTElevation.None);
            builder.CloseComponent();
            builder.OpenComponent<NTCard>(4);
            builder.AddComponentParameter(5, "BackgroundColor", color);
            builder.CloseComponent();
            builder.OpenComponent<NTCard>(6);
            builder.AddMultipleAttributes(7, new System.Collections.Generic.Dictionary<string, object>());
            builder.AddComponentParameter(8, "Elevation", NTElevation.None);
            builder.CloseComponent();
            """);

        // A splat could supply Variant, and an unknown Variant or color proves nothing.
        Assert.Empty(diagnostics);
    }
}
