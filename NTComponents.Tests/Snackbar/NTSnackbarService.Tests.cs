using NTComponents.Snackbar;

namespace NTComponents.Tests.Snackbar;

/// <summary>
///     Unit tests for <see cref="NTSnackbarService" />.
/// </summary>
public class NTSnackbarService_Tests {

    [Fact]
    public void Constructor_InitializesCorrectly() {
        // Arrange & Act
        var service = new NTSnackbarService();

        // Assert
        service.Should().NotBeNull();
        service.Should().BeAssignableTo<INTSnackbarService>();
        service.ActiveSnackbar.Should().BeNull();
    }

    [Fact]
    public async Task ShowAsync_UsesMaterial3Defaults_WhenNoActionIsProvided() {
        // Arrange
        var service = new NTSnackbarService();
        INTSnackbar? createdSnackbar = null;

        service.OnOpen += (snackbar) => {
            createdSnackbar = snackbar;
            return Task.CompletedTask;
        };

        // Act
        await service.ShowAsync("Saved");

        // Assert
        createdSnackbar.Should().NotBeNull();
        createdSnackbar!.Message.Should().Be("Saved");
        createdSnackbar.Timeout.Should().Be(6);
        createdSnackbar.ShowClose.Should().BeFalse();
        createdSnackbar.BackgroundColor.Should().Be(TnTColor.InverseSurface);
        createdSnackbar.TextColor.Should().Be(TnTColor.InverseOnSurface);
        createdSnackbar.ActionColor.Should().Be(TnTColor.InversePrimary);
        createdSnackbar.HasAction.Should().BeFalse();
    }

    [Fact]
    public async Task ShowAsync_WithAction_UsesPersistentActionDefaults() {
        // Arrange
        var service = new NTSnackbarService();
        INTSnackbar? createdSnackbar = null;

        service.OnOpen += (snackbar) => {
            createdSnackbar = snackbar;
            return Task.CompletedTask;
        };

        // Act
        await service.ShowAsync("Email archived", "Undo", () => Task.CompletedTask);

        // Assert
        createdSnackbar.Should().NotBeNull();
        createdSnackbar!.HasAction.Should().BeTrue();
        createdSnackbar.ActionLabel.Should().Be("Undo");
        createdSnackbar.Timeout.Should().Be(0);
        createdSnackbar.ShowClose.Should().BeTrue();
    }

    [Fact]
    public async Task ShowAsync_HonorsExplicitOverrides_WhenActionIsProvided() {
        // Arrange
        var service = new NTSnackbarService();
        INTSnackbar? createdSnackbar = null;

        service.OnOpen += (snackbar) => {
            createdSnackbar = snackbar;
            return Task.CompletedTask;
        };

        // Act
        await service.ShowAsync("Saved", "Undo", () => Task.CompletedTask, timeout: 9, showClose: false);

        // Assert
        createdSnackbar.Should().NotBeNull();
        createdSnackbar!.Timeout.Should().Be(9);
        createdSnackbar.ShowClose.Should().BeFalse();
    }

