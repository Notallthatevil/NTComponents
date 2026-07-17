using NTComponents.Toast;

namespace NTComponents.Tests.Toast;

/// <summary>
///     Unit tests for <see cref="TnTToastService" />.
/// </summary>
public class TnTToastService_Tests {

    [Fact]
    public void Constructor_InitializesCorrectly() {
        // Arrange & Act
        var service = new TnTToastService();

        // Assert
        service.Should().NotBeNull();
        service.Should().BeAssignableTo<ITnTToastService>();
    }

    [Fact]
    public async Task EventHandlers_CanBeUnsubscribed() {
        // Arrange
        var service = new TnTToastService();
        var openCount = 0;
        var closeCount = 0;

        ITnTToastService.OnOpenCallback openHandler = (toast) => {
            openCount++;
            return Task.CompletedTask;
        };

        ITnTToastService.OnCloseCallback closeHandler = (toast) => {
            closeCount++;
            return Task.CompletedTask;
        };

        service.OnOpen += openHandler;
        service.OnClose += closeHandler;

        // Act
        await service.ShowAsync("Test 1");
        await service.CloseAsync(CreateMockToast("Test"));

        service.OnOpen -= openHandler;
        service.OnClose -= closeHandler;

        await service.ShowAsync("Test 2");
        await service.CloseAsync(CreateMockToast("Test"));

        // Assert
        openCount.Should().Be(1);
        closeCount.Should().Be(1);
    }

    [Fact]
    public async Task Events_NoException_WhenNoSubscribers() {
        // Arrange
        var service = new TnTToastService();
        var mockToast = CreateMockToast("Test Toast");

        // Act & Assert
        await service.ShowAsync("Test Title");
        await service.CloseAsync(mockToast);
        // No exceptions should be thrown
    }

    [Fact]
    public async Task MultipleToasts_EachTriggersEvent() {
        // Arrange
        var service = new TnTToastService();
        var toastCount = 0;

        service.OnOpen += (toast) => {
            toastCount++;
            return Task.CompletedTask;
        };

        // Act
        await service.ShowAsync("Toast 1");
        await service.ShowErrorAsync("Toast 2");
        await service.ShowInfoAsync("Toast 3");
        await service.ShowSuccessAsync("Toast 4");
        await service.ShowWarningAsync("Toast 5");

        // Assert
        toastCount.Should().Be(5);
    }

    [Fact]
    public async Task OnClose_EventTriggered_WhenCloseAsyncCalled() {
        // Arrange
        var service = new TnTToastService();
        var mockToast = CreateMockToast("Test Toast");
        ITnTToast? capturedToast = null;
        var eventTriggered = false;

        service.OnClose += (toast) => {
            capturedToast = toast;
            eventTriggered = true;
            return Task.CompletedTask;
        };

        // Act
        await service.CloseAsync(mockToast);

        // Assert
        eventTriggered.Should().BeTrue();
        capturedToast.Should().Be(mockToast);
    }

    [Fact]
    public async Task OnOpen_EventTriggered_WhenShowAsyncCalled() {
        // Arrange
        var service = new TnTToastService();
        ITnTToast? capturedToast = null;
        var eventTriggered = false;

        service.OnOpen += (toast) => {
            capturedToast = toast;
            eventTriggered = true;
            return Task.CompletedTask;
        };

        // Act
        await service.ShowAsync("Test Title", "Test Message");

        // Assert
        eventTriggered.Should().BeTrue();
        capturedToast.Should().NotBeNull();
        capturedToast!.Title.Should().Be("Test Title");
        capturedToast.Message.Should().Be("Test Message");
    }

    [Fact]
    public async Task ShowAsync_CreatesToastWithCorrectProperties() {
        // Arrange
        var service = new TnTToastService();
        ITnTToast? createdToast = null;

        service.OnOpen += (toast) => {
            createdToast = toast;
            return Task.CompletedTask;
        };

        // Act
        await service.ShowAsync(
            title: "Test Title",
            message: "Test Message",
            timeout: 5,
            showClose: false,
            backgroundColor: TnTColor.Primary,
            textColor: TnTColor.OnPrimary
        );

        // Assert
        createdToast.Should().NotBeNull();
        createdToast!.Title.Should().Be("Test Title");
        createdToast.Message.Should().Be("Test Message");
        createdToast.Timeout.Should().Be(5);
        createdToast.ShowClose.Should().BeFalse();
        createdToast.BackgroundColor.Should().Be(TnTColor.Primary);
        createdToast.TextColor.Should().Be(TnTColor.OnPrimary);
    }

