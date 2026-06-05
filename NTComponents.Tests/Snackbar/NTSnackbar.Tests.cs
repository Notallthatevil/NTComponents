using Microsoft.AspNetCore.Components;
using NTComponents.Snackbar;

namespace NTComponents.Tests.Snackbar;

/// <summary>
///     bUnit tests for <see cref="NTSnackbar" />.
/// </summary>
public class NTSnackbar_Tests : BunitContext {

    [Fact]
    public void Constructor_InitializesCorrectly() {
        // Arrange & Act
        var cut = RenderSnackbarComponent();

        // Assert
        cut.Should().NotBeNull();
        cut.Instance.Should().NotBeNull();
    }

    [Fact]
    public void Render_RendersStaticHostAndModuleOnly() {
        // Arrange & Act
        var cut = RenderSnackbarComponent();

        // Assert
        cut.Find(".nt-snackbar-container").GetAttribute("data-nt-snackbar-host").Should().Be("true");
        cut.Find("tnt-page-script").GetAttribute("src").Should().Be(NTSnackbar.JsModulePathValue);
        cut.FindAll(".nt-snackbar").Should().BeEmpty();
        cut.FindAll("script").Should().BeEmpty();
    }

    [Fact]
    public void Snackbar_DefaultsToBottomCenterPosition() {
        // Arrange & Act
        var cut = RenderSnackbarComponent();

        // Assert
        cut.Find(".nt-snackbar-container").ClassList.Should().Contain("nt-snackbar-bottom-center");
    }

    [Fact]
    public void Snackbar_UsesConfiguredPositionClass() {
        // Arrange & Act
        var cut = RenderSnackbarComponent(parameters => parameters.Add(p => p.Position, NTSnackbarPosition.TopRightCorner));

        // Assert
        var container = cut.Find(".nt-snackbar-container");
        container.ClassList.Should().Contain("nt-snackbar-top-right-corner");
        container.ClassList.Should().NotContain("nt-snackbar-bottom-center");
    }

    private IRenderedComponent<NTSnackbar> RenderSnackbarComponent(Action<ComponentParameterCollectionBuilder<NTSnackbar>>? configure = null) {
        return configure is null ? Render<NTSnackbar>() : Render<NTSnackbar>(configure);
    }
}
