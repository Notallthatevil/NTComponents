using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using NTComponents.CodeDocumentation;

namespace NTComponents;

/// <summary>
///     Renders its content only once it is first visible, and keeps it rendered from then on.
/// </summary>
/// <remarks>
///     Use it for content that is costly to keep on the page while no one can see it, such as the editors inside closed
///     panels or hidden tabs. Until then it renders an empty placeholder that the browser watches; a placeholder inside a hidden
///     tab or a closed <c>details</c> element is not visible. The content renders slightly before the placeholder scrolls into
///     view, so it is usually ready when it appears. Static SSR sends the content inside an inert <c>template</c>, so it is not
///     part of the page until the placeholder is visible; it is shown as static HTML, so interactive components inside it do
///     not start.
/// </remarks>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.ProgressivelyEnhanced,
    CompatibilitySummary = "Defers its content until the placeholder is visible, in static SSR and interactive pages alike.",
    CompatibilityDetails = "Static SSR emits the placeholder with the content inside an inert template, and the page script moves it into the page once the placeholder is visible. Prerendering for an interactive page emits only the placeholder, and the content renders once Blazor is interactive and the placeholder is visible.")]
public partial class NTLazyLoader {
    private const string LazyLoaderJsModulePath = "./_content/NTComponents/LazyLoader/NTLazyLoader.razor.js";

    private bool _shown;

    /// <summary>
    ///     Gets or sets the content rendered once the placeholder is first visible.
    /// </summary>
    [Parameter, EditorRequired]
    public RenderFragment ChildContent { get; set; } = default!;

    /// <inheritdoc />
    public override string? ElementClass => null;

    /// <inheritdoc />
    public override string? ElementStyle => null;

    /// <inheritdoc />
    public override string? JsModulePath => LazyLoaderJsModulePath;

    // Static SSR never becomes interactive, so the page script shows the content; prerendering for an interactive page leaves that to Blazor.
    private bool IsStatic => !RendererInfo.IsInteractive && AssignedRenderMode is null;

    /// <summary>
    ///     Renders the content; the browser calls it when the placeholder becomes visible.
    /// </summary>
    [JSInvokable]
    public Task ShowAsync() => InvokeAsync(() => {
        if (!_shown) {
            _shown = true;
            StateHasChanged();
        }
    });
}
