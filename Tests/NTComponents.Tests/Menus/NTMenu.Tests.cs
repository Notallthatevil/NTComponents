using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace NTComponents.Tests.Menus;

public class NTMenu_Tests : BunitContext {

    public NTMenu_Tests() {
        var module = JSInterop.SetupModule("./_content/NTComponents/Menus/NTMenu.razor.js");
        module.SetupVoid("onLoad", _ => true);
        module.SetupVoid("onUpdate", _ => true);
        module.SetupVoid("onDispose", _ => true);
    }

    [Theory]
    [InlineData(nameof(NTMenu.AriaLabel))]
    [InlineData(nameof(NTMenu.ChildContent))]
    public void Required_Parameters_Have_EditorRequired_Attribute(string propertyName) {
        var property = typeof(NTMenu).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);

        property.Should().NotBeNull();
        property!.GetCustomAttribute<EditorRequiredAttribute>().Should().NotBeNull();
    }

    [Fact]
    public void Label_Item_Label_Has_EditorRequired_Attribute() {
        var property = typeof(NTMenuLabelItem).GetProperty(nameof(NTMenuLabelItem.Label), BindingFlags.Instance | BindingFlags.Public);

        property.Should().NotBeNull();
        property!.GetCustomAttribute<EditorRequiredAttribute>().Should().NotBeNull();
    }

    [Fact]
    public void ElementClass_Uses_Default_Medium_Elevation() {
        var menu = new NTMenu();

        menu.ElementClass.Should().Contain("nt-elevation-medium");
    }

    [Fact]
    public void ElementClass_Uses_Configured_Elevation() {
        var menu = new NTMenu {
            Elevation = NTElevation.High
        };

        menu.ElementClass.Should().Contain("nt-elevation-high");
        menu.ElementClass.Should().NotContain("nt-elevation-medium");
    }

    [Fact]
    public void ElementClass_Uses_Compact_Appearance_Class() {
        var menu = new NTMenu {
            Appearance = NTMenuAppearance.Compact
        };

        menu.ElementClass.Should().Contain("nt-menu-compact");
    }

    [Fact]
    public void ElementClass_Does_Not_Render_Compact_Class_By_Default() {
        var menu = new NTMenu();

        menu.ElementClass.Should().NotContain("nt-menu-compact");
    }

    [Fact]
    public void Render_Puts_Menu_Items_Inside_Surface_Wrapper() {
        var cut = Render<NTMenu>(parameters => parameters
            .Add(x => x.AriaLabel, "Actions")
            .AddChildContent<NTMenuButtonItem>(item => item.Add(x => x.Label, "Rename")));

        cut.Find("nt-menu").ClassList.Should().Contain("nt-elevation-medium");
        cut.Find("nt-menu > .nt-menu-surface > .nt-menu-content > .nt-menu-item").TextContent.Should().Contain("Rename");
    }

    // Behavior source: NTMenuButtonItem.OnClickCallback XML documentation and NTMenu's NTDocumentation interaction contract.
    [Fact]
    public void Enabled_Button_Item_Invokes_Its_Click_Callback() {
        var clickCount = 0;
        var cut = Render<NTMenu>(parameters => parameters
            .Add(x => x.AriaLabel, "Actions")
            .AddChildContent<NTMenuButtonItem>(item => {
                item.Add(x => x.Label, "Rename");
                item.Add(x => x.OnClickCallback, EventCallback.Factory.Create<MouseEventArgs>(this, () => clickCount++));
            }));

        cut.Find("button.nt-menu-item").Click();

        clickCount.Should().Be(1);
    }

    // Behavior source: NTMenu.Disabled XML documentation states that the menu and registered items are disabled.
    [Fact]
    public void Disabled_Menu_Disables_Button_And_Anchor_Interactions() {
        var clickCount = 0;
        var cut = Render<NTMenu>(parameters => parameters
            .Add(x => x.ElementId, "actions-menu")
            .Add(x => x.AriaLabel, "Actions")
            .Add(x => x.Disabled, true)
            .AddChildContent(builder => {
                builder.OpenComponent<NTMenuButtonItem>(0);
                builder.AddComponentParameter(1, nameof(NTMenuButtonItem.Label), "Rename");
                builder.AddComponentParameter(2, nameof(NTMenuButtonItem.OnClickCallback), EventCallback.Factory.Create<MouseEventArgs>(this, () => clickCount++));
                builder.CloseComponent();

                builder.OpenComponent<NTMenuAnchorItem>(3);
                builder.AddComponentParameter(4, nameof(NTMenuAnchorItem.Label), "Open details");
                builder.AddComponentParameter(5, nameof(NTMenuAnchorItem.Href), "/details");
                builder.CloseComponent();
            }));

        var button = cut.Find("button.nt-menu-item");
        button.Click();
        var anchor = cut.Find("a.nt-menu-item");

        clickCount.Should().Be(0);
        button.GetAttribute("aria-disabled").Should().Be("true");
        button.HasAttribute("popovertarget").Should().BeFalse();
        anchor.GetAttribute("aria-disabled").Should().Be("true");
        anchor.GetAttribute("tabindex").Should().Be("-1");
        anchor.HasAttribute("href").Should().BeFalse();
    }

    // Behavior source: NTMenuAnchorItem Href, Target, AriaLabel, Icon, and Selected XML documentation define its enabled static-navigation contract.
    [Fact]
    public void Enabled_Selected_Anchor_Renders_Navigation_And_Accessibility_Contract() {
        var cut = Render<NTMenu>(parameters => parameters
            .Add(x => x.AriaLabel, "Actions")
            .AddChildContent<NTMenuAnchorItem>(item => {
                item.Add(x => x.Label, "Open details");
                item.Add(x => x.AriaLabel, "Open project details");
                item.Add(x => x.Href, "/details");
                item.Add(x => x.Target, "_blank");
                item.Add(x => x.Icon, MaterialIcon.OpenInNew);
                item.Add(x => x.Selected, true);
            }));

        var anchor = cut.Find("a.nt-menu-item");

        anchor.GetAttribute("href").Should().Be("/details");
        anchor.GetAttribute("target").Should().Be("_blank");
        anchor.GetAttribute("aria-label").Should().Be("Open project details");
        anchor.GetAttribute("aria-selected").Should().Be("true");
        anchor.HasAttribute("aria-disabled").Should().BeFalse();
        anchor.HasAttribute("tabindex").Should().BeFalse();
        anchor.QuerySelector(".nt-menu-item-icon").Should().NotBeNull();
    }

    // Behavior source: NTMenu.CloseOnContentClick XML documentation and the native popover rendering contract in NTMenu remarks.
    [Fact]
    public void CloseOnContentClick_False_Leaves_Button_Without_A_Close_Target() {
        var cut = Render<NTMenu>(parameters => parameters
            .Add(x => x.ElementId, "actions-menu")
            .Add(x => x.AriaLabel, "Actions")
            .Add(x => x.CloseOnContentClick, false)
            .AddChildContent<NTMenuButtonItem>(item => item.Add(x => x.Label, "Rename")));

        var menu = cut.Find("nt-menu");
        var button = cut.Find("button.nt-menu-item");

        menu.GetAttribute("data-close-on-item-click").Should().Be("false");
        button.HasAttribute("popovertarget").Should().BeFalse();
    }

    // Behavior source: NTMenuLabelItem and NTMenuDividerItem NTDocumentation contracts require usable static semantics.
    [Fact]
    public void Label_And_Divider_Items_Render_Static_Accessibility_Semantics_And_Attributes() {
        var cut = Render<NTMenu>(parameters => parameters
            .Add(x => x.AriaLabel, "Actions")
            .AddChildContent(builder => {
                builder.OpenComponent<NTMenuLabelItem>(0);
                builder.AddComponentParameter(1, nameof(NTMenuLabelItem.Label), "Document");
                builder.AddComponentParameter(2, nameof(NTMenuLabelItem.AdditionalAttributes), new Dictionary<string, object?> {
                    ["class"] = "document-label",
                    ["data-section"] = "document",
                    ["data-empty"] = null
                });
                builder.CloseComponent();

                builder.OpenComponent<NTMenuDividerItem>(3);
                builder.AddComponentParameter(4, nameof(NTMenuDividerItem.Inset), true);
                builder.CloseComponent();
            }));

        var label = cut.Find(".nt-menu-label");
        var divider = cut.Find(".nt-menu-divider");

        label.TextContent.Should().Be("Document");
        label.GetAttribute("role").Should().Be("presentation");
        label.ClassList.Should().Contain("document-label");
        label.GetAttribute("data-section").Should().Be("document");
        label.HasAttribute("data-empty").Should().BeFalse();
        divider.GetAttribute("role").Should().Be("separator");
        divider.GetAttribute("aria-orientation").Should().Be("horizontal");
        divider.ClassList.Should().Contain("nt-menu-divider-inset");
    }

    // Behavior source: NTMenuSubMenuItem NTDocumentation and NTMenu.IsSubMenu XML documentation define nested menu rendering and interaction semantics.
    [Fact]
    public void SubMenu_Item_Renders_An_Anchored_Nested_Menu_With_Trailing_Indicator() {
        var cut = Render<NTMenu>(parameters => parameters
            .Add(x => x.AriaLabel, "Actions")
            .AddChildContent<NTMenuSubMenuItem>(item => {
                item.Add(x => x.Label, "Share");
                item.Add(x => x.Icon, MaterialIcon.Share);
                item.AddChildContent<NTMenuButtonItem>(child => child.Add(x => x.Label, "Invite reviewer"));
            }));

        var trigger = cut.Find("button[data-nt-menu-submenu-trigger='true']");
        var nestedMenu = cut.Find("nt-menu[data-submenu='true']");

        trigger.GetAttribute("aria-haspopup").Should().Be("menu");
        trigger.GetAttribute("aria-controls").Should().Be(nestedMenu.Id);
        trigger.GetAttribute("popovertarget").Should().Be(nestedMenu.Id);
        trigger.QuerySelector(".nt-menu-item-icon").Should().NotBeNull();
        trigger.QuerySelector(".nt-menu-item-trailing-icon").Should().NotBeNull();
        nestedMenu.QuerySelector("button.nt-menu-item")!.TextContent.Should().Contain("Invite reviewer");
    }

    [Fact]
    public void Registered_Button_Items_Update_Selected_Class_When_Parameters_Change() {
        var cut = Render<MenuSelectionHost>();

        cut.FindAll(".nt-menu-item")[0].ClassList.Should().Contain("nt-menu-item-selected");

        cut.Instance.SelectRestaurants();

        cut.WaitForAssertion(() => {
            var items = cut.FindAll(".nt-menu-item");
            items[0].ClassList.Should().NotContain("nt-menu-item-selected");
            items[1].ClassList.Should().Contain("nt-menu-item-selected");
            items[1].GetAttribute("aria-selected").Should().Be("true");
        });
    }

    [Fact]
    public void Button_Item_Does_Not_Report_Rendered_State_Change_For_Equivalent_Regenerated_Icons() {
        var item = new NTMenuButtonItem {
            Icon = MaterialIcon.Edit,
            Label = "Rename"
        };

        RenderedStateChanged(item, MaterialIcon.Edit, "Rename").Should().BeFalse();
    }

    [Fact]
    public void Anchor_Item_Does_Not_Report_Rendered_State_Change_For_Equivalent_Regenerated_Icons() {
        var item = new NTMenuAnchorItem {
            Href = "/nt-menu",
            Icon = MaterialIcon.OpenInNew,
            Label = "Open"
        };

        RenderedStateChanged(item, MaterialIcon.OpenInNew, "Open", "/nt-menu").Should().BeFalse();
    }

    [Fact]
    public void SubMenu_Item_Does_Not_Report_Rendered_State_Change_For_Equivalent_Regenerated_Icons_Or_ChildContent_Delegates() {
        var item = new NTMenuSubMenuItem {
            ChildContent = builder => builder.AddContent(0, "Share"),
            Icon = MaterialIcon.Share,
            Label = "Share"
        };

        RenderedStateChanged(item, MaterialIcon.Share, "Share").Should().BeFalse();
    }

    [Fact]
    public void Button_Item_Reports_Rendered_State_Change_For_Different_Icon_State() {
        var item = new NTMenuButtonItem {
            Icon = MaterialIcon.Edit,
            Label = "Rename"
        };

        RenderedStateChanged(item, MaterialIcon.ContentCopy, "Rename").Should().BeTrue();
    }

    [Fact]
    public void Label_Item_Requires_Label() {
        var item = new NTMenuLabelItem {
            Parent = new NTMenu()
        };

        var render = () => item.SetParametersAsync(ParameterView.Empty).GetAwaiter().GetResult();

        render.Should().Throw<InvalidOperationException>()
            .WithMessage("*NTMenuLabelItem requires a non-empty Label*");
    }

    private static bool RenderedStateChanged(NTMenuButtonItem item, TnTIcon? previousIcon, string previousLabel) {
        var method = typeof(NTMenuButtonItem).GetMethod("RenderedStateChanged", BindingFlags.Instance | BindingFlags.NonPublic);

        method.Should().NotBeNull();
        return (bool)method!.Invoke(item, [item.AriaLabel, item.Disabled, previousIcon, previousLabel, item.Selected])!;
    }

    private static bool RenderedStateChanged(NTMenuAnchorItem item, TnTIcon? previousIcon, string previousLabel, string previousHref) {
        var method = typeof(NTMenuAnchorItem).GetMethod("RenderedStateChanged", BindingFlags.Instance | BindingFlags.NonPublic);

        method.Should().NotBeNull();
        return (bool)method!.Invoke(item, [item.AriaLabel, item.Disabled, previousHref, previousIcon, previousLabel, item.Selected, item.Target])!;
    }

    private static bool RenderedStateChanged(NTMenuSubMenuItem item, TnTIcon? previousIcon, string previousLabel) {
        var method = typeof(NTMenuSubMenuItem).GetMethod("RenderedStateChanged", BindingFlags.Instance | BindingFlags.NonPublic);

        method.Should().NotBeNull();
        return (bool)method!.Invoke(item, [item.AriaLabel, item.Disabled, previousIcon, previousLabel, item.Selected])!;
    }

    private sealed class MenuSelectionHost : ComponentBase {
        private string _selected = "All";

        public void SelectRestaurants() {
            _selected = "Restaurants";
            InvokeAsync(StateHasChanged).GetAwaiter().GetResult();
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder) {
            builder.OpenComponent<NTMenu>(0);
            builder.AddAttribute(1, nameof(NTMenu.ElementId), "category-menu");
            builder.AddAttribute(2, nameof(NTMenu.AriaLabel), "Category");
            builder.AddAttribute(3, nameof(NTMenu.ChildContent), (RenderFragment)BuildMenuItems);
            builder.CloseComponent();
        }

        private void BuildMenuItems(RenderTreeBuilder builder) {
            builder.OpenComponent<NTMenuButtonItem>(0);
            builder.AddAttribute(1, nameof(NTMenuButtonItem.Label), "All categories");
            builder.AddAttribute(2, nameof(NTMenuButtonItem.Selected), _selected == "All");
            builder.CloseComponent();

            builder.OpenComponent<NTMenuButtonItem>(3);
            builder.AddAttribute(4, nameof(NTMenuButtonItem.Label), "Restaurants");
            builder.AddAttribute(5, nameof(NTMenuButtonItem.Selected), _selected == "Restaurants");
            builder.CloseComponent();
        }
    }
}
