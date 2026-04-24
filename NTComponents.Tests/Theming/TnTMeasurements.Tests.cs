namespace NTComponents.Tests.Theming;

/// <summary>
///     Unit tests for <see cref="TnTMeasurements" />.
/// </summary>
public class TnTMeasurements_Tests : BunitContext {

    [Fact]
    public void AllCustomValues_RenderCorrectly() {
        // Arrange & Act
        var cut = RenderMeasurements(parameters => parameters
            .Add(p => p.HeaderHeight, 100)
            .Add(p => p.FooterHeight, 50)
            .Add(p => p.SideNavWidth, 280));

        // Assert
        cut.Markup.Should().Contain("--tnt-header-height:100px");
        cut.Markup.Should().Contain("--tnt-footer-height:50px");
        cut.Markup.Should().Contain("--tnt-side-nav-width:280px");
    }

    [Fact]
    public void AllTypographyClasses_ArePresent() {
        // Arrange & Act
        var cut = RenderMeasurements();
        var markup = cut.Markup;

        // Assert - Display styles
        markup.Should().Contain(".tnt-display-large{");
        markup.Should().Contain(".tnt-display-medium{");
        markup.Should().Contain(".tnt-display-small{");

        // Headline styles
        markup.Should().Contain(".tnt-headline-large{");
        markup.Should().Contain(".tnt-headline-medium{");
        markup.Should().Contain(".tnt-headline-small{");

        // Body styles
        markup.Should().Contain(".tnt-body-large{");
        markup.Should().Contain(".tnt-body-medium{");
        markup.Should().Contain(".tnt-body-small{");

        // Label styles
        markup.Should().Contain(".tnt-label-large{");
        markup.Should().Contain(".tnt-label-medium{");
        markup.Should().Contain(".tnt-label-small{");

        // Title styles
        markup.Should().Contain(".tnt-title-large{");
        markup.Should().Contain(".tnt-title-medium{");
        markup.Should().Contain(".tnt-title-small{");

        // New nt-prefixed styles
        markup.Should().Contain(".nt-display-large{");
        markup.Should().Contain(".nt-headline-large{");
        markup.Should().Contain(".nt-title-large{");
        markup.Should().Contain(".nt-body-large{");
        markup.Should().Contain(".nt-label-large{");
        markup.Should().Contain(".nt-display-large-emphasized{");
        markup.Should().Contain(".nt-headline-large-emphasized{");
        markup.Should().Contain(".nt-title-large-emphasized{");
        markup.Should().Contain(".nt-body-large-emphasized{");
        markup.Should().Contain(".nt-label-large-emphasized{");
    }

    [Fact]
    public void BodyReset_IsPresent() {
        // Arrange & Act
        var cut = RenderMeasurements();

        // Assert
        cut.Markup.Should().Contain("body{padding:0;margin:0;}");
    }

    [Fact]
    public void BothStyleElements_AreRendered() {
        // Arrange & Act
        var cut = RenderMeasurements();

        // Assert
        var measurementStyles = cut.FindAll("style.tnt-measurements");
        var fontStyles = cut.FindAll("style.tnt-fonts");

        measurementStyles.Should().HaveCount(1);
        fontStyles.Should().HaveCount(1);
    }

    [Fact]
    public void Constructor_InitializesCorrectly() {
        // Arrange & Act
        var measurements = new TnTMeasurements();

        // Assert
        measurements.Should().NotBeNull();
        measurements.HeaderHeight.Should().Be(64);
        measurements.FooterHeight.Should().Be(64);
        measurements.SideNavWidth.Should().Be(256);
    }

    [Fact]
    public void CustomValuesAndFonts_RenderTogether() {
        // Arrange & Act
        var cut = RenderMeasurements(parameters => parameters
            .Add(p => p.HeaderHeight, 120)
            .Add(p => p.FooterHeight, 40)
            .Add(p => p.SideNavWidth, 320));

        // Assert Both custom measurements and fonts should be present
        cut.Markup.Should().Contain("--tnt-header-height:120px");
        cut.Markup.Should().Contain(".tnt-display-large{font-family:Roboto;");
        cut.Markup.Should().Contain(".nt-display-large{font-family:var(--nt-sys-typescale-display-large-font,var(--nt-ref-typeface-brand,Roboto));");
        cut.Markup.Should().Contain("body{padding:0;margin:0;}");
    }

