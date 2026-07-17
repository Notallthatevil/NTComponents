using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace NTComponents.Tests.Tooltip;

/// <summary>
///     Unit tests for <see cref="NTTooltip" />.
/// </summary>
public class NTTooltip_Tests : BunitContext {

    public NTTooltip_Tests() {
        var tooltipModule = JSInterop.SetupModule("./_content/NTComponents/Tooltip/NTTooltip.razor.js");
        tooltipModule.SetupVoid("onLoad", _ => true);
        tooltipModule.SetupVoid("onUpdate", _ => true);
        tooltipModule.SetupVoid("onDispose", _ => true);
    }

    [Fact]
    public void BackgroundColor_DefaultValue_UsesMaterialPlainToken() {
        // Arrange & Act
        var cut = RenderTooltip();

        // Assert
        cut.Instance.BackgroundColor.Should().BeNull();
        cut.Markup.Should().NotContain("--nt-tooltip-background-color:");
    }

    [Fact]
    public void BackgroundColor_CanBeCustomized() {
        // Arrange & Act
        var cut = RenderTooltip(parameters => parameters
            .Add(p => p.BackgroundColor, TnTColor.Primary));

        // Assert
        cut.Instance.BackgroundColor.Should().Be(TnTColor.Primary);
        cut.Markup.Should().Contain("--nt-tooltip-background-color:var(--tnt-color-primary)");
    }

    [Fact]
    public void TextColor_DefaultValue_UsesMaterialPlainToken() {
        // Arrange & Act
        var cut = RenderTooltip();

        // Assert
        cut.Instance.TextColor.Should().BeNull();
        cut.Markup.Should().NotContain("--nt-tooltip-text-color:");
    }

    [Fact]
    public void TextColor_CanBeCustomized() {
        // Arrange & Act
        var cut = RenderTooltip(parameters => parameters
            .Add(p => p.TextColor, TnTColor.OnPrimary));

        // Assert
        cut.Instance.TextColor.Should().Be(TnTColor.OnPrimary);
        cut.Markup.Should().Contain("--nt-tooltip-text-color:var(--tnt-color-on-primary)");
    }

    [Fact]
    public void BorderColor_DefaultValue_UsesNoVisibleBorder() {
        // Arrange & Act
        var cut = RenderTooltip();

        // Assert
        cut.Instance.BorderColor.Should().BeNull();
        cut.Markup.Should().NotContain("--nt-tooltip-border-color:");
        cut.Markup.Should().NotContain("--nt-tooltip-border-width:");
    }

    [Fact]
    public void BorderColor_CanBeCustomized() {
        // Arrange & Act
        var cut = RenderTooltip(parameters => parameters
            .Add(p => p.BorderColor, TnTColor.Primary));

        // Assert
        cut.Instance.BorderColor.Should().Be(TnTColor.Primary);
        cut.Markup.Should().Contain("--nt-tooltip-border-color:var(--tnt-color-primary)");
        cut.Markup.Should().Contain("--nt-tooltip-border-width:1px");
    }

    [Fact]
    public void DelayVariables_AreRenderedWithNtPrefix() {
        // Arrange & Act
        var cut = RenderTooltip(parameters => parameters
            .Add(p => p.ShowDelay, 800)
            .Add(p => p.HideDelay, 300));

        // Assert
        var style = cut.Find(".nt-tooltip").GetAttribute("style");
        style.Should().Contain("--nt-tooltip-show-delay:800ms");
        style.Should().Contain("--nt-tooltip-hide-delay:300ms");
    }

    [Fact]
    public void ChildContent_IsRenderedInsideTooltip() {
        // Arrange & Act
        var cut = RenderTooltip(parameters => parameters
            .AddChildContent("Tooltip Text"));

        // Assert
        cut.Find(".nt-tooltip-content").TextContent.Should().Contain("Tooltip Text");
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
        cut.Find(".nt-tooltip-content > span").TextContent.Should().Be("Rendered Content");
    }

    [Fact]
    public void ElementClass_ContainsNtTooltipClass() {
        // Arrange & Act
        var cut = RenderTooltip();

        // Assert
        cut.Find(".nt-tooltip").GetAttribute("class").Should().Contain("nt-tooltip");
    }

    [Fact]
    public void ElementClass_IncludesAdditionalAttributes() {
        // Arrange & Act
        var cut = RenderTooltip(parameters => parameters
            .AddUnmatched("class", "custom-tooltip-class"));

        // Assert
        var tooltip = cut.Find(".nt-tooltip");
        tooltip.GetAttribute("class").Should().Contain("custom-tooltip-class");
        tooltip.GetAttribute("class").Should().Contain("nt-tooltip");
    }

