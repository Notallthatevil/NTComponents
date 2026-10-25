namespace NTComponents.Analyzers.Tests;

public sealed class NTDialogRules_Tests {

    [Theory]
    [InlineData("", "NTC1100")]
    [InlineData("Title=\"\"", "NTC1100")]
    [InlineData("Title=\"\" | aria-label=\"\"", "NTC1100")]
    [InlineData("SupportingText=\"Sure?\"", "NTC1100")]
    // The dialog replaces a passed aria-labelledby with its own, so it does not name the dialog.
    [InlineData("aria-labelledby=\"heading\"", "NTC1100 NTC1109")]
    [InlineData("Title=\"Delete\"", "")]
    [InlineData("TitleContent=(RenderFragment<NTDialogParameters>)(parameters => b => { })", "")]
    [InlineData("aria-label=\"Delete\"", "")]
    [InlineData("Title=DateTime.Now.ToString()", "")]
    [InlineData("aria-label=DateTime.Now.ToString()", "")]
    [InlineData("TitleContent=(RenderFragment<NTDialogParameters>?)(DateTime.Now.Year > 0 ? null : null)", "")]
    [InlineData("@", "")]
    public async Task Accessible_Name(string parameters, string expected) => Assert.Equal(expected, await ComponentSnippet.IdsAsync("NTDialog", parameters, "NTC11"));
}
