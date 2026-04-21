namespace NTComponents.Tests.Enums;

/// <summary>
/// Unit tests for <see cref="NTTypography" />.
/// </summary>
public class NTTypography_Tests {
    [Fact]
    public void ToCssClass_ForBaselineStyle_ReturnsExpectedClass() {
        // Arrange
        var typography = NTTypography.DisplayLarge;

        // Act
        var cssClass = typography.ToCssClass();

        // Assert
        cssClass.Should().Be("nt-display-large");
    }

    [Fact]
    public void ToCssClass_ForEmphasizedStyle_ReturnsExpectedClass() {
        // Arrange
        var typography = NTTypography.TitleMedium;

        // Act
        var cssClass = typography.ToCssClass(emphasized: true);

        // Assert
        cssClass.Should().Be("nt-title-medium-emphasized");
    }

    [Fact]
    public void ToCssClass_ForAllTypographyValues_ReturnsNtPrefixedClass() {
        // Arrange
        var typographyValues = Enum.GetValues<NTTypography>();

        // Act
        var cssClasses = typographyValues.Select(static x => x.ToCssClass()).ToArray();

        // Assert
        cssClasses.Should().OnlyContain(static x => x.StartsWith("nt-"));
        cssClasses.Should().OnlyHaveUniqueItems();
        cssClasses.Should().HaveCount(15);
    }

    [Fact]
    public void ToCssClass_ForInvalidValue_ThrowsArgumentOutOfRangeException() {
        // Arrange
        var typography = (NTTypography)999;

        // Act
        var action = () => typography.ToCssClass();

        // Assert
        action.Should().Throw<ArgumentOutOfRangeException>();
    }
}
