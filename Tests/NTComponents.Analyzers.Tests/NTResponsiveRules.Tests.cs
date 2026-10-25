namespace NTComponents.Analyzers.Tests;

public sealed class NTResponsiveRules_Tests {

    [Theory]
    [InlineData("CustomBreakpoint=-1", "NTC1099")]
    [InlineData("CustomBreakpoint=0", "")]
    [InlineData("CustomBreakpoint=720", "")]
    [InlineData("CustomBreakpoint=(int?)null", "")]
    [InlineData("CustomBreakpoint=-DateTime.Now.Year", "")]
    public async Task CustomBreakpoint(string parameters, string expected) => Assert.Equal(expected, await ComponentSnippet.IdsAsync("NTResponsive", parameters));

    [Theory]
    [InlineData("Breakpoint=NTBreakpoint.Large | CustomBreakpoint=720", "NTC1102")]
    [InlineData("Breakpoint=NTBreakpoint.Large | CustomBreakpoint=0", "NTC1102")]
    [InlineData("Breakpoint=NTBreakpoint.Large", "")]
    [InlineData("Breakpoint=NTBreakpoint.Large | CustomBreakpoint=(int?)null", "")]
    [InlineData("Breakpoint=NTBreakpoint.Large | CustomBreakpoint=DateTime.Now.Year", "")]
    [InlineData("Breakpoint=NTBreakpoint.Large | CustomBreakpoint=-1", "NTC1099")]
    [InlineData("CustomBreakpoint=720", "")]
    public async Task Breakpoint_Is_Ignored_With_A_Custom_Breakpoint(string parameters, string expected) => Assert.Equal(expected, await ComponentSnippet.IdsAsync("NTResponsive", parameters));
}
