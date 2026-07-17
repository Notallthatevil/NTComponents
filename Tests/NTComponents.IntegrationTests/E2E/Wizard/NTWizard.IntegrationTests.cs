using Microsoft.Playwright;

namespace NTComponents.IntegrationTests.Wizard;

[Collection(PlaywrightE2ECollection.Name)]
public class NTWizard_IntegrationTests : IAsyncLifetime {

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
    public async Task Demo_Renders_New_Wizard_With_Legacy_Likeness() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToWizardDemoAsync();

        var legacyDemo = _page.GetByTestId("legacy-wizard-demo");
        var ntDemo = _page.GetByTestId("nt-wizard-demo");
        var legacyWizard = legacyDemo.Locator(".tnt-wizard");
        var ntWizard = ntDemo.Locator(".nt-wizard");

        await legacyWizard.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
        await ntWizard.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });

        (await legacyDemo.Locator(".tnt-wizard-step-indicator").CountAsync()).Should().Be(3);
        (await ntDemo.Locator(".nt-wizard-step-indicator").CountAsync()).Should().Be(3);

        var legacyText = await legacyWizard.InnerTextAsync();
        var ntText = await ntWizard.InnerTextAsync();
        foreach (var expected in new[] { "Wizard", "Details", "Start here", "Settings", "Configure", "Review", "Confirm", "Project details", "Next Step" }) {
            legacyText.Should().Contain(expected);
            ntText.Should().Contain(expected);
        }

        await AssertBoundingBoxesSimilarAsync(legacyWizard, ntWizard);

        (await legacyDemo.Locator(".tnt-wizard-step-indicator.current-step").InnerTextAsync()).Should().Contain("Details");
        (await ntDemo.Locator(".nt-wizard-step-indicator.current-step").InnerTextAsync()).Should().Contain("Details");

        await legacyDemo.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Next Step" }).ClickAsync();
        await ntDemo.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Next Step" }).ClickAsync();

        await _page.WaitForFunctionAsync(
            """
            () => document.querySelector('[data-testid="legacy-wizard-demo"] .tnt-wizard-step-indicator.current-step')?.textContent?.includes('Settings')
                && document.querySelector('[data-testid="nt-wizard-demo"] .nt-wizard-step-indicator.current-step')?.textContent?.includes('Settings')
            """,
            null,
            new PageWaitForFunctionOptions { Timeout = 5000 });

        (await legacyDemo.Locator(".tnt-wizard-step-indicator.completed-step").InnerTextAsync()).Should().Contain("Details");
        (await ntDemo.Locator(".nt-wizard-step-indicator.completed-step").InnerTextAsync()).Should().Contain("Details");
        await AssertBoundingBoxesSimilarAsync(legacyWizard, ntWizard);
    }

    [Fact]
    public async Task Horizontal_NTWizard_Matches_Legacy_Step_Layout() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToWizardDemoAsync();
        await _page.GetByTestId("wizard-layout-select").SelectOptionAsync("Horizontal");

        var legacyDemo = _page.GetByTestId("legacy-wizard-demo");
        var ntDemo = _page.GetByTestId("nt-wizard-demo");
        var legacyWizard = legacyDemo.Locator(".tnt-wizard");
        var ntWizard = ntDemo.Locator(".nt-wizard");

        await _page.WaitForFunctionAsync(
            """
            () => document.querySelector('[data-testid="legacy-wizard-demo"] .tnt-wizard')?.classList.contains('tnt-layout-horizontal') === true
                && document.querySelector('[data-testid="nt-wizard-demo"] .nt-wizard')?.classList.contains('nt-wizard-layout-horizontal') === true
            """,
            null,
            new PageWaitForFunctionOptions { Timeout = 5000 });

        var legacySteps = legacyDemo.Locator(".tnt-wizard-steps");
        var ntSteps = ntDemo.Locator(".nt-wizard-steps");
        (await ntSteps.EvaluateAsync<string>("element => getComputedStyle(element).flexDirection")).Should().Be(await legacySteps.EvaluateAsync<string>("element => getComputedStyle(element).flexDirection"));
        (await ntSteps.EvaluateAsync<string>("element => getComputedStyle(element).gap")).Should().Be(await legacySteps.EvaluateAsync<string>("element => getComputedStyle(element).gap"));

        var lastIndicatorClipDelta = await ntSteps.EvaluateAsync<double>(
            """
            element => {
                const container = element.getBoundingClientRect();
                const lastIndicator = element.querySelector('.nt-wizard-step-indicator:last-child');
                const lastTitle = lastIndicator?.querySelector('.nt-wizard-step-title');
                const lastIndex = lastIndicator?.querySelector('.nt-wizard-step-index');
                const indicatorRight = lastIndicator?.getBoundingClientRect().right ?? 0;
                const titleRight = lastTitle?.getBoundingClientRect().right ?? 0;
                const indexRight = lastIndex?.getBoundingClientRect().right ?? 0;

                return Math.max(indicatorRight, titleRight, indexRight) - container.right;
            }
            """);
        lastIndicatorClipDelta.Should().BeLessThanOrEqualTo(1);

        var legacyIndicators = legacyDemo.Locator(".tnt-wizard-step-indicator");
        var ntIndicators = ntDemo.Locator(".nt-wizard-step-indicator");
        (await ntIndicators.CountAsync()).Should().Be(await legacyIndicators.CountAsync());
        (await ntIndicators.Nth(0).EvaluateAsync<string>("element => getComputedStyle(element).columnGap")).Should().Be(await legacyIndicators.Nth(0).EvaluateAsync<string>("element => getComputedStyle(element).columnGap"));

        var layoutDeltas = await ntIndicators.EvaluateAllAsync<double[]>(
            """
            (ntElements) => {
                const legacyElements = Array.from(document.querySelectorAll('[data-testid="legacy-wizard-demo"] .tnt-wizard-step-indicator'));

                return ntElements.flatMap((element, index) => {
                    const legacy = legacyElements[index];
                    const title = element.querySelector('.nt-wizard-step-title')?.getBoundingClientRect();
                    const subtitle = element.querySelector('.nt-wizard-step-subtitle')?.getBoundingClientRect();
                    const legacyTitle = legacy?.querySelector('.tnt-wizard-step-title')?.getBoundingClientRect();
                    const legacySubtitle = legacy?.querySelector('.tnt-wizard-step-subtitle')?.getBoundingClientRect();

                    return [
                        Math.abs((title?.height ?? 0) - (legacyTitle?.height ?? 0)),
                        Math.abs((subtitle?.height ?? 0) - (legacySubtitle?.height ?? 0))
                    ];
                });
            }
            """);
        layoutDeltas.Should().OnlyContain(delta => delta <= 2);

        await AssertBoundingBoxesSimilarAsync(legacyWizard, ntWizard);
    }

    [Fact]
    public async Task Demos_Render_As_Full_Width_Sections() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToWizardDemoAsync();

        var splitComparisonItems = await _page.Locator(".wizard-demo .comparison > .surface").EvaluateAllAsync<bool[]>(
            """
            elements => elements.map(element => {
                const container = element.parentElement.getBoundingClientRect();
                const item = element.getBoundingClientRect();

                return Math.abs(container.width - item.width) <= 2;
            })
            """);
        splitComparisonItems.Should().OnlyContain(isFullWidth => isFullWidth);

        var permutationItems = await _page.Locator(".wizard-demo .scenario-grid > .wizard-shell").EvaluateAllAsync<bool[]>(
            """
            elements => elements.map(element => {
                const container = element.parentElement.getBoundingClientRect();
                const item = element.getBoundingClientRect();

                return Math.abs(container.width - item.width) <= 2;
            })
            """);
        permutationItems.Should().OnlyContain(isFullWidth => isFullWidth);
    }

    [Fact]
    public async Task Step_Headers_Expose_Tab_Semantics_And_Aria_State() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToWizardDemoAsync();

        var demo = _page.GetByTestId("nt-wizard-demo");
        var tablist = demo.GetByRole(AriaRole.Tablist);
        await tablist.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
        (await tablist.GetAttributeAsync("aria-orientation")).Should().Be("vertical");

        var tabs = demo.GetByRole(AriaRole.Tab);
        (await tabs.CountAsync()).Should().Be(3);
        (await tabs.Nth(0).GetAttributeAsync("aria-selected")).Should().Be("true");
        (await tabs.Nth(0).GetAttributeAsync("aria-current")).Should().Be("step");
        (await tabs.Nth(0).GetAttributeAsync("aria-controls")).Should().NotBeNullOrWhiteSpace();
        (await tabs.Nth(1).GetAttributeAsync("aria-disabled")).Should().Be("false");
        (await tabs.Nth(2).GetAttributeAsync("aria-disabled")).Should().Be("true");
        (await tabs.Nth(2).GetAttributeAsync("tabindex")).Should().Be("-1");
        (await tabs.Nth(2).EvaluateAsync<string>("element => getComputedStyle(element).cursor")).Should().Be("not-allowed");
    }

    [Fact]
    public async Task Linear_Flow_Skips_Disabled_Step_When_Progressing() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToWizardDemoAsync();

        var demo = _page.GetByTestId("nt-wizard-e2e-linear");
        await demo.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Next Step" }).ClickAsync();

        await WaitForCurrentStepAsync("nt-wizard-e2e-linear", "Review Flow");
        (await demo.Locator(".nt-wizard-step-indicator").Nth(0).GetAttributeAsync("class")).Should().Contain("completed-step");
        (await demo.Locator(".nt-wizard-step-indicator").Nth(1).GetAttributeAsync("class")).Should().Contain("disabled-step");
        (await demo.Locator(".nt-wizard-step-indicator").Nth(2).GetAttributeAsync("class")).Should().Contain("current-step");
        (await demo.InnerTextAsync()).Should().NotContain("Disabled step should never become current");
    }

    [Fact]
    public async Task Optional_Step_Skip_Marks_Skipped_And_Does_Not_Complete() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToWizardDemoAsync();

        var demo = _page.GetByTestId("nt-wizard-e2e-linear");
        await demo.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Skip" }).ClickAsync();

        await WaitForCurrentStepAsync("nt-wizard-e2e-linear", "Review Flow");
        await _page.WaitForFunctionAsync(
            "() => document.querySelector('[data-testid=\"nt-wizard-e2e-skip-status\"]')?.textContent?.includes('Skipped: 0') === true",
            null,
            new PageWaitForFunctionOptions { Timeout = 5000 });

        var optionalStepClass = await demo.Locator(".nt-wizard-step-indicator").Nth(0).GetAttributeAsync("class");
        optionalStepClass.Should().Contain("skipped-step");
        optionalStepClass.Should().NotContain("completed-step");
        (await demo.Locator(".nt-wizard-step-indicator").Nth(0).GetAttributeAsync("data-state")).Should().Be("skipped");
    }

    [Fact]
    public async Task Free_Navigation_Allows_Unvisited_Step_Header_Click() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToWizardDemoAsync();

        var demo = _page.GetByTestId("nt-wizard-e2e-free");
        (await demo.Locator("button.nt-wizard-step-button").Nth(2).EvaluateAsync<string>("element => getComputedStyle(element).cursor")).Should().Be("pointer");
        await demo.Locator("button.nt-wizard-step-button").Nth(2).ClickAsync();

        await WaitForCurrentStepAsync("nt-wizard-e2e-free", "Free Three");
        (await demo.Locator(".nt-wizard-step-indicator").Nth(2).GetAttributeAsync("class")).Should().Contain("current-step");
        (await demo.Locator(".nt-wizard-step-indicator").Nth(2).GetAttributeAsync("data-state")).Should().Be("current");
    }

    [Fact]
    public async Task Form_Step_Updates_Next_Button_While_Typing_And_Advances() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToWizardDemoAsync();

        var demo = _page.GetByTestId("nt-wizard-form-demo");
        var nextButton = demo.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Next Step" });

        await WaitForButtonDisabledAsync("nt-wizard-form-demo", "Next Step");
        (await nextButton.IsDisabledAsync()).Should().BeTrue();

        await demo.GetByLabel("Name").FillAsync("Nate");
        await WaitForButtonEnabledAsync("nt-wizard-form-demo", "Next Step");
        await nextButton.ClickAsync();

        await WaitForCurrentStepAsync("nt-wizard-form-demo", "Review");
        (await demo.InnerTextAsync()).Should().Contain("Ready");
    }

    [Fact]
    public async Task Async_Step_Validation_Blocks_Progress_And_Marks_Current_Step_Invalid() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToWizardDemoAsync();

        var demo = _page.GetByTestId("nt-wizard-e2e-validation");
        await demo.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Next Step" }).ClickAsync();

        await _page.WaitForFunctionAsync(
            "() => document.querySelector('[data-testid=\"nt-wizard-e2e-validation-status\"]')?.textContent?.includes('Validation rejected') === true",
            null,
            new PageWaitForFunctionOptions { Timeout = 5000 });

        (await demo.Locator(".nt-wizard-step-indicator").Nth(0).GetAttributeAsync("class")).Should().Contain("invalid-step");
        (await demo.Locator(".nt-wizard-step-indicator").Nth(0).GetAttributeAsync("class")).Should().Contain("current-step");
        (await demo.Locator(".nt-wizard-step-indicator").Nth(0).GetAttributeAsync("data-state")).Should().Be("current");

        await _page.WaitForFunctionAsync(
            """
            () => {
                const indicator = document.querySelector('[data-testid="nt-wizard-e2e-validation"] .nt-wizard-step-indicator.current-step.invalid-step');
                const index = indicator?.querySelector('.nt-wizard-step-index');
                if (!indicator || !index) {
                    return false;
                }

                const swatch = document.createElement('span');
                swatch.style.color = 'var(--tnt-color-error)';
                document.body.appendChild(swatch);

                const errorColor = getComputedStyle(swatch).color;
                const indexStyle = getComputedStyle(index);
                const connectorStyle = getComputedStyle(indicator, '::after');
                swatch.remove();

                return indexStyle.backgroundColor === errorColor
                    && indexStyle.borderColor === errorColor
                    && connectorStyle.backgroundColor === errorColor;
            }
            """,
            null,
            new PageWaitForFunctionOptions { Timeout = 5000 });

        var invalidCurrentColors = await demo.Locator(".nt-wizard-step-indicator.current-step.invalid-step").EvaluateAsync<string[]>(
            """
            element => {
                const swatch = document.createElement('span');
                swatch.style.color = 'var(--tnt-color-error)';
                document.body.appendChild(swatch);

                const errorColor = getComputedStyle(swatch).color;
                const index = element.querySelector('.nt-wizard-step-index');
                const indexStyle = getComputedStyle(index);
                const connectorStyle = getComputedStyle(element, '::after');
                swatch.remove();

                return [indexStyle.backgroundColor, indexStyle.borderColor, connectorStyle.backgroundColor, errorColor];
            }
            """);
        invalidCurrentColors[0].Should().Be(invalidCurrentColors[3]);
        invalidCurrentColors[1].Should().Be(invalidCurrentColors[3]);
        invalidCurrentColors[2].Should().Be(invalidCurrentColors[3]);

        (await demo.InnerTextAsync()).Should().Contain("Validate me");
        (await demo.InnerTextAsync()).Should().NotContain("Blocked target");
    }

    [Fact]
    public async Task Permutation_State_Showcase_Renders_All_Step_States_And_Layout_Options() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToWizardDemoAsync();

        var demo = _page.GetByTestId("nt-wizard-permutation-states");
        var wizard = demo.Locator(".nt-wizard");
        var indicators = demo.Locator(".nt-wizard-step-indicator");

        (await indicators.CountAsync()).Should().Be(5);
        (await indicators.Nth(0).GetAttributeAsync("data-state")).Should().Be("current");
        (await indicators.Nth(1).GetAttributeAsync("data-state")).Should().Be("completed");
        (await indicators.Nth(2).GetAttributeAsync("data-state")).Should().Be("invalid");
        (await indicators.Nth(3).GetAttributeAsync("data-state")).Should().Be("skipped");
        (await indicators.Nth(4).GetAttributeAsync("data-state")).Should().Be("disabled");

        var wizardClass = await wizard.GetAttributeAsync("class");
        wizardClass.Should().Contain("nt-wizard-layout-horizontal");
        wizardClass.Should().NotContain("nt-wizard-vertical-on-small-screens");
        wizardClass.Should().NotContain("nt-wizard-buttons-on-bottom");

        var firstIndicatorColumnGap = await indicators.Nth(0).EvaluateAsync<string>("element => getComputedStyle(element).columnGap");
        firstIndicatorColumnGap.Should().Be("8px");
    }

    [Fact]
    public async Task Inline_Progress_Buttons_Render_Near_Content_When_Not_Pushed_To_Bottom() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToWizardDemoAsync();

        var demo = _page.GetByTestId("nt-wizard-permutation-inline-buttons");
        var wizard = demo.Locator(".nt-wizard");
        var wizardClass = await wizard.GetAttributeAsync("class");
        wizardClass.Should().NotContain("nt-wizard-buttons-on-bottom");

        var contentBox = await demo.Locator(".nt-wizard-current-step, .nt-wizard-content").First.BoundingBoxAsync();
        var buttonBox = await demo.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Next Step" }).BoundingBoxAsync();
        contentBox.Should().NotBeNull();
        buttonBox.Should().NotBeNull();
        (buttonBox!.Y - (contentBox!.Y + contentBox.Height)).Should().BeLessThan(96);

        await demo.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Next Step" }).ClickAsync();
        await WaitForCurrentStepAsync("nt-wizard-permutation-inline-buttons", "Inline Submit");
        await demo.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Submit" }).WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
    }

    [Fact]
    public async Task Wizard_Does_Not_Overflow_Constrained_Parent() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToWizardDemoAsync(390, 900);

        var demo = _page.GetByTestId("nt-wizard-permutation-constrained-parent");
        var wizard = demo.Locator(".nt-wizard");
        await wizard.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
        await demo.ScrollIntoViewIfNeededAsync();

        var parentBox = await demo.BoundingBoxAsync();
        var wizardBox = await wizard.BoundingBoxAsync();
        parentBox.Should().NotBeNull();
        wizardBox.Should().NotBeNull();
        wizardBox!.Width.Should().BeLessThanOrEqualTo(parentBox!.Width + 1);

        var parentOverflows = await demo.EvaluateAsync<bool>("element => Math.ceil(element.scrollWidth) > Math.floor(element.clientWidth) + 1");
        parentOverflows.Should().BeFalse();

        var wizardOverflows = await wizard.EvaluateAsync<bool>("element => Math.ceil(element.scrollWidth) > Math.floor(element.clientWidth) + 1");
        wizardOverflows.Should().BeFalse();
    }

    [Fact]
    public async Task VerticalOnSmallScreens_Uses_Horizontal_Stacked_Shell_On_Small_Screens() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToWizardDemoAsync(390, 900);

        var demo = _page.GetByTestId("nt-wizard-permutation-vertical-small");
        var wizard = demo.Locator(".nt-wizard");
        await wizard.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
        await demo.ScrollIntoViewIfNeededAsync();

        var wizardClass = await wizard.GetAttributeAsync("class");
        wizardClass.Should().Contain("nt-wizard-layout-horizontal");
        wizardClass.Should().Contain("nt-wizard-vertical-on-small-screens");

        var layout = await wizard.EvaluateAsync<string[]>(
            """
            element => {
                const style = getComputedStyle(element);
                const steps = element.querySelector('.nt-wizard-steps');
                const content = element.querySelector('.nt-wizard-content, .nt-wizard-current-step');
                const buttons = element.querySelector('.nt-wizard-buttons');
                const stepsBox = steps.getBoundingClientRect();
                const contentBox = content.getBoundingClientRect();
                const buttonsBox = buttons.getBoundingClientRect();

                return [
                    style.gridTemplateAreas,
                    style.gridTemplateColumns,
                    getComputedStyle(steps).flexDirection,
                    String(contentBox.top >= stepsBox.bottom - 1),
                    String(buttonsBox.top >= contentBox.bottom - 1)
                ];
            }
            """);

        layout[0].Should().Contain("title");
        layout[0].Should().Contain("steps");
        layout[0].Should().Contain("content");
        layout[0].Should().Contain("buttons");
        layout[1].Split(' ', StringSplitOptions.RemoveEmptyEntries).Should().HaveCount(1);
        layout[2].Should().Be("column");
        layout[3].Should().Be("true");
        layout[4].Should().Be("true");
    }

    [Fact]
    public async Task Horizontal_Small_Screen_Centers_Current_Middle_Step_With_Adjacent_Steps() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToWizardDemoAsync(390, 900);

        var demo = _page.GetByTestId("nt-wizard-permutation-constrained-parent");
        var wizard = demo.Locator(".nt-wizard");
        await wizard.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
        await demo.ScrollIntoViewIfNeededAsync();
        await demo.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Next Step" }).ClickAsync();
        await WaitForCurrentStepAsync("nt-wizard-permutation-constrained-parent", "Second Step With A Long Name");

        var layout = await wizard.Locator(".nt-wizard-steps").EvaluateAsync<string[]>(
            """
            steps => {
                const visibleIndicators = Array.from(steps.querySelectorAll('.nt-wizard-step-indicator'))
                    .filter(element => getComputedStyle(element).display !== 'none');
                const current = steps.querySelector('.nt-wizard-step-indicator.current-step');
                const stepsBox = steps.getBoundingClientRect();
                const currentBox = current.getBoundingClientRect();
                const currentCenterDelta = Math.abs((currentBox.left + currentBox.width / 2) - (stepsBox.left + stepsBox.width / 2));

                return [
                    getComputedStyle(steps).display,
                    visibleIndicators.length.toString(),
                    visibleIndicators.map(element => element.textContent.trim()).join('|'),
                    currentCenterDelta.toString()
                ];
            }
            """);

        layout[0].Should().Be("grid");
        layout[1].Should().Be("3");
        layout[2].Should().Contain("Extremely Long Introductory Step Title");
        layout[2].Should().Contain("Second Step With A Long Name");
        layout[2].Should().Contain("Final Confirmation Step");
        double.Parse(layout[3], System.Globalization.CultureInfo.InvariantCulture).Should().BeLessThanOrEqualTo(2);
    }

    [Fact]
    public async Task Horizontal_Step_Labels_Are_Not_Clipped_And_Connectors_Fill_Remaining_Distance() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToWizardDemoAsync(390, 900);

        var demo = _page.GetByTestId("nt-wizard-permutation-constrained-parent");
        var wizard = demo.Locator(".nt-wizard");
        await wizard.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
        await demo.ScrollIntoViewIfNeededAsync();

        var midWordWrappingRules = await wizard.Locator(".nt-wizard-step-title, .nt-wizard-step-label, .nt-wizard-step-subtitle").EvaluateAllAsync<string[]>(
            """
            elements => Array.from(new Set(elements.map(element => getComputedStyle(element).overflowWrap)))
            """);
        midWordWrappingRules.Should().NotContain("anywhere");

        var connectorFillDeltas = await wizard.Locator(".nt-wizard-step-indicator:not(:last-child)").EvaluateAllAsync<double[]>(
            """
            elements => elements.map(element => {
                const title = element.querySelector('.nt-wizard-step-title');
                const style = getComputedStyle(element);
                const connectorStyle = getComputedStyle(element, '::after');
                const indicatorWidth = element.getBoundingClientRect().width;
                const titleWidth = title?.getBoundingClientRect().width ?? 0;
                const columnGap = Number.parseFloat(style.columnGap) || 0;
                const expectedWidth = Math.max(0, indicatorWidth - titleWidth - columnGap);
                const connectorWidth = Number.parseFloat(connectorStyle.width) || 0;

                return Math.abs(expectedWidth - connectorWidth);
            })
            """);
        connectorFillDeltas.Should().OnlyContain(delta => delta <= 2);
    }

    [Fact]
    public async Task Controlled_And_Guarded_Permutations_Update_Callback_Status() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToWizardDemoAsync();

        var controlled = _page.GetByTestId("nt-wizard-permutation-controlled");
        await controlled.Locator(".controls button").Nth(1).ClickAsync();
        await WaitForCurrentStepAsync("nt-wizard-permutation-controlled", "Controlled Two");

        await controlled.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Next Step" }).ClickAsync();
        await WaitForCurrentStepAsync("nt-wizard-permutation-controlled", "Controlled Three");
        (await _page.GetByTestId("nt-wizard-controlled-status").InnerTextAsync()).Should().Contain("Changed: 1->2");

        var guarded = _page.GetByTestId("nt-wizard-permutation-guarded");
        await guarded.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Next Step" }).ClickAsync();
        await WaitForCurrentStepAsync("nt-wizard-permutation-guarded", "Guarded Start");
        await _page.WaitForFunctionAsync(
            "() => document.querySelector('[data-testid=\"nt-wizard-guarded-status\"]')?.textContent?.includes('Guard blocked: 0->1') === true",
            null,
            new PageWaitForFunctionOptions { Timeout = 5000 });
        (await _page.GetByTestId("nt-wizard-guarded-status").InnerTextAsync()).Should().Contain("Guard blocked: 0->1");
    }

    [Fact]
    public async Task Button_And_Dom_Permutations_Render_Disabled_And_Retained_Modes() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToWizardDemoAsync();

        var disabledButtons = _page.GetByTestId("nt-wizard-permutation-disabled-buttons");
        (await disabledButtons.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Next Step" }).First.IsDisabledAsync()).Should().BeTrue();
        (await disabledButtons.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Previous Step" }).IsDisabledAsync()).Should().BeTrue();
        (await disabledButtons.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Submit" }).Nth(1).IsDisabledAsync()).Should().BeTrue();
        (await _page.GetByTestId("nt-wizard-disabled-submit-status").InnerTextAsync()).Should().Be("Submit disabled");

        (await _page.GetByTestId("nt-wizard-hidden-retained-panel").CountAsync()).Should().Be(1);
        (await _page.GetByTestId("nt-wizard-removed-deferred-panel").CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Optional_And_Form_Permutations_Cover_NoSkip_And_Invalid_Modes() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToWizardDemoAsync();

        var noSkip = _page.GetByTestId("nt-wizard-permutation-no-skip");
        (await noSkip.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Skip" }).CountAsync()).Should().Be(0);
        (await noSkip.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Next Step" }).CountAsync()).Should().Be(1);

        var grayForm = _page.GetByTestId("nt-wizard-permutation-gray-form");
        var grayNext = grayForm.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Next Step" });
        (await grayNext.IsDisabledAsync()).Should().BeFalse();
        (await grayNext.GetAttributeAsync("class")).Should().Contain("tnt-disabled");

        await grayNext.ClickAsync();
        await _page.WaitForFunctionAsync(
            "() => document.querySelector('[data-testid=\"nt-wizard-gray-form-status\"]')?.textContent?.includes('Gray form invalid') === true",
            null,
            new PageWaitForFunctionOptions { Timeout = 5000 });

        var optionsForm = _page.GetByTestId("nt-wizard-permutation-form-options");
        (await optionsForm.GetByLabel("Options Name").InputValueAsync()).Should().Be("Read only");
        (await optionsForm.GetByLabel("Options Name").EvaluateAsync<bool>("element => element.hasAttribute('readonly')")).Should().BeTrue();
    }

    [Fact]
    public async Task Deferred_Validation_Demo_Allows_Later_Step_Navigation_But_Blocks_Submit_Until_All_Complete() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToWizardDemoAsync();

        var demo = _page.GetByTestId("nt-wizard-deferred-validation-demo");
        await demo.ScrollIntoViewIfNeededAsync();

        (await demo.Locator(".nt-wizard-step-button").Nth(2).EvaluateAsync<string>("element => getComputedStyle(element).cursor")).Should().Be("not-allowed");
        await demo.Locator(".nt-wizard-step-button").Nth(2).ClickAsync(new LocatorClickOptions { Force = true });
        await WaitForCurrentStepAsync("nt-wizard-deferred-validation-demo", "Account");
        await WaitForButtonDisabledAsync("nt-wizard-deferred-validation-demo", "Next Step");

        await demo.GetByLabel("Account Name").FillAsync("Account ready");
        await WaitForButtonEnabledAsync("nt-wizard-deferred-validation-demo", "Next Step");

        await demo.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Next Step" }).ClickAsync();
        await WaitForCurrentStepAsync("nt-wizard-deferred-validation-demo", "Details");
        await WaitForButtonEnabledAsync("nt-wizard-deferred-validation-demo", "Next Step");

        await demo.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Next Step" }).ClickAsync();
        await WaitForCurrentStepAsync("nt-wizard-deferred-validation-demo", "Review");
        await WaitForButtonDisabledAsync("nt-wizard-deferred-validation-demo", "Submit");

        await demo.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Previous Step" }).ClickAsync();
        await WaitForCurrentStepAsync("nt-wizard-deferred-validation-demo", "Details");
        await demo.GetByLabel("Details Name").FillAsync("Details ready");
        await demo.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Next Step" }).ClickAsync();
        await WaitForCurrentStepAsync("nt-wizard-deferred-validation-demo", "Review");
        await WaitForButtonDisabledAsync("nt-wizard-deferred-validation-demo", "Submit");

        await demo.GetByLabel("Review Name").FillAsync("Review ready");
        await WaitForButtonEnabledAsync("nt-wizard-deferred-validation-demo", "Submit");
        await demo.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Submit" }).ClickAsync();

        await _page.WaitForFunctionAsync(
            "() => document.querySelector('[data-testid=\"nt-wizard-deferred-validation-status\"]')?.textContent?.includes('Deferred validation submitted') === true",
            null,
            new PageWaitForFunctionOptions { Timeout = 5000 });
    }

    private static async Task AssertBoundingBoxesSimilarAsync(ILocator legacyWizard, ILocator ntWizard) {
        var legacyBox = await legacyWizard.BoundingBoxAsync();
        var ntBox = await ntWizard.BoundingBoxAsync();

        legacyBox.Should().NotBeNull();
        ntBox.Should().NotBeNull();
        Math.Abs(legacyBox!.Width - ntBox!.Width).Should().BeLessThan(32);
        Math.Abs(legacyBox.Height - ntBox.Height).Should().BeLessThan(48);
    }

    private async Task NavigateToWizardDemoAsync(int viewportWidth = 1280, int viewportHeight = 900) {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.SetViewportSizeAsync(viewportWidth, viewportHeight);
        await _page.GotoAsync($"{_appBaseUrl}/nt-wizard", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await _page.Locator("h1").Filter(new LocatorFilterOptions { HasTextString = "NTWizard" })
            .WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
    }

    private async Task WaitForButtonEnabledAsync(string testId, string buttonName) {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.WaitForFunctionAsync(
            """
            ([testId, buttonName]) => {
                const root = document.querySelector(`[data-testid="${testId}"]`);
                const button = Array.from(root?.querySelectorAll('button') ?? [])
                    .find(button => button.textContent?.includes(buttonName));

                return button instanceof HTMLButtonElement && !button.disabled;
            }
            """,
            new[] { testId, buttonName },
            new PageWaitForFunctionOptions { Timeout = 5000 });
    }

    private async Task WaitForButtonDisabledAsync(string testId, string buttonName) {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.WaitForFunctionAsync(
            """
            ([testId, buttonName]) => {
                const root = document.querySelector(`[data-testid="${testId}"]`);
                const button = Array.from(root?.querySelectorAll('button') ?? [])
                    .find(button => button.textContent?.includes(buttonName));

                return button instanceof HTMLButtonElement && button.disabled;
            }
            """,
            new[] { testId, buttonName },
            new PageWaitForFunctionOptions { Timeout = 5000 });
    }

    private async Task WaitForCurrentStepAsync(string testId, string stepText) {
        ArgumentNullException.ThrowIfNull(_page);

        try {
            await _page.WaitForFunctionAsync(
                """
                ([testId, stepText]) => {
                    const root = document.querySelector(`[data-testid="${testId}"]`);
                    const current = root?.querySelector('.nt-wizard-step-indicator.current-step');

                    return current?.textContent?.includes(stepText) === true;
                }
                """,
                new[] { testId, stepText },
                new PageWaitForFunctionOptions { Timeout = 5000 });
        }
        catch (TimeoutException exception) {
            var rootText = await _page.GetByTestId(testId).InnerTextAsync();
            var stepStates = await _page.GetByTestId(testId).Locator(".nt-wizard-step-indicator").EvaluateAllAsync<string[]>(
                "elements => elements.map(element => `${element.getAttribute('class')}|${element.getAttribute('data-state')}|${element.textContent?.trim()}`)");
            throw new TimeoutException($"Timed out waiting for current step '{stepText}' in '{testId}'. Text: '{rootText}'. Steps: {string.Join(" || ", stepStates)}", exception);
        }
    }
}
