using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using NTComponents.CodeDocumentation;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
///     A progressively enhanced Material 3 carousel with responsive keyline layouts, native scrolling, pointer dragging, parallax media, and accessible keyboard navigation.
/// </summary>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.ProgressivelyEnhanced,
    CompatibilitySummary = "Renders an accessible native-scrolling carousel and enhances it with Material 3 keyline motion.",
    CompatibilityDetails = "Static SSR emits ordered slides, labels, and a usable overflow surface. Dynamic keyline sizing, parallax, momentum, autoplay, and index synchronization require the browser module.")]
public partial class NTCarousel {

    private bool _autoPlayPaused;

    /// <summary>
    ///     Gets or sets whether mouse and pen dragging can scroll the carousel. Touch scrolling remains native.
    /// </summary>
    [Parameter]
    public bool AllowDragging { get; set; } = true;

    /// <summary>
    ///     Gets or sets the accessible content name for the carousel.
    /// </summary>
    [Parameter, EditorRequired]
    public string AriaLabel { get; set; } = default!;

    /// <summary>
    ///     Gets or sets the Material 3 carousel layout.
    /// </summary>
    [Parameter]
    public CarouselAppearance Appearance { get; set; } = CarouselAppearance.MultiBrowse;

    /// <summary>
    ///     Gets or sets the optional autoplay interval in seconds. A null value disables autoplay.
    /// </summary>
    [Parameter]
    public double? AutoPlayInterval { get; set; }

    /// <summary>
    ///     Gets or sets the optional carousel background color.
    /// </summary>
    [Parameter]
    public TnTColor? BackgroundColor { get; set; }

    /// <summary>
    ///     Gets or sets the carousel items.
    /// </summary>
    [Parameter, EditorRequired]
    public RenderFragment ChildContent { get; set; } = default!;

    /// <summary>
    ///     Gets or sets whether scrolling settles on item keylines.
    /// </summary>
    [Parameter]
    public bool EnableSnapping { get; set; } = true;

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("nt-carousel")
        .AddClass("nt-carousel-snapping", EnableSnapping)
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddVariable("nt-carousel-background-color", BackgroundColor.GetValueOrDefault(), BackgroundColor.HasValue)
        .AddVariable("nt-carousel-height", $"{ItemHeight}px")
        .Build();

    /// <summary>
    ///     Gets or sets whether the carousel uses a named region landmark instead of a group.
    /// </summary>
    [Parameter]
    public bool IsLandmark { get; set; }

    /// <summary>
    ///     Gets or sets the horizontal item height in CSS pixels.
    /// </summary>
    [Parameter]
    public int ItemHeight { get; set; } = 240;

    /// <inheritdoc />
    public override string? JsModulePath => "./_content/NTComponents/Carousel/NTCarousel.razor.js";

    /// <summary>
    ///     Gets or sets the optional maximum width of a large item in CSS pixels.
    /// </summary>
    [Parameter]
    public int? MaxLargeItemWidth { get; set; }

    /// <summary>
    ///     Gets or sets the callback raised when scrolling settles on a different logical item.
    /// </summary>
    [Parameter]
    public EventCallback<int> OnIndexChanged { get; set; }

    /// <summary>
    ///     Gets or sets the target width of large multi-browse items in CSS pixels. The layout may adjust it to fit complete keyline arrangements.
    /// </summary>
    [Parameter]
    public int PreferredItemWidth { get; set; } = 186;

    private string AutoPlayControlText => _autoPlayPaused ? "Start rotation" : "Pause rotation";
    private string LayoutName => Appearance switch {
        CarouselAppearance.MultiBrowse => "multi-browse",
        CarouselAppearance.Uncontained => "uncontained",
        CarouselAppearance.UncontainedMultiAspectRatio => "uncontained-multi-aspect-ratio",
        CarouselAppearance.Hero => "hero",
        CarouselAppearance.CenterAlignedHero => "center-aligned-hero",
        CarouselAppearance.FullScreen => "full-screen",
        _ => throw new ArgumentOutOfRangeException(nameof(Appearance), Appearance, null)
    };
    private string Role => IsLandmark ? "region" : "group";

    /// <summary>
    ///     Receives the settled logical index from the browser controller.
    /// </summary>
    [JSInvokable]
    public Task NotifyIndexChangedAsync(int index) => OnIndexChanged.InvokeAsync(index);

    /// <summary>
    ///     Synchronizes the visible autoplay control with browser pause state.
    /// </summary>
    [JSInvokable]
    public Task NotifyAutoPlayPausedChangedAsync(bool paused) {
        if (_autoPlayPaused == paused) {
            return Task.CompletedTask;
        }

        _autoPlayPaused = paused;
        return InvokeAsync(StateHasChanged);
    }

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();

        if (string.IsNullOrWhiteSpace(AriaLabel)) {
            throw new InvalidOperationException($"{nameof(AriaLabel)} is required for {nameof(NTCarousel)}.");
        }

        if (AutoPlayInterval is <= 0) {
            throw new ArgumentOutOfRangeException(nameof(AutoPlayInterval), AutoPlayInterval, "Autoplay must be greater than zero seconds.");
        }

        if (MaxLargeItemWidth is <= 0) {
            throw new ArgumentOutOfRangeException(nameof(MaxLargeItemWidth), MaxLargeItemWidth, "The maximum large item width must be greater than zero.");
        }

        if (PreferredItemWidth <= 0) {
            throw new ArgumentOutOfRangeException(nameof(PreferredItemWidth), PreferredItemWidth, "The preferred item width must be greater than zero.");
        }

        if (ItemHeight <= 0) {
            throw new ArgumentOutOfRangeException(nameof(ItemHeight), ItemHeight, "The item height must be greater than zero.");
        }

        if (Appearance == CarouselAppearance.FullScreen && !EnableSnapping) {
            throw new InvalidOperationException("The full-screen Material 3 layout requires snapping.");
        }
    }
}

/// <summary>
///     Material 3 carousel appearance variants for <see cref="NTCarousel" />.
/// </summary>
public enum CarouselAppearance {

    /// <summary>
    ///     Horizontal contained layout showing large, medium, and small items.
    /// </summary>
    MultiBrowse = 0,

    /// <summary>
    ///     Horizontal uncontained row with uniform item sizing.
    /// </summary>
    Uncontained = 1,

    /// <summary>
    ///     Horizontal uncontained row where item widths vary by aspect ratio.
    /// </summary>
    UncontainedMultiAspectRatio = 2,

    /// <summary>
    ///     Horizontal contained layout with one dominant item and a trailing preview.
    /// </summary>
    Hero = 3,

    /// <summary>
    ///     Horizontal contained hero layout centered between previews.
    /// </summary>
    CenterAlignedHero = 4,

    /// <summary>
    ///     Vertical edge-to-edge layout showing one item per viewport.
    /// </summary>
    FullScreen = 5
}
