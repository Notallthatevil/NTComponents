using Microsoft.AspNetCore.Components;
using RippleTestingUtility = NTComponents.Tests.TestingUtility.TestingUtility;

namespace NTComponents.Tests.Carousel;

public class NTCarouselItem_Tests : BunitContext {

    public NTCarouselItem_Tests() {
        // Arrange (global) ripple module
        RippleTestingUtility.SetupRippleEffectModule(this);
        // Arrange JS modules for carousel & item
        var carouselModule = JSInterop.SetupModule("./_content/NTComponents/Carousel/NTCarousel.razor.js");
        carouselModule.SetupVoid("onLoad", _ => true);
        carouselModule.SetupVoid("onUpdate", _ => true);
        carouselModule.SetupVoid("onDispose", _ => true);
        var itemModule = JSInterop.SetupModule("./_content/NTComponents/Carousel/NTCarouselItem.razor.js");
        itemModule.SetupVoid("onLoad", _ => true);
        itemModule.SetupVoid("onUpdate", _ => true);
        itemModule.SetupVoid("onDispose", _ => true);
    }

    [Fact]
    public void AdditionalAttributes_Class_And_Style_Merged() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "class", "extra" }, { "style", "margin:4px" } };
        RenderFragment items = b => {
            b.OpenComponent<NTCarouselItem>(0);
            b.AddAttribute(1, nameof(NTCarouselItem.Order), 1);
            b.AddAttribute(2, nameof(NTCarouselItem.AdditionalAttributes), attrs);
            b.AddAttribute(3, nameof(NTCarouselItem.ChildContent), (RenderFragment)(c => c.AddContent(0, "Attr")));
            b.CloseComponent();
        };
        // Act
        var cut = Render<NTCarousel>(p => p.AddChildContent(items));
        var div = cut.Find("div.tnt-carousel-item-content");
        // Assert
        div.GetAttribute("class")!.Should().Contain("extra").And.Contain("tnt-carousel-item-content");
        div.GetAttribute("style")!.Should().Contain("margin:4px");
    }

    [Fact]
    public void Background_Image_Sets_Style() {
        // Arrange
        RenderFragment items = b => {
            b.OpenComponent<NTCarouselItem>(0);
            b.AddAttribute(1, nameof(NTCarouselItem.Order), 1);
            b.AddAttribute(2, nameof(NTCarouselItem.BackgroundImageSrc), "image.png");
            b.AddAttribute(3, nameof(NTCarouselItem.ChildContent), (RenderFragment)(c => c.AddContent(0, "BG")));
            b.CloseComponent();
        };
        // Act
        var cut = Render<NTCarousel>(p => p.AddChildContent(items));
        var contentDiv = cut.Find("div.tnt-carousel-item-content");
        // Assert
        contentDiv.GetAttribute("style")!.Should().Contain("background-image:url('image.png')");
    }

    [Fact]
    public void Clickable_Item_Adds_tnt_interactable_Class_And_Ripple() {
        // Arrange
        RenderFragment items = b => {
            b.OpenComponent<NTCarouselItem>(0);
            b.AddAttribute(1, nameof(NTCarouselItem.Order), 1);
            b.AddAttribute(2, nameof(NTCarouselItem.OnClickCallback), EventCallback.Factory.Create(this, (Action)(() => { })));
            b.AddAttribute(3, nameof(NTCarouselItem.ChildContent), (RenderFragment)(c => c.AddContent(0, "Clickable")));
            b.CloseComponent();
        };
        // Act
        var cut = Render<NTCarousel>(p => p.AddChildContent(items));
        var div = cut.Find("div.tnt-carousel-item-content");
        // Assert
        div.GetAttribute("class")!.Should().Contain("tnt-interactable");
        // Ripple inside the item
        cut.Find("tnt-carousel-item").InnerHtml.Should().Contain("tnt-ripple-effect");
    }

    [Fact]
    public void Hero_Item_Gets_Hero_Class() {
        // Arrange
        RenderFragment items = b => {
            b.OpenComponent<NTCarouselItem>(0);
            b.AddAttribute(1, nameof(NTCarouselItem.Order), 1);
            b.AddAttribute(2, nameof(NTCarouselItem.ChildContent), (RenderFragment)(c => c.AddContent(0, "Hero")));
            b.CloseComponent();
        };
        // Act
        var cut = Render<NTCarousel>(p => p.Add(c => c.Appearance, CarouselAppearance.CenterAlignedHero).AddChildContent(items));
        // Assert
        cut.Find("tnt-carousel-item").GetAttribute("class")!.Should().Contain("tnt-carousel-hero");
    }

    [Fact]
    public void Item_Renders_With_Content() {
        // Arrange
        RenderFragment items = b => {
            b.OpenComponent<NTCarouselItem>(0);
            b.AddAttribute(1, nameof(NTCarouselItem.Order), 1);
            b.AddAttribute(2, nameof(NTCarouselItem.ChildContent), (RenderFragment)(c => c.AddContent(0, "Hello")));
            b.CloseComponent();
        };
        // Act
        var cut = Render(BuildCarousel(items));
        // Assert
        cut.Markup.Should().Contain("Hello");
        cut.Find("tnt-carousel-item");
    }

    [Fact]
    public void Non_Clickable_Item_No_Ripple_Component() {
        // Arrange
        RenderFragment items = b => {
            b.OpenComponent<NTCarouselItem>(0);
            b.AddAttribute(1, nameof(NTCarouselItem.Order), 1);
            b.AddAttribute(2, nameof(NTCarouselItem.ChildContent), (RenderFragment)(c => c.AddContent(0, "Plain")));
            b.CloseComponent();
        };
        // Act
        var cut = Render<NTCarousel>(p => p.AddChildContent(items));
        // Assert
        cut.Find("tnt-carousel-item").InnerHtml.Should().NotContain("tnt-ripple-effect");
    }

    [Fact]
    public void OnClick_Invokes_Callback() {
        // Arrange
        var clicked = 0;
        RenderFragment items = b => {
            b.OpenComponent<NTCarouselItem>(0);
            b.AddAttribute(1, nameof(NTCarouselItem.Order), 1);
            b.AddAttribute(2, nameof(NTCarouselItem.OnClickCallback), EventCallback.Factory.Create(this, (Action)(() => clicked++)));
            b.AddAttribute(3, nameof(NTCarouselItem.ChildContent), (RenderFragment)(c => c.AddContent(0, "Click")));
            b.CloseComponent();
        };
        // Act
        var cut = Render<NTCarousel>(p => p.AddChildContent(items));
        cut.Find("div.tnt-carousel-item-content").Click();
        // Assert
        clicked.Should().Be(1);
    }

    [Fact]
    public void Ripple_Disabled_Removes_Ripple_Component() {
        // Arrange
        RenderFragment items = b => {
            b.OpenComponent<NTCarouselItem>(0);
            b.AddAttribute(1, nameof(NTCarouselItem.Order), 1);
            b.AddAttribute(2, nameof(NTCarouselItem.OnClickCallback), EventCallback.Factory.Create(this, (Action)(() => { })));
            b.AddAttribute(3, nameof(NTCarouselItem.EnableRipple), false);
            b.AddAttribute(4, nameof(NTCarouselItem.ChildContent), (RenderFragment)(c => c.AddContent(0, "NoRipple")));
            b.CloseComponent();
        };
        // Act
        var cut = Render<NTCarousel>(p => p.AddChildContent(items));
        // Assert
        cut.Find("tnt-carousel-item").InnerHtml.Should().NotContain("tnt-ripple-effect");
    }

    private RenderFragment BuildCarousel(RenderFragment items) => b => {
        b.OpenComponent<NTCarousel>(0);
        b.AddAttribute(1, nameof(NTCarousel.ChildContent), items);
        b.CloseComponent();
    };
}
