namespace NTComponents.Analyzers.Tests;

public sealed class NTWindowRules_Tests {

    [Theory]
    [InlineData("", "NTC1095")]
    [InlineData("Title=\"\"", "NTC1095")]
    [InlineData("Title=\"  \"", "NTC1095")]
    [InlineData("Title=\"Inbox\"", "")]
    [InlineData("Title=DateTime.Now.ToString()", "")]
    [InlineData("@", "")]
    public async Task Title(string parameters, string expected) => Assert.Equal(expected, await ComponentSnippet.IdsAsync("NTWindow", parameters));
}
