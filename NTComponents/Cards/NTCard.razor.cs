using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
///     Material 3 card for displaying content and actions about a single subject.
/// </summary>
/// <remarks>
///     <para>
///         Use a card to group related information that belongs to one topic, record, or action entry point. Cards work best when the content benefits from a visible container, such as a summary,
///         preview, status block, or navigational teaser.
///     </para>
///     <para>
///         Best practice: keep each card focused on one subject and preserve a clear content hierarchy inside it. A card should help users scan related content quickly, not become a generic wrapper
///         for unrelated controls, layout structure, or whole-page sections.
///     </para>
///     <para>
///         Prefer one interaction model per card. Either make the card surface directly actionable by providing <see cref="OnClickCallback" /> or an <c>href</c> in <see
///         cref="TnTComponentBase.AdditionalAttributes" />, or leave the container non-actionable and place buttons, links, or menus inside the content. Avoid stacking a primary actionable surface
///         with competing nested primary actions unless those inner actions are clearly secondary.
///     </para>
///     <para>
///         Use <see cref="NTCardVariant.Filled" /> as the default presentation, <see cref="NTCardVariant.Outlined" /> when the layout needs a stronger edge definition without adding depth, and <see
///         cref="NTCardVariant.Elevated" /> when the card needs shadow-based separation from the surrounding surface.
///     </para>
///     <para>
///         Typography best practice: use title roles such as <see cref="NTTypography.TitleLarge" /> or <see cref="NTTypography.TitleMedium" /> for card headings and body roles such as <see
///         cref="NTTypography.BodyMedium" /> for supporting text. Keep the heading compact enough that the card still reads as a contained surface rather than a full page section.
///     </para>
/// </remarks>
public partial class NTCard : TnTComponentBase {

    /// <summary>
    ///     Override for the card container color.
    /// </summary>
    [Parameter]
    public TnTColor? BackgroundColor { get; set; }

    /// <summary>
    ///     The content rendered inside the card.
    /// </summary>
    [Parameter]
    public RenderFragment ChildContent { get; set; } = default!;

    /// <summary>
    ///     Corner radius token used by the card container.
    /// </summary>
    [Parameter]
    public NTCornerRadius CornerRadius { get; set; } = NTCornerRadius.Medium;

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("nt-card")
        .AddClass("nt-card-filled", Variant == NTCardVariant.Filled)
        .AddClass("nt-card-outlined", Variant == NTCardVariant.Outlined)
        .AddClass("nt-card-elevated", Variant == NTCardVariant.Elevated)
        .AddClass("nt-card-actionable", RendersButton || RendersAnchor)
        .AddClass("nt-card-disabled", IsDisabled)
        .AddClass("nt-card-link", RendersAnchor)
        .AddClass("nt-card-button", RendersButton)
        .AddCornerRadius(CornerRadius)
        .AddElevation(Elevation, Variant == NTCardVariant.Elevated)
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddVariable("nt-card-background-color", EffectiveBackgroundColor)
        .AddVariable("nt-card-content-color", EffectiveTextColor.ToCssTnTColorVariable(), EffectiveTextColor.HasValue)
        .AddVariable("nt-card-outline-color", OutlineColor)
        .AddVariable("nt-card-state-layer-color", EffectiveStateLayerColor)
        .Build();

    /// <summary>
    ///     Elevation level used for the elevated variant.
    /// </summary>
    [Parameter]
    public NTElevation Elevation { get; set; } = NTElevation.Lowest;

    /// <summary>
    ///     Click handler for directly actionable cards.
    /// </summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnClickCallback { get; set; }

    /// <summary>
    ///     Override for the outline and focus-ring color.
    /// </summary>
    [Parameter]
    public TnTColor OutlineColor { get; set; } = TnTColor.OutlineVariant;

    /// <summary>
    ///     Override for the card content color.
    /// </summary>
    [Parameter]
    public TnTColor? TextColor { get; set; }

    /// <summary>
    ///     Visual variant of the card.
    /// </summary>
    [Parameter]
    public NTCardVariant Variant { get; set; } = NTCardVariant.Filled;

