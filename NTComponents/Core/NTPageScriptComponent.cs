using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Diagnostics.CodeAnalysis;
using NTComponents.Ext;
using NTComponents.Interfaces;

namespace NTComponents.Core;

/// <summary>
///     Represents a base class for components that have an isolated JavaScript module.
/// </summary>
/// <typeparam name="TDerived">The type of the component. Must match the derived class type (CRTP pattern).</typeparam>
public abstract class NTPageScriptComponent<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] TDerived> : TnTDisposableComponentBase, INTPageScriptComponent<TDerived> where TDerived : ComponentBase {

    /// <inheritdoc />
    public DotNetObjectReference<TDerived>? DotNetObjectRef { get; set; }

    /// <inheritdoc />
    public IJSObjectReference? IsolatedJsModule { get; private set; }

    /// <inheritdoc />
    public abstract string? JsModulePath { get; }

    /// <summary>
    ///     The JSRuntime instance used for JavaScript interop.
    /// </summary>
    [Inject]
    protected IJSRuntime JSRuntime { get; private set; } = default!;

    /// <summary>
    ///     Gets the render fragment for the page script. Always uses the latest JsModulePath.
    /// </summary>
    protected RenderFragment PageScript => builder => {
        builder.OpenComponent<NTPageScript>(0);
        builder.AddAttribute(1, "Src", JsModulePath);
        builder.CloseComponent();
    };

    /// <summary>
    ///     Initializes a new instance of the <see cref="NTPageScriptComponent{TDerived}" /> class. The type parameter TDerived must match the actual derived class (CRTP pattern).
    /// </summary>
    protected NTPageScriptComponent() {
        if (this is not TDerived derived) {
            throw new InvalidCastException($"NTPageScriptComponent: TDerived must match the actual derived class type. Got {GetType().Name} but expected {typeof(TDerived).Name}.");
        }
        DotNetObjectRef = DotNetObjectReference.Create(derived);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing) {
        if (disposing) {
            DotNetObjectRef?.Dispose();
            DotNetObjectRef = null;
            // Do not dispose IsolatedJsModule here; it should be disposed asynchronously in DisposeAsyncCore.
        }
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore() {
        if (IsolatedJsModule is not null) {
            try {
                await IsolatedJsModule.InvokeVoidAsync("onDispose", Element, DotNetObjectRef);
                await IsolatedJsModule.DisposeAsync().ConfigureAwait(false);
            }
            catch (JSDisconnectedException) {
                // JS runtime was disconnected, safe to ignore during disposal.
            }
            IsolatedJsModule = null;
        }

        if (DotNetObjectRef is IAsyncDisposable asyncDisposable) {
            await asyncDisposable.DisposeAsync().ConfigureAwait(false);
        }
        else {
            DotNetObjectRef?.Dispose();
        }
        DotNetObjectRef = null;
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender) {
        await base.OnAfterRenderAsync(firstRender);
        try {
            if (firstRender) {
                IsolatedJsModule = await JSRuntime.ImportIsolatedJs(this, JsModulePath);
                await (IsolatedJsModule?.InvokeVoidAsync("onLoad", Element, DotNetObjectRef) ?? ValueTask.CompletedTask);
            }

            await (IsolatedJsModule?.InvokeVoidAsync("onUpdate", Element, DotNetObjectRef) ?? ValueTask.CompletedTask);
        }
        catch (JSDisconnectedException) {
            // JS runtime was disconnected, safe to ignore during render.
        }
    }
}

/// <summary>
///     Obsolete compatibility alias for <see cref="NTPageScriptComponent{TDerived}" />.
/// </summary>
[Obsolete("TnTPageScriptComponent is obsolete. Use NTPageScriptComponent instead.")]
public abstract class TnTPageScriptComponent<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] TDerived> : NTPageScriptComponent<TDerived>, ITnTPageScriptComponent<TDerived> where TDerived : ComponentBase;
