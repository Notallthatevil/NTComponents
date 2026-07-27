namespace NTComponents.Tests.Enums;

/// <summary>
/// Unit tests for <see cref="NTMotionDuration" />.
/// </summary>
public class NTMotionDuration_Tests {
    [Theory]
    [InlineData(NTMotionDuration.Ms50, "nt-motion-duration-50", 50)]
    [InlineData(NTMotionDuration.Ms100, "nt-motion-duration-100", 100)]
    [InlineData(NTMotionDuration.Ms150, "nt-motion-duration-150", 150)]
    [InlineData(NTMotionDuration.Ms200, "nt-motion-duration-200", 200)]
    [InlineData(NTMotionDuration.Ms250, "nt-motion-duration-250", 250)]
    [InlineData(NTMotionDuration.Ms300, "nt-motion-duration-300", 300)]
    [InlineData(NTMotionDuration.Ms350, "nt-motion-duration-350", 350)]
    [InlineData(NTMotionDuration.Ms400, "nt-motion-duration-400", 400)]
    [InlineData(NTMotionDuration.Ms450, "nt-motion-duration-450", 450)]
    [InlineData(NTMotionDuration.Ms500, "nt-motion-duration-500", 500)]
    [InlineData(NTMotionDuration.Ms550, "nt-motion-duration-550", 550)]
    [InlineData(NTMotionDuration.Ms600, "nt-motion-duration-600", 600)]
    [InlineData(NTMotionDuration.Ms700, "nt-motion-duration-700", 700)]
    [InlineData(NTMotionDuration.Ms800, "nt-motion-duration-800", 800)]
    [InlineData(NTMotionDuration.Ms900, "nt-motion-duration-900", 900)]
    [InlineData(NTMotionDuration.Ms1000, "nt-motion-duration-1000", 1000)]
    public void Duration_Token_Maps_To_Its_Public_Css_Class_And_Millisecond_Value(NTMotionDuration duration, string expectedCssClass, int expectedMilliseconds) {
        duration.ToCssClass().Should().Be(expectedCssClass);
        duration.ToMilliseconds().Should().Be(expectedMilliseconds);
    }

    [Fact]
    public void ToCssClass_For_Invalid_Duration_Throws_ArgumentOutOfRangeException() {
        var action = () => ((NTMotionDuration)999).ToCssClass();

        action.Should().Throw<ArgumentOutOfRangeException>()
            .Which.ParamName.Should().Be("duration");
    }

    [Fact]
    public void ToMilliseconds_For_Invalid_Duration_Throws_ArgumentOutOfRangeException() {
        var action = () => ((NTMotionDuration)999).ToMilliseconds();

        action.Should().Throw<ArgumentOutOfRangeException>()
            .Which.ParamName.Should().Be("duration");
    }
}
