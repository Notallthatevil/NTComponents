using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Threading.Tasks;
using NTComponents.Core;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
///     A Material 3 compliant carousel component for displaying a sequence of items with navigation and indicators.
/// </summary>
/// <remarks>
///     The component expects child content consisting of <see cref="NTCarouselItem" /> elements. It exposes appearance and behavior options such as snapping, autoplay, background color, and
///     dragging. JavaScript interop is used for lifecycle hooks via the module path returned by <see cref="JsModulePath" />.
/// </remarks>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.ProgressivelyEnhanced,
    CompatibilitySummary = "Renders carousel content statically and enhances scrolling with JavaScript.",
    CompatibilityDetails = "Static SSR emits the carousel items and initial layout. Dragging, snapping, automatic scrolling, and index synchronization depend on the browser module.")]
public partial class NTCarousel {

    /// <summary>
    ///     Controls whether pointer dragging is allowed to navigate the carousel.
    /// </summary>
    [Parameter]
    public bool AllowDragging { get; set; } = true;

    /// <summary>
    ///     Material 3 carousel appearance variant.
    /// </summary>
    [Parameter]
    public CarouselAppearance Appearance { get; set; } = CarouselAppearance.MultiBrowse;

    /// <summary>
    ///     Optional interval in milliseconds for autoplay. When null, autoplay is disabled.
    /// </summary>
    [Parameter]
    public int? AutoPlayInterval { get; set; }

    /// <summary>
    ///     Optional background color for the carousel expressed as a <see cref="TnTColor" />. When not specified, no inline background variable will be emitted.
    /// </summary>
    [Parameter]
    public TnTColor? BackgroundColor { get; set; }

    /// <summary>
    ///     The content (child items) to render inside the carousel.
    /// </summary>
    [Parameter, EditorRequired]
    public RenderFragment ChildContent { get; set; }

    /// <summary>
    ///     Computed CSS class string for the carousel element. Includes classes controlled by parameters and merges additional attributes that provide classes.
    /// </summary>
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("tnt-carousel")
        .AddClass("tnt-carousel-hero", Appearance is CarouselAppearance.Hero or CarouselAppearance.CenterAlignedHero)
        .AddClass("tnt-carousel-centered", Appearance == CarouselAppearance.CenterAlignedHero && EnableSnapping)
        .AddClass("tnt-carousel-snapping", EnableSnapping)
        .Build();

    /// <summary>
    ///     Computed inline style string for the carousel element. Exposes CSS variables (for example background color) when appropriate parameters are provided.
    /// </summary>
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddVariable("tnt-carousel-bg-color", BackgroundColor.GetValueOrDefault(), BackgroundColor.HasValue)
        .Build();

    /// <summary>
    ///     Enables CSS snapping behavior for carousel items when true. When enabled, additional classes are applied to support centered and snapping layouts.
    /// </summary>
    [Parameter]
    public bool EnableSnapping { get; set; } = true;

    /// <summary>
    ///     Path to the JavaScript module that implements lifecycle interop for the carousel. The module is expected to export <c>onLoad</c>, <c>onUpdate</c>, and <c>onDispose</c>.
    /// </summary>
    public override string? JsModulePath => "./_content/NTComponents/Carousel/NTCarousel.razor.js";

    /// <summary>
    ///     Invoked when the currently displayed index changes. The callback receives the new index (zero-based).
    /// </summary>
    [Parameter]
    public EventCallback<int> OnIndexChanged { get; set; }

    /// <summary>
    ///     Enumerates the currently registered carousel items in display order (first by <see cref="NTCarouselItem.Order" />, then by internal id to provide a stable ordering).
    /// </summary>
    private IEnumerable<NTCarouselItem> _carouselItems => _items.Values.OrderBy(item => item.Order).ThenBy(item => item.InternalId);

    /// <summary>
    ///     Internal storage of child items keyed by their internal id.
    /// </summary>
    private Dictionary<int, NTCarouselItem> _items = new();

    /// <summary>
    ///     Monotonic counter used to assign internal ids to child items when they are added.
    /// </summary>
    private int _nextId;

    /// <summary>
    ///     Registers a child <see cref="NTCarouselItem" /> with this carousel. Assigns an internal id if one isn't present.
    /// </summary>
    /// <param name="item">The carousel item being added.</param>
    internal void AddChild(NTCarouselItem item) {
        item.InternalId ??= System.Threading.Interlocked.Increment(ref _nextId);
        if (_items.TryAdd(item.InternalId.Value, item)) {
            StateHasChanged();
        }
    }

    /// <summary>
    ///     Removes a previously registered <see cref="NTCarouselItem" /> from this carousel. If the item was present it will trigger a re-render.
    /// </summary>
    /// <param name="item">The carousel item to remove.</param>
    internal void RemoveChild(NTCarouselItem item) {
        if (item.InternalId is not null && _items.Remove(item.InternalId.Value)) {
            StateHasChanged();
        }
    }
}

/// <summary>
///     Material 3 carousel appearance variants for the <see cref="NTCarousel" />.
/// </summary>
public enum CarouselAppearance {

    /// <summary>
    ///     Horizontal contained layout showing a large item with medium and small trailing items when space allows.
    /// </summary>
    MultiBrowse = 0,

    /// <summary>
    ///     Horizontal uncontained row with uniform item sizing.
    /// </summary>
    Uncontained = 1,

    /// <summary>
    ///     Horizontal uncontained row where item widths vary by content aspect ratio.
    /// </summary>
    UncontainedMultiAspectRatio = 2,

    /// <summary>
    ///     Horizontal contained layout with one dominant large item and a trailing preview.
    /// </summary>
    Hero = 3,

    /// <summary>
    ///     Horizontal contained hero layout with the dominant item centered between previews.
    /// </summary>
    CenterAlignedHero = 4,

    /// <summary>
    ///     Vertical edge-to-edge layout showing one item per carousel viewport.
    /// </summary>
    FullScreen = 5
}
