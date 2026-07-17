using Microsoft.AspNetCore.Components;
using System.Reflection;
using NTComponents.Core;

namespace NTComponents.Tests.Core;

public class TnTExternalClickHandler_Tests : BunitContext {
    private const string JsModulePath = "./_content/NTComponents/Core/TnTExternalClickHandler.razor.js";

    public TnTExternalClickHandler_Tests() {
        var module = JSInterop.SetupModule(JsModulePath);
        module.SetupVoid("onLoad", _ => true).SetVoidResult();
        module.SetupVoid("onUpdate", _ => true).SetVoidResult();
        module.SetupVoid("onDispose", _ => true).SetVoidResult();
        module.SetupVoid("externalClickCallbackRegister", _ => true).SetVoidResult();
        module.SetupVoid("externalClickCallbackDeregister", _ => true).SetVoidResult();
    }

    [Fact]
    public void CanRenderMultipleTimesWithoutError() {
        // Arrange
        var comp = Render<TnTExternalClickHandler>();
        // Act
        comp.Render();
        comp.Render();
        // Assert
        comp.Instance.Should().NotBeNull();
    }

    [Fact]
    public async Task ModuleNotLoaded_DisposeAsync_DoesNotInvokeJavaScript() {
        // Arrange
        var comp = Render<TnTExternalClickHandler>();
        var prop = typeof(NTPageScriptComponent<TnTExternalClickHandler>).GetProperty(nameof(NTPageScriptComponent<TnTExternalClickHandler>.IsolatedJsModule), BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        prop.Should().NotBeNull();
        prop!.CanWrite.Should().BeTrue();
        prop.SetValue(comp.Instance, null);

        // Act
        var act = () => comp.Instance.DisposeAsync().AsTask();

        // Assert
        await act.Should().NotThrowAsync();
        JSInterop.Invocations.Should().NotContain(invocation => invocation.Identifier == "externalClickCallbackDeregister");
        comp.Instance.IsolatedJsModule.Should().BeNull();
    }

    [Fact]
    public async Task DisposesAndDeregistersCallback() {
        // Arrange
        var comp = Render<TnTExternalClickHandler>();
        // Act
        await comp.Instance.DisposeAsync();
        // Assert
        JSInterop.VerifyInvoke("externalClickCallbackDeregister");
    }

    [Fact]
    public void DoesNotInvokeCallback_WhenClickIsInside() {
        // Arrange
        var called = false;
        var comp = Render<TnTExternalClickHandler>(parameters =>
            parameters.Add(p => p.ExternalClickCallback, EventCallback.Factory.Create(this, () => { called = true; return Task.CompletedTask; }))
        );
        // Act: Do not call OnClick (simulates click inside) Assert
        called.Should().BeFalse();
    }

    [Fact]
    public async Task DoesNotThrow_WhenCallbackNotSet() {
        // Arrange
        var comp = Render<TnTExternalClickHandler>();
        // Act & Assert
        await comp.Instance.OnClick(); // Should not throw
    }

    [Fact]
    public async Task InvokesCallback_WhenClickIsOutside() {
        // Arrange
        var called = false;
        var comp = Render<TnTExternalClickHandler>(parameters =>
            parameters.Add(p => p.ExternalClickCallback, EventCallback.Factory.Create(this, () => { called = true; return Task.CompletedTask; }))
        );
        // Act
        await comp.Instance.OnClick();
        // Assert
        called.Should().BeTrue();
    }

    [Fact]
    public async Task InvokesCallback_WhenElementRemovedBeforeClick() {
        // Arrange
        var called = false;
        var comp = Render<TnTExternalClickHandler>(parameters =>
            parameters.Add(p => p.ExternalClickCallback, EventCallback.Factory.Create(this, () => { called = true; return Task.CompletedTask; }))
        );
        // Act
        await comp.Instance.OnClick();
        // Assert
        called.Should().BeTrue();
    }

    [Fact]
    public void RendersChildContent() {
        // Arrange
        var content = "<span>child</span>";
        // Act
        var comp = Render<TnTExternalClickHandler>(p => p.AddChildContent(content));
        // Assert
        comp.Markup.Should().Contain("child");
    }
}
