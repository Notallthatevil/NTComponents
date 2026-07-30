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
    public async Task Disposal_While_OnLoad_Is_Awaiting_Does_Not_Initialize_The_DropZone() {
        var module = SetupModule();
        module.SetupModule("initializeFileDropZone", _ => true).SetupVoid("dispose", _ => true).SetVoidResult();
        var cut = Render<TestableTnTInputFile>();
        var initialInitializeCount = JSInterop.Invocations.Count(invocation => invocation.Identifier == "initializeFileDropZone");
        var initialUpdateCount = JSInterop.Invocations.Count(invocation => invocation.Identifier == "onUpdate");
        var pendingOnLoad = module.SetupVoid("onLoad", _ => true);

        var renderTask = cut.InvokeAsync(() => cut.Instance.InvokeOnAfterRenderAsync(firstRender: true));
        await Task.Yield();
        pendingOnLoad.Invocations.Should().ContainSingle();
        await cut.Instance.DisposeAsync();
        pendingOnLoad.SetVoidResult();
        await renderTask;

        JSInterop.Invocations.Count(invocation => invocation.Identifier == "initializeFileDropZone").Should().Be(initialInitializeCount);
        JSInterop.Invocations.Count(invocation => invocation.Identifier == "onUpdate").Should().Be(initialUpdateCount);
        JSInterop.Invocations.Where(invocation => invocation.Identifier is "onLoad" or "onUpdate").Should().OnlyContain(invocation => invocation.Arguments.LastOrDefault() != null);
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

    private sealed class TestableTnTInputFile : global::NTComponents.TnTInputFile {
        public Task InvokeOnAfterRenderAsync(bool firstRender) => base.OnAfterRenderAsync(firstRender);
    }
}
