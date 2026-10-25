namespace NTComponents.Analyzers.Tests;

public sealed class NTButtonGroupRules_Tests {

    private static string Group(string parameters) => $$"""
        builder.OpenComponent<NTButtonGroup<string>>(0);
        {{parameters}}
        builder.CloseComponent();
        """;

    [Fact]
    public async Task DoesNotReport_A_Valid_Group() {
        Assert.Empty(await AnalyzerHarness.AnalyzeBuildAsync(Group("")));
    }

    [Theory]
    [InlineData("NTButtonGroupSelectionMode.Single")]
    [InlineData("NTButtonGroupSelectionMode.Multiple")]
    public async Task Reports_A_Selectable_Text_Group(string mode) {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync(Group($"builder.AddComponentParameter(1, \"Variant\", NTButtonVariant.Text); builder.AddComponentParameter(2, \"SelectionMode\", {mode});"));

        Assert.Equal(["NTC1019"], diagnostics.Ids());
    }

    [Fact]
    public async Task Single_Is_The_Default_Selection_Mode() {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync(Group("builder.AddComponentParameter(1, \"Variant\", NTButtonVariant.Text);"));

        Assert.Equal(["NTC1019"], diagnostics.Ids());
    }

    [Theory]
    [InlineData("builder.AddComponentParameter(2, \"SelectionMode\", NTButtonGroupSelectionMode.None);")]
    [InlineData("builder.AddComponentParameter(2, \"SelectionMode\", (NTButtonGroupSelectionMode)99);")]
    [InlineData("builder.AddComponentParameter(2, \"SelectionMode\", DateTime.Now.Year > 0 ? NTButtonGroupSelectionMode.None : NTButtonGroupSelectionMode.Single);")]
    [InlineData("builder.AddMultipleAttributes(2, new System.Collections.Generic.Dictionary<string, object>());")]
    public async Task DoesNotReport_A_Text_Group_That_Is_Not_Known_To_Be_Selectable(string selectionMode) {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync(Group("builder.AddComponentParameter(1, \"Variant\", NTButtonVariant.Text);" + selectionMode));

        Assert.Empty(diagnostics);
    }

    [Theory]
    [InlineData("Text", "Primary", "NTC1020")]
    [InlineData("Outlined", "Primary", "NTC1020")]
    [InlineData("Filled", "Transparent", "NTC1021")]
    [InlineData("Tonal", "Transparent", "NTC1021")]
    [InlineData("Elevated", "Transparent", "NTC1021")]
    public async Task Reports_Background_That_Does_Not_Match_The_Variant(string variant, string color, string expectedId) {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync(Group($"builder.AddComponentParameter(1, \"Variant\", NTButtonVariant.{variant}); builder.AddComponentParameter(2, \"BackgroundColor\", NTColor.{color}); builder.AddComponentParameter(3, \"SelectionMode\", NTButtonGroupSelectionMode.None);"));

        Assert.Equal([expectedId], diagnostics.Ids());
    }

    [Fact]
    public async Task Tonal_Is_The_Default_Variant() {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync(Group("builder.AddComponentParameter(1, \"BackgroundColor\", NTColor.Transparent);"));

        Assert.Equal(["NTC1021"], diagnostics.Ids());
    }

    [Fact]
    public async Task Reports_A_Transparent_Text_Color() {
        Assert.Equal(["NTC1022"], (await AnalyzerHarness.AnalyzeBuildAsync(Group("builder.AddComponentParameter(1, \"TextColor\", NTColor.Transparent);"))).Ids());
    }

    [Fact]
    public async Task Reports_A_Transparent_Selected_Background_When_Selectable() {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync(Group("builder.AddComponentParameter(1, \"SelectedBackgroundColor\", NTColor.Transparent);"));

        Assert.Equal(["NTC1023"], diagnostics.Ids());
    }

    [Fact]
    public async Task Reports_A_Transparent_Selected_Text_Color_When_Selectable() {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync(Group("builder.AddComponentParameter(1, \"SelectedTextColor\", NTColor.Transparent);"));

        Assert.Equal(["NTC1024"], diagnostics.Ids());
    }

    [Theory]
    [InlineData("builder.AddComponentParameter(2, \"SelectionMode\", NTButtonGroupSelectionMode.None);")]
    [InlineData("builder.AddComponentParameter(2, \"SelectionMode\", DateTime.Now.Year > 0 ? NTButtonGroupSelectionMode.None : NTButtonGroupSelectionMode.Single);")]
    public async Task DoesNotReport_Selected_Colors_Unless_Known_To_Be_Selectable(string selectionMode) {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync(Group("builder.AddComponentParameter(1, \"SelectedTextColor\", NTColor.Transparent); builder.AddComponentParameter(3, \"SelectedBackgroundColor\", NTColor.Transparent);" + selectionMode));

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Selectable_Text_Group_Reports_Only_The_Selection_Rule() {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync(Group("builder.AddComponentParameter(1, \"Variant\", NTButtonVariant.Text); builder.AddComponentParameter(2, \"SelectedBackgroundColor\", NTColor.Transparent);"));

        Assert.Equal(["NTC1019"], diagnostics.Ids());
    }

    private static string Item(string parameters) => $$"""
        builder.OpenComponent<NTButtonGroupItem<string>>(0);
        builder.AddComponentParameter(1, "Key", "a");
        {{parameters}}
        builder.CloseComponent();
        """;

    [Theory]
    [InlineData("")]
    [InlineData("builder.AddComponentParameter(3, \"Label\", \" \"); builder.AddComponentParameter(4, \"AriaLabel\", \"\");")]
    public async Task Reports_An_Icon_Only_Item_Without_An_Aria_Label(string labels) {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync(Item("builder.AddComponentParameter(2, \"Icon\", MaterialIcon.Add);" + labels));

        Assert.Equal(["NTC1025"], diagnostics.Ids());
    }

    [Theory]
    [InlineData("builder.AddComponentParameter(3, \"AriaLabel\", \"Add\");")]
    [InlineData("builder.AddComponentParameter(3, \"Label\", \"Add\");")]
    [InlineData("builder.AddComponentParameter(3, \"AriaLabel\", DateTime.Now.ToString());")]
    [InlineData("builder.AddMultipleAttributes(3, new System.Collections.Generic.Dictionary<string, object>());")]
    public async Task DoesNotReport_Icon_Items_With_A_Known_Or_Unknown_Label(string labels) {
        Assert.Empty(await AnalyzerHarness.AnalyzeBuildAsync(Item("builder.AddComponentParameter(2, \"Icon\", MaterialIcon.Add);" + labels)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("builder.AddComponentParameter(2, \"Icon\", null);")]
    [InlineData("builder.AddComponentParameter(2, \"Icon\", DateTime.Now.Year > 0 ? MaterialIcon.Add : null);")]
    public async Task DoesNotReport_Items_Without_A_Known_Icon(string icon) {
        Assert.Empty(await AnalyzerHarness.AnalyzeBuildAsync(Item(icon)));
    }
}