    [Fact]
    public async Task ShowAsync_UsesDefaultValues_WhenOptionalParametersNotProvided() {
        // Arrange
        var service = new TnTToastService();
        ITnTToast? createdToast = null;

        service.OnOpen += (toast) => {
            createdToast = toast;
            return Task.CompletedTask;
        };

        // Act
        await service.ShowAsync("Test Title");

        // Assert
        createdToast.Should().NotBeNull();
        createdToast!.Title.Should().Be("Test Title");
        createdToast.Message.Should().BeNull();
        createdToast.Timeout.Should().Be(10);
        createdToast.ShowClose.Should().BeTrue();
        createdToast.BackgroundColor.Should().Be(TnTColor.SurfaceVariant);
        createdToast.TextColor.Should().Be(TnTColor.OnSurfaceVariant);
    }

    [Fact]
    public async Task ShowAsync_WithEmptyTitle_HandlesGracefully() {
        // Arrange
        var service = new TnTToastService();
        ITnTToast? createdToast = null;

        service.OnOpen += (toast) => {
            createdToast = toast;
            return Task.CompletedTask;
        };

        // Act
        await service.ShowAsync("");

        // Assert
        createdToast.Should().NotBeNull();
        createdToast!.Title.Should().Be("");
    }

    [Fact]
    public async Task ShowAsync_WithLongTexts_HandlesGracefully() {
        // Arrange
        var service = new TnTToastService();
        ITnTToast? createdToast = null;
        var longTitle = new string('A', 1000);
        var longMessage = new string('B', 2000);

        service.OnOpen += (toast) => {
            createdToast = toast;
            return Task.CompletedTask;
        };

        // Act
        await service.ShowAsync(longTitle, longMessage);

        // Assert
        createdToast.Should().NotBeNull();
        createdToast!.Title.Should().Be(longTitle);
        createdToast.Message.Should().Be(longMessage);
    }

    [Fact]
    public async Task ShowAsync_WithNegativeTimeout_CreatesValidToast() {
        // Arrange
        var service = new TnTToastService();
        ITnTToast? createdToast = null;

        service.OnOpen += (toast) => {
            createdToast = toast;
            return Task.CompletedTask;
        };

        // Act
        await service.ShowAsync("Test Title", timeout: -5);

        // Assert
        createdToast.Should().NotBeNull();
        createdToast!.Timeout.Should().Be(-5);
    }

    [Fact]
    public async Task ShowAsync_WithNullMessage_CreatesValidToast() {
        // Arrange
        var service = new TnTToastService();
        ITnTToast? createdToast = null;

        service.OnOpen += (toast) => {
            createdToast = toast;
            return Task.CompletedTask;
        };

        // Act
        await service.ShowAsync("Test Title", null);

        // Assert
        createdToast.Should().NotBeNull();
        createdToast!.Title.Should().Be("Test Title");
        createdToast.Message.Should().BeNull();
    }

    [Fact]
    public async Task ShowAsync_WithZeroTimeout_CreatesValidToast() {
        // Arrange
        var service = new TnTToastService();
        ITnTToast? createdToast = null;

        service.OnOpen += (toast) => {
            createdToast = toast;
            return Task.CompletedTask;
        };

        // Act
        await service.ShowAsync("Test Title", timeout: 0);

        // Assert
        createdToast.Should().NotBeNull();
        createdToast!.Timeout.Should().Be(0);
    }

    [Fact]
    public async Task ShowErrorAsync_UsesDefaultValues() {
        // Arrange
        var service = new TnTToastService();
        ITnTToast? createdToast = null;

        service.OnOpen += (toast) => {
            createdToast = toast;
            return Task.CompletedTask;
        };

        // Act
        await service.ShowErrorAsync("Error Title");

        // Assert
        createdToast.Should().NotBeNull();
        createdToast!.Timeout.Should().Be(10);
        createdToast.ShowClose.Should().BeTrue();
    }

