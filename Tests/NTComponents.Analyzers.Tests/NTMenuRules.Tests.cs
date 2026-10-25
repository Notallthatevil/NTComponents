namespace NTComponents.Analyzers.Tests;

public sealed class NTMenuRules_Tests {
    private const string Button = "b.OpenComponent<NTMenuButtonItem>(0); b.AddComponentParameter(1, \"Label\", \"Save\"); b.CloseComponent();";
    private const string Divider = "b.OpenComponent<NTMenuDividerItem>(0); b.CloseComponent();";
    private const string Label = "b.OpenComponent<NTMenuLabelItem>(0); b.AddComponentParameter(1, \"Label\", \"Heading\"); b.CloseComponent();";

    private static string Menu(string content, string extra = "") => $$"""
        builder.OpenComponent<NTMenu>(0);
        builder.AddComponentParameter(1, "AriaLabel", "Actions");
        {{extra}}
        builder.AddComponentParameter(3, "ChildContent", (RenderFragment)(b => { {{content}} }));
        builder.CloseComponent();
        """;

    [Theory]
    [InlineData(Button)]
    [InlineData("b.OpenComponent<NTMenuAnchorItem>(0); b.AddComponentParameter(1, \"Label\", \"Home\"); b.AddComponentParameter(2, \"Href\", \"/\"); b.CloseComponent();")]
    [InlineData("b.OpenComponent<NTMenuSubMenuItem>(0); b.AddComponentParameter(1, \"Label\", \"More\"); b.CloseComponent();")]
    [InlineData(Divider + Button)]
    [InlineData("if (DateTime.Now.Year > 0) { " + Button + " }")]
    public async Task DoesNotReport_A_Menu_With_An_Actionable_Item(string content) {
        Assert.Empty(await AnalyzerHarness.AnalyzeBuildAsync(Menu(content)));
    }

    [Theory]
    [InlineData("")]
    [InlineData(Divider)]
    [InlineData(Label + Divider)]
    [InlineData("if (DateTime.Now.Year > 0) { " + Divider + " }")]
    public async Task Reports_A_Menu_Without_An_Actionable_Item(string content) {
        Assert.Equal(["NTC1048"], (await AnalyzerHarness.AnalyzeBuildAsync(Menu(content))).Ids());
    }

    [Fact]
    public async Task Reports_A_Menu_With_No_Content() {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync("""
            builder.OpenComponent<NTMenu>(0);
            builder.AddComponentParameter(1, "AriaLabel", "Actions");
            builder.CloseComponent();
            """);

        Assert.Equal(["NTC1048"], diagnostics.Ids());
    }

    [Theory]
    [InlineData("RenderFragment f = c => { }; b.OpenElement(0, \"div\"); b.AddContent(1, f); b.CloseElement();")]
    [InlineData("RenderFragment f = c => { }; b.AddContent(0, f);")]
    [InlineData("b.OpenComponent<NTBadge>(0); b.CloseComponent();")]
    [InlineData(Divider + "b.OpenComponent<NTBadge>(0); b.CloseComponent();")]
    public async Task DoesNotReport_Content_That_May_Render_Items(string content) {
        Assert.Empty(await AnalyzerHarness.AnalyzeBuildAsync(Menu(content)));
    }

    [Fact]
    public async Task DoesNotReport_Content_Passed_As_An_Opaque_Fragment() {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync("""
            RenderFragment content = c => { };
            builder.OpenComponent<NTMenu>(0);
            builder.AddComponentParameter(1, "AriaLabel", "Actions");
            builder.AddComponentParameter(2, "ChildContent", content);
            builder.CloseComponent();
            builder.OpenComponent<NTMenu>(3);
            builder.AddComponentParameter(4, "AriaLabel", "Actions");
            builder.AddMultipleAttributes(5, new System.Collections.Generic.Dictionary<string, object>());
            builder.CloseComponent();
            """);

        Assert.Empty(diagnostics);
    }

    [Theory]
    [InlineData("builder.AddComponentParameter(1, \"AriaLabel\", \"\");")]
    [InlineData("builder.AddComponentParameter(1, \"AriaLabel\", null);")]
    [InlineData("")]
    public async Task Reports_A_Missing_Aria_Label(string ariaLabel) {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync($$"""
            builder.OpenComponent<NTMenu>(0);
            {{ariaLabel}}
            builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(b => { {{Button}} }));
            builder.CloseComponent();
            """);

        Assert.Equal(["NTC1047"], diagnostics.Ids());
    }

