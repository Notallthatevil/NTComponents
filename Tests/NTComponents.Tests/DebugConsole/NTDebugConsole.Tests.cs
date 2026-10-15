using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NTComponents.DebugConsole;

namespace NTComponents.Tests.DebugConsole;

public class NTDebugConsole_Tests : BunitContext {
    private const string JsModulePath = "./_content/NTComponents/DebugConsole/NTDebugConsole.razor.js";
    private readonly BunitJSModuleInterop _module;

    public NTDebugConsole_Tests() {
        JSInterop.Mode = JSRuntimeMode.Loose;
        _module = JSInterop.SetupModule(JsModulePath);
        _module.Mode = JSRuntimeMode.Loose;
        _module.Setup<bool>("copyText", _ => true).SetResult(true);
    }

    private INTDebugConsole Console => Services.GetRequiredService<INTDebugConsole>();

    [Fact]
    public void WithoutAuthorize_RendersNothing() {
        // Arrange
        AddConsole(authorize: false);

        // Act
        var cut = Render<NTDebugConsole>();

        // Assert
        cut.Markup.Trim().Should().BeEmpty();
        _module.Invocations.Should().BeEmpty("a disabled console does not load its browser module");
    }

    [Fact]
    public void Collapsed_ShowsTheBarWithCountsByLevel() {
        // Arrange
        AddConsole();
        Console.Write(LogLevel.Error, "Payment failed");
        Console.Write(LogLevel.Critical, "Database unavailable");
        Console.Write(LogLevel.Warning, "Slow response");
        Console.Write(LogLevel.Information, "Opened cart");

        // Act
        var cut = Render<NTDebugConsole>();

        // Assert
        cut.Find(".nt-debug-console-title").TextContent.Should().Be("Debug console");
        cut.Find(".nt-debug-console-count-error").TextContent.Should().Contain("2").And.Contain("errors");
        cut.Find(".nt-debug-console-count-warning").TextContent.Should().Contain("1").And.Contain("warning");
        cut.FindAll(".nt-debug-console-count")[2].TextContent.Should().Contain("4").And.Contain("entries");
        cut.FindAll(".nt-debug-console-panel").Should().BeEmpty();
        cut.Find("button[aria-label='Expand debug console']").GetAttribute("aria-expanded").Should().Be("false");
    }

    [Fact]
    public void ExpandButton_ExpandsAndCollapsesTheSheet() {
        // Arrange
        AddConsole();
        Console.Write(LogLevel.Information, "Opened cart");
        var expanded = new List<bool>();
        var cut = Render<NTDebugConsole>(parameters => parameters.Add(p => p.ExpandedChanged, value => expanded.Add(value)));

        // Act
        cut.Find("button[aria-label='Expand debug console']").Click();
        var panelId = cut.Find(".nt-debug-console-panel").Id;
        var collapseButton = cut.Find("button[aria-label='Collapse debug console']");
        var expandedState = collapseButton.GetAttribute("aria-expanded");
        var controls = collapseButton.GetAttribute("aria-controls");
        collapseButton.Click();

        // Assert
        expandedState.Should().Be("true");
        controls.Should().Be(panelId);
        cut.FindAll(".nt-debug-console-panel").Should().BeEmpty();
        expanded.Should().Equal(true, false);
    }

    [Fact]
    public void ResizeHandle_RendersOnlyWhileExpanded() {
        // Arrange
        AddConsole();
        var cut = Render<NTDebugConsole>();
        var collapsedHandles = cut.FindAll("[data-nt-debug-console-resize]").Count;

        // Act
        cut.Find("button[aria-label='Expand debug console']").Click();
        var handle = cut.Find("[data-nt-debug-console-resize]");

        // Assert
        collapsedHandles.Should().Be(0);
        handle.GetAttribute("role").Should().Be("separator");
        handle.GetAttribute("aria-orientation").Should().Be("horizontal");
        handle.GetAttribute("aria-label").Should().Be("Resize debug console");
        handle.GetAttribute("aria-controls").Should().Be(cut.Find(".nt-debug-console-panel").Id);
        handle.GetAttribute("tabindex").Should().Be("0");
    }

