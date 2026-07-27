using System.Linq;
using AwesomeAssertions;

namespace NTComponents.Tests.Buttons.NTButtonGroup;

/// <summary>
///     Tests the <c>OnParametersSet</c> lifecycle handling for the button group.
/// </summary>
public sealed class OnParametersSet_Tests : NTButtonGroupTestContext {
    /// <summary>
    ///     Ensures an item marked as default selected is active when no explicit SelectedKey is provided.
    /// </summary>
    [Fact]
    public void WithDefaultSelectedItem_WhenNoSelectedKey_SelectsDefaultItem() {
        // Arrange
        var items = CreateItems(defaultSecondItem: true);

        // Act
        var cut = Render<NTButtonGroup<string>>(parameters => parameters.AddChildContent(RenderItems(items)));
        var selectedButtons = cut.FindAll("button[aria-pressed='true']");

        // Assert
        selectedButtons.Count.Should().Be(1);
        selectedButtons.Single().TextContent.Should().Contain(items.Last().Label!);
    }

    /// <summary>
    ///     Validates that an explicit SelectedKey overrides any default selection.
    /// </summary>
    [Fact]
    public void WithExplicitSelectedKey_WhenDefaultItemExists_SelectsExplicitItem() {
        // Arrange
        var items = CreateItems(defaultSecondItem: true);
        var explicitKey = items.First().Key;

        // Act
        var cut = Render<NTButtonGroup<string>>(parameters => parameters
            .AddChildContent(RenderItems(items))
            .Add(p => p.SelectedKey, explicitKey));
        var selectedButtons = cut.FindAll("button[aria-pressed='true']");

        // Assert
        selectedButtons.Count.Should().Be(1);
        selectedButtons.Single().TextContent.Should().Contain(items.First().Label!);
    }

    /// <summary>
    ///     Ensures selection-required groups select the first enabled item when no default is provided.
    /// </summary>
    [Fact]
    public void WithSelectionRequired_WhenNoSelectedKey_SelectsFirstItem() {
        // Arrange
        var items = CreateItems();

        // Act
        var cut = Render<NTButtonGroup<string>>(parameters => parameters
            .AddChildContent(RenderItems(items))
            .Add(p => p.SelectionRequired, true));
        var selectedButtons = cut.FindAll("button[aria-pressed='true']");

        // Assert
        selectedButtons.Count.Should().Be(1);
        selectedButtons.Single().TextContent.Should().Contain(items.First().Label!);
    }

    // Behavior source: NTButtonGroup.SelectionRequired and NTButtonGroupItem.Disabled XML documentation require an active, usable option.
    [Fact]
    public void WithSelectionRequired_WhenFirstItemDisabled_SelectsFirstEnabledItem() {
        // Arrange
        var items = new[] {
            new NTButtonGroupTestItem { Key = "disabled", Label = "Disabled", Disabled = true },
            new NTButtonGroupTestItem { Key = "enabled", Label = "Enabled" }
        };

        // Act
        var cut = Render<NTButtonGroup<string>>(parameters => parameters
            .AddChildContent(RenderItems(items))
            .Add(p => p.SelectionRequired, true));

        // Assert
        var buttons = cut.FindAll("button.nt-btn-grp-btn");
        buttons[0].HasAttribute("disabled").Should().BeTrue();
        buttons[0].GetAttribute("aria-pressed").Should().Be("false");
        buttons[1].GetAttribute("aria-pressed").Should().Be("true");
    }

    // Behavior source: NTButtonGroup.SelectionMode and SelectionRequired XML documentation applies the same required-choice invariant to Multiple mode.
    [Fact]
    public void WithRequiredMultipleSelection_WhenNoKeysProvided_SelectsFirstEnabledItem() {
        // Arrange
        var items = new[] {
            new NTButtonGroupTestItem { Key = "disabled", Label = "Disabled", Disabled = true },
            new NTButtonGroupTestItem { Key = "enabled", Label = "Enabled" }
        };

        // Act
        var cut = Render<NTButtonGroup<string>>(parameters => parameters
            .AddChildContent(RenderItems(items))
            .Add(p => p.SelectionMode, NTButtonGroupSelectionMode.Multiple)
            .Add(p => p.SelectionRequired, true));

        // Assert
        cut.FindAll("button[aria-pressed='true']").Should().ContainSingle().Which.TextContent.Should().Contain("Enabled");
    }

}
