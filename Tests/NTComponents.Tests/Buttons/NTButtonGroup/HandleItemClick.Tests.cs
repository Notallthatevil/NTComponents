using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using Microsoft.AspNetCore.Components;

namespace NTComponents.Tests.Buttons.NTButtonGroup;

/// <summary>
///     Tests the <c>HandleItemClickAsync</c> behavior for the button group.
/// </summary>
public sealed class HandleItemClick_Tests : NTButtonGroupTestContext {
    /// <summary>
    ///     Validates that clicking an unselected button updates the selection and raises the selection changed event.
    /// </summary>
    [Fact]
    public void GivenUnselectedItem_WhenClicked_RaisesSelectionChangedAndMarksButton() {
        // Arrange
        var items = CreateItems();
        var recordedKeys = new List<string?>();
        var cut = Render<NTButtonGroup<string>>(parameters => parameters
            .AddChildContent(RenderItems(items))
            .Add(p => p.SelectedKeyChanged, EventCallback.Factory.Create<string?>(this, key => recordedKeys.Add(key))));
        var buttonElements = cut.FindAll("button.nt-btn-grp-btn");

        // Act
        buttonElements[1].Click();
        var updatedButtons = cut.FindAll("button.nt-btn-grp-btn");

        // Assert
        recordedKeys.Should().Equal(items.Last().Key);
        updatedButtons[1].GetAttribute("aria-pressed").Should().Be("true");
        updatedButtons[0].GetAttribute("aria-pressed").Should().Be("false");
    }

    /// <summary>
    ///     Validates that clicking the already selected button clears optional single selection.
    /// </summary>
    [Fact]
    public async Task GivenSelectedItem_WhenClickedAgain_ClearsSelection() {
        // Arrange
        var items = CreateItems();
        var recordedKeys = new List<string?>();
        var cut = Render<NTButtonGroup<string>>(parameters => parameters
            .AddChildContent(RenderItems(items))
            .Add(p => p.SelectedKey, items.First().Key)
            .Add(p => p.SelectedKeyChanged, EventCallback.Factory.Create<string?>(this, key => recordedKeys.Add(key))));
        var buttonElements = cut.FindAll("button.nt-btn-grp-btn");

        // Act
        await buttonElements[0].ClickAsync();
        var updatedButtons = cut.FindAll("button.nt-btn-grp-btn");

        // Assert
        recordedKeys.Should().Equal([null]);
        updatedButtons[0].GetAttribute("aria-pressed").Should().Be("false");
    }

    /// <summary>
    ///     Validates that clicking the selected button keeps selection when selection is required.
    /// </summary>
    [Fact]
    public async Task GivenRequiredSelectedItem_WhenClickedAgain_DoesNotClearSelection() {
        // Arrange
        var items = CreateItems();
        var recordedKeys = new List<string?>();
        var cut = Render<NTButtonGroup<string>>(parameters => parameters
            .AddChildContent(RenderItems(items))
            .Add(p => p.SelectedKey, items.First().Key)
            .Add(p => p.SelectionRequired, true)
            .Add(p => p.SelectedKeyChanged, EventCallback.Factory.Create<string?>(this, key => recordedKeys.Add(key))));
        var buttonElements = cut.FindAll("button.nt-btn-grp-btn");

        // Act
        await buttonElements[0].ClickAsync();
        var updatedButtons = cut.FindAll("button.nt-btn-grp-btn");

        // Assert
        recordedKeys.Should().BeEmpty();
        updatedButtons[0].GetAttribute("aria-pressed").Should().Be("true");
    }

    /// <summary>
    ///     Validates that multi-select mode toggles independent selected keys.
    /// </summary>
    [Fact]
    public async Task GivenMultipleSelectionMode_WhenItemsClicked_UpdatesSelectedKeys() {
        // Arrange
        var items = CreateItems();
        var recordedKeys = new List<IReadOnlyCollection<string>>();
        var cut = Render<NTButtonGroup<string>>(parameters => parameters
            .AddChildContent(RenderItems(items))
            .Add(p => p.SelectionMode, NTButtonGroupSelectionMode.Multiple)
            .Add(p => p.SelectedKeysChanged, EventCallback.Factory.Create<IReadOnlyCollection<string>>(this, keys => recordedKeys.Add(keys))));
        var buttonElements = cut.FindAll("button.nt-btn-grp-btn");

        // Act
        await buttonElements[0].ClickAsync();
        buttonElements = cut.FindAll("button.nt-btn-grp-btn");
        await buttonElements[1].ClickAsync();
        var updatedButtons = cut.FindAll("button.nt-btn-grp-btn");

        // Assert
        recordedKeys.Should().HaveCount(2);
        recordedKeys.Last().Should().Equal(items.Select(item => item.Key));
        updatedButtons.Should().AllSatisfy(button => button.GetAttribute("aria-pressed").Should().Be("true"));
    }
}