    [Fact]
    public void DecimalValues_RenderCorrectly() {
        // Arrange & Act
        var cut = RenderMeasurements(parameters => parameters
            .Add(p => p.HeaderHeight, 64.5)
            .Add(p => p.FooterHeight, 48.25)
            .Add(p => p.SideNavWidth, 256.75));

        // Assert
        cut.Markup.Should().Contain("--tnt-header-height:64.5px");
        cut.Markup.Should().Contain("--tnt-footer-height:48.25px");
        cut.Markup.Should().Contain("--tnt-side-nav-width:256.75px");
    }

    [Fact]
    public void FontProperties_AreCorrectForDisplayLarge() {
        // Arrange & Act
        var cut = RenderMeasurements();

        // Assert
        cut.Markup.Should().Contain(".tnt-display-large{font-family:Roboto;font-weight:400;font-size:57px;line-height:64px;letter-spacing:-0.25px;}");
    }

    [Fact]
    public void FontProperties_AreCorrectForLabelStyles() {
        // Arrange & Act
        var cut = RenderMeasurements();

        // Assert
        cut.Markup.Should().Contain(".tnt-label-large{font-family:Roboto;font-weight:500;font-size:14px;line-height:20px;letter-spacing:0.10px;}");
        cut.Markup.Should().Contain(".tnt-label-medium{font-family:Roboto;font-weight:500;font-size:12px;line-height:16px;letter-spacing:0.50px;}");
        cut.Markup.Should().Contain(".tnt-label-small{font-family:Roboto;font-weight:500;font-size:11px;line-height:16px;letter-spacing:0.50px;}");
    }

    [Fact]
    public void NtTypefaceTokens_ArePresent() {
        // Arrange & Act
        var cut = RenderMeasurements();

        // Assert
        cut.Markup.Should().Contain("--nt-ref-typeface-brand:Roboto;");
        cut.Markup.Should().Contain("--nt-ref-typeface-plain:Roboto;");
        cut.Markup.Should().Contain("--nt-ref-typeface-weight-regular:400;");
        cut.Markup.Should().Contain("--nt-ref-typeface-weight-medium:500;");
        cut.Markup.Should().Contain("--nt-ref-typeface-weight-bold:700;");
    }

    [Fact]
    public void LegacyTntTitleMedium_IsRetained() {
        // Arrange & Act
        var cut = RenderMeasurements();

        // Assert
        cut.Markup.Should().Contain(".tnt-title-medium{font-family:Roboto;font-weight:500;font-size:18px;line-height:24px;letter-spacing:0.15px;}");
    }

    [Fact]
    public void NtTypographyTokens_AreRendered() {
        // Arrange & Act
        var cut = RenderMeasurements();

        // Assert
        cut.Markup.Should().Contain("--nt-sys-typescale-title-medium-size:16px;");
        cut.Markup.Should().Contain("--nt-sys-typescale-emphasized-display-large-weight:500;");
        cut.Markup.Should().Contain("--nt-sys-typescale-emphasized-title-medium-weight:700;");
        cut.Markup.Should().Contain(".nt-title-medium{font-family:var(--nt-sys-typescale-title-medium-font,var(--nt-ref-typeface-brand,Roboto));font-weight:var(--nt-sys-typescale-title-medium-weight,500);font-size:var(--nt-sys-typescale-title-medium-size,16px);line-height:var(--nt-sys-typescale-title-medium-line-height,24px);letter-spacing:var(--nt-sys-typescale-title-medium-tracking,0.15px);}");
        cut.Markup.Should().Contain(".nt-label-large-emphasized{font-family:var(--nt-sys-typescale-emphasized-label-large-font,var(--nt-ref-typeface-brand,Roboto));font-weight:var(--nt-sys-typescale-emphasized-label-large-weight,700);font-size:var(--nt-sys-typescale-emphasized-label-large-size,14px);line-height:var(--nt-sys-typescale-emphasized-label-large-line-height,20px);letter-spacing:var(--nt-sys-typescale-emphasized-label-large-tracking,0.10px);}");
    }