    [Fact]
    public async Task ShowErrorAsync_WithException_UsesExceptionMessage() {
        // Arrange
        var service = new TnTToastService();
        ITnTToast? createdToast = null;
        var exception = new InvalidOperationException("Something went wrong");

        service.OnOpen += (toast) => {
            createdToast = toast;
            return Task.CompletedTask;
        };

        // Act
        await service.ShowErrorAsync("Error Title", exception);

        // Assert
        createdToast.Should().NotBeNull();
        createdToast!.Title.Should().Be("Error Title");
        createdToast.Message.Should().Be("Something went wrong");
        createdToast.BackgroundColor.Should().Be(TnTColor.Error);
        createdToast.TextColor.Should().Be(TnTColor.OnError);
    }

    [Fact]
    public async Task ShowErrorAsync_WithNullException_HandlesGracefully() {
        // Arrange
        var service = new TnTToastService();
        ITnTToast? createdToast = null;

        service.OnOpen += (toast) => {
            createdToast = toast;
            return Task.CompletedTask;
        };

        // Act
        await service.ShowErrorAsync("Error Title", (Exception?)null);

        // Assert
        createdToast.Should().NotBeNull();
        createdToast!.Title.Should().Be("Error Title");
        createdToast.Message.Should().BeNull();
    }

    [Fact]
    public async Task ShowErrorAsync_WithStringMessage_CreatesErrorToast() {
        // Arrange
        var service = new TnTToastService();
        ITnTToast? createdToast = null;

        service.OnOpen += (toast) => {
            createdToast = toast;
            return Task.CompletedTask;
        };

        // Act
        await service.ShowErrorAsync("Error Title", "Error Message", 15, false);

        // Assert
        createdToast.Should().NotBeNull();
        createdToast!.Title.Should().Be("Error Title");
        createdToast.Message.Should().Be("Error Message");
        createdToast.Timeout.Should().Be(15);
        createdToast.ShowClose.Should().BeFalse();
        createdToast.BackgroundColor.Should().Be(TnTColor.Error);
        createdToast.TextColor.Should().Be(TnTColor.OnError);
    }

    [Fact]
    public async Task ShowInfoAsync_CreatesInfoToast() {
        // Arrange
        var service = new TnTToastService();
        ITnTToast? createdToast = null;

        service.OnOpen += (toast) => {
            createdToast = toast;
            return Task.CompletedTask;
        };

        // Act
        await service.ShowInfoAsync("Info Title", "Info Message", 8, false);

        // Assert
        createdToast.Should().NotBeNull();
        createdToast!.Title.Should().Be("Info Title");
        createdToast.Message.Should().Be("Info Message");
        createdToast.Timeout.Should().Be(8);
        createdToast.ShowClose.Should().BeFalse();
        createdToast.BackgroundColor.Should().Be(TnTColor.Info);
        createdToast.TextColor.Should().Be(TnTColor.OnInfo);
    }

    [Fact]
    public async Task ShowInfoAsync_UsesDefaultValues() {
        // Arrange
        var service = new TnTToastService();
        ITnTToast? createdToast = null;

        service.OnOpen += (toast) => {
            createdToast = toast;
            return Task.CompletedTask;
        };

        // Act
        await service.ShowInfoAsync("Info Title");

        // Assert
        createdToast.Should().NotBeNull();
        createdToast!.Timeout.Should().Be(10);
        createdToast.ShowClose.Should().BeTrue();
        createdToast.Message.Should().BeNull();
    }

    [Fact]
    public async Task ShowSuccessAsync_CreatesSuccessToast() {
        // Arrange
        var service = new TnTToastService();
        ITnTToast? createdToast = null;

        service.OnOpen += (toast) => {
            createdToast = toast;
            return Task.CompletedTask;
        };

        // Act
        await service.ShowSuccessAsync("Success Title", "Success Message", 12, false);

        // Assert
        createdToast.Should().NotBeNull();
        createdToast!.Title.Should().Be("Success Title");
        createdToast.Message.Should().Be("Success Message");
        createdToast.Timeout.Should().Be(12);
        createdToast.ShowClose.Should().BeFalse();
        createdToast.BackgroundColor.Should().Be(TnTColor.Success);
        createdToast.TextColor.Should().Be(TnTColor.OnSuccess);
    }

