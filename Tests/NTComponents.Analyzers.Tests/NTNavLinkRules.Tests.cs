namespace NTComponents.Analyzers.Tests;

public sealed class NTNavLinkRules_Tests {

    private static string NavLink(string parameters) => $$"""
        builder.OpenComponent<NTNavLink>(0);
        builder.AddComponentParameter(1, "Label", "Home");
        {{parameters}}
        builder.CloseComponent();
        """;

    private static string Variant(string variant, string parameters) => NavLink($"builder.AddComponentParameter(2, \"Variant\", NTNavLinkVariant.{variant}); {parameters}");

    [Fact]
    public async Task DoesNotReport_A_Valid_Link() {
        Assert.Empty(await AnalyzerHarness.AnalyzeBuildAsync(NavLink("")));
    }

    [Theory]
    [InlineData("")]
    [InlineData("builder.AddComponentParameter(2, \"Label\", null);")]
    [InlineData("builder.AddComponentParameter(2, \"Label\", \"  \");")]
    public async Task Reports_A_Missing_Label(string label) {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync($$"""
            builder.OpenComponent<NTNavLink>(0);
            {{label}}
            builder.CloseComponent();
            """);

        Assert.Equal(["NTC1056"], diagnostics.Ids());
    }

    [Fact]
    public async Task DoesNotReport_A_Label_The_Analyzer_Cannot_See() {
        Assert.Empty(await AnalyzerHarness.AnalyzeBuildAsync("""
            builder.OpenComponent<NTNavLink>(0);
            builder.AddComponentParameter(1, "Label", DateTime.Now.ToString());
            builder.CloseComponent();
            """));
    }

    [Theory]
    [InlineData("aria-current")]
    [InlineData("aria-disabled")]
    [InlineData("ARIA-CURRENT")]
    public async Task Reports_Component_Owned_Aria_State(string name) {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync(NavLink($"builder.AddComponentParameter(2, \"{name}\", \"true\");"));

        Assert.Equal(["NTC1057"], diagnostics.Ids());
    }

    [Fact]
    public async Task Reports_An_Explicit_Tab_Index_On_A_Disabled_Link() {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync(NavLink("builder.AddComponentParameter(2, \"Disabled\", true); builder.AddComponentParameter(3, \"tabindex\", \"0\");"));

        Assert.Equal(["NTC1058"], diagnostics.Ids());
    }

    [Theory]
    [InlineData("builder.AddComponentParameter(2, \"tabindex\", \"0\");")]
    [InlineData("builder.AddComponentParameter(2, \"Disabled\", false); builder.AddComponentParameter(3, \"tabindex\", \"0\");")]
    [InlineData("builder.AddComponentParameter(2, \"Disabled\", DateTime.Now.Year > 0); builder.AddComponentParameter(3, \"tabindex\", \"0\");")]
    public async Task DoesNotReport_A_Tab_Index_On_A_Link_That_May_Be_Enabled(string parameters) {
        Assert.Empty(await AnalyzerHarness.AnalyzeBuildAsync(NavLink(parameters)));
    }

    [Theory]
    [InlineData("Text", "Primary", "NTC1083")]
    [InlineData("Outlined", "Primary", "NTC1083")]
    [InlineData("Filled", "Transparent", "NTC1084")]
    [InlineData("Tonal", "Transparent", "NTC1084")]
    [InlineData("Elevated", "Transparent", "NTC1084")]
    public async Task Reports_Background_That_Does_Not_Match_The_Variant(string variant, string color, string expectedId) {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync(Variant(variant, $"builder.AddComponentParameter(3, \"BackgroundColor\", NTColor.{color});"));

        Assert.Equal([expectedId], diagnostics.Ids());
    }

    [Fact]
    public async Task Filled_Is_The_Default_Variant() {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync(NavLink("builder.AddComponentParameter(2, \"BackgroundColor\", NTColor.Transparent);"));

        Assert.Equal(["NTC1084"], diagnostics.Ids());
    }

