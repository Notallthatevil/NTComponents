using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using NTComponents.Snackbar;

namespace NTComponents.Tests.Snackbar;

/// <summary>
///     bUnit tests for <see cref="NTSnackbar" />.
/// </summary>
public class NTSnackbar_Tests : BunitContext {

    public NTSnackbar_Tests() {
        Services.AddSingleton<INTSnackbarService, NTSnackbarService>();
        SetRendererInfo(new RendererInfo("WebAssembly", true));
    }

    [Fact]
    public void Constructor_InitializesCorrectly() {
        // Arrange & Act
        var cut = RenderSnackbarComponent();

        // Assert
        cut.Should().NotBeNull();
        cut.Instance.Should().NotBeNull();
    }

    [Fact]
    public void EmptySnackbars_RenderNothing() {
        // Arrange & Act
        var cut = RenderSnackbarComponent();

        // Assert
        cut.Markup.Trim().Should().BeEmpty();
    }

    [Fact]
    public async Task ShowAsync_RendersSnackbarMessage() {
        // Arrange
        var cut = RenderSnackbarComponent();
        var service = Services.GetRequiredService<INTSnackbarService>();

        // Act
        await service.ShowAsync("Profile saved");
        cut.Render();

        // Assert
        cut.Markup.Should().Contain("nt-snackbar-container");
        cut.Markup.Should().Contain("nt-snackbar");
        cut.Markup.Should().Contain("Profile saved");
    }

    [Fact]
    public async Task Snackbar_DefaultsToBottomCenterPosition() {
        // Arrange
        var cut = RenderSnackbarComponent();
        var service = Services.GetRequiredService<INTSnackbarService>();

        // Act
        await service.ShowAsync("Positioned");
        cut.Render();

        // Assert
        cut.Find(".nt-snackbar-container").ClassList.Should().Contain("nt-snackbar-bottom-center");
    }

    [Fact]
    public async Task Snackbar_UsesConfiguredPositionClass() {
        // Arrange
        var cut = RenderSnackbarComponent(parameters => parameters.Add(p => p.Position, NTSnackbarPosition.TopRightCorner));
        var service = Services.GetRequiredService<INTSnackbarService>();

        // Act
        await service.ShowAsync("Positioned");
        cut.Render();

        // Assert
        var container = cut.Find(".nt-snackbar-container");
        container.ClassList.Should().Contain("nt-snackbar-top-right-corner");
        container.ClassList.Should().NotContain("nt-snackbar-bottom-center");
    }

    [Fact]
    public async Task ExistingActiveSnackbar_IsRenderedOnInitialLoad() {
        // Arrange
        var service = Services.GetRequiredService<INTSnackbarService>();
        await service.ShowAsync("Already active");

        // Act
        var cut = RenderSnackbarComponent();

        // Assert
        cut.Markup.Should().Contain("Already active");
    }

    [Fact]
    public async Task Snackbar_AppliesCustomStyleVariables() {
        // Arrange
        var cut = RenderSnackbarComponent();
        var service = Services.GetRequiredService<INTSnackbarService>();

        // Act
        await service.ShowAsync(
            message: "Saved",
            backgroundColor: TnTColor.Primary,
            textColor: TnTColor.OnPrimary,
            actionColor: TnTColor.Secondary
        );
        cut.Render();

        // Assert
        var snackbar = cut.Find(".nt-snackbar");
        var style = snackbar.GetAttribute("style");
        style.Should().Contain("--nt-snackbar-background-color:var(--tnt-color-primary)");
        style.Should().Contain("--nt-snackbar-text-color:var(--tnt-color-on-primary)");
        style.Should().Contain("--nt-snackbar-action-color:var(--tnt-color-secondary)");
    }

    [Fact]
    public async Task Snackbar_HasAccessibleStatusAttributes() {
        // Arrange
        var cut = RenderSnackbarComponent();
        var service = Services.GetRequiredService<INTSnackbarService>();

        // Act
        await service.ShowAsync("Accessible update");
        cut.Render();

        // Assert
        var snackbar = cut.Find(".nt-snackbar");
        snackbar.GetAttribute("role").Should().Be("status");
        snackbar.GetAttribute("aria-live").Should().Be("polite");
        snackbar.GetAttribute("aria-atomic").Should().Be("true");
    }

    [Fact]
    public async Task ActionSnackbar_RendersActionButton_AndCloseButtonByDefault() {
        // Arrange
        var cut = RenderSnackbarComponent();
        var service = Services.GetRequiredService<INTSnackbarService>();

        // Act
        await service.ShowAsync("Email archived", "Undo", () => Task.CompletedTask);
        cut.Render();

        // Assert
        cut.Find(".nt-snackbar-action").TextContent.Should().Be("Undo");
        cut.Find(".nt-snackbar-close").Should().NotBeNull();
    }