    [Fact]
    public void AccessibilityAttributes_AreAppliedToTooltip() {
        // Arrange & Act
        var cut = RenderTooltip(parameters => parameters
            .Add(p => p.ElementId, "tooltip-id")
            .Add(p => p.ElementTitle, "Tooltip title")
            .Add(p => p.ElementLang, "en"));

        // Assert
        var tooltip = cut.Find(".nt-tooltip");
        tooltip.GetAttribute("id").Should().Be("tooltip-id");
        tooltip.GetAttribute("title").Should().Be("Tooltip title");
        tooltip.GetAttribute("lang").Should().Be("en");
        tooltip.GetAttribute("role").Should().Be("tooltip");
        tooltip.GetAttribute("aria-hidden").Should().Be("true");
    }

    [Fact]
    public void GeneratedId_IsRendered() {
        // Arrange & Act
        var cut = RenderTooltip();

        // Assert
        var tooltip = cut.Find(".nt-tooltip");
        tooltip.GetAttribute("id").Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void AdditionalAttributes_AreAppliedToTooltip() {
        // Arrange & Act
        var cut = RenderTooltip(parameters => parameters
            .AddUnmatched("data-testid", "test-tooltip")
            .AddUnmatched("aria-label", "Test Tooltip"));

        // Assert
        var tooltip = cut.Find(".nt-tooltip");
        tooltip.GetAttribute("data-testid").Should().Be("test-tooltip");
        tooltip.GetAttribute("aria-label").Should().Be("Test Tooltip");
    }

    [Fact]
    public void Constructor_InitializesWithMaterialDefaults() {
        // Arrange & Act
        var tooltip = new NTTooltip();

        // Assert
        tooltip.BackgroundColor.Should().BeNull();
        tooltip.TextColor.Should().BeNull();
        tooltip.BorderColor.Should().BeNull();
        tooltip.ShowDelay.Should().Be(500);
        tooltip.HideDelay.Should().Be(200);
        tooltip.Variant.Should().Be(NTTooltipVariant.Plain);
    }

    [Fact]
    public void DataPermanentAttribute_IsPresent() {
        // Arrange & Act
        var cut = RenderTooltip();

        // Assert
        cut.Find(".nt-tooltip").HasAttribute("data-permanent").Should().BeTrue();
    }

    [Fact]
    public void RichVariant_UsesMaterialRichClassAndTokenDefaults() {
        // Arrange & Act
        var cut = RenderTooltip(parameters => parameters
            .Add(p => p.Variant, NTTooltipVariant.Rich));

        // Assert
        var tooltip = cut.Find(".nt-tooltip");
        tooltip.GetAttribute("class").Should().Contain("nt-tooltip-rich");
        cut.Markup.Should().NotContain("--nt-tooltip-background-color:");
        cut.Markup.Should().NotContain("--nt-tooltip-text-color:");
    }

    [Fact]
    public void JsModulePath_ReturnsCorrectPath() {
        // Arrange
        var tooltip = new NTTooltip();

        // Act
        var path = tooltip.JsModulePath;

        // Assert
        path.Should().Be("./_content/NTComponents/Tooltip/NTTooltip.razor.js");
    }

    [Fact]
    public void MultipleTooltips_CanBePlacedInDifferentContainers() {
        // Arrange & Act
        var cut = Render<NTTooltipFragment>(parameters => parameters
            .AddChildContent((builder) => {
                builder.OpenElement(0, "div");
                builder.OpenComponent<NTTooltip>(1);
                builder.AddAttribute(2, nameof(NTTooltip.ChildContent), (RenderFragment)(b => b.AddContent(0, "Tooltip 1")));
                builder.CloseComponent();
                builder.CloseElement();

                builder.OpenElement(3, "div");
                builder.OpenComponent<NTTooltip>(4);
                builder.AddAttribute(5, nameof(NTTooltip.ChildContent), (RenderFragment)(b => b.AddContent(0, "Tooltip 2")));
                builder.CloseComponent();
                builder.CloseElement();
            }));

        // Assert
        cut.FindAll(".nt-tooltip").Should().HaveCount(2);
    }

    private IRenderedComponent<NTTooltip> RenderTooltip(Action<ComponentParameterCollectionBuilder<NTTooltip>>? parameterBuilder = null) {
        return Render<NTTooltip>(parameters => {
            parameterBuilder?.Invoke(parameters);
        });
    }
}

/// <summary>
///     Fragment component for testing multiple NTTooltip instances.
/// </summary>
public class NTTooltipFragment : ComponentBase {

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder) {
        builder.AddContent(0, ChildContent);
    }
}
