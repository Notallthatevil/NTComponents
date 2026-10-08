using Microsoft.AspNetCore.Components;

namespace NTComponents.Tests.Enums;

public class NTColor_Tests {
    [Theory]
    [InlineData(NTColor.Primary, "var(--nt-color-primary)")]
    [InlineData(NTColor.OnPrimaryFixedVariant, "var(--nt-color-on-primary-fixed-variant)")]
    [InlineData(NTColor.Transparent, "var(--nt-color-transparent)")]
    public void Color_UsesNTThemeVariable(NTColor color, string expected) {
        color.ToCssNTColorVariable().Should().Be(expected);
    }

    [Fact]
    public void OptionalColorWithoutValue_ProducesNoOverride() {
        NTColor? color = null;
        color.ToCssNTColorVariable().Should().BeEmpty();
    }

    [Fact]
    public void SharedColors_CastToTheSameLegacyMember() {
        foreach (var color in Enum.GetValues<NTColor>()) {
            ((TnTColor)(int)color).ToString().Should().Be(color.ToString());
        }
    }

    [Fact]
    public void ActiveNTComponentColorProperties_UseTheNewEnum() {
        var legacyProperties = typeof(NTButton).Assembly.GetTypes()
            .Where(type => type.IsPublic && !type.IsDefined(typeof(ObsoleteAttribute), false) && type.Name.StartsWith("NT", StringComparison.Ordinal) && typeof(IComponent).IsAssignableFrom(type))
            .SelectMany(type => type.GetProperties())
            .Where(property => (Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType) == typeof(TnTColor))
            .Select(property => $"{property.DeclaringType?.Name}.{property.Name}");

        legacyProperties.Should().BeEmpty();
    }
}
