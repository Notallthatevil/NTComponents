using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using NTComponents.Storage;

namespace NTComponents.Tests.Storage;

public class StorageService_Resilience_Tests : BunitContext {
    private const string StorageUnavailableMessage = "Unable to access the browser storage. This is most likely due to the browser settings.";
    private readonly ILocalStorageService _storageService;
    private readonly ISessionStorageService _sessionStorageService;

    public StorageService_Resilience_Tests() {
        Services.AddSingleton<ILocalStorageService>(provider => new LocalStorageService(provider.GetRequiredService<IJSRuntime>()));
        Services.AddSingleton<ISessionStorageService>(provider => new SessionStorageService(provider.GetRequiredService<IJSRuntime>()));
        _storageService = Services.GetRequiredService<ILocalStorageService>();
        _sessionStorageService = Services.GetRequiredService<ISessionStorageService>();
    }

    [Theory]
    [InlineData(StorageOperation.Clear)]
    [InlineData(StorageOperation.ContainKey)]
    [InlineData(StorageOperation.GetItem)]
    [InlineData(StorageOperation.Key)]
    [InlineData(StorageOperation.Keys)]
    [InlineData(StorageOperation.Length)]
    [InlineData(StorageOperation.RemoveItem)]
    [InlineData(StorageOperation.RemoveItems)]
    [InlineData(StorageOperation.SetItemAsString)]
    [InlineData(StorageOperation.SetItem)]
    public async Task Operation_WhenBrowserStorageIsDisabled_ThrowsSpecificExceptionWithOriginalCause(StorageOperation operation) {
        var jsException = new JSException("Failed to read the 'LocalStorage' property from 'Window'");
        SetupOperationFailure(operation, jsException);

        var exception = await Assert.ThrowsAsync<BrowserStorageDisabledException>(() => InvokeOperationAsync(operation));

        exception.Message.Should().Be(StorageUnavailableMessage);
        exception.InnerException.Should().BeSameAs(jsException);
    }

    [Theory]
    [InlineData(StorageOperation.Clear)]
    [InlineData(StorageOperation.ContainKey)]
    [InlineData(StorageOperation.GetItem)]
    [InlineData(StorageOperation.Key)]
    [InlineData(StorageOperation.Keys)]
    [InlineData(StorageOperation.Length)]
    [InlineData(StorageOperation.RemoveItem)]
    [InlineData(StorageOperation.RemoveItems)]
    public async Task Operation_WhenJavaScriptFailsForAnotherReason_PreservesOriginalException(StorageOperation operation) {
        var jsException = new JSException("JavaScript dependency failed");
        SetupOperationFailure(operation, jsException);

        var exception = await Assert.ThrowsAsync<JSException>(() => InvokeOperationAsync(operation));

        exception.Should().BeSameAs(jsException);
    }

    [Fact]
    public async Task SessionStorageOperation_WhenBrowserStorageIsDisabled_ThrowsSpecificExceptionWithOriginalCause() {
        var jsException = new JSException("Failed to read the 'SessionStorage' property from 'Window'");
        JSInterop.SetupVoid("sessionStorage.clear").SetException(jsException);

        var exception = await Assert.ThrowsAsync<BrowserStorageDisabledException>(() => _sessionStorageService.ClearAsync(Xunit.TestContext.Current.CancellationToken).AsTask());

        exception.Message.Should().Be(StorageUnavailableMessage);
        exception.InnerException.Should().BeSameAs(jsException);
    }

    [Fact]
    public async Task GetItemAsync_WhenStoredValueIsMalformedForRequestedType_PreservesJsonException() {
        JSInterop.Setup<string>("localStorage.getItem", "key").SetResult("not-an-integer");

        var exception = await Assert.ThrowsAsync<System.Text.Json.JsonException>(() => _storageService.GetItemAsync<int>("key", cancellationToken: Xunit.TestContext.Current.CancellationToken).AsTask());

        exception.Path.Should().Be("$");
    }

    [Theory]
    [InlineData(StorageOperation.SetItemAsString)]
    [InlineData(StorageOperation.SetItem)]
    public async Task SetOperation_WhenJavaScriptWriteFails_DoesNotRaiseChanged(StorageOperation operation) {
        var changedCount = 0;
        var jsException = new JSException("JavaScript dependency failed");
        _storageService.Changed += (_, _) => changedCount++;
        SetupOperationFailure(operation, jsException);

        var exception = await Assert.ThrowsAsync<JSException>(() => InvokeOperationAsync(operation));

        exception.Should().BeSameAs(jsException);
        changedCount.Should().Be(0);
    }

