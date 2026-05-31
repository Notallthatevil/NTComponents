using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using NTComponents.Core;
using NTComponents.Toast;

namespace NTComponents.Tests.Toast;

/// <summary>
///     Unit tests for <see cref="NTToastService" />.
/// </summary>
public class NTToastService_Tests : BunitContext {
    private bool _closeShouldFail;
    private object? _queuedOptions;

    public NTToastService_Tests() {
        var module = JSInterop.SetupModule(NTToast.JsModulePathValue);
        module.Setup<string>("queueToast", invocation => {
            _queuedOptions = invocation.Arguments[0];
            return true;
        }).SetResult("queued");
        module.SetupVoid("clearToastsFromBlazor", _ => true).SetVoidResult();
        module.Setup<bool>("closeToastFromBlazor", _ => !_closeShouldFail).SetResult(true);
        module.Setup<bool>("closeToastFromBlazor", _ => _closeShouldFail).SetResult(false);
    }

    [Fact]
    public void Constructor_InitializesCorrectly() {
        // Arrange & Act
        var service = CreateService();

        // Assert
        service.Should().NotBeNull();
        service.Should().BeAssignableTo<INTToastService>();
        service.ActiveToasts.Should().BeEmpty();
    }

    [Fact]
    public void AddTnTServices_RegistersToastService() {
        // Arrange & Act
        Services.AddTnTServices();

        // Assert
        Services.GetRequiredService<INTToastService>().Should().BeAssignableTo<INTToastService>();
    }

    [Fact]
    public async Task ShowAsync_QueuesToastThroughJavaScript_WithMaterialDefaults() {
        // Arrange
        var service = CreateService();
        INTToast? openedToast = null;
        service.OnOpen += toast => {
            openedToast = toast;
            return Task.CompletedTask;
        };

        // Act
        await service.ShowAsync("Saved", "Changes were stored.");

        // Assert
        openedToast.Should().NotBeNull();
        openedToast!.Title.Should().Be("Saved");
        openedToast.Message.Should().Be("Changes were stored.");
        openedToast.Timeout.Should().Be(4);
        openedToast.ShowClose.Should().BeTrue();
        openedToast.BackgroundColor.Should().Be(TnTColor.SurfaceContainerHigh);
        openedToast.TextColor.Should().Be(TnTColor.OnSurface);
        openedToast.IconColor.Should().Be(TnTColor.Primary);
        openedToast.Variant.Should().Be(NTToastVariant.Default);

        GetOption<string>("Title").Should().Be("Saved");
        GetOption<string>("Message").Should().Be("Changes were stored.");
        GetOption<double>("Timeout").Should().Be(4);
        GetOption<bool>("ShowClose").Should().BeTrue();
        GetOption<string>("BackgroundColor").Should().BeNull();
        GetOption<string>("TextColor").Should().BeNull();
        GetOption<string>("IconColor").Should().BeNull();
        GetOption<string>("Variant").Should().Be("default");
        GetOption<object>("DotNetReference").Should().NotBeNull();
        GetOption<string>("DotNetCloseMethod").Should().Be(nameof(NTToastService.NotifyClosedFromJavaScript));
        JSInterop.VerifyInvoke("import", 1);
        JSInterop.VerifyInvoke("queueToast", 1);
    }

    [Fact]
    public async Task ShowAsync_HonorsExplicitOverrides() {
        // Arrange
        var service = CreateService();

        // Act
        await service.ShowAsync("Custom", "Overrides", NTToastVariant.Warning, timeout: 9, showClose: false, icon: "star", backgroundColor: TnTColor.Primary, textColor: TnTColor.OnPrimary, iconColor: TnTColor.Secondary);

        // Assert
        GetOption<double>("Timeout").Should().Be(9);
        GetOption<bool>("ShowClose").Should().BeFalse();
        GetOption<string>("Icon").Should().Be("star");
        GetOption<string>("BackgroundColor").Should().Be("var(--tnt-color-primary)");
        GetOption<string>("TextColor").Should().Be("var(--tnt-color-on-primary)");
        GetOption<string>("IconColor").Should().Be("var(--tnt-color-secondary)");
        GetOption<string>("Variant").Should().Be("warning");
    }

