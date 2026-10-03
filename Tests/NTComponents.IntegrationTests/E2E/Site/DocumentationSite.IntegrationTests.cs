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
using System.IO.Compression;
using System.Text;

namespace NTComponents.IntegrationTests.Site;

/// <summary>
///     Browser-level coverage for every generated NTComponents.Site component page and sandbox.
/// </summary>
public sealed class DocumentationSite_IntegrationTests : IAsyncLifetime {
    private const int ExpectedComponentTypeCount = 90;
    private const int ExpectedRootRouteCount = 64;
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

        var exportedComponentTypes = typeof(NTButton).Assembly.ExportedTypes
            .Where(type => !type.IsAbstract && type.Name.StartsWith("NT", StringComparison.Ordinal) && typeof(IComponent).IsAssignableFrom(type))
            .ToArray();
        var expectedComponentNames = exportedComponentTypes
            .Where(type => type != typeof(NTBadge) && !type.IsDefined(typeof(ObsoleteAttribute), inherit: false))
            .Select(type => RemoveGenericArity(type.Name))
            .ToHashSet(StringComparer.Ordinal);
        expectedComponentNames.Should().HaveCount(ExpectedComponentTypeCount, "the browser coverage contract should be updated intentionally when the public NT component surface changes");
        var expectedDependentComponentNames = ExpectedDependentComponentNames.Intersect(expectedComponentNames, StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal);
        var expectedRootComponentNames = expectedComponentNames.Except(expectedDependentComponentNames, StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal);
        expectedRootComponentNames.Should().HaveCount(ExpectedRootRouteCount, "every current exported component should be classified as either a root route or a composed dependent demo");

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

