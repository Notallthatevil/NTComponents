namespace NTComponents.Analyzers.Tests;

public sealed class NTBadgeRules_Tests {

    [Theory]
    [InlineData("NTButton")]
    [InlineData("NTIconButton")]
    [InlineData("NTFabButton")]
    [InlineData("NTChip")]
    [InlineData("NTTab")]
    [InlineData("NTNavigationRailItem")]
    [InlineData("NTMenuAnchorItem")]
    [InlineData("NTMenuButtonItem")]
    [InlineData("NTMenuSubMenuItem")]
    public async Task Host_Dot_Badge_Without_A_Label_Is_Reported(string component) {
        Assert.Equal("NTC1101", await ComponentSnippet.IdsAsync(component, "ShowBadge=true", "NTC1101"));
        Assert.Equal("NTC1101", await ComponentSnippet.IdsAsync(component, "ShowBadge=true | BadgeContent=\"\" | BadgeAriaLabel=\" \"", "NTC1101"));
    }

    [Theory]
    [InlineData("ShowBadge=true | BadgeContent=\"3\"")]
    [InlineData("ShowBadge=true | BadgeAriaLabel=\"3 unread\"")]
    [InlineData("ShowBadge=true | BadgeContent=\"3\" | BadgeAriaLabel=\"3 unread\"")]
    [InlineData("ShowBadge=false")]
    [InlineData("")]
    [InlineData("ShowBadge=DateTime.Now.Year > 1")]
    [InlineData("ShowBadge=true | BadgeContent=DateTime.Now.ToString()")]
    [InlineData("ShowBadge=true | BadgeAriaLabel=DateTime.Now.ToString()")]
    [InlineData("ShowBadge=true | @")]
    public async Task Host_Badge_That_Is_Hidden_Labeled_Or_Unknown_Is_Not_Reported(string parameters) => Assert.Equal("", await ComponentSnippet.IdsAsync("NTChip", "Label=\"Tag\" | " + parameters));

    [Theory]
    [InlineData("", "NTC1101")]
    [InlineData("Content=\"\" | AriaLabel=\"\"", "NTC1101")]
    [InlineData("Content=\"3\"", "")]
    [InlineData("AriaLabel=\"3 unread\"", "")]
    [InlineData("Content=DateTime.Now.ToString()", "")]
    [InlineData("@", "")]
    public async Task Standalone_Badge(string parameters, string expected) => Assert.Equal(expected, await ComponentSnippet.IdsAsync("NTBadge", parameters));
}
