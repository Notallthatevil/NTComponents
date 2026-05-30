using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using RippleTestingUtility = NTComponents.Tests.TestingUtility.TestingUtility;

namespace NTComponents.Tests.Buttons;

public class NTSplitButton_Tests : BunitContext {

    public NTSplitButton_Tests() {
        RippleTestingUtility.SetupRippleEffectModule(this);

        var splitButtonModule = JSInterop.SetupModule("./_content/NTComponents/Buttons/NTSplitButton.razor.js");
        splitButtonModule.SetupVoid("onLoad", _ => true);
        splitButtonModule.SetupVoid("onUpdate", _ => true);
        splitButtonModule.SetupVoid("onDispose", _ => true);

        var menuModule = JSInterop.SetupModule("./_content/NTComponents/Menus/NTMenu.razor.js");
        menuModule.SetupVoid("onLoad", _ => true);
        menuModule.SetupVoid("onUpdate", _ => true);
        menuModule.SetupVoid("onDispose", _ => true);
    }

    [Fact]
    public void Default_Render_Uses_Filled_Variant_And_Button_Type() {
        var cut = Render<ValidSplitButton>();

        var actionButton = cut.Find(".nt-split-button-leading");

        cut.Find("nt-split-button").GetAttribute("class")!.Should().Contain("nt-split-button-filled");
        actionButton.GetAttribute("type")!.Should().Be("button");
        actionButton.TextContent.Should().Contain("Save");
    }

    [Fact]
    public void Empty_Label_Without_LeadingIcon_Throws() {
        var render = () => Render<NTSplitButton>(parameters => parameters
            .Add(x => x.Label, string.Empty)
            .AddChildContent<NTMenuButtonItem>(item => item.Add(x => x.Label, "Save draft")));

        render.Should().Throw<ArgumentException>()
            .WithMessage("*requires a non-empty Label unless a LeadingIcon is supplied*");
    }

    [Fact]
    public void Icon_Only_Action_Without_ActionAriaLabel_Throws() {
        var render = () => Render<NTSplitButton>(parameters => parameters
            .Add(x => x.LeadingIcon, MaterialIcon.Save)
            .AddChildContent<NTMenuButtonItem>(item => item.Add(x => x.Label, "Save draft")));

        render.Should().Throw<ArgumentException>()
            .WithMessage("*Icon-only NTSplitButton actions require a non-empty ActionAriaLabel*");
    }

    [Fact]
    public void Icon_Only_Action_With_ActionAriaLabel_Renders_Accessible_Label() {
        var cut = Render<NTSplitButton>(parameters => parameters
            .Add(x => x.LeadingIcon, MaterialIcon.Save)
            .Add(x => x.ActionAriaLabel, "Save")
            .AddChildContent<NTMenuButtonItem>(item => item.Add(x => x.Label, "Save draft")));

        var actionButton = cut.Find(".nt-split-button-leading");

        actionButton.GetAttribute("aria-label")!.Should().Be("Save");
        actionButton.QuerySelector(".nt-split-button-label").Should().BeNull();
    }

    [Theory]
    [InlineData(NTButtonVariant.Text)]
    [InlineData(NTButtonVariant.Outlined)]
    public void Transparent_Variants_With_Visible_BackgroundColor_Throw(NTButtonVariant variant) {
        var render = () => Render<NTSplitButton>(parameters => parameters
            .Add(x => x.Label, "Invalid")
            .Add(x => x.Variant, variant)
            .Add(x => x.BackgroundColor, TnTColor.Primary)
            .AddChildContent<NTMenuButtonItem>(item => item.Add(x => x.Label, "Save draft")));

        render.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{variant} split buttons must use a transparent BackgroundColor*");
    }

    [Theory]
    [InlineData(NTButtonVariant.Elevated)]
    [InlineData(NTButtonVariant.Filled)]
    [InlineData(NTButtonVariant.Tonal)]
    public void Contained_Variants_With_Transparent_BackgroundColor_Throw(NTButtonVariant variant) {
        var render = () => Render<NTSplitButton>(parameters => parameters
            .Add(x => x.Label, "Invalid")
            .Add(x => x.Variant, variant)
            .Add(x => x.BackgroundColor, TnTColor.Transparent)
            .AddChildContent<NTMenuButtonItem>(item => item.Add(x => x.Label, "Save draft")));

        render.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{variant} split buttons must use a visible container BackgroundColor*");
    }

