namespace NTComponents.Analyzers.Tests;

public sealed class NTNavigationRailRules_Tests {
    private const string Menu = "Menu=" + ComponentSnippet.Fragment;

    [Theory]
    [InlineData("", "NTC1092")]
    [InlineData("AriaLabel=\"\"", "NTC1092")]
    [InlineData("AriaLabel=\"  \"", "NTC1092")]
    [InlineData("AriaLabel=\"Main\"", "")]
    [InlineData("AriaLabel=DateTime.Now.ToString()", "")]
    [InlineData("@", "")]
    public async Task AriaLabel(string parameters, string expected) => Assert.Equal(expected, await ComponentSnippet.IdsAsync("NTNavigationRail", parameters));

    [Theory]
    [InlineData("NTNavigationRailItem", "", "NTC1093")]
    [InlineData("NTNavigationRailItem", "Label=\"\"", "NTC1093")]
    [InlineData("NTNavigationRailItem", "Label=\"Home\"", "")]
    [InlineData("NTNavigationRailGroup", "", "NTC1093")]
    [InlineData("NTNavigationRailGroup", "Label=\"Group\"", "")]
    [InlineData("NTNavigationRailSectionHeader", "Label=\" \"", "NTC1093")]
    [InlineData("NTNavigationRailSectionHeader", "Label=\"Section\"", "")]
    [InlineData("NTNavigationRailSectionHeader", "Label=DateTime.Now.ToString()", "")]
    public async Task Label(string component, string parameters, string expected) => Assert.Equal(expected, await ComponentSnippet.IdsAsync(component, parameters));

    [Theory]
    [InlineData("AriaLevel=0", "NTC1094")]
    [InlineData("AriaLevel=7", "NTC1094")]
    [InlineData("Label=\"\" | AriaLevel=7", "NTC1093 NTC1094")]
    [InlineData("Label=\"Section\" | AriaLevel=1", "")]
    [InlineData("Label=\"Section\" | AriaLevel=6", "")]
    [InlineData("Label=\"Section\" | AriaLevel=DateTime.Now.Month", "")]
    public async Task AriaLevel(string parameters, string expected) => Assert.Equal(expected, await ComponentSnippet.IdsAsync("NTNavigationRailSectionHeader", parameters.Contains("Label") ? parameters : "Label=\"Section\" | " + parameters));

    [Theory]
    [InlineData("AriaLabel=\"Main\" | " + Menu + " | MenuHref=\"/x\"", "NTC1102")]
    [InlineData("AriaLabel=\"Main\" | " + Menu + " | HideRailOnXSScreens=false | MenuIcon=MaterialIcon.Menu | MenuAriaLabel=\"Open\"", "NTC1102 NTC1102")]
    [InlineData("AriaLabel=\"Main\" | " + Menu + " | HideRailOnXSScreens=false | MenuHref=\"/x\" | MenuIconWhenExpanded=MaterialIcon.Close", "NTC1102 NTC1102")]
    // The small-screen menu button still uses these.
    [InlineData("AriaLabel=\"Main\" | " + Menu + " | MenuIcon=MaterialIcon.Menu | MenuAriaLabel=\"Open\"", "")]
    [InlineData("AriaLabel=\"Main\" | " + Menu + " | HideRailOnXSScreens=false | CollapseBehavior=RailCollapseBehavior.Hide | MenuIcon=MaterialIcon.Menu", "")]
    [InlineData("AriaLabel=\"Main\" | " + Menu + " | HideRailOnXSScreens=DateTime.Now.Year > 1 | MenuIcon=MaterialIcon.Menu", "")]
    [InlineData("AriaLabel=\"Main\" | MenuHref=\"/x\" | MenuIcon=MaterialIcon.Menu", "")]
    [InlineData("AriaLabel=\"Main\" | Menu=(RenderFragment?)(DateTime.Now.Year > 0 ? null : null) | MenuHref=\"/x\"", "")]
    public async Task Custom_Menu_Ignores_The_Default_Button_Parameters(string parameters, string expected) => Assert.Equal(expected, await ComponentSnippet.IdsAsync("NTNavigationRail", parameters));

    [Theory]
    [InlineData("Label=\"Home\" | Href=\"/x\" | Type=ButtonType.Submit | ElementName=\"n\" | Value=\"v\"", "NTC1102 NTC1102 NTC1102")]
    [InlineData("Label=\"Home\" | Href=\"/x\" | Value=\"v\"", "NTC1102")]
    [InlineData("Label=\"Home\" | Type=ButtonType.Submit | ElementName=\"n\" | Value=\"v\"", "")]
    [InlineData("Label=\"Home\" | Href=\"\" | Value=\"v\"", "")]
    [InlineData("Label=\"Home\" | Href=DateTime.Now.ToString() | Value=\"v\"", "")]
    public async Task Link_Items_Ignore_Button_Parameters(string parameters, string expected) => Assert.Equal(expected, await ComponentSnippet.IdsAsync("NTNavigationRailItem", parameters));
}
