using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;

namespace NTComponents.Site.Components;

public sealed class DocumentationPreviewBoundary : ErrorBoundary {
    [Inject]
    private ILogger<DocumentationPreviewBoundary> Logger { get; set; } = default!;

    protected override Task OnErrorAsync(Exception exception) {
        Logger.LogWarning(exception, "The documentation preview could not render with the selected options.");
        return Task.CompletedTask;
    }
}
