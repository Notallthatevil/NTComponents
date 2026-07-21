#if NET10_0_OR_GREATER
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using System.Collections.Concurrent;

namespace NTComponents.IntegrationTests.Site;

/// <summary>
///     Browser-level coverage for every generated NTComponents.Site component page and sandbox.
/// </summary>
[Collection(PlaywrightE2ECollection.Name)]
public sealed class DocumentationSite_IntegrationTests : IAsyncLifetime {
    private const int ExpectedComponentTypeCount = 88;
    private const int ExpectedRootRouteCount = 61;
    private static readonly string[] ExpectedDependentComponentNames = [
        "NTAccordionItem",
        "NTAutocompleteOption",
        "NTAutocompleteOptionGroup",
        "NTBody",
        "NTButtonGroupItem",
        "NTCarouselItem",
        "NTFabMenuAnchorItem",
        "NTFabMenuButtonItem",
        "NTFileUploadItem",
        "NTFooter",
        "NTHeader",
        "NTInputRadio",
        "NTInputSelectOption",
        "NTMenuAnchorItem",
        "NTMenuButtonItem",
        "NTMenuDividerItem",
        "NTMenuLabelItem",
        "NTMenuSubMenuItem",
        "NTNavigationRailGroup",
        "NTNavigationRailItem",
        "NTNavigationRailSectionHeader",
        "NTPageScript",
        "NTPropertyColumn",
        "NTTab",
        "NTTemplateColumn",
        "NTWizardFormStep",
        "NTWizardStep"
    ];
    private static readonly string[] GeneratedSampleSymbols = ["SampleOptions", "SampleItems", "SampleEvents", "LookupItemsAsync", "_sampleValue", "_sampleModel", "_wizardModel"];
    private readonly ConcurrentQueue<BrowserDiagnostic> _browserDiagnostics = new();
    private WebApplication? _application;
    private IBrowser? _browser;
    private IBrowserContext? _context;
    private IPage? _page;
    private IPlaywright? _playwright;
    private string _activeRoute = "/components";
    private string _baseUrl = default!;

    public async ValueTask InitializeAsync() {
        var repositoryRoot = FindRepositoryRoot();
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent?.Name ?? "Debug";
        var siteRoot = Path.Combine(repositoryRoot, "NTComponents.Site");
        var targetFramework = $"net{Environment.Version.Major}.0";
        var staticWebAssetsManifest = Path.Combine(siteRoot, "bin", configuration, targetFramework, "NTComponents.Site.staticwebassets.runtime.json");
        var staticWebAssetsEndpoints = Path.Combine(siteRoot, "obj", configuration, targetFramework, "staticwebassets.build.endpoints.json");
        if (!File.Exists(staticWebAssetsManifest)) {
            throw new FileNotFoundException($"Build NTComponents.Site before running its browser tests. Expected static-web-assets manifest: {staticWebAssetsManifest}");
        }
        if (!File.Exists(staticWebAssetsEndpoints)) {
            throw new FileNotFoundException($"Build NTComponents.Site before running its browser tests. Expected static-asset endpoints: {staticWebAssetsEndpoints}");
        }

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions {
            ContentRootPath = siteRoot
        });
        builder.Configuration[WebHostDefaults.StaticWebAssetsKey] = staticWebAssetsManifest;
        builder.WebHost.UseStaticWebAssets();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.SetMinimumLevel(LogLevel.Warning);

        _application = builder.Build();
        _application.UseStaticFiles();
        _application.MapStaticAssets(staticWebAssetsEndpoints);
        _application.MapFallbackToFile("index.html");
        await _application.StartAsync();

