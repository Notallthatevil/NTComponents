using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Diagnostics.CodeAnalysis;

namespace NTComponents.Core;

/// <summary>
///     A component that handles external click events.
/// </summary>
[Obsolete("TnTExternalClickHandler is obsolete. Use component-owned external click handling instead.")]
[method: DynamicDependency(nameof(OnClick))]
public partial class TnTExternalClickHandler() : TnTPageScriptComponent<TnTExternalClickHandler> {

    /// <summary>
    ///     Gets or sets the child content to be rendered inside this component.
    /// </summary>
    [Parameter]
    public RenderFragment ChildContent { get; set; } = default!;

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
        .AddClass("tnt-external-click-handler")
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .Build();

    /// <summary>
    ///     Gets or sets the callback to be invoked when an external click is detected.
    /// </summary>
    [Parameter, EditorRequired]
    public EventCallback ExternalClickCallback { get; set; }

    /// <inheritdoc />
    public override string? JsModulePath => "./_content/NTComponents/Core/TnTExternalClickHandler.razor.js";

    /// <summary>
    ///     Invoked by JavaScript to handle an external click event.
    /// </summary>
    [JSInvokable]
    public async Task OnClick() => await ExternalClickCallback.InvokeAsync();

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore() {
        if (IsolatedJsModule is not null) {
            await IsolatedJsModule.InvokeVoidAsync("externalClickCallbackDeregister", DotNetObjectRef).ConfigureAwait(false);
        }
        await base.DisposeAsyncCore().ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender) {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender && IsolatedJsModule is not null) {
            await IsolatedJsModule.InvokeVoidAsync("externalClickCallbackRegister", Element, DotNetObjectRef);
        }
    }
}
