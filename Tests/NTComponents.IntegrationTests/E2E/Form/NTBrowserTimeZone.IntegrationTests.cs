using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace NTComponents.IntegrationTests.Form;

[Collection(PlaywrightE2ECollection.Name)]
public class NTBrowserTimeZone_IntegrationTests : IAsyncLifetime {
    private PlaywrightFixture? _fixture;
    private IPage? _page;

    public async ValueTask InitializeAsync() {
        _fixture = new PlaywrightFixture();
        await _fixture.InitializeAsync();
        _page = _fixture.Page;
    }

    public async ValueTask DisposeAsync() {
        if (_fixture is not null) {
            await _fixture.DisposeAsync();
        }
    }

    // Behavior source: NTBrowserTimeZone public XML remarks and NTDocumentation compatibility details require a named UTC fallback in the initial server render.
    [Fact]
    public async Task Server_Prerender_Contains_The_Named_Utc_Fallback_For_Each_Render_Mode() {
        ArgumentNullException.ThrowIfNull(_fixture);

        using var response = await _fixture.HttpClient.GetAsync("/validation-render-modes", TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var fields = Regex.Matches(html, """<input\b[^>]*\bdata-nt-browser-time-zone="true"[^>]*>""").Select(match => match.Value).ToArray();

        response.EnsureSuccessStatusCode();
        fields.Should().HaveCount(3);
        foreach (var mode in new[] { "ssr", "server", "wasm" }) {
            var field = fields.Should().ContainSingle(markup => markup.Contains($"data-testid=\"{mode}-browser-time-zone-field\"", StringComparison.Ordinal)).Subject;
            field.Should().Contain("type=\"hidden\"");
            field.Should().Contain("name=\"Model.BrowserTimeZoneId\"");
            field.Should().Contain("value=\"UTC\"");
        }
    }

    // Behavior source: NTBrowserTimeZone public XML remarks require static SSR forms to submit the progressively enhanced value by name.
    [Fact]
    public async Task Static_Ssr_Form_Serializes_The_Detected_Browser_Time_Zone_By_Name() {
        ArgumentNullException.ThrowIfNull(_fixture);
        ArgumentNullException.ThrowIfNull(_page);

        await _page.GotoAsync($"{_fixture.ServerAddress}/validation-render-modes", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        var expectedTimeZoneId = await _page.EvaluateAsync<string>("Intl.DateTimeFormat().resolvedOptions().timeZone");
        var demo = _page.GetByTestId("validation-render-mode-ssr");
        var field = demo.GetByTestId("ssr-browser-time-zone-field");

        await Assertions.Expect(field).ToHaveValueAsync(expectedTimeZoneId, new LocatorAssertionsToHaveValueOptions { Timeout = 30000 });
        (await field.GetAttributeAsync("name")).Should().Be("Model.BrowserTimeZoneId");
        var submittedTimeZoneId = await demo.Locator("form").EvaluateAsync<string?>(
            "form => { const value = new FormData(form).get('Model.BrowserTimeZoneId'); return typeof value === 'string' ? value : null; }");

        submittedTimeZoneId.Should().Be(expectedTimeZoneId);
    }

    // Behavior source: NTBrowserTimeZone public XML remarks require interactive components to receive the detected value through ValueChanged after hydration.
    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task Interactive_Hydration_Publishes_The_Detected_Browser_Time_Zone_To_The_Bound_Value(string mode) {
        ArgumentNullException.ThrowIfNull(_fixture);
        ArgumentNullException.ThrowIfNull(_page);

        await _page.GotoAsync($"{_fixture.ServerAddress}/validation-render-modes", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        var expectedTimeZoneId = await _page.EvaluateAsync<string>("Intl.DateTimeFormat().resolvedOptions().timeZone");

        await Assertions.Expect(_page.GetByTestId($"{mode}-browser-time-zone-field")).ToHaveValueAsync(expectedTimeZoneId, new LocatorAssertionsToHaveValueOptions { Timeout = 30000 });
        await Assertions.Expect(_page.GetByTestId($"{mode}-browser-time-zone-value")).ToHaveTextAsync(expectedTimeZoneId, new LocatorAssertionsToHaveTextOptions { Timeout = 30000 });
    }
}
