using Microsoft.AspNetCore.Components;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
///     Material 3 footer region for supplemental layout content.
/// </summary>
/// <remarks>
///     Defaults to <see cref="NTLayoutTag.Footer" />. Set <see cref="NTComponents.Layout.NTLayoutComponentBase.Tag" /> to a neutral or sectioning tag when nesting outside a valid footer context.
/// </remarks>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.SsrCompatible,
    CompatibilitySummary = "Renders useful static HTML without requiring Blazor interactivity.",
    CompatibilityDetails = "Static SSR preserves the component structure, styling, and accessibility semantics. Dynamic parameter changes require a new render.")]
public partial class NTFooter {

    /// <inheritdoc />
    [Parameter]
    public override NTElevation? Elevation { get; set; } = NTElevation.Low;

    /// <summary>
    ///     Applies the sticky footer layout state when this component is a direct child of <see cref="NTLayout" /> and makes the sibling <see cref="NTBody" /> the scroll container.
    /// </summary>
    [Parameter]
    public bool FixedPosition { get; set; } = true;

    /// <inheritdoc />
    protected override string ComponentClass => "nt-footer";

    /// <inheritdoc />
    protected override string? ComponentStateClass => FixedPosition ? "nt-footer-fixed-position" : null;

    /// <inheritdoc />
    protected override NTLayoutTag DefaultTag => NTLayoutTag.Footer;

}
