namespace NTComponents.Analyzers.Tests;

public sealed class NTFabMenuRules_Tests {
    private const string Valid = """
        builder.AddComponentParameter(1, "Icon", MaterialIcon.Add);
        builder.AddComponentParameter(2, "AriaLabel", "Actions");
        """;

    private const string Button = """b.OpenComponent<NTFabMenuButtonItem>(0);b.AddComponentParameter(1, "Label", "Item");b.CloseComponent();""";

    private const string Anchor = """b.OpenComponent<NTFabMenuAnchorItem>(0);b.AddComponentParameter(1, "Label", "Home");b.AddComponentParameter(2, "Href", "/");b.CloseComponent();""";

    /// <summary>A menu with the given parameters whose child content is the given statements, written against the child builder <c>b</c>.</summary>
    private static Task<string[]> MenuAsync(string content, string parameters = Valid) =>
        IdsAsync($"{parameters}builder.AddAttribute(10, \"ChildContent\", (RenderFragment)(b => {{ {content} }}));");

    private static Task<string[]> IdsAsync(string parameters) => AnalyzerHarness.ComponentIdsAsync("NTFabMenu", parameters);

    /// <summary>An NTCard (an NT component the walker can see into) whose content is the given item statements.</summary>
    private static string Card(string items) => $"b.OpenComponent<NTCard>(5);b.AddAttribute(6, \"ChildContent\", (RenderFragment)(c => {{ {items.Replace("b.", "c.")} }}));b.CloseComponent();";

    private static string Repeat(string item, int count) => string.Concat(Enumerable.Repeat(item, count));

    [Fact]
    public async Task Valid_Menu_Is_Clean() => Assert.Empty(await MenuAsync(Button + Button));

    [Fact]
    public async Task Reports_Omitted_Or_Null_Icon() {
        Assert.Equal(["NTC1039"], await MenuAsync(Button + Button, """builder.AddComponentParameter(2, "AriaLabel", "Actions");"""));
        Assert.Equal(["NTC1039"], await MenuAsync(Button + Button, """builder.AddComponentParameter(1, "Icon", (NTIcon)null!);builder.AddComponentParameter(2, "AriaLabel", "Actions");"""));
    }

    [Theory]
    [InlineData("")]
    [InlineData("""builder.AddComponentParameter(2, "AriaLabel", null);""")]
    [InlineData("""builder.AddComponentParameter(2, "AriaLabel", " ");""")]
    public async Task Reports_Missing_Or_Blank_AriaLabel(string label) =>
        Assert.Equal(["NTC1040"], await MenuAsync(Button + Button, $"""builder.AddComponentParameter(1, "Icon", MaterialIcon.Add);{label}"""));

    [Theory]
    [InlineData("BackgroundColor")]
    [InlineData("TextColor")]
    [InlineData("SelectedFabBackgroundColor")]
    [InlineData("SelectedFabTextColor")]
    [InlineData("MenuItemBackgroundColor")]
    [InlineData("MenuItemTextColor")]
    public async Task Reports_Transparent_Color_Overrides(string name) {
        Assert.Equal(["NTC1041"], await MenuAsync(Button + Button, $"{Valid}builder.AddComponentParameter(3, \"{name}\", NTColor.Transparent);"));
        Assert.Empty(await MenuAsync(Button + Button, $"{Valid}builder.AddComponentParameter(3, \"{name}\", NTColor.Primary);"));
    }

    [Theory]
    [InlineData("Smallest", true)]
    [InlineData("Largest", true)]
    [InlineData("Medium", false)]
    public async Task Reports_Unsupported_Sizes(string size, bool reported) =>
        Assert.Equal(reported ? ["NTC1042"] : [], await MenuAsync(Button + Button, $"{Valid}builder.AddComponentParameter(3, \"ButtonSize\", NTSize.{size});"));

    [Fact]
    public async Task Reports_Undefined_Placement_Only() {
        Assert.Equal(["NTC1043"], await MenuAsync(Button + Button, $"""{Valid}builder.AddComponentParameter(3, "Placement", (NTFabButtonPlacement)99);"""));
        Assert.Empty(await MenuAsync(Button + Button, $"""{Valid}builder.AddComponentParameter(3, "Placement", NTFabButtonPlacement.Inline);"""));
    }

