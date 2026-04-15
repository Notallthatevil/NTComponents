using Microsoft.AspNetCore.Components;
using NTComponents.Popover;

namespace NTComponents.Tests.Popover;

/// <summary>
///     Unit tests for <see cref="NTPopoverService" />.
/// </summary>
public class NTPopoverService_Tests {

    [Fact]
    public async Task BringToFrontAsync_AssignsHigherZIndex() {
        // Arrange
        var service = new NTPopoverService();
        var first = await service.OpenAsync<TestComponent>(new NTPopoverOptions { Title = "First" });
        var second = await service.OpenAsync<TestComponent>(new NTPopoverOptions { Title = "Second" });

        // Act
        await service.BringToFrontAsync(first);

        // Assert
        first.ZIndex.Should().BeGreaterThan(second.ZIndex);
    }

    [Fact]
    public async Task BringToFrontAsync_ForFrontmostPopover_DoesNotRaiseChanged() {
        // Arrange
        var service = new NTPopoverService();
        var changedCount = 0;
        service.OnChanged += () => {
            changedCount++;
            return Task.CompletedTask;
        };

        await service.OpenAsync<TestComponent>(new NTPopoverOptions { Title = "First" });
        var second = await service.OpenAsync<TestComponent>(new NTPopoverOptions { Title = "Second" });
        changedCount = 0;

        // Act
        await service.BringToFrontAsync(second);

        // Assert
        changedCount.Should().Be(0);
    }

    [Fact]
    public async Task CloseAsync_RemovesPopoverFromCollection() {
        // Arrange
        var service = new NTPopoverService();
        var popover = await service.OpenAsync<TestComponent>();

        // Act
        await service.CloseAsync(popover);

        // Assert
        service.GetPopovers().Should().BeEmpty();
    }

    [Fact]
    public async Task HideAsync_MarksPopoverInvisibleWithoutRemovingIt() {
        // Arrange
        var service = new NTPopoverService();
        var popover = await service.OpenAsync<TestComponent>();

        // Act
        await service.HideAsync(popover);

        // Assert
        service.GetPopovers().Should().ContainSingle();
        popover.IsVisible.Should().BeFalse();
    }

    [Fact]
    public async Task OpenAsync_ForRenderFragment_PreservesChildContent() {
        // Arrange
        var service = new NTPopoverService();

        // Act
        var popover = await service.OpenAsync(builder => {
            builder.OpenElement(0, "span");
            builder.AddContent(1, "Inline");
            builder.CloseElement();
        });

        // Assert
        popover.ChildContent.Should().NotBeNull();
        popover.Type.Should().BeNull();
    }

    [Fact]
    public async Task OpenAsync_Generic_UsesProvidedOptionsAndDefaultsToVisible() {
        // Arrange
        var service = new NTPopoverService();

        // Act
        var popover = await service.OpenAsync<TestComponent>(new NTPopoverOptions {
            InitialLeft = 128,
            InitialTop = 256,
            Title = "Inspector"
        });

        // Assert
        popover.IsVisible.Should().BeTrue();
        popover.Left.Should().Be(128);
        popover.Top.Should().Be(256);
        popover.Options.Title.Should().Be("Inspector");
        popover.Type.Should().Be(typeof(TestComponent));
    }

    [Fact]
    public async Task OpenAsync_WithMatchingInstanceKey_ReusesExistingPopover() {
        // Arrange
        var service = new NTPopoverService();

        // Act
        var first = await service.OpenAsync<TestComponent>(new NTPopoverOptions {
            InstanceKey = "inspector",
            Title = "Inspector"
        });
        var second = await service.OpenAsync<TestComponent>(new NTPopoverOptions {
            InstanceKey = "inspector",
            Title = "Inspector"
        });

        // Assert
        second.Should().BeSameAs(first);
        service.GetPopovers().Should().ContainSingle();
    }

    [Fact]
    public async Task OpenAsync_WithMatchingVisibleInstanceKey_RequestsHighlight() {
        // Arrange
        var service = new NTPopoverService();
        var changedCount = 0;
        service.OnChanged += () => {
            changedCount++;
            return Task.CompletedTask;
        };

        var first = await service.OpenAsync<TestComponent>(new NTPopoverOptions {
            InstanceKey = "inspector",
            Title = "Inspector"
        });
        var highlightState = first.Should().BeAssignableTo<INTPopoverHighlightState>().Which;
        changedCount = 0;

        // Act
        var second = await service.OpenAsync<TestComponent>(new NTPopoverOptions {
            InstanceKey = "inspector",
            Title = "Inspector"
        });

        // Assert
        second.Should().BeSameAs(first);
        highlightState.HighlightRequestId.Should().Be(1);
        changedCount.Should().Be(1);
    }

    [Fact]
    public async Task OpenAsync_WithMatchingHiddenInstanceKey_ShowsExistingPopover() {
        // Arrange
        var service = new NTPopoverService();
        var popover = await service.OpenAsync<TestComponent>(new NTPopoverOptions {
            InstanceKey = "notes",
            Title = "Notes"
        });
        await service.HideAsync(popover);

        // Act
        var reopened = await service.OpenAsync<TestComponent>(new NTPopoverOptions {
            InstanceKey = "notes",
            Title = "Notes"
        });

        // Assert
        reopened.Should().BeSameAs(popover);
        reopened.IsVisible.Should().BeTrue();
        service.GetPopovers().Should().ContainSingle();
    }

    [Fact]
    public async Task ShowAsync_RestoresPopoverVisibility() {
        // Arrange
        var service = new NTPopoverService();
        var popover = await service.OpenAsync<TestComponent>();
        await service.HideAsync(popover);

        // Act
        await service.ShowAsync(popover);

        // Assert
        popover.IsVisible.Should().BeTrue();
    }

    [Fact]
    public async Task UpdatePositionAsync_PersistsLeftAndTop() {
        // Arrange
        var service = new NTPopoverService();
        var popover = await service.OpenAsync<TestComponent>();

        // Act
        await service.UpdatePositionAsync(popover, 320.5, 144.25);

        // Assert
        popover.Left.Should().Be(320.5);
        popover.Top.Should().Be(144.25);
    }

    private sealed class TestComponent : ComponentBase {
    }
}
