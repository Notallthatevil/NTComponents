using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using NTComponents.Popover;

namespace NTComponents.Tests.Popover;

/// <summary>
///     bUnit tests for <see cref="NTPopoverHost" />.
/// </summary>
public class NTPopoverHost_Tests : BunitContext {
    private const string JsModulePath = "./_content/NTComponents/Popover/NTPopoverWindow.razor.js";

    public NTPopoverHost_Tests() {
        Services.AddSingleton<INTPopoverService, NTPopoverService>();
        SetRendererInfo(new RendererInfo("WebAssembly", true));

        var module = JSInterop.SetupModule(JsModulePath);
        module.SetupVoid("initializePopoverWindow", _ => true).SetVoidResult();
        module.SetupVoid("updatePopoverWindow", _ => true).SetVoidResult();
        module.SetupVoid("highlightPopoverWindow", _ => true).SetVoidResult();
        module.SetupVoid("animatePopoverFromLauncher", _ => true).SetVoidResult();
        module.SetupVoid("animatePopoverToLauncher", _ => true).SetVoidResult();
        module.SetupVoid("disposePopoverWindow", _ => true).SetVoidResult();
        module.SetupVoid("waitForCloseAnimation", _ => true).SetVoidResult();
    }

    [Fact]
    public void EmptyHost_RendersNothing() {
        // Act
        var cut = Render<NTPopoverHost>();

        // Assert
        cut.Markup.Trim().Should().BeEmpty();
    }

    [Fact]
    public async Task EscapeKey_ClosesVisiblePopover() {
        // Arrange
        var service = Services.GetRequiredService<INTPopoverService>();
        var cut = Render<NTPopoverHost>();
        await service.OpenAsync(CreateContent("Notes"), new NTPopoverOptions { Title = "Notes" });

        // Act
        await cut.Find(".nt-popover").TriggerEventAsync("onkeydown", new KeyboardEventArgs { Key = "Escape" });

        // Assert
        cut.WaitForAssertion(() => cut.FindAll(".nt-popover").Should().BeEmpty());
    }

    [Fact]
    public async Task HideButton_MovesPopoverToLauncherStrip() {
        // Arrange
        var service = Services.GetRequiredService<INTPopoverService>();
        var cut = Render<NTPopoverHost>();
        await service.OpenAsync(CreateContent("Notes"), new NTPopoverOptions { Title = "Notes" });

        // Act
        await cut.Find("button[aria-label='Hide Notes']").ClickAsync(new MouseEventArgs());

        // Assert
        cut.WaitForAssertion(() => {
            cut.FindAll(".nt-popover").Should().BeEmpty();
            cut.Find(".nt-popover-host__launcher").TextContent.Should().Contain("Notes");
        });
    }

    [Fact]
    public async Task HiddenPopover_CanBeRestoredFromLauncherStrip() {
        // Arrange
        var service = Services.GetRequiredService<INTPopoverService>();
        var cut = Render<NTPopoverHost>();
        await service.OpenAsync(CreateContent("Inspector"), new NTPopoverOptions { Title = "Inspector" });
        await cut.Find("button[aria-label='Hide Inspector']").ClickAsync(new MouseEventArgs());

        // Act
        await cut.Find("button[aria-label='Restore Inspector']").ClickAsync(new MouseEventArgs());

        // Assert
        cut.WaitForAssertion(() => {
            cut.Find(".nt-popover__title").TextContent.Should().Be("Inspector");
            cut.FindAll(".nt-popover-host__launcher").Should().BeEmpty();
        });
    }

    [Fact]
    public async Task ReopeningVisiblePopover_InvokesHighlightAnimation() {
        // Arrange
        var service = Services.GetRequiredService<INTPopoverService>();
        var cut = Render<NTPopoverHost>();
        var options = new NTPopoverOptions {
            InstanceKey = "inspector",
            Title = "Inspector"
        };
        await service.OpenAsync(CreateContent("Inspector"), options);

        // Act
        await service.OpenAsync(CreateContent("Inspector"), options);

        // Assert
        cut.WaitForAssertion(() =>
            JSInterop.Invocations.Should().Contain(invocation => invocation.Identifier == "highlightPopoverWindow"));
    }

    [Fact]
    public async Task VisiblePopover_RendersAccessibleDialogMarkup() {
        // Arrange
        var service = Services.GetRequiredService<INTPopoverService>();
        var cut = Render<NTPopoverHost>();

        // Act
        await service.OpenAsync(CreateContent("Accessible"), new NTPopoverOptions {
            Description = "Modeless window",
            Title = "Accessible"
        });

        // Assert
        var popover = cut.Find(".nt-popover");
        popover.GetAttribute("role").Should().Be("dialog");
        popover.GetAttribute("aria-modal").Should().Be("false");
        popover.GetAttribute("aria-labelledby").Should().NotBeNullOrWhiteSpace();
        popover.GetAttribute("aria-describedby").Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task SsrMode_RendersWithoutJsInteropAndDisablesHeaderButtons() {
        // Arrange
        var service = Services.GetRequiredService<INTPopoverService>();
        SetRendererInfo(new RendererInfo("Static", false));
        await service.OpenAsync(CreateContent("Static"), new NTPopoverOptions { Title = "Static" });

        // Act
        var cut = Render<NTPopoverHost>();

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
