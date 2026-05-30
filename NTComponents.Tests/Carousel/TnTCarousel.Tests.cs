using Microsoft.AspNetCore.Components;
using RippleTestingUtility = NTComponents.Tests.TestingUtility.TestingUtility;

namespace NTComponents.Tests.Carousel;

public class TnTCarousel_Tests : BunitContext {

    public TnTCarousel_Tests() {
        RippleTestingUtility.SetupRippleEffectModule(this);
        var carouselModule = JSInterop.SetupModule("./_content/NTComponents/Carousel/TnTCarousel.razor.js");
        carouselModule.SetupVoid("onLoad", _ => true);
        carouselModule.SetupVoid("onUpdate", _ => true);
        carouselModule.SetupVoid("onDispose", _ => true);
        var itemModule = JSInterop.SetupModule("./_content/NTComponents/Carousel/TnTCarouselItem.razor.js");
        itemModule.SetupVoid("onLoad", _ => true);
        itemModule.SetupVoid("onUpdate", _ => true);
        itemModule.SetupVoid("onDispose", _ => true);
    }

    [Fact, Trait("Component", "Carousel")]
    public void AdditionalAttributes_Class_Merged() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "class", "extra" } };
        var items = BuildItems((1, "One", false, true, null));
        // Act
        var cut = Render<TnTCarousel>(p => p.Add(c => c.AdditionalAttributes, attrs).AddChildContent(items));
        var cls = cut.Find("tnt-carousel").GetAttribute("class")!;
        // Assert
        cls.Should().Contain("extra").And.Contain("tnt-carousel");
    }

    [Fact, Trait("Component", "Carousel")]
    public void AdditionalAttributes_Style_Merged() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "style", "margin:3px" } };
        var items = BuildItems((1, "One", false, true, null));
        // Act
        var cut = Render<TnTCarousel>(p => p.Add(c => c.BackgroundColor, TnTColor.Primary).Add(c => c.AdditionalAttributes, attrs).AddChildContent(items));
        var style = cut.Find("tnt-carousel").GetAttribute("style")!;
        // Assert
        style.Should().Contain("margin:3px");
        style.Should().Contain("--tnt-carousel-bg-color");
    }

    [Fact, Trait("Component", "Carousel")]
    public void AutoPlayInterval_And_Dragging_Attributes_Render() {
        // Arrange
        var items = BuildItems((1, "One", false, true, null));
        // Act
        var cut = Render<TnTCarousel>(p => p.Add(c => c.AutoPlayInterval, 5000).Add(c => c.AllowDragging, false).AddChildContent(items));
        var root = cut.Find("tnt-carousel");
        // Assert
        root.GetAttribute("tnt-auto-play-interval")!.Should().Be("5000");
        // When AllowDragging is false the attribute should be absent.
        root.HasAttribute("tnt-allow-dragging").Should().BeFalse();
    }

    [Fact, Trait("Component", "Carousel")]
    public void AutoPlayInterval_Null_Does_Not_Render_Attribute() {
        // Arrange
        var items = BuildItems((1, "One", false, true, null));
        // Act
        var cut = Render<TnTCarousel>(p => p.AddChildContent(items));
        // Assert
        cut.Find("tnt-carousel").HasAttribute("tnt-auto-play-interval").Should().BeFalse();
    }

    [Fact, Trait("Component", "Carousel")]
    public void BackgroundColor_Adds_Style_Variable() {
        // Arrange
        var items = BuildItems((1, "One", false, true, null));
        // Act
        var cut = Render<TnTCarousel>(p => p.Add(c => c.BackgroundColor, TnTColor.Primary).AddChildContent(items));
        // Assert
        cut.Find("tnt-carousel").GetAttribute("style")!.Should().Contain("--tnt-carousel-bg-color:var(--tnt-color-primary)");
    }

    [Fact, Trait("Component", "Carousel")]
    public void CenterAlignedHero_With_Snapping_Adds_Centered_And_Snapping_Classes() {
        // Arrange
        var items = BuildItems((1, "One", false, true, null));
        // Act
        var cut = Render<TnTCarousel>(p => p.Add(c => c.Appearance, CarouselAppearance.CenterAlignedHero).Add(c => c.EnableSnapping, true).AddChildContent(items));
        var cls = cut.Find("tnt-carousel").GetAttribute("class")!;
        // Assert
        cls.Should().Contain("tnt-carousel-centered");
        cls.Should().Contain("tnt-carousel-snapping");
    }

    [Fact, Trait("Component", "Carousel")]
    public void CenterAlignedHero_Without_Snapping_Does_Not_Add_Centered_Class() {
        // Arrange
        var items = BuildItems((1, "One", false, true, null));
        // Act
        var cut = Render<TnTCarousel>(p => p.Add(c => c.Appearance, CarouselAppearance.CenterAlignedHero).Add(c => c.EnableSnapping, false).AddChildContent(items));
        // Assert
        cut.Find("tnt-carousel").GetAttribute("class")!.Should().NotContain("tnt-carousel-centered");
    }

    [Fact, Trait("Component", "Carousel")]
    public void Clickable_Item_Renders_Ripple_Component() {
        // Arrange
        var items = BuildItems((1, "One", true, true, null));
        // Act
        var cut = Render<TnTCarousel>(p => p.AddChildContent(items));
        // Assert
        cut.Find("tnt-carousel-item").InnerHtml.Should().Contain("tnt-ripple-effect");
    }

    [Fact, Trait("Component", "Carousel")]
    public void Default_AllowDragging_True() {
        // Arrange
        var items = BuildItems((1, "One", false, true, null));
        // Act
        var cut = Render<TnTCarousel>(p => p.AddChildContent(items));
        // Assert Default true -> attribute present (value may be empty string in markup rendering)
        cut.Find("tnt-carousel").HasAttribute("tnt-allow-dragging").Should().BeTrue();
    }

    [Fact, Trait("Component", "Carousel")]
    public void Default_Appearance_Is_MultiBrowse() {
        // Arrange
        var items = BuildItems((1, "One", false, true, null));
        // Act
        var cut = Render<TnTCarousel>(p => p.AddChildContent(items));
        // Assert
        cut.Instance.Appearance.Should().Be(CarouselAppearance.MultiBrowse);
        cut.Find("tnt-carousel").GetAttribute("tnt-layout").Should().Be("MultiBrowse");
    }

    [Fact, Trait("Component", "Carousel")]
    public void Hero_Appearance_Adds_Hero_Class_To_Root_And_Items() {
        // Arrange
        var items = BuildItems((1, "One", false, true, null));
        // Act
        var cut = Render<TnTCarousel>(p => p.Add(c => c.Appearance, CarouselAppearance.Hero).AddChildContent(items));
        var rootClass = cut.Find("tnt-carousel").GetAttribute("class")!;
        // Assert
        rootClass.Should().Contain("tnt-carousel-hero");
        cut.Find("tnt-carousel-item").GetAttribute("class")!.Should().Contain("tnt-carousel-hero");
    }

    [Theory, Trait("Component", "Carousel")]
    [InlineData(CarouselAppearance.MultiBrowse)]
    [InlineData(CarouselAppearance.Uncontained)]
    [InlineData(CarouselAppearance.UncontainedMultiAspectRatio)]
    [InlineData(CarouselAppearance.Hero)]
    [InlineData(CarouselAppearance.CenterAlignedHero)]
    [InlineData(CarouselAppearance.FullScreen)]
    public void M3_Appearance_Renders_Layout_Attribute(CarouselAppearance appearance) {
        // Arrange
        var items = BuildItems((1, "One", false, true, null));
        // Act
        var cut = Render<TnTCarousel>(p => p.Add(c => c.Appearance, appearance).AddChildContent(items));
        // Assert
        cut.Find("tnt-carousel").GetAttribute("tnt-layout").Should().Be(appearance.ToString());
    }

    [Fact, Trait("Component", "Carousel")]
    public void Item_AdditionalAttributes_Merged() {
        // Arrange
        var itemAttrs = new Dictionary<string, object> { { "class", "item-extra" }, { "style", "margin:4px" } };
        var items = BuildItems((1, "One", false, true, itemAttrs));
        // Act
        var cut = Render<TnTCarousel>(p => p.AddChildContent(items));
        var div = cut.Find("div.tnt-carousel-item-content");
        // Assert
        div.GetAttribute("class")!.Should().Contain("item-extra");
        div.GetAttribute("style")!.Should().Contain("margin:4px");
    }

    [Fact, Trait("Component", "Carousel")]
    public void Item_OnClick_Invokes_Callback() {
        // Arrange
        var clicked = 0;
        RenderFragment child = b => {
            b.OpenComponent<TnTCarouselItem>(0);
            b.AddAttribute(1, nameof(TnTCarouselItem.Order), 1);
            b.AddAttribute(2, nameof(TnTCarouselItem.OnClickCallback), EventCallback.Factory.Create(this, () => clicked++));
            b.AddAttribute(3, nameof(TnTCarouselItem.ChildContent), (RenderFragment)(c => c.AddContent(0, "One")));
            b.CloseComponent();
        };
        // Act
        var cut = Render<TnTCarousel>(p => p.AddChildContent(child));
        cut.Find("div.tnt-carousel-item-content").Click();
        // Assert
        clicked.Should().Be(1);
    }

    [Fact, Trait("Component", "Carousel")]
    public void Non_Clickable_Item_No_Ripple() {
        // Arrange
        var items = BuildItems((1, "One", false, true, null));
        // Act
        var cut = Render<TnTCarousel>(p => p.AddChildContent(items));
        // Assert
        cut.Find("tnt-carousel-item").InnerHtml.Should().NotContain("tnt-ripple-effect");
    }

    [Fact, Trait("Component", "Carousel")]
    public void Null_BackgroundColor_Does_Not_Add_Style_Variable() {
        // Arrange
        var items = BuildItems((1, "One", false, true, null));
        // Act
        var cut = Render<TnTCarousel>(p => p.AddChildContent(items));
        var style = cut.Find("tnt-carousel").GetAttribute("style") ?? string.Empty;
        // Assert
        style.Should().NotContain("--tnt-carousel-bg-color");
    }

    [Fact, Trait("Component", "Carousel")]
    public void Orders_Items_By_Order_Property() {
        // Arrange
        var items = BuildItems((5, "Five", false, true, null), (1, "One", false, true, null), (3, "Three", false, true, null));
        // Act
        var cut = Render<TnTCarousel>(p => p.AddChildContent(items));
        var texts = cut.FindAll("tnt-carousel-item").Select(e => e.TextContent.Trim()).ToArray();
        // Assert
        texts.Should().ContainInOrder("One", "Three", "Five");
    }

    [Fact, Trait("Component", "Carousel")]
    public void Removing_Item_Rerenders_List() {
        // Arrange
        var firstItems = BuildItems((1, "One", false, true, null), (2, "Two", false, true, null));
        var secondItems = BuildItems((1, "One", false, true, null));
        // Act
        var firstRender = Render<TnTCarousel>(p => p.AddChildContent(firstItems));
        var secondRender = Render<TnTCarousel>(p => p.AddChildContent(secondItems));
        // Assert
        firstRender.FindAll("tnt-carousel-item").Count.Should().Be(2);
        secondRender.FindAll("tnt-carousel-item").Count.Should().Be(1);
    }

    [Fact, Trait("Component", "Carousel")]
    public void Renders_Carousel_With_Items() {
        // Arrange
        var items = BuildItems((2, "Two", false, true, null), (1, "One", false, true, null));
        // Act
        var cut = Render<TnTCarousel>(p => p.AddChildContent(items));
        // Assert
        cut.FindAll("tnt-carousel-item").Count.Should().Be(2);
        cut.Find("tnt-carousel").GetAttribute("class")!.Should().Contain("tnt-carousel");
    }

    [Fact, Trait("Component", "Carousel")]
    public void Renders_No_Embedded_Navigation_Buttons() {
        // Arrange
        var items = BuildItems((1, "One", false, true, null), (2, "Two", false, true, null));
        // Act
        var cut = Render<TnTCarousel>(p => p.AddChildContent(items));
        // Assert
        cut.FindAll(".tnt-carousel-button").Should().BeEmpty();
        cut.FindAll(".tnt-carousel-prev-button").Should().BeEmpty();
        cut.FindAll(".tnt-carousel-next-button").Should().BeEmpty();
    }

    [Fact, Trait("Component", "Carousel")]
    public void Ripple_Disabled_Removes_Ripple_Component() {
        // Arrange
        var items = BuildItems((1, "One", true, false, null));
        // Act
        var cut = Render<TnTCarousel>(p => p.AddChildContent(items));
        // Assert
        cut.Find("tnt-carousel-item").InnerHtml.Should().NotContain("tnt-ripple-effect");
    }

    [Fact, Trait("Component", "Carousel")]
    public void Same_Order_Preserves_Insertion_Order() {
        // Arrange
        var items = BuildItems((1, "First", false, true, null), (1, "Second", false, true, null), (1, "Third", false, true, null));
        // Act
        var cut = Render<TnTCarousel>(p => p.AddChildContent(items));
        var texts = cut.FindAll("tnt-carousel-item").Select(e => e.TextContent.Trim()).ToArray();
        // Assert
        texts.Should().ContainInOrder("First", "Second", "Third");
    }

    [Fact, Trait("Component", "Carousel")]
    public void Snapping_False_Removes_Snapping_Class() {
        // Arrange
        var items = BuildItems((1, "One", false, true, null));
        // Act
        var cut = Render<TnTCarousel>(p => p.Add(c => c.EnableSnapping, false).AddChildContent(items));
        // Assert
        cut.Find("tnt-carousel").GetAttribute("class")!.Should().NotContain("tnt-carousel-snapping");
    }

    private RenderFragment BuildItems(params (int order, string text, bool clickable, bool enableRipple, Dictionary<string, object>? additional)[] items) => b => {
        foreach (var (order, text, clickable, enableRipple, additional) in items) {
            b.OpenComponent<TnTCarouselItem>(0);
            b.AddAttribute(10, nameof(TnTCarouselItem.Order), order);
            if (clickable) {
                b.AddAttribute(20, nameof(TnTCarouselItem.OnClickCallback), EventCallback.Factory.Create(this, () => { }));
            }
            b.AddAttribute(30, nameof(TnTCarouselItem.EnableRipple), enableRipple);
            if (additional is not null) {
                b.AddAttribute(40, nameof(TnTCarouselItem.AdditionalAttributes), additional);
            }

            b.AddAttribute(50, nameof(TnTCarouselItem.ChildContent), (RenderFragment)(c => c.AddContent(0, text)));
            b.CloseComponent();
        }
    };
}