    [Fact]
    public void Expanded_ShowsEachEntryWithTimeLevelCategoryAndMessage() {
        AddConsole();
        Console.Write(LogLevel.Warning, "Card expires soon", "Shop.Billing.CardService");

        var cut = RenderExpanded();

        var entry = cut.Find(".nt-debug-console-entry");
        entry.ClassList.Should().Contain("nt-debug-console-level-warning");
        entry.QuerySelector(".nt-debug-console-time")!.TextContent.Should().MatchRegex(@"^\d{2}:\d{2}:\d{2}\.\d{3}$");
        entry.QuerySelector(".nt-debug-console-level")!.TextContent.Should().Be("WARN");
        entry.QuerySelector(".nt-debug-console-category")!.TextContent.Should().Be("CardService");
        entry.QuerySelector(".nt-debug-console-category")!.GetAttribute("title").Should().Be("Shop.Billing.CardService");
        entry.QuerySelector(".nt-debug-console-source")!.TextContent.Should().Be("App");
        entry.QuerySelector(".nt-debug-console-message")!.TextContent.Should().Be("Card expires soon");
    }

    [Fact]
    public void Expanded_TheListIsAPoliteLog() {
        AddConsole();

        var list = RenderExpanded().Find(".nt-debug-console-list");

        list.GetAttribute("role").Should().Be("log");
        list.GetAttribute("aria-live").Should().Be("polite");
    }

    [Fact]
    public void AnEntryWithAnException_ExpandsToShowIt() {
        // Arrange
        AddConsole();
        var session = Services.GetRequiredService<NTDebugConsoleService>();
        session.Capture(new(DateTimeOffset.Now, LogLevel.Error, "Shop.Checkout", "Checkout failed", "System.InvalidOperationException: Card declined", default, NTDebugConsoleEntrySource.Log));

        // Act
        var cut = RenderExpanded();

        // Assert
        var exception = cut.Find(".nt-debug-console-exception");
        exception.QuerySelector("summary")!.TextContent.Should().Be("Exception");
        exception.QuerySelector("pre")!.TextContent.Should().Be("System.InvalidOperationException: Card declined");
        cut.FindAll(".nt-debug-console-source").Should().BeEmpty("log entries need no source label");
    }

    [Fact]
    public void LevelFilter_HidesTheLevelsThatAreTurnedOff() {
        // Arrange
        AddConsole();
        Console.Write(LogLevel.Error, "Payment failed");
        Console.Write(LogLevel.Information, "Opened cart");
        var cut = RenderExpanded();

        // Act
        cut.FindAll(".nt-debug-console-filters input[type=checkbox]")[0].Change(false);

        // Assert
        cut.FindAll(".nt-debug-console-message").Select(message => message.TextContent).Should().Equal("Opened cart");
    }

    [Fact]
    public void Search_ShowsOnlyMatchingEntries() {
        AddConsole();
        Console.Write(LogLevel.Information, "Opened cart");
        Console.Write(LogLevel.Information, "Applied coupon SPRING");
        var cut = RenderExpanded();

        cut.Find(".nt-debug-console-search input").Input("coupon");

        cut.FindAll(".nt-debug-console-message").Select(message => message.TextContent).Should().Equal("Applied coupon SPRING");
    }

    [Fact]
    public void FiltersThatMatchNothing_SayNoEntriesMatch() {
        AddConsole();
        Console.Write(LogLevel.Information, "Opened cart");
        var cut = RenderExpanded();

        cut.Find(".nt-debug-console-search input").Input("refund");

        cut.Find(".nt-debug-console-empty").TextContent.Should().Be("No entries match the filters.");
    }

