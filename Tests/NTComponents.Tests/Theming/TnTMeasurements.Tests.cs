namespace NTComponents.Tests.Theming;

/// <summary>
///     Unit tests for <see cref="TnTMeasurements" />.
/// </summary>
public class TnTMeasurements_Tests : BunitContext {

    [Fact]
    public void AllCustomValues_RenderCorrectly() {
        var cut = RenderMeasurements(parameters => parameters
            .Add(p => p.HeaderHeight, 100)
            .Add(p => p.FooterHeight, 50)
            .Add(p => p.SideNavWidth, 280));

        cut.Markup.Should().Contain("--tnt-header-height:100px");
        cut.Markup.Should().Contain("--tnt-footer-height:50px");
        cut.Markup.Should().Contain("--tnt-side-nav-width:280px");
    }

    [Fact]
    public void Constructor_InitializesCorrectly() {
        var measurements = new TnTMeasurements();

        measurements.Should().NotBeNull();
        measurements.HeaderHeight.Should().Be(64);
        measurements.FooterHeight.Should().Be(64);
        measurements.SideNavWidth.Should().Be(256);
        measurements.TokenScopeSelector.Should().Be(":root");
    }

    [Fact]
    public void DecimalValues_RenderCorrectly() {
        var cut = RenderMeasurements(parameters => parameters
            .Add(p => p.HeaderHeight, 64.5)
            .Add(p => p.FooterHeight, 48.25)
            .Add(p => p.SideNavWidth, 256.75));

        cut.Markup.Should().Contain("--tnt-header-height:64.5px");
        cut.Markup.Should().Contain("--tnt-footer-height:48.25px");
        cut.Markup.Should().Contain("--tnt-side-nav-width:256.75px");
    }

    [Fact]
    public void RendersOnlyDynamicMeasurementStyle() {
        var cut = RenderMeasurements();

        cut.FindAll("style.tnt-measurements").Should().HaveCount(1);
        cut.FindAll("style.tnt-fonts").Should().BeEmpty();
        cut.FindAll("style.nt-elevations").Should().BeEmpty();
        cut.FindAll("style.nt-corner-radii").Should().BeEmpty();
        cut.FindAll("style.nt-scrollbars").Should().BeEmpty();
        cut.Markup.Should().NotContain(".tnt-display-large");
        cut.Markup.Should().NotContain(".nt-scrollbar");
    }

    [Fact]
    public void RendersCorrectCssStructure() {
        var cut = RenderMeasurements();

        cut.Markup.Should().Contain("<style class=\"tnt-measurements\">");
        cut.Markup.Should().Contain(":root{");
        cut.Markup.Should().Contain("--tnt-header-height:64px;");
        cut.Markup.Should().Contain("--tnt-footer-height:64px;");
        cut.Markup.Should().Contain("--tnt-side-nav-width:256px;");
        cut.Markup.Should().Contain("</style>");
    }

    [Fact]
    public void TokenScopeSelector_CanScopeMeasurementTokens() {
        var cut = RenderMeasurements(parameters => parameters.Add(p => p.TokenScopeSelector, ".app-shell"));

        cut.Markup.Should().Contain(".app-shell{--tnt-header-height:64px;--tnt-footer-height:64px;--tnt-side-nav-width:256px;");
    }

    [Fact]
    public void TokenScopeSelector_NormalizesBlankToRoot() {
        var cut = RenderMeasurements(parameters => parameters.Add(p => p.TokenScopeSelector, " "));

        cut.Markup.Should().Contain(":root{--tnt-header-height:64px;");
    }

    [Fact]
    public void ZeroValues_RenderCorrectly() {
        var cut = RenderMeasurements(parameters => parameters
            .Add(p => p.HeaderHeight, 0)
            .Add(p => p.FooterHeight, 0)
            .Add(p => p.SideNavWidth, 0));

        cut.Markup.Should().Contain("--tnt-header-height:0px");
        cut.Markup.Should().Contain("--tnt-footer-height:0px");
        cut.Markup.Should().Contain("--tnt-side-nav-width:0px");
    }

    private IRenderedComponent<TnTMeasurements> RenderMeasurements(Action<ComponentParameterCollectionBuilder<TnTMeasurements>>? parameterBuilder = null) {
        return Render<TnTMeasurements>(parameters => {
            parameterBuilder?.Invoke(parameters);
        });
    }
}