    [Fact]
    public async Task Reports_Menu_Without_Child_Content() => Assert.Equal(["NTC1044"], await IdsAsync(Valid));

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(6, false)]
    [InlineData(7, true)]
    public async Task Counts_Items_Written_Directly(int count, bool reported) =>
        Assert.Equal(reported ? ["NTC1044"] : [], await MenuAsync(Repeat(Button, count)));

    [Fact]
    public async Task Counts_Anchor_Items_And_Items_Inside_NT_Components() {
        Assert.Empty(await MenuAsync(Anchor + Button));
        Assert.Equal(["NTC1044"], await MenuAsync(Anchor));
        Assert.Empty(await MenuAsync(Card(Button) + Button));
        Assert.Equal(["NTC1044"], await MenuAsync(Card(Repeat(Button, 7))));
    }

    [Fact]
    public async Task Conditional_Items_Make_The_Count_Uncertain() {
        // @foreach and @if around items: the number of call sites is not the runtime count.
        Assert.Empty(await MenuAsync($"foreach (var i in new[] {{ 1, 2, 3 }}) {{ {Button} }}"));
        Assert.Empty(await MenuAsync($"if (DateTime.Now.Second > 30) {{ {Button} }}{Button}"));
        Assert.Empty(await MenuAsync($"if (DateTime.Now.Second > 30) {{ {Card(Button)} }}{Button}"));
        // Reported once the unconditional items alone are already too many.
        Assert.Equal(["NTC1044"], await MenuAsync($"foreach (var i in new[] {{ 1 }}) {{ {Button} }}{Repeat(Button, 7)}"));
    }

    [Fact]
    public async Task Opaque_Or_Foreign_Content_Makes_The_Count_Uncertain() {
        Assert.Empty(await IdsAsync($$"""{{Valid}}RenderFragment content = _ => { };builder.AddAttribute(10, "ChildContent", content);"""));
        Assert.Empty(await MenuAsync("b.OpenComponent<Microsoft.AspNetCore.Components.Routing.NavLink>(5);b.CloseComponent();" + Button));
        Assert.Equal(["NTC1044"], await MenuAsync("b.OpenComponent<Microsoft.AspNetCore.Components.Routing.NavLink>(5);b.CloseComponent();" + Repeat(Button, 7)));
    }

    [Fact]
    public async Task Reports_Item_Label_And_Href() {
        Assert.Equal(["NTC1045"], await MenuAsync("b.OpenComponent<NTFabMenuButtonItem>(0);b.CloseComponent();" + Button));
        Assert.Equal(["NTC1045"], await MenuAsync("""b.OpenComponent<NTFabMenuButtonItem>(0);b.AddComponentParameter(1, "Label", null);b.CloseComponent();""" + Button));
        Assert.Equal(["NTC1045", "NTC1046"], await MenuAsync("b.OpenComponent<NTFabMenuAnchorItem>(0);b.CloseComponent();" + Button));
        Assert.Equal(["NTC1046"], await MenuAsync("""b.OpenComponent<NTFabMenuAnchorItem>(0);b.AddComponentParameter(1, "Label", "Home");b.AddComponentParameter(2, "Href", " ");b.CloseComponent();""" + Button));
    }

    [Fact]
    public async Task DoesNotReport_Values_Known_Only_At_Runtime() {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync("""
            var text = DateTime.Now.ToString();
            builder.OpenComponent<NTFabMenu>(0);
            builder.AddComponentParameter(1, "Icon", MaterialIcon.Add);
            builder.AddComponentParameter(2, "AriaLabel", text);
            builder.AddComponentParameter(3, "ButtonSize", (NTSize)DateTime.Now.Second);
            builder.AddAttribute(4, "ChildContent", (RenderFragment)(b => {
                b.OpenComponent<NTFabMenuAnchorItem>(0);
                b.AddComponentParameter(1, "Label", text);
                b.AddComponentParameter(2, "Href", text);
                b.CloseComponent();
                b.OpenComponent<NTFabMenuButtonItem>(3);
                b.AddMultipleAttributes(4, new System.Collections.Generic.Dictionary<string, object>());
                b.CloseComponent();
            }));
            builder.CloseComponent();
            """);

        Assert.Empty(diagnostics);
    }
}