    [Fact]
    public async Task RemoveItemsAsync_WhenFirstRemovalFails_DoesNotContinueMutatingStorage() {
        var jsException = new JSException("JavaScript dependency failed");
        JSInterop.SetupVoid("localStorage.removeItem", "key").SetException(jsException);

        var exception = await Assert.ThrowsAsync<JSException>(() => _storageService.RemoveItemsAsync(["key", "later-key"], Xunit.TestContext.Current.CancellationToken).AsTask());

        exception.Should().BeSameAs(jsException);
        JSInterop.Invocations["localStorage.removeItem"].Should().ContainSingle()
            .Which.Arguments.Should().ContainSingle().Which.Should().Be("key");
    }

    [Fact]
    public void GetStorageType_WhenValueIsInvalid_ThrowsWithInvalidValue() {
        const StorageType invalidStorageType = (StorageType)int.MaxValue;

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => invalidStorageType.GetStorageType());

        exception.ParamName.Should().Be("storageType");
        exception.ActualValue.Should().Be(invalidStorageType);
    }

    private void SetupOperationFailure(StorageOperation operation, Exception exception) {
        switch (operation) {
            case StorageOperation.Clear:
                JSInterop.SetupVoid("localStorage.clear").SetException(exception);
                break;
            case StorageOperation.ContainKey:
                JSInterop.Setup<bool>("localStorage.hasOwnProperty", "key").SetException(exception);
                break;
            case StorageOperation.GetItem:
                JSInterop.Setup<string>("localStorage.getItem", "key").SetException(exception);
                break;
            case StorageOperation.Key:
                JSInterop.Setup<string>("localStorage.key", 0).SetException(exception);
                break;
            case StorageOperation.Keys:
                JSInterop.Setup<IEnumerable<string>>("eval", "Object.keys(localStorage)").SetException(exception);
                break;
            case StorageOperation.Length:
                JSInterop.Setup<int>("eval", "localStorage.length").SetException(exception);
                break;
            case StorageOperation.RemoveItem:
                JSInterop.SetupVoid("localStorage.removeItem", "key").SetException(exception);
                break;
            case StorageOperation.RemoveItems:
                JSInterop.SetupVoid("localStorage.removeItem", "key").SetException(exception);
                break;
            case StorageOperation.SetItemAsString:
                JSInterop.Setup<string>("localStorage.getItem", "key").SetResult("\"old-value\"");
                JSInterop.SetupVoid("localStorage.setItem", "key", "new-value").SetException(exception);
                break;
            case StorageOperation.SetItem:
                JSInterop.Setup<string>("localStorage.getItem", "key").SetResult("1");
                JSInterop.SetupVoid("localStorage.setItem", "key", "2").SetException(exception);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(operation), operation, null);
        }
    }

    private Task InvokeOperationAsync(StorageOperation operation) {
        return operation switch {
            StorageOperation.Clear => _storageService.ClearAsync(Xunit.TestContext.Current.CancellationToken).AsTask(),
            StorageOperation.ContainKey => _storageService.ContainKeyAsync("key", Xunit.TestContext.Current.CancellationToken).AsTask(),
            StorageOperation.GetItem => _storageService.GetItemAsync<string>("key", cancellationToken: Xunit.TestContext.Current.CancellationToken).AsTask(),
            StorageOperation.Key => _storageService.KeyAsync(0, Xunit.TestContext.Current.CancellationToken).AsTask(),
            StorageOperation.Keys => _storageService.KeysAsync(Xunit.TestContext.Current.CancellationToken).AsTask(),
            StorageOperation.Length => _storageService.LengthAsync(Xunit.TestContext.Current.CancellationToken).AsTask(),
            StorageOperation.RemoveItem => _storageService.RemoveItemAsync("key", Xunit.TestContext.Current.CancellationToken).AsTask(),
            StorageOperation.RemoveItems => _storageService.RemoveItemsAsync(["key"], Xunit.TestContext.Current.CancellationToken).AsTask(),
            StorageOperation.SetItemAsString => _storageService.SetItemAsStringAsync("key", "new-value", Xunit.TestContext.Current.CancellationToken).AsTask(),
            StorageOperation.SetItem => _storageService.SetItemAsync("key", 2, cancellationToken: Xunit.TestContext.Current.CancellationToken).AsTask(),
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
        };
    }

    public enum StorageOperation {
        Clear,
        ContainKey,
        GetItem,
        Key,
        Keys,
        Length,
        RemoveItem,
        RemoveItems,
        SetItemAsString,
        SetItem
    }
}
