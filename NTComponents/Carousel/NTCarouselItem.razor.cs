using Microsoft.AspNetCore.Components;
using NTComponents.CodeDocumentation;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
///     A slide rendered by <see cref="NTCarousel" /> with separate media and stable content layers.
/// </summary>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.ProgressivelyEnhanced,
    CompatibilitySummary = "Renders a labeled, focusable slide before browser enhancement.",
    CompatibilityDetails = "The item remains readable in static SSR. Keyline sizing, media parallax, roving focus, and pointer settlement are supplied by the parent browser controller.")]
public partial class NTCarouselItem {

    /// <summary>
    ///     Gets or sets the content-specific accessible item name. Position and total are appended automatically.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public string AriaLabel { get; set; } = default!;

    /// <summary>
    ///     Gets or sets the item width divided by its height for the multi-aspect-ratio layout.
    /// </summary>
    [Parameter]
    public double? AspectRatio { get; set; }

    /// <summary>
    ///     Gets or sets an optional background image URL for the parallax media layer.
    /// </summary>
    [Parameter]
    public string? BackgroundImageSrc { get; set; }

    /// <summary>
    ///     Gets or sets the stable content rendered above the media layer.
    /// </summary>
    [Parameter, EditorRequired]
    public RenderFragment ChildContent { get; set; } = default!;

    /// <summary>
    ///     Gets or sets whether this item is unavailable for activation.
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("nt-carousel-item")
        .AddClass("nt-carousel-item-clickable", OnClickCallback.HasDelegate)
        .AddClass("nt-carousel-item-disabled", Disabled)
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddVariable("nt-carousel-item-aspect-ratio", AspectRatio?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "1")
        .Build();

    /// <summary>
    ///     Gets or sets whether clickable items display the shared ripple effect.
    /// </summary>
    [Parameter]
    public bool EnableRipple { get; set; } = true;

    /// <summary>
    ///     Gets or sets arbitrary media content, such as an image or muted video, rendered in the parallax layer.
    /// </summary>
    [Parameter]
    public RenderFragment? MediaContent { get; set; }

    /// <summary>
    ///     Gets or sets the callback invoked when the item is activated.
    /// </summary>
    [Parameter]
    public EventCallback OnClickCallback { get; set; }

    [CascadingParameter]
    private NTCarousel _carousel { get; set; } = default!;

    private string? MediaStyle => CssStyleBuilder.Create()
        .AddStyle("background-image", $"url('{BackgroundImageSrc}')", !string.IsNullOrWhiteSpace(BackgroundImageSrc))
        .Build();

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();
        if (AspectRatio is < (9d / 16d) or > (16d / 9d)) {
            throw new ArgumentOutOfRangeException(nameof(AspectRatio), AspectRatio, "The Material 3 multi-aspect ratio must be between 9:16 and 16:9.");
        }

        if (string.IsNullOrWhiteSpace(AriaLabel)) {
            throw new InvalidOperationException($"{nameof(AriaLabel)} is required for {nameof(NTCarouselItem)}.");
        }

        if (_carousel is null) {
            throw new InvalidOperationException($"{nameof(NTCarouselItem)} must be a descendant of {nameof(NTCarousel)}.");
        }
    }
}
