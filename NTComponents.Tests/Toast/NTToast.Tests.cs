using Microsoft.AspNetCore.Components;
using NTComponents.Toast;

namespace NTComponents.Tests.Toast;

/// <summary>
///     bUnit tests for <see cref="NTToast" />.
/// </summary>
public class NTToast_Tests : BunitContext {
    [Fact]
    public void Render_RendersStaticHostAndModuleOnly() {
        // Arrange & Act
        var cut = RenderToastComponent();

        // Assert
        cut.Find(".nt-toast-container").GetAttribute("data-nt-toast-host").Should().Be("true");
        cut.Find("tnt-page-script").GetAttribute("src").Should().Be(NTToast.JsModulePathValue);
        cut.FindAll(".nt-toast").Should().BeEmpty();
        cut.FindAll("script").Should().BeEmpty();
    }

    [Fact]
    public void Toast_DefaultsToBottomRightPosition() {
        // Arrange & Act
        var cut = RenderToastComponent();

        // Assert
        cut.Find(".nt-toast-container").ClassList.Should().Contain("nt-toast-bottom-right-corner");
    }

    [Fact]
    public void Toast_UsesConfiguredPositionClass() {
        // Arrange & Act
        var cut = RenderToastComponent(parameters => parameters.Add(p => p.Position, NTToastPosition.TopLeftCorner));

        // Assert
        var container = cut.Find(".nt-toast-container");
        container.ClassList.Should().Contain("nt-toast-top-left-corner");
        container.ClassList.Should().NotContain("nt-toast-bottom-right-corner");
    }

    private IRenderedComponent<NTToast> RenderToastComponent(Action<ComponentParameterCollectionBuilder<NTToast>>? configure = null) {
        return configure is null ? Render<NTToast>() : Render<NTToast>(configure);
    }
}
