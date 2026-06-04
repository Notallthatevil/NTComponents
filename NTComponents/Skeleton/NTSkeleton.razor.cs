using Microsoft.AspNetCore.Components;
using NTComponents.Core;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
///     Material 3-aligned skeleton placeholder for stabilizing layout while content is loading.
/// </summary>
/// <remarks>
///     <para>
///         Use skeletons when the rough shape of loading content is known and preserving that space reduces perceived wait time or layout shift. Prefer progress indicators when the interface is
///         processing a user action, when progress can be measured, or when the loading structure is unknown.
///     </para>
///     <para>
///         Skeletons are hidden from assistive technology by default because they are visual placeholders, not content. Keep the loading region's owning container responsible for accessible loading
///         state, such as <c>aria-busy</c> or a status message when one is needed.
///     </para>
///     <para>
///         The default colors use Material 3 surface container roles and the animation respects reduced-motion preferences in CSS.
///     </para>
/// </remarks>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.SsrCompatible,
    CompatibilitySummary = "Renders useful static HTML without requiring Blazor interactivity.",
    CompatibilityDetails = "Static SSR preserves the component structure, styling, and accessibility semantics. Dynamic parameter changes require a new render.")]
public partial class NTSkeleton : NTComponentBase {

    /// <summary>
    ///     Whether to show the skeleton placeholder instead of rendering <see cref="ChildContent" /> directly.
    /// </summary>
    /// <remarks>
    ///     Set this to <see langword="true" /> while content is loading. When set to <see langword="false" />, the component renders only <see cref="ChildContent" /> so callers do not have to
    ///     duplicate the final content markup around an external loading branch.
    /// </remarks>
    [Parameter]
    public bool Show { get; set; } = true;

    /// <summary>
    ///     Animation treatment for the placeholder.
    /// </summary>
    [Parameter]
    public NTSkeletonAnimation Animation { get; set; } = NTSkeletonAnimation.Wave;

    /// <summary>
    ///     Optional content rendered invisibly so the skeleton can reserve the final content's natural size.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    ///     Corner radius token used when <see cref="Shape" /> is <see cref="NTSkeletonShape.Text" /> or <see cref="NTSkeletonShape.Rectangle" />.
    /// </summary>
    [Parameter]
    public NTCornerRadius CornerRadius { get; set; } = NTCornerRadius.Small;

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("nt-skeleton")
        .AddClass("nt-skeleton-text", Shape == NTSkeletonShape.Text)
        .AddClass("nt-skeleton-rectangle", Shape == NTSkeletonShape.Rectangle)
        .AddClass("nt-skeleton-circle", Shape == NTSkeletonShape.Circle)
        .AddClass("nt-skeleton-wave", Animation == NTSkeletonAnimation.Wave)
        .AddClass("nt-skeleton-pulse", Animation == NTSkeletonAnimation.Pulse)
        .AddClass("nt-skeleton-has-content", ChildContent is not null)
        .AddCornerRadius(EffectiveCornerRadius)
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddVariable("nt-skeleton-container-color", ContainerColor.ToCssTnTColorVariable())
        .AddVariable("nt-skeleton-highlight-color", HighlightColor.ToCssTnTColorVariable(), HighlightColor.HasValue)
        .AddVariable("nt-skeleton-width", Width!, !string.IsNullOrWhiteSpace(Width))
        .AddVariable("nt-skeleton-height", Height!, !string.IsNullOrWhiteSpace(Height))
        .Build();

    /// <summary>
    ///     Height of the placeholder as a CSS size value.
    /// </summary>
    [Parameter]
    public string? Height { get; set; }

    /// <summary>
    ///     Whether the placeholder is hidden from assistive technology.
    /// </summary>
    [Parameter]
    public bool HideFromAssistiveTechnology { get; set; } = true;

    /// <summary>
    ///     Optional highlight color used by animated skeletons.
    /// </summary>
    [Parameter]
    public TnTColor? HighlightColor { get; set; }

    /// <summary>
    ///     Container color for the resting placeholder surface.
    /// </summary>
    [Parameter]
    public TnTColor ContainerColor { get; set; } = TnTColor.SurfaceContainerHighest;

    /// <summary>
    ///     Shape used to mimic the loading content.
    /// </summary>
    [Parameter]
    public NTSkeletonShape Shape { get; set; } = NTSkeletonShape.Text;

    /// <summary>
    ///     Width of the placeholder as a CSS size value.
    /// </summary>
    [Parameter]
    public string? Width { get; set; }

    private string? AriaHidden => HideFromAssistiveTechnology ? "true" : null;
    private NTCornerRadius EffectiveCornerRadius => Shape == NTSkeletonShape.Circle ? NTCornerRadius.Full : CornerRadius;
}

/// <summary>
///     Skeleton animation treatments.
/// </summary>
public enum NTSkeletonAnimation {

    /// <summary>
    ///     No animation.
    /// </summary>
    None,

    /// <summary>
    ///     Subtle opacity pulse.
    /// </summary>
    Pulse,

    /// <summary>
    ///     Directional shimmer wave.
    /// </summary>
    Wave
}

/// <summary>
///     Skeleton shape options used to mimic loading content.
/// </summary>
public enum NTSkeletonShape {

    /// <summary>
    ///     A compact line placeholder for text content.
    /// </summary>
    Text,

    /// <summary>
    ///     A rectangular placeholder for cards, media, fields, and larger content blocks.
    /// </summary>
    Rectangle,

    /// <summary>
    ///     A fully rounded placeholder for avatars, icons, and circular controls.
    /// </summary>
    Circle
}
