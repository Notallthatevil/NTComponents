namespace NTComponents.Analyzers.Tests;

public sealed class NTFabButtonRules_Tests {
    private const string Valid = """
        builder.AddComponentParameter(1, "Icon", MaterialIcon.Add);
        builder.AddComponentParameter(2, "AriaLabel", "Add");
        """;

    private static Task<string[]> IdsAsync(string parameters) => AnalyzerHarness.ComponentIdsAsync("NTFabButton", parameters);

    [Fact]
    public async Task Valid_Configuration_Is_Clean() {
        Assert.Empty(await IdsAsync(Valid));
        Assert.Empty(await IdsAsync("""builder.AddComponentParameter(1, "Icon", MaterialIcon.Add);builder.AddComponentParameter(3, "Label", "Create");"""));
    }

    [Fact]
    public async Task Reports_Omitted_Or_Null_Icon() {
        Assert.Equal(["NTC1032"], await IdsAsync("""builder.AddComponentParameter(2, "AriaLabel", "Add");"""));
        Assert.Equal(["NTC1032"], await IdsAsync("""builder.AddComponentParameter(1, "Icon", (NTIcon)null!);builder.AddComponentParameter(2, "AriaLabel", "Add");"""));
    }

    [Theory]
    [InlineData("")]
    [InlineData("""builder.AddComponentParameter(2, "AriaLabel", "  ");""")]
    [InlineData("""builder.AddComponentParameter(2, "AriaLabel", null);builder.AddComponentParameter(3, "Label", " ");""")]
    public async Task Reports_Icon_Only_Without_AriaLabel(string parameters) =>
        Assert.Equal(["NTC1033"], await IdsAsync($"""builder.AddComponentParameter(1, "Icon", MaterialIcon.Add);{parameters}"""));

    [Fact]
    public async Task Reports_Label_With_Line_Breaks() {
        Assert.Equal(["NTC1034"], await IdsAsync("builder.AddComponentParameter(1, \"Icon\", MaterialIcon.Add);builder.AddComponentParameter(3, \"Label\", \"A\\nB\");"));
        Assert.Equal(["NTC1034"], await IdsAsync("builder.AddComponentParameter(1, \"Icon\", MaterialIcon.Add);builder.AddComponentParameter(3, \"Label\", \"A\\rB\");"));
    }

    [Fact]
    public async Task Reports_Transparent_Colors() {
        Assert.Equal(["NTC1035"], await IdsAsync($"""{Valid}builder.AddComponentParameter(3, "BackgroundColor", NTColor.Transparent);"""));
        Assert.Equal(["NTC1036"], await IdsAsync($"""{Valid}builder.AddComponentParameter(3, "TextColor", NTColor.Transparent);"""));
        Assert.Empty(await IdsAsync($"""{Valid}builder.AddComponentParameter(3, "BackgroundColor", NTColor.PrimaryContainer);"""));
    }

    [Theory]
    [InlineData("Smallest", true)]
    [InlineData("Largest", true)]
    [InlineData("Small", false)]
    [InlineData("Medium", false)]
    [InlineData("Large", false)]
    public async Task Reports_Unsupported_Sizes(string size, bool reported) =>
        Assert.Equal(reported ? ["NTC1037"] : [], await IdsAsync($"{Valid}builder.AddComponentParameter(3, \"ButtonSize\", NTSize.{size});"));

    [Fact]
    public async Task Reports_Undefined_Placement_Only() {
        Assert.Equal(["NTC1038"], await IdsAsync($"""{Valid}builder.AddComponentParameter(3, "Placement", (NTFabButtonPlacement)99);"""));
        Assert.Empty(await IdsAsync($"""{Valid}builder.AddComponentParameter(3, "Placement", NTFabButtonPlacement.UpperLeft);"""));
    }

    [Fact]
    public async Task DoesNotReport_Values_Known_Only_At_Runtime() {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync("""
            var text = DateTime.Now.ToString();
            builder.OpenComponent<NTFabButton>(0);
            builder.AddComponentParameter(1, "Icon", MaterialIcon.Add);
            builder.AddComponentParameter(2, "Label", text);
            builder.AddComponentParameter(3, "Placement", (NTFabButtonPlacement)DateTime.Now.Second);
            builder.AddComponentParameter(4, "ButtonSize", (NTSize)DateTime.Now.Second);
            builder.CloseComponent();
            builder.OpenComponent<NTFabButton>(5);
            builder.AddComponentParameter(6, "Icon", MaterialIcon.Add);
            builder.AddComponentParameter(7, "AriaLabel", text);
            builder.CloseComponent();
            builder.OpenComponent<NTFabButton>(8);
            builder.AddMultipleAttributes(9, new System.Collections.Generic.Dictionary<string, object>());
            builder.CloseComponent();
            """);

        Assert.Empty(diagnostics);
    }
}
