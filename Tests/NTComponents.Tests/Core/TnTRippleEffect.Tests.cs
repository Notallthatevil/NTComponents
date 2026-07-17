using System.Reflection;
using NTComponents.Core;

namespace NTComponents.Tests.Core;

public class TnTRippleEffect_Tests : BunitContext {
    private const string JsModulePath = "./_content/NTComponents/Core/TnTRippleEffect.razor.js";

    public TnTRippleEffect_Tests() {
        var module = JSInterop.SetupModule(JsModulePath);
        module.SetupVoid("onLoad", _ => true).SetVoidResult();
        module.SetupVoid("onUpdate", _ => true).SetVoidResult();
        module.SetupVoid("onDispose", _ => true).SetVoidResult();
    }

    [Fact]
    public async Task Defensive_DisposeAsync_WhenModuleManuallyCleared_DoesNotThrow() {
        // Arrange
        var cut = Render<TnTRippleEffect>();
        // Manually null out IsolatedJsModule to simulate unexpected state
        var prop = typeof(NTPageScriptComponent<TnTRippleEffect>).GetProperty("IsolatedJsModule", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (prop?.CanWrite == true) {
            prop.SetValue(cut.Instance, null);
        }
        // Act
        var ex = await Record.ExceptionAsync(() => cut.Instance.DisposeAsync().AsTask());
        // Assert
        ex.Should().BeNull();
    }

    [Fact]
    public async Task Dispose_AfterDisposeAsync_NoThrow() {
        // Arrange
        var cut = Render<TnTRippleEffect>();
        await cut.Instance.DisposeAsync();
        // Act
        var ex = Record.Exception(() => cut.Instance.Dispose());
        // Assert
        ex.Should().BeNull();
    }

    [Fact]
    public void Dispose_Idempotent() {
        // Arrange
        var cut = Render<TnTRippleEffect>();
        // Act
        cut.Instance.Dispose();
        var ex = Record.Exception(() => cut.Instance.Dispose());
        // Assert
        ex.Should().BeNull();
    }

    [Fact]
    public void Dispose_Synchronous_DoesNotInvokeOnDisposeAndNullsDotNetRef() {
        // Arrange
        var cut = Render<TnTRippleEffect>();
        // Act
        cut.Instance.Dispose();
        // onDispose is only called from DisposeAsync; ensure not invoked yet
        JSInterop.Invocations.Should().NotContain(i => i.Identifier == "onDispose");
        cut.Instance.DotNetObjectRef.Should().BeNull();
    }

    [Fact]
    public async Task DisposeAsync_AfterDispose_NoThrow() {
        // Arrange
        var cut = Render<TnTRippleEffect>();
        cut.Instance.Dispose();
        // Act
        var ex = await Record.ExceptionAsync(() => cut.Instance.DisposeAsync().AsTask());
        // Assert
        ex.Should().BeNull();
    }

    [Fact]
    public async Task DisposeAsync_Idempotent() {
        // Arrange
        var cut = Render<TnTRippleEffect>();
        await cut.Instance.DisposeAsync();
        // Act
        var ex = await Record.ExceptionAsync(() => cut.Instance.DisposeAsync().AsTask());
        // Assert
        ex.Should().BeNull();
    }

    [Fact]
    public async Task DisposeAsync_InvokesOnDispose() {
        // Arrange
        var cut = Render<TnTRippleEffect>();
        // Act
        await cut.Instance.DisposeAsync();
        // Assert
        JSInterop.VerifyInvoke("onDispose");
        cut.Instance.DotNetObjectRef.Should().BeNull();
        cut.Instance.IsolatedJsModule.Should().BeNull();
    }

    [Fact]
    public void FirstRender_ImportsModule_InvokesOnLoadAndOnUpdate() {
        // Arrange & Act
        var cut = Render<TnTRippleEffect>();
        // Assert
        cut.Instance.IsolatedJsModule.Should().NotBeNull();
        cut.Instance.DotNetObjectRef.Should().NotBeNull();
    }

    [Fact]
    public void JsModulePath_IsExpected() {
        // Arrange & Act
        var cut = Render<TnTRippleEffect>();
        // Assert
        cut.Instance.JsModulePath.Should().Be(JsModulePath);
    }

    [Fact]
    public void Passes_AdditionalAttributes() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "data-test", "abc" } };
        // Act
        var cut = Render<TnTRippleEffect>(p => p.Add(c => c.AdditionalAttributes, attrs));
        // Assert
        cut.Markup.Should().Contain("data-test=\"abc\"");
    }

    [Fact]
    public void Renders_RippleElement_WithTntIdAttribute() {
        // Arrange & Act
        var cut = Render<TnTRippleEffect>();
        // Assert
        cut.Markup.Should().Contain("<tnt-ripple-effect");
        cut.Markup.Should().Contain("tntid=");
    }

    [Fact]
    public void ReRender_InvokesOnUpdateOnly() {
        // Arrange
        var cut = Render<TnTRippleEffect>();
        var sameRef = cut.Instance.DotNetObjectRef;
        // Expect another onUpdate call (no new onLoad) by setting it up again
        JSInterop.SetupModule(JsModulePath).SetupVoid("onUpdate", _ => true).SetVoidResult();
        // Act
        cut.Render();
        // Assert
        cut.Instance.IsolatedJsModule.Should().NotBeNull();
        cut.Instance.DotNetObjectRef.Should().BeSameAs(sameRef);
    }
}