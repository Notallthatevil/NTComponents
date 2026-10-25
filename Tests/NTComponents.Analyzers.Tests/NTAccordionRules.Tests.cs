namespace NTComponents.Analyzers.Tests;

public sealed class NTAccordionRules_Tests {

    [Theory]
    [InlineData("", "NTC1096")]
    [InlineData("Label=\"\"", "NTC1096")]
    [InlineData("Label=\"\" | HeaderContent=(RenderFragment?)null", "NTC1096")]
    [InlineData("Label=\"Section\"", "")]
    [InlineData("HeaderContent=" + ComponentSnippet.Fragment, "")]
    [InlineData("Label=\"\" | HeaderContent=" + ComponentSnippet.Fragment, "")]
    [InlineData("Label=DateTime.Now.ToString()", "")]
    [InlineData("Label=\"\" | HeaderContent=(RenderFragment?)(DateTime.Now.Year > 0 ? null : null)", "")]
    [InlineData("@", "")]
    public async Task Header(string parameters, string expected) => Assert.Equal(expected, await ComponentSnippet.IdsAsync("NTAccordionItem", parameters));

    [Fact]
    public async Task Label_Is_Ignored_When_HeaderContent_Is_Set() =>
        Assert.Equal("NTC1102", await ComponentSnippet.IdsAsync("NTAccordionItem", "Label=\"Section\" | HeaderContent=" + ComponentSnippet.Fragment));

    [Theory]
    [InlineData("GroupName=\"faq\"", "NTC1102")]
    [InlineData("GroupName=\"faq\" | LimitToOneExpanded=false", "NTC1102")]
    [InlineData("GroupName=\"faq\" | LimitToOneExpanded=true", "")]
    [InlineData("GroupName=\"faq\" | LimitToOneExpanded=DateTime.Now.Year > 1", "")]
    [InlineData("GroupName=\"faq\" | @", "")]
    [InlineData("GroupName=null", "")]
    [InlineData("LimitToOneExpanded=false", "")]
    public async Task GroupName_Needs_LimitToOneExpanded(string parameters, string expected) => Assert.Equal(expected, await ComponentSnippet.IdsAsync("NTAccordion", parameters));
}
