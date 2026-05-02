using System.Reflection;
using Microsoft.AspNetCore.Components;

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
    public void Label_Item_Requires_Label() {
        var item = new NTMenuLabelItem {
            Parent = new NTMenu()
        };

        var render = () => item.SetParametersAsync(ParameterView.Empty).GetAwaiter().GetResult();

        render.Should().Throw<InvalidOperationException>()
            .WithMessage("*NTMenuLabelItem requires a non-empty Label*");
    }
}
