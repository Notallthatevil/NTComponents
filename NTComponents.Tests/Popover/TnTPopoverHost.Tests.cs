using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using NTComponents.Popover;

namespace NTComponents.Tests.Popover;

/// <summary>
///     bUnit tests for <see cref="TnTPopoverHost" />.
/// </summary>
public class TnTPopoverHost_Tests : BunitContext {
    private const string JsModulePath = "./_content/NTComponents/Popover/TnTPopoverWindow.razor.js";

    public TnTPopoverHost_Tests() {
        Services.AddSingleton<ITnTPopoverService, TnTPopoverService>();
        SetRendererInfo(new RendererInfo("WebAssembly", true));

        var module = JSInterop.SetupModule(JsModulePath);
        module.SetupVoid("initializePopoverWindow", _ => true).SetVoidResult();
        module.SetupVoid("updatePopoverWindow", _ => true).SetVoidResult();
        module.SetupVoid("animatePopoverFromLauncher", _ => true).SetVoidResult();
        module.SetupVoid("animatePopoverToLauncher", _ => true).SetVoidResult();
        module.SetupVoid("disposePopoverWindow", _ => true).SetVoidResult();
    }

    [Fact]
    public void EmptyHost_RendersNothing() {
        // Act
        var cut = Render<TnTPopoverHost>();

        // Assert
        cut.Markup.Trim().Should().BeEmpty();
    }

    [Fact]
    public async Task EscapeKey_ClosesVisiblePopover() {
        // Arrange
        var service = Services.GetRequiredService<ITnTPopoverService>();
        var cut = Render<TnTPopoverHost>();
        await service.OpenAsync(CreateContent("Notes"), new TnTPopoverOptions { Title = "Notes" });

        // Act
        await cut.Find(".tnt-popover").TriggerEventAsync("onkeydown", new KeyboardEventArgs { Key = "Escape" });
        await cut.Find(".tnt-popover").TriggerEventAsync("onanimationend", EventArgs.Empty);

        // Assert
        cut.WaitForAssertion(() => cut.FindAll(".tnt-popover").Should().BeEmpty());
    }

    [Fact]
    public async Task HideButton_MovesPopoverToLauncherStrip() {
        // Arrange
        var service = Services.GetRequiredService<ITnTPopoverService>();
        var cut = Render<TnTPopoverHost>();
        await service.OpenAsync(CreateContent("Notes"), new TnTPopoverOptions { Title = "Notes" });

        // Act
        await cut.Find("button[aria-label='Hide Notes']").ClickAsync(new MouseEventArgs());

        // Assert
        cut.WaitForAssertion(() => {
            cut.FindAll(".tnt-popover").Should().BeEmpty();
            cut.Find(".tnt-popover-host__launcher").TextContent.Should().Contain("Notes");
        });
    }

    [Fact]
    public async Task HiddenPopover_CanBeRestoredFromLauncherStrip() {
        // Arrange
        var service = Services.GetRequiredService<ITnTPopoverService>();
        var cut = Render<TnTPopoverHost>();
        await service.OpenAsync(CreateContent("Inspector"), new TnTPopoverOptions { Title = "Inspector" });
        await cut.Find("button[aria-label='Hide Inspector']").ClickAsync(new MouseEventArgs());

        // Act
        await cut.Find("button[aria-label='Restore Inspector']").ClickAsync(new MouseEventArgs());

        // Assert
        cut.WaitForAssertion(() => {
            cut.Find(".tnt-popover__title").TextContent.Should().Be("Inspector");
            cut.FindAll(".tnt-popover-host__launcher").Should().BeEmpty();
        });
    }

    [Fact]
    public async Task VisiblePopover_RendersAccessibleDialogMarkup() {
        // Arrange
        var service = Services.GetRequiredService<ITnTPopoverService>();
        var cut = Render<TnTPopoverHost>();

        // Act
        await service.OpenAsync(CreateContent("Accessible"), new TnTPopoverOptions {
            Description = "Modeless window",
            Title = "Accessible"
        });

        // Assert
        var popover = cut.Find(".tnt-popover");
        popover.GetAttribute("role").Should().Be("dialog");
        popover.GetAttribute("aria-modal").Should().Be("false");
        popover.GetAttribute("aria-labelledby").Should().NotBeNullOrWhiteSpace();
        popover.GetAttribute("aria-describedby").Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task SsrMode_RendersWithoutJsInteropAndDisablesHeaderButtons() {
        // Arrange
        var service = Services.GetRequiredService<ITnTPopoverService>();
        SetRendererInfo(new RendererInfo("Static", false));
        await service.OpenAsync(CreateContent("Static"), new TnTPopoverOptions { Title = "Static" });

        // Act
        var cut = Render<TnTPopoverHost>();

        // Assert
        cut.Find("button[aria-label='Hide Static']").HasAttribute("disabled").Should().BeTrue();
        cut.Find("button[aria-label='Close Static']").HasAttribute("disabled").Should().BeTrue();
        JSInterop.Invocations.Should().BeEmpty();
    }

    private static RenderFragment CreateContent(string text) {
        return builder => {
            builder.OpenElement(0, "div");
            builder.AddContent(1, text);
            builder.CloseElement();
        };
    }
}
