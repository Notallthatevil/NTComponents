using Microsoft.Playwright;

namespace NTComponents.IntegrationTests.Wizard;

[Collection(PlaywrightE2ECollection.Name)]
public class NTWizardResponsive_IntegrationTests : IAsyncLifetime {

    private string _appBaseUrl = default!;
    private PlaywrightFixture? _fixture;
    private IPage? _page;

    public async ValueTask InitializeAsync() {
        _fixture = new PlaywrightFixture();
        await _fixture.InitializeAsync();
        _page = _fixture.Page;
        _appBaseUrl = _fixture.ServerAddress;
    }

    public async ValueTask DisposeAsync() {
        if (_fixture != null) {
            await _fixture.DisposeAsync();
        }
    }

    [Fact]
    public async Task Horizontal_Small_Screen_Shows_Three_Steps_At_First_And_Last_Positions() {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.SetViewportSizeAsync(390, 900);
        await _page.GotoAsync($"{_appBaseUrl}/nt-wizard", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var steps = _page.GetByTestId("nt-wizard-scroll-demo").Locator(".nt-wizard-steps");
        await steps.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });

        var visibleStepIndexes = await steps.EvaluateAsync<string[]>(
            """
            steps => {
                while (steps.querySelectorAll('.nt-wizard-step-indicator').length < 5) {
                    steps.append(steps.lastElementChild.cloneNode(true));
                }

                const indicators = Array.from(steps.querySelectorAll('.nt-wizard-step-indicator'));
                const visibleIndexes = () => indicators
                    .map((indicator, index) => getComputedStyle(indicator).display === 'none' ? null : index)
                    .filter(index => index !== null)
                    .join(',');
                const first = visibleIndexes();

                indicators[0].classList.remove('current-step');
                indicators[2].classList.add('current-step');
                const middle = visibleIndexes();

                indicators[2].classList.remove('current-step');
                indicators.at(-1).classList.add('current-step');

                return [first, middle, visibleIndexes()];
            }
            """);

        visibleStepIndexes[0].Should().Be("0,1,2");
        visibleStepIndexes[1].Should().Be("1,2,3");
        visibleStepIndexes[2].Should().Be("2,3,4");
    }
}
