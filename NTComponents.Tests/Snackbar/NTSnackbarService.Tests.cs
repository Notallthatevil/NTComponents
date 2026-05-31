using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using NTComponents.Snackbar;

namespace NTComponents.Tests.Snackbar;

/// <summary>
///     Unit tests for <see cref="NTSnackbarService" />.
/// </summary>
public class NTSnackbarService_Tests : BunitContext {
    private bool _closeShouldFail;
    private object? _queuedOptions;

    public NTSnackbarService_Tests() {
        var module = JSInterop.SetupModule(NTSnackbar.JsModulePathValue);
        module.Setup<string>("queueSnackbar", invocation => {
            _queuedOptions = invocation.Arguments[0];
            return true;
        }).SetResult("queued");
        module.Setup<bool>("closeSnackbarFromBlazor", _ => !_closeShouldFail).SetResult(true);
        module.Setup<bool>("closeSnackbarFromBlazor", _ => _closeShouldFail).SetResult(false);
    }

    [Fact]
    public void Constructor_InitializesCorrectly() {
        // Arrange & Act
        var service = CreateService();

        // Assert
        service.Should().NotBeNull();
        service.Should().BeAssignableTo<INTSnackbarService>();
        service.ActiveSnackbar.Should().BeNull();
    }

    [Fact]
    public async Task ShowAsync_QueuesSnackbarThroughJavaScript_WithMaterial3Defaults() {
        // Arrange
        var service = CreateService();
        INTSnackbar? openedSnackbar = null;
        service.OnOpen += snackbar => {
            openedSnackbar = snackbar;
            return Task.CompletedTask;
        };

        // Act
        await service.ShowAsync("Saved");

        // Assert
        openedSnackbar.Should().NotBeNull();
        openedSnackbar!.Message.Should().Be("Saved");
        openedSnackbar.Timeout.Should().Be(4);
        openedSnackbar.ShowClose.Should().BeFalse();
        openedSnackbar.BackgroundColor.Should().Be(TnTColor.InverseSurface);
        openedSnackbar.TextColor.Should().Be(TnTColor.InverseOnSurface);
        openedSnackbar.ActionColor.Should().Be(TnTColor.InversePrimary);
        openedSnackbar.HasAction.Should().BeFalse();

        GetOption<string>("Message").Should().Be("Saved");
        GetOption<double>("Timeout").Should().Be(4);
        GetOption<bool>("ShowClose").Should().BeFalse();
        GetOption<string>("BackgroundColor").Should().Be("var(--tnt-color-inverse-surface)");
        GetOption<string>("TextColor").Should().Be("var(--tnt-color-inverse-on-surface)");
        GetOption<string>("ActionColor").Should().Be("var(--tnt-color-inverse-primary)");
        JSInterop.VerifyInvoke("import", 1);
        JSInterop.VerifyInvoke("queueSnackbar", 1);
    }

    [Fact]
    public async Task ShowAsync_WithAction_PassesDotNetCallbackOptions() {
        // Arrange
        var service = CreateService();

        // Act
        await service.ShowAsync("Email archived", "Undo", () => Task.CompletedTask);

        // Assert
        service.ActiveSnackbar.Should().NotBeNull();
        service.ActiveSnackbar!.HasAction.Should().BeTrue();
        GetOption<string>("ActionLabel").Should().Be("Undo");
        GetOption<bool>("ShowClose").Should().BeTrue();
        GetOption<double>("Timeout").Should().Be(0);
        GetOption<object>("DotNetReference").Should().NotBeNull();
        GetOption<string>("DotNetActionMethod").Should().Be(nameof(NTSnackbarService.InvokeActionFromJavaScript));
        GetOption<string>("DotNetCloseMethod").Should().Be(nameof(NTSnackbarService.NotifyClosedFromJavaScript));
    }

    [Fact]
    public async Task ShowAsync_HonorsExplicitOverrides_WhenActionIsProvided() {
        // Arrange
        var service = CreateService();

        // Act
        await service.ShowAsync("Saved", "Undo", () => Task.CompletedTask, timeout: 9, showClose: false, backgroundColor: TnTColor.Primary, textColor: TnTColor.OnPrimary, actionColor: TnTColor.Secondary);

        // Assert
        GetOption<double>("Timeout").Should().Be(9);
        GetOption<bool>("ShowClose").Should().BeFalse();
        GetOption<string>("BackgroundColor").Should().Be("var(--tnt-color-primary)");
        GetOption<string>("TextColor").Should().Be("var(--tnt-color-on-primary)");
        GetOption<string>("ActionColor").Should().Be("var(--tnt-color-secondary)");
    }

    [Fact]
    public async Task ShowAsync_Throws_WhenActionDefinitionIsIncomplete() {
        // Arrange
        var service = CreateService();

        // Act
        var labelOnly = () => service.ShowAsync("Saved", actionLabel: "Undo");
        var callbackOnly = () => service.ShowAsync("Saved", actionCallback: () => Task.CompletedTask);

        // Assert
        await labelOnly.Should().ThrowAsync<ArgumentException>();
        await callbackOnly.Should().ThrowAsync<ArgumentException>();
        JSInterop.Invocations.Should().NotContain(invocation => invocation.Identifier == "queueSnackbar");
    }