    [Theory]
    [InlineData("DefaultAnchor")]
    [InlineData("InlineText")]
    public async Task DoesNotCheck_Background_Or_Elevation_For_Text_Link_Variants(string variant) {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync(Variant(variant, "builder.AddComponentParameter(3, \"BackgroundColor\", NTColor.Primary); builder.AddComponentParameter(4, \"Elevation\", NTElevation.High);"));

        Assert.Empty(diagnostics);
    }

    [Theory]
    [InlineData("TextColor")]
    [InlineData("HoverTextColor")]
    [InlineData("ActiveTextColor")]
    public async Task Reports_A_Transparent_Content_Color_For_Every_Variant(string parameter) {
        var buttonChrome = await AnalyzerHarness.AnalyzeBuildAsync(NavLink($"builder.AddComponentParameter(2, \"{parameter}\", NTColor.Transparent);"));
        var textLink = await AnalyzerHarness.AnalyzeBuildAsync(Variant("InlineText", $"builder.AddComponentParameter(3, \"{parameter}\", NTColor.Transparent);"));

        Assert.Equal(["NTC1085"], buttonChrome.Ids());
        Assert.Equal(["NTC1085"], textLink.Ids());
        Assert.Contains(parameter, buttonChrome[0].GetMessage());
    }

    [Theory]
    [InlineData("DefaultAnchor", true)]
    [InlineData("InlineText", true)]
    [InlineData("Filled", false)]
    [InlineData("Text", false)]
    public async Task VisitedTextColor_Only_Matters_With_Text_Link_Chrome(string variant, bool reported) {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync(Variant(variant, "builder.AddComponentParameter(3, \"VisitedTextColor\", NTColor.Transparent);"));

        Assert.Equal(reported ? new[] { "NTC1085" } : Array.Empty<string>(), diagnostics.Ids());
    }

    [Fact]
    public async Task DoesNotReport_VisitedTextColor_When_The_Variant_Is_Unknown() {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync(NavLink("builder.AddComponentParameter(2, \"Variant\", DateTime.Now.Year > 0 ? NTNavLinkVariant.Filled : NTNavLinkVariant.InlineText); builder.AddComponentParameter(3, \"VisitedTextColor\", NTColor.Transparent);"));

        Assert.Empty(diagnostics);
    }

    [Theory]
    [InlineData("Elevated", "None")]
    [InlineData("Filled", "Lowest")]
    [InlineData("Tonal", "Low")]
    [InlineData("Outlined", "Medium")]
    [InlineData("Text", "High")]
    public async Task Reports_Elevation_That_Does_Not_Match_The_Variant(string variant, string elevation) {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync(Variant(variant, $"builder.AddComponentParameter(3, \"Elevation\", NTElevation.{elevation});"));

        Assert.Equal(["NTC1086"], diagnostics.Ids());
    }

    [Fact]
    public async Task DoesNotReport_Matching_Omitted_Or_Unknown_Values() {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync(
            Variant("Elevated", "builder.AddComponentParameter(3, \"Elevation\", NTElevation.Low); builder.AddComponentParameter(4, \"BackgroundColor\", NTColor.Surface);")
            + Variant("Filled", "builder.AddComponentParameter(3, \"Elevation\", NTElevation.None); builder.AddComponentParameter(4, \"BackgroundColor\", null); builder.AddComponentParameter(5, \"TextColor\", null);")
            + NavLink("builder.AddComponentParameter(2, \"Variant\", DateTime.Now.Year > 0 ? NTNavLinkVariant.Text : NTNavLinkVariant.Filled); builder.AddComponentParameter(3, \"Elevation\", NTElevation.High); builder.AddComponentParameter(4, \"BackgroundColor\", NTColor.Primary);"));

        Assert.Empty(diagnostics);
    }
}
