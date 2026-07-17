namespace NTComponents.Tests.Enums;

/// <summary>
/// Unit tests for <see cref="NTCornerRadius" />.
/// </summary>
public class NTCornerRadius_Tests {
    [Fact]
    public void ToCssClass_ForMedium_ReturnsExpectedClass() {
        // Arrange
        var cornerRadius = NTCornerRadius.Medium;

        // Act
        var cssClass = cornerRadius.ToCssClass();

        // Assert
        cssClass.Should().Be("nt-corner-radius-medium");
    }

    [Fact]
    public void ToCssValue_ForMedium_ReturnsExpectedPixelValue() {
        // Arrange
        var cornerRadius = NTCornerRadius.Medium;

        // Act
        var cssValue = cornerRadius.ToCssValue();

        // Assert
        cssValue.Should().Be("12px");
    }

    [Fact]
    public void ToCssValue_ForFull_ReturnsFiftyPercent() {
        // Arrange
        var cornerRadius = NTCornerRadius.Full;

        // Act
        var cssValue = cornerRadius.ToCssValue();

        // Assert
        cssValue.Should().Be("50%");
    }

    [Fact]
    public void ToCssValue_ForAllCornerRadiusValues_ReturnsUniqueValues() {
        // Arrange
        var cornerRadiusValues = Enum.GetValues<NTCornerRadius>();

        // Act
        var cssValues = cornerRadiusValues.Select(static x => x.ToCssValue()).ToArray();

        // Assert
        cssValues.Should().OnlyHaveUniqueItems();
        cssValues.Should().HaveCount(10);
    }

    [Fact]
    public void ToCssClass_ForAllCornerRadiusValues_ReturnsNtPrefixedClasses() {
        // Arrange
        var cornerRadiusValues = Enum.GetValues<NTCornerRadius>();

        // Act
        var cssClasses = cornerRadiusValues.Select(static x => x.ToCssClass()).ToArray();

        // Assert
        cssClasses.Should().OnlyContain(static x => x.StartsWith("nt-corner-radius-"));
        cssClasses.Should().OnlyHaveUniqueItems();
        cssClasses.Should().HaveCount(10);
    }

    [Fact]
    public void ToCssValue_ForInvalidValue_ThrowsArgumentOutOfRangeException() {
        // Arrange
        var cornerRadius = (NTCornerRadius)999;

        // Act
        var action = () => cornerRadius.ToCssValue();

        // Assert
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ToCssClass_ForInvalidValue_ThrowsArgumentOutOfRangeException() {
        // Arrange
        var cornerRadius = (NTCornerRadius)999;

        // Act
        var action = () => cornerRadius.ToCssClass();

        // Assert
        action.Should().Throw<ArgumentOutOfRangeException>();
    }
}
