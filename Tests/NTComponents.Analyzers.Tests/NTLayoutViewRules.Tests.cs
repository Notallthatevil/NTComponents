namespace NTComponents.Analyzers.Tests;

public sealed class NTLayoutViewRules_Tests {

    [Fact]
    public async Task Reports_Skeleton_Corner_Radius_On_A_Circle() {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync("""
            builder.OpenComponent<NTSkeleton>(0);
            builder.AddComponentParameter(1, "Shape", NTSkeletonShape.Circle);
            builder.AddComponentParameter(2, "CornerRadius", NTCornerRadius.Small);
            builder.CloseComponent();
            builder.OpenComponent<NTSkeleton>(3);
            builder.AddComponentParameter(4, "CornerRadius", NTCornerRadius.Small);
            builder.CloseComponent();
            """);

        Assert.Equal(["NTC1102"], diagnostics.Ids());
    }

    [Theory]
    [InlineData(0, "PaneCount value 0 is changed to 1")]
    [InlineData(7, "PaneCount value 7 is changed to 5")]
    public async Task Reports_Pane_Counts_Outside_One_To_Five(int count, string message) {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync($$"""
            builder.OpenComponent<NTMultiPaneView>(0);
            builder.AddComponentParameter(1, "PaneCount", {{count}});
            builder.CloseComponent();
            builder.OpenComponent<NTMultiPaneView>(2);
            builder.AddComponentParameter(3, "PaneCount", 3);
            builder.CloseComponent();
            """);

        Assert.Contains(message, Assert.Single(diagnostics).GetMessage());
    }

    [Fact]
    public async Task Reports_A_Theme_Toggle_With_Nothing_To_Toggle() {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync("""
            builder.OpenComponent<NTThemeToggle>(0);
            builder.AddComponentParameter(1, "AllowThemeSelection", false);
            builder.AddComponentParameter(2, "AllowContrastSelection", false);
            builder.CloseComponent();
            builder.OpenComponent<NTThemeToggle>(3);
            builder.AddComponentParameter(4, "AllowThemeSelection", false);
            builder.CloseComponent();
            """);

        Assert.Equal(["NTC1102"], diagnostics.Ids());
    }

    [Fact]
    public async Task Reports_On_This_Page_Parameters_When_Navigation_Is_Disabled() {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync("""
            var enabled = DateTime.Now.Second > 30;
            builder.OpenComponent<NTContainerView>(0);
            builder.AddComponentParameter(1, "EnableOnThisPageNavigation", false);
            builder.AddComponentParameter(2, "OnThisPageLabel", "Contents");
            builder.AddComponentParameter(3, "OnThisPageTextColor", NTColor.Primary);
            builder.CloseComponent();
            builder.OpenComponent<NTContainerView>(4);
            builder.AddComponentParameter(5, "EnableOnThisPageNavigation", enabled);
            builder.AddComponentParameter(6, "OnThisPageLabel", "Contents");
            builder.CloseComponent();
            """);

        Assert.Equal(["NTC1102", "NTC1102"], diagnostics.Ids());
    }
}
