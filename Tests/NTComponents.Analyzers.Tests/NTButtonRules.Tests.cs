namespace NTComponents.Analyzers.Tests;

public sealed class NTButtonRules_Tests {

    [Theory]
    [InlineData("")]
    [InlineData("builder.AddComponentParameter(1, \"Label\", null);")]
    [InlineData("builder.AddComponentParameter(1, \"Label\", \"  \");")]
    public async Task Reports_Missing_Or_Blank_Label(string label) {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync($$"""
            builder.OpenComponent<NTButton>(0);
            {{label}}
            builder.CloseComponent();
            """);

        Assert.Equal(["NTC1008"], diagnostics.Ids());
    }

    [Theory]
    [InlineData("builder.AddComponentParameter(1, \"Label\", DateTime.Now.ToString());")]
    [InlineData("builder.AddMultipleAttributes(1, new System.Collections.Generic.Dictionary<string, object>());")]
    [InlineData("builder.AddComponentParameter(1, string.Concat(\"La\", DateTime.Now.Year.ToString()), \"Save\");")]
    public async Task DoesNotReport_Label_The_Analyzer_Cannot_See(string label) {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync($$"""
            builder.OpenComponent<NTButton>(0);
            {{label}}
            builder.CloseComponent();
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Reports_Text_Toggle() {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync("""
            builder.OpenComponent<NTButton>(0);
            builder.AddComponentParameter(1, "Label", "Save");
            builder.AddComponentParameter(2, "Variant", NTButtonVariant.Text);
            builder.AddComponentParameter(3, "IsToggleButton", true);
            builder.CloseComponent();
            """);

        Assert.Equal(["NTC1007"], diagnostics.Ids());
    }

    [Theory]
    [InlineData("Text", "Primary", "NTC1003")]
    [InlineData("Outlined", "Primary", "NTC1003")]
    [InlineData("Filled", "Transparent", "NTC1004")]
    [InlineData("Tonal", "Transparent", "NTC1004")]
    [InlineData("Elevated", "Transparent", "NTC1004")]
    public async Task Reports_Background_That_Does_Not_Match_The_Variant(string variant, string color, string expectedId) {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync($$"""
            builder.OpenComponent<NTButton>(0);
            builder.AddComponentParameter(1, "Label", "Save");
            builder.AddComponentParameter(2, "Variant", NTButtonVariant.{{variant}});
            builder.AddComponentParameter(3, "BackgroundColor", NTColor.{{color}});
            builder.CloseComponent();
            """);

        Assert.Equal([expectedId], diagnostics.Ids());
    }

    [Fact]
    public async Task Selected_Outlined_Toggle_Requires_A_Visible_Background() {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync("""
            builder.OpenComponent<NTButton>(0);
            builder.AddComponentParameter(1, "Label", "Save");
            builder.AddComponentParameter(2, "Variant", NTButtonVariant.Outlined);
            builder.AddComponentParameter(3, "IsToggleButton", true);
            builder.AddComponentParameter(4, "Selected", true);
            builder.AddComponentParameter(5, "BackgroundColor", NTColor.Transparent);
            builder.CloseComponent();
            builder.OpenComponent<NTButton>(6);
            builder.AddComponentParameter(7, "Label", "Save");
            builder.AddComponentParameter(8, "Variant", NTButtonVariant.Outlined);
            builder.AddComponentParameter(9, "IsToggleButton", true);
            builder.AddComponentParameter(10, "Selected", true);
            builder.AddComponentParameter(11, "BackgroundColor", NTColor.InverseSurface);
            builder.CloseComponent();
            """);

        Assert.Equal(["NTC1004"], diagnostics.Ids());
    }

    [Theory]
    [InlineData("Elevated", "None")]
    [InlineData("Filled", "Low")]
    public async Task Reports_Elevation_That_Does_Not_Match_The_Variant(string variant, string elevation) {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync($$"""
            builder.OpenComponent<NTButton>(0);
            builder.AddComponentParameter(1, "Label", "Save");
            builder.AddComponentParameter(2, "Variant", NTButtonVariant.{{variant}});
            builder.AddComponentParameter(3, "Elevation", NTElevation.{{elevation}});
            builder.CloseComponent();
            """);

        Assert.Equal(["NTC1006"], diagnostics.Ids());
    }

    [Fact]
    public async Task Reports_Transparent_Text_Color() {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync("""
            builder.OpenComponent<NTButton>(0);
            builder.AddComponentParameter(1, "Label", "Save");
            builder.AddComponentParameter(2, "TextColor", NTColor.Transparent);
            builder.CloseComponent();
            """);

        Assert.Equal(["NTC1005"], diagnostics.Ids());
    }

    [Fact]
    public async Task Null_Colors_And_Elevation_Use_Valid_Defaults() {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync("""
            builder.OpenComponent<NTButton>(0);
            builder.AddComponentParameter(1, "Label", "Save");
            builder.AddComponentParameter(2, "Variant", NTButtonVariant.Elevated);
            builder.AddComponentParameter(3, "BackgroundColor", (NTColor?)null);
            builder.AddComponentParameter(4, "TextColor", (NTColor?)null);
            builder.AddComponentParameter(5, "Elevation", (NTElevation?)null);
            builder.CloseComponent();
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task DoesNotReport_Values_Known_Only_At_Runtime() {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync("""
            var variant = DateTime.Now.Second > 30 ? NTButtonVariant.Text : NTButtonVariant.Filled;
            builder.OpenComponent<NTButton>(0);
            builder.AddComponentParameter(1, "Label", "Save");
            builder.AddComponentParameter(2, "Variant", variant);
            builder.AddComponentParameter(3, "BackgroundColor", NTColor.Transparent);
            builder.AddComponentParameter(4, "Elevation", NTElevation.Low);
            builder.AddComponentParameter(5, "IsToggleButton", true);
            builder.CloseComponent();
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Reads_Razor_TypeCheck_Wrappers_And_Typeof_Opens() {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync("""
            builder.OpenComponent(0, typeof(NTButton));
            builder.AddComponentParameter(1, "Label", Microsoft.AspNetCore.Components.CompilerServices.RuntimeHelpers.TypeCheck<string>(((" "))));
            builder.AddComponentParameter(2, "Variant", Microsoft.AspNetCore.Components.CompilerServices.RuntimeHelpers.TypeCheck<NTButtonVariant>(NTButtonVariant.Text));
            builder.AddComponentParameter(3, "BackgroundColor", Microsoft.AspNetCore.Components.CompilerServices.RuntimeHelpers.TypeCheck<NTColor?>(NTColor.Primary));
            builder.CloseComponent();
            """);

        Assert.Equal(["NTC1008", "NTC1003"], diagnostics.Ids());
    }
}