            await WaitForRenderAsync(_page);

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
            await ValidateControlLayoutAsync(failures, route, sandbox);
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
            await _page.SetViewportSizeAsync(390, 844);
            await WaitForRenderAsync(_page);
            var mobileOverflow = await _page.EvaluateAsync<int>("Math.max(document.documentElement.scrollWidth, document.body.scrollWidth) - innerWidth");
            if (mobileOverflow > 1) {
                failures.Add($"{route}: mobile page overflowed horizontally by {mobileOverflow}px.");
            }
            await _page.SetViewportSizeAsync(1280, 720);
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
    public async Task MaterialThemeCreator_GeneratesSixSchemes_UpdatesThePreviewLive_WithoutRecoloringTheSite() {
        ArgumentNullException.ThrowIfNull(_page);
        await _page.GotoAsync($"{_baseUrl}/", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await _page.Locator(".docs-home").WaitForAsync();
        var originalPrimary = await RootColorAsync("--tnt-color-primary");

        _activeRoute = "/tools/material-theme";
        await _page.GotoAsync($"{_baseUrl}{_activeRoute}", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await _page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Material 3 theme creator", Exact = true }).WaitForAsync();

        var generatedOutput = _page.Locator("[aria-label='Generated theme files']");
        await generatedOutput.WaitForAsync();
        (await generatedOutput.Locator("details").CountAsync()).Should().Be(6);
        await generatedOutput.Locator("summary").First.ClickAsync();
        var lightTheme = await generatedOutput.Locator("details").First.Locator("pre code").InnerTextAsync();
        lightTheme.Should().StartWith(":root {");
        lightTheme.Should().Contain("--tnt-color-primary: rgb(101 85 143);");
        lightTheme.Should().Contain("--tnt-color-on-assert-container:");
        lightTheme.Split("--tnt-color-", StringSplitOptions.None).Should().HaveCount(66, "the file should contain the 49 Material system roles and four 4-role NTComponents colors");
        var defaultExtendedColors = new[] {
            (Id: "success", Color: "#00c853"),
            (Id: "info", Color: "#0091ea"),
            (Id: "warning", Color: "#ffab00"),
            (Id: "assert", Color: "#aa00ff")
        };
        foreach (var color in defaultExtendedColors) {
            await Assertions.Expect(_page.Locator($"#theme-{color.Id}")).ToHaveValueAsync(color.Color);
        }

        var usageGuide = _page.GetByTestId("theme-usage-guide");
        await usageGuide.WaitForAsync();
        (await usageGuide.Locator(".theme-style-card").CountAsync()).Should().Be(9);
        (await usageGuide.Locator(".theme-role-card").CountAsync()).Should().Be(7);
        (await usageGuide.InnerTextAsync()).Should().Contain("Custom palette overrides always win");

        var preview = _page.Locator("[data-testid='theme-preview']");
        var previewButton = preview.Locator(".nt-button-filled");
        var previewHeading = preview.GetByRole(AriaRole.Heading, new LocatorGetByRoleOptions { Name = "See the theme in NTComponents", Exact = true });
        await _page.WaitForFunctionAsync("expected => getComputedStyle(document.querySelector('[data-testid=theme-preview] .nt-button-filled'), '::after').backgroundColor === expected", "rgb(101, 85, 143)");
        (await previewHeading.EvaluateAsync<string>("element => getComputedStyle(element).color")).Should().Be("rgb(101, 85, 143)");
        (await RootColorAsync("--tnt-color-primary")).Should().Be(originalPrimary, "the generated theme should be scoped to the preview");
        await _page.Locator("#theme-style").SelectOptionAsync("vibrant");
        await _page.WaitForFunctionAsync("expected => getComputedStyle(document.querySelector('[data-testid=theme-preview] .nt-button-filled'), '::after').backgroundColor === expected", "rgb(111, 25, 255)");
        await _page.Locator("#theme-style").SelectOptionAsync("tonal-spot");
        await _page.WaitForFunctionAsync("expected => getComputedStyle(document.querySelector('[data-testid=theme-preview] .nt-button-filled'), '::after').backgroundColor === expected", "rgb(101, 85, 143)");

        await _page.Locator(".theme-advanced summary").ClickAsync();
        (await _page.Locator(".theme-advanced input[type='checkbox']").CountAsync()).Should().Be(0);
        var paletteOverrides = new[] {
            (Id: "secondary", Color: "#005a80", Token: "--tnt-color-secondary"),
            (Id: "tertiary", Color: "#805200", Token: "--tnt-color-tertiary"),
            (Id: "neutral", Color: "#44505a", Token: "--tnt-color-surface"),
            (Id: "neutral-variant", Color: "#5b5f6a", Token: "--tnt-color-outline"),
            (Id: "error", Color: "#9f2030", Token: "--tnt-color-error")
        };
        var paletteDisplays = paletteOverrides
            .Select(palette => (palette.Id, palette.Token))
            .Prepend((Id: "primary", Token: "--tnt-color-primary"))
            .ToArray();
        foreach (var palette in paletteDisplays) {
            await WaitForPalettePickerToMatchPreviewAsync(palette.Id, palette.Token);
        }

        var overriddenProperties = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var palette in paletteOverrides) {
            var previousValue = await preview.EvaluateAsync<string>("(element, token) => element.style.getPropertyValue(token)", palette.Token);
            var picker = _page.Locator($"#theme-palette-{palette.Id}");
            await Assertions.Expect(picker).ToBeEnabledAsync();
            await picker.EvaluateAsync(
                """
                (element, color) => {
                    element.value = color;
                    element.dispatchEvent(new Event('input', { bubbles: true }));
                    element.dispatchEvent(new Event('change', { bubbles: true }));
                }
                """,
                palette.Color);
            await _page.WaitForFunctionAsync(
                "args => document.querySelector('[data-testid=theme-preview]').style.getPropertyValue(args.token) !== args.previousValue",
                new { token = palette.Token, previousValue });
            await WaitForPalettePickerToMatchPreviewAsync(palette.Id, palette.Token);
            overriddenProperties[palette.Token] = await preview.EvaluateAsync<string>("(element, token) => element.style.getPropertyValue(token)", palette.Token);
        }

        var downloadLink = _page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Download generated themes", Exact = true });
        (await downloadLink.GetAttributeAsync("download")).Should().Be("ntcomponents-theme-6750a4.zip");
        var primaryPicker = _page.Locator("#theme-primary");
        await primaryPicker.EvaluateAsync(
            """
            element => {
                for (const color of ['#00515c', '#005f6b', '#006874']) {
                    element.value = color;
                    element.dispatchEvent(new Event('input', { bubbles: true }));
                }
            }
            """);
        await _page.WaitForFunctionAsync("expected => getComputedStyle(document.querySelector('[data-testid=theme-preview] .nt-button-filled'), '::after').backgroundColor === expected", "rgb(0, 104, 116)");
        await WaitForPalettePickerToMatchPreviewAsync("primary", "--tnt-color-primary");
        (await previewHeading.EvaluateAsync<string>("element => getComputedStyle(element).color")).Should().Be("rgb(0, 104, 116)", "the preview heading should consume the live primary theme variable");
        await Assertions.Expect(primaryPicker.Locator("xpath=..").Locator("[data-theme-color-output]")).ToHaveTextAsync("#006874");
        (await downloadLink.GetAttributeAsync("download")).Should().Be("ntcomponents-theme-6750a4.zip", "live input should not rebuild the ZIP");
        (await RootColorAsync("--tnt-color-primary")).Should().Be(originalPrimary);

        await primaryPicker.EvaluateAsync("element => element.dispatchEvent(new Event('change', { bubbles: true }))");
        await Assertions.Expect(downloadLink).ToHaveAttributeAsync("download", "ntcomponents-theme-006874.zip");
        foreach (var property in overriddenProperties) {
            (await preview.EvaluateAsync<string>("(element, token) => element.style.getPropertyValue(token)", property.Key)).Should().Be(property.Value, $"the custom {property.Key} palette should survive a source-color change");
        }

        var previousPrimaryOverride = await preview.EvaluateAsync<string>("element => element.style.getPropertyValue('--tnt-color-primary')");
        var primaryOverridePicker = _page.Locator("#theme-palette-primary");
        await primaryOverridePicker.EvaluateAsync(
            """
            (element, color) => {
                element.value = color;
                element.dispatchEvent(new Event('input', { bubbles: true }));
                element.dispatchEvent(new Event('change', { bubbles: true }));
            }
            """,
            "#7b2cbf");
        await _page.WaitForFunctionAsync(
            "previous => document.querySelector('[data-testid=theme-preview]').style.getPropertyValue('--tnt-color-primary') !== previous",
            previousPrimaryOverride);
        await WaitForPalettePickerToMatchPreviewAsync("primary", "--tnt-color-primary");
        overriddenProperties["--tnt-color-primary"] = await preview.EvaluateAsync<string>("element => element.style.getPropertyValue('--tnt-color-primary')");

        await primaryPicker.EvaluateAsync(
            """
            (element, color) => {
                element.value = color;
                element.dispatchEvent(new Event('input', { bubbles: true }));
                element.dispatchEvent(new Event('change', { bubbles: true }));
            }
            """,
            "#b3261e");
        await Assertions.Expect(downloadLink).ToHaveAttributeAsync("download", "ntcomponents-theme-b3261e.zip");
        (await preview.EvaluateAsync<string>("element => element.style.getPropertyValue('--tnt-color-primary')")).Should().Be(overriddenProperties["--tnt-color-primary"], "the custom primary palette should survive a source-color change");

        await _page.Locator("#theme-style").SelectOptionAsync("vibrant");
        await Assertions.Expect(usageGuide.Locator(".theme-style-card.selected h4")).ToHaveTextAsync("Vibrant");
        foreach (var property in overriddenProperties) {
            (await preview.EvaluateAsync<string>("(element, token) => element.style.getPropertyValue(token)", property.Key)).Should().Be(property.Value, $"the custom {property.Key} palette should survive a style change");
        }
        await _page.Locator("#theme-style").SelectOptionAsync("tonal-spot");
        await Assertions.Expect(usageGuide.Locator(".theme-style-card.selected h4")).ToHaveTextAsync("Tonal spot");

        var lightSurface = await preview.EvaluateAsync<string>("element => getComputedStyle(element).backgroundColor");
        var lightPrimary = await previewButton.EvaluateAsync<string>("element => getComputedStyle(element, '::after').backgroundColor");
        await _page.Locator("#theme-preview-mode").SelectOptionAsync("dark");
        await _page.WaitForFunctionAsync("previous => getComputedStyle(document.querySelector('[data-testid=theme-preview] .nt-button-filled'), '::after').backgroundColor !== previous", lightPrimary);
        foreach (var palette in paletteDisplays) {
            await WaitForPalettePickerToMatchPreviewAsync(palette.Id, palette.Token);
        }
        (await preview.EvaluateAsync<string>("element => getComputedStyle(element).backgroundColor")).Should().NotBe(lightSurface);
        (await previewButton.EvaluateAsync<string>("element => getComputedStyle(element, '::after').backgroundColor"))
            .Should().NotBe(lightPrimary, "the representative NTButton should consume the selected dark preview token");
        (await RootColorAsync("--tnt-color-primary")).Should().Be(originalPrimary);

        var downloadUrl = await downloadLink.GetAttributeAsync("href");
        downloadUrl.Should().StartWith("data:application/zip;base64,");
        using (var archiveStream = new MemoryStream(Convert.FromBase64String(downloadUrl![(downloadUrl.IndexOf(',') + 1)..])))
        using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Read)) {
            archive.Entries.Should().HaveCount(6);
            archive.Entries.Should().OnlyContain(entry => entry.FullName.StartsWith("Themes/", StringComparison.Ordinal));
        }

