using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using NTComponents.Core;
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
    private readonly NTDisposalState _disposalState = new();

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
        if (_disposalState.TryBegin()) {
            try {
                Dispose(disposing: true);
            }
            finally {
                GC.SuppressFinalize(this);
            }
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync() {
        if (_disposalState.TryBegin()) {
            try {
                await DisposeAsyncCore().ConfigureAwait(false);
                Dispose(disposing: false);
            }
            finally {
                GC.SuppressFinalize(this);
            }
        }
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

        if (!DisposalStarted) {
            try {
                if (ShouldLoadJsModule && !string.IsNullOrWhiteSpace(JsModulePath)) {
                    if (firstRender || IsolatedJsModule is null) {
                        var importedModule = await JSRuntime.ImportIsolatedJs(this, JsModulePath);
                        if (!DisposalStarted) {
                            IsolatedJsModule = importedModule;
                            if (TryGetInteropReferences(out var module, out var dotNetRef)) {
                                await module.InvokeVoidAsync("onLoad", Element, dotNetRef);
                            }
                        }
                        else {
                            await importedModule.DisposeAsync().ConfigureAwait(false);
                        }
                    }

                    if (TryGetInteropReferences(out var currentModule, out var currentDotNetRef)) {
                        await currentModule.InvokeVoidAsync("onUpdate", Element, currentDotNetRef);
                    }
                }
                else {
                    await DisposeJsModuleAsync().ConfigureAwait(false);
                }
            }
            catch (JSDisconnectedException) {
                // JS runtime was disconnected, safe to ignore during render.
            }
        }
    }

    private bool DisposalStarted => _disposalState.HasStarted;

    private bool TryGetInteropReferences([NotNullWhen(true)] out IJSObjectReference? module, [NotNullWhen(true)] out DotNetObjectReference<TDerived>? dotNetRef) {
        module = IsolatedJsModule;
        dotNetRef = DotNetObjectRef;
        return !DisposalStarted && module is not null && dotNetRef is not null;
    }
}
