using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using NTComponents.Toast;

namespace NTComponents.Tests.Toast;

/// <summary>
///     Unit tests for <see cref="TnTToast" />.
/// </summary>
public class TnTToast_Tests : BunitContext {

    public TnTToast_Tests() {
        Services.AddSingleton<ITnTToastService, TnTToastService>();
        // Set renderer info to handle NET9_0_OR_GREATER conditional compilation
        SetRendererInfo(new RendererInfo("WebAssembly", true));

        // Setup required JS modules that the TnTToast component might use
        var rippleModule = JSInterop.SetupModule("./_content/NTComponents/Core/TnTRippleEffect.razor.js");
        rippleModule.SetupVoid("onLoad", _ => true);
        rippleModule.SetupVoid("onUpdate", _ => true);
        rippleModule.SetupVoid("onDispose", _ => true);
    }

    [Fact]
    public async Task CloseButton_Click_RemovesToast() {
        // Arrange
        var cut = RenderToastComponent();
        var service = Services.GetRequiredService<ITnTToastService>();
        await service.ShowAsync("Test Title", showClose: true);
        cut.Render();

        // Verify toast is rendered
        cut.FindAll(".tnt-toast").Should().HaveCount(1);

        // Act
        var closeButton = cut.Find("button.tnt-image-button");
        await closeButton.ClickAsync(new MouseEventArgs());

        // Wait a bit for the close delay
        await Task.Delay(300, Xunit.TestContext.Current.CancellationToken);

        // Assert
        cut.FindAll(".tnt-toast").Should().BeEmpty();
    }

    [Fact]
    public void Constructor_InitializesCorrectly() {
        // Arrange & Act
        var cut = RenderToastComponent();

        // Assert
        cut.Should().NotBeNull();
        cut.Instance.Should().NotBeNull();
    }

    [Fact]
    public void Dispose_CleansUpResources() {
        // Arrange
        var cut = RenderToastComponent();

        // Act & Assert - Should not throw exception
        cut.Instance.Dispose();
    }

    [Fact]
    public void EmptyToasts_RendersNothing() {
        // Arrange & Act
        var cut = RenderToastComponent();

        // Assert
        var markup = cut.Markup.Trim();
        markup.Should().BeEmpty(); // The component renders nothing when no toasts are present
    }

    [Fact]
    public async Task MoreThanFiveToasts_RendersOnlyFirstFive() {
        // Arrange
        var cut = RenderToastComponent();
        var service = Services.GetRequiredService<ITnTToastService>();

        // Act - Create 7 toasts
        for (int i = 1; i <= 7; i++) {
            await service.ShowAsync($"Toast {i}");
        }
        cut.Render();

        // Assert
        var renderedToasts = cut.FindAll(".tnt-toast");
        renderedToasts.Should().HaveCount(5);

        var markup = cut.Markup;
        markup.Should().Contain("Toast 1");
        markup.Should().Contain("Toast 5");
        markup.Should().NotContain("Toast 6");
        markup.Should().NotContain("Toast 7");
    }

    [Fact]
    public async Task MultipleToasts_RendersAllToasts() {
        // Arrange
        var cut = RenderToastComponent();
        var service = Services.GetRequiredService<ITnTToastService>();

        // Act
        await service.ShowAsync("Toast 1");
        await service.ShowAsync("Toast 2");
        await service.ShowAsync("Toast 3");
        cut.Render();

        // Assert
        var toasts = cut.FindAll(".tnt-toast");
        toasts.Should().HaveCount(3);

        var markup = cut.Markup;
        markup.Should().Contain("Toast 1");
        markup.Should().Contain("Toast 2");
        markup.Should().Contain("Toast 3");
    }

    [Fact]
    public async Task ProgressBar_HasCorrectBackgroundColor() {
        // Arrange
        var cut = RenderToastComponent();
        var service = Services.GetRequiredService<ITnTToastService>();

        // Act
        await service.ShowWarningAsync("Test Title", timeout: 5);
        cut.Render();

        // Assert
        var progressBar = cut.Find(".tnt-toast-progress");
        var style = progressBar.GetAttribute("style");
        style.Should().Contain("background-color: var(--tnt-color-on-warning)");
    }

