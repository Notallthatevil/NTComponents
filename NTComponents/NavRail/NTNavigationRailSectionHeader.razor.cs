using Microsoft.AspNetCore.Components;
using NTComponents.Core;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
///     Non-interactive section label for grouping <see cref="NTNavigationRail" /> destinations.
/// </summary>
/// <remarks>
///     Section headers are static content. The navigation rail browser module decides when they are visible in expanded rail or popover presentation.
/// </remarks>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.SsrCompatible,
    CompatibilitySummary = "Renders useful static HTML without requiring Blazor interactivity.",
    CompatibilityDetails = "Static SSR preserves the component structure, styling, and accessibility semantics. Dynamic parameter changes require a new render.")]
public partial class NTNavigationRailSectionHeader : NTComponentBase {

    /// <summary>
    ///     Heading level exposed to assistive technologies.
    /// </summary>
    [Parameter]
    public int AriaLevel { get; set; } = 2;

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("nt-navigation-rail-section-header")
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .Build();

    /// <summary>
    ///     Visible section label.
    /// </summary>
    [Parameter, EditorRequired]
    public string Label { get; set; } = string.Empty;

    private string? EffectiveElementId => ElementId ?? TryGetStringAttribute("id");
    private string? EffectiveElementLang => ElementLang ?? TryGetStringAttribute("lang");
    private string? EffectiveElementTitle => ElementTitle ?? TryGetStringAttribute("title");

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();

        if (string.IsNullOrWhiteSpace(Label)) {
            throw new ArgumentException("NTNavigationRailSectionHeader requires a non-empty Label.", nameof(Label));
        }

        if (AriaLevel is < 1 or > 6) {
            throw new ArgumentOutOfRangeException(nameof(AriaLevel), AriaLevel, "NTNavigationRailSectionHeader AriaLevel must be between 1 and 6.");
        }
    }

    private string? TryGetStringAttribute(string attributeName) {
        return AdditionalAttributes?.TryGetValue(attributeName, out var value) == true
            ? Convert.ToString(value)
            : null;
    }
}
