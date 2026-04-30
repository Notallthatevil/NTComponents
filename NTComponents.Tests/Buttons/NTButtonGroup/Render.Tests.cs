using System.Linq;
using AwesomeAssertions;
using Microsoft.AspNetCore.Components;
using NTComponents.Core;

namespace NTComponents.Tests.Buttons.NTButtonGroup;

/// <summary>
///     Verifies the rendering behavior of the <see cref="NTButtonGroup{TObjectType}" /> component.
/// </summary>
public sealed class Render_Tests : NTButtonGroupTestContext {
    /// <summary>
    ///     Ensures the component renders one button for each provided item.
    /// </summary>
    [Fact]
    public void WithMultipleItems_RendersAButtonForEachItem() {
        // Arrange
        var items = CreateItems();

        // Act
        var cut = Render<NTButtonGroup<string>>(parameters => parameters.AddChildContent(RenderItems(items)));
        var buttons = cut.FindAll("button.nt-btn-grp-btn");

        // Assert
        buttons.Count.Should().Be(items.Count);
    }

    /// <summary>
    ///     Ensures the button interaction initializer is emitted once for the group instead of once per item.
    /// </summary>
    [Fact]
    public void WithMultipleItems_RendersOneButtonInteractionInitializer() {
        // Arrange
        var items = CreateItems();

        // Act
        var cut = Render<NTButtonGroup<string>>(parameters => parameters
            .AddChildContent(RenderItems(items))
            .Add(p => p.DisableRipple, true));
        var scripts = cut.FindAll("script");

        // Assert
        scripts.Should().ContainSingle();
        scripts[0].TextContent.Should().Contain("startButtonInteraction");
    }

    /// <summary>
    ///     Ensures the group-level interaction initializer is not duplicated when ripple hosts already register each button.
    /// </summary>
    [Fact]
    public void WithRippleEnabled_DoesNotRenderGroupInteractionInitializer() {
        // Arrange
        var items = CreateItems();

        // Act
        var cut = Render<NTButtonGroup<string>>(parameters => parameters.AddChildContent(RenderItems(items)));

        // Assert
        cut.FindAll("script").Should().HaveCount(items.Count);
        cut.Markup.Should().NotContain("startButtonInteraction");
    }

    /// <summary>
    ///     Validates that the disconnected display type emits the correct CSS modifier.
    /// </summary>
    [Fact]
    public void WithDisconnectedDisplayType_AddsDisconnectedModifier() {
        // Arrange
        var items = CreateItems();

        // Act
        var cut = Render<NTButtonGroup<string>>(parameters => parameters
            .AddChildContent(RenderItems(items))
            .Add(p => p.DisplayType, NTButtonGroupDisplayType.Disconnected));
        var container = cut.Find("div.nt-button-group");

        // Assert
        var classes = container.GetAttribute("class")!;
        classes.Should().Contain("nt-button-group-disconnected");
        classes.Should().NotContain("nt-button-group-connected");
    }

    /// <summary>
    ///     Validates full-width layout is opt-in rather than forced by connected groups.
    /// </summary>
    [Fact]
    public void WithConnectedDisplayType_WhenFullWidthFalse_DoesNotAddFullWidthModifier() {
        // Arrange
        var items = CreateItems();

        // Act
        var cut = Render<NTButtonGroup<string>>(parameters => parameters
            .AddChildContent(RenderItems(items))
            .Add(p => p.DisplayType, NTButtonGroupDisplayType.Connected));
        var container = cut.Find("div.nt-button-group");

        // Assert
        container.ClassList.Should().Contain("nt-button-group-connected");
        container.ClassList.Should().NotContain("nt-button-group-full-width");
    }

    /// <summary>
    ///     Validates full-width layout can be requested explicitly.
    /// </summary>
    [Fact]
    public void WithFullWidth_AddsFullWidthModifier() {
        // Arrange
        var items = CreateItems();

        // Act
        var cut = Render<NTButtonGroup<string>>(parameters => parameters
            .AddChildContent(RenderItems(items))
            .Add(p => p.FullWidth, true));
        var container = cut.Find("div.nt-button-group");

        // Assert
        container.ClassList.Should().Contain("nt-button-group-full-width");
    }

