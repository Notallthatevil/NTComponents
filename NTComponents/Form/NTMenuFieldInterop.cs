using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using NTComponents.Core;
using NTComponents.Ext;

namespace NTComponents;

/// <summary>
///     Owns the isolated JS module (and optional .NET callback reference) shared by the combobox-style menu fields.
/// </summary>
internal sealed class NTMenuFieldInterop<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] TOwner>(TOwner _owner, string _modulePath, NTDisposalState _disposalState, bool _passDotNetRef, bool _tolerateJsException) where TOwner : class {
    private DotNetObjectReference<TOwner>? _dotNetRef;

    public IJSObjectReference? Module { get; private set; }

    public async Task AfterRenderAsync(IJSRuntime jsRuntime, ElementReference element, bool firstRender) {
        if (_disposalState.HasStarted) {
            return;
        }

        try {
            if (_passDotNetRef) {
                _dotNetRef ??= DotNetObjectReference.Create(_owner);
            }

            if (firstRender) {
                var importedModule = await jsRuntime.ImportIsolatedJs(_owner, _modulePath);
                if (!_disposalState.HasStarted && IsReady) {
                    Module = importedModule;
                    await importedModule.InvokeVoidAsync("onLoad", Args(element));
                }
                else {
                    await importedModule.DisposeAsync();
                }
            }
            else if (!_disposalState.HasStarted && Module is not null && IsReady) {
                await Module.InvokeVoidAsync("onUpdate", Args(element));
            }
        }
        catch (JSDisconnectedException) {
            // JS runtime was disconnected, safe to ignore during render.
        }
        catch (JSException) when (_tolerateJsException) {
            // Enhancement failed. Keep the native field usable instead of failing the interactive circuit.
            var module = Module;
            Module = null;
            if (module is not null) {
                try {
                    await module.DisposeAsync();
                }
                catch (JSDisconnectedException) {
                    // JS runtime was disconnected while releasing the failed enhancement.
                }
            }
        }
    }

    public async ValueTask ReleaseAsync(ElementReference element) {
        var module = Module;
        Module = null;
        try {
            if (module is not null) {
                try {
                    try {
                        await module.InvokeVoidAsync("onDispose", Args(element)).ConfigureAwait(false);
                    }
                    finally {
                        await module.DisposeAsync().ConfigureAwait(false);
                    }
                }
                catch (JSDisconnectedException) {
                    // JS runtime was disconnected, safe to ignore during disposal.
                }
            }
        }
        finally {
            _dotNetRef?.Dispose();
            _dotNetRef = null;
        }
    }

    private bool IsReady => !_passDotNetRef || _dotNetRef is not null;

    private object?[] Args(ElementReference element) => _passDotNetRef ? [element, _dotNetRef] : [element];
}