    [Fact]
    public async Task ShowAsync_TriggersToastDisplay() {
        // Arrange
        var cut = RenderToastComponent();
        var service = Services.GetRequiredService<ITnTToastService>();

        // Verify initially empty
        cut.Markup.Trim().Should().BeEmpty();

        // Act
        await service.ShowAsync("Test Title", "Test Message");

        // Force re-render to see changes
        cut.Render();

        // Assert
        var markup = cut.Markup;
        markup.Should().NotBeEmpty();
        markup.Should().Contain("Test Title");
        markup.Should().Contain("Test Message");
        markup.Should().Contain("tnt-toast-container");
        markup.Should().Contain("tnt-toast");
    }

    [Fact]
    public async Task ShowErrorAsync_CreatesErrorToast() {
        // Arrange
        var cut = RenderToastComponent();
        var service = Services.GetRequiredService<ITnTToastService>();

        // Act
        await service.ShowErrorAsync("Error Title", "Error Message");
        cut.Render();

        // Assert
        var markup = cut.Markup;
        markup.Should().Contain("Error Title");
        markup.Should().Contain("Error Message");
        markup.Should().Contain("tnt-toast-container");
    }

    [Fact]
    public async Task ShowInfoAsync_CreatesInfoToast() {
        // Arrange
        var cut = RenderToastComponent();
        var service = Services.GetRequiredService<ITnTToastService>();

        // Act
        await service.ShowInfoAsync("Info Title", "Info Message");
        cut.Render();

        // Assert
        var markup = cut.Markup;
        markup.Should().Contain("Info Title");
        markup.Should().Contain("Info Message");
        markup.Should().Contain("tnt-toast-container");
    }

    [Fact]
    public async Task ShowSuccessAsync_CreatesSuccessToast() {
        // Arrange
        var cut = RenderToastComponent();
        var service = Services.GetRequiredService<ITnTToastService>();

        // Act
        await service.ShowSuccessAsync("Success Title", "Success Message");
        cut.Render();

        // Assert
        var markup = cut.Markup;
        markup.Should().Contain("Success Title");
        markup.Should().Contain("Success Message");
        markup.Should().Contain("tnt-toast-container");
    }

    [Fact]
    public async Task ShowWarningAsync_CreatesWarningToast() {
        // Arrange
        var cut = RenderToastComponent();
        var service = Services.GetRequiredService<ITnTToastService>();

        // Act
        await service.ShowWarningAsync("Warning Title", "Warning Message");
        cut.Render();

        // Assert
        var markup = cut.Markup;
        markup.Should().Contain("Warning Title");
        markup.Should().Contain("Warning Message");
        markup.Should().Contain("tnt-toast-container");
    }

    [Fact]
    public async Task Toast_AppliesCorrectCssClasses() {
        // Arrange
        var cut = RenderToastComponent();
        var service = Services.GetRequiredService<ITnTToastService>();

        // Act
        await service.ShowAsync("Test Title");
        cut.Render();

        // Assert
        var toastElement = cut.Find(".tnt-toast");
        toastElement.GetAttribute("class").Should().Contain("tnt-toast");
    }

    [Fact]
    public async Task Toast_AppliesCorrectStyleVariables() {
        // Arrange
        var cut = RenderToastComponent();
        var service = Services.GetRequiredService<ITnTToastService>();

        // Act
        await service.ShowAsync("Test Title", "Test Message",
            backgroundColor: TnTColor.Primary,
            textColor: TnTColor.OnPrimary,
            timeout: 8);
        cut.Render();

        // Assert
        var toastElement = cut.Find(".tnt-toast");
        var style = toastElement.GetAttribute("style");
        style.Should().Contain("--tnt-toast-background-color:var(--tnt-color-primary)");
        style.Should().Contain("--tnt-toast-text-color:var(--tnt-color-on-primary)");
        style.Should().Contain("--timeout:8s");
    }

    [Fact]
    public async Task Toast_WithCloseButton_RendersCloseButton() {
        // Arrange
        var cut = RenderToastComponent();
        var service = Services.GetRequiredService<ITnTToastService>();

        // Act
        await service.ShowAsync("Test Title", "Test Message", showClose: true);
        cut.Render();

        // Assert
        var markup = cut.Markup;
        markup.Should().Contain("tnt-image-button");
        markup.Should().Contain("close"); // MaterialIcon.Close renders as "close"
    }

