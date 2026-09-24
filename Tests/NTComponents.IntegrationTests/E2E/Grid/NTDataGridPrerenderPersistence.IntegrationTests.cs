using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace NTComponents.IntegrationTests.Grid;

[Collection(PlaywrightE2ECollection.Name)]
public class NTDataGridPrerenderPersistence_IntegrationTests : IAsyncLifetime {
    private readonly PlaywrightFixture _fixture = new();

    public ValueTask InitializeAsync() => _fixture.InitializeAsync();
    public ValueTask DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    public async Task PersistenceEnabled_PrerenderCompletesAndHydrationReusesProviderResult() {
        var url = $"/grid-prerender-persistence?id={Guid.NewGuid():N}";
        var response = await _fixture.Page.GotoAsync($"{_fixture.ServerAddress}{url}");
        response.Should().NotBeNull();
        response!.Status.Should().Be(200);
        var html = await response.TextAsync();
        html.Should().Contain("Fetch 2");
        html.Should().NotContain("InvalidOperationException");

        await _fixture.Page.GetByRole(AriaRole.Button, new() { Name = "Show fetch count" }).ClickAsync();
        await Expect(_fixture.Page.GetByLabel("Provider fetch count")).ToHaveTextAsync("2");
        await Expect(_fixture.Page.GetByRole(AriaRole.Cell, new() { Name = "Fetch 2" })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task PersistenceDisabled_HydrationFetchesAgain() {
        await _fixture.Page.GotoAsync($"{_fixture.ServerAddress}/grid-prerender-persistence?id={Guid.NewGuid():N}&optOut=true");
        await _fixture.Page.GetByRole(AriaRole.Button, new() { Name = "Show fetch count" }).ClickAsync();
        await Expect(_fixture.Page.GetByLabel("Provider fetch count")).ToHaveTextAsync("4");
        await Expect(_fixture.Page.GetByRole(AriaRole.Cell, new() { Name = "Fetch 4" })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task DistinctPersistenceKeys_RestoreBothGridsWithoutDuplicateFetches() {
        var url = $"/grid-prerender-persistence?id={Guid.NewGuid():N}&two=true";
        var response = await _fixture.Page.GotoAsync($"{_fixture.ServerAddress}{url}");
        response.Should().NotBeNull();
        response!.Status.Should().Be(200);
        (await response.TextAsync()).Should().Contain("Prerender persistence grid");
        await Expect(_fixture.Page.GetByRole(AriaRole.Table, new() { Name = "Prerender persistence grid" })).ToHaveCountAsync(2);

        await _fixture.Page.GetByRole(AriaRole.Button, new() { Name = "Show fetch count" }).ClickAsync();
        await Expect(_fixture.Page.GetByLabel("Provider fetch count")).ToHaveTextAsync("4");
    }

    [Fact]
    public async Task RestoredProviderResult_ExplicitRefreshFetchesAgain() {
        await _fixture.Page.GotoAsync($"{_fixture.ServerAddress}/grid-prerender-persistence?id={Guid.NewGuid():N}");
        await Expect(_fixture.Page.GetByRole(AriaRole.Cell, new() { Name = "Fetch 2" })).ToBeVisibleAsync();

        await _fixture.Page.GetByRole(AriaRole.Button, new() { Name = "Refresh grid" }).ClickAsync();
        await Expect(_fixture.Page.GetByRole(AriaRole.Cell, new() { Name = "Fetch 3" })).ToBeVisibleAsync();
        await _fixture.Page.GetByRole(AriaRole.Button, new() { Name = "Show fetch count" }).ClickAsync();
        await Expect(_fixture.Page.GetByLabel("Provider fetch count")).ToHaveTextAsync("3");
    }
}