    [Fact]
    public async Task Snackbar_WithoutAction_DoesNotRenderActionButton() {
        // Arrange
        var cut = RenderSnackbarComponent();
        var service = Services.GetRequiredService<INTSnackbarService>();

        // Act
        await service.ShowAsync("Saved");
        cut.Render();

        // Assert
        cut.FindAll(".nt-snackbar-action").Should().BeEmpty();
    }

    [Fact]
    public async Task Snackbar_WithoutCloseButton_DoesNotRenderCloseButton() {
        // Arrange
        var cut = RenderSnackbarComponent();
        var service = Services.GetRequiredService<INTSnackbarService>();

        // Act
        await service.ShowAsync("Saved", showClose: false);
        cut.Render();

        // Assert
        cut.FindAll(".nt-snackbar-close").Should().BeEmpty();
    }

    [Fact]
    public async Task ActionButton_Click_InvokesCallback_AndClosesSnackbar() {
        // Arrange
        var cut = RenderSnackbarComponent();
        var service = Services.GetRequiredService<INTSnackbarService>();
        var callbackInvoked = false;

        await service.ShowAsync("Email archived", "Undo", () => {
            callbackInvoked = true;
            return Task.CompletedTask;
        });
        cut.Render();

        // Act
        await cut.Find(".nt-snackbar-action").ClickAsync(new MouseEventArgs());
        await Task.Delay(250, Xunit.TestContext.Current.CancellationToken);

        // Assert
        callbackInvoked.Should().BeTrue();
        cut.FindAll(".nt-snackbar").Should().BeEmpty();
    }

    [Fact]
    public async Task ActionButton_WhenCallbackThrows_KeepsSnackbarVisible() {
        // Arrange
        var cut = RenderSnackbarComponent();
        var service = Services.GetRequiredService<INTSnackbarService>();

        await service.ShowAsync("Email archived", "Undo", () => throw new InvalidOperationException("boom"));
        cut.Render();

        // Act
        Func<Task> click = async () => await cut.Find(".nt-snackbar-action").ClickAsync(new MouseEventArgs());

        // Assert
        await click.Should().ThrowAsync<InvalidOperationException>();
        cut.FindAll(".nt-snackbar").Should().HaveCount(1);
        cut.Markup.Should().Contain("Email archived");
    }

    [Fact]
    public async Task CloseButton_Click_RemovesSnackbar() {
        // Arrange
        var cut = RenderSnackbarComponent();
        var service = Services.GetRequiredService<INTSnackbarService>();

        await service.ShowAsync("Saved", showClose: true);
        cut.Render();

        // Act
        await cut.Find(".nt-snackbar-close").ClickAsync(new MouseEventArgs());
        await Task.Delay(250, Xunit.TestContext.Current.CancellationToken);

        // Assert
        cut.FindAll(".nt-snackbar").Should().BeEmpty();
    }

    [Fact]
    public async Task AutoDismiss_ClosesTimedSnackbar() {
        // Arrange
        var cut = RenderSnackbarComponent();
        var service = Services.GetRequiredService<INTSnackbarService>();

        // Act
        await service.ShowAsync("Short lived", timeout: 1);
        cut.Render();
        await Task.Delay(1300, Xunit.TestContext.Current.CancellationToken);

        // Assert
        cut.FindAll(".nt-snackbar").Should().BeEmpty();
    }

    [Fact]
    public async Task DefaultActionSnackbar_DoesNotAutoDismiss() {
        // Arrange
        var cut = RenderSnackbarComponent();
        var service = Services.GetRequiredService<INTSnackbarService>();

        // Act
        await service.ShowAsync("Email archived", "Undo", () => Task.CompletedTask);
        cut.Render();
        await Task.Delay(1200, Xunit.TestContext.Current.CancellationToken);

        // Assert
        cut.FindAll(".nt-snackbar").Should().HaveCount(1);
    }

