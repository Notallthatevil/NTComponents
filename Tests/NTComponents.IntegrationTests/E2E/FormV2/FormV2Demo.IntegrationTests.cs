using Microsoft.Playwright;

namespace NTComponents.IntegrationTests.FormV2;

/// <summary>
///     Browser coverage for the FormV2 LiveTest demo page.
/// </summary>
[Collection(PlaywrightE2ECollection.Name)]
public class FormV2Demo_IntegrationTests : IAsyncLifetime {
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

    [Fact]
    public async Task FormFields_Renders_Navigation_And_Hydrates_Interactive_Controls() {
        ArgumentNullException.ThrowIfNull(_fixture);
        ArgumentNullException.ThrowIfNull(_page);

        var response = await _page.GotoAsync($"{_fixture.ServerAddress}/form-fields", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        response.Should().NotBeNull();
        response!.Status.Should().Be(200);

        var navigationLink = _page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Form Fields", Exact = true });
        await navigationLink.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5_000 });
        (await navigationLink.GetAttributeAsync("href")).Should().Be("/form-fields");

        await _page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "NTForm Fields", Exact = true }).WaitForAsync();
        await _page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Hide controls", Exact = true }).ClickAsync();
        await _page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Show controls", Exact = true }).WaitForAsync();
    }

    [Fact]
    public async Task Submit_Marks_Required_Selects_And_Active_Date_Field_Invalid() {
        ArgumentNullException.ThrowIfNull(_fixture);
        ArgumentNullException.ThrowIfNull(_page);

        await _page.GotoAsync($"{_fixture.ServerAddress}/form-fields", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var submitButton = _page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Submit", Exact = true });
        await submitButton.ClickAsync();

        foreach (var fieldName in new[] { "Status", "Approved", "Priority" }) {
            await _page.Locator($"select[title='{fieldName}'][aria-invalid='true']").WaitForAsync();
        }

        var dateType = _page.GetByRole(AriaRole.Combobox, new PageGetByRoleOptions { Name = "Date type", Exact = true });
        foreach (var dateCase in new[] {
            new { InputKind = "Date", FieldName = "StartDate" },
            new { InputKind = "Month", FieldName = "StartDate" },
            new { InputKind = "Time", FieldName = "StartTime" },
            new { InputKind = "DateTime", FieldName = "Appointment" }
        }) {
            await dateType.SelectOptionAsync(dateCase.InputKind);
            await _page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Reset values", Exact = true }).ClickAsync();
            await submitButton.ClickAsync();
            await _page.Locator($"input[title='{dateCase.FieldName}'][aria-invalid='true']").WaitForAsync();
        }
    }
}