    [Theory]
    [InlineData("ContainerColor")]
    [InlineData("TextColor")]
    [InlineData("SelectedContainerColor")]
    [InlineData("SelectedTextColor")]
    public async Task Reports_Transparent_Menu_Colors(string parameter) {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync(Menu(Button, $"builder.AddComponentParameter(2, \"{parameter}\", NTColor.Transparent);"));

        Assert.Equal(["NTC1051"], diagnostics.Ids());
    }

    [Fact]
    public async Task DoesNotReport_Visible_Or_Unknown_Menu_Colors() {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync(Menu(Button, """
            builder.AddComponentParameter(2, "ContainerColor", NTColor.Surface);
            builder.AddComponentParameter(4, "TextColor", DateTime.Now.Year > 0 ? NTColor.Transparent : NTColor.Primary);
            """));

        Assert.Empty(diagnostics);
    }

    [Theory]
    [InlineData("NTMenuButtonItem", "")]
    [InlineData("NTMenuSubMenuItem", "")]
    [InlineData("NTMenuLabelItem", "")]
    [InlineData("NTMenuButtonItem", "b.AddComponentParameter(1, \"Label\", \"  \");")]
    [InlineData("NTMenuLabelItem", "b.AddComponentParameter(1, \"Label\", null);")]
    public async Task Reports_An_Empty_Item_Label(string item, string label) {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync(Menu($"b.OpenComponent<{item}>(0); {label} b.CloseComponent(); {Button}"));

        Assert.Equal(["NTC1049"], diagnostics.Ids());
    }

    [Fact]
    public async Task Reports_An_Empty_Anchor_Href() {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync(Menu($"{Button} b.OpenComponent<NTMenuAnchorItem>(0); b.AddComponentParameter(1, \"Label\", \"Home\"); b.AddComponentParameter(2, \"Href\", \"\"); b.CloseComponent();"));

        Assert.Equal(["NTC1050"], diagnostics.Ids());
    }

    [Fact]
    public async Task Reports_An_Anchor_With_No_Label_Or_Href() {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync(Menu($"{Button} b.OpenComponent<NTMenuAnchorItem>(0); b.CloseComponent();"));

        Assert.Equal(["NTC1049", "NTC1050"], diagnostics.Ids());
    }

    [Fact]
    public async Task DoesNotReport_Item_Values_The_Analyzer_Cannot_See() {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync(Menu($"b.OpenComponent<NTMenuAnchorItem>(0); b.AddComponentParameter(1, \"Label\", DateTime.Now.ToString()); b.AddComponentParameter(2, \"Href\", DateTime.Now.ToString()); b.CloseComponent();"));

        Assert.Empty(diagnostics);
    }

    private static string ContextMenu(string target, string menu, string extra = "") => $$"""
        builder.OpenComponent<NTContextMenu>(0);
        builder.AddComponentParameter(1, "AriaLabel", "Actions");
        {{extra}}
        builder.AddComponentParameter(3, "TargetContent", (RenderFragment)(t => { {{target}} }));
        {{menu}}
        builder.CloseComponent();
        """;

    [Fact]
    public async Task ContextMenu_Counts_Only_Items_In_MenuContent() {
        const string target = "t.OpenComponent<NTButton>(0); t.AddComponentParameter(1, \"Label\", \"Open\"); t.CloseComponent();";
        var valid = ContextMenu(target, $"builder.AddComponentParameter(4, \"MenuContent\", (RenderFragment)(b => {{ {Button} }}));");
        var empty = ContextMenu(target, $"builder.AddComponentParameter(4, \"MenuContent\", (RenderFragment)(b => {{ {Divider} }}));");

        Assert.Empty(await AnalyzerHarness.AnalyzeBuildAsync(valid));
        Assert.Equal(["NTC1048"], (await AnalyzerHarness.AnalyzeBuildAsync(empty)).Ids());
    }

    [Fact]
    public async Task ContextMenu_Reports_Missing_Content_And_Colors() {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync(ContextMenu(string.Empty, string.Empty, "builder.AddComponentParameter(2, \"ContainerColor\", NTColor.Transparent);"));

        Assert.Equal(["NTC1048", "NTC1051"], diagnostics.Ids());
    }

    [Fact]
    public async Task ContextMenu_That_May_Inherit_Parent_Items_Is_Not_Checked() {
        var inherit = ContextMenu(string.Empty, "builder.AddComponentParameter(4, \"MenuContent\", (RenderFragment)(b => { }));", "builder.AddComponentParameter(2, \"InheritParentMenus\", true);");
        var unknown = ContextMenu(string.Empty, string.Empty, "builder.AddComponentParameter(2, \"InheritParentMenus\", DateTime.Now.Year > 0);");

        Assert.Empty(await AnalyzerHarness.AnalyzeBuildAsync(inherit));
        Assert.Empty(await AnalyzerHarness.AnalyzeBuildAsync(unknown));
    }
}