    [Fact]
    public async Task QueuedSnackbars_RenderUpToThreeStackLayers() {
        // Arrange
        var cut = RenderSnackbarComponent();
        var service = Services.GetRequiredService<INTSnackbarService>();

        // Act
        await service.ShowAsync("First");
        await service.ShowAsync("Second");
        await service.ShowAsync("Third");
        await service.ShowAsync("Fourth");
        cut.Render();

        // Assert
        var stackItems = cut.FindAll(".nt-snackbar-stack-item");
        stackItems.Should().HaveCount(3);

        stackItems[0].ClassList.Should().Contain("nt-snackbar-front");
        stackItems[0].ClassList.Should().Contain("nt-snackbar-stack-depth-0");
        stackItems[0].TextContent.Should().Contain("First");

        stackItems[1].ClassList.Should().Contain("nt-snackbar-queued");
        stackItems[1].ClassList.Should().Contain("nt-snackbar-stack-depth-1");
        stackItems[1].TextContent.Should().Contain("Second");

        stackItems[2].ClassList.Should().Contain("nt-snackbar-queued");
        stackItems[2].ClassList.Should().Contain("nt-snackbar-stack-depth-2");
        stackItems[2].TextContent.Should().Contain("Third");

        cut.Markup.Should().NotContain("Fourth");
    }

    [Fact]
    public async Task QueuedSnackbars_PromoteWhenFrontSnackbarCloses() {
        // Arrange
        var cut = RenderSnackbarComponent();
        var service = (NTSnackbarService)Services.GetRequiredService<INTSnackbarService>();

        // Act
        await service.ShowAsync("First");
        await service.ShowAsync("Second");
        await service.ShowAsync("Third");
        cut.Render();

        // Assert
        cut.Find(".nt-snackbar-front .nt-snackbar-message").TextContent.Should().Contain("First");
        cut.Find(".nt-snackbar-stack-depth-1 .nt-snackbar-message").TextContent.Should().Contain("Second");

        // Act
        await service.CloseAsync(service.ActiveSnackbar!);
        await Task.Delay(250, Xunit.TestContext.Current.CancellationToken);
        cut.Render();

        // Assert
        var stackItems = cut.FindAll(".nt-snackbar-stack-item");
        stackItems.Should().HaveCount(2);
        cut.Find(".nt-snackbar-front .nt-snackbar-message").TextContent.Should().Contain("Second");
        cut.Find(".nt-snackbar-stack-depth-1 .nt-snackbar-message").TextContent.Should().Contain("Third");
    }

    [Fact]
    public async Task QueuedSnackbars_OnlyFrontSnackbarRendersInteractiveActions() {
        // Arrange
        var cut = RenderSnackbarComponent();
        var service = Services.GetRequiredService<INTSnackbarService>();

        // Act
        await service.ShowAsync("First", "Undo", () => Task.CompletedTask);
        await service.ShowAsync("Second", "Undo", () => Task.CompletedTask);
        cut.Render();

        // Assert
        cut.FindAll(".nt-snackbar-action").Should().HaveCount(1);
        cut.FindAll(".nt-snackbar-close").Should().HaveCount(1);
        cut.FindAll(".nt-snackbar[role='status']").Should().HaveCount(1);
        cut.FindAll(".nt-snackbar[aria-hidden='true']").Should().HaveCount(1);
    }

    [Fact]
    public async Task Dispose_UnsubscribesFromServiceEvents() {
        // Arrange
        var cut = RenderSnackbarComponent();
        var service = Services.GetRequiredService<INTSnackbarService>();

        // Act
        cut.Instance.Dispose();
        await service.ShowAsync("Should not render after dispose");
        cut.Render();

        // Assert
        cut.Markup.Trim().Should().BeEmpty();
    }

    [Fact]
    public async Task NonInteractiveSnackbar_RendersSsrAutoDismissScript_AndDisabledActionButton() {
        // Arrange
        SetRendererInfo(new RendererInfo("Static", false));
        var cut = RenderSnackbarComponent();
        var service = Services.GetRequiredService<INTSnackbarService>();

        // Act
        await service.ShowAsync("Email archived", "Undo", () => Task.CompletedTask, timeout: 5);
        cut.Render();

        // Assert
        cut.FindAll("script").Should().HaveCount(1);
        cut.Find(".nt-snackbar-action").HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public async Task NonInteractivePersistentSnackbar_DoesNotRenderAutoDismissScript() {
        // Arrange
        SetRendererInfo(new RendererInfo("Static", false));
        var cut = RenderSnackbarComponent();
        var service = Services.GetRequiredService<INTSnackbarService>();

        // Act
        await service.ShowAsync("Email archived", "Undo", () => Task.CompletedTask);
        cut.Render();

        // Assert
        cut.FindAll("script").Should().BeEmpty();
    }

    private IRenderedComponent<NTSnackbar> RenderSnackbarComponent(Action<ComponentParameterCollectionBuilder<NTSnackbar>>? configure = null) {
        return configure is null ? Render<NTSnackbar>() : Render<NTSnackbar>(configure);
    }
}
