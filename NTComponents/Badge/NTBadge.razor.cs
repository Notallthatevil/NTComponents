using Microsoft.AspNetCore.Components;
using NTComponents.CodeDocumentation;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
///     Material 3 style badge that shows a status dot or a short count on an icon.
/// </summary>
/// <remarks>
///     <para>
///         Leave <see cref="Content" /> empty for the small badge, which signals new or unread status without a count. Provide <see cref="Content" /> for the large badge; keep it to four characters
///         or fewer, such as "3" or "999+". Badges are sized up from the Material 3 baseline (an 8px dot and a 20px label using Label Medium) and carry a level-1 elevation shadow. Always provide <see cref="AriaLabel" /> when the badge carries meaning, such as "3 unread messages".
///     </para>
///     <para>
///         The badge is absolutely positioned. The host owns placement: it provides a positioned containing block and sets <c>--nt-badge-anchor-block-start</c> and
///         <c>--nt-badge-anchor-inline-end</c> to the top and end edges of the icon the badge decorates. Both default to the containing block's top-end corner. The badge applies the Material 3
///         offsets from that anchor; with <see cref="NTBadgePlacement.Edge" /> the anchor is instead the point the badge centers on. In a grid host, <c>--nt-badge-grid-area</c> places the badge in the icon's grid cell so that cell becomes its containing block.
///     </para>
///     <para>
///         Prefer the badge parameters on components that implement <see cref="Interfaces.INTBadgeable" />, such as <see cref="NTIconButton" />. When placing a badge manually, set
///         <see cref="NTComponentBase.ElementId" /> and reference it from the host's <c>aria-describedby</c>.
///     </para>
/// </remarks>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.SsrCompatible,
    CompatibilitySummary = "Renders useful static HTML without requiring Blazor interactivity.",
    CompatibilityDetails = "Static SSR preserves the badge markup, styling, and accessible description. Dynamic content changes require a new render.")]
public partial class NTBadge {

    /// <summary>
    ///     Gets or sets the accessible description for the badge, such as "3 unread messages".
    /// </summary>
    /// <remarks>When set, the visible <see cref="Content" /> is hidden from assistive technology and this text is announced instead.</remarks>
    [Parameter]
    public string? AriaLabel { get; set; }

    /// <summary>
    ///     Gets or sets the badge label.
    /// </summary>
    /// <remarks>Leave empty for the small badge. Provide up to four characters, such as "3" or "999+", for the large badge.</remarks>
    [Parameter]
    public string? Content { get; set; }

    /// <summary>
    ///     Gets or sets how the badge positions itself relative to the host's anchor.
    /// </summary>
    /// <remarks>
    ///     Use <see cref="NTBadgePlacement.Icon" /> on icons, <see cref="NTBadgePlacement.Edge" /> on containers such as buttons and chips, and <see cref="NTBadgePlacement.Inline" /> where an
    ///     overlapping badge would cover text, such as trailing a menu item or text-only tab label.
    /// </remarks>
    [Parameter]
    public NTBadgePlacement Placement { get; set; }

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("nt-badge")
        .AddClass("nt-badge-large", IsLarge)
        .AddClass("nt-badge-small", !IsLarge)
        .AddClass("nt-badge-edge", Placement == NTBadgePlacement.Edge)
        .AddClass("nt-badge-inline", Placement == NTBadgePlacement.Inline)
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .Build();

    private bool HasAriaLabel => !string.IsNullOrWhiteSpace(AriaLabel);

    private bool IsLarge => !string.IsNullOrWhiteSpace(Content);

    private string? LabelAriaHidden => HasAriaLabel ? "true" : null;

    /// <summary>
    ///     Appends a shown badge's id to the host's existing <c>aria-describedby</c> value.
    /// </summary>
    internal static string? GetDescribedBy(object? existingDescribedBy, bool showBadge, string badgeId) {
        var describedBy = existingDescribedBy?.ToString();
        if (!showBadge) {
            return describedBy;
        }

        return string.IsNullOrWhiteSpace(describedBy) ? badgeId : $"{describedBy} {badgeId}";
    }
}
