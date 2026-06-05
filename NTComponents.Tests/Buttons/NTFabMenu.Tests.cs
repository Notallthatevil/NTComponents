using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using RippleTestingUtility = NTComponents.Tests.TestingUtility.TestingUtility;

namespace NTComponents.Tests.Buttons;

public class NTFabMenu_Tests : BunitContext {
    private static TnTIcon SampleIcon => MaterialIcon.Add;

    public NTFabMenu_Tests() {
        RippleTestingUtility.SetupRippleEffectModule(this);

        var fabMenuModule = JSInterop.SetupModule("./_content/NTComponents/Buttons/NTFabMenu.razor.js");
        fabMenuModule.SetupVoid("onLoad", _ => true);
        fabMenuModule.SetupVoid("onUpdate", _ => true);
        fabMenuModule.SetupVoid("onDispose", _ => true);
    }

    [Fact]
    public void Default_Render_Uses_Native_Popover_And_Ssr_Page_Script() {
        var cut = Render<ValidFabMenu>();

        var host = cut.Find("nt-fab-menu");
        var button = cut.Find(".nt-fab-menu-button");
        var panel = cut.Find(".nt-fab-menu-panel");

        host.GetAttribute("class")!.Should().Contain("nt-fab-menu-placement-lower-right");
        host.GetAttribute("class")!.Should().Contain("tnt-size-s");
        host.GetAttribute("class")!.Should().NotContain("nt-fab-menu-color-");
        button.GetAttribute("type")!.Should().Be("button");
        button.GetAttribute("aria-label")!.Should().Be("Create options");
        button.GetAttribute("aria-haspopup")!.Should().Be("menu");
        button.GetAttribute("aria-expanded")!.Should().Be("false");
        button.GetAttribute("popovertarget").Should().Be(panel.GetAttribute("id"));
        button.GetAttribute("popovertargetaction")!.Should().Be("toggle");
        panel.GetAttribute("popover")!.Should().Be("auto");
        panel.GetAttribute("role")!.Should().Be("menu");
        cut.Find("tnt-page-script").GetAttribute("src")!.Should().Be("./_content/NTComponents/Buttons/NTFabMenu.razor.js");
    }

    [Fact]
    public void Requires_Icon_Parameter() {
        var render = () => Render<NTFabMenu>(parameters => parameters
            .Add(x => x.AriaLabel, "Create options")
            .AddChildContent<NTFabMenuButtonItem>(item => item.Add(x => x.Label, "Draft"))
            .AddChildContent<NTFabMenuButtonItem>(item => item.Add(x => x.Label, "Import")));

        render.Should().Throw<ArgumentNullException>()
            .WithMessage("*NTFabMenu requires a non-null Icon parameter*");
    }

    [Fact]
    public void Requires_AriaLabel() {
        var render = () => Render<NTFabMenu>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .AddChildContent<NTFabMenuButtonItem>(item => item.Add(x => x.Label, "Draft"))
            .AddChildContent<NTFabMenuButtonItem>(item => item.Add(x => x.Label, "Import")));