    /// <summary>
    ///     Ensures the group emits the shared size class consumed by the component stylesheet.
    /// </summary>
    [Fact]
    public void WithButtonSize_AddsGroupSizeClass() {
        // Arrange
        var items = CreateItems();

        // Act
        var cut = Render<NTButtonGroup<string>>(parameters => parameters
            .AddChildContent(RenderItems(items))
            .Add(p => p.ButtonSize, Size.Large));
        var container = cut.Find("div.nt-button-group");

        // Assert
        container.ClassList.Should().Contain("tnt-size-l");
    }

    /// <summary>
    ///     Ensures icon-only items render the spec-aligned icon button variant with an accessible name.
    /// </summary>
    [Fact]
    public void WithIconOnlyItem_RendersIconButtonWithAccessibleName() {
        // Arrange
        var items = CreateItems(iconOnlyFirstItem: true);

        // Act
        var cut = Render<NTButtonGroup<string>>(parameters => parameters.AddChildContent(RenderItems(items)));
        var iconButton = cut.Find("button.nt-btn-grp-btn");

        // Assert
        iconButton.GetAttribute("aria-label").Should().Be(items.First().AriaLabel);
    }

    /// <summary>
    ///     Ensures icon-only items cannot render without an explicit accessible name.
    /// </summary>
    [Fact]
    public void WithIconOnlyItemWithoutAriaLabel_Throws() {
        // Arrange
        RenderFragment items = builder => {
            builder.OpenComponent<NTButtonGroupItem<string>>(0);
            builder.AddAttribute(1, nameof(NTButtonGroupItem<string>.Key), "search");
            builder.AddAttribute(2, nameof(NTButtonGroupItem<string>.Icon), (object)MaterialIcon.Search);
            builder.CloseComponent();
        };

        // Act
        var act = () => Render<NTButtonGroup<string>>(parameters => parameters.AddChildContent(items));

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*AriaLabel*icon-only*");
    }

    /// <summary>
    ///     Ensures selected items expose toggle state to assistive technology.
    /// </summary>
    [Fact]
    public void WithSelectedItem_RendersAriaPressed() {
        // Arrange
        var items = CreateItems();

        // Act
        var cut = Render<NTButtonGroup<string>>(parameters => parameters
            .AddChildContent(RenderItems(items))
            .Add(p => p.SelectedKey, items.First().Key));
        var buttons = cut.FindAll("button.nt-btn-grp-btn");

        // Assert
        buttons[0].GetAttribute("aria-pressed").Should().Be("true");
        buttons[1].GetAttribute("aria-pressed").Should().Be("false");
    }

    /// <summary>
    ///     Ensures action-only groups do not expose toggle state.
    /// </summary>
    [Fact]
    public async Task WithNoSelectionMode_DoesNotRenderAriaPressedOrSelectedState() {
        // Arrange
        var items = CreateItems();
        var cut = Render<NTButtonGroup<string>>(parameters => parameters
            .AddChildContent(RenderItems(items))
            .Add(p => p.SelectionMode, NTButtonGroupSelectionMode.None)
            .Add(p => p.SelectedKey, items.First().Key));
        var buttons = cut.FindAll("button.nt-btn-grp-btn");

        // Act
        await buttons[0].ClickAsync();
        buttons = cut.FindAll("button.nt-btn-grp-btn");

        // Assert
        buttons.Should().AllSatisfy(button => {
            button.HasAttribute("aria-pressed").Should().BeFalse();
        });
    }

    /// <summary>
    ///     Ensures the deprecated end icon parameter is ignored by the group renderer.
    /// </summary>
    [Fact]
    public void WithEndIcon_DoesNotRenderEndIcon() {
        // Arrange
#pragma warning disable CS0618
        RenderFragment items = builder => {
            builder.OpenComponent<NTButtonGroupItem<string>>(0);
            builder.AddAttribute(1, nameof(NTButtonGroupItem<string>.Key), "mail");
            builder.AddAttribute(2, nameof(NTButtonGroupItem<string>.Label), "Mail");
            builder.AddAttribute(3, nameof(NTButtonGroupItem<string>.EndIcon), (object)new MaterialIcon("mail"));
            builder.CloseComponent();
        };
#pragma warning restore CS0618

        // Act
        var cut = Render<NTButtonGroup<string>>(parameters => parameters.AddChildContent(items));

        // Assert
        cut.FindAll("span.nt-button-icon").Should().BeEmpty();
    }
}