    [Fact]
    public void NtCornerRadiusTokens_AreRendered() {
        // Arrange & Act
        var cut = RenderMeasurements();

        // Assert
        cut.Markup.Should().Contain("--nt-sys-shape-corner-medium:12px;");
        cut.Markup.Should().Contain("--nt-sys-shape-corner-full:50%;");
        cut.Markup.Should().Contain(".nt-corner-radius-medium{border-radius:var(--nt-sys-shape-corner-medium,12px);}");
        cut.Markup.Should().Contain(".nt-corner-radius-full{border-radius:var(--nt-sys-shape-corner-full,50%);}");
    }

    [Fact]
    public void FooterHeight_CanBeCustomized() {
        // Arrange & Act
        var cut = RenderMeasurements(parameters => parameters
            .Add(p => p.FooterHeight, 48));

        // Assert
        cut.Instance.FooterHeight.Should().Be(48);
        cut.Markup.Should().Contain("--tnt-footer-height:48px");
    }

    [Fact]
    public void FooterHeight_DefaultValue_Is64() {
        // Arrange & Act
        var cut = RenderMeasurements();

        // Assert
        cut.Instance.FooterHeight.Should().Be(64);
        cut.Markup.Should().Contain("--tnt-footer-height:64px");
    }

    [Fact]
    public void HeaderHeight_CanBeCustomized() {
        // Arrange & Act
        var cut = RenderMeasurements(parameters => parameters
            .Add(p => p.HeaderHeight, 80));

        // Assert
        cut.Instance.HeaderHeight.Should().Be(80);
        cut.Markup.Should().Contain("--tnt-header-height:80px");
    }

    [Fact]
    public void HeaderHeight_DefaultValue_Is64() {
        // Arrange & Act
        var cut = RenderMeasurements();

        // Assert
        cut.Instance.HeaderHeight.Should().Be(64);
        cut.Markup.Should().Contain("--tnt-header-height:64px");
    }

    [Fact]
    public void LargeValues_RenderCorrectly() {
        // Arrange & Act
        var cut = RenderMeasurements(parameters => parameters
            .Add(p => p.HeaderHeight, 1000)
            .Add(p => p.FooterHeight, 999)
            .Add(p => p.SideNavWidth, 500));

        // Assert
        cut.Markup.Should().Contain("--tnt-header-height:1000px");
        cut.Markup.Should().Contain("--tnt-footer-height:999px");
        cut.Markup.Should().Contain("--tnt-side-nav-width:500px");
    }

    [Fact]
    public void NegativeValues_RenderAsProvided() {
        // Arrange & Act
        var cut = RenderMeasurements(parameters => parameters
            .Add(p => p.HeaderHeight, -10)
            .Add(p => p.FooterHeight, -5)
            .Add(p => p.SideNavWidth, -50));

        // Assert
        cut.Markup.Should().Contain("--tnt-header-height:-10px");
        cut.Markup.Should().Contain("--tnt-footer-height:-5px");
        cut.Markup.Should().Contain("--tnt-side-nav-width:-50px");
    }

    [Fact]
    public void RendersCorrectCssStructure() {
        // Arrange & Act
        var cut = RenderMeasurements();

        // Assert
        cut.Markup.Should().Contain("<style class=\"tnt-measurements\">");
        cut.Markup.Should().Contain(":root{");
        cut.Markup.Should().Contain("--tnt-header-height:64px;");
        cut.Markup.Should().Contain("--tnt-footer-height:64px;");
        cut.Markup.Should().Contain("--tnt-side-nav-width:256px;");
        cut.Markup.Should().Contain("</style>");
    }

