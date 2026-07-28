using Microsoft.Playwright;

namespace NTComponents.IntegrationTests.Form;

[Collection(PlaywrightE2ECollection.Name)]
public class NTInputCheckboxStaticSsr_IntegrationTests : IAsyncLifetime {

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
    public async Task Native_Checked_State_Updates_Static_Ssr_Visuals_Without_Root_Class_Changes() {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.GotoAsync($"{_appBaseUrl}/validation-render-modes", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var demo = _page.GetByTestId("validation-render-mode-ssr");
        var root = demo.Locator(".nt-checkbox");
        var input = demo.GetByRole(AriaRole.Checkbox, new LocatorGetByRoleOptions { Name = "I checked the validation state" });
        var labelText = root.Locator(".nt-checkbox-label-text");
        await input.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });

        var inputId = await input.GetAttributeAsync("id");
        var initialRootClass = await root.GetAttributeAsync("class");
        inputId.Should().NotBeNullOrWhiteSpace();
        initialRootClass.Should().NotContain("nt-checkbox-selected");
        await WaitForVisualStateAsync(inputId!, isChecked: false);

        await input.ClickAsync();
        await WaitForVisualStateAsync(inputId!, isChecked: true);
        await input.ClickAsync();
        await WaitForVisualStateAsync(inputId!, isChecked: false);

        await labelText.ClickAsync();
        await WaitForVisualStateAsync(inputId!, isChecked: true);
        await labelText.ClickAsync();
        await WaitForVisualStateAsync(inputId!, isChecked: false);

        await input.FocusAsync();
        await input.PressAsync("Space");
        await WaitForVisualStateAsync(inputId!, isChecked: true);
        await input.PressAsync("Space");
        await WaitForVisualStateAsync(inputId!, isChecked: false);

        (await root.GetAttributeAsync("class")).Should().Be(initialRootClass);
    }

    private async Task WaitForVisualStateAsync(string inputId, bool isChecked) {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.WaitForFunctionAsync(
            """
            ([inputId, isChecked]) => {
                const input = document.getElementById(inputId);
                const root = input?.closest('.nt-checkbox');
                const outline = root?.querySelector('.nt-checkbox-outline');
                const background = root?.querySelector('.nt-checkbox-background');
                const checkmark = root?.querySelector('.nt-checkbox-checkmark');
                const marks = root?.querySelectorAll('.nt-checkbox-checkmark .nt-checkbox-mark');

                if (!(input instanceof HTMLInputElement) || !outline || !background || !checkmark || !marks?.length) {
                    return false;
                }

                const outlineOpacity = Number.parseFloat(getComputedStyle(outline).opacity);
                const backgroundOpacity = Number.parseFloat(getComputedStyle(background).opacity);
                const checkmarkOpacity = Number.parseFloat(getComputedStyle(checkmark).opacity);
                const marksVisible = Array.from(marks).every(mark => Number.parseFloat(getComputedStyle(mark).strokeDashoffset) === 0);

                return input.checked === isChecked
                    && (isChecked
                        ? outlineOpacity === 0 && backgroundOpacity === 1 && checkmarkOpacity === 1 && marksVisible
                        : outlineOpacity === 1 && backgroundOpacity === 0 && checkmarkOpacity === 0 && !marksVisible);
            }
            """,
            new object[] { inputId, isChecked },
            new PageWaitForFunctionOptions { Timeout = 5000 });
    }
}