    [Fact]
    public async Task ShowAsync_Throws_WhenActionDefinitionIsIncomplete() {
        // Arrange
        var service = new NTSnackbarService();

        // Act
        var labelOnly = () => service.ShowAsync("Saved", actionLabel: "Undo");
        var callbackOnly = () => service.ShowAsync("Saved", actionCallback: () => Task.CompletedTask);

        // Assert
        await labelOnly.Should().ThrowAsync<ArgumentException>();
        await callbackOnly.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ShowAsync_QueuesSnackbars_AndOpensNextAfterClose() {
        // Arrange
        var service = new NTSnackbarService();
        var openedMessages = new List<string>();

        service.OnOpen += (snackbar) => {
            openedMessages.Add(snackbar.Message);
            return Task.CompletedTask;
        };

        // Act
        await service.ShowAsync("First");
        await service.ShowAsync("Second");
        await service.CloseAsync(service.ActiveSnackbar!);

        // Assert
        openedMessages.Should().Equal("First", "Second");
        service.ActiveSnackbar.Should().NotBeNull();
        service.ActiveSnackbar!.Message.Should().Be("Second");
    }

    [Fact]
    public async Task CloseAsync_FiresCloseBeforeOpeningNextSnackbar() {
        // Arrange
        var service = new NTSnackbarService();
        var eventLog = new List<string>();

        service.OnClose += (snackbar) => {
            eventLog.Add($"close:{snackbar.Message}");
            return Task.CompletedTask;
        };

        service.OnOpen += (snackbar) => {
            eventLog.Add($"open:{snackbar.Message}");
            return Task.CompletedTask;
        };

        await service.ShowAsync("First");
        await service.ShowAsync("Second");

        // Act
        await service.CloseAsync(service.ActiveSnackbar!);

        // Assert
        eventLog.Should().Equal("open:First", "close:First", "open:Second");
    }

    [Fact]
    public async Task InvokeActionAsync_ExecutesCallback_AndClosesSnackbar() {
        // Arrange
        var service = new NTSnackbarService();
        var callbackInvoked = false;
        var closeCount = 0;

        service.OnClose += (_) => {
            closeCount++;
            return Task.CompletedTask;
        };

        await service.ShowAsync("Email archived", "Undo", () => {
            callbackInvoked = true;
            return Task.CompletedTask;
        });

        // Act
        await service.InvokeActionAsync(service.ActiveSnackbar!);

        // Assert
        callbackInvoked.Should().BeTrue();
        closeCount.Should().Be(1);
        service.ActiveSnackbar.Should().BeNull();
    }

    [Fact]
    public async Task InvokeActionAsync_WithoutAction_DoesNothing() {
        // Arrange
        var service = new NTSnackbarService();

        await service.ShowAsync("Saved");
        var activeSnackbar = service.ActiveSnackbar;

        // Act
        await service.InvokeActionAsync(activeSnackbar!);

        // Assert
        service.ActiveSnackbar.Should().BeSameAs(activeSnackbar);
    }

    [Fact]
    public async Task InvokeActionAsync_WhenActionThrows_DoesNotCloseSnackbar() {
        // Arrange
        var service = new NTSnackbarService();
        var closeCount = 0;

        service.OnClose += (_) => {
            closeCount++;
            return Task.CompletedTask;
        };

        await service.ShowAsync("Email archived", "Undo", () => throw new InvalidOperationException("boom"));

        // Act
        var action = () => service.InvokeActionAsync(service.ActiveSnackbar!);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>();
        closeCount.Should().Be(0);
        service.ActiveSnackbar.Should().NotBeNull();
        service.ActiveSnackbar!.Message.Should().Be("Email archived");
    }

    [Fact]
    public async Task CloseAsync_UntrackedSnackbar_DoesNothing() {
        // Arrange
        var service = new NTSnackbarService();

        await service.ShowAsync("First");
        await service.ShowAsync("Second");

        var currentSnackbar = service.ActiveSnackbar;

        // Act
        await service.CloseAsync(new NTSnackbarService.NTSnackbarImplementation { Message = "Not tracked" });

        // Assert
        service.ActiveSnackbar.Should().BeSameAs(currentSnackbar);
        service.ActiveSnackbar!.Message.Should().Be("First");
    }

    [Fact]
    public async Task CloseAsync_WhenNoQueuedSnackbar_ClearsActiveSnackbar() {
        // Arrange
        var service = new NTSnackbarService();
        var closeCount = 0;

        service.OnClose += (_) => {
            closeCount++;
            return Task.CompletedTask;
        };

        await service.ShowAsync("Only");

        // Act
        await service.CloseAsync(service.ActiveSnackbar!);

        // Assert
        closeCount.Should().Be(1);
        service.ActiveSnackbar.Should().BeNull();
    }

    [Fact]
    public void NTSnackbarImplementation_HasExpectedDefaults() {
        // Arrange & Act
        var snackbar = new NTSnackbarService.NTSnackbarImplementation();

        // Assert
        snackbar.Message.Should().BeNullOrEmpty();
        snackbar.Timeout.Should().Be(6);
        snackbar.ShowClose.Should().BeFalse();
        snackbar.BackgroundColor.Should().Be(TnTColor.InverseSurface);
        snackbar.TextColor.Should().Be(TnTColor.InverseOnSurface);
        snackbar.ActionColor.Should().Be(TnTColor.InversePrimary);
        snackbar.HasAction.Should().BeFalse();
        snackbar.Closing.Should().BeFalse();
    }
}
