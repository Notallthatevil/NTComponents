using Microsoft.AspNetCore.Components;
using NTComponents.Core;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
///     Represents a single item inside a <see cref="NTCarousel" />. The item hosts arbitrary render content and provides presentation options such as a background image, ordering, and click handling.
/// </summary>
/// <remarks>
///     Instances of <see cref="NTCarouselItem" /> must be descendants of <see cref="NTCarousel" />. During initialization the item registers itself with the parent carousel and unregisters on
///     disposal to keep the parent's item collection up to date.
/// </remarks>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.ProgressivelyEnhanced,
    CompatibilitySummary = "Participates in parent component rendering and inherits the parent interaction model.",
    CompatibilityDetails = "The item content is present in static SSR. Click callbacks, active item state, and carousel-driven scrolling behavior require the parent carousel enhancement.")]
public partial class NTCarouselItem {

    /// <summary>
    ///     Optional URL to use as the background image for the item. When specified an inline background-image style will be emitted.
    /// </summary>
    [Parameter]
    public string? BackgroundImageSrc { get; set; }

    /// <summary>
    ///     The content to render inside the carousel item. This parameter is required.
    /// </summary>
    [Parameter, EditorRequired]
    public RenderFragment ChildContent { get; set; }

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
            .AddFromAdditionalAttributes(AdditionalAttributes)
            .AddClass("tnt-carousel-item-content")
            .AddClass("tnt-interactable", OnClickCallback.HasDelegate)
            .AddClass("tnt-carousel-hero", _hero)
            .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
            .AddFromAdditionalAttributes(AdditionalAttributes)
            .AddStyle("background-image", $"url('{BackgroundImageSrc}')", !string.IsNullOrWhiteSpace(BackgroundImageSrc))
            .AddStyle("width", "80%", !_hero)
            .AddStyle("width", "100%", _hero)
            .Build();

    /// <summary>
    ///     When true, enables the ripple visual effect for interactable items. Defaults to <c>true</c>.
    /// </summary>
    [Parameter]
    public bool EnableRipple { get; set; } = true;

    /// <inheritdoc />
    public override string? JsModulePath => "./_content/NTComponents/Carousel/NTCarouselItem.razor.js";

    /// <summary>
    ///     Callback invoked when the item is clicked. Consumers can supply an <see cref="EventCallback" /> to react to clicks on the item's content.
    /// </summary>
    [Parameter]
    public EventCallback OnClickCallback { get; set; }

    /// <summary>
    ///     Logical order used when the parent carousel sorts its items. Lower values appear earlier; when equal the parent's internal id provides a stable tie-breaker.
    /// </summary>
    [Parameter]
    public int Order { get; set; }

    /// <summary>
    ///     Internal id assigned by the parent carousel to provide a stable ordering when multiple items share the same <see cref="Order" />. This value is managed by <see cref="NTCarousel" /> and
    ///     should not be set by consumers.
    /// </summary>
    internal int? InternalId { get; set; }

    /// <summary>
    ///     The parent carousel that this item belongs to. Supplied via cascading parameter.
    /// </summary>
    [CascadingParameter]
    private NTCarousel _carousel { get; set; } = default!;

    /// <summary>
    ///     Returns true when the parent carousel uses a hero-style layout.
    /// </summary>
    private bool _hero => _carousel?.Appearance is CarouselAppearance.Hero or CarouselAppearance.CenterAlignedHero;


    /// <inheritdoc />
    protected override void Dispose(bool disposing) {
        base.Dispose(disposing);
        if (disposing) {
            _carousel?.RemoveChild(this);
        }
    }

    /// <inheritdoc />
    protected override void OnInitialized() {
        base.OnInitialized();
        if (_carousel is null) {
            throw new InvalidOperationException($"{nameof(NTCarouselItem)} must be a decendant of {nameof(NTCarousel)}");
        }

        _carousel.AddChild(this);
    }
}
