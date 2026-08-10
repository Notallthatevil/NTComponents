using Microsoft.Playwright;

namespace NTComponents.IntegrationTests.Form;

[Collection(PlaywrightE2ECollection.Name)]
public class NTInputDateTime_IntegrationTests : IAsyncLifetime {

    private PlaywrightFixture? _fixture;
    private IPage? _page;

    public async ValueTask InitializeAsync() {
        _fixture = new PlaywrightFixture();
        await _fixture.InitializeAsync();
        _page = _fixture.Page;
    }

    public async ValueTask DisposeAsync() {
        if (_fixture != null) {
            await _fixture.DisposeAsync();
        }
    }

    [Fact]
    public async Task Native_Date_Preserves_Segmented_Year_Entry_Inside_NTForm() {
        ArgumentNullException.ThrowIfNull(_fixture);
        ArgumentNullException.ThrowIfNull(_page);

        await _page.GotoAsync($"{_fixture.ServerAddress}/date-time-render-modes", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        var runtime = _page.GetByTestId("intersect-restriction-runtime");
        await Assertions.Expect(runtime).ToContainTextAsync("interactive", new LocatorAssertionsToContainTextOptions { Timeout = 15000 });

        var input = _page.Locator("#intersect-new-restriction-form").GetByLabel("Start Date");
        await input.PressSequentiallyAsync("01012000", new LocatorPressSequentiallyOptions { Delay = 25 });

        await Assertions.Expect(input).ToHaveValueAsync("2000-01-01");
        await Assertions.Expect(_page.GetByTestId("intersect-new-restriction-form-bound-start-date")).ToContainTextAsync("01/01/2000");

        await input.BlurAsync();

        await Assertions.Expect(input).ToHaveValueAsync("2000-01-01");
        var rootClass = await input.Locator("xpath=ancestor::div[contains(@class, 'nt-input-date-time')][1]").GetAttributeAsync("class");
        rootClass.Should().Contain("nt-modified");
        rootClass.Should().NotContain("nt-invalid");
    }

    [Fact]
    public async Task Native_Date_Markup_Remains_Available_In_Static_Ssr() {
        ArgumentNullException.ThrowIfNull(_fixture);

        using var response = await _fixture.HttpClient.GetAsync("/date-time-render-modes", TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        html.Should().Contain("SSR runtime: static");
        html.Should().Contain("id=\"ssr-native-date\"");
        html.Should().Contain("type=\"date\"");
        html.Should().Contain("format=\"MM/dd/yyyy\"");
        html.Should().Contain("data-tnt-dtp-native-input=\"true\"");
    }
}