    [Theory]
    [InlineData(NTToastVariant.Success, "var(--tnt-color-success-container)", "var(--tnt-color-on-success-container)", "var(--tnt-color-success)")]
    [InlineData(NTToastVariant.Info, "var(--tnt-color-info-container)", "var(--tnt-color-on-info-container)", "var(--tnt-color-info)")]
    [InlineData(NTToastVariant.Warning, "var(--tnt-color-warning-container)", "var(--tnt-color-on-warning-container)", "var(--tnt-color-warning)")]
    [InlineData(NTToastVariant.Error, "var(--tnt-color-error-container)", "var(--tnt-color-on-error-container)", "var(--tnt-color-error)")]
    [InlineData(NTToastVariant.Assert, "var(--tnt-color-assert-container)", "var(--tnt-color-on-assert-container)", "var(--tnt-color-assert)")]
    public async Task ShowAsync_UsesVariantColors(NTToastVariant variant, string backgroundColor, string textColor, string iconColor) {
        // Arrange
        var service = CreateService();

        // Act
        await service.ShowAsync("Variant", variant: variant);

        // Assert
        service.ActiveToasts[^1].BackgroundColor.ToCssTnTColorVariable().Should().Be(backgroundColor);
        service.ActiveToasts[^1].TextColor.ToCssTnTColorVariable().Should().Be(textColor);
        service.ActiveToasts[^1].IconColor.ToCssTnTColorVariable().Should().Be(iconColor);
        GetOption<string>("BackgroundColor").Should().BeNull();
        GetOption<string>("TextColor").Should().BeNull();
        GetOption<string>("IconColor").Should().BeNull();
        GetOption<string>("Variant").Should().Be(variant.ToString().ToLowerInvariant());
    }

    [Fact]
    public async Task ExtensionMethods_ShowSemanticVariants() {
        // Arrange
        var service = CreateService();

        // Act
        await service.ShowSuccessAsync("Success");
        await service.ShowInfoAsync("Info");
        await service.ShowWarningAsync("Warning");
        await service.ShowErrorAsync("Error");
        await service.ShowAssertAsync("Assert");

        // Assert
        service.ActiveToasts.Select(toast => toast.Variant).Should().ContainInOrder(
            NTToastVariant.Success,
            NTToastVariant.Info,
            NTToastVariant.Warning,
            NTToastVariant.Error,
            NTToastVariant.Assert);
        JSInterop.Invocations.Count(invocation => invocation.Identifier == "queueToast").Should().Be(5);
    }

    [Fact]
    public async Task CloseAsync_ClosesToastThroughJavaScript_AndRaisesClose() {
        // Arrange
        var service = CreateService();
        var closeCount = 0;
        service.OnClose += _ => {
            closeCount++;
            return Task.CompletedTask;
        };

        await service.ShowAsync("Saved");

        // Act
        await service.CloseAsync(service.ActiveToasts[0]);

        // Assert
        closeCount.Should().Be(1);
        service.ActiveToasts.Should().BeEmpty();
        JSInterop.VerifyInvoke("closeToastFromBlazor", 1);
    }

    [Fact]
    public async Task CloseAsync_WhenJavaScriptDoesNotClose_KeepsToastTracked() {
        // Arrange
        var service = CreateService();
        await service.ShowAsync("Saved");
        _closeShouldFail = true;

        // Act
        await service.CloseAsync(service.ActiveToasts[0]);

        // Assert
        service.ActiveToasts.Should().ContainSingle();
        JSInterop.VerifyInvoke("closeToastFromBlazor", 1);
    }

    [Fact]
    public async Task NotifyClosedFromJavaScript_RemovesToast_AndRaisesClose() {
        // Arrange
        var service = CreateService();
        var closeCount = 0;
        service.OnClose += _ => {
            closeCount++;
            return Task.CompletedTask;
        };

        await service.ShowAsync("Saved");
        var toastId = ((NTToastService.NTToastImplementation)service.ActiveToasts[0]).Id;

        // Act
        await service.NotifyClosedFromJavaScript(toastId);

        // Assert
        closeCount.Should().Be(1);
        service.ActiveToasts.Should().BeEmpty();
    }

    [Fact]
    public async Task ShowAsync_WhenJavaScriptInteropIsUnavailable_ThrowsActionableExceptionAndUntracksToast() {
        // Arrange
        var service = new NTToastService(new UnavailableJavaScriptRuntime());

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ShowAsync("Prerender"));

        // Assert
        exception.Message.Should().Contain("interactive Blazor render context");
        exception.Message.Should().Contain("window.NTToast?.queueToast");
        service.ActiveToasts.Should().BeEmpty();
    }

    [Fact]
    public async Task DisposeAsync_ClearsTrackedJavaScriptToasts_WithoutCloseCallbacks() {
        // Arrange
        var service = CreateService();
        var closeCount = 0;
        service.OnClose += _ => {
            closeCount++;
            return Task.CompletedTask;
        };
        await service.ShowAsync("Saved");

        // Act
        await service.DisposeAsync();

        // Assert
        closeCount.Should().Be(0);
        service.ActiveToasts.Should().BeEmpty();
        JSInterop.VerifyInvoke("clearToastsFromBlazor", 1);
    }

    private NTToastService CreateService() {
        return new NTToastService(Services.GetRequiredService<IJSRuntime>());
    }

    private T? GetOption<T>(string propertyName) {
        _queuedOptions.Should().NotBeNull();
        return (T?)_queuedOptions!.GetType().GetProperty(propertyName)!.GetValue(_queuedOptions);
    }

    private sealed class UnavailableJavaScriptRuntime : IJSRuntime {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) {
            throw new InvalidOperationException("JavaScript interop calls cannot be issued during server-side static rendering.");
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) {
            throw new InvalidOperationException("JavaScript interop calls cannot be issued during server-side static rendering.");
        }
    }
}
