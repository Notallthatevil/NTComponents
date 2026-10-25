namespace NTComponents.Analyzers.Tests;

public sealed class NTSplitButtonRules_Tests {
    private const string Item = "b.OpenComponent<NTMenuButtonItem>(0); b.AddComponentParameter(1, \"Label\", \"Save\"); b.CloseComponent();";

    private static string SplitButton(string parameters, string content = Item) => $$"""
        builder.OpenComponent<NTSplitButton>(0);
        {{parameters}}
        builder.AddComponentParameter(20, "ChildContent", (RenderFragment)(b => { {{content}} }));
        builder.CloseComponent();
        """;

    private static string Labeled(string parameters = "", string content = Item) => SplitButton("builder.AddComponentParameter(1, \"Label\", \"Save\");" + parameters, content);

    [Fact]
    public async Task DoesNotReport_A_Valid_Split_Button() {
        Assert.Empty(await AnalyzerHarness.AnalyzeBuildAsync(Labeled()));
    }

    [Theory]
    [InlineData("")]
    [InlineData("builder.AddComponentParameter(1, \"Label\", \" \");")]
    [InlineData("builder.AddComponentParameter(1, \"Label\", \"\"); builder.AddComponentParameter(2, \"LeadingIcon\", null);")]
    public async Task Reports_An_Empty_Label_Without_An_Icon(string parameters) {
        Assert.Equal(["NTC1009"], (await AnalyzerHarness.AnalyzeBuildAsync(SplitButton(parameters))).Ids());
    }

    [Theory]
    [InlineData("")]
    [InlineData("builder.AddComponentParameter(3, \"ActionAriaLabel\", \"\");")]
    public async Task Reports_An_Icon_Only_Action_Without_An_Aria_Label(string ariaLabel) {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync(SplitButton("builder.AddComponentParameter(2, \"LeadingIcon\", MaterialIcon.Add);" + ariaLabel));

        Assert.Equal(["NTC1010"], diagnostics.Ids());
    }

    [Theory]
    [InlineData("builder.AddComponentParameter(2, \"LeadingIcon\", MaterialIcon.Add); builder.AddComponentParameter(3, \"ActionAriaLabel\", \"Save\");")]
    [InlineData("builder.AddComponentParameter(1, \"Label\", DateTime.Now.ToString());")]
    [InlineData("builder.AddMultipleAttributes(1, new System.Collections.Generic.Dictionary<string, object>());")]
    [InlineData("builder.AddComponentParameter(2, \"LeadingIcon\", MaterialIcon.Add); builder.AddComponentParameter(3, \"ActionAriaLabel\", DateTime.Now.ToString());")]
    public async Task DoesNotReport_Labels_That_Are_Valid_Or_Unknown(string parameters) {
        Assert.Empty(await AnalyzerHarness.AnalyzeBuildAsync(SplitButton(parameters)));
    }

    [Theory]
    [InlineData("Text", "Primary", "NTC1011")]
    [InlineData("Outlined", "Primary", "NTC1011")]
    [InlineData("Filled", "Transparent", "NTC1012")]
    [InlineData("Tonal", "Transparent", "NTC1012")]
    [InlineData("Elevated", "Transparent", "NTC1012")]
    public async Task Reports_Background_That_Does_Not_Match_The_Variant(string variant, string color, string expectedId) {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync(Labeled($"builder.AddComponentParameter(2, \"Variant\", NTButtonVariant.{variant}); builder.AddComponentParameter(3, \"BackgroundColor\", NTColor.{color});"));

        Assert.Equal([expectedId], diagnostics.Ids());
    }

    [Fact]
    public async Task Filled_Is_The_Default_Variant() {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync(Labeled("builder.AddComponentParameter(2, \"BackgroundColor\", NTColor.Transparent);"));

        Assert.Equal(["NTC1012"], diagnostics.Ids());
    }