    [Fact]
    public async Task Toast_WithEmptyMessage_RendersCorrectly() {
        // Arrange
        var cut = RenderToastComponent();
        var service = Services.GetRequiredService<ITnTToastService>();

        // Act
        await service.ShowAsync("Title", "");
        cut.Render();

        // Assert
        var headerSpan = cut.Find(".tnt-toast-header span");
        headerSpan.TextContent.Should().Be("Title");

        var body = cut.Find(".tnt-toast-body");
        body.TextContent.Should().Be("");
    }

    [Fact]
    public async Task Toast_WithEmptyTitle_RendersCorrectly() {
        // Arrange
        var cut = RenderToastComponent();
        var service = Services.GetRequiredService<ITnTToastService>();

        // Act
        await service.ShowAsync("", "Message");
        cut.Render();

        // Assert
        var headerSpan = cut.Find(".tnt-toast-header span");
        headerSpan.TextContent.Should().Be("");

        var body = cut.Find(".tnt-toast-body");
        body.TextContent.Should().Be("Message");
    }

    [Fact]
    public async Task Toast_WithLongTexts_RendersCorrectly() {
        // Arrange
        var cut = RenderToastComponent();
        var service = Services.GetRequiredService<ITnTToastService>();
        var longTitle = new string('A', 100);
        var longMessage = new string('B', 500);

        // Act
        await service.ShowAsync(longTitle, longMessage);
        cut.Render();

        // Assert
        var headerSpan = cut.Find(".tnt-toast-header span");
        headerSpan.TextContent.Should().Be(longTitle);

        var body = cut.Find(".tnt-toast-body");
        body.TextContent.Should().Be(longMessage);
    }

    [Fact]
    public async Task Toast_WithNegativeTimeout_RendersWithoutProgressBar() {
        // Arrange
        var cut = RenderToastComponent();
        var service = Services.GetRequiredService<ITnTToastService>();

        // Act
        await service.ShowAsync("Test Title", timeout: -5);
        cut.Render();

        // Assert
        cut.FindAll(".tnt-toast-progress").Should().BeEmpty();
    }

    [Fact]
    public async Task Toast_WithoutCloseButton_DoesNotRenderCloseButton() {
        // Arrange
        var cut = RenderToastComponent();
        var service = Services.GetRequiredService<ITnTToastService>();

        // Act
        await service.ShowAsync("Test Title", "Test Message", showClose: false);
        cut.Render();

        // Assert
        var markup = cut.Markup;
        markup.Should().NotContain("tnt-image-button");
        markup.Should().NotContain("close");
    }

    [Fact]
    public async Task Toast_WithoutMessage_DoesNotRenderBody() {
        // Arrange
        var cut = RenderToastComponent();
        var service = Services.GetRequiredService<ITnTToastService>();

        // Act
        await service.ShowAsync("Test Title", null);
        cut.Render();

        // Assert
        var markup = cut.Markup;
        markup.Should().Contain("Test Title");
        markup.Should().NotContain("tnt-toast-body");
    }

    [Fact]
    public async Task Toast_WithTimeout_RendersProgressBar() {
        // Arrange
        var cut = RenderToastComponent();
        var service = Services.GetRequiredService<ITnTToastService>();

        // Act
        await service.ShowAsync("Test Title", "Test Message", timeout: 5);
        cut.Render();

        // Assert
        var markup = cut.Markup;
        markup.Should().Contain("tnt-toast-progress");
    }

    [Fact]
    public async Task Toast_WithZeroTimeout_DoesNotIncludeTimeoutVariable() {
        // Arrange
        var cut = RenderToastComponent();
        var service = Services.GetRequiredService<ITnTToastService>();

        // Act
        await service.ShowAsync("Test Title", timeout: 0);
        cut.Render();

        // Assert
        var toastElement = cut.Find(".tnt-toast");
        var style = toastElement.GetAttribute("style");
        style.Should().NotContain("--timeout");
    }

    [Fact]
    public async Task Toast_WithZeroTimeout_DoesNotRenderProgressBar() {
        // Arrange
        var cut = RenderToastComponent();
        var service = Services.GetRequiredService<ITnTToastService>();

        // Act
        await service.ShowAsync("Test Title", "Test Message", timeout: 0);
        cut.Render();

        // Assert
        var markup = cut.Markup;
        markup.Should().NotContain("tnt-toast-progress");
    }

    private IRenderedComponent<TnTToast> RenderToastComponent() {
        return Render<TnTToast>();
    }
}