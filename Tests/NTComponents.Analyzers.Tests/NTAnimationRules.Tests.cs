namespace NTComponents.Analyzers.Tests;

public sealed class NTAnimationRules_Tests {

    [Theory]
    [InlineData("")]
    [InlineData("builder.AddComponentParameter(1, \"Trigger\", NTAnimationTrigger.Visible);")]
    [InlineData("builder.AddComponentParameter(1, \"Trigger\", NTAnimationTrigger.Render);")]
    public async Task Reports_Shown_Unless_The_Trigger_Is_Manual(string trigger) {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync($$"""
            builder.OpenComponent<NTAnimation>(0);
            {{trigger}}
            builder.AddComponentParameter(2, "Shown", true);
            builder.CloseComponent();
            """);

        Assert.Equal(["NTC1080"], diagnostics.Ids());
    }

    [Theory]
    [InlineData("Render")]
    [InlineData("Manual")]
    public async Task Reports_Each_Visible_Only_Parameter_When_The_Trigger_Is_Not_Visible(string trigger) {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync($$"""
            builder.OpenComponent<NTAnimation>(0);
            builder.AddComponentParameter(1, "Trigger", NTAnimationTrigger.{{trigger}});
            builder.AddComponentParameter(2, "Repeat", NTAnimationRepeat.Replay);
            builder.AddComponentParameter(3, "Threshold", 0.5);
            builder.AddComponentParameter(4, "RootMargin", "10px");
            builder.AddComponentParameter(5, "OnEntered", default(Microsoft.AspNetCore.Components.EventCallback));
            builder.AddComponentParameter(6, "OnExited", default(Microsoft.AspNetCore.Components.EventCallback));
            builder.CloseComponent();
            """);

        Assert.Equal(["NTC1081", "NTC1081", "NTC1081", "NTC1081", "NTC1081"], diagnostics.Ids());
        Assert.Contains("Repeat", diagnostics[0].GetMessage(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("1.5")]
    [InlineData("-0.1")]
    [InlineData("double.NaN")]
    public async Task Reports_Threshold_Outside_Zero_To_One(string threshold) {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync($$"""
            builder.OpenComponent<NTAnimation>(0);
            builder.AddComponentParameter(1, "Threshold", {{threshold}});
            builder.CloseComponent();
            """);

        Assert.Equal(["NTC1082"], diagnostics.Ids());
    }

    [Fact]
    public async Task DoesNotReport_Parameters_That_Match_The_Trigger() {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync("""
            builder.OpenComponent<NTAnimation>(0);
            builder.AddComponentParameter(1, "Repeat", NTAnimationRepeat.Replay);
            builder.AddComponentParameter(2, "Threshold", 1d);
            builder.AddComponentParameter(3, "RootMargin", "10px");
            builder.CloseComponent();
            builder.OpenComponent<NTAnimation>(4);
            builder.AddComponentParameter(5, "Trigger", NTAnimationTrigger.Manual);
            builder.AddComponentParameter(6, "Shown", false);
            builder.CloseComponent();
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task DoesNotReport_A_Trigger_Known_Only_At_Runtime() {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync("""
            var trigger = Enum.Parse<NTAnimationTrigger>("Render");
            builder.OpenComponent<NTAnimation>(0);
            builder.AddComponentParameter(1, "Trigger", trigger);
            builder.AddComponentParameter(2, "Shown", true);
            builder.AddComponentParameter(3, "Repeat", NTAnimationRepeat.Replay);
            builder.CloseComponent();
            builder.OpenComponent<NTAnimation>(4);
            builder.AddComponentParameter(5, "Threshold", (double)DateTime.Now.Year);
            builder.CloseComponent();
            """);

        Assert.Empty(diagnostics);
    }
}
