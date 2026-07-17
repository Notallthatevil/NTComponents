namespace NTComponents.IntegrationTests.Menus;

[Collection(PlaywrightE2ECollection.Name)]
public class NTMenuRoute_IntegrationTests {

    [Theory]
    [InlineData("/nt-menu", "NTMenu")]
    [InlineData("/nt-context-menu", "NTContextMenu")]
    public async Task Menu_Route_Returns_Prerendered_Content(string route, string expectedHeading) {
        using var factory = new NTWebAppFactory();
        using var client = new HttpClient {
            BaseAddress = new Uri(factory.ServerAddress),
            Timeout = TimeSpan.FromSeconds(5)
        };

        var markup = await client.GetStringAsync(route, Xunit.TestContext.Current.CancellationToken);

        markup.Should().Contain(expectedHeading);
    }
}
