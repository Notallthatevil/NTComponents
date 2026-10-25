namespace NTComponents.Analyzers.Tests;

public sealed class NTIconButtonRules_Tests {
    private const string Valid = """
        builder.AddComponentParameter(1, "Icon", MaterialIcon.Add);
        builder.AddComponentParameter(2, "AriaLabel", "Add");
        """;

    private static async Task<string[]> IdsAsync(string parameters) =>
        [.. (await AnalyzerHarness.AnalyzeBuildAsync($"builder.OpenComponent<NTIconButton>(0);\n{parameters}\nbuilder.CloseComponent();")).Ids().Order()];

    [Fact]
    public async Task Valid_Configuration_Is_Clean() => Assert.Empty(await IdsAsync(Valid));

    [Theory]
    [InlineData("""builder.AddComponentParameter(2, "AriaLabel", "Add");""")]
    [InlineData("""builder.AddComponentParameter(1, "Icon", (NTIcon)null!); builder.AddComponentParameter(2, "AriaLabel", "Add");""")]
    public async Task Reports_Omitted_Or_Null_Icon(string parameters) => Assert.Equal(["NTC1026"], await IdsAsync(parameters));

    [Theory]
    [InlineData("")]
    [InlineData("""builder.AddComponentParameter(2, "AriaLabel", null);""")]
    [InlineData("""builder.AddComponentParameter(2, "AriaLabel", "  ");""")]
    public async Task Reports_Missing_Or_Blank_AriaLabel(string label) =>
        Assert.Equal(["NTC1027"], await IdsAsync($"""builder.AddComponentParameter(1, "Icon", MaterialIcon.Add);{label}"""));

    [Theory]
    [InlineData(null, "Primary", "NTC1028")]
    [InlineData("Text", "Primary", "NTC1028")]
    [InlineData("Outlined", "Primary", "NTC1028")]
    [InlineData("Filled", "Transparent", "NTC1029")]
    [InlineData("Tonal", "Transparent", "NTC1029")]
    [InlineData("Elevated", "Transparent", "NTC1029")]
    public async Task Reports_Background_That_Does_Not_Match_The_Variant(string? variant, string color, string expectedId) {
        var variantParameter = variant is null ? "" : $"builder.AddComponentParameter(3, \"Variant\", NTButtonVariant.{variant});";
        Assert.Equal([expectedId], await IdsAsync($"{Valid}{variantParameter}builder.AddComponentParameter(4, \"BackgroundColor\", NTColor.{color});"));
    }

    [Fact]
    public async Task Text_Is_The_Default_Variant() {
        // Primary is a visible background, which the default Text variant forbids; Filled allows it.
        Assert.Equal(["NTC1028"], await IdsAsync($"""{Valid}builder.AddComponentParameter(4, "BackgroundColor", NTColor.Primary);"""));
        Assert.Empty(await IdsAsync($"""{Valid}builder.AddComponentParameter(3, "Variant", NTButtonVariant.Filled);builder.AddComponentParameter(4, "BackgroundColor", NTColor.Primary);"""));
    }

    [Fact]
    public async Task Selected_Outlined_Toggle_Requires_A_Visible_Background() {
        var toggle = """builder.AddComponentParameter(3, "Variant", NTButtonVariant.Outlined);builder.AddComponentParameter(4, "IsToggleButton", true);builder.AddComponentParameter(5, "Selected", true);""";
        Assert.Equal(["NTC1029"], await IdsAsync($"""{Valid}{toggle}builder.AddComponentParameter(6, "BackgroundColor", NTColor.Transparent);"""));
        Assert.Empty(await IdsAsync($"""{Valid}{toggle}builder.AddComponentParameter(6, "BackgroundColor", NTColor.InverseSurface);"""));
    }

    [Fact]
    public async Task Reports_Transparent_TextColor() =>
        Assert.Equal(["NTC1030"], await IdsAsync($"""{Valid}builder.AddComponentParameter(3, "TextColor", NTColor.Transparent);"""));

    [Theory]
    [InlineData("Elevated", "None")]
    [InlineData("Filled", "Low")]
    [InlineData("Text", "Medium")]
    public async Task Reports_Elevation_That_Does_Not_Match_The_Variant(string variant, string elevation) =>
        Assert.Equal(["NTC1031"], await IdsAsync($"{Valid}builder.AddComponentParameter(3, \"Variant\", NTButtonVariant.{variant});builder.AddComponentParameter(4, \"Elevation\", NTElevation.{elevation});"));

    [Fact]
    public async Task DoesNotReport_Values_Known_Only_At_Runtime() {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync("""
            var label = DateTime.Now.ToString();
            var variant = DateTime.Now.Second > 30 ? NTButtonVariant.Text : NTButtonVariant.Filled;
            builder.OpenComponent<NTIconButton>(0);
            builder.AddComponentParameter(1, "Icon", MaterialIcon.Add);
            builder.AddComponentParameter(2, "AriaLabel", label);
            builder.AddComponentParameter(3, "Variant", variant);
            builder.AddComponentParameter(4, "BackgroundColor", NTColor.Transparent);
            builder.AddComponentParameter(5, "Elevation", NTElevation.Low);
            builder.CloseComponent();
            builder.OpenComponent<NTIconButton>(6);
            builder.AddMultipleAttributes(7, new System.Collections.Generic.Dictionary<string, object>());
            builder.CloseComponent();
            """);

        Assert.Empty(diagnostics);
    }
}
