namespace NTComponents.Analyzers.Tests;

public sealed class NTProgressRules_Tests {

    [Theory]
    [InlineData("0d")]
    [InlineData("-1d")]
    public async Task Reports_A_Non_Positive_Max(string max) {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync($$"""
            builder.OpenComponent<NTProgress>(0);
            builder.AddComponentParameter(1, "Max", {{max}});
            builder.AddComponentParameter(2, "Value", 500d);
            builder.CloseComponent();
            """);

        Assert.Equal(["NTC1052"], diagnostics.Ids());
    }

    [Theory]
    [InlineData("", "-1d")]
    [InlineData("", "101d")]
    [InlineData("builder.AddComponentParameter(1, \"Max\", 10d);", "11d")]
    public async Task Reports_A_Value_Outside_Zero_To_Max(string max, string value) {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync($$"""
            builder.OpenComponent<NTProgress>(0);
            {{max}}
            builder.AddComponentParameter(2, "Value", {{value}});
            builder.CloseComponent();
            """);

        Assert.Equal(["NTC1053"], diagnostics.Ids());
    }

    [Fact]
    public async Task DoesNotReport_Values_In_Range_Or_Unknown() {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync("""
            var max = DateTime.Now.Year - 2000d;
            builder.OpenComponent<NTProgress>(0);
            builder.AddComponentParameter(1, "Value", 100d);
            builder.CloseComponent();
            builder.OpenComponent<NTProgress>(2);
            builder.AddComponentParameter(3, "Max", max);
            builder.AddComponentParameter(4, "Value", 5000d);
            builder.CloseComponent();
            builder.OpenComponent<NTProgress>(5);
            builder.AddComponentParameter(6, "Value", (double?)null);
            builder.CloseComponent();
            builder.OpenComponent<NTProgress>(7);
            builder.AddMultipleAttributes(8, new System.Collections.Generic.Dictionary<string, object>());
            builder.AddComponentParameter(9, "Value", 5000d);
            builder.CloseComponent();
            """);

        Assert.Empty(diagnostics);
    }

    [Theory]
    [InlineData("TimeSpan.FromMilliseconds(100)")]
    [InlineData("TimeSpan.FromMilliseconds(399.4)")]
    [InlineData("TimeSpan.FromSeconds(0.3)")]
    [InlineData("TimeSpan.Zero")]
    [InlineData("new TimeSpan(0, 0, 0)")]
    [InlineData("new TimeSpan(0, 0, 0, 0, 100)")]
    [InlineData("new TimeSpan(1000)")]
    public async Task Reports_A_Loader_Duration_Below_400ms(string duration) {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync($$"""
            builder.OpenComponent<NTLoader>(0);
            builder.AddComponentParameter(1, "AnimationDuration", {{duration}});
            builder.CloseComponent();
            """);

        Assert.Equal(["NTC1054"], diagnostics.Ids());
    }

    [Theory]
    [InlineData("TimeSpan.FromMilliseconds(400)")]
    [InlineData("TimeSpan.FromMilliseconds(399.6)")]
    [InlineData("TimeSpan.FromSeconds(1)")]
    [InlineData("new TimeSpan(0, 0, 1)")]
    [InlineData("new TimeSpan(0, 0, 0, 0, 650)")]
    [InlineData("new TimeSpan(4000000)")]
    [InlineData("TimeSpan.FromMilliseconds(DateTime.Now.Millisecond)")]
    public async Task DoesNotReport_A_Loader_Duration_Of_At_Least_400ms_Or_Unknown(string duration) {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync($$"""
            builder.OpenComponent<NTLoader>(0);
            builder.AddComponentParameter(1, "AnimationDuration", {{duration}});
            builder.CloseComponent();
            """);

        Assert.Empty(diagnostics);
    }

    [Theory]
    [InlineData("new[] { NTShapeType.Circle }")]
    [InlineData("new NTShapeType[] { NTShapeType.Circle }")]
    [InlineData("(System.Collections.Generic.IReadOnlyList<NTShapeType>)[NTShapeType.Circle]")]
    public async Task Reports_A_Single_Shape_When_Animated(string shapes) {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync($$"""
            builder.OpenComponent<NTLoader>(0);
            builder.AddComponentParameter(1, "Shapes", {{shapes}});
            builder.CloseComponent();
            """);

        Assert.Equal(["NTC1055"], diagnostics.Ids());
    }

