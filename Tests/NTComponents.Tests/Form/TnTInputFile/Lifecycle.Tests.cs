using Bunit.JSInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace NTComponents.Tests.Form.TnTInputFile;

public class Lifecycle_Tests : BunitContext {
    private const string JsModulePath = "./_content/NTComponents/Form/TnTInputFile.razor.js";

    public Lifecycle_Tests() => SetRendererInfo(new RendererInfo("WebAssembly", true));

    [Fact]
    public void FirstRender_LoadsAndInitializesDropZoneThenUpdates() {
        var module = SetupModule();
        module.SetupModule("initializeFileDropZone", _ => true).SetupVoid("dispose", _ => true).SetVoidResult();

        var cut = Render<global::NTComponents.TnTInputFile>();

        cut.Instance.IsolatedJsModule.Should().NotBeNull();
        cut.Instance.DotNetObjectRef.Should().NotBeNull();
        JSInterop.VerifyInvoke("onLoad", 1);
        JSInterop.VerifyInvoke("initializeFileDropZone", 1);
        JSInterop.VerifyInvoke("onUpdate", 1);
    }

    [Fact]
    public void SubsequentRender_UpdatesWithoutReinitializingDropZone() {
        var module = SetupModule();
        module.SetupModule("initializeFileDropZone", _ => true).SetupVoid("dispose", _ => true).SetVoidResult();
        var cut = Render<global::NTComponents.TnTInputFile>();

        cut.Render();

        JSInterop.VerifyInvoke("onLoad", 1);
        JSInterop.VerifyInvoke("initializeFileDropZone", 1);
        JSInterop.VerifyInvoke("onUpdate", 2);
    }

    [Fact]
    public void FirstRender_WhenJavaScriptDisconnects_DoesNotFailRenderingOrContinueInitialization() {
        var module = JSInterop.SetupModule(JsModulePath);
        module.SetupVoid("onLoad", _ => true).SetException(new JSDisconnectedException("Disconnected"));

        var cut = Render<global::NTComponents.TnTInputFile>();

        cut.Instance.IsolatedJsModule.Should().NotBeNull();
        JSInterop.VerifyInvoke("onLoad", 1);
        JSInterop.VerifyNotInvoke("initializeFileDropZone");
        JSInterop.VerifyNotInvoke("onUpdate");
    }

    [Fact]
    public async Task DisposeAsync_DisposesDropZoneAndModuleThenClearsJavaScriptReferences() {
        var module = SetupModule();
        module.SetupModule("initializeFileDropZone", _ => true).SetupVoid("dispose", _ => true).SetVoidResult();
        var cut = Render<global::NTComponents.TnTInputFile>();

        await cut.Instance.DisposeAsync();

        JSInterop.VerifyInvoke("dispose", 1);
        JSInterop.VerifyInvoke("onDispose", 1);
        cut.Instance.IsolatedJsModule.Should().BeNull();
        cut.Instance.Dispose();
        cut.Instance.DotNetObjectRef.Should().BeNull();
    }

    [Fact]
    public async Task DisposeAsync_WhenJavaScriptDisconnects_StillClearsJavaScriptReferences() {
        var module = SetupModule();
        module.SetupModule("initializeFileDropZone", _ => true).SetupVoid("dispose", _ => true).SetVoidResult();
        module.SetupVoid("onDispose", _ => true).SetException(new JSDisconnectedException("Disconnected"));
        var cut = Render<global::NTComponents.TnTInputFile>();

        await cut.Instance.DisposeAsync();

        JSInterop.VerifyInvoke("dispose", 1);
        JSInterop.VerifyInvoke("onDispose", 1);
        cut.Instance.IsolatedJsModule.Should().BeNull();
        cut.Instance.Dispose();
        cut.Instance.DotNetObjectRef.Should().BeNull();
    }

    [Fact]
    public void Dispose_ReleasesDotNetObjectReference() {
        var input = new global::NTComponents.TnTInputFile();
        input.DotNetObjectRef.Should().NotBeNull();

        input.Dispose();

        input.DotNetObjectRef.Should().BeNull();
    }

    private BunitJSModuleInterop SetupModule() {
        var module = JSInterop.SetupModule(JsModulePath);
        module.SetupVoid("onLoad", _ => true).SetVoidResult();
        module.SetupVoid("onUpdate", _ => true).SetVoidResult();
        module.SetupVoid("onDispose", _ => true).SetVoidResult();
        return module;
    }
}
