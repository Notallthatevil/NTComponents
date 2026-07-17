using Microsoft.AspNetCore.Components;
using NTComponents.Core;

namespace NTComponents.Tests.Core;

public class NTPageScriptComponentTests : BunitContext {

    [Fact]
    public void Constructor_CreatesDotNetObjectRef() {
        var comp = new TestPageScriptComponent();
        comp.DotNetObjectRef.Should().NotBeNull();
        comp.DotNetObjectRef!.Value.Should().Be(comp);
    }

    [Fact]
    public void Constructor_ThrowsIfTypeMismatch() {
        var ex = Assert.Throws<InvalidCastException>(() => new InvalidDerived());
        ex.Message.Should().Contain("NTPageScriptComponent: TDerived must match the actual derived class type");
    }

    [Fact]
    public void Dispose_SetsDotNetObjectRefToNull() {
        var comp = new TestPageScriptComponent();
        comp.Dispose();
        comp.DotNetObjectRef.Should().BeNull();
    }

    [Fact]
    public async Task DisposeAsync_DisposesModuleAndDotNetObjectRef() {
        // Arrange: render so module loads
        var module = JSInterop.SetupModule("./test.js");
        module.SetupVoid("onLoad", _ => true).SetVoidResult();
        module.SetupVoid("onUpdate", _ => true).SetVoidResult();
        module.SetupVoid("onDispose", _ => true).SetVoidResult();

        var rendered = Render<TestPageScriptComponent>();
        var instance = rendered.Instance;
        instance.IsolatedJsModule.Should().NotBeNull();
        instance.DotNetObjectRef.Should().NotBeNull();

        // Act
        await instance.DisposeAsync();

        // Assert
        instance.IsolatedJsModule.Should().BeNull();
        instance.DotNetObjectRef.Should().BeNull();
    }

    [Fact]
    public void FirstRender_ImportsModule_InvokesOnLoadAndOnUpdate() {
        // Arrange JS interop expectations
        var module = JSInterop.SetupModule("./test.js");
        module.SetupVoid("onLoad", _ => true).SetVoidResult();
        module.SetupVoid("onUpdate", _ => true).SetVoidResult();

        // Act - render component (triggers first render + OnAfterRenderAsync)
        var rendered = Render<TestPageScriptComponent>();

        // Assert
        rendered.Instance.IsolatedJsModule.Should().NotBeNull();
        rendered.Instance.DotNetObjectRef.Should().NotBeNull();
    }

    [Fact]
    public void SubsequentRender_OnlyInvokesOnUpdate() {
        var module = JSInterop.SetupModule("./test.js");
        // First render expectations
        module.SetupVoid("onLoad", _ => true).SetVoidResult();
        module.SetupVoid("onUpdate", _ => true).SetVoidResult();
        var rendered = Render<TestPageScriptComponent>();

        // Second render expectation (only onUpdate)
        module.SetupVoid("onUpdate", _ => true).SetVoidResult();
        rendered.Render(); // trigger re-render

        rendered.Instance.IsolatedJsModule.Should().NotBeNull();
        rendered.Instance.DotNetObjectRef.Should().NotBeNull();
    }

    private class InvalidDerived : NTPageScriptComponent<TestPageScriptComponent> {
        public override string? ElementClass => null;
        public override string? ElementStyle => null;
        public override string? JsModulePath => null;
    }

    private class TestPageScriptComponent : NTPageScriptComponent<TestPageScriptComponent> {
        public override string? ElementClass => null;
        public override string? ElementStyle => null;
        public override string? JsModulePath => "./test.js";
        public ElementReference TestElement => Element;
    }
}