    [Fact]
    public async Task Reports_A_Transparent_Text_Color() {
        Assert.Equal(["NTC1013"], (await AnalyzerHarness.AnalyzeBuildAsync(Labeled("builder.AddComponentParameter(2, \"TextColor\", NTColor.Transparent);"))).Ids());
    }

    [Theory]
    [InlineData("Elevated", "None", "NTC1014")]
    [InlineData("Filled", "Lowest", "NTC1014")]
    [InlineData("Text", "Medium", "NTC1014")]
    public async Task Reports_Elevation_That_Does_Not_Match_The_Variant(string variant, string elevation, string expectedId) {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync(Labeled($"builder.AddComponentParameter(2, \"Variant\", NTButtonVariant.{variant}); builder.AddComponentParameter(3, \"Elevation\", NTElevation.{elevation});"));

        Assert.Equal([expectedId], diagnostics.Ids());
    }

    [Fact]
    public async Task DoesNotReport_Matching_Or_Omitted_Elevation() {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync(Labeled("""
            builder.AddComponentParameter(2, "Variant", NTButtonVariant.Elevated);
            builder.AddComponentParameter(3, "Elevation", NTElevation.Low);
            builder.AddComponentParameter(4, "BackgroundColor", null);
            """) + Labeled("builder.AddComponentParameter(2, \"Variant\", DateTime.Now.Year > 0 ? NTButtonVariant.Text : NTButtonVariant.Filled); builder.AddComponentParameter(3, \"BackgroundColor\", NTColor.Transparent);"));

        Assert.Empty(diagnostics);
    }

    [Theory]
    [InlineData("MenuBackgroundColor")]
    [InlineData("MenuTextColor")]
    [InlineData("MenuSelectedBackgroundColor")]
    [InlineData("MenuSelectedTextColor")]
    public async Task Reports_Transparent_Menu_Colors(string parameter) {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync(Labeled($"builder.AddComponentParameter(2, \"{parameter}\", NTColor.Transparent);"));

        Assert.Equal(["NTC1015"], diagnostics.Ids());
    }

    [Theory]
    [InlineData("b.OpenComponent<NTMenuDividerItem>(0); b.CloseComponent();")]
    [InlineData("b.OpenComponent<NTMenuLabelItem>(0); b.AddComponentParameter(1, \"Label\", \"Heading\"); b.CloseComponent();")]
    [InlineData("")]
    public async Task Reports_A_Menu_Without_An_Actionable_Item(string content) {
        Assert.Equal(["NTC1016"], (await AnalyzerHarness.AnalyzeBuildAsync(Labeled(content: content))).Ids());
    }

    [Fact]
    public async Task Reports_A_Split_Button_With_No_Content() {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync("""
            builder.OpenComponent<NTSplitButton>(0);
            builder.AddComponentParameter(1, "Label", "Save");
            builder.CloseComponent();
            """);

        Assert.Equal(["NTC1016"], diagnostics.Ids());
    }

    [Theory]
    [InlineData("b.OpenComponent<NTMenuSubMenuItem>(0); b.AddComponentParameter(1, \"Label\", \"More\"); b.CloseComponent();")]
    [InlineData("if (DateTime.Now.Year > 0) { " + Item + " }")]
    [InlineData("foreach (var i in new[] { 1 }) { " + Item + " }")]
    [InlineData("b.OpenComponent<NTBadge>(0); b.AddComponentParameter(1, \"Content\", \"3\"); b.CloseComponent();")]
    [InlineData("RenderFragment f = c => { }; b.AddContent(0, f);")]
    public async Task DoesNotReport_Menus_That_Have_Or_May_Render_Items(string content) {
        Assert.Empty(await AnalyzerHarness.AnalyzeBuildAsync(Labeled(content: content)));
    }

    [Fact]
    public async Task Item_Labels_Are_Checked_By_The_Menu_Rules() {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync(Labeled(content: "b.OpenComponent<NTMenuButtonItem>(0); b.CloseComponent();"));

        Assert.Equal(["NTC1049"], diagnostics.Ids());
    }
}
