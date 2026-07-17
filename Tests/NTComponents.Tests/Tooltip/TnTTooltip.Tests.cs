using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace NTComponents.Tests.Tooltip;

/// <summary>
///     Unit tests for <see cref="TnTTooltip" />.
/// </summary>
public class TnTTooltip_Tests : BunitContext {

    public TnTTooltip_Tests() {
        // Set up JavaScript module for tooltip functionality
        // The tooltip uses a custom HTML element (tnt-tooltip) that manages its own lifecycle
        // via connectedCallback/disconnectedCallback, but we still mock the module exports
        var tooltipModule = JSInterop.SetupModule("./_content/NTComponents/Tooltip/TnTTooltip.razor.js");
        tooltipModule.SetupVoid("onLoad", _ => true);
        tooltipModule.SetupVoid("onUpdate", _ => true);
        tooltipModule.SetupVoid("onDispose", _ => true);
    }

    [Fact]
    public void BackgroundColor_DefaultValue_IsSurfaceVariant() {
        // Arrange & Act
        var cut = RenderTooltip();

        // Assert
        cut.Instance.BackgroundColor.Should().Be(TnTColor.SurfaceVariant);
    }

    [Fact]
    public void BackgroundColor_CanBeCustomized() {
        // Arrange & Act
        var cut = RenderTooltip(parameters => parameters
            .Add(p => p.BackgroundColor, TnTColor.Primary));

        // Assert
        cut.Instance.BackgroundColor.Should().Be(TnTColor.Primary);
        cut.Markup.Should().Contain("--tnt-tooltip-background-color:var(--tnt-color-primary)");
    }

    [Fact]
    public void TextColor_DefaultValue_IsOnSurfaceVariant() {
        // Arrange & Act
        var cut = RenderTooltip();

        // Assert
        cut.Instance.TextColor.Should().Be(TnTColor.OnSurfaceVariant);
    }

    [Fact]
    public void TextColor_CanBeCustomized() {
        // Arrange & Act
        var cut = RenderTooltip(parameters => parameters
            .Add(p => p.TextColor, TnTColor.OnPrimary));

        // Assert
        cut.Instance.TextColor.Should().Be(TnTColor.OnPrimary);
        cut.Markup.Should().Contain("--tnt-tooltip-text-color:var(--tnt-color-on-primary)");
    }

    [Fact]
    public void ShowDelay_DefaultValue_Is500Milliseconds() {
        // Arrange & Act
        var cut = RenderTooltip();

        // Assert
        cut.Instance.ShowDelay.Should().Be(500);
    }

    [Fact]
    public void ShowDelay_CanBeCustomized() {
        // Arrange & Act
        var cut = RenderTooltip(parameters => parameters
            .Add(p => p.ShowDelay, 1000));

        // Assert
        cut.Instance.ShowDelay.Should().Be(1000);
        cut.Markup.Should().Contain("--tnt-tooltip-show-delay:1000ms");
    }

    [Fact]
    public void HideDelay_DefaultValue_Is200Milliseconds() {
        // Arrange & Act
        var cut = RenderTooltip();

        // Assert
        cut.Instance.HideDelay.Should().Be(200);
    }

    [Fact]
    public void HideDelay_CanBeCustomized() {
        // Arrange & Act
        var cut = RenderTooltip(parameters => parameters
            .Add(p => p.HideDelay, 500));

        // Assert
        cut.Instance.HideDelay.Should().Be(500);
        cut.Markup.Should().Contain("--tnt-tooltip-hide-delay:500ms");
    }

    [Fact]
    public void ChildContent_IsRenderedInsideTooltip() {
        // Arrange & Act
        var cut = RenderTooltip(parameters => parameters
            .AddChildContent("Tooltip Text"));

        // Assert
        cut.Markup.Should().Contain("Tooltip Text");
        cut.Find(".tnt-tooltip-content").TextContent.Should().Contain("Tooltip Text");
    }

    [Fact]
    public void ChildContent_CanBeRenderFragment() {
        // Arrange & Act
        var cut = RenderTooltip(parameters => parameters
            .AddChildContent((builder) => {
                builder.OpenElement(0, "span");
                builder.AddContent(1, "Rendered Content");
                builder.CloseElement();
            }));

        // Assert
        cut.Find(".tnt-tooltip-content > span").Should().NotBeNull();
        cut.Find(".tnt-tooltip-content > span").TextContent.Should().Be("Rendered Content");
    }

    [Fact]
    public void ElementClass_ContainsTooltipClass() {
        // Arrange & Act
        var cut = RenderTooltip();

        // Assert
        cut.Find(".tnt-tooltip").Should().NotBeNull();
        cut.Find(".tnt-tooltip").GetAttribute("class").Should().Contain("tnt-tooltip");
    }

    [Fact]
    public void ElementClass_IncludesAdditionalAttributes() {
        // Arrange & Act
        var cut = RenderTooltip(parameters => parameters
            .AddUnmatched("class", "custom-tooltip-class"));

        // Assert
        cut.Find(".tnt-tooltip").GetAttribute("class").Should().Contain("custom-tooltip-class");
        cut.Find(".tnt-tooltip").GetAttribute("class").Should().Contain("tnt-tooltip");
    }

    [Fact]
    public void ElementStyle_IncludesColorVariables() {
        // Arrange & Act
        var cut = RenderTooltip(parameters => parameters
            .Add(p => p.BackgroundColor, TnTColor.Error)
            .Add(p => p.TextColor, TnTColor.OnError));

        // Assert
        var style = cut.Find(".tnt-tooltip").GetAttribute("style");
        style.Should().Contain("--tnt-tooltip-background-color:var(--tnt-color-error)");
        style.Should().Contain("--tnt-tooltip-text-color:var(--tnt-color-on-error)");
    }