    [Fact]
    public void ClearButton_RemovesTheEntries() {
        AddConsole();
        Console.Write(LogLevel.Information, "Opened cart");
        var cut = RenderExpanded();

        cut.Find("button[aria-label='Clear entries']").Click();

        Console.Entries.Should().BeEmpty();
        cut.FindAll(".nt-debug-console-entry").Should().BeEmpty();
        cut.Find(".nt-debug-console-empty").TextContent.Should().Be("No entries yet.");
    }

    [Fact]
    public void NewEntries_RenderWithoutAParentRender() {
        AddConsole();
        var cut = RenderExpanded();

        Console.Write(LogLevel.Information, "Opened cart");
        Console.Write(LogLevel.Information, "Applied coupon");

        cut.WaitForAssertion(() => cut.FindAll(".nt-debug-console-entry").Should().HaveCount(2), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void ManyEntries_RenderOnlyTheNewestAndSayHowManyMatch() {
        AddConsole();
        for (var i = 1; i <= NTDebugConsole.MaxRenderedEntries + 20; i++) {
            Console.Write(LogLevel.Information, $"Step {i}");
        }

        var cut = RenderExpanded();

        var messages = cut.FindAll(".nt-debug-console-message");
        messages.Should().HaveCount(NTDebugConsole.MaxRenderedEntries);
        messages[^1].TextContent.Should().Be($"Step {NTDebugConsole.MaxRenderedEntries + 20}");
        cut.Find(".nt-debug-console-cap").TextContent.Should().Contain($"{NTDebugConsole.MaxRenderedEntries + 20} matching entries");
    }

    [Fact]
    public void RowKeys_FollowCaptureOrder() {
        AddConsole();
        Console.Write(LogLevel.Error, "First");
        Console.Write(LogLevel.Information, "Second");
        var cut = RenderExpanded();
        cut.FindAll(".nt-debug-console-filters input[type=checkbox]")[0].Change(false);

        cut.FindAll(".nt-debug-console-filters input[type=checkbox]")[0].Change(true);

        cut.FindAll(".nt-debug-console-entry").Select(row => long.Parse(row.GetAttribute("data-seq")!)).Should().BeInAscendingOrder().And.OnlyHaveUniqueItems();
    }

    [Fact]
    public void CopyButton_CopiesTheVisibleEntriesAsText() {
        // Arrange
        AddConsole();
        Console.Write(LogLevel.Error, "Payment failed");
        Console.Write(LogLevel.Information, "Opened cart");
        var cut = RenderExpanded();
        cut.FindAll(".nt-debug-console-filters input[type=checkbox]")[2].Change(false);

        // Act
        cut.Find("button[aria-label='Copy visible entries']").Click();

        // Assert
        var text = _module.Invocations["copyText"].Should().ContainSingle().Which.Arguments[0].Should().BeOfType<string>().Which;
        text.Should().Contain("ERROR [App] (App) Payment failed").And.NotContain("Opened cart");
        cut.Find(".nt-debug-console-status").TextContent.Should().Be("Copied 1 entry");
    }

    [Fact]
    public void Bar_HasOnlyTheExpandControl() {
        // The console is hidden with the shortcut or the Visible parameter, not a close button.
        AddConsole();

        var cut = Render<NTDebugConsole>();

        cut.FindAll(".nt-debug-console-bar-actions button").Select(button => button.GetAttribute("aria-label")).Should().Equal("Expand debug console");
    }

    [Fact]
    public async Task TheShortcut_ShowsAHiddenConsole() {
        AddConsole();
        var cut = Render<NTDebugConsole>(parameters => parameters.Add(p => p.Visible, false));
        var hiddenMarkup = cut.Markup.Trim();

        await cut.Instance.ToggleVisibleAsync();

        hiddenMarkup.Should().BeEmpty();
        cut.FindAll(".nt-debug-console-bar").Should().ContainSingle();
    }

    [Fact]
    public async Task RestoredState_IsApplied() {
        AddConsole();
        var cut = Render<NTDebugConsole>();

        await cut.Instance.RestoreStateAsync(expanded: true);

        cut.FindAll(".nt-debug-console-panel").Should().ContainSingle();
    }

    [Fact]
    public void FirstRender_LoadsTheModuleWithTheShortcut() {
        AddConsole();

        var cut = Render<NTDebugConsole>(parameters => parameters.Add(p => p.Shortcut, "Ctrl+Shift+L"));

        cut.WaitForAssertion(() => _module.Invocations["onLoad"].Should().ContainSingle().Which.Arguments[3].Should().Be("Ctrl+Shift+L"));
    }

    [Fact]
    public void ChangingTheState_TellsTheModuleSoItIsRemembered() {
        AddConsole();
        var cut = Render<NTDebugConsole>();
        cut.WaitForAssertion(() => _module.Invocations["onLoad"].Should().ContainSingle());

        cut.Find("button[aria-label='Expand debug console']").Click();

        cut.WaitForAssertion(() => _module.Invocations["onUpdate"].Should().ContainSingle().Which.Arguments.Skip(3).Should().Equal(true));
    }

    [Fact]
    public void NewEntries_DoNotCallTheModule() {
        AddConsole();
        var cut = RenderExpanded();
        cut.WaitForAssertion(() => _module.Invocations["onLoad"].Should().ContainSingle());
        var calls = _module.Invocations.Count;

        Console.Write(LogLevel.Information, "Opened cart");
        cut.WaitForAssertion(() => cut.FindAll(".nt-debug-console-entry").Should().ContainSingle(), TimeSpan.FromSeconds(5));

        _module.Invocations.Should().HaveCount(calls, "the browser module follows new rows by itself");
    }

    [Theory]
    [InlineData("Shop.Checkout", "Checkout")]
    [InlineData("Checkout", "Checkout")]
    [InlineData("Shop.Repository<Shop.Models.Order>", "Repository<Shop.Models.Order>")]
    public void ShortCategory_KeepsTheLastSegment(string category, string expected) => NTDebugConsole.ShortCategory(category).Should().Be(expected);

    [Fact]
    public void ToJson_WritesEveryField() {
        var entry = new NTDebugConsoleEntry(new DateTimeOffset(2026, 10, 14, 9, 30, 15, 250, TimeSpan.Zero), LogLevel.Error, "Shop.Checkout", "Checkout failed", "boom", new EventId(7, "CheckoutFailed"), NTDebugConsoleEntrySource.Log);

        var json = NTDebugConsole.ToJson([entry]);

        using var document = System.Text.Json.JsonDocument.Parse(json);
        var item = document.RootElement.EnumerateArray().Should().ContainSingle().Subject;
        item.GetProperty("timestamp").GetDateTimeOffset().Should().Be(entry.Timestamp);
        item.GetProperty("level").GetString().Should().Be("Error");
        item.GetProperty("category").GetString().Should().Be("Shop.Checkout");
        item.GetProperty("message").GetString().Should().Be("Checkout failed");
        item.GetProperty("exception").GetString().Should().Be("boom");
        item.GetProperty("eventId").GetInt32().Should().Be(7);
        item.GetProperty("eventName").GetString().Should().Be("CheckoutFailed");
        item.GetProperty("source").GetString().Should().Be("Log");
    }

    private void AddConsole(bool authorize = true) {
        Services.AddLogging();
        Services.AddNTServices();
        AddAuthorization();
        // A single-session host authorizes the console itself, as WebAssembly does.
        Services.AddSingleton(new NTDebugConsoleSessionAccessor(singleSession: true));
        Services.AddNTDebugConsole(options => {
            if (authorize) {
                options.AllowAll();
            }
        });
        SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
    }

    private IRenderedComponent<NTDebugConsole> RenderExpanded() => Render<NTDebugConsole>(parameters => parameters.Add(p => p.Expanded, true));
}
