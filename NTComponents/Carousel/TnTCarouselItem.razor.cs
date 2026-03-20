using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
///     Represents a single item inside a <see cref="TnTCarousel" />. The item hosts arbitrary render content and provides presentation options such as a background image, ordering, and click handling.
/// </summary>
/// <remarks>
///     Instances of <see cref="TnTCarouselItem" /> must be descendants of <see cref="TnTCarousel" />. During initialization the item registers itself with the parent carousel and unregisters on
///     disposal to keep the parent's item collection up to date.
/// </remarks>
public partial class TnTCarouselItem {

    /// <summary>
    ///     Optional URL to use as the background image for the item. When specified an inline background-image style will be emitted.
    /// </summary>
    [Parameter]
    public string? BackgroundImageSrc { get; set; }

    /// <summary>
    ///     Optional accessible name applied when the carousel item is clickable.
    /// </summary>
    [Parameter]
    public string? AriaLabel { get; set; }

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
    public override string? JsModulePath => "./_content/NTComponents/Carousel/TnTCarouselItem.razor.js";

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
    ///     Internal id assigned by the parent carousel to provide a stable ordering when multiple items share the same <see cref="Order" />. This value is managed by <see cref="TnTCarousel" /> and
    ///     should not be set by consumers.
    /// </summary>
    internal int? InternalId { get; set; }

    /// <summary>
    ///     The parent carousel that this item belongs to. Supplied via cascading parameter.
    /// </summary>
    [CascadingParameter]
    private TnTCarousel _carousel { get; set; } = default!;

    /// <summary>
    ///     Returns true when the parent carousel's appearance flags indicate hero layout.
    /// </summary>
    private bool _hero => _carousel?.Appearance.HasFlag(CarouselAppearance.Hero) == true;


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
            throw new InvalidOperationException($"{nameof(TnTCarouselItem)} must be a decendant of {nameof(TnTCarousel)}");
        }

        _carousel.AddChild(this);
    }

    private Task HandleKeyDownAsync(KeyboardEventArgs args) {
        if (OnClickCallback.HasDelegate && (args.Key == "Enter" || args.Key == " " || args.Key == "Spacebar")) {
            return OnClickCallback.InvokeAsync();
        }

        return Task.CompletedTask;
    }
}