        await _page.Locator("a[href='/']").First.ClickAsync();
        await _page.WaitForURLAsync($"{_baseUrl}/");
        (await RootColorAsync("--tnt-color-primary")).Should().Be(originalPrimary);
        _browserDiagnostics.Where(diagnostic => string.Equals(diagnostic.Route, _activeRoute, StringComparison.Ordinal)).Should().BeEmpty();
    }

    [Fact]
    public async Task MaterialThemeConverter_ConvertsSixGeneratedSchemes_AndBuildsReadyToCopyArchive() {
        ArgumentNullException.ThrowIfNull(_page);
        _activeRoute = "/tools/material-theme";
        await _page.GotoAsync($"{_baseUrl}{_activeRoute}", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await _page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Material 3 theme creator", Exact = true }).WaitForAsync();

        var sourceDirectory = Path.Combine(Path.GetTempPath(), $"ntcomponents-theme-{Guid.NewGuid():N}");
        Directory.CreateDirectory(sourceDirectory);
        try {
            var files = new[] {
                ("light.css", ".light"),
                ("light-mc.css", ".light-medium-contrast"),
                ("light-hc.css", ".light-high-contrast"),
                ("dark.css", ".dark"),
                ("dark-mc.css", ".dark-medium-contrast"),
                ("dark-hc.css", ".dark-high-contrast")
            };
            foreach (var (name, selector) in files) {
                await File.WriteAllTextAsync(Path.Combine(sourceDirectory, name), CreateThemeSource(selector), Encoding.UTF8, TestContext.Current.CancellationToken);
            }

            await _page.Locator("#material-theme-files").SetInputFilesAsync(sourceDirectory);
            await _page.Locator(".theme-import .theme-message").WaitForAsync();
        }
        finally {
            Directory.Delete(sourceDirectory, recursive: true);
        }

        var conversionErrors = await _page.Locator(".theme-error").AllInnerTextsAsync();
        conversionErrors.Should().BeEmpty("the selected Material Theme Builder folder should convert successfully: {0}", string.Join(" | ", conversionErrors));
        await _page.GetByText("Six converted theme files are ready", new PageGetByTextOptions { Exact = true }).WaitForAsync();
        var output = _page.Locator("[aria-label='Converted theme files']");
        (await output.Locator("details").CountAsync()).Should().Be(6);
        await output.Locator("summary").First.ClickAsync();
        var preview = await output.Locator("details").First.Locator("pre code").InnerTextAsync();
        preview.Should().StartWith(":root {");
        preview.Should().Contain("--tnt-color-primary: rgb(1 2 3);");
        preview.Should().Contain("--tnt-color-on-success-container: rgb(4 5 6);");
        preview.Should().NotContain("--md-");

        var downloadLink = _page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Download converted themes", Exact = true });
        (await downloadLink.GetAttributeAsync("download")).Should().Be("ntcomponents-themes.zip");
        var downloadUrl = await downloadLink.GetAttributeAsync("href");
        downloadUrl.Should().StartWith("data:application/zip;base64,");
        var archiveBytes = Convert.FromBase64String(downloadUrl![(downloadUrl.IndexOf(',') + 1)..]);
        using var archiveStream = new MemoryStream(archiveBytes);
        using var archive = new ZipArchive(archiveStream, ZipArchiveMode.Read);
        archive.Entries.Select(entry => entry.FullName).Should().Equal(
            "Themes/light.css",
            "Themes/light-mc.css",
            "Themes/light-hc.css",
            "Themes/dark.css",
            "Themes/dark-mc.css",
            "Themes/dark-hc.css");

        using var lightReader = new StreamReader(archive.GetEntry("Themes/light.css")!.Open());
        (await lightReader.ReadToEndAsync(TestContext.Current.CancellationToken)).Should().Be(preview.ReplaceLineEndings("\r\n"));
        _browserDiagnostics.Where(diagnostic => string.Equals(diagnostic.Route, _activeRoute, StringComparison.Ordinal)).Should().BeEmpty();
    }

    [Fact]
    public async Task GettingStarted_RendersSetupGuide_AndComponentsStartCollapsed() {
        ArgumentNullException.ThrowIfNull(_page);
        _activeRoute = "/";
        await _page.GotoAsync(_baseUrl);
        var navigation = _page.GetByRole(AriaRole.Navigation, new PageGetByRoleOptions { Name = "Documentation navigation", Exact = true });
        var components = navigation.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Components", Exact = true });
        await Assertions.Expect(components).ToHaveAttributeAsync("aria-expanded", "false");
        var panel = navigation.Locator(".nt-navigation-rail-group-panel[aria-label='Components destinations']");
        await Assertions.Expect(panel).ToHaveAttributeAsync("inert", "");
        (await panel.EvaluateAsync<double>("element => element.getBoundingClientRect().height")).Should().BeLessThanOrEqualTo(1);
        await components.ClickAsync();
        await Assertions.Expect(components).ToHaveAttributeAsync("aria-expanded", "true");
        await Assertions.Expect(navigation.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "All components", Exact = true })).ToBeVisibleAsync();
        await components.ClickAsync();
        await Assertions.Expect(components).ToHaveAttributeAsync("aria-expanded", "false");
        await Assertions.Expect(panel).ToHaveAttributeAsync("inert", "");

        _activeRoute = "/getting-started";
        await navigation.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "Getting started", Exact = true }).ClickAsync();
        await _page.WaitForURLAsync("**/getting-started");
        await Assertions.Expect(_page.Locator("main h1")).ToHaveTextAsync("Getting started");
        (await _page.Locator("main h2").CountAsync()).Should().Be(8);
        var guideCode = string.Join("\n", await _page.Locator("main pre code").AllTextContentsAsync());
        guideCode.Should().Contain("dotnet add package NTComponents").And.Contain("builder.Services.AddNTServices();")
            .And.Contain("<NTHeadDependencies />").And.Contain("<HeadContent>").And.Contain("<NTToast />")
            .And.Contain("@page \"/welcome\"").And.Contain("light-hc.css").And.Contain("dark-hc.css");
        await Assertions.Expect(_page.Locator("main a[href='/tools/material-theme']")).ToBeVisibleAsync();
        await _page.ReloadAsync();
        await Assertions.Expect(_page.Locator("main h1")).ToHaveTextAsync("Getting started");
        await Assertions.Expect(components).ToHaveAttributeAsync("aria-expanded", "false");
        await _page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(FindRepositoryRoot(), "artifacts", "site-getting-started-desktop.png") });
        await _page.SetViewportSizeAsync(390, 844);
        await WaitForRenderAsync(_page);
        await navigation.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Collapse navigation rail", Exact = true }).ClickAsync();
        await Assertions.Expect(_page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Expand navigation rail", Exact = true })).ToBeVisibleAsync();
        await _page.EvaluateAsync("() => Promise.allSettled(document.getAnimations().filter(animation => animation.effect?.getTiming().iterations !== Infinity).map(animation => animation.finished))");
        (await _page.Locator("main h1").BoundingBoxAsync())!.X.Should().BeGreaterThanOrEqualTo(0);
        (await _page.EvaluateAsync<int>("Math.max(document.documentElement.scrollWidth, document.body.scrollWidth) - innerWidth")).Should().BeLessThanOrEqualTo(1);
        await _page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(FindRepositoryRoot(), "artifacts", "site-getting-started-mobile.png") });
        _browserDiagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task HeaderThemeSelector_ChangesAndPersistsTheme_AndFitsMobile() {
        ArgumentNullException.ThrowIfNull(_page);
        await OpenComponentDemoAsync("ntbutton");
        var selector = _page.Locator(".docs-topbar nt-theme-toggle select");
        await Assertions.Expect(selector).ToBeVisibleAsync();
        (await selector.Locator("option").AllTextContentsAsync()).Should().Equal("Light", "Dark", "System");

        var activeTheme = _page.Locator("link[data-nt-theme][data-nt-theme-loaded='true']");
        await selector.SelectOptionAsync("LIGHT-DEFAULT");
        await Assertions.Expect(activeTheme).ToHaveAttributeAsync("href", new System.Text.RegularExpressions.Regex("/Themes/light\\.css$"));
        var lightSurface = await RootColorAsync("--tnt-color-surface");
        await selector.SelectOptionAsync("DARK-DEFAULT");
        await Assertions.Expect(activeTheme).ToHaveAttributeAsync("href", new System.Text.RegularExpressions.Regex("/Themes/dark\\.css$"));
        (await RootColorAsync("--tnt-color-surface")).Should().NotBe(lightSurface);
        (await _page.EvaluateAsync<string>("localStorage.getItem('NTComponentsStoredThemeKey')")).Should().Be("DARK");
        await _page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(FindRepositoryRoot(), "artifacts", "site-header-theme-desktop.png") });

        await _page.ReloadAsync();
        await _page.Locator(".docs-topbar nt-theme-toggle select").WaitForAsync();
        await Assertions.Expect(selector).ToHaveValueAsync("DARK-DEFAULT");
        await Assertions.Expect(activeTheme).ToHaveAttributeAsync("href", new System.Text.RegularExpressions.Regex("/Themes/dark\\.css$"));
        await _page.SetViewportSizeAsync(390, 844);
        var github = _page.Locator(".docs-github-link");
        await Assertions.Expect(github).ToBeVisibleAsync();
        await Assertions.Expect(selector).ToBeVisibleAsync();
        var githubBounds = (await github.BoundingBoxAsync())!;
        var selectorBounds = (await selector.BoundingBoxAsync())!;
        selectorBounds.X.Should().BeGreaterThanOrEqualTo(githubBounds.X + githubBounds.Width);
        (await _page.EvaluateAsync<int>("Math.max(document.documentElement.scrollWidth, document.body.scrollWidth) - innerWidth")).Should().BeLessThanOrEqualTo(1);
        await _page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(FindRepositoryRoot(), "artifacts", "site-header-theme-mobile.png") });

        await _page.EmulateMediaAsync(new PageEmulateMediaOptions { ColorScheme = ColorScheme.Light });
        await selector.SelectOptionAsync("SYSTEM-DEFAULT");
        await Assertions.Expect(activeTheme).ToHaveAttributeAsync("href", new System.Text.RegularExpressions.Regex("/Themes/light\\.css$"));
        (await _page.EvaluateAsync<string>("localStorage.getItem('NTComponentsStoredThemeKey')")).Should().Be("SYSTEM");
        _browserDiagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task DemoControls_ArePrioritizedGroupedAndHeightContained() {
        ArgumentNullException.ThrowIfNull(_page);

        await OpenComponentDemoAsync("ntbutton");
        var groupNames = await _page.Locator(".docs-control-group").EvaluateAllAsync<string[]>("groups => groups.map(group => group.dataset.docsControlGroup)");
        groupNames.Should().Equal("Content", "Appearance", "Behavior", "Accessibility");
        var buttonControls = await _page.Locator("[data-docs-control-name]").EvaluateAllAsync<string[]>("rows => rows.map(row => row.dataset.docsControlName)");
        buttonControls.Should().Equal("Label", "BadgeContent", "Variant", "LeadingIcon", "Shape", "Elevation", "IsToggleButton", "Selected", "ShowBadge", "BadgeAriaLabel");
        (await _page.Locator("#docs-sandbox-ntbutton-label").InputValueAsync()).Should().Be("Save changes");

        await OpenComponentDemoAsync("ntbuttongroup");
        var scrollState = await _page.Locator(".docs-sandbox-controls").EvaluateAsync<ControlScrollState>(
            "element => ({ overflowY: getComputedStyle(element).overflowY, clientHeight: element.clientHeight, scrollHeight: element.scrollHeight })");
        scrollState.OverflowY.Should().Be("auto");
        scrollState.ScrollHeight.Should().BeGreaterThan(scrollState.ClientHeight);

        await OpenComponentDemoAsync("ntdialog");
        var dialogControls = await _page.Locator("[data-docs-control-name]").EvaluateAllAsync<string[]>("rows => rows.map(row => row.dataset.docsControlName)");
        dialogControls.Should().StartWith("Title", "SupportingText", "ButtonSpacing", "Elevation");
        dialogControls.Should().Contain("ShowCloseButton", "curated compositions should retain their relevant editable settings");
    }

    [Fact]
    public async Task AccordionDemo_LimitToOneExpanded_KeepsSingleItemOpen() {
        ArgumentNullException.ThrowIfNull(_page);

        await OpenComponentDemoAsync("ntaccordion");
        await _page.Locator("#docs-sandbox-ntaccordion-limittooneexpanded").CheckAsync();

        var items = _page.Locator(".docs-generated-example details.nt-accordion-item");
        (await items.EvaluateAllAsync<string?[]>("items => items.map(item => item.getAttribute('name'))")).Should().OnlyContain(name => name == "docs-accordion-example");

        await items.Nth(1).Locator("summary").ClickAsync();
        (await items.EvaluateAllAsync<bool[]>("items => items.map(item => item.open)")).Should().Equal(false, true);
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
        await _page.Locator("#docs-sandbox-ntdialog-title").FillAsync("Schedule review");
        await _page.Locator("#docs-sandbox-ntdialog-title").PressAsync("Tab");
        await _page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Open example dialog", Exact = true }).ClickAsync();
        var dialog = _page.Locator("dialog#docs-example-dialog[open]");
        await dialog.WaitForAsync();
        await dialog.GetByRole(AriaRole.Heading, new LocatorGetByRoleOptions { Name = "Schedule review", Exact = true }).WaitForAsync();
        await dialog.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Cancel", Exact = true }).ClickAsync();
        await _page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Razor", Exact = true }).ClickAsync();
        (await _page.Locator(".docs-code-group pre code").InnerTextAsync()).Should().Contain("Title=\"Schedule review\"");

        await OpenComponentDemoAsync("ntmenu");
        await _page.Locator("#docs-sandbox-ntmenu-appearance").SelectOptionAsync(new SelectOptionValue { Label = "Compact" });
        await _page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Open example menu", Exact = true }).ClickAsync();
        await _page.Locator("#docs-example-menu.nt-menu-compact:popover-open").WaitForAsync();
        await _page.GetByText("Edit", new PageGetByTextOptions { Exact = true }).WaitForAsync();

        await OpenComponentDemoAsync("ntsnackbar");
        await _page.Locator("#docs-sandbox-ntsnackbar-position").SelectOptionAsync(new SelectOptionValue { Label = "TopLeftCorner" });
        await _page.Locator(".nt-snackbar-container.nt-snackbar-top-left-corner").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Attached });
        await _page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Show example snackbar", Exact = true }).ClickAsync();
        await _page.GetByText("Changes saved", new PageGetByTextOptions { Exact = true }).WaitForAsync();

        await OpenComponentDemoAsync("nttoast");
        await _page.Locator("#docs-sandbox-nttoast-position").SelectOptionAsync(new SelectOptionValue { Label = "TopLeftCorner" });
        await _page.Locator(".nt-toast-container.nt-toast-top-left-corner").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Attached });
        await _page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Show example toast", Exact = true }).ClickAsync();
        await _page.GetByText("Your changes were saved.", new PageGetByTextOptions { Exact = true }).WaitForAsync();

        await OpenComponentDemoAsync("nttooltip");
        await _page.Locator("#docs-sandbox-nttooltip-showdelay").FillAsync("0");
        await _page.Locator("#docs-sandbox-nttooltip-showdelay").PressAsync("Tab");
        await _page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Hover or focus for help", Exact = true }).FocusAsync();
        await _page.GetByRole(AriaRole.Tooltip).WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

        await OpenComponentDemoAsync("ntvirtualize");
        await _page.Locator("#docs-sandbox-ntvirtualize-itemsize").FillAsync("40");
        await _page.Locator("#docs-sandbox-ntvirtualize-itemsize").PressAsync("Tab");
        var virtualize = _page.Locator(".docs-curated-demo .virtualize");
        await virtualize.EvaluateAsync("element => element.scrollTop = 0");
        await virtualize.Locator(".virtualize-item").First.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Attached, Timeout = 5_000 });
        var initialItems = await virtualize.Locator(".virtualize-item").AllInnerTextsAsync();
        initialItems.Should().Contain("Item 1", "the virtualized demo should begin at the top; rendered items: {0}", string.Join(", ", initialItems));
        await virtualize.EvaluateAsync("element => element.scrollTop = element.scrollHeight");
        await _page.GetByText("Item 100", new PageGetByTextOptions { Exact = true }).WaitForAsync();

        _browserDiagnostics.Should().BeEmpty("curated interactions should not produce browser errors");
    }

    [Theory]
    [InlineData("ntbutton", "label", "", "Label")]
    [InlineData("ntcarousel", "autoplayinterval", "0", "AutoPlayInterval")]
    [InlineData("ntvirtualize", "itemsize", "0", "ItemSize")]
    [InlineData("ntvirtualize", "overscancount", "1.5", "OverscanCount requires a whole number")]
    [InlineData("nttooltip", "showdelay", "999999999999", "ShowDelay is outside the supported range")]
    public async Task InvalidDemoOptions_ShowSpecificError_AndRecoverAfterCorrection(string slug, string parameter, string invalidValue, string expectedError) {
        ArgumentNullException.ThrowIfNull(_page);
        await OpenComponentDemoAsync(slug);
        var control = _page.Locator($"#docs-sandbox-{slug}-{parameter}");
        var originalValue = await control.InputValueAsync();
        await control.FillAsync(invalidValue);
        await control.PressAsync("Tab");
        var alert = _page.Locator(".docs-generated-example [role='alert']");
        await Assertions.Expect(alert).ToContainTextAsync(expectedError);
        await Assertions.Expect(_page.Locator(".docs-sandbox-controls")).ToBeVisibleAsync();

        await control.FillAsync(originalValue);
        await control.PressAsync("Tab");
        await Assertions.Expect(alert).ToHaveCountAsync(0);
        var failures = new List<string>();
        await ValidatePreviewAsync(failures, _activeRoute, _page.Locator(".docs-sandbox"));
        failures.Should().BeEmpty();
        _browserDiagnostics.Should().BeEmpty("an invalid demo option must be contained in its preview");
    }

    [Fact]
    public async Task IncompatibleDemoOptions_ReportTheContract_AndResetToWorkingDefaults() {
        ArgumentNullException.ThrowIfNull(_page);
        await OpenComponentDemoAsync("ntbutton");
        await _page.Locator("#docs-sandbox-ntbutton-variant").SelectOptionAsync(new SelectOptionValue { Label = "Text" });
        await _page.Locator("#docs-sandbox-ntbutton-istogglebutton").CheckAsync();
        var alert = _page.Locator(".docs-generated-example [role='alert']");
        await Assertions.Expect(alert).ToContainTextAsync("Text buttons do not support toggle behavior");
        await alert.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Reset example", Exact = true }).ClickAsync();
        await Assertions.Expect(alert).ToHaveCountAsync(0);
        await Assertions.Expect(_page.Locator("#docs-sandbox-ntbutton-istogglebutton")).Not.ToBeCheckedAsync();
        await Assertions.Expect(_page.Locator(".docs-generated-example button").First).ToHaveTextAsync("Save changes");

        await OpenComponentDemoAsync("ntdatagrid");
        await _page.Locator("#docs-sandbox-ntdatagrid-virtualize").CheckAsync();
        await _page.Locator("#docs-sandbox-ntdatagrid-showpagination").CheckAsync();
        await Assertions.Expect(alert).ToContainTextAsync("does not support using Virtualize and ShowPagination together");
        await _page.Locator("#docs-sandbox-ntdatagrid-showpagination").UncheckAsync();
        await Assertions.Expect(alert).ToHaveCountAsync(0);
        await Assertions.Expect(_page.Locator(".docs-generated-example")).ToContainTextAsync("Alpha");
        _browserDiagnostics.Should().BeEmpty();
    }

    [Theory]
    [InlineData("ntinputtext", "Grace Hopper")]
    [InlineData("nttextArea", "An edited message")]
    [InlineData("ntinputnumeric", "42")]
    public async Task InputDemo_EditsUpdateTheBoundValue_AndSurviveOptionAndModeChanges(string slug, string value) {
        ArgumentNullException.ThrowIfNull(_page);
        slug = slug.ToLowerInvariant();
        await OpenComponentDemoAsync(slug);
        var input = _page.Locator(".docs-generated-example input:not([type=hidden]), .docs-generated-example textarea").First;
        await input.FillAsync(value);
        await input.PressAsync("Tab");
        var output = _page.Locator("output[aria-label='Current value']");
        await Assertions.Expect(output).ToHaveTextAsync(value);

        var label = _page.Locator($"#docs-sandbox-{slug}-label");
        await label.FillAsync("Edited label");
        await label.PressAsync("Tab");
        await Assertions.Expect(input).ToHaveValueAsync(value);
        await _page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Razor", Exact = true }).ClickAsync();
        await _page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Preview", Exact = true }).ClickAsync();
        await Assertions.Expect(input).ToHaveValueAsync(value);
        await Assertions.Expect(output).ToHaveTextAsync(value);
        _browserDiagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task SelectionAndSliderDemos_UpdateTheirBoundOutput() {
        ArgumentNullException.ThrowIfNull(_page);
        foreach (var slug in new[] { "ntinputcheckbox", "ntinputswitch" }) {
            await OpenComponentDemoAsync(slug);
            await _page.Locator(".docs-generated-example input[type=checkbox]").UncheckAsync();
            await Assertions.Expect(_page.Locator("output[aria-label='Current value']")).ToHaveTextAsync("False");
        }

        await OpenComponentDemoAsync("ntinputradiogroup");
        await _page.Locator(".docs-generated-example input[type=radio]").Nth(1).CheckAsync();
        await Assertions.Expect(_page.Locator("output[aria-label='Current value']")).ToHaveTextAsync("two");

        await OpenComponentDemoAsync("ntselect");
        await _page.Locator(".docs-generated-example select").SelectOptionAsync("two");
        await Assertions.Expect(_page.Locator("output[aria-label='Current value']")).ToHaveTextAsync("two");

        await OpenComponentDemoAsync("ntcombobox");
        await _page.Locator(".docs-generated-example input[role=combobox]").ClickAsync();
        await _page.Locator(".docs-generated-example [role=option]").Filter(new LocatorFilterOptions { HasText = "Two" }).ClickAsync();
        await Assertions.Expect(_page.Locator("output[aria-label='Current value']")).ToHaveTextAsync("one, two");

        await OpenComponentDemoAsync("ntinputslider");
        var slider = _page.Locator(".docs-generated-example input[type=range]");
        await slider.PressAsync("Home");
        await slider.PressAsync("ArrowRight");
        await Assertions.Expect(_page.Locator("output[aria-label='Current value']")).ToHaveTextAsync("1");

        await OpenComponentDemoAsync("ntinputcolor");
        await _page.Locator(".docs-generated-example input[type=color]").EvaluateAsync("input => { input.value = '#123456'; input.dispatchEvent(new Event('change', { bubbles: true })); }");
        await Assertions.Expect(_page.Locator("output[aria-label='Current value']")).ToHaveTextAsync("#123456");
        _browserDiagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task TabWizardAndButtonGroupDemos_NavigateToTheSelectedContent() {
        ArgumentNullException.ThrowIfNull(_page);
        await OpenComponentDemoAsync("nttabview");
        var demo = _page.Locator(".docs-generated-example");
        await demo.GetByRole(AriaRole.Tab, new LocatorGetByRoleOptions { Name = "Details", Exact = true }).ClickAsync();
        await Assertions.Expect(demo.GetByRole(AriaRole.Tabpanel)).ToHaveTextAsync("Details content");

        await OpenComponentDemoAsync("ntwizard");
        await demo.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Next Step", Exact = true }).ClickAsync();
        await Assertions.Expect(demo.GetByRole(AriaRole.Tabpanel)).ToContainTextAsync("Second step content");
        await demo.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Previous Step", Exact = true }).ClickAsync();
        await Assertions.Expect(demo.GetByRole(AriaRole.Tabpanel)).ToContainTextAsync("First step content");

        await OpenComponentDemoAsync("ntbuttongroup");
        var two = demo.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Two", Exact = true });
        await two.ClickAsync();
        await Assertions.Expect(two).ToHaveAttributeAsync("aria-pressed", "true");
        await Assertions.Expect(_page.Locator("#docs-sandbox-ntbuttongroup-selectedkey")).ToHaveValueAsync("two");
        _browserDiagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task FormDemo_ValidatesAndSubmitsEditedValues_AndAppliesFormOptions() {
        ArgumentNullException.ThrowIfNull(_page);
        await OpenComponentDemoAsync("ntform");
        var demo = _page.Locator(".docs-curated-demo");
        var name = demo.GetByRole(AriaRole.Textbox, new LocatorGetByRoleOptions { NameRegex = new System.Text.RegularExpressions.Regex("^Name\\b") });
        var email = demo.GetByRole(AriaRole.Textbox, new LocatorGetByRoleOptions { NameRegex = new System.Text.RegularExpressions.Regex("^Email\\b") });
        await name.FillAsync("");
        await demo.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Submit example form", Exact = true }).ClickAsync();
        await Assertions.Expect(demo).ToContainTextAsync("The Name field is required.");
        await Assertions.Expect(demo.Locator("output")).ToBeEmptyAsync();

        await name.FillAsync("Grace Hopper");
        await email.FillAsync("grace@example.com");
        await demo.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Submit example form", Exact = true }).ClickAsync();
        await Assertions.Expect(demo.Locator("output")).ToHaveTextAsync("Submitted Grace Hopper (grace@example.com).");
        await _page.Locator("#docs-sandbox-ntform-disabled").CheckAsync();
        await Assertions.Expect(name).ToBeDisabledAsync();
        await _page.Locator("#docs-sandbox-ntform-disabled").UncheckAsync();
        await Assertions.Expect(name).ToBeEnabledAsync();
        await Assertions.Expect(name).ToHaveValueAsync("Grace Hopper");
        _browserDiagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task FileUploadDemo_ReadsTheSelectedBytes_AndRejectsOversizedFiles() {
        ArgumentNullException.ThrowIfNull(_page);
        await OpenComponentDemoAsync("ntfileupload");
        var demo = _page.Locator(".docs-curated-demo");
        var fileInput = demo.Locator("input[type=file]");
        await fileInput.SetInputFilesAsync(new FilePayload { Name = "hello.txt", MimeType = "text/plain", Buffer = Encoding.UTF8.GetBytes("hello") });
        await demo.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Upload", Exact = true }).ClickAsync();
        await Assertions.Expect(demo.Locator("output")).ToHaveTextAsync("Read hello.txt: 5 bytes.");
        await Assertions.Expect(demo).ToContainTextAsync("Complete");

        await _page.Locator("#docs-sandbox-ntfileupload-maximumfilesize").FillAsync("4");
        await _page.Locator("#docs-sandbox-ntfileupload-maximumfilesize").PressAsync("Tab");
        await fileInput.SetInputFilesAsync(new FilePayload { Name = "oversized.txt", MimeType = "text/plain", Buffer = Encoding.UTF8.GetBytes("hello") });
        await Assertions.Expect(demo).ToContainTextAsync("Too large");
        await Assertions.Expect(demo.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Upload", Exact = true })).ToBeDisabledAsync();
        _browserDiagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task ContextMenuDemo_InvokesTheAction_AndHonorsDisabled() {
        ArgumentNullException.ThrowIfNull(_page);
        await OpenComponentDemoAsync("ntcontextmenu");
        var demo = _page.Locator(".docs-curated-demo");
        var trigger = demo.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Right-click or long-press for actions", Exact = true });
        await trigger.ClickAsync(new LocatorClickOptions { Button = MouseButton.Right });
        await demo.GetByRole(AriaRole.Menuitem, new LocatorGetByRoleOptions { Name = "Archive example", Exact = true }).ClickAsync();
        await Assertions.Expect(demo.Locator("output")).ToHaveTextAsync("Example archived");
        await _page.Locator("#docs-sandbox-ntcontextmenu-disabled").CheckAsync();
        await trigger.ClickAsync(new LocatorClickOptions { Button = MouseButton.Right });
        await Assertions.Expect(demo.Locator(":popover-open")).ToHaveCountAsync(0);
        _browserDiagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task WindowHostDemo_OpensClosesAndReopensManagedContent() {
        ArgumentNullException.ThrowIfNull(_page);
        await OpenComponentDemoAsync("ntwindowhost");
        var demo = _page.Locator(".docs-curated-demo");
        var open = demo.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Open managed window", Exact = true });
        await open.ClickAsync();
        var window = demo.Locator(".nt-window");
        await Assertions.Expect(window).ToContainTextAsync("This window is rendered by NTWindowHost.");
        await window.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Close Managed project notes", Exact = true }).ClickAsync();
        await Assertions.Expect(window).ToHaveCountAsync(0);
        await open.ClickAsync();
        await Assertions.Expect(window).ToHaveCountAsync(1);
        await _page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Razor", Exact = true }).ClickAsync();
        await _page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Preview", Exact = true }).ClickAsync();
        await Assertions.Expect(window).ToHaveCountAsync(0, new LocatorAssertionsToHaveCountOptions { Timeout = 5_000 });
        _browserDiagnostics.Should().BeEmpty("preview disposal must release its managed windows");
    }

    [Fact]
    public async Task LayoutDemos_RenderRealPanesCardsFieldsAndDocumentNavigation() {
        ArgumentNullException.ThrowIfNull(_page);
        await OpenComponentDemoAsync("ntmultipaneview");
        await Assertions.Expect(_page.Locator(".docs-generated-example .nt-multi-pane-view > .nt-card")).ToHaveCountAsync(3);
        await _page.Locator("#docs-sandbox-ntmultipaneview-panecount").FillAsync("3");
        await _page.Locator("#docs-sandbox-ntmultipaneview-panecount").PressAsync("Tab");
        await Assertions.Expect(_page.Locator(".docs-generated-example .nt-multi-pane-view")).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("nt-multi-pane-view-3-panes"));

        await OpenComponentDemoAsync("ntfeedview");
        await Assertions.Expect(_page.Locator(".docs-generated-example .nt-feed-view > .nt-card")).ToHaveCountAsync(6);

        foreach (var slug in new[] { "ntformfieldgridview", "ntformsectionview", "ntformfieldlayoutspan" }) {
            await OpenComponentDemoAsync(slug);
            var fields = _page.Locator(".docs-generated-example input:not([type=hidden])");
            await Assertions.Expect(fields).ToHaveCountAsync(2);
            await fields.First.FillAsync("Edited field");
            await fields.First.PressAsync("Tab");
            await Assertions.Expect(fields.First).ToHaveValueAsync("Edited field");
        }

        await OpenComponentDemoAsync("ntcontainerview");
        var view = _page.Locator(".docs-generated-example .nt-container-view");
        await Assertions.Expect(view.Locator(".nt-container-view-content h2")).ToHaveCountAsync(3);
        await _page.Locator("#docs-sandbox-ntcontainerview-enableonthispagenavigation").CheckAsync();
        await Assertions.Expect(view.Locator(".nt-container-view-quick-nav-list a")).ToHaveCountAsync(3);
        _browserDiagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task ClearingOptionalDemoValues_RestoresAutomaticSpan_AndRemovesTheIcon() {
        ArgumentNullException.ThrowIfNull(_page);
        await OpenComponentDemoAsync("ntformfieldlayoutspan");
        var columns = _page.Locator("#docs-sandbox-ntformfieldlayoutspan-smallcolumns");
        await Assertions.Expect(columns).ToHaveValueAsync("");
        await columns.FillAsync("4");
        await columns.PressAsync("Tab");
        var span = _page.Locator(".docs-generated-example .nt-form-field-layout-span");
        await _page.WaitForFunctionAsync("getComputedStyle(document.querySelector('.docs-generated-example .nt-form-field-layout-span')).getPropertyValue('--nt-form-field-layout-span-small').trim() === '4'");
        await columns.FillAsync("");
        await columns.PressAsync("Tab");
        (await span.EvaluateAsync<string>("element => element.style.getPropertyValue('--nt-form-field-layout-span-small')")).Should().BeEmpty();
        await _page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Razor", Exact = true }).ClickAsync();
        await Assertions.Expect(_page.Locator(".docs-code-group pre code")).ToContainTextAsync("SmallColumns=\"@null\"");

        await OpenComponentDemoAsync("ntbutton");
        var icon = _page.Locator("#docs-sandbox-ntbutton-leadingicon");
        await icon.SelectOptionAsync(new SelectOptionValue { Label = "MaterialIcon.Add" });
        await Assertions.Expect(_page.Locator(".docs-generated-example .nt-button-icon")).ToHaveCountAsync(1);
        await icon.SelectOptionAsync(new SelectOptionValue { Label = "None" });
        await Assertions.Expect(_page.Locator(".docs-generated-example .nt-button-icon")).ToHaveCountAsync(0);
        _browserDiagnostics.Should().BeEmpty();
    }

    private async Task OpenComponentDemoAsync(string slug) {
        ArgumentNullException.ThrowIfNull(_page);
        _activeRoute = $"/components/{slug}";
        await _page.GotoAsync($"{_baseUrl}{_activeRoute}", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await _page.Locator(".docs-sandbox-preview").WaitForAsync();
        await Assertions.Expect(_page.Locator(".docs-generated-example [role='alert']")).ToHaveCountAsync(0);
    }

    private static async Task ValidateControlLayoutAsync(ICollection<string> failures, string route, ILocator sandbox) {
        var groupNames = await sandbox.Locator(".docs-control-group").EvaluateAllAsync<string[]>("groups => groups.map(group => group.dataset.docsControlGroup)");
        var expectedGroupOrder = new[] { "Content", "Appearance", "Behavior", "Accessibility", "Advanced" };
        var sortedGroupNames = groupNames.OrderBy(name => Array.IndexOf(expectedGroupOrder, name), Comparer<int>.Default).ToArray();
        if (!groupNames.SequenceEqual(sortedGroupNames, StringComparer.Ordinal)) {
            failures.Add($"{route}: control groups were not ordered from common content settings through advanced settings: {string.Join(", ", groupNames)}.");
        }

        if (groupNames.Distinct(StringComparer.Ordinal).Count() != groupNames.Length) {
            failures.Add($"{route}: rendered duplicate control groups: {string.Join(", ", groupNames)}.");
        }

        var ungroupedControlCount = await sandbox.Locator(".docs-sandbox-controls > .docs-control-row").CountAsync();
        if (ungroupedControlCount > 0) {
            failures.Add($"{route}: rendered {ungroupedControlCount} controls outside a semantic group.");
        }
    }

    private static async Task ValidatePreviewAsync(ICollection<string> failures, string route, ILocator sandbox) {
        var errors = await sandbox.Locator(".docs-generated-example [role='alert']").AllInnerTextsAsync();
        if (errors.Count > 0) {
            failures.Add($"{route}: default preview failed: {string.Join(" | ", errors)}");
            return;
        }
        var generatedExample = sandbox.Locator(".docs-generated-example");
        var expectedSelector = route switch {
            "/components/ntheaddependencies" => "[data-docs-demo-component='NTPageScript']",
            "/components/ntbrowsertimezone" => "input[data-nt-browser-time-zone]",
            "/components/ntform" => "form",
            "/components/ntinputcheckbox" or "/components/ntinputswitch" => "input[type=checkbox]",
            "/components/ntinputradiogroup" => "input[type=radio]",
            "/components/ntinputslider" or "/components/ntinputrangeslider" => "input[type=range]",
            "/components/ntinputcolor" => "input[type=color]",
            "/components/ntinputnumeric" => "input[type=number]",
            "/components/ntinputcurrency" or "/components/ntinputtext" or "/components/ntinputdatetime" => ".nt-input-control",
            "/components/ntselect" => "select",
            "/components/nttextarea" => "textarea",
            "/components/ntautocomplete" or "/components/ntcombobox" or "/components/nttypeahead" => "input[role=combobox]",
            "/components/ntfileupload" => "input[type=file]",
            "/components/ntrichtexteditor" => "[contenteditable=true]",
            "/components/ntsnackbar" => ".nt-snackbar-container",
            "/components/nttoast" => ".nt-toast-container",
            "/components/ntthemetoggle" => "nt-theme-toggle",
            "/components/ntvirtualize" => ".virtualize-item",
            _ => string.Empty
        };
        // Most roots use a kebab-case component class. Resolve these explicitly so a marker alone cannot prove rendering.
        if (expectedSelector.Length == 0) {
            var name = (await sandbox.Locator("[data-docs-demo-component]").First.GetAttributeAsync("data-docs-demo-component"))![2..];
            expectedSelector = ".nt-" + System.Text.RegularExpressions.Regex.Replace(name, "(?<=[a-z0-9])(?=[A-Z])", "-").ToLowerInvariant();
        }
        if (!await TryWaitForAttachedAsync(generatedExample.Locator(expectedSelector).First)) {
            failures.Add($"{route}: expected component output '{expectedSelector}' was missing.");
        }
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
            var isHiddenResponsivePreview = route == "/components/ntresponsive" && await generatedExample.EvaluateAsync<bool>(
                """
                element => {
                    const responsive = element.querySelector('.nt-responsive');
                    return responsive?.textContent?.trim().length > 0
                        && responsive.hasAttribute('data-nt-breakpoint')
                        && responsive.hasAttribute('data-nt-breakpoint-direction')
                        && responsive.hasAttribute('data-nt-breakpoint-visibility')
                        && getComputedStyle(responsive).display === 'none';
                }
                """);
            if (!isHiddenResponsivePreview) {
                failures.Add($"{route}: Preview was blank and did not expose data-docs-preview-verified='true' for a successful nonvisual demo.");
            }
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

    private static string CreateThemeSource(string selector) => $$"""
        {{selector}} {
          --md-sys-color-primary: rgb(1 2 3);
          --md-sys-color-on-primary: rgb(255 255 255);
          --md-extended-color-success-on-color-container: rgb(4 5 6);
        }
        """;

    private async Task<string> RootColorAsync(string propertyName) {
        ArgumentNullException.ThrowIfNull(_page);
        await _page.WaitForFunctionAsync("name => getComputedStyle(document.documentElement).getPropertyValue(name).trim().length > 0", propertyName);
        return await _page.EvaluateAsync<string>(
            """
            name => {
                const probe = document.createElement('span');
                probe.style.color = `var(${name})`;
                document.body.append(probe);
                const color = getComputedStyle(probe).color;
                probe.remove();
                return color;
            }
            """,
            propertyName);
    }

    private async Task WaitForPalettePickerToMatchPreviewAsync(string paletteId, string token) {
        ArgumentNullException.ThrowIfNull(_page);
        await _page.WaitForFunctionAsync(
            """
            args => {
                const input = document.querySelector(`#theme-palette-${args.paletteId}`);
                const rgb = document.querySelector('[data-testid=theme-preview]').style.getPropertyValue(args.token).match(/\d+/g);
                return input?.value === `#${rgb?.map(channel => Number(channel).toString(16).padStart(2, '0')).join('')}`;
            }
            """,
            new { paletteId, token });
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

    private static async Task<bool> TryWaitForAttachedAsync(ILocator locator) {
        try {
            await locator.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Attached, Timeout = 5_000 });
            return true;
        }
        catch (TimeoutException) {
            return false;
        }
    }

    private static Task WaitForRenderAsync(IPage page) => page.EvaluateAsync(
        "() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))");

    private sealed record BrowserDiagnostic(string Route, string Kind, string Message);

    private sealed class ControlScrollState {
        public string OverflowY { get; set; } = string.Empty;

        public int ClientHeight { get; set; }

        public int ScrollHeight { get; set; }
    }

    private sealed record ControlChange(string PropertyName, string Value, bool RequirePropertyName);

    private sealed class SelectOption {
        public string Label { get; set; } = string.Empty;

        public bool Selected { get; set; }

        public string Value { get; set; } = string.Empty;
    }
}
#endif
