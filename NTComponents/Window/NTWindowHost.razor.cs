using Microsoft.AspNetCore.Components;
using NTComponents.CodeDocumentation;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
///     Renders windows managed by <see cref="INTWindowService" /> in a persistent viewport layer.
/// </summary>
/// <remarks>
///     Place one host in a persistent interactive layout. The host uses <c>data-permanent</c> so its rendered window DOM
///     survives enhanced static navigation and subscribes to service changes when Blazor is interactive.
/// </remarks>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.SsrCompatible,
    CompatibilitySummary = "Renders managed floating windows and preserves their DOM during enhanced static navigation.",
    CompatibilityDetails = "Static SSR emits managed windows present during the request and marks the host as permanent. Live collection changes require an interactive scoped service shared by the caller and host; independent static requests use independent scopes.")]
public partial class NTWindowHost : NTComponentBase, IDisposable {

    /// <summary>
    ///     Gets or sets the accessible label for the window layer.
    /// </summary>
    [Parameter]
    public string AriaLabel { get; set; } = "Open windows";

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("nt-window-host")
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .Build();

    /// <inheritdoc />
    protected override void OnInitialized() {
        base.OnInitialized();
        WindowService.Changed += HandleWindowsChanged;
    }

    /// <inheritdoc />
    public void Dispose() {
        WindowService.Changed -= HandleWindowsChanged;
        GC.SuppressFinalize(this);
    }

    private void HandleWindowsChanged() => _ = InvokeAsync(StateHasChanged);

    private void SetOpen(INTWindow window, bool open) {
        if (!open) {
            WindowService.Close(window);
        }
    }

    private void SetState(INTWindow window, NTWindowState state) => WindowService.SetState(window, state);
}
