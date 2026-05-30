using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace NTComponents.Tests.Menus;

public class NTContextMenu_Tests : BunitContext {

    public NTContextMenu_Tests() {
        var contextMenuModule = JSInterop.SetupModule("./_content/NTComponents/Menus/NTContextMenu.razor.js");
        contextMenuModule.SetupVoid("onLoad", _ => true);
        contextMenuModule.SetupVoid("onUpdate", _ => true);
        contextMenuModule.SetupVoid("onDispose", _ => true);

        var menuModule = JSInterop.SetupModule("./_content/NTComponents/Menus/NTMenu.razor.js");
        menuModule.SetupVoid("onLoad", _ => true);
        menuModule.SetupVoid("onUpdate", _ => true);
        menuModule.SetupVoid("onDispose", _ => true);
    }

    [Theory]
    [InlineData(nameof(NTContextMenu.AriaLabel))]
    [InlineData(nameof(NTContextMenu.MenuContent))]
    [InlineData(nameof(NTContextMenu.TargetContent))]
    public void Required_Parameters_Have_EditorRequired_Attribute(string propertyName) {
        var property = typeof(NTContextMenu).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);

        property.Should().NotBeNull();
        property!.GetCustomAttribute<EditorRequiredAttribute>().Should().NotBeNull();
    }

    [Fact]
    public void Render_Puts_TargetContent_Inside_Target_Wrapper() {
        var cut = Render<NTContextMenu>(parameters => parameters
            .Add(x => x.AriaLabel, "Row actions")
            .Add(x => x.TargetContent, builder => {
                builder.OpenElement(0, "button");
                builder.AddAttribute(1, "id", "row-target");
                builder.AddContent(2, "Invoice 123");
                builder.CloseElement();
            })
            .Add(x => x.MenuContent, builder => {
                builder.OpenComponent<NTMenuButtonItem>(0);
                builder.AddAttribute(1, nameof(NTMenuButtonItem.Label), "Rename");
                builder.CloseComponent();
            }));

        cut.Find("nt-context-menu > .nt-context-menu-target > #row-target").TextContent.Should().Be("Invoice 123");
    }

    [Fact]
    public void Render_Passes_MenuContent_And_Visual_Parameters_To_NTMenu() {
        var cut = Render<NTContextMenu>(parameters => parameters
            .Add(x => x.AriaLabel, "Row actions")
            .Add(x => x.Appearance, NTMenuAppearance.Compact)
            .Add(x => x.CloseOnContentClick, false)
            .Add(x => x.ContainerColor, TnTColor.SecondaryContainer)
            .Add(x => x.TextColor, TnTColor.OnSecondaryContainer)
            .Add(x => x.SelectedContainerColor, TnTColor.TertiaryContainer)
            .Add(x => x.SelectedTextColor, TnTColor.OnTertiaryContainer)
            .Add(x => x.Elevation, NTElevation.High)
            .Add(x => x.TargetContent, builder => builder.AddContent(0, "Row"))
            .Add(x => x.MenuContent, builder => {
                builder.OpenComponent<NTMenuButtonItem>(0);
                builder.AddAttribute(1, nameof(NTMenuButtonItem.Label), "Rename");
                builder.CloseComponent();
            }));

        var menu = cut.Find("nt-menu");
        var contextMenu = cut.Find("nt-context-menu");
        var style = menu.GetAttribute("style");

        contextMenu.GetAttribute("data-menu-id").Should().Be(menu.GetAttribute("id"));
        menu.GetAttribute("aria-label").Should().Be("Row actions");
        menu.GetAttribute("data-close-on-item-click").Should().Be("false");
        menu.GetAttribute("class").Should().Contain("nt-menu-compact");
        menu.GetAttribute("class").Should().Contain("nt-elevation-high");
        style.Should().Contain("--nt-menu-container-color:var(--tnt-color-secondary-container)");
        style.Should().Contain("--nt-menu-content-color:var(--tnt-color-on-secondary-container)");
        style.Should().Contain("--nt-menu-selected-container-color:var(--tnt-color-tertiary-container)");
        style.Should().Contain("--nt-menu-selected-content-color:var(--tnt-color-on-tertiary-container)");
        cut.Find("nt-menu > .nt-menu-surface > .nt-menu-content > .nt-menu-item").TextContent.Should().Contain("Rename");
    }

    [Fact]
    public void Nested_ContextMenu_Can_Inherit_Ancestor_MenuContent() {
        var cut = Render<NestedInheritedContextMenu>();

        var childMenu = cut.Find("nt-menu[aria-label='Child actions']");

        childMenu.TextContent.Should().Contain("Open child item");
        childMenu.TextContent.Should().Contain("Open parent container");
        childMenu.QuerySelectorAll(".nt-menu-divider").Should().HaveCount(1);
    }

    [Fact]
    public void Nested_ContextMenu_Does_Not_Inherit_Ancestor_MenuContent_By_Default() {
        var cut = Render<NestedContextMenuWithoutInheritance>();

        var childMenu = cut.Find("nt-menu[aria-label='Child actions']");

        childMenu.TextContent.Should().Contain("Open child item");
        childMenu.TextContent.Should().NotContain("Open parent container");
    }

    private sealed class NestedInheritedContextMenu : ComponentBase {

        protected override void BuildRenderTree(RenderTreeBuilder builder) {
            RenderNestedContextMenu(builder, inheritParentMenus: true);
        }
    }

    private sealed class NestedContextMenuWithoutInheritance : ComponentBase {

        protected override void BuildRenderTree(RenderTreeBuilder builder) {
            RenderNestedContextMenu(builder, inheritParentMenus: false);
        }
    }

    private static void RenderNestedContextMenu(RenderTreeBuilder builder, bool inheritParentMenus) {
        builder.OpenComponent<NTContextMenu>(0);
        builder.AddAttribute(1, nameof(NTContextMenu.AriaLabel), "Parent actions");
        builder.AddAttribute(2, nameof(NTContextMenu.TargetContent), (RenderFragment)(parentTargetBuilder => {
            parentTargetBuilder.OpenElement(0, "section");

            parentTargetBuilder.OpenComponent<NTContextMenu>(1);
            parentTargetBuilder.AddAttribute(2, nameof(NTContextMenu.AriaLabel), "Child actions");
            parentTargetBuilder.AddAttribute(3, nameof(NTContextMenu.InheritParentMenus), inheritParentMenus);
            parentTargetBuilder.AddAttribute(4, nameof(NTContextMenu.TargetContent), (RenderFragment)(childTargetBuilder => {
                childTargetBuilder.OpenElement(0, "button");
                childTargetBuilder.AddContent(1, "Child target");
                childTargetBuilder.CloseElement();
            }));
            parentTargetBuilder.AddAttribute(5, nameof(NTContextMenu.MenuContent), (RenderFragment)(childMenuBuilder => {
                childMenuBuilder.OpenComponent<NTMenuButtonItem>(0);
                childMenuBuilder.AddAttribute(1, nameof(NTMenuButtonItem.Label), "Open child item");
                childMenuBuilder.CloseComponent();
            }));
            parentTargetBuilder.CloseComponent();

            parentTargetBuilder.CloseElement();
        }));
        builder.AddAttribute(3, nameof(NTContextMenu.MenuContent), (RenderFragment)(parentMenuBuilder => {
            parentMenuBuilder.OpenComponent<NTMenuButtonItem>(0);
            parentMenuBuilder.AddAttribute(1, nameof(NTMenuButtonItem.Label), "Open parent container");
            parentMenuBuilder.CloseComponent();
        }));
        builder.CloseComponent();
    }
}
