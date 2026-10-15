using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using NTComponents.Site.Documentation;
using System.Diagnostics.CodeAnalysis;

namespace NTComponents.Site.Components;

/// <summary>
///     Renders one hand-written example live, next to the example's own Razor source.
/// </summary>
/// <typeparam name="TExample">The example component. Its <c>.razor</c> file is embedded as a resource with the same name.</typeparam>
public partial class DocsExample<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TExample> {
    private static readonly string Source = ReadSource();
    private DotNetObjectReference<DocsExample<TExample>>? _dotNetRef;
    private ElementReference _preview;

    [Parameter, EditorRequired]
    public string Title { get; set; } = default!;

    [Parameter]
    public string? Description { get; set; }

    private string Id => $"example-{DocumentationCatalog.CreateSlug(Title)}";

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    [Inject]
    private ILoggerFactory LoggerFactory { get; set; } = default!;

    /// <summary>
    ///     Logs an action pressed in the preview under the example's own category, so every demo button gives feedback in the debug console.
    /// </summary>
    [JSInvokable]
    public void LogAction(string label, string kind) => LoggerFactory.CreateLogger(typeof(TExample)).LogInformation("Clicked \"{Label}\" ({Kind})", label, kind);

    public void Dispose() => _dotNetRef?.Dispose();

    protected override async Task OnAfterRenderAsync(bool firstRender) {
        if (firstRender) {
            _dotNetRef = DotNetObjectReference.Create(this);
            await JSRuntime.InvokeVoidAsync("ntDocs.watchExampleActions", _preview, _dotNetRef);
        }
    }

    private static readonly RenderFragment RenderExample = builder => {
        builder.OpenComponent<TExample>(0);
        builder.CloseComponent();
    };

    private static string ReadSource() {
        var resourceName = $"{typeof(TExample).Name}.razor";
        using var stream = typeof(TExample).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"The example source '{resourceName}' is not embedded.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd().Trim();
    }
}
