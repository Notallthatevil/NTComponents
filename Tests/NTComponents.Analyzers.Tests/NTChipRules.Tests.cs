namespace NTComponents.Analyzers.Tests;

public sealed class NTChipRules_Tests {
    private const string Menu = "MenuContent=" + ComponentSnippet.Fragment;

    [Theory]
    [InlineData("", "NTC1090")]
    [InlineData("Label=\"\"", "NTC1090")]
    [InlineData("Label=null", "NTC1090")]
    [InlineData("Label=\"Tag\"", "")]
    [InlineData("Label=DateTime.Now.ToString()", "")]
    [InlineData("@", "")]
    public async Task Label(string parameters, string expected) => Assert.Equal(expected, await ComponentSnippet.IdsAsync("NTChip", parameters));

    [Theory]
    [InlineData("Label=\"Tag\" | Href=\"/x\" | Variant=NTChipVariant.Filter", "NTC1091")]
    [InlineData("Label=\"Tag\" | Href=\"/x\" | Selectable=true", "NTC1091")]
    [InlineData("Label=\"Tag\" | Href=\"/x\" | Variant=NTChipVariant.Filter | Selectable=null", "NTC1091")]
    [InlineData("Label=\"Tag\" | Href=\"/x\" | Variant=NTChipVariant.Filter | MenuContent=(RenderFragment?)null", "NTC1091")]
    [InlineData("Label=\"Tag\" | Href=\"/x\" | " + Menu, "NTC1091")]
    [InlineData("Label=\"Tag\" | Href=\"/x\" | Variant=NTChipVariant.Filter | " + Menu, "NTC1091")]
    [InlineData("Label=\"Tag\" | Selectable=true | " + Menu, "NTC1091")]
    [InlineData("Label=\"Tag\" | Href=\"/x\" | Selectable=true | " + Menu, "NTC1091 NTC1091")]
    public async Task Modes_That_Cannot_Combine(string parameters, string expected) => Assert.Equal(expected, await ComponentSnippet.IdsAsync("NTChip", parameters));

    [Theory]
    [InlineData("Label=\"Tag\" | Href=\"/x\"")]
    [InlineData("Label=\"Tag\" | Href=\"/x\" | Variant=NTChipVariant.Filter | Selectable=false")]
    [InlineData("Label=\"Tag\" | Variant=NTChipVariant.Filter")]
    [InlineData("Label=\"Tag\" | Selectable=true")]
    [InlineData("Label=\"Tag\" | " + Menu)]
    [InlineData("Label=\"Tag\" | Variant=NTChipVariant.Filter | " + Menu)]
    [InlineData("Label=\"Tag\" | Selectable=false | " + Menu)]
    public async Task Valid_Combinations(string parameters) => Assert.Equal("", await ComponentSnippet.IdsAsync("NTChip", parameters));

    [Theory]
    [InlineData("Label=\"Tag\" | Href=DateTime.Now.ToString() | Variant=NTChipVariant.Filter")]
    [InlineData("Label=\"Tag\" | Href=\"/x\" | Selectable=DateTime.Now.Year > 2000")]
    [InlineData("Label=\"Tag\" | Href=\"/x\" | Variant=NTChipVariant.Filter | MenuContent=(RenderFragment?)(DateTime.Now.Year > 0 ? null : null)")]
    [InlineData("Label=\"Tag\" | Href=\"/x\" | Variant=NTChipVariant.Filter | @")]
    [InlineData("Label=\"Tag\" | Selectable=true | MenuContent=(RenderFragment?)(DateTime.Now.Year > 0 ? null : null)")]
    public async Task Runtime_Unknown_Values_Are_Not_Reported(string parameters) => Assert.Equal("", await ComponentSnippet.IdsAsync("NTChip", parameters));
}
