using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using NTComponents.Ext;
using NTComponents.Interfaces;
using System.Diagnostics.CodeAnalysis;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
///     Base class for view components that keep static SSR markup and add an isolated JavaScript enhancement module.
/// </summary>
/// <typeparam name="TDerived">The concrete view component type. Must match the derived component type.</typeparam>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.ProgressivelyEnhanced,
    CompatibilitySummary = "Renders useful static markup and enhances behavior with browser JavaScript.",
    CompatibilityDetails = "Static SSR emits the component shell and accessible markup. The browser module adds richer behavior after the page reaches the browser.")]
public abstract class NTPageScriptViewBase<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] TDerived> : NTCanonicalViewBase, INTPageScriptComponent<TDerived> where TDerived : ComponentBase {

    /// <inheritdoc />
    public DotNetObjectReference<TDerived>? DotNetObjectRef { get; private set; }

    /// <inheritdoc />
    public IJSObjectReference? IsolatedJsModule { get; private set; }

    /// <inheritdoc />
    public abstract string? JsModulePath { get; }

    /// <summary>
    ///     Gets a value indicating whether the component should load and update its JavaScript enhancement module.
    /// </summary>
    protected virtual bool ShouldLoadJsModule => true;

    /// <summary>
    ///     The JSRuntime instance used for JavaScript interop in interactive render modes.
    /// </summary>
    [Inject]
    protected IJSRuntime JSRuntime { get; private set; } = default!;

    /// <summary>
    ///     Gets the page script fragment used to enhance static SSR markup when the page loads in the browser.
    /// </summary>
    protected RenderFragment PageScript => builder => {
        builder.OpenComponent<NTPageScript>(0);
        builder.AddAttribute(1, nameof(NTPageScript.Src), JsModulePath);
        builder.CloseComponent();
    };

    /// <summary>
    ///     Initializes a new instance of the <see cref="NTPageScriptViewBase{TDerived}" /> class.
    /// </summary>
    protected NTPageScriptViewBase() {
        if (this is not TDerived derived) {
            throw new InvalidCastException($"NTPageScriptViewBase: TDerived must match the actual derived class type. Got {GetType().Name} but expected {typeof(TDerived).Name}.");
        }

        DotNetObjectRef = DotNetObjectReference.Create(derived);
    }

    /// <inheritdoc />
    public void Dispose() {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync() {
        await DisposeAsyncCore().ConfigureAwait(false);
        Dispose(disposing: false);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Disposes managed resources used by the view.
    /// </summary>
    /// <param name="disposing">Whether managed resources should be disposed.</param>
    protected virtual void Dispose(bool disposing) {
        if (disposing) {
            DotNetObjectRef?.Dispose();
            DotNetObjectRef = null;
        }
    }

    /// <summary>
    ///     Disposes JavaScript resources used by the view.
    /// </summary>
    protected virtual async ValueTask DisposeAsyncCore() {
        await DisposeJsModuleAsync().ConfigureAwait(false);

        if (DotNetObjectRef is IAsyncDisposable asyncDisposable) {
            await asyncDisposable.DisposeAsync().ConfigureAwait(false);
        }
        else {
            DotNetObjectRef?.Dispose();
        }

        DotNetObjectRef = null;
    }

    private async ValueTask DisposeJsModuleAsync() {
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
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender) {
        await base.OnAfterRenderAsync(firstRender);

        try {
            if (!ShouldLoadJsModule || string.IsNullOrWhiteSpace(JsModulePath)) {
                await DisposeJsModuleAsync().ConfigureAwait(false);
                return;
            }

            if (firstRender || IsolatedJsModule is null) {
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
