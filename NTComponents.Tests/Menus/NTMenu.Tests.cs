using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace NTComponents.Tests.Menus;

public class NTMenu_Tests {

    [Theory]
    [InlineData(nameof(NTMenu.AriaLabel))]
    [InlineData(nameof(NTMenu.ChildContent))]
    public void Required_Parameters_Have_EditorRequired_Attribute(string propertyName) {
        var property = typeof(NTMenu).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);

        property.Should().NotBeNull();
        property!.GetCustomAttribute<EditorRequiredAttribute>().Should().NotBeNull();
    }
}