    [Fact]
    public async Task DoesNotReport_Shapes_Known_Only_At_Runtime() {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync("""
            var shapes = new System.Collections.Generic.List<NTShapeType> { NTShapeType.Circle };
            var animate = DateTime.Now.Year > 2000;
            builder.OpenComponent<NTLoader>(0);
            builder.AddComponentParameter(1, "Shapes", shapes);
            builder.CloseComponent();
            builder.OpenComponent<NTLoader>(2);
            builder.AddComponentParameter(3, "Shapes", new[] { NTShapeType.Circle });
            builder.AddComponentParameter(4, "Animate", animate);
            builder.CloseComponent();
            builder.OpenComponent<NTLoader>(5);
            builder.AddComponentParameter(6, "Shapes", (System.Collections.Generic.IReadOnlyList<NTShapeType>)[.. shapes]);
            builder.CloseComponent();
            """);

        Assert.Empty(diagnostics);
    }

    [Theory]
    [InlineData("new[] { NTShapeType.Circle, NTShapeType.Diamond }", true)]
    [InlineData("(System.Collections.Generic.IReadOnlyList<NTShapeType>)[NTShapeType.Circle, NTShapeType.Diamond]", true)]
    [InlineData("(System.Collections.Generic.IReadOnlyList<NTShapeType>)[]", true)]
    [InlineData("(System.Collections.Generic.IReadOnlyList<NTShapeType>)[.. System.Linq.Enumerable.Empty<NTShapeType>()]", true)]
    [InlineData("new[] { NTShapeType.Circle }", false)]
    [InlineData("(System.Collections.Generic.IReadOnlyList<NTShapeType>)[NTShapeType.Circle]", false)]
    public async Task DoesNotReport_Multiple_Shapes_Or_A_Static_Loader(string shapes, bool animate) {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync($$"""
            builder.OpenComponent<NTLoader>(0);
            builder.AddComponentParameter(1, "Shapes", {{shapes}});
            builder.AddComponentParameter(2, "Animate", {{animate.ToString().ToLowerInvariant()}});
            builder.CloseComponent();
            """);

        Assert.Empty(diagnostics);
    }

    [Theory]
    [InlineData("NTProgress")]
    [InlineData("NTLoader")]
    public async Task Reports_An_Empty_Accessible_Name(string component) {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync($$"""
            builder.OpenComponent<{{component}}>(0);
            builder.AddComponentParameter(1, "AriaLabel", "");
            builder.CloseComponent();
            builder.OpenComponent<{{component}}>(2);
            builder.AddComponentParameter(3, "AriaLabel", null);
            builder.CloseComponent();
            builder.OpenComponent<{{component}}>(4);
            builder.AddComponentParameter(5, "AriaLabel", " ");
            builder.AddComponentParameter(6, "AriaLabelledBy", "");
            builder.CloseComponent();
            builder.OpenComponent<{{component}}>(7);
            builder.AddComponentParameter(8, "AriaLabel", "");
            builder.AddAttribute(9, "aria-labelledby", "");
            builder.CloseComponent();
            builder.OpenComponent<{{component}}>(10);
            builder.AddComponentParameter(11, "AriaLabel", "Loading");
            builder.AddAttribute(12, "aria-label", "");
            builder.CloseComponent();
            """);

        Assert.Equal(["NTC1110", "NTC1110", "NTC1110", "NTC1110", "NTC1110"], diagnostics.Ids());
        Assert.Contains(component, diagnostics[0].GetMessage(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("NTProgress")]
    [InlineData("NTLoader")]
    public async Task DoesNotReport_A_Name_That_Is_Present_Or_Unknown(string component) {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync($$"""
            var text = DateTime.Now.ToString();
            builder.OpenComponent<{{component}}>(0);
            builder.CloseComponent();
            builder.OpenComponent<{{component}}>(1);
            builder.AddComponentParameter(2, "AriaLabel", "");
            builder.AddComponentParameter(3, "AriaLabelledBy", "title");
            builder.CloseComponent();
            builder.OpenComponent<{{component}}>(4);
            builder.AddComponentParameter(5, "AriaLabel", "");
            builder.AddAttribute(6, "aria-label", "Saving");
            builder.CloseComponent();
            builder.OpenComponent<{{component}}>(7);
            builder.AddComponentParameter(8, "AriaLabel", "");
            builder.AddAttribute(9, "aria-labelledby", "title");
            builder.CloseComponent();
            builder.OpenComponent<{{component}}>(10);
            builder.AddComponentParameter(11, "AriaLabel", text);
            builder.CloseComponent();
            builder.OpenComponent<{{component}}>(12);
            builder.AddComponentParameter(13, "AriaLabel", "");
            builder.AddComponentParameter(14, "AriaLabelledBy", text);
            builder.CloseComponent();
            builder.OpenComponent<{{component}}>(15);
            builder.AddComponentParameter(16, "AriaLabel", "");
            builder.AddMultipleAttributes(17, new System.Collections.Generic.Dictionary<string, object>());
            builder.CloseComponent();
            builder.OpenComponent<{{component}}>(18);
            builder.AddComponentParameter(19, "AriaLabel", "");
            builder.AddAttribute(20, "aria-label", text);
            builder.CloseComponent();
            """);

        Assert.Empty(diagnostics);
    }
}