    [Fact]
    public async Task CloseAsync_ClosesSnackbarThroughJavaScript_AndRaisesClose() {
        // Arrange
        var service = CreateService();
        var closeCount = 0;
        service.OnClose += _ => {
            closeCount++;
            return Task.CompletedTask;
        };

        await service.ShowAsync("Saved");

        // Act
        await service.CloseAsync(service.ActiveSnackbar!);

        // Assert
        closeCount.Should().Be(1);
        service.ActiveSnackbar.Should().BeNull();
        JSInterop.VerifyInvoke("closeSnackbarFromBlazor", 1);
    }

    [Fact]
    public async Task CloseAsync_WhenJavaScriptDoesNotClose_KeepsSnackbarTracked() {
        // Arrange
        var service = CreateService();
        var closeCount = 0;
        service.OnClose += _ => {
            closeCount++;
            return Task.CompletedTask;
        };

        await service.ShowAsync("Saved");
        _closeShouldFail = true;

        // Act
        await service.CloseAsync(service.ActiveSnackbar!);

        // Assert
        closeCount.Should().Be(0);
        service.ActiveSnackbar.Should().NotBeNull();
        service.ActiveSnackbar!.Message.Should().Be("Saved");
        JSInterop.VerifyInvoke("closeSnackbarFromBlazor", 1);
    }

    [Fact]
    public async Task InvokeActionAsync_ExecutesCallback_AndClosesSnackbar() {
        // Arrange
        var service = CreateService();
        var callbackInvoked = false;
        var closeCount = 0;
        service.OnClose += _ => {
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
        JSInterop.VerifyInvoke("closeSnackbarFromBlazor", 1);
    }

    [Fact]
    public async Task InvokeActionFromJavaScript_ExecutesCallback_WithoutClosingImmediately() {
        // Arrange
        var service = CreateService();
        var callbackInvoked = false;
        await service.ShowAsync("Email archived", "Undo", () => {
            callbackInvoked = true;
            return Task.CompletedTask;
        });

        var snackbarId = ((NTSnackbarService.NTSnackbarImplementation)service.ActiveSnackbar!).Id;

        // Act
        await service.InvokeActionFromJavaScript(snackbarId);

        // Assert
        callbackInvoked.Should().BeTrue();
        service.ActiveSnackbar.Should().NotBeNull();
        JSInterop.Invocations.Count(invocation => invocation.Identifier == "closeSnackbarFromBlazor").Should().Be(0);
    }

    [Fact]
    public async Task NotifyClosedFromJavaScript_RemovesSnackbar_AndRaisesClose() {
        // Arrange
        var service = CreateService();
        var closeCount = 0;
        service.OnClose += _ => {
            closeCount++;
            return Task.CompletedTask;
        };

        await service.ShowAsync("Saved");
        var snackbarId = ((NTSnackbarService.NTSnackbarImplementation)service.ActiveSnackbar!).Id;

        // Act
        await service.NotifyClosedFromJavaScript(snackbarId);

        // Assert
        closeCount.Should().Be(1);
        service.ActiveSnackbar.Should().BeNull();
    }

    [Fact]
    public async Task InvokeActionFromJavaScript_WhenActionThrows_DoesNotCloseSnackbar() {
        // Arrange
        var service = CreateService();
        await service.ShowAsync("Email archived", "Undo", () => throw new InvalidOperationException("boom"));
        var snackbarId = ((NTSnackbarService.NTSnackbarImplementation)service.ActiveSnackbar!).Id;

        // Act
        var action = () => service.InvokeActionFromJavaScript(snackbarId);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>();
        service.ActiveSnackbar.Should().NotBeNull();
    }

    [Fact]
    public async Task CloseAsync_UntrackedSnackbar_DoesNothing() {
        // Arrange
        var service = CreateService();
        await service.ShowAsync("Tracked");

        // Act
        await service.CloseAsync(new NTSnackbarService.NTSnackbarImplementation { Id = "missing", Message = "Not tracked" });

        // Assert
        service.ActiveSnackbar.Should().NotBeNull();
        service.ActiveSnackbar!.Message.Should().Be("Tracked");
        JSInterop.Invocations.Count(invocation => invocation.Identifier == "closeSnackbarFromBlazor").Should().Be(0);
    }

    [Fact]
    public void NTSnackbarImplementation_HasExpectedDefaults() {
        // Arrange & Act
        var snackbar = new NTSnackbarService.NTSnackbarImplementation();

        // Assert
        snackbar.Message.Should().BeNullOrEmpty();
        snackbar.Timeout.Should().Be(4);
        snackbar.ShowClose.Should().BeFalse();
        snackbar.BackgroundColor.Should().Be(TnTColor.InverseSurface);
        snackbar.TextColor.Should().Be(TnTColor.InverseOnSurface);
        snackbar.ActionColor.Should().Be(TnTColor.InversePrimary);
        snackbar.HasAction.Should().BeFalse();
        snackbar.Closing.Should().BeFalse();
    }

    private NTSnackbarService CreateService() {
        return new NTSnackbarService(Services.GetRequiredService<IJSRuntime>());
    }

    private T? GetOption<T>(string propertyName) {
        _queuedOptions.Should().NotBeNull();
        return (T?)_queuedOptions!.GetType().GetProperty(propertyName)!.GetValue(_queuedOptions);
    }
}
