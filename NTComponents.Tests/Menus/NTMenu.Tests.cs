using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

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
    public void Label_Item_Requires_Label() {
        var item = new NTMenuLabelItem {
            Parent = new NTMenu()
        };

        var render = () => item.SetParametersAsync(ParameterView.Empty).GetAwaiter().GetResult();

        render.Should().Throw<InvalidOperationException>()
            .WithMessage("*NTMenuLabelItem requires a non-empty Label*");
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