    [Fact]
    public void Filled_Button_With_Elevation_Throws() {
        var render = () => Render<NTSplitButton>(parameters => parameters
            .Add(x => x.Label, "Invalid")
            .Add(x => x.Variant, NTButtonVariant.Filled)
            .Add(x => x.Elevation, NTElevation.Lowest)
            .AddChildContent<NTMenuButtonItem>(item => item.Add(x => x.Label, "Save draft")));

        render.Should().Throw<InvalidOperationException>()
            .WithMessage("*Filled split buttons must use None Elevation*");
    }

    [Fact]
    public void Elevated_Button_With_No_Elevation_Throws() {
        var render = () => Render<NTSplitButton>(parameters => parameters
            .Add(x => x.Label, "Invalid")
            .Add(x => x.Variant, NTButtonVariant.Elevated)
            .Add(x => x.Elevation, NTElevation.None)
            .AddChildContent<NTMenuButtonItem>(item => item.Add(x => x.Label, "Save draft")));

        render.Should().Throw<InvalidOperationException>()
            .WithMessage("*Elevated split buttons must use a non-zero Elevation*");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Menu_Colors_Must_Be_Visible(bool validateBackground) {
        var render = () => Render<NTSplitButton>(parameters => {
            parameters.Add(x => x.Label, "Invalid");

            if (validateBackground) {
                parameters.Add(x => x.MenuBackgroundColor, TnTColor.Transparent);
            }
            else {
                parameters.Add(x => x.MenuTextColor, TnTColor.Transparent);
            }

            parameters.AddChildContent<NTMenuButtonItem>(item => item.Add(x => x.Label, "Save draft"));
        });

        render.Should().Throw<InvalidOperationException>()
            .WithMessage(validateBackground
                ? "*MenuBackgroundColor must be a visible menu container color*"
                : "*MenuTextColor must be a visible menu content color*");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Selected_Menu_Colors_Must_Be_Visible(bool validateBackground) {
        var render = () => Render<NTSplitButton>(parameters => {
            parameters.Add(x => x.Label, "Invalid");

            if (validateBackground) {
                parameters.Add(x => x.MenuSelectedBackgroundColor, TnTColor.Transparent);
            }
            else {
                parameters.Add(x => x.MenuSelectedTextColor, TnTColor.Transparent);
            }

            parameters.AddChildContent<NTMenuButtonItem>(item => item.Add(x => x.Label, "Save draft"));
        });

        render.Should().Throw<InvalidOperationException>()
            .WithMessage(validateBackground
                ? "*MenuSelectedBackgroundColor must be a visible selected menu item container color*"
                : "*MenuSelectedTextColor must be a visible selected menu item content color*");
    }

    [Fact]
    public void Requires_At_Least_One_Actionable_Menu_Item() {
        var render = () => Render<NTSplitButton>(parameters => parameters.Add(x => x.Label, "Save"));

        render.Should().Throw<InvalidOperationException>()
            .WithMessage("*requires at least one NTMenuButtonItem, NTMenuAnchorItem, or NTMenuSubMenuItem child*");
    }

    [Fact]
    public void Divider_Only_Menu_Throws() {
        var render = () => Render<NTSplitButton>(parameters => parameters
            .Add(x => x.Label, "Save")
            .AddChildContent<NTMenuDividerItem>());

        render.Should().Throw<InvalidOperationException>()
            .WithMessage("*requires at least one NTMenuButtonItem, NTMenuAnchorItem, or NTMenuSubMenuItem child*");
    }

    [Fact]
    public void Button_Item_Requires_Label() {
        var item = new NTMenuButtonItem {
            Parent = new NTMenu()
        };
        var parameters = ParameterView.Empty;

        var render = () => item.SetParametersAsync(parameters).GetAwaiter().GetResult();

        render.Should().Throw<InvalidOperationException>()
            .WithMessage("*NTMenuButtonItem requires a non-empty Label*");
    }

    [Fact]
    public void Anchor_Item_Requires_Href() {
        var item = new NTMenuAnchorItem {
            Parent = new NTMenu()
        };
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?> {
            [nameof(NTMenuAnchorItem.Label)] = "Export"
        });

        var render = () => item.SetParametersAsync(parameters).GetAwaiter().GetResult();

        render.Should().Throw<InvalidOperationException>()
            .WithMessage("*NTMenuAnchorItem requires a non-empty Href*");
    }

    [Fact]
    public void Menu_Item_Whitespace_AriaLabel_Falls_Back_To_Visible_Label() {
        var cut = Render<NTSplitButton>(parameters => parameters
            .Add(x => x.Label, "Save")
            .AddChildContent<NTMenuButtonItem>(item => item
                .Add(x => x.Label, "Save draft")
                .Add(x => x.AriaLabel, "   ")));

        cut.Find(".nt-menu-item").GetAttribute("aria-label")!.Should().Be("Save draft");
    }

    [Fact]
    public void Menu_Renders_Material_Menu_Classes_And_Ripple_Host() {
        var cut = Render<NTSplitButton>(parameters => parameters
            .Add(x => x.Label, "Save")
            .AddChildContent<NTMenuButtonItem>(item => item
                .Add(x => x.Label, "Save draft")
                .Add(x => x.Icon, MaterialIcon.Save)));

        cut.Find(".nt-split-button-menu-panel").ClassList.Should().Contain("nt-menu");
        cut.Find(".nt-split-button-menu-panel").ClassList.Should().Contain("nt-menu-placement-auto");
        cut.Find(".nt-menu-item").ClassList.Should().Contain("nt-menu-item");
        cut.Find(".nt-menu-item .nt-button-ripple-host").Should().NotBeNull();
    }

    [Fact]
    public void Selected_Menu_Item_Renders_Selected_State() {
        var cut = Render<NTSplitButton>(parameters => parameters
            .Add(x => x.Label, "Save")
            .AddChildContent<NTMenuButtonItem>(item => item
                .Add(x => x.Label, "Save draft")
                .Add(x => x.Selected, true)));

        var item = cut.Find(".nt-menu-item");

        item.ClassList.Should().Contain("nt-menu-item-selected");
        item.GetAttribute("aria-selected")!.Should().Be("true");
    }

    [Fact]
    public void Menu_SubMenu_Item_Renders_Nested_Anchored_Menu() {
        var cut = Render<NTSplitButton>(parameters => parameters
            .Add(x => x.Label, "Save")
            .AddChildContent<NTMenuSubMenuItem>(subMenu => subMenu
                .Add(x => x.Label, "More actions")
                .Add(x => x.ChildContent, (RenderFragment)(builder => {
                    builder.OpenComponent<NTMenuButtonItem>(0);
                    builder.AddAttribute(1, nameof(NTMenuButtonItem.Label), "Schedule send");
                    builder.CloseComponent();
                }))));

        var trigger = cut.Find("button[data-nt-menu-submenu-trigger]");
        var nestedMenu = cut.Find("nt-menu.nt-menu-submenu");

        trigger.GetAttribute("aria-haspopup")!.Should().Be("menu");
        trigger.GetAttribute("popovertarget").Should().Be(nestedMenu.Id);
        nestedMenu.GetAttribute("data-submenu")!.Should().Be("true");
        nestedMenu.TextContent.Should().Contain("Schedule send");
    }

    private sealed class ValidSplitButton : ComponentBase {

        protected override void BuildRenderTree(RenderTreeBuilder builder) {
            builder.OpenComponent<NTSplitButton>(0);
            builder.AddAttribute(1, nameof(NTSplitButton.Label), "Save");
            builder.AddAttribute(2, nameof(NTSplitButton.ChildContent), (RenderFragment)(childBuilder => {
                childBuilder.OpenComponent<NTMenuButtonItem>(0);
                childBuilder.AddAttribute(1, nameof(NTMenuButtonItem.Label), "Save draft");
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }
}