        var server = _application.Services.GetRequiredService<IServer>();
        _baseUrl = server.Features.Get<IServerAddressesFeature>()?.Addresses.Single()
            ?? throw new InvalidOperationException("The documentation test server did not publish an address.");

        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        _context = await _browser.NewContextAsync(new BrowserNewContextOptions {
            ExtraHTTPHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Accept-Encoding"] = "identity" }
        });
        _page = await _context.NewPageAsync();
        _page.Console += (_, message) => {
            if (string.Equals(message.Type, "error", StringComparison.Ordinal)) {
                _browserDiagnostics.Enqueue(new(_activeRoute, "console", message.Text));
            }
        };
        _page.PageError += (_, error) => _browserDiagnostics.Enqueue(new(_activeRoute, "pageerror", error));
    }

    public async ValueTask DisposeAsync() {
        if (_context is not null) {
            await _context.CloseAsync();
        }

        if (_browser is not null) {
            await _browser.CloseAsync();
        }

        _playwright?.Dispose();

        if (_application is not null) {
            await _application.StopAsync();
            await _application.DisposeAsync();
        }
    }

    [Fact]
    public async Task GeneratedComponentRoutes_RenderWorkingPreviewAndRazor_AndRepresentEveryPublicComponent() {
        ArgumentNullException.ThrowIfNull(_page);

        // Arrange
        await _page.GotoAsync($"{_baseUrl}/components", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        try {
            await _page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "NT components", Exact = true }).WaitForAsync();
        }
        catch (TimeoutException exception) {
            var diagnostics = string.Join("\n", _browserDiagnostics.Select(diagnostic => $"{diagnostic.Kind}: {FirstLine(diagnostic.Message)}"));
            throw new InvalidOperationException($"The documentation application did not start at {_page.Url}. Browser diagnostics:\n{diagnostics}", exception);
        }
        var indexedRoutes = await _page.Locator("a[href^='/components/']").EvaluateAllAsync<string[]>(
            "links => links.map(link => link.getAttribute('href')).filter(Boolean)");
        var routes = indexedRoutes.Distinct(StringComparer.Ordinal).OrderBy(route => route, StringComparer.Ordinal).ToArray();
        routes.Should().HaveCount(ExpectedRootRouteCount, "the generated component index should link to exactly the expected root component pages");

        var expectedComponentNames = typeof(NTButton).Assembly.ExportedTypes
            .Where(type => !type.IsAbstract && type.Name.StartsWith("NT", StringComparison.Ordinal) && typeof(IComponent).IsAssignableFrom(type))
            .Select(type => RemoveGenericArity(type.Name))
            .ToHashSet(StringComparer.Ordinal);
        expectedComponentNames.Should().HaveCount(ExpectedComponentTypeCount, "the browser coverage contract should be updated intentionally when the public NT component surface changes");
        var expectedDependentComponentNames = ExpectedDependentComponentNames.ToHashSet(StringComparer.Ordinal);
        expectedDependentComponentNames.Should().BeSubsetOf(expectedComponentNames, "every documented dependent type should remain part of the exported public component surface");
        var expectedRootComponentNames = expectedComponentNames.Except(expectedDependentComponentNames, StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal);
        expectedRootComponentNames.Should().HaveCount(ExpectedRootRouteCount, "every exported component should be classified as either a root route or a composed dependent demo");

        var failures = new List<string>();
        var rootComponentNames = new List<string>();
        var demoComponentNames = new HashSet<string>(StringComparer.Ordinal);

        // Act
        foreach (var route in routes) {
            _activeRoute = route;
            var diagnosticCountBeforeNavigation = _browserDiagnostics.Count;

            await _page.EvaluateAsync("href => document.querySelector(`a[href=\"${href}\"]`)?.click()", route);
            await _page.WaitForURLAsync($"**{route}");

            var heading = _page.Locator("main h1").First;
            await heading.WaitForAsync();
            var rootComponentName = RemoveGenericArity((await heading.InnerTextAsync()).Trim());
            rootComponentNames.Add(rootComponentName);
            var expectedRoute = $"/components/{rootComponentName.ToLowerInvariant()}";
            if (!string.Equals(route, expectedRoute, StringComparison.Ordinal)) {
                failures.Add($"{route}: root heading {rootComponentName} belongs at {expectedRoute}.");
            }

            var sandbox = _page.Locator(".docs-sandbox");
            if (await sandbox.CountAsync() != 1) {
                failures.Add($"{route}: expected exactly one sandbox.");
                continue;
            }

            var routeDemoComponentNames = (await sandbox.Locator("[data-docs-demo-component]").EvaluateAllAsync<string[]>(
                    "elements => elements.map(element => element.getAttribute('data-docs-demo-component')).filter(Boolean)"))
                .Select(RemoveGenericArity)
                .ToHashSet(StringComparer.Ordinal);
            demoComponentNames.UnionWith(routeDemoComponentNames);
            if (!routeDemoComponentNames.Contains(rootComponentName)) {
                failures.Add($"{route}: root component {rootComponentName} did not render a concrete data-docs-demo-component marker.");
            }

            var unsupportedMessages = await sandbox.Locator(":scope > .docs-callout").AllInnerTextsAsync();
            if (unsupportedMessages.Count > 0) {
                failures.Add($"{route}: unsupported sandbox: {string.Join(" | ", unsupportedMessages)}");
            }

            var preview = sandbox.Locator(".docs-sandbox-preview");
            if (!await TryWaitForVisibleAsync(preview)) {
                failures.Add($"{route}: Preview mode did not render.");
            }
            else {
                await ValidatePreviewAsync(failures, route, sandbox);
            }

            var controls = sandbox.Locator(".docs-sandbox-controls input:not([type='hidden']), .docs-sandbox-controls select, .docs-sandbox-controls textarea");
            var controlChange = await TryChangeFirstControlAsync(controls, _page);

            var razorButton = sandbox.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Razor", Exact = true });
            await razorButton.ClickAsync();
            await WaitForRenderAsync(_page);
            var generatedMarkup = sandbox.Locator(".docs-code-group pre code");
            if (!await TryWaitForVisibleAsync(generatedMarkup) || string.IsNullOrWhiteSpace(await generatedMarkup.InnerTextAsync())) {
                failures.Add($"{route}: Razor mode did not render nonempty generated markup.");
            }
            else if (controlChange is not null) {
                var markup = await generatedMarkup.InnerTextAsync();
                if (!markup.Contains(controlChange.Value, StringComparison.Ordinal) || controlChange.RequirePropertyName && !markup.Contains(controlChange.PropertyName, StringComparison.Ordinal)) {
                    failures.Add($"{route}: changing {controlChange.PropertyName} to '{controlChange.Value}' was not reflected in generated Razor.");
                }

                ValidateGeneratedRazor(failures, route, markup);
            }
            else {
                ValidateGeneratedRazor(failures, route, await generatedMarkup.InnerTextAsync());
            }

            var previewButton = sandbox.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Preview", Exact = true });
            await previewButton.ClickAsync();
            await WaitForRenderAsync(_page);
            if (!await TryWaitForVisibleAsync(preview)) {
                failures.Add($"{route}: Preview mode was not restored after viewing Razor markup.");
            }
            else {
                await ValidatePreviewAsync(failures, route, sandbox);
            }

            await WaitForRenderAsync(_page);
            foreach (var diagnostic in _browserDiagnostics.Skip(diagnosticCountBeforeNavigation)) {
                failures.Add($"{diagnostic.Route}: browser {diagnostic.Kind}: {FirstLine(diagnostic.Message)}");
            }
        }

        var duplicateRootComponentNames = rootComponentNames.GroupBy(name => name, StringComparer.Ordinal).Where(group => group.Count() > 1).Select(group => group.Key).OrderBy(name => name, StringComparer.Ordinal).ToArray();
        if (duplicateRootComponentNames.Length > 0) {
            failures.Add($"Components covered by more than one root route: {string.Join(", ", duplicateRootComponentNames)}");
        }

        var unexpectedRootComponentNames = rootComponentNames.Except(expectedRootComponentNames, StringComparer.Ordinal).OrderBy(name => name, StringComparer.Ordinal).ToArray();
        if (unexpectedRootComponentNames.Length > 0) {
            failures.Add($"Unexpected root component pages: {string.Join(", ", unexpectedRootComponentNames)}");
        }

        var missingRootComponentNames = expectedRootComponentNames.Except(rootComponentNames, StringComparer.Ordinal).OrderBy(name => name, StringComparer.Ordinal).ToArray();
        if (missingRootComponentNames.Length > 0) {
            failures.Add($"Expected root component pages missing from the component index: {string.Join(", ", missingRootComponentNames)}");
        }

        var unexpectedDemoComponentNames = demoComponentNames.Except(expectedComponentNames, StringComparer.Ordinal).OrderBy(name => name, StringComparer.Ordinal).ToArray();
        if (unexpectedDemoComponentNames.Length > 0) {
            failures.Add($"Unexpected concrete demo markers: {string.Join(", ", unexpectedDemoComponentNames)}");
        }

        var missingComponentNames = expectedComponentNames.Except(demoComponentNames, StringComparer.Ordinal).OrderBy(name => name, StringComparer.Ordinal).ToArray();
        if (missingComponentNames.Length > 0) {
            failures.Add($"Components missing concrete data-docs-demo-component markers: {string.Join(", ", missingComponentNames)}");
        }

        var missingDependentDemoComponentNames = expectedDependentComponentNames.Except(demoComponentNames, StringComparer.Ordinal).OrderBy(name => name, StringComparer.Ordinal).ToArray();
        if (missingDependentDemoComponentNames.Length > 0) {
            failures.Add($"Dependent component APIs without concrete demos: {string.Join(", ", missingDependentDemoComponentNames)}");
        }

        // Assert
        failures.Should().BeEmpty("every generated documentation component and sandbox should work in a real browser:\n{0}", string.Join("\n", failures.Distinct(StringComparer.Ordinal)));
    }

    [Fact]
    public async Task CuratedComponentDemos_RespondToUserInteraction() {
        ArgumentNullException.ThrowIfNull(_page);

        await OpenComponentDemoAsync("ntbutton");
        await _page.Locator("#docs-sandbox-ntbutton-elevation").SelectOptionAsync(new SelectOptionValue { Label = "Lowest" });
        await WaitForRenderAsync(_page);
        (await _page.Locator(".docs-generated-example .docs-callout").CountAsync()).Should().Be(0, "coordinated button controls should preserve a valid preview");
        await _page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Razor", Exact = true }).ClickAsync();
        var buttonMarkup = await _page.Locator(".docs-code-group pre code").InnerTextAsync();
        buttonMarkup.Should().Contain("Variant=\"Elevated\"").And.Contain("Elevation=\"Lowest\"");

        await OpenComponentDemoAsync("ntdialog");
        await _page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Open example dialog", Exact = true }).ClickAsync();
        await _page.Locator("dialog#docs-example-dialog[open]").WaitForAsync();

        await OpenComponentDemoAsync("ntmenu");
        await _page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Open example menu", Exact = true }).ClickAsync();
        await _page.Locator("#docs-example-menu:popover-open").WaitForAsync();
        await _page.GetByText("Edit", new PageGetByTextOptions { Exact = true }).WaitForAsync();

        await OpenComponentDemoAsync("ntsnackbar");
        await _page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Show example snackbar", Exact = true }).ClickAsync();
        await _page.GetByText("Changes saved", new PageGetByTextOptions { Exact = true }).WaitForAsync();

        await OpenComponentDemoAsync("nttoast");
        await _page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Show example toast", Exact = true }).ClickAsync();
        await _page.GetByText("Your changes were saved.", new PageGetByTextOptions { Exact = true }).WaitForAsync();

        await OpenComponentDemoAsync("nttooltip");
        await _page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Hover or focus for help", Exact = true }).FocusAsync();
        await _page.GetByRole(AriaRole.Tooltip).WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

        await OpenComponentDemoAsync("ntvirtualize");
        var virtualize = _page.Locator(".docs-curated-demo .virtualize");
        await _page.GetByText("Item 1", new PageGetByTextOptions { Exact = true }).WaitForAsync();
        await virtualize.EvaluateAsync("element => element.scrollTop = element.scrollHeight");
        await _page.GetByText("Item 100", new PageGetByTextOptions { Exact = true }).WaitForAsync();

        _browserDiagnostics.Should().BeEmpty("curated interactions should not produce browser errors");
    }

    private async Task OpenComponentDemoAsync(string slug) {
        ArgumentNullException.ThrowIfNull(_page);
        _activeRoute = $"/components/{slug}";
        await _page.GotoAsync($"{_baseUrl}{_activeRoute}", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await _page.Locator(".docs-sandbox-preview").WaitForAsync();
    }

    private static async Task ValidatePreviewAsync(ICollection<string> failures, string route, ILocator sandbox) {
        var generatedExample = sandbox.Locator(".docs-generated-example");
        var messages = (await generatedExample.Locator(".docs-callout").AllInnerTextsAsync())
            .Where(message => message.Contains("requires additional sample data", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (messages.Length > 0) {
            failures.Add($"{route}: Preview rendered the ErrorBoundary fallback: {string.Join(" | ", messages)}");
        }

        var previewText = (await generatedExample.InnerTextAsync()).Trim();
        if (previewText.Contains("No step provided", StringComparison.OrdinalIgnoreCase)) {
            failures.Add($"{route}: Preview rendered the invalid placeholder 'No step provided'.");
        }

        var hasVerifiedOutput = await generatedExample.EvaluateAsync<bool>(
            """
            element => element.querySelector('[data-docs-preview-verified="true"]') !== null
                || element.innerText.trim().length > 0
                || Array.from(element.querySelectorAll('*')).some(candidate => {
                    if (['SCRIPT', 'STYLE', 'LINK', 'META'].includes(candidate.tagName)) {
                        return false;
                    }

                    const style = getComputedStyle(candidate);
                    const rect = candidate.getBoundingClientRect();
                    return style.display !== 'none' && style.visibility !== 'hidden' && (rect.width > 0 || rect.height > 0);
                })
            """);
        if (!hasVerifiedOutput) {
            failures.Add($"{route}: Preview was blank and did not expose data-docs-preview-verified='true' for a successful nonvisual demo.");
        }
    }

    private static void ValidateGeneratedRazor(ICollection<string> failures, string route, string markup) {
        foreach (var symbol in GeneratedSampleSymbols.Where(symbol => markup.Contains(symbol, StringComparison.Ordinal))) {
            if (!HasCodeDeclaration(markup, symbol)) {
                failures.Add($"{route}: generated Razor references {symbol} without declaring it in an @code block.");
            }
        }

        if (string.Equals(route, "/components/ntlayout", StringComparison.Ordinal)) {
            RequireGeneratedMarkup(failures, route, markup, "<NTHeader", "<NTBody", "<NTFooter");
        }
        else if (string.Equals(route, "/components/ntsplitbutton", StringComparison.Ordinal)) {
            RequireGeneratedMarkup(failures, route, markup, "<NTMenuLabelItem", "<NTMenuButtonItem", "<NTMenuAnchorItem");
        }
        else if (string.Equals(route, "/components/ntscheduler", StringComparison.Ordinal)) {
            RequireGeneratedMarkup(failures, route, markup, "NTComponents.Scheduler.TnTEvent");
        }
    }

    private static bool HasCodeDeclaration(string markup, string symbol) {
        var codeIndex = markup.IndexOf("@code", StringComparison.Ordinal);
        if (codeIndex < 0) {
            return false;
        }

        return markup[codeIndex..].ReplaceLineEndings("\n").Split('\n', StringSplitOptions.TrimEntries)
            .Any(line => line.Contains(symbol, StringComparison.Ordinal)
                && (line.StartsWith("private ", StringComparison.Ordinal)
                    || line.StartsWith("protected ", StringComparison.Ordinal)
                    || line.StartsWith("internal ", StringComparison.Ordinal)
                    || line.StartsWith("public ", StringComparison.Ordinal)));
    }

    private static void RequireGeneratedMarkup(ICollection<string> failures, string route, string markup, params string[] requiredFragments) {
        var missingFragments = requiredFragments.Where(fragment => !markup.Contains(fragment, StringComparison.Ordinal)).ToArray();
        if (missingFragments.Length > 0) {
            failures.Add($"{route}: generated Razor omitted composed markup: {string.Join(", ", missingFragments)}");
        }
    }

    private static string FindRepositoryRoot() {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent) {
            if (File.Exists(Path.Combine(directory.FullName, "NTComponents.slnx"))) {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate the NTComponents repository root from the test output directory.");
    }

    private static string FirstLine(string value) {
        var lines = value.ReplaceLineEndings("\n").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return lines.FirstOrDefault(line => line.StartsWith("System.", StringComparison.Ordinal) || line.StartsWith("TypeError", StringComparison.Ordinal))
            ?? lines.FirstOrDefault()
            ?? value;
    }

    private static string RemoveGenericArity(string name) {
        var arityIndex = name.IndexOf('`');
        return arityIndex < 0 ? name : name[..arityIndex];
    }

    private static async Task<ControlChange?> TryChangeFirstControlAsync(ILocator controls, IPage page) {
        for (var index = 0; index < await controls.CountAsync(); index++) {
            var control = controls.Nth(index);
            if (!await control.IsVisibleAsync() || !await control.IsEnabledAsync()) {
                continue;
            }

            var propertyName = (await control.EvaluateAsync<string?>(
                "element => element.closest('.docs-control-row')?.querySelector('.nt-input-label, .nt-checkbox-label-text, .nt-switch-label-text')?.textContent?.trim() ?? null"))?.TrimEnd('*').Trim();
            if (string.IsNullOrWhiteSpace(propertyName)) {
                continue;
            }
            var tagName = await control.EvaluateAsync<string>("element => element.tagName");
            if (string.Equals(tagName, "SELECT", StringComparison.Ordinal)) {
                var options = await control.Locator("option").EvaluateAllAsync<SelectOption[]>(
                    "options => options.map(option => ({ value: option.value, label: option.textContent?.trim() ?? option.value, selected: option.selected }))");
                var replacement = options.FirstOrDefault(option => !option.Selected && !string.Equals(option.Label, "None", StringComparison.Ordinal))
                    ?? options.FirstOrDefault(option => !option.Selected);
                if (replacement is null) {
                    continue;
                }

                await control.SelectOptionAsync(new SelectOptionValue { Value = replacement.Value });
                await WaitForRenderAsync(page);
                return new(propertyName, replacement.Label, true);
            }

            var inputType = string.Equals(tagName, "INPUT", StringComparison.Ordinal)
                ? await control.GetAttributeAsync("type") ?? "text"
                : "text";
            if (string.Equals(inputType, "checkbox", StringComparison.OrdinalIgnoreCase)) {
                var changedValue = !await control.IsCheckedAsync();
                await control.SetCheckedAsync(changedValue);
                await WaitForRenderAsync(page);
                return new(propertyName, changedValue.ToString().ToLowerInvariant(), true);
            }

            if (string.Equals(inputType, "file", StringComparison.OrdinalIgnoreCase)) {
                continue;
            }

            var currentValue = await control.InputValueAsync();
            var changedText = await GetSafeChangedControlValueAsync(control, inputType, propertyName, currentValue);
            if (string.Equals(currentValue, changedText, StringComparison.Ordinal)) {
                continue;
            }

            await control.FillAsync(changedText);
            await control.PressAsync("Tab");
            await WaitForRenderAsync(page);
            return new(propertyName, changedText, !string.Equals(propertyName, "ChildContent", StringComparison.Ordinal));
        }

        return null;
    }

    private static async Task<string> GetSafeChangedControlValueAsync(ILocator control, string inputType, string propertyName, string currentValue) => inputType.ToLowerInvariant() switch {
        "color" => currentValue == "#123456" ? "#654321" : "#123456",
        "date" => currentValue == "2030-01-02" ? "2030-01-03" : "2030-01-02",
        "datetime-local" => currentValue == "2030-01-02T12:34" ? "2030-01-03T12:34" : "2030-01-02T12:34",
        "email" => currentValue == "browser-verification@example.com" ? "browser-verification-2@example.com" : "browser-verification@example.com",
        "month" => currentValue == "2030-01" ? "2030-02" : "2030-01",
        "number" or "range" => await control.EvaluateAsync<string>(
            """
            element => {
                const current = Number(element.value) || 0;
                const minimum = element.min === '' ? Number.NEGATIVE_INFINITY : Number(element.min);
                const maximum = element.max === '' ? Number.POSITIVE_INFINITY : Number(element.max);
                const step = element.step === '' || element.step === 'any' ? 1 : Number(element.step);
                const increased = current + step;
                return String(increased <= maximum ? increased : Math.max(minimum, current - step));
            }
            """),
        "time" => currentValue == "12:34" ? "13:34" : "12:34",
        "url" => currentValue == "https://example.com/browser-verification" ? "https://example.com/browser-verification-2" : "https://example.com/browser-verification",
        _ => GetSafeChangedText(propertyName)
    };

    private static string GetSafeChangedText(string propertyName) => propertyName switch {
        "Accept" => ".txt",
        "AnchorName" => "--docs-browser-verification",
        "CultureCode" => "en-US",
        "ElementId" => "docs-browser-verification",
        "Src" => "/js/docs.js?browser-verification=1",
        _ when propertyName.EndsWith("Css", StringComparison.Ordinal) => "/css/app.css?browser-verification=1",
        _ when propertyName.EndsWith("Gap", StringComparison.Ordinal) => "1rem",
        _ when propertyName.EndsWith("Width", StringComparison.Ordinal) => "12rem",
        _ => "Browser verification"
    };

    private static async Task<bool> TryWaitForVisibleAsync(ILocator locator) {
        try {
            await locator.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 2000 });
            return true;
        }
        catch (TimeoutException) {
            return false;
        }
    }

    private static Task WaitForRenderAsync(IPage page) => page.EvaluateAsync(
        "() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))");

    private sealed record BrowserDiagnostic(string Route, string Kind, string Message);

    private sealed record ControlChange(string PropertyName, string Value, bool RequirePropertyName);

    private sealed class SelectOption {
        public string Label { get; set; } = string.Empty;

        public bool Selected { get; set; }

        public string Value { get; set; } = string.Empty;
    }
}
#endif