    [Fact]
    public void ElementStyle_IncludesBorderColorVariable() {
        // Arrange & Act
        var cut = RenderTooltip(parameters => parameters
            .Add(p => p.BorderColor, TnTColor.Secondary));

        // Assert
        var style = cut.Find(".tnt-tooltip").GetAttribute("style");
        style.Should().Contain("--tnt-tooltip-border-color:var(--tnt-color-secondary)");
    }

    [Fact]
    public void ElementStyle_IncludesDelayVariables() {
        // Arrange & Act
        var cut = RenderTooltip(parameters => parameters
            .Add(p => p.ShowDelay, 800)
            .Add(p => p.HideDelay, 300));

        // Assert
        var style = cut.Find(".tnt-tooltip").GetAttribute("style");
        style.Should().Contain("--tnt-tooltip-show-delay:800ms");
        style.Should().Contain("--tnt-tooltip-hide-delay:300ms");
    }

    [Fact]
    public void JsModulePath_ReturnsCorrectPath() {
        // Arrange
        var tooltip = new TnTTooltip();

        // Act
        var path = tooltip.JsModulePath;

        // Assert
        path.Should().Be("./_content/NTComponents/Tooltip/TnTTooltip.razor.js");
    }

    [Fact]
    public void AdditionalAttributes_AreAppliedToTooltip() {
        // Arrange & Act
        var cut = RenderTooltip(parameters => parameters
            .AddUnmatched("data-testid", "test-tooltip")
            .AddUnmatched("aria-label", "Test Tooltip"));

        // Assert
        var tooltip = cut.Find(".tnt-tooltip");
        tooltip.GetAttribute("data-testid").Should().Be("test-tooltip");
        tooltip.GetAttribute("aria-label").Should().Be("Test Tooltip");
    }

    [Fact]
    public void ChildContent_WithComplexMarkup_RendersCorrectly() {
        // Arrange & Act
        var cut = RenderTooltip(parameters => parameters
            .AddChildContent((builder) => {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "tooltip-inner");
                builder.OpenElement(2, "strong");
                builder.AddContent(3, "Title");
                builder.CloseElement();
                builder.OpenElement(4, "p");
                builder.AddContent(5, "Description");
                builder.CloseElement();
                builder.CloseElement();
            }));

        // Assert
        var content = cut.Find(".tnt-tooltip-content .tooltip-inner");
        content.Should().NotBeNull();
        var strong = content.QuerySelector("strong");
        var paragraph = content.QuerySelector("p");
        strong.Should().NotBeNull();
        paragraph.Should().NotBeNull();
        strong!.TextContent.Should().Be("Title");
        paragraph!.TextContent.Should().Be("Description");
    }

    [Fact]
    public void Constructor_InitializesWithDefaultValues() {
        // Arrange & Act
        var tooltip = new TnTTooltip();

        // Assert
        tooltip.BackgroundColor.Should().Be(TnTColor.SurfaceVariant);
        tooltip.TextColor.Should().Be(TnTColor.OnSurfaceVariant);
        tooltip.BorderColor.Should().Be(TnTColor.Outline);
        tooltip.ShowDelay.Should().Be(500);
        tooltip.HideDelay.Should().Be(200);
    }

    [Fact]
    public void RenderFragment_IsNotNull_WhenNotSet() {
        // Arrange & Act
        var tooltip = new TnTTooltip();

        // Assert
        tooltip.ChildContent.Should().BeNull();
    }

    [Fact]
    public void DataPermanentAttribute_IsPresent() {
        // Arrange & Act
        var cut = RenderTooltip();

        // Assert
        cut.Find(".tnt-tooltip").HasAttribute("data-permanent").Should().BeTrue();
    }

    [Fact]
    public void MultipleTooltips_CanBePlacedInDifferentContainers() {
        // Arrange & Act
        var cut = Render<Fragment>(parameters => parameters
            .AddChildContent((builder) => {
                builder.OpenComponent<TnTTooltip>(0);
                builder.AddAttribute(1, nameof(TnTTooltip.ChildContent), (RenderFragment)(b => b.AddContent(0, "Tooltip 1")));
                builder.CloseComponent();

                builder.OpenComponent<TnTTooltip>(2);
                builder.AddAttribute(3, nameof(TnTTooltip.ChildContent), (RenderFragment)(b => b.AddContent(0, "Tooltip 2")));
                builder.CloseComponent();
            }));

        // Assert
        var tooltips = cut.FindAll(".tnt-tooltip");
        tooltips.Should().HaveCount(2);
    }

    [Fact]
    public void BorderColor_DefaultValue_IsOutline() {
        // Arrange & Act
        var cut = RenderTooltip();

        // Assert
        cut.Instance.BorderColor.Should().Be(TnTColor.Outline);
    }

    [Fact]
    public void BorderColor_CanBeCustomized() {
        // Arrange & Act
        var cut = RenderTooltip(parameters => parameters
            .Add(p => p.BorderColor, TnTColor.Primary));

        // Assert
        cut.Instance.BorderColor.Should().Be(TnTColor.Primary);
        cut.Markup.Should().Contain("--tnt-tooltip-border-color:var(--tnt-color-primary)");
    }

    private IRenderedComponent<TnTTooltip> RenderTooltip(
        Action<ComponentParameterCollectionBuilder<TnTTooltip>>? parameterBuilder = null) {
        return Render<TnTTooltip>(parameters => {
            parameterBuilder?.Invoke(parameters);
        });
    }
}

/// <summary>
/// Fragment component for testing multiple tooltips
/// </summary>
public class Fragment : ComponentBase {
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder) {
        builder.AddContent(0, ChildContent);
    }
}
