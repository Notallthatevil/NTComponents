namespace NTComponents.Analyzers.Tests;

public sealed class NTVirtualizeRules_Tests {
    private const string Provider = "ItemsProvider=(NTVirtualizeItemsProvider<string>)(request => default)";

    [Theory]
    [InlineData("", "NTC1097")]
    [InlineData("ItemSize=40f", "NTC1097")]
    [InlineData("ItemsProvider=(NTVirtualizeItemsProvider<string>?)null", "NTC1097")]
    [InlineData(Provider, "")]
    [InlineData("ItemsProvider=(NTVirtualizeItemsProvider<string>?)(DateTime.Now.Year > 0 ? null : null)", "")]
    [InlineData("@", "")]
    public async Task ItemsProvider(string parameters, string expected) => Assert.Equal(expected, await ComponentSnippet.IdsAsync("NTVirtualize<string>", parameters));

    [Theory]
    [InlineData("ItemSize=0f", "NTC1098")]
    [InlineData("ItemSize=-5f", "NTC1098")]
    [InlineData("ItemSize=float.NaN", "NTC1098")]
    [InlineData("ItemSize=float.PositiveInfinity", "NTC1098")]
    [InlineData("ItemSize=0.5f", "")]
    [InlineData("ItemSize=40f", "")]
    [InlineData("ItemSize=(float)DateTime.Now.Year", "")]
    public async Task ItemSize(string parameters, string expected) => Assert.Equal(expected, await ComponentSnippet.IdsAsync("NTVirtualize<string>", Provider + " | " + parameters));
}