        render.Should().Throw<ArgumentException>()
            .WithMessage("*requires a non-empty AriaLabel*");
    }

    [Fact]
    public void Requires_At_Least_Two_Items() {
        var render = () => Render<NTFabMenu>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Create options")
            .AddChildContent<NTFabMenuButtonItem>(item => item.Add(x => x.Label, "Draft")));

        render.Should().Throw<InvalidOperationException>()
            .WithMessage("*requires 2 to 6 NTFabMenuButtonItem or NTFabMenuAnchorItem children*");
    }

    [Fact]
    public void Allows_Up_To_Six_Items() {
        var cut = Render<SixItemFabMenu>();

        cut.FindAll(".nt-fab-menu-item").Should().HaveCount(6);
    }

    [Fact]
    public void More_Than_Six_Items_Throws() {
        var render = () => Render<SevenItemFabMenu>();

        render.Should().Throw<InvalidOperationException>()
            .WithMessage("*requires 2 to 6 NTFabMenuButtonItem or NTFabMenuAnchorItem children*");
    }

    [Fact]
    public void Button_Item_Requires_Label() {
        var item = new NTFabMenuButtonItem {
            Parent = new NTFabMenu()
        };
        var parameters = ParameterView.Empty;

        var render = () => item.SetParametersAsync(parameters).GetAwaiter().GetResult();

        render.Should().Throw<InvalidOperationException>()
            .WithMessage("*NTFabMenuButtonItem requires a non-empty Label*");
    }

    [Fact]
    public void Anchor_Item_Requires_Href() {
        var item = new NTFabMenuAnchorItem {
            Parent = new NTFabMenu()
        };
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?> {
            [nameof(NTFabMenuAnchorItem.Label)] = "Import"
        });

        var render = () => item.SetParametersAsync(parameters).GetAwaiter().GetResult();

        render.Should().Throw<InvalidOperationException>()
            .WithMessage("*NTFabMenuAnchorItem requires a non-empty Href*");
    }

    [Fact]
    public void Button_Item_With_Native_Onclick_Preserves_Ssr_Attribute() {
        var cut = Render<NativeOnClickFabMenu>();

        var item = cut.Find(".nt-fab-menu-item");

        item.GetAttribute("onclick")!.Should().Be("window.createDraft()");
    }

    [Fact]
    public void Button_Item_Without_Callback_Does_Not_Render_Onclick_Attribute() {
        var cut = Render<ValidFabMenu>();

        var firstItem = cut.Find(".nt-fab-menu-item");

        firstItem.HasAttribute("onclick").Should().BeFalse();
    }

    [Fact]
    public void Button_Item_With_Callback_Invokes_Callback() {
        var clicked = 0;
        var cut = Render<NTFabMenu>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Create options")
            .AddChildContent<NTFabMenuButtonItem>(item => item
                .Add(x => x.Icon, MaterialIcon.Edit)
                .Add(x => x.Label, "Draft")
                .Add(x => x.OnClickCallback, EventCallback.Factory.Create<MouseEventArgs>(this, () => clicked++)))
            .AddChildContent<NTFabMenuButtonItem>(item => item
                .Add(x => x.Icon, MaterialIcon.Upload)
                .Add(x => x.Label, "Import")));

        cut.Find(".nt-fab-menu-item").Click();

        clicked.Should().Be(1);
    }

    [Fact]
    public void Anchor_Item_Renders_Href_Target_And_Label() {
        var cut = Render<NTFabMenu>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Create options")
            .AddChildContent<NTFabMenuAnchorItem>(item => item
                .Add(x => x.Icon, MaterialIcon.Upload)
                .Add(x => x.Label, "Import")
                .Add(x => x.Href, "/imports")
                .Add(x => x.Target, "_blank"))
            .AddChildContent<NTFabMenuButtonItem>(item => item
                .Add(x => x.Icon, MaterialIcon.Edit)
                .Add(x => x.Label, "Draft")));

        var anchor = cut.Find("a.nt-fab-menu-item");

        anchor.GetAttribute("href")!.Should().Be("/imports");
        anchor.GetAttribute("target")!.Should().Be("_blank");
        anchor.GetAttribute("aria-label")!.Should().Be("Import");
    }

    [Fact]
    public void Custom_Colors_Render_As_Css_Variables() {
        var cut = Render<NTFabMenu>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Create options")
            .Add(x => x.BackgroundColor, TnTColor.SecondaryContainer)
            .Add(x => x.TextColor, TnTColor.OnSecondaryContainer)
            .Add(x => x.SelectedFabBackgroundColor, TnTColor.Secondary)
            .Add(x => x.SelectedFabTextColor, TnTColor.OnSecondary)
            .Add(x => x.MenuItemBackgroundColor, TnTColor.TertiaryContainer)
            .Add(x => x.MenuItemTextColor, TnTColor.OnTertiaryContainer)
            .AddChildContent<NTFabMenuButtonItem>(item => item.Add(x => x.Label, "Draft"))
            .AddChildContent<NTFabMenuButtonItem>(item => item.Add(x => x.Label, "Import")));

        var style = cut.Find("nt-fab-menu").GetAttribute("style");

        style.Should().Contain("--nt-fab-menu-fab-bg:var(--tnt-color-secondary-container)");
        style.Should().Contain("--nt-fab-menu-fab-fg:var(--tnt-color-on-secondary-container)");
        style.Should().Contain("--nt-fab-menu-selected-fab-bg:var(--tnt-color-secondary)");
        style.Should().Contain("--nt-fab-menu-selected-fab-fg:var(--tnt-color-on-secondary)");
        style.Should().Contain("--nt-fab-menu-item-bg:var(--tnt-color-tertiary-container)");
        style.Should().Contain("--nt-fab-menu-item-fg:var(--tnt-color-on-tertiary-container)");
    }

    [Theory]
    [InlineData(Size.Smallest, "tnt-size-s")]
    [InlineData(Size.Small, "tnt-size-s")]
    [InlineData(Size.Medium, "tnt-size-m")]
    [InlineData(Size.Large, "tnt-size-l")]
    [InlineData(Size.Largest, "tnt-size-l")]
    public void ButtonSize_Uses_Supported_Fab_Size(Size suppliedSize, string expectedClass) {
        var cut = Render<NTFabMenu>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Create options")
            .Add(x => x.ButtonSize, suppliedSize)
            .AddChildContent<NTFabMenuButtonItem>(item => item.Add(x => x.Label, "Draft"))
            .AddChildContent<NTFabMenuButtonItem>(item => item.Add(x => x.Label, "Import")));

        var classes = cut.Find("nt-fab-menu").GetAttribute("class")!;

        classes.Should().Contain(expectedClass);
        classes.Should().NotContain("tnt-size-xs");
        classes.Should().NotContain("tnt-size-xl");
    }

    private sealed class ValidFabMenu : ComponentBase {

        protected override void BuildRenderTree(RenderTreeBuilder builder) {
            builder.OpenComponent<NTFabMenu>(0);
            builder.AddAttribute(1, nameof(NTFabMenu.Icon), (object)(TnTIcon)MaterialIcon.Add);
            builder.AddAttribute(2, nameof(NTFabMenu.AriaLabel), "Create options");
            builder.AddAttribute(3, nameof(NTFabMenu.ChildContent), (RenderFragment)(childBuilder => {
                childBuilder.OpenComponent<NTFabMenuButtonItem>(0);
                childBuilder.AddAttribute(1, nameof(NTFabMenuButtonItem.Icon), (object)(TnTIcon)MaterialIcon.Edit);
                childBuilder.AddAttribute(2, nameof(NTFabMenuButtonItem.Label), "Draft");
                childBuilder.CloseComponent();

                childBuilder.OpenComponent<NTFabMenuButtonItem>(10);
                childBuilder.AddAttribute(11, nameof(NTFabMenuButtonItem.Icon), (object)(TnTIcon)MaterialIcon.Upload);
                childBuilder.AddAttribute(12, nameof(NTFabMenuButtonItem.Label), "Import");
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }

    private sealed class NativeOnClickFabMenu : ComponentBase {

        protected override void BuildRenderTree(RenderTreeBuilder builder) {
            builder.OpenComponent<NTFabMenu>(0);
            builder.AddAttribute(1, nameof(NTFabMenu.Icon), (object)(TnTIcon)MaterialIcon.Add);
            builder.AddAttribute(2, nameof(NTFabMenu.AriaLabel), "Create options");
            builder.AddAttribute(3, nameof(NTFabMenu.ChildContent), (RenderFragment)(childBuilder => {
                childBuilder.OpenComponent<NTFabMenuButtonItem>(0);
                childBuilder.AddAttribute(1, nameof(NTFabMenuButtonItem.Icon), (object)(TnTIcon)MaterialIcon.Edit);
                childBuilder.AddAttribute(2, nameof(NTFabMenuButtonItem.Label), "Draft");
                childBuilder.AddAttribute(3, "onclick", "window.createDraft()");
                childBuilder.CloseComponent();

                childBuilder.OpenComponent<NTFabMenuButtonItem>(10);
                childBuilder.AddAttribute(11, nameof(NTFabMenuButtonItem.Icon), (object)(TnTIcon)MaterialIcon.Upload);
                childBuilder.AddAttribute(12, nameof(NTFabMenuButtonItem.Label), "Import");
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }

    private sealed class SixItemFabMenu : ComponentBase {

        protected override void BuildRenderTree(RenderTreeBuilder builder) => RenderItemCount(builder, 6);
    }

    private sealed class SevenItemFabMenu : ComponentBase {

        protected override void BuildRenderTree(RenderTreeBuilder builder) => RenderItemCount(builder, 7);
    }

    private static void RenderItemCount(RenderTreeBuilder builder, int count) {
        builder.OpenComponent<NTFabMenu>(0);
        builder.AddAttribute(1, nameof(NTFabMenu.Icon), (object)(TnTIcon)MaterialIcon.Add);
        builder.AddAttribute(2, nameof(NTFabMenu.AriaLabel), "Create options");
        builder.AddAttribute(3, nameof(NTFabMenu.ChildContent), (RenderFragment)(childBuilder => {
            for (var i = 0; i < count; i++) {
                RenderItem(childBuilder, $"Item {i + 1}");
            }
        }));
        builder.CloseComponent();
    }

    private static void RenderItem(RenderTreeBuilder builder, string label) {
        builder.OpenComponent<NTFabMenuButtonItem>(0);
        builder.AddAttribute(1, nameof(NTFabMenuButtonItem.Icon), (object)(TnTIcon)MaterialIcon.Add);
        builder.AddAttribute(2, nameof(NTFabMenuButtonItem.Label), label);
        builder.CloseComponent();
    }
}
