using Microsoft.AspNetCore.Components;
using NTComponents.CodeDocumentation;
using NTComponents.Layout;

namespace NTComponents;

/// <summary>
///     Material 3 app layout shell for composing headers, body regions, footers, navigation rails, and nested layouts.
/// </summary>
/// <remarks>
///     Place <see cref="NTNavigationRail" /> as a direct child beside either an <see cref="NTBody" /> or another nested <see cref="NTLayout" /> to create a rail-and-content application shell.
/// </remarks>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.SsrCompatible,
    CompatibilitySummary = "Renders semantic layout regions without requiring Blazor interactivity.",
    CompatibilityDetails = "Use NTLayout with NTHeader, NTBody, NTFooter, and NTNavigationRail for static SSR shells. Interactive child content keeps its own render-mode requirements.")]
public partial class NTLayout {

    [CascadingParameter]
    private NTLayout? ParentLayout { get; set; }

    /// <inheritdoc />
    protected override string ComponentClass => "nt-layout";

    /// <inheritdoc />
    protected override string? ComponentStateClass => ParentLayout is null ? null : "nt-layout-nested";

}