    [Fact]
    public void RendersFontCssCorrectly() {
        // Arrange & Act
        var cut = RenderMeasurements();

        // Assert
        cut.Markup.Should().Contain("<style class=\"tnt-fonts\">");
        cut.Markup.Should().Contain(":root{--nt-ref-typeface-brand:Roboto;");
        cut.Markup.Should().Contain("body{padding:0;margin:0;}");
        cut.Markup.Should().Contain(".tnt-display-large{font-family:Roboto;");
        cut.Markup.Should().Contain(".tnt-body-medium{font-family:Roboto;font-weight:400;font-size:14px;line-height:20px;letter-spacing:0.25px;}");
        cut.Markup.Should().Contain(".tnt-title-small{font-family:Roboto;font-weight:500;font-size:14px;line-height:20px;letter-spacing:0.10px;}");
        cut.Markup.Should().Contain(".nt-body-medium{font-family:var(--nt-sys-typescale-body-medium-font,var(--nt-ref-typeface-plain,Roboto));font-weight:var(--nt-sys-typescale-body-medium-weight,400);font-size:var(--nt-sys-typescale-body-medium-size,14px);line-height:var(--nt-sys-typescale-body-medium-line-height,20px);letter-spacing:var(--nt-sys-typescale-body-medium-tracking,0.25px);}");
        cut.Markup.Should().Contain(".nt-title-small{font-family:var(--nt-sys-typescale-title-small-font,var(--nt-ref-typeface-brand,Roboto));font-weight:var(--nt-sys-typescale-title-small-weight,500);font-size:var(--nt-sys-typescale-title-small-size,14px);line-height:var(--nt-sys-typescale-title-small-line-height,20px);letter-spacing:var(--nt-sys-typescale-title-small-tracking,0.10px);}");
    }

    [Fact]
    public void SideNavWidth_CanBeCustomized() {
        // Arrange & Act
        var cut = RenderMeasurements(parameters => parameters
            .Add(p => p.SideNavWidth, 300));

        // Assert
        cut.Instance.SideNavWidth.Should().Be(300);
        cut.Markup.Should().Contain("--tnt-side-nav-width:300px");
    }

    [Fact]
    public void SideNavWidth_DefaultValue_Is256() {
        // Arrange & Act
        var cut = RenderMeasurements();

        // Assert
        cut.Instance.SideNavWidth.Should().Be(256);
        cut.Markup.Should().Contain("--tnt-side-nav-width:256px");
    }

    [Fact]
    public void VerySmallDecimalValues_RenderCorrectly() {
        // Arrange & Act
        var cut = RenderMeasurements(parameters => parameters
            .Add(p => p.HeaderHeight, 0.1)
            .Add(p => p.FooterHeight, 0.01)
            .Add(p => p.SideNavWidth, 0.001));

        // Assert
        cut.Markup.Should().Contain("--tnt-header-height:0.1px");
        cut.Markup.Should().Contain("--tnt-footer-height:0.01px");
        cut.Markup.Should().Contain("--tnt-side-nav-width:0.001px");
    }

    [Fact]
    public void ZeroValues_RenderCorrectly() {
        // Arrange & Act
        var cut = RenderMeasurements(parameters => parameters
            .Add(p => p.HeaderHeight, 0)
            .Add(p => p.FooterHeight, 0)
            .Add(p => p.SideNavWidth, 0));

        // Assert
        cut.Markup.Should().Contain("--tnt-header-height:0px");
        cut.Markup.Should().Contain("--tnt-footer-height:0px");
        cut.Markup.Should().Contain("--tnt-side-nav-width:0px");
    }

    private IRenderedComponent<TnTMeasurements> RenderMeasurements(
        Action<ComponentParameterCollectionBuilder<TnTMeasurements>>? parameterBuilder = null) {
        return Render<TnTMeasurements>(parameters => {
            parameterBuilder?.Invoke(parameters);
        });
    }
}