    [Fact]
    public async Task ShowSuccessAsync_UsesDefaultValues() {
        // Arrange
        var service = new TnTToastService();
        ITnTToast? createdToast = null;

        service.OnOpen += (toast) => {
            createdToast = toast;
            return Task.CompletedTask;
        };

        // Act
        await service.ShowSuccessAsync("Success Title");

        // Assert
        createdToast.Should().NotBeNull();
        createdToast!.Timeout.Should().Be(10);
        createdToast.ShowClose.Should().BeTrue();
        createdToast.Message.Should().BeNull();
    }

    [Fact]
    public async Task ShowWarningAsync_CreatesWarningToast() {
        // Arrange
        var service = new TnTToastService();
        ITnTToast? createdToast = null;

        service.OnOpen += (toast) => {
            createdToast = toast;
            return Task.CompletedTask;
        };

        // Act
        await service.ShowWarningAsync("Warning Title", "Warning Message", 6, false);

        // Assert
        createdToast.Should().NotBeNull();
        createdToast!.Title.Should().Be("Warning Title");
        createdToast.Message.Should().Be("Warning Message");
        createdToast.Timeout.Should().Be(6);
        createdToast.ShowClose.Should().BeFalse();
        createdToast.BackgroundColor.Should().Be(TnTColor.Warning);
        createdToast.TextColor.Should().Be(TnTColor.OnWarning);
    }

    [Fact]
    public async Task ShowWarningAsync_UsesDefaultValues() {
        // Arrange
        var service = new TnTToastService();
        ITnTToast? createdToast = null;

        service.OnOpen += (toast) => {
            createdToast = toast;
            return Task.CompletedTask;
        };

        // Act
        await service.ShowWarningAsync("Warning Title");

        // Assert
        createdToast.Should().NotBeNull();
        createdToast!.Timeout.Should().Be(10);
        createdToast.ShowClose.Should().BeTrue();
        createdToast.Message.Should().BeNull();
    }

    [Fact]
    public void TnTToastImplementation_CanSetProperties() {
        // Arrange
        var toast = new TnTToastService.TnTToastImplementation();

        // Act
        toast.Title = "Test Title";
        toast.Message = "Test Message";
        toast.ShowClose = false;
        toast.Timeout = 5;
        toast.BackgroundColor = TnTColor.Primary;
        toast.TextColor = TnTColor.OnPrimary;

        // Assert
        toast.Title.Should().Be("Test Title");
        toast.Message.Should().Be("Test Message");
        toast.ShowClose.Should().BeFalse();
        toast.Timeout.Should().Be(5);
        toast.BackgroundColor.Should().Be(TnTColor.Primary);
        toast.TextColor.Should().Be(TnTColor.OnPrimary);
    }

    [Fact]
    public void TnTToastImplementation_Closing_CanBeSetInternally() {
        // Arrange
        var toast = new TnTToastService.TnTToastImplementation();

        // Act
        var impl = toast as TnTToastService.TnTToastImplementation;
        impl!.Closing = true;

        // Assert
        toast.Closing.Should().BeTrue();
    }

    [Fact]
    public void TnTToastImplementation_HasCorrectDefaults() {
        // Arrange & Act
        var toast = new TnTToastService.TnTToastImplementation();

        // Assert
        toast.BackgroundColor.Should().Be(TnTColor.SurfaceVariant);
        toast.TextColor.Should().Be(TnTColor.OnSurfaceVariant);
        toast.ShowClose.Should().BeTrue();
        toast.Timeout.Should().Be(10);
        toast.Closing.Should().BeFalse();
        toast.TextAlignment.Should().BeNull();
        toast.Message.Should().BeNull();
    }

    private static ITnTToast CreateMockToast(string title) {
        return new TnTToastService.TnTToastImplementation {
            Title = title,
            Message = "Test Message",
            ShowClose = true,
            Timeout = 10,
            BackgroundColor = TnTColor.SurfaceVariant,
            TextColor = TnTColor.OnSurfaceVariant
        };
    }
}