    private TnTColor EffectiveBackgroundColor {
        get {
            if (BackgroundColor.HasValue && (Variant == NTCardVariant.Outlined || BackgroundColor.Value != TnTColor.Transparent)) {
                return BackgroundColor.Value;
            }

            return Variant switch {
                NTCardVariant.Elevated => TnTColor.SurfaceContainerLow,
                NTCardVariant.Outlined => TnTColor.Transparent,
                _ => TnTColor.SurfaceContainerHighest
            };
        }
    }

    private TnTColor? EffectiveTextColor => TextColor ?? (Variant == NTCardVariant.Outlined ? null : TnTColor.OnSurface);
    private TnTColor EffectiveStateLayerColor => TextColor ?? TnTColor.OnSurface;

    private bool HasHrefAttribute => AdditionalAttributes?.TryGetValue("href", out var href) == true
        && !string.IsNullOrWhiteSpace(Convert.ToString(href));

    private bool IsDisabled => AdditionalAttributes?.TryGetValue("disabled", out var disabledValue) == true
        && disabledValue is not false and not null;

    private IReadOnlyDictionary<string, object>? RenderedAdditionalAttributes {
        get {
            if (AdditionalAttributes is null) {
                return null;
            }

            var filteredAttributes = new Dictionary<string, object>(AdditionalAttributes);

            if (RendersAnchor && !IsDisabled) {
                return filteredAttributes;
            }

            if (RendersAnchor) {
                foreach (var attributeName in _linkAttributeNames) {
                    filteredAttributes.Remove(attributeName);
                }

                filteredAttributes["aria-disabled"] = "true";
                filteredAttributes["tabindex"] = "-1";

                return filteredAttributes;
            }

            foreach (var attributeName in _linkAttributeNames) {
                filteredAttributes.Remove(attributeName);
            }

            return filteredAttributes;
        }
    }

    private bool RendersAnchor => !RendersButton && HasHrefAttribute;
    private bool RendersButton => OnClickCallback.HasDelegate;
    private static readonly HashSet<string> _linkAttributeNames = ["href", "target", "rel", "download", "hreflang", "ping", "referrerpolicy"];
}

/// <summary>
///     Visual variants for <see cref="NTCard" />.
/// </summary>
/// <remarks>
///     <para>Choose the variant based on how strongly the card needs to separate from its surroundings. Variants should change emphasis, not the card's core purpose or content structure.</para>
///     <para>
///         Best practice: keep collections visually consistent. When multiple cards appear together, prefer using the same resting variant across the group unless a single card intentionally needs a
///         different emphasis.
///     </para>
/// </remarks>
public enum NTCardVariant {

    /// <summary>
    ///     Filled card with subtle separation from the background.
    /// </summary>
    /// <remarks>
    ///     <para>Use this as the default card style for most application surfaces. It provides a contained presentation without adding the extra prominence of an outline or shadow.</para>
    ///     <para>Best practices: prefer filled cards for standard lists, dashboards, and detail summaries. Do not make them transparent; the filled variant is intended to read as a surfaced container.</para>
    /// </remarks>
    Filled,

    /// <summary>
    ///     Outlined card with a visible boundary.
    /// </summary>
    /// <remarks>
    ///     <para>Use this when the layout needs a stronger edge definition but not additional depth. This works well on busy or shared surfaces where the card boundary needs to stay explicit.</para>
    ///     <para>
    ///         Best practices: keep the background transparent by default so the parent surface can show through, and rely on the outline to communicate grouping. Prefer this over elevation when the
    ///         goal is structural clarity rather than prominence.
    ///     </para>
    /// </remarks>
    Outlined,

    /// <summary>
    ///     Elevated card with shadow-based separation.
    /// </summary>
    /// <remarks>
    ///     <para>Use this when the card needs to lift off the surrounding surface, such as featured content, draggable surfaces, or layouts where depth helps separate adjacent layers.</para>
    ///     <para>
    ///         Best practices: use elevation intentionally and sparingly so it remains meaningful. Adjust <see cref="NTCard.Elevation" /> only for this variant, and keep the background opaque so the
    ///         card continues to read as a surfaced element instead of a floating transparent frame.
    ///     </para>
    /// </remarks>
    Elevated
